---
type: subsystem
title: Comparing Subsystem Overview
description: The condition evaluation subsystem — fluent ConditionBuilder state-machine DSL, ConditionDef/OperatorDef/ConditionDefConfig models, the Comparator interpretive + compiled-expression path, the operator-type catalog with native/equality/regex/in/contains operators, and enum coercion.
tags: [comparing, conditions, operators, expression-trees, dsl, toolkit-comparing]
---

# Comparing Subsystem Overview

The Comparing subsystem (namespace `HomebrewDot.Net.Rimworld.Comparing`, components in `.Comparing.Components`, models in `.Comparing.Models`, templates in `.Comparing.Template`) evaluates conditions against objects. It provides a fluent builder DSL that produces serializable condition definitions, and a `Comparator` that evaluates them via either an interpretive path or a compiled LINQ expression-tree path with caching. It is consumed by [Collecting](../collecting/overview.md) (via `CollectionComparator`) and indirectly by [Referencing](../referencing/overview.md) (operands are references the comparator resolves).

## Fluent builder DSL

`ConditionBuilder<TReturn>` (`Comparing/Models/ConditionBuilder.cs`) is an abstract base implementing a stage-machine of interfaces so the C# compiler enforces the order of the fluent chain. The non-generic `ConditionBuilder : ConditionBuilder<IConditionBuilder>, IConditionBuilder` is the entry point; the static factory `ConditionBuilder.Build(Action<IConditionBuilder>)` returns a single `ConditionDef` if `builder.Conditions.Count == 1`, or a wrapping `ConditionDef { Conditions = builder.Conditions.ToArray() }` when multiple conditions were built.

### Stage machine

The `_state` field encodes a strict state machine with these numeric values: **0** = initial, **1** = setting-left (after `Compare`), **2** = left-set (after `Reference(left)`), **3** = setting-operator (after `With`), **4** = operator-set (after `Operator`), **5** = setting-right (after `To`), **10** = group-set (after `Group`).

```
Compare .Value(x) .With .Equal() .To .Value(y) .And .Compare .Indexed("def.label") .With .GreaterThan() .To .Value(3) .Or ...
 ↑0→1    ↑1→2     ↑2→3   ↑3→4     ↑4→5        ↑5→finalize→0
```

| Stage interface | Transition | Throws if |
|-----------------|-----------|-----------|
| `IConditionBuilder<TReturn>.Compare` (getter) | 0→1 | `_state != 0` ("left operand already set") |
| `IConditionOperandBuilder<...>.Reference(IReference)` | 1→2 (left) / sets right at 5 | — |
| `IConditionToOperatorBuilder<TReturn>.With` (getter) | 2→3 | `_state != 2` |
| `IConditionWithBuilder<TReturn>.Operator(IOperator)`/`.Operator(string)` | 3→4 | `_state != 3` |
| `IConditionToRightBuilder<TReturn>.To` (getter) | 4→5 | `_state != 4` |
| `IConditionChainBuilder<TReturn>.And`/`.Or`/`.AndOr(bool)` | finalize + reset to 0 | `!(_state == 4 || _state == 5 || _state == 10)` |
| `Group(Func<IConditionBuilder,IConditionBuilder>)` | 0→10 (group) | `_state != 0` |

### `FinalizeCondition()`

Called by `And`/`Or`/`AndOr` and by the `Conditions` getter when `_state` is 4, 5, or 10. Appends a new `ConditionDef` to `_conditions` with: `Compare = _leftOperand`, `With = _operator`, `To = _rightOperand`, `IsOr = _isOr`, `Inverted = _isInverted`, `Conditions = _groupConditions?.ToArray() ?? Empty`, `ConditionGroupIsOr = (_state == 10) ? _isOr : false`. Then resets all transient fields: `_state = 0`, `_leftOperand = null`, `_operator = null`, `_rightOperand = null`, `_isOr = false`, `_isInverted = false`, `_groupConditions = null`.

### `Group` internals

`Group(Func<IConditionBuilder,IConditionBuilder>)` is only valid at `_state == 0`. It creates a **nested `ConditionBuilder`** (the non-generic entry point), invokes the group-builder on it, captures `nestedBuilder.Conditions` (which triggers `FinalizeCondition` on the nested builder) into `_groupConditions`, and sets `_state = 10`. The finalized `ConditionDef` will carry the nested conditions in its `Conditions` array and `ConditionGroupIsOr` reflects `_isOr`.

