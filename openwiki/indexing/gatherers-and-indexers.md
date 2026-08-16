---
type: subsystem
title: Indexing Gatherers and Indexers
description: The data acquisition layer — DefGatherer, HarmonyThingGatherer, MapThingGatherer; TrackedIndexer and IIndexerBuilder enrichment; IChangeTracker/PropertyChangeTracker change detection; TypedSnapshotManager compiled tracker delegates; SnapshotOrchestrator lifecycle.
tags: [indexing, gatherers, indexers, change-tracking, orchestration]
---

# Indexing Gatherers and Indexers

The data-acquisition and enrichment layer of the [Indexing subsystem](overview.md). Namespace `HomebrewDot.Net.Rimworld.Indexing.Components` / `.Models`.

## Gatherers

`IDataGatherer` ([`Indexing/IDataGatherer.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/IDataGatherer.cs)) — `Initialize(Game)`, `GatherData(Game, ISnapshotManager)`, `Reset()`. Registered via `Toolkit.Indexing.ConfigureOrchestrator += b => b.With(gatherer)`.

### DefGatherer

`DefGatherer` ([`Indexing/Components/DefGatherer.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/DefGatherer.cs)) — singleton (`Instance`). Scans all loaded assemblies, finds all concrete `Def` subclasses, reads `DefDatabase<T>.AllDefsListForReading` via reflection, and pushes each def to `ISnapshotManager.Push`. Powers the [`Toolkit.Indexing.Def`](overview.md) root table.

### HarmonyThingGatherer

`HarmonyThingGatherer` ([`Indexing/Components/HarmonyThingGatherer.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/HarmonyThingGatherer.cs)) — singleton. Patches `Thing` lifecycle methods via Harmony:
- `Thing.SpawnSetup` (postfix) → pushes the Thing.
- `Thing.DoTick` (postfix) → tick-hash spreading: re-pushes a Thing when `(TicksGame + thingIDNumber) % TickHashInterval == 0` (default `TickHashInterval = 60`), distributing indexing load across ticks.
- `Thing.Destroy` / `Thing.DeSped` → `Destroyed`.
- `ThingOwner.NotifyAdded`/`NotifyRemoved`/`ExposeData` → push/destroy for container-held things.
- `ResearchManager.FinishProject` → increments static `ResearchTracker`, used by `TrackIsConstructionMaterial` to invalidate its buildable-def cache.

`Reset()` clears the static `_snapshotManager` so postfix patches become no-ops when the gatherer is torn down. Powers the [`Toolkit.Indexing.Thing`](overview.md) root table and `Thing.Resources`.

### MapThingGatherer

`MapThingGatherer` ([`Indexing/Components/MapThingGatherer.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/MapThingGatherer.cs)) — singleton. Scans `map.listerThings.AllThings`, `map.GetDirectlyHeldThings()`, `map.GetChildHolders()`. Registers a `MapLifecycleTrigger.Generated` hook to scan new maps; forces a snapshot after scanning to absorb the spike during map generation.

## Indexers

`IIndexer<T>` ([`Indexing/IIndexer.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/IIndexer.cs)) extends `IDatabaseListener<T>` and adds `Initialize()`. Indexers are database-level listeners that enrich metadata during `OnUpserting`.

### IIndexerBuilder and TrackedIndexer

`IIndexerBuilder<T>` ([`Indexing/IIndexerBuilder.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/IIndexerBuilder.cs)) — fluent builder: `When(condition)`, `Set<TValue>(key, func, watchForChanges)`, `Requires<TValue>(key, func)`, `Include<TValue>(key, watchForChanges)`.

`TrackedIndexer<T>` ([`Indexing/Models/TrackedIndexer.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Models/TrackedIndexer.cs)) implements `IIndexer<T>` + `IIndexerBuilder<T>` + `IChangeTracker<T>` + `IDisposable`. It maintains four collections:

- `_includes` (`HashSet<IndexMetadataKey>`) — keys whose incoming metadata values should be persisted to the indexed item.
- `_watchers` (`Dictionary<IndexMetadataKey, SetCondition>`) — conditions comparing current value vs previous metadata; make `WatchesChanges = true`.
- `_getters` (`Dictionary<IndexMetadataKey, SetGetValue>`) — non-watch value extractors run during `OnUpserting`.
- `_conditions` (`HashSet<SetCondition>`) — `When()` predicates; if any is false, getters don't run.

**`OnUpserting`** (called before tables process the item): for each `_includes`, `metadata.PersistKey(include)`. If no getters, return. Evaluate all `_conditions`; if any false, skip. Run each getter: `getter.Value(indexed.Value, indexed, ref metadata)` setting persistent metadata.

**`HasChanged`** (called by `TypedSnapshotManager<T>.Push` before updating): if no `_watchers`, return false. For each watcher: if `watcher.Value(current, indexed, ref metadata)` returns true, set `anyChanged`. If no watcher reported change but `_conditions` exist, evaluate them; any true sets `anyChanged`. Return `anyChanged`.

Watcher logic (for `Set` with `watchForChanges: true` and `Requires`): if `indexed is null` (new) → set metadata, return true. If previous metadata has the key and value differs → set, return true. If key absent but current value non-null → set, return true. Else false.

`Initialize()` subscribes to `Toolkit.Indexing.ConfigureManager` (registering itself as a change tracker) if `WatchesChanges`. `Dispose()` unsubscribes.

## Change trackers

`IChangeTracker<in T>` ([`Indexing/IChangeTracker.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/IChangeTracker.cs)) — `bool HasChanged(T current, IIndexed<T> indexed, ref IndexMetadata metadata)`. `IChangeTrackerCompileable<in T>` adds `GetCacheKey(...)` and `Compile(...)` so the tracker can be inlined into the [`TypedSnapshotManager<T>`](database-and-snapshots.md) compiled delegate.

