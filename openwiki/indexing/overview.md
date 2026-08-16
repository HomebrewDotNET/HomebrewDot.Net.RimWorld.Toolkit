---
type: subsystem
title: Indexing Subsystem Overview
description: The snapshot-based, incrementally-updated, thread-safe query index over live RimWorld game data — the Toolkit.Indexing facade, the snapshot lifecycle, and the table helper hierarchy.
tags: [indexing, snapshots, facade, toolkit-indexing]
---

# Indexing Subsystem Overview

The Indexing subsystem (namespace `HomebrewDot.Net.Rimworld.Indexing`, components in `.Indexing.Components`, models in `.Indexing.Models`, triggers in `.Indexing.Triggers`) provides a snapshot-based, incrementally-updated, thread-safe query index over live RimWorld game objects (`Def`s and `Thing`s). Its mission: **gather** game data, **index** it into a typed table-organized database with metadata enrichment, **snapshot** the database into an immutable read-only view safe for background threads, **track changes** between snapshots, and **notify** consumers.

The storage internals (database, tables, snapshots, metadata) are documented in [Database & Snapshots](database-and-snapshots.md). The data acquisition layer (gatherers, indexers, change trackers, orchestration) is in [Gatherers & Indexers](gatherers-and-indexers.md).

## The Toolkit.Indexing facade