### `CompareFrom(IConditionDef condition)`

Copies a condition via the `ConditionDef(IConditionDef)` deep-copy constructor. If `Not()` was called before `CompareFrom`, the copied condition's `Inverted` is forced `true` and the local `_isInverted` cleared. Used by the Collecting layer to re-import conditions split from a built group.

### Inversion (`Not()` / `MarkCurrentInverted()`)

`Not()` is implemented at every stage (builder, compare, with, to) so it can appear before `Compare`, after `Compare`, after `With`, or after `To`. Each returns the same stage so chaining continues. `Not()` calls `MarkCurrentInverted()` which sets `_isInverted = true`; the finalized `ConditionDef.Inverted` reflects it.

### Extension methods (`ConditionBuilderExtensions`)

Natural-language DSL on `IConditionOperandBuilder<TReturn>`:

| Category | Methods | Produces `With` |
|----------|---------|-----------------|
| Operands | `.Value(x)`, `.Self()`, `.Property(name)`, `.Indexed(name)`, `.Stat(...)`, `.Comp(...)`, `.ThingCategory(name)`, `.StuffCategory(name)` | (operands; see [Referencing](../referencing/overview.md)) |
| Native | `.Equal()`, `.NotEqual()`, `.GreaterThan()`, `.GreaterThanOrEqual()`, `.LessThan()`, `.LessThanOrEqual()`, `.True()`, `.False()`, `.Null()`, `.NotNull()` | operator string via `NativeOperatorType.ToOperatorString()` |
| Match | `.Match(pattern)`, `.Match(Regex)` | `"Match"` |
| In | `.In()`, `.InBy(NativeOperatorType)` | `"In"` (or `OperatorDef` with `Arguments["NativeOperator"]`) |
| Contains | `.Contains()`, `.Contains(value)`, `.ContainsBy(NativeOperatorType)` | `"Contains"` (or `OperatorDef`) |
| ThingCategory | `.InThingCategory()`, `.InThingCategory(name)`, `.InThingCategory(ThingCategoryDef)` | `"InThingCategory"` |

## Def models

### `ConditionDef` (`Comparing/Models/ConditionDef.cs`)
```csharp
public class ConditionDef : IConditionDef, ICacheable
{
    public ConditionDef[] Conditions { get; set; }      // nested group
    public bool ConditionGroupIsOr { get; set; }         // how group relates to current condition
    public object Compare { get; set; }                 // left operand (IReference or raw)
    public object With { get; set; }                    // operator (IOperator, string, or OperatorDef)
    public object To { get; set; }                     // right operand (IReference or raw)
    public bool IsOr { get; set; }                     // how THIS relates to the NEXT in a list
    public bool Inverted { get; set; }                  // negates the result
}
```
- `GetCacheKey()` → `ToString(includeTypeNames: true)` — the stable string key used by `Comparator`'s compiled-delegate cache.
- `ToString` renders `(cond1 OR/AND cond2 ...)` for groups, and `Compare [not ] With [To]` for conditions.

### `OperatorDef` (`Comparing/Models/OperatorDef.cs`)
```csharp
public class OperatorDef : IOperator
{
    public string Type { get; set; }
    public IReadOnlyDictionary<string, object> Arguments { get; set; }
}
```
Used by `InBy`/`ContainsBy` to carry the native operator override in `Arguments["NativeOperator"]`.

### `ConditionDefConfig` (`Comparing/Models/ConditionDefConfig.cs`)
A Scribeable (`IExposable`) serialization model. Does **not** support nested conditions (`FromConditionDef` throws if `Conditions` is non-empty). Fields mirror `ConditionDef` with typed slots: `CompareDefault`/`CompareType`+`CompareValue`/`IsCompareReferenceMode` (compare side), `ToDefault`/`ToNumber`/`ToDecimal`/`ToType`(ConstantType)/`ToReferenceType`+`ToReferenceValue`/`IsToReferenceMode` (to side), `Operator`, `IsOr`, `Inverted`. `ToConditionDef()` maps non-reference compare to an `IndexedReferenceType` reference and non-reference to to a `ValueReferenceType` reference; `FromConditionDef` inspects the `To` reference's value type to pick `ToNumber`/`ToDecimal`/`ToDefault`.

