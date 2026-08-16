---
type: subsystem
title: Collecting Subsystem Overview
description: The named live collection subsystem — Toolkit.Collecting facade, CollectionBuilder fluent DSL, CollectionDef/StaticCollectionDef models, CollectionComparator evaluation engine, SnapshotCollector and MonitorCollector lifecycle, sub-collection references, and the OnCollectionsChanged trigger.
tags: [collecting, collections, snapshot-collector, monitor-collector, toolkit-collecting]
---

# Collecting Subsystem Overview

The Collecting subsystem (namespace `HomebrewDot.Net.Rimworld.Collecting`, components in `.Collecting.Components`, models in `.Collecting.Models`, triggers in `.Collecting.Triggers`) maintains named, deduplicated live sets of game objects that match declarative conditions. A collection is built with a fluent DSL (inheriting the [Comparing](../comparing/overview.md) condition DSL), evaluated by `CollectionComparator`, and kept in sync by a collector that either reacts to [Indexing](../indexing/overview.md) snapshots (`SnapshotCollector<T>`) or to another collection's runtime events (`MonitorCollector<T>`). The public surface is `Toolkit.Collecting` on the [Toolkit facade](../facades/toolkit.md).

## Toolkit.Collecting facade

`Toolkit.Collecting` (`[StaticConstructorOnStartup] public static class` in `Toolkit.cs`) holds three dictionaries (all `OrdinalIgnoreCase`):

| Member | Behavior |
|--------|----------|
| `Comparator` (`ICollectionComparator`) | Lazy default: `new CollectionComparator(new Comparator(referenceResolver, operatorTypes))` built from `Toolkit.Services`. Setter disposes old if `IDisposable`. `ReloadDefaultComparator()` nulls `_comparator` only if it is a `CollectionComparator` (the default type), forcing the next `Comparator` getter to rebuild from current `Toolkit.Services` registrations. A custom (non-`CollectionComparator`) comparator is not auto-replaced. |
| `Build(name, Func<ICollectionBuilder, ICollectionBuilder>, startCollecting=true)` | Runs the builder, wraps `builder.Collection` in `new StaticCollectionDef(...)`. If `TryBuildCollector` succeeds → `Set(name, collector, startCollecting)`. Else → `Set(name, collection)` (def-only, no collector). |
| `BuildOnly(buildAction) → (ICollectionDef, ICollector)` | Same as Build but does not register; returns the def and optional collector. |
| `Set(name, ICollectionDef)` | Adds/overwrites def; fires `OnCollectionsChanged(name, def, null, true)`. |
| `Set(name, ICollector, startCollecting=true)` | Adds collector; `Set(name, collector.Definition)`; if `startCollecting` → stop+start with the facade's `Comparator` and `_collectionDefinitions`; fires `OnCollectionsChanged(..., collector, true)`. |
| `Remove(name)` | Stops+disposes collector; removes from both dictionaries; fires `OnCollectionsChanged(..., false)` if anything removed. |
| `StartCollection()` | Stops **all** collectors, then starts **all** with current `Comparator` + `_collectionDefinitions`. Use after changing defs/comparator. |
| `GetAllDefinitions()` / `GetAllCollectors()` | Return **copies** of the dictionaries. |

### Cache warmup (static constructor)

On `OnSaveLoadedTrigger`: calls `WarmupCache()` and registers an `OnCollectionsChanged` hook (priority `byte.MaxValue`) to call `WarmupCache(true)` whenever collections change. `WarmupCache(reset)`: if `reset` is true, calls `CollectionComparator.ClearCache()` (which clears only the `_compiledCollectionWithSubExpressionsCache` — the sub-collection expression cache — forcing recompilation of collections with sub-collections); scans all definitions and all types assignable to `Thing`/`ThingDef` (plus their indexed/tracked generic variants); calls `comparator.WarmupCache(allCollections, allTypes)` — which builds `_cacheWarmers` by making a generic `Matches<T>` method via `_matchMethod.MakeGenericMethod(type)`, compiling a lambda that casts the input and calls `Matches`, then pre-compiling expression trees for all collection/type combinations via `FormatterServices.GetUninitializedObject`.

## CollectionBuilder fluent DSL