`Toolkit.Indexing` (nested static class in [`Toolkit.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Toolkit.cs)) is the primary integration surface. It lazily constructs a `SnapshotOrchestrator` and a `SnapshotManager(new Database(), Hooks.Manager)`.

### Configuration events (take effect on next `StartIndexing`)

| Event | Delegate | Purpose |
|-------|----------|---------|
| `ConfigureOrchestrator` | `Action<ISnapshotOrchestratorBuilder>` | Add gatherers via `builder.With(gatherer)` |
| `ConfigureManager` | `Action<ISnapshotManagerConfigurator>` | Add change trackers via `config.WithChangeTracker<T>(tracker)` |
| `ConfigureSchema` | `Action<IDatabaseSchemaBuilder>` | Define tables, listeners, indexes |

### Properties and methods

| Member | Behavior |
|--------|----------|
| `Orchestrator` (`ISnapshotOrchestrator`) | Lazy `SnapshotOrchestrator(Hooks.Manager, Settings.SlowGatheringEnabled)`. Setter disposes previous. |
| `Manager` (`ISnapshotManager`) | Lazy `SnapshotManager(new Database(), Hooks.Manager)`. Setter disposes previous. Exposes `DatabaseSnapshot` (the immutable `IReadOnlyDatabase`). |
| `StartIndexing(Game game, bool takeSnapshot = false)` | Calls `Orchestrator.RebuildIndex(...)` passing the three config events; if `takeSnapshot`, calls `ForceSnapshot()`. |
| `ReloadOrchestration()` | Disposes current orchestrator, nulls it, calls `StartIndexing(Current.Game)`. |
| `ReloadManager()` | Nulls current manager, calls `StartIndexing(Current.Game)`. |

The static constructor auto-registers an `OnSaveLoadedTrigger` hook (priority `byte.MaxValue`, lowest) that calls `StartIndexing(e.Game, true)`, and a `ToolkitSettings.Changed` hook that calls `StartIndexing(Current.Game)`.

## Snapshot lifecycle

```mermaid
sequenceDiagram
    participant Mod as Mod code
    participant TK as Toolkit.Indexing
    participant Orch as SnapshotOrchestrator
    participant Mgr as SnapshotManager
    participant DB as Database
    participant G as IDataGatherer

    Mod->>TK: ConfigureSchema += builder => ...
    Mod->>TK: StartIndexing(game, takeSnapshot)
    TK->>Orch: RebuildIndex(game, isStartup, Mgr, config, mgrConfig, schema)
    Orch->>Orch: unregister previous tick hook; reset gatherers
    Orch->>Mgr: Reset(mgrConfig, schemaBuilder)
    Mgr->>DB: Deploy(schema) — clears tables, registers schema listeners
    Mgr->>DB: StartSnapshot().Build() — initial empty snapshot
    Orch->>Orch: invoke configure(builder) — registers gatherers
    loop each gatherer
        Orch->>G: Initialize(game)
        Orch->>G: GatherData(game, Mgr) — pushes initial data
    end
    Orch->>Orch: register OnGameTickTrigger hook
    Note over Orch: each tick window (Rare/Long)
    Orch->>Mgr: Snapshot(isForce)
    Mgr->>DB: StartSnapshot() — cooperative work across ticks
    Mgr-->>TK: OnSnapshotTakenTrigger fires
    TK-->>Mod: DatabaseSnapshot updated, queryable
```

## Table helper hierarchy

`Toolkit.Indexing` exposes nested helper classes for the common RimWorld tables:

| Helper | `TableName` / `FullTableName` | Filters / sub-tables |
|--------|-------------------------------|----------------------|
| `Def` | `"Def"` (`Verse.Def`) | root; `EnsureTable()`/`EnsureGatherer()`/`GetTable()`/`ConfigureTable(builder)` |
| `Def.Thing` | `"Def.Thing"` (`ThingDef`) | sub-table of `Def`; built-in indexers below |
| `Def.Thing.Weapon` | `"Def.Thing.Weapon"` | filtered by `IsWeapon` |
| `Def.Thing.Weapon.Melee` | `"Def.Thing.Weapon.Melee"` | filtered by `IsMeleeWeapon` |
| `Def.Thing.Weapon.Ranged` | `"Def.Thing.Weapon.Ranged"` | filtered by `IsRangedWeapon` |
| `Def.Thing.Apparel` | `"Def.Thing.Apparel"` | filtered by `IsApparel` |
| `Thing` | `"Thing"` (`Verse.Thing`) | root; `TrackMap`, `TrackModId`, `TrackIsUnique`, `TrackHitPointPercentage` |
| `Thing.Resources` | filtered sub-table | resources |

Table names are hierarchical, joined by `.` (`Database.TableNameSeparator`). `WithTable` rejects names containing `.`. `GetTable<T>(name)` looks up by full name in `_tablesByName` and throws `InvalidOperationException` if the table exists but is not `IReadOnlyTable<T>`.

## Built-in indexers

`Def.Thing` provides ready-made indexers that enrich `ThingDef` metadata (keys live in [`ToolkitConstants.Def.Thing`](../facades/constants.md)):

| Method | Metadata key | Behavior |
|--------|-------------|----------|
| `TrackIsConstructionMaterial()` | `IsConstructionMaterial` (bool) | Marks defs used as build costs or stuff categories; cache keyed by `HarmonyThingGatherer.ResearchTracker`, invalidated on research completion. |
| `TrackIsFoul()` | `IsFoul` (bool) | Marks meat/leather from humanlike or non-normal-flesh creatures as foul; also indexes Things by their def. |
| `TrackIsDrink()` | `IsDrink` (bool) | Marks ingestible defs that are drinks. |
| `TrackIsAlcoholic()` | `IsAlcoholic` (bool) | Marks ingestible defs that are alcoholic. |
| `TrackIsMedical()` | `IsMedical` (bool) | Marks medical defs. |
| `TrackIsSurgical()` | `IsSurgical` (bool) | Marks surgical defs. |

`Thing` provides `TrackMap()`, `TrackModId()`, `TrackIsUnique()` (handles Make It Unique mod + Odyssey unique weapons), `TrackHitPointPercentage()`.

Each `TrackIs*` method calls `Toolkit.Indexing.Indexers.BuildIndexer<Verse.Def>(name, builder => ...)` with `When` conditions and `Set`/`Requires` clauses.

## Indexers registration API

`Toolkit.Indexing.Indexers` (nested static class) registers custom [`IIndexer<T>`](gatherers-and-indexers.md) instances:

- `RegisterIndexer<T>(string name, IIndexer<T> indexer, bool overwrite = false)` — `name` is for deduplication (the property being indexed). Overwrite replaces and unsubscribes the old `ConfigureSchema` handler; otherwise returns silently if a same-name indexer exists. Calls `indexer.Initialize()` and `StartIndexing(Current.Game)`.
- `BuildIndexer<T>(string name, Action<IIndexerBuilder<T>> builder, bool overwrite)` — creates a `TrackedIndexer<T>`, runs the builder, registers.
- `ByProperty<T, TProperty>(Expression<Func<T,TProperty>>)` — compiles the expression, creates a `PropertyChangeTracker<T,TProperty>`, registers via `ConfigureManager`.
- `ByNestedProperty<T, TProperty>(expr, metadataKey)` — for nested paths.
- `ByPath(Type type, string path)` — runtime reflection-based path; used by [`SnapshotCollector.Autodex`](../collecting/overview.md) to index properties referenced by collection conditions.

## Querying the snapshot

```csharp
var snapshot = Toolkit.Indexing.Manager.DatabaseSnapshot;
var table = snapshot?.GetTable<ThingDef>("Def.Thing.Weapon.Ranged");
foreach (var indexed in table)
{
    var isFoul = indexed.GetValue<bool>("IsFoul");
    var modId  = indexed.Metadata["ModId"];
}
var results = snapshot.Query<ThingDef, bool>("IsConstructionMaterial", true, "Def.Thing");
```

`IIndexed<T>.GetValue<TValue>(propertyName)` checks **metadata first** (takes precedence), then falls back to the property/field on `Value`. See [Database & Snapshots](database-and-snapshots.md) for the `IIndexed`/`Indexed<T>` compiled-accessor details.

## Snapshot-taken hook

```csharp
Toolkit.Hooks.Manager.RegisterHook<OnSnapshotTakenTrigger>(this, e =>
{
    var snapshot = e.Snapshot;   // IReadOnlyDatabase
    var isForced = e.IsForced;
});
```

`OnSnapshotTakenTrigger` ([`Triggers/OnSnapshotTakenTrigger.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Triggers/OnSnapshotTakenTrigger.cs)) carries `IReadOnlyDatabase Snapshot` and `bool IsForced`. Fired by `SnapshotManager.Snapshot()` via `IHookManager.Trigger` (forced) or `TriggerDelayed` (normal). Consumed by [`SnapshotCollector<T>`](../collecting/overview.md).