## Comparator

`Comparator` (`Comparing/Components/Comparator.cs`) implements `IComparatorCompiler`. Constructor:
```csharp
Comparator(IReferenceResolver referenceResolver,
           IReadOnlyDictionary<string, IOperatorType> operatorTypes,
           Func<...> compareStringToReference = null,
           Func<...> operatorStringToOperator = null,
           Func<...> toStringToReference = null)
```

### Context dictionary keys (public constants)

| Constant | Value | Type expected | Purpose |
|----------|-------|---------------|---------|
| `ContextOperatorTypesKey` | `"OperatorTypes"` | `IReadOnlyDictionary<string, IOperatorType>` | Per-call operator-type override (wins over constructor) |
| `ContextReferenceResolverKey` | `"ReferenceResolver"` | `IReferenceResolver` | Per-call resolver override |
| `CompareStringToReferenceKey` | `"CompareStringToReference"` | `Func<...>` | Converts `Compare` strings → `IReference` |
| `OperatorStringToOperatorKey` | `"OperatorStringToOperator"` | `Func<...>` | Converts `With` strings → `IOperator` |
| `ToStringToReferenceKey` | `"ToStringToReference"` | `Func<...>` | Converts `To` strings → `IReference` |

### `Compare(object input, IConditionDef condition, context)` — interpretive + compiled-cache

1. If `condition.With == null` and no `Conditions` → throws `InvalidOperationException`.
2. If `condition is ICacheable` with a non-null `cacheKey`: check the per-instance `_compiledComparisonsCache`; hit → invoke compiled `Func<object, context, bool>`; miss → build via `CompileCondition`, compile, cache, invoke.
3. Interpretive group: if `isGroupCondition`, recursively `Compare(input, condition.Conditions, context)`. `ConditionGroupIsOr` short-circuits: OR satisfied → true; AND failed → false.
4. Resolve operands: `compareValue` via `ResolveValue`; `withValue` via `GetOperatorType` (looks up `IOperatorType`); `toValue` via `ResolveValue`.
5. `conditionResult = operatorType.Compare(compareValue, toValue, arguments, context)`.
6. If `Inverted` → negate.
7. If group: `ConditionGroupIsOr ? groupResult || conditionResult : groupResult && conditionResult`.

### `Compare(input, IReadOnlyList<IConditionDef>, context)` — list evaluation
Empty list → false. Iterates left-to-right; `IsOr` on condition `i` controls how condition `i+1` combines: a chain `c0(And) c1(Or) c2(And)` evaluates as `((c0 AND c1) OR c2)`. This matches the builder where `And`/`Or` sets `IsOr` on the condition being finalized.

### `CompileCondition` — expression-tree compilation
Builds a LINQ expression over `(object input, IReadOnlyDictionary<string,object> context)` parameters:
- Each operand: `CompileGetter` resolves the reference. If the resolver is `IReferenceTypeResolver` and the `IReferenceType` is `IReferenceTypeCompileable`, its `Compile(...)` is inlined; otherwise a virtual `Resolve` call is emitted.
- The operator: `CompileComparison` — if `IOperatorTypeCompileable`, inlines `compileable.Compile(...)`; otherwise a virtual `Compare` call with boxing.
- Groups: recursively compiled, folded left with `AndAlso`/`OrElse` based on each sub-condition's `IsOr`.
- `Inverted` → `Expression.Not`.

## Operator types

All operator types are singletons (`Instance`). Registration in [`Toolkit.ConfigureServices`](../facades/toolkit.md) registers each under all its aliases.

### Base classes (`Comparing/Template/`)

**`BaseNativeOperatorType`** — shared machinery for C# native operators (`==`, `!=`, `>`, `<`, `>=`, `<=`, `op_True`, `op_False`). Static `Dictionary<Type, Dictionary<Type, Dictionary<string, Func<object,object,bool>>>` method cache. `NormalizeEnumOperands` coerces enum operands to a common numeric type (fixes `QualityCategory` byte-backed vs `int`). `TryCompile` builds `Expression.Call(method, left, right)` for binary ops or `Expression.Or(Call(left), Call(right))` for True/False; falls back to `Expression.Equal`/etc. factory methods with `GuardNulls` (null-guarded comparison for reference types). `NativeOperatorType` enum + `NativeOperatorTypeExtensions` map to method names, operator strings, and `Expression` factories.