`CollectionBuilder<TReturn>` (`Collecting/Models/CollectionBuilder.cs`) extends `ConditionBuilder<TReturn>` (from [Comparing](../comparing/overview.md)), so it inherits the full `Compare...With...To...And/Or/Group/Not` condition DSL. It adds collection-level methods:

| Method | Effect |
|--------|--------|
| `CollectWith(Func<ICollectionDef, ICollector>)` | Stores the collector factory; called later by `TryBuildCollector`. |
| `CollectFromSnapshot(getDataInfo, getThingsToPush, isStatic)` | Extension → `CollectWith(def => new SnapshotCollector<T>(...))`. |
| `CollectFromSnapshot(tableName, isStatic)` | Extension resolving a specific indexing table by name. |
| `CollectFromCollection(monitoredCollectionName)` | Extension → `CollectWith(def => new MonitorCollector<T>(...))`. |
| `IncludeFrom(...)`, `OrIncludeFrom(...)` | Adds inclusions; `IncludeFrom` clears + sets `InclusionsAreOr=false`; `OrIncludeFrom` appends + sets `InclusionsAreOr=true`. |
| `ExcludeFrom(...)` | Clears + sets exclusions. |
| `FromDef(ICollectionDef)` | Imports conditions + inclusions + exclusions + `InclusionsAreOr` from an existing def (clears current state). |

`CollectionConditionBuilder` is the nested DSL for inclusion/exclusion entries: `.Collection("name").By("propertyPath").And` / `.NotCollection("name")`.

The `Collection` property builds a `CollectionDef`; throws `InvalidOperationException` if the condition builder is mid-flight.

## Def models

| Type | Role |
|------|------|
| `CollectionDef` | Mutable POCO: `ConditionDef[] Conditions`, `CollectionConditionDef[] Inclusions/Exclusions`, `bool InclusionsAreOr`. `CombinedConditions` is lazy (builds a single `ConditionDef { Conditions = Conditions }`). `ICacheable` via `ToString(includeTypeNames)`. |
| `StaticCollectionDef` | Immutable wrapper over `CollectionDef`: deep-copies into read-only props; `CombinedConditions` pre-computed; `GetCacheKey()` returns an **MD5 hash** of the source `CollectionDef.GetCacheKey()`. This is the form the facade registers via `Build`. |
| `CollectionDefConfig` | Scribeable (`IExposable`) serialization mirror using `List<ConditionDefConfig>`/`List<CollectionConditionDefConfig>`; `ToDef()`/`From(CollectionDef)` round-trip. |
| `CollectionConditionDef` | POCO: `Name`, `By`, `Inverted`, `IsOr`. `By` is the sub-property path for cross-collection comparison (e.g. `"def"`). |

`ICollectionDef.HasSubCollections()` → true if `Inclusions?.Count > 0 || Exclusions?.Count > 0`.

## CollectionComparator

`CollectionComparator` (`Collecting/Components/CollectionComparator.cs`) wraps an `IComparator` (from [Comparing](../comparing/overview.md)). Constructor: `CollectionComparator(IComparator comparator)`. `ClearCache()` clears only the `_compiledCollectionWithSubExpressionsCache` (the sub-collection expression cache), forcing recompilation of collections with sub-collections on next access. The simple-collection cache (`_compiledCollectionExpressionsCache`) is not cleared.

### Evaluation: `Matches<T>(collection, obj, collections, context)`

Non-cached path:
1. **Exclusions** first: if `MatchesCollections(Exclusions, obj, ...)` → return `false` (short-circuit).
2. **Conditions** via `_comparator.Compare(obj, collection.CombinedConditions, context)`.
3. **Inclusions** via `MatchesCollections(Inclusions, obj, ...)`:
   - `InclusionsAreOr=false` (AND): `conditionsMet = conditionsMet && inclusionsMet` (short-circuits if conditions already false).
   - `InclusionsAreOr=true` (OR): `conditionsMet = conditionsMet || inclusionsMet`.
   - Only inclusions (no conditions): `conditionsMet = inclusionsMet`.

### `MatchesCollections` — sub-collection chain (AND-group / OR-chain)

Contiguous terms where `IsOr == false` form an AND-group; a term with `IsOr == true` ends the current group. Final result is `true` if **any** AND-group passes. Each term: resolve the referenced collection by name (throws `InvalidOperationException` if missing), recursively `Matches`, apply `Inverted`.