`PropertyChangeTracker<T, TProperty>` ([`Indexing/Components/PropertyChangeTracker.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/PropertyChangeTracker.cs)) — gets the current property value via a compiled `_getProperty`, compares with the previous value in `IIndexed<T>.Metadata[_metadataKey.Name]`. Supports an optional `IComparer<TProperty>`. Always persists the key and sets the new value in metadata.

The [`Toolkit.Indexing.Indexers.ByProperty/ByNestedProperty/ByPath`](overview.md) helpers create `PropertyChangeTracker` instances and register them via `ConfigureManager`.

## TypedSnapshotManager compiled delegate

`SnapshotManager.TypedSnapshotManager<T>` ([`Indexing/Components/SnapshotManager.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/SnapshotManager.cs)) compiles all registered `IChangeTracker<T>` into a single OR'd LINQ expression delegate (`Changed`). If a tracker implements `IChangeTrackerCompileable<T>`, its expression is inlined; otherwise a virtual call is emitted. This means `HasChanged` for a type is a single compiled delegate, not N virtual calls.

## SnapshotOrchestrator

`SnapshotOrchestrator` ([`Indexing/Components/SnapshotOrchestrator.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/SnapshotOrchestrator.cs)) implements `ISnapshotOrchestrator` ([`Indexing/ISnapshotOrchestrator.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/ISnapshotOrchestrator.cs)).

`RebuildIndex(game, isGameStartup, snapshotManager, configure, configureManager, schemaBuilder)`:
1. Unregister previous `OnGameTickTrigger` and `MapLifecycleTrigger` hooks owned by the orchestrator.
2. Reset all existing gatherers; clear `_dataGatherers`.
3. `snapshotManager.Reset(configureManager, schemaBuilder)` — deploys schema, creates initial empty snapshot.
4. Invoke `configure(this)` — registers `IDataGatherer`s via `builder.With(gatherer)`.
5. For each gatherer: `Initialize(game)` (patches Harmony, registers hooks). A failing `Initialize` skips `GatherData` for that gatherer but continues others.
6. For each gatherer: `GatherData(game, snapshotManager)` — pushes initial data.
7. Register an `OnGameTickTrigger` hook.

On each tick: if pending snapshot not finished and is a snapshot window → force build. If pending finished → finalize via `SnapshotManager.Snapshot()`. If is a snapshot window (`Rare` or `Long` depending on `SlowGatheringEnabled`): `Snapshot(false)`; if not finished immediately, create cooperative work and trigger. `ForceSnapshot()` calls `Snapshot(true)`.

`Dispose()` unregisters hooks and resets gatherers.

## Focused tests

- `SnapshotOrchestratorTests` (unit): null-snapshot-manager guard; `RebuildIndex` calls `snapshotManager.Reset` once and registers exactly one tick hook; gatherers `Initialize`+`GatherData` called once each; failing gatherer's `GatherData` skipped, healthy gatherer's still called; second `RebuildIndex` resets previous gatherers; `Rare`/`Long` tick windows take snapshots; duplicate gatherer registered once; `Dispose` resets and unregisters.
- `HarmonyThingGathererTests` (unit): after `GatherData(null, snapshotManager)`, `Destroy_Postfix(pawn, Vanish)` calls `typedThingManager.Destroyed`; `Reset` clears the static manager so postfix does nothing; `SpawnSetup_Postfix` does not push when manager is null.
- `PropertyChangeTrackerTests` (unit): null guards; no prior value → changed; unchanged → false; value changed → true; null current throws; custom `IComparer` (`ThresholdComparer`) used for comparison.
- `TrackedIndexerTests` (unit): `WatchesChanges` false with no watchers, true after `Set(watch:true)`/`Requires`, false after `Set(watch:false)`; watch change detection (changed/unchanged/absent-key/no-incoming-value); `When` conditions don't affect watchers; `Initialize`/`Dispose` don't throw; fluent builder returns self.
- `TrackedIndexerUpsertFlowTests` (unit, real `Database`): `Set` enriches metadata in snapshot; multiple `Set`s all persisted; `When(true)`/`When(false)` gate enrichment; `When` reads upsert metadata; `Requires` sets on initial push and updates on changed value, skips on unchanged.
- `IndexerIntegrationTests` (integration): full pipeline upsert → `StartSnapshot().Build()` → find → `GetValue<int>("Number") == 42`; multiple sets; conditions gate; `Include` copies incoming metadata to indexed item.