**`BaseComparableOperatorType`** extends `BaseNativeOperatorType` and implements `IOperatorTypeCompileable`. Constructor takes `(NativeOperatorType, Func<IComparable, object, bool> comparableFunc, allowPositionSwap)`. Interpretive: tries native operator, falls back to `IComparable.CompareTo` via `comparableFunc`. `allowPositionSwap` (used by Equals/NotEquals) swaps operands so `null == 5` works as `5 == null`.

### Catalog

| Class | DefaultTypeName | Aliases | Base | Compileable | Compares |
|-------|-----------------|---------|------|-------------|----------|
| `EqualsOperatorType` | `"Equals"` | `Equal`, `eq`, `==`, ... | `BaseComparable(Equal, x=>CompareTo==0, swap:true)` | ✅ | `==` or `CompareTo == 0` |
| `NotEqualsOperatorType` | `"NotEquals"` | `NotEqual`, `neq`, `!=`, ... | `BaseComparable(NotEqual, swap:true)` | ✅ | `!=` or `CompareTo != 0` |
| `GreaterOperatorType` | `"GreaterThan"` | `Greater`, `gt`, `>`, ... | `BaseComparable(GreaterThan)` | ✅ | `>` or `CompareTo > 0` |
| `GreaterOrEqualOperatorType` | `"GreaterThanOrEqual"` | `>=`, ... | `BaseComparable(GreaterThanOrEqual)` | ✅ | `>=` |
| `LesserOperatorType` | `"LessThan"` | `Less`, `lt`, `<`, ... | `BaseComparable(LessThan)` | ✅ | `<` |
| `LesserOrEqualOperatorType` | `"LessThanOrEqual"` | `<=`, ... | `BaseComparable(LessThanOrEqual)` | ✅ | `<=` |
| `TrueOperatorType` | `"True"` | `True`, `Y`, `Yes`, `true` | `BaseNativeOperatorType` | ✅ | `op_True(left)` or `op_True(right)`; bool→value; int→`>0` |
| `FalseOperatorType` | `"False"` | `False`, `N`, `No`, `false` | `BaseNativeOperatorType` | ❌ (IOperatorType only) | `op_False`; `!bool`; `int <= 0` |
| `NullOperatorType` | `"Null"` | `IsNull`, `Undefined`, `None` | `DelegateOperatorType` | ✅ | `left == null` |
| `NotNullOperatorType` | `"NotNull"` | `IsNotNull`, `Defined`, `Any` | `DelegateOperatorType` | ❌ | `left != null` |
| `MatchOperatorType` | `"Match"` | `Matches`, `Regex` | (direct) | ✅ | `Regex.IsMatch(left.ToString(), right)`; null→false; cache key only for string/Regex right |
| `InOperatorType` | `"In"` | — | `BaseNativeOperatorType` | ✅ | left in enumerable right; default native `Equal`, overridable via `Arguments["NativeOperator"]`; compiles a loop via `Expression.CompileLoop` |
| `ContainsOperatorType` | `"Contains"` | — | `BaseNativeOperatorType` | ✅ | left collection contains right; same `NativeOperator` override; iterates left |
| `InThingCategoryOperatorType` | `"InThingCategory"` | — | (direct) | ❌ | `ThingDef.IsWithinCategory(categoryDef)`; unwraps `IIndexed<object>`/Thing.def first |
| `DelegateOperatorType` | — | — | (direct) | ❌ | Wraps a `Func<object,object,dict,dict,bool>`; test/utility base for Null/NotNull |

### Enum coercion

`BaseNativeOperatorType.NormalizeEnumOperands` converts enum operands to a common numeric type so byte-backed enums (e.g. `QualityCategory`) compare correctly against `int`. This is proven by `EnumComparisonOperatorTests`: `Lesser(QualityCategory.Awful, 2) → true`, `Greater(QualityCategory.Good, 2) → true`, and the compile path mirrors the interpretive path.

## Registration