### Compiled-cache path

If the collection is `ICacheable` with a non-null cache key, selects `_compiledCollectionExpressionsCache` (simple) or `_compiledCollectionWithSubExpressionsCache` (when `collection.HasSubCollections()`), keyed by (collection cacheKey → obj.Type → compiled `Func<object, context, bool>`). On first miss: `Compile(...)` builds a LINQ expression tree, compiles, caches, invokes. `WarmupCache` pre-compiles all collection/type combinations via `FormatterServices.GetUninitializedObject`.

### `Compile` expression-tree structure

`Compile` builds the full match expression:
1. **Exclusions**: if present, compiles each exclusion ref via `CompileCollectionRef` and chains them with `OrElse`/`AndAlso` based on each `IsOr` (first expression is the seed; `exclusionExpressions[i-1].IsOr` controls `OrElse` vs `AndAlso` for the next). The exclusion chain is wrapped in `Expression.Not(isExcluded)`.
2. **Conditions**: if the comparator implements `IComparatorCompiler`, inlines via `expressionCompiler.Compile(inputParameter, input, collection.CombinedConditions, contextParameter, context)`; otherwise emits a virtual `Compare` call with `Expression.Constant(comparator)` and `Expression.Constant(collection.CombinedConditions)`.
3. **Inclusions**: if present, same `CompileCollectionRef` + `OrElse`/`AndAlso` chain as exclusions. Combined with conditions via `InclusionsAreOr ? OrElse : AndAlso`. If only inclusions (no conditions), `itemIsMatch = isIncluded`.
4. **Final**: `isExcluded is null ? itemIsMatch : AndAlso(itemIsMatch, Not(isExcluded))`. If `itemIsMatch` is null (no conditions, no inclusions), returns `Not(isExcluded)` or `Constant(false)`.

### `CompileCollectionRef` by-path behavior

`CompileCollectionRef(inputParameter, by, ...)` handles sub-collection references that traverse a property path before evaluating the sub-collection:
1. If `by` is null/whitespace, compiles the sub-collection directly against the input.
2. Otherwise, calls `Helpers.Traversing.TryWalkIndexedPath(input.GetType(), by)` to determine the expected type. Creates a `Block` expression: declares a typed variable (`byVariable`), assigns it via `GenerateFullGetter(inputParameter, inputType, by)` (a null-safe compiled getter chain), then recursively calls `Compile(byVariable, ...)` against the traversed value — so the sub-collection's conditions are evaluated against the property-path result, not the original input.

Context key `"Comparator"` (`ContextComparatorKey`) overrides the comparator per-call.

## Collector<T> — the base

`Collector<T>` (`Collecting/Components/Collector.cs`) is `ICollector<T> where T : class`. Internal storage: `HashSet<T> _collected`.

| Member | Behavior |
|--------|----------|
| `StartCollecting(comparer, collections)` | Stores comparer + collections, **clears** `_collected`. |
| `StopCollecting()` | Nulls comparer + collections, **clears** `_collected`. |
| `Clear()` | Fires `OnClear` with current contents, then clears. Does **not** null the comparer (can still collect after). |
| `CanCollect(obj, context)` | Non-mutating predicate; false if null or not started. |
| `Collect(obj, context)` | Calls `comparer.Matches`, then `HandleMatch`. If match → `_collected.Add`; on success fires `OnCollected`, returns true. If no match → `Remove` (self-correcting: removes previously-collected items that no longer match). |
| `Collect(IEnumerable<T>, context)` | Batch via `comparer.Matches` (plural). |
| `Remove(obj)` | `_collected.Remove`; on success fires `OnRemoved`. |
| `GetAll()` | Returns a **copy** (array). |
| `Contains(obj)` | `_collected.Contains`. |
| `Count` | `_collected.Count`. |

Events: `OnCollected` (add), `OnRemoved` (remove), `OnClear` (pre-clear snapshot).

## SnapshotCollector<T>

`SnapshotCollector<T>` (`Collecting/Components/SnapshotCollector.cs`) extends `Collector<T>` and implements `IHook<OnSnapshotTakenTrigger>`. It pulls items from the immutable [IReadOnlyDatabase](../indexing/database-and-snapshots.md) snapshot.

