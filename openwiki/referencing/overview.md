---
type: subsystem
title: Referencing Subsystem Overview
description: The late-bound value resolution subsystem — IReference/ReferenceDef models, ReferenceResolver with compile path and context overrides, the reference-type catalog (Value/Self/Property/Indexed/Stat/Comp/DefReferenceType/DelegateReferenceType), registration via ConfigureServices, and the resolution flow through the Comparator and SnapshotCollector.
tags: [referencing, references, resolution, expression-trees, toolkit-referencing]
---

# Referencing Subsystem Overview

The Referencing subsystem (namespace `HomebrewDot.Net.Rimworld.Referencing`, components in `.Referencing.Components`, models in `.Referencing.Models`) resolves operands to concrete values at evaluation time. A condition's `Compare` and `To` operands are `IReference` objects; the `ReferenceResolver` looks up the matching `IReferenceType` by name from [`Toolkit.Services`](../facades/toolkit.md), then either resolves reflectively or compiles an expression tree. The [Comparator](../comparing/overview.md) calls the resolver during both interpretive and compiled evaluation.

## Contracts

### `IReference` (`Referencing/IReference.cs`)
```csharp
public interface IReference : ICacheable
{
    string Type { get; }   // lookup key into the reference-type dictionary
    object Value { get; }  // payload handed to the matching IReferenceType
}
```
Extends `ICacheable` so `ReferenceDef.GetCacheKey()` feeds the [Comparator's](../comparing/overview.md) compiled-comparison cache.

### `IReferenceResolver` / `IReferenceTypeResolver` (`Referencing/IReferenceResolver.cs`)
```csharp
public interface IReferenceResolver
{
    bool TryResolve(object input, IReference reference, IReadOnlyDictionary<string, object> context, out object result);
}
public interface IReferenceTypeResolver : IReferenceResolver
{
    IReferenceType GetReferenceType(object input, IReference reference, IReadOnlyDictionary<string, object> context);
}
```
`IReferenceTypeResolver` enables the `Comparator.CompileGetter` path to access the compileable interface for inlining.

### `IReferenceType` / `IReferenceTypeCompileable` (`Referencing/IReferenceType.cs`)
```csharp
public interface IReferenceType
{
    object Resolve(object input, object value, IReadOnlyDictionary<string, object> context);
    bool RequiresValue { get; }   // false = fixed object (Self); true = needs Value
}
public interface IReferenceTypeCompileable : IReferenceType
{
    string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType);
    Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context);
}
```
`GetCacheKey` returns `null` to skip compilation caching. `returnType` is the type the compiled expression yields (used by `Comparator.CompileGetter` to set `compareType`). All built-in types except `DelegateReferenceType` implement `IReferenceTypeCompileable`.

## ReferenceDef

`ReferenceDef` (`Referencing/Models/ReferenceDef.cs`) is the concrete serializable `IReference`: settable `Type` and `Value` POCO. `GetCacheKey()` produces `{[Type]<value-key>}` via [`ObjectExtensions.ToCacheKey`](../generic/overview.md), feeding the comparator cache so identical references reuse compiled lambdas.

## ReferenceResolver

`ReferenceResolver` (`Referencing/Components/ReferenceResolver.cs`) implements `IReferenceTypeResolver`.

### Fields and context key

| Symbol | Purpose |
|--------|---------|
| `_compiledResolversCache` (static) | Process-wide cache of compiled resolver delegates keyed by `IReferenceTypeCompileable.GetCacheKey`. |
| `ContextReferenceTypesKey = "ReferenceTypes"` | Context key for per-call override reference types (value must be `IReadOnlyDictionary<string, IReferenceTypeCompileable>`). |
| `_referenceTypes` | Constructor-supplied type registry (falls back to `NullDictionary` when null). |

### `GetReferenceType` flow

1. Read `context["ReferenceTypes"]` as `IReadOnlyDictionary<string, IReferenceTypeCompileable>` (context wins).
2. `reference.Type.Trim()` lookup: context first, then constructor `_referenceTypes`, else `null`.
Type names are **whitespace-tolerant** (trimmed). If `context["ReferenceTypes"]` is the wrong type, the context is ignored and the constructor dictionary is used.

### `TryResolve` flow

1. `GetReferenceType` → if null, return `false` + `null`.
2. If `IReferenceTypeCompileable` with a non-null cache key: look up `_compiledResolversCache`; on miss, `GetCompiledResolver` compiles `referenceType.Compile(...)` into a `Func<object, object, dict, object>` lambda and stores it; invoke the cached delegate.
3. Else (not compileable, or null cache key): call `referenceType.Resolve(input, reference.Value, context)`.

### `GetCompiledResolver`

Builds a lambda over `(object input, IReadOnlyDictionary<string,object> context)` parameters; the concrete `value`/`context`/`input` are captured at compile time. `ReferenceDef.GetCacheKey()` and the cache ensure identical references reuse compiled resolvers.

## Reference-type catalog

All built-in types are singletons (`Instance`) with a `DefaultTypeName` const. Registration in [`Toolkit.ConfigureServices`](../facades/toolkit.md) keys each by its `DefaultTypeName`.

| Type | DefaultTypeName | Compileable | RequiresValue | Resolves | Compile expression |
|------|-----------------|-------------|---------------|----------|--------------------|
| `ValueReferenceType` | `"Value"` | ✅ | true | Returns `value` as-is | `Expression.Constant(value)` or `Default` if null |
| `SelfReferenceType` | `"Self"` | ✅ | **false** | Returns `input` | `inputParameter` (identity) |
| `PropertyReferenceType` | `"Property"` | ✅ | true | `Helpers.Traversing.TraversePath(input, split(value))` — dotted property path | `Helpers.Traversing.GenerateFullGetter(...)` compiled getter chain; null when input/value null |
| `IndexedReferenceType` | `"Indexed"` | ✅ | true | If `input is IIndexed<object>`: `GetValue<object>(name)` (single-segment) or `GetValue` + `TraversePath` (multi-segment). Else falls back to `TraversePath`. `GetCacheKey`: returns `null` (uncacheable) when `input` or `value` is null; otherwise computes `returnType` via `Helpers.Traversing.TryWalkIndexedPath(input.GetType(), value.ToString())` and returns `"{input.GetType().FullName}:{value}"` as the cache key. | Block: member access on `indexed.Value` then `indexed.Metadata[key]` dict lookup; same as Property for non-indexed |
| `StatReferenceType` | `"Stat"` | ✅ | true | `StatDef` lookup; `IIndexed<Def>`/`Def` → `GetValueAbstract(buildableDef)`; `IIndexed<Thing>`/`Thing` → `GetValue(thing)`. Null otherwise. | Def path: `GetValueAbstract` block; Thing path: `Call(GetValue(thing))`; returns `float` |
| `CompReferenceType` | `"Comp"` | ✅ | true | `value` → comp `Type` (direct or name→`Toolkit.Cache`). Optional `"\|"` path separator for sub-property. Def input: `GetCompProperties<T>(ThingDef)`; Thing input: `TryGetComp<T>(Thing)`. Then optional `TraversePath`. | Def → `GetCompProperties` call; Thing → `TryGetComp` call; pipe path wraps with `GenerateFullGetter`; caches compiled lambdas in `Toolkit.Cache` |
| `DefReferenceType<T>` | `typeof(T).Name` (e.g. `"ThingCategoryDef"`) | ✅ | true | `DefDatabase<T>.GetNamedSilentFail(defName)` | `Expression.Constant(def, typeof(T))` (resolved at compile time) or `Constant(null, typeof(T))` if not found |
| `DelegateReferenceType` | (none — instance) | ❌ | true | Invokes the constructor-supplied `Func<object,object,dict,object>` delegate | N/A |

### Path separator

`CompReferenceType.PathSeparator = '|'` — allows `"CompGlower|compClass"` to resolve the comp then traverse a sub-property. The [UI input helper](../ui/overview.md) `CompReferenceTypeInputHelper.BuildValue` produces this format.

## Registration

`Toolkit.ConfigureServices()` registers reference types into `Toolkit.Services` by name:
```csharp
Services.Register<IReferenceType>(IndexedReferenceType.Instance, "Indexed");
Services.Register<IReferenceType>(PropertyReferenceType.Instance, "Property");
Services.Register<IReferenceType>(ValueReferenceType.Instance, "Value");
Services.Register<IReferenceType>(StatReferenceType.Instance, "Stat");
Services.Register<IReferenceType>(CompReferenceType.Instance, "Comp");
Services.Register<IReferenceType>(DefReferenceType<ThingCategoryDef>.Instance, "ThingCategoryDef");
Services.Register<IReferenceType>(DefReferenceType<StuffCategoryDef>.Instance, "StuffCategoryDef");
Services.Register<IReferenceType>(SelfReferenceType.Instance, "Self");
```
`DelegateReferenceType` is **not** registered by default (for custom runtime use).

The default `CollectionComparator` (built by `Toolkit.Collecting`) lazily creates:
```csharp
var referenceTypes = Services.GetAllNamed<IReferenceType>();
var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
var operatorTypes = Services.GetAllNamed<IOperatorType>();
new CollectionComparator(new Comparator(referenceResolver, operatorTypes));
```

## Fluent extension methods

All operate on `IConditionOperandBuilder<TReturn>` and call `builder.Reference(new ReferenceDef { Type = ..., Value = ... })`:

| Extension | `Type` | `Value` |
|-----------|--------|---------|
| `.Value(x)` | `"Value"` | `x` |
| `.Self()` | `"Self"` | null |
| `.Property(name)` | `"Property"` | `name` (Guard.NotNullOrEmpty) |
| `.Indexed(name)` | `"Indexed"` | `name` |
| `.Stat(name)` / `.Stat(StatDef)` | `"Stat"` | `name` or `statDef` |
| `.Comp(Type)` / `.Comp(name)` | `"Comp"` | `compType` or `compName` |
| `.ThingCategory(name)` | `"ThingCategoryDef"` | `name` |
| `.StuffCategory(name)` | `"StuffCategoryDef"` | `name` |

## Resolution flow (Comparator → ReferenceResolver)

The [Comparator](../comparing/overview.md) resolves operands in both paths:

**Runtime** (`Comparator.Compare` → `ResolveValue`): if operand is a string and a string-to-reference delegate exists, convert to `IReference`. If `IReference`, call `GetReferenceResolver(context)` (context key `"ReferenceResolver"` wins over constructor), then `resolver.TryResolve(input, reference, context, out resolved)` — throws `InvalidOperationException` if null or fails.

**Compile** (`Comparator.CompileGetter`): if the resolver is `IReferenceTypeResolver`, `GetReferenceType(input, reference, context)` → if `IReferenceTypeCompileable`, `GetCacheKey` (sets `returnType`) + `Compile(...)` inlines the resolution; else emits a virtual `Resolve` call. If the resolver is not `IReferenceTypeResolver`, emits a call to `Comparator.Resolve`.

### SnapshotCollector context

`SnapshotCollector<T>` uses a static `CompareContext`:
```csharp
{ Comparator.CompareStringToReferenceKey, (condition, ctx, str) =>
    new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = str } }
```
This maps plain string property names in serialized collection conditions to `IndexedReferenceType` references (so they resolve against `IIndexed<T>` objects). The collector does not inject `"ReferenceTypes"` or `"ReferenceResolver"` overrides — it relies on the globally-configured `Comparator`'s constructor resolver.

## Focused tests

- **`ReferenceResolverTests`**: null registry → `NullDictionary` fallback, unknown type → false; null reference throws; known constructor type resolves; context types override constructor; context missing name falls back to constructor; type name trimmed; value/context forwarded; context key wrong type falls back to constructor.
- **`ReferenceTypeTests`** (`DelegateReferenceTypeTests`, `IndexedReferenceTypeTests`, `ReferenceTypeRequiresValueTests`): delegate invokes/forwards/returns; Indexed null value/context/input → null, `IIndexed` input calls `GetValue`, `DefaultTypeName == "Indexed"`; `RequiresValue` for all types (Self=false, all others true).
- **`PropertyReferenceTypeTests`**: null value/context → null; single-level and nested property resolution; integer property; singleton identity; `.Property("MyProp")` produces correct `ReferenceDef`; null/empty name guards.
- **`ReferenceResolverIntegrationTests`**: full `Toolkit.ConfigureServices()`; `≥` value conditions (equal/greater/less, cross-type float/int); property/comp reference resolution on `Tentity` (incl. null handling); uses `NonCompileableProxy` to exercise the non-compiled path (notes a pre-existing compiled-resolver lambda parameter mismatch worked around by the proxy).

## Validation

```bash
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj --filter "FullyQualifiedName~Referencing"
dotnet test tests/Integration/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.IntegrationTests.csproj --filter "FullyQualifiedName~ReferenceResolver"
```