`Toolkit.ConfigureServices()` registers each operator type once per alias. Each operator type exposes a static `Aliases` collection (e.g. `EqualsOperatorType.Aliases = { "Equal", "eq", "Equals", "==" }`). `ConfigureServices` loops each alias calling `Services.Register<IOperatorType>(instance, alias)` — so the same singleton is registered under every alias name. `InOperatorType`/`ContainsOperatorType`/`InThingCategoryOperatorType` are registered under their `DefaultTypeName` only. The default `CollectionComparator` is built lazily from `Services.GetAllNamed<IOperatorType>()`. Integration tests call `Toolkit.ConfigureServices()` in their constructors to ensure the registry is populated. See [Toolkit facade](../facades/toolkit.md).

### `GetOperatorType` resolution chain

`Comparator.GetOperatorType(condition, @operator, context, out arguments)` resolves the `IOperatorType` instance:
1. If `@operator` is a **string**: `operatorType = operatorString`; then if context key `"OperatorStringToOperator"` yields a `Func<IConditionDef, dict, string, IOperator>` delegate, invoke it to get an `IOperator` whose `.Type` and `.Arguments` are extracted; else if the constructor-supplied `_operatorStringToOperator` delegate exists, invoke it instead.
2. If `@operator` is an **`IOperator`**: `operatorType = @operator.Type`, `operatorArguments = @operator.Arguments`.
3. Look up the operator type: context `"OperatorTypes"` dict (per-call override) → constructor `_operatorTypes` dict → throw `InvalidOperationException`. Both dictionaries fall back to `NullDictionary` when null (no allocation, returns false on lookup).

## Focused tests

- **`OperatorTypesTests`**: basic behavior for all operators (`Equals(5,5)→true`, `NotEquals(5,6)→true`, `Greater(6,5)→true`, etc.); alias presence; `True`/`False`; `Null`/`NotNull` (incl. value-type int); `Match` (string/Regex, null handling); `In`/`Contains` (array, list, singleton, empty, null collection, NativeOperator override); compile path mirrors interpretive.
- **`EnumComparisonOperatorTests`**: byte-backed enum vs int and enum-vs-enum for all relational operators; `GetCacheKey` non-null; compile path; end-to-end through `Comparator` with a mock resolver.
- **`ContainsOperatorTypeTests`**: collection contains value (string array, List, singleton); not-in/empty/null; NativeOperator override; reference-type semantics (same instance vs different instance); compile path.
- **`ComparatorTests`**: missing `With`+`Conditions` throws; simple condition; context operator-type override; inverted condition (interpretive + compiled); `OperatorDef` with `Arguments`; group + current condition with `ConditionGroupIsOr`; 5+ condition AND/OR chaining (IsOr-on-previous semantics); string reference converters; unresolvable reference throws; list overload (null/empty/single); context resolver override.
- **`ConditionBuilderTests`**: valid build; state-machine enforcement (`With` before `Compare` throws, `To` before `With` throws, `And` mid-condition throws); multiple conditions; `Contains`/`InBy`/`ContainsBy`; `Not()` at various stages; `Not()` + chaining; `ToString` with `Not()`; `CompareFrom` preserves/forces `Inverted`.
- **`ConditionDefConfigTests`**: default field values; `ToConditionDef` from defaults (compare→Indexed, to→Value); inverted round-trip; reference-mode config; full round-trip preserves reference types/values/`With`/`IsOr`; `ExposeData`.
- **`FoulPresetGroupReproTests`**: reproduces the foul-meat-preset condition flow — conditions built, split (via `CompareFrom`), evaluated through `CollectionComparator`. Leaf AND OR-group; leaf AND AND-group; self-reference in leaf+group through compiled collection; TopLevelOr with moved item (OR branch) and foul meat (AND branch) and normal meat (no match).
- **`ComparingIntegrationTests`**: end-to-end with full `Toolkit.ConfigureServices()` — `Equals(5,5)→true`, `NotEquals(5,6)→true`, `Greater(3,5)→false`, `Null` on `Tentity.Text` (null) → true, `NotNull` on `Tentity.Text="hello"` → true.

## Validation

```bash
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj --filter "FullyQualifiedName~Comparing"
dotnet test tests/Integration/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.IntegrationTests.csproj --filter "FullyQualifiedName~ComparingIntegration"
```