| Property | Value |
|----------|-------|
| `Owner` | `this` |
| `Once` | `isStatic` (if true, only processes the first snapshot) |
| `Priority` | `byte.MaxValue` (highest) |

`OnTrigger(OnSnapshotTakenTrigger)`:
1. Gets `IDatabaseObject` from the snapshot. Skips if null or version matches `_lastVersion`.
2. If a previous cooperative work (`_lastWork`) is still running and not finished: cancels it via `_lastWork.Cancel()` and resets `_lastVersion = -1` — **unless** `arg.IsForced`, in which case the warning is suppressed but cancellation still occurs. Always sets `_lastWork = null` after.
3. If `Once` (static) or `IsForced` → `LoadFrom` synchronously with `context.NoInterval()`.
4. Else → wraps `LoadFrom` in a `RaiseCooperativeWork<WorkContext>` and triggers it via `_hookmanager.Trigger(work)` (cooperative/yielding). If the hook manager rejects (returns `false`), falls back to synchronous `context.NoInterval()` + `LoadFrom`. On acceptance, tracks the work in `_lastWork = work`.

`LoadFrom` (returns `IEnumerable` for cooperative yielding):
- Sets `workContext.CheckInterval = 8` so `WaitForNextTick` yields every 8 items.
- **Incremental** (when `data.TrackingChanges && data.Version - 1 == _lastVersion && data.Version > 0`): processes only `Changed` and `Deleted` items.
- **Full reload** (else): `Clear()`, then pulls all items from `_getThingsToPush(snapshot)`.
- After processing all items, sets `_lastVersion = data.Version`.

`StartCollecting` override: calls `base.StartCollecting(comparer, collections)` (which clears `_collected` and stores comparer + collections), then `_hookmanager.RegisterHook(this)`, then `Autodex()`, and if a current snapshot exists, triggers `OnTrigger` with `IsForced=true` (immediate initial load).

`Autodex()`: walks `Definition.Conditions` (and recursively `Inclusions`/`Exclusions` via `_collections`) to find property paths, then calls `Toolkit.Indexing.Indexers.ByPath(typeof(T), property)` to ensure those properties are indexed. The static `CompareContext` dictionary wires `Comparator.CompareStringToReferenceKey` to a delegate that converts string compare-operands to `new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = str }` — so plain property names in serialized conditions resolve against `IIndexed<T>` objects via the `IndexedReferenceType`.

`StopCollecting` override: clears, nulls, unregisters hook, cancels pending work.

## MonitorCollector<T>

`MonitorCollector<T>` (`Collecting/Components/MonitorCollector.cs`) extends `Collector<T>` and implements `IHook<OnCollectionsChanged>, IDisposable`. It is a **sub-collection** that depends on another named collection's runtime events.

| Property | Value |
|----------|-------|
| `Owner` | `this` |
| `Once` | `false` |
| `Priority` | `100` |

Constructor `MonitorCollector(ICollectionDef collectionDef, string monitoredCollectionName)`:
1. `CreateFullCollectionDef` **prepends** an inverted exclusion `{ Name = monitoredCollectionName, Inverted = true, IsOr = true }` — so items must NOT be excluded from the monitored collection (effectively must be in it). Wraps in `StaticCollectionDef`.
2. Registers as `IHook<OnCollectionsChanged>`.
3. `SubstribeToMonitoredCollection()` — looks up the monitored collector via `Toolkit.Collecting.GetAllCollectors()`; if found, subscribes to its `OnCollected`/`OnRemoved`; if not found, logs warning and retries on `OnCollectionsChanged`.

`OnTrigger(OnCollectionsChanged)`: if `arg.Name` matches and `Added` → resubscribe; if removed → unsubscribe.

`OnMonitoredCollected(obj)`: evaluates `_comparer.Matches(_collectionDef, obj, ...)` using the **original** def (not the augmented one); if match → `_collected.Add` directly (**does not fire its own `OnCollected`** — a known asymmetry).

`OnMonitoredRemoved(obj)` → `_collected.Remove` directly (no event).

`Dispose()` → `Toolkit.Hooks.Manager.UnregisterAllBy<OnCollectionsChanged>(this)`.

## Snapshot vs Monitor

| Aspect | `SnapshotCollector<T>` | `MonitorCollector<T>` |
|--------|------------------------|-----------------------|
| Source | `IReadOnlyDatabase` snapshot | Another `ICollector<T>` by name |
| Trigger | `OnSnapshotTakenTrigger` | `OnCollectionsChanged` |
| Priority | `byte.MaxValue` | `100` |
| Once | `isStatic` param | `false` |
| Incremental | Yes (tracks `_lastVersion`) | No (event-driven) |
| Cooperative work | Yes (yields every 8 items) | No (synchronous) |
| Autodex | Yes | No |
| Event firing | Via base `HandleMatch` | Direct set manipulation (no own events) |
| Extension | `CollectFromSnapshot` | `CollectFromCollection` |

## Lifecycle

<!-- openwiki: mermaid parse failed and this diagram was converted to a text fence so it does not break rendering. Fix the diagram source and restore the mermaid fence. Parser error: Heuristic: an unescaped angle bracket inside a label breaks rendering; rephrase the label. -->
```text
sequenceDiagram
    participant Mod as Mod code
    participant Facade as Toolkit.Collecting
    participant Def as CollectionDef
    participant Coll as SnapshotCollector<T>
    participant Cmp as CollectionComparator
    participant Snap as SnapshotManager

    Mod->>Facade: Build("Snipers", b => b.Compare...CollectFromSnapshot(...))
    Facade->>Def: new StaticCollectionDef(builder.Collection)
    Facade->>Coll: factory(def) → SnapshotCollector
    Facade->>Coll: StartCollecting(Comparator, definitions)
    Coll->>Coll: Autodex() → Indexers.ByPath
    Note over Coll: if snapshot exists → OnTrigger(IsForced=true)
    Snap->>Coll: OnSnapshotTakenTrigger
    Coll->>Coll: LoadFrom (incremental or full)
    loop each candidate
        Coll->>Cmp: Matches(def, item, collections, context)
        Cmp->>Cmp: exclusions → conditions → inclusions
    end
    Coll->>Coll: HandleMatch (add/remove, fire OnCollected/OnRemoved)
    Facade-->>Mod: OnCollectionsChanged(added=true) → WarmupCache
```

## Focused tests

- **`CollectionBuilderTests`**: single condition builds expected operands; `Equal` shortcut; `And`/`Or` chains set `IsOr`; `Group` builds nested; group + following condition; `Group` mid-condition throws; `TryBuildCollector` without factory returns false; `CollectWith` factory returns the collector; incomplete condition throws on `.Collection` access.
- **`CollectionComparatorTests`**: missing collection reference throws; exclusion match returns false even when conditions pass; AND mode requires both; OR mode allows either; inclusion AND/OR combinations; 5+ inclusion AND/OR chaining (AND-group/OR-chain); nested inclusions resolve recursively; nested exclusion excludes.
- **`CollectorTests`**: constructor guards; `CanCollect` before/after start; `Collect` dedup (same item twice → count 1); `Contains`; `GetAll` returns copy; `StartCollecting`/`StopCollecting`/`Clear` semantics.
- **`SnapshotCollectorTests`**: constructor guards; before snapshot all false/empty; `OnTrigger` pushes items; after trigger `Contains`/`GetAll` work; non-matching comparator → false; `StartCollecting` registers hook; `StopCollecting` unregisters; `Clear`.
- **`MonitorCollectorTests`**: constructor guards; adds prepended inverted exclusion; preserves existing exclusions; does not modify original def; `Collect` matching/non-matching; `OnMonitoredCollected` evaluates condition against the item.
- **`CollectingBuildIntegrationTests`**: `Build` registers def in `GetAllDefinitions`; non-matching returns empty; `GetAllCollectors` empty after cleanup; null name throws; `ReloadDefaultComparator` safe.
- **`CollectionIntegrationTests`**: end-to-end indexing real RimWorld `ThingDef` XML → build collection with `Match` on `defName` → `CollectFromSnapshot` → asserts Steel/Wood/Silver/Gold; nested null property path does not throw; deeply null path returns empty.

## Validation

```bash
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj --filter "FullyQualifiedName~Collecting"
dotnet test tests/Integration/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.IntegrationTests.csproj --filter "FullyQualifiedName~Collection"
```
