---
type: subsystem
title: Indexing Database and Snapshots
description: The mutable Database and Table internals, the immutable IReadOnlyDatabase snapshot model, the Indexed/IndexMetadata metadata system, the SnapshotManager buffering/dedup pipeline, and database/table listeners.
tags: [indexing, database, snapshot, immutable, metadata, listeners]
---

# Indexing Database and Snapshots

The storage layer of the [Indexing subsystem](overview.md). Namespace `HomebrewDot.Net.Rimworld.Indexing` (interfaces) and `.Indexing.Components`/`.Indexing.Models` (implementations).

## Threading model

- **`IDatabase` / `Database`** is **main-thread only**. Mutations (`Upsert`/`Update`/`Delete`) and gatherer pushes happen on the RimWorld main thread.
- **`IReadOnlyDatabase` / `ReadOnlyDatabaseSnapshot`** is the immutable snapshot. Once built, it is never mutated; new snapshots are new objects. Safe to read from background threads.
- Snapshot building is **cooperative** (spread across ticks via [`RaiseCooperativeWork`](../hooks/overview.md)), not truly parallel — but the published snapshot is immutable.

## Database and Table

`IDatabase` ([`Indexing/IDatabase.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/IDatabase.cs)) extends `IReadOnlyDatabase` with `Upsert<T>`/`Update<T>`/`Delete<T>`, `AsTyped<T>()` (returns an `IDatabase<T>` optimized for type `T`), `Deploy(schemaBuilder)`, and `StartSnapshot()`. `IDatabase<T>` is the typed wrapper avoiding generic dispatch overhead.

`Database` ([`Indexing/Components/Database.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/Database.cs)) implements `IDatabase` and `IDatabaseSchemaBuilder`. It holds:
- `List<Table> _tables`, `Dictionary<string, Table> _tablesByName` (full-name keyed).
- A typed DB cache (`AsTyped<T>()` returns a cached `TypedDatabase<T>`).
- Listeners (`HashSet<object>`), snapshot cache (`_cachedSnapshot`), intent logs.

Nested types: `TypedDatabase<T>` (the `IDatabase<T>` impl), `Table<T>` (the `IReadOnlyTable<T>` + `ITableBuilder<T>` impl), `TrackingIndexed<T>`, `SnapshotBuilder`, `ReadOnlyDatabaseSnapshot`, `SnapshotTable`, `DatabaseAction`/`TableAction` (pooled).

### Schema DSL

`IDatabaseSchemaBuilder` provides `TrackChanges()`, `WithTable<T>(name, builder, predicate)`, `OnInserting/OnInserted/OnDeleting/OnDeleted` (database-level, create `DelegateDatabaseListener<T>` wrappers), `WithListener<T>(IDatabaseListener<T>)`.

`ITableBuilder<T>` provides `WithSubTable<TSub>(name, filter, builder)`, `WithIndex<TProperty>(propertyName, selector, filter, name)`, `OnInserting/OnInserted/OnDeleting/OnDeleted` (table-level, create `DelegateTableListener<T>`), `TrackChanges()`, `WithListener(ITableListener<T>)`.

`Database.Deploy(schemaBuilder)` is a **full reset**: clears all tables, listeners, caches, intent logs; resets `Version = 0`; clears `_cachedSnapshot`; invokes `schemaBuilder(this)`. Re-deploying replaces all tables. `WithTable` rejects names containing `.` and duplicate names.

### Sub-tables

Two `WithSubTable` overloads: one for a more-derived type (`TSub : T`), one for a filtered sub-table of the same type. Items upserted into a parent table are automatically upserted into matching sub-tables. Sub-tables are recursively snapshotted.

`IReadOnlyDatabase.GetTable<T>(string name)` looks up by full name; throws `InvalidOperationException` if the table exists but is not `IReadOnlyTable<T>`.

## Upsert and delete flow

`TypedDatabase<T>.Update(data, existing, ref metadata)`:
1. If `existing` is not `ITrackingIndexed<T>`, create one via a compiled `GetCreatorForType` delegate.
2. Fire `IDatabaseListener<T>.OnUpserting` on all database-level listeners (this is where [`TrackedIndexer<T>.OnUpserting`](gatherers-and-indexers.md) enriches metadata).
3. For each table (cached for type T): `table.TryAddOrUpdate(trackedItem, metadata)`. If a table has a filter and the item fails it, the item is **deleted** from that table (not rejected). Fire table listeners. If changed, enqueue a `TableAction` (LogType.Upsert) in the intent log. Recursively upsert into sub-tables.
4. `metadata.PersistTo(trackedItem)` transfers persistent keys to the indexed item.
5. `metadata.Dispose()` returns pooled dictionaries.
6. If inserted or changed: enqueue `DatabaseAction`, set `HasChanges`, fire `IDatabaseListener<T>.OnUpserted`, `trackedItem.Commit()`.

`Delete` similarly fires `OnDeleting`, removes from `_data`/indexes, recursively deletes from sub-tables, fires `OnDeleted`.

## Indexed and IndexMetadata

`IIndexed<out T>` ([`Indexing/IIndexed.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/IIndexed.cs)) — `Value`, `Metadata` (`IReadOnlyDictionary<string,object>`), `HasSnapshot`, `IsSnapshot`, `Snapshot`, `GetValue<TValue>(propertyName)`. `IWriteableIndexed<out T>` adds `Set<TData>`/`Unset`.

`Indexed<T>` ([`Indexing/Models/Indexed.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Models/Indexed.cs)) — the default impl. Uses **compiled LINQ expression trees** for fast property/field access; static `_propertyAccessors` cache keyed by `(propertyName, typeof(TValue))`. `GetValue` checks **metadata first** (takes precedence), then falls back to the property/field on `Value`.

`Database.TrackingIndexed<T>` (internal) — combines `IIndexed<T>` + `IWriteableIndexed<T>` + tracking: `IndexedBy`, `Clone()`, `TakeSnapshot()`, `HasChanges`, `IsInsert`, `Commit()`. `Set()` transitions from frozen metadata to a mutable copy. `TakeSnapshot()` creates/copies a snapshot for diff tracking.

`IndexMetadata` (struct, [`Indexing/Models/IndexMetadata.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Models/IndexMetadata.cs)) — a mutable metadata bag using **type-specific pooled dictionaries** (`Dictionary<IndexMetadataKey, int>`/`bool`/`float`/`object`, rented from Unity's `UnityEngine.Pool`). `Set<T>(key, value, persistent)`, `TryGetValue<T>`, `ContainsKey<T>`, `Unset<T>`, `PersistKey`, `PersistTo(IWriteableIndexed<T>)`, `MergeInto(ref IndexMetadata)`, `Dispose()` (returns dicts to pools). Persistent keys are tracked in a `PooledHashSet<IndexMetadataKey>`; `PersistTo` transfers only persistent keys to the indexed item.

`IndexMetadataKey` / `IndexMetadataKey<T>` ([`Indexing/Models/IndexMetadataKey.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Models/IndexMetadataKey.cs)) — cached lookup keys via a static `Dictionary<string, IndexMetadataKey>` (case-insensitive). `Get(name)` returns/creates a singleton; `IndexMetadataKey<T>.Get(name)` validates the type matches the cached key.

## SnapshotManager

`ISnapshotManager` ([`Indexing/ISnapshotManager.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/ISnapshotManager.cs)) — `Database` (live, main-thread only), `DatabaseSnapshot` (read-only, background-safe), `Push<T>`/`Destroyed<T>`, `AsTyped<T>()`, `Snapshot(isForce)`, `Reset(configurator, schemaBuilder)`.

`SnapshotManager` ([`Indexing/Components/SnapshotManager.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/Components/SnapshotManager.cs)) holds `IDatabase _database`, `IHookManager`, `_databaseSnapshot`, change trackers, and a typed-manager cache. Nested `TypedSnapshotManager<T>`:
- Compiles all `IChangeTracker<T>` into a single OR'd LINQ expression delegate (`Changed`). If a tracker implements `IChangeTrackerCompileable<T>`, its expression is inlined; otherwise a virtual call is emitted. If no trackers are registered for type `T`, `Compile` returns `null`, and `HasChanged` returns `false` (all pushes treated as unchanged unless existing is null).
- `Push(T data, ref IndexMetadata metadata, bool allowBuffering)`: if queue not enabled or `allowBuffering == false`, find existing and push directly. If buffering, enqueue a pooled `PendingUpsert`; if the item is already pending, merge metadata via `metadata.MergeInto(ref pending.Metadata)`. Raise cooperative work via `_hookManager.Trigger(work)`.
- `Destroyed(T data, ref IndexMetadata metadata, bool allowBuffering)`: the symmetric delete path — queue-disabled (or `allowBuffering == false`) calls `_typedDb.Delete` directly; when the `_queueEnabled` flag is set, the destroyed item is buffered the same way as a push (push-then-destroy collapses to a delete, destroy-then-push collapses to an update — last call wins). When dedup hits an existing pending entry, `pending.IsDelete` is **flipped** to `true` (destroy after push → delete) or `false` (push after destroy → update), and the new metadata is merged via `metadata.MergeInto(ref pending.Metadata)`.
- `DoWork`: the cooperative-draining loop that runs the buffered `PendingUpsert` queue under a `RaiseCooperativeWork`. Sets `context.CheckInterval = 4`. Dequeues `_work` items; each item is processed in a try/finally that calls `_pending.Remove(data)` in the `finally` so the pending-dedup map is always cleaned up. Inside the try, `IsDelete` dispatches to `ActualDestroyed` (delete) or `Push(data, existing, ref metadata)` (upsert). Yields via `context.WaitForNextTick` between items. If the cooperative work hook is not accepted (manager rejects), the `Push`/`Destroyed` methods fall back to immediate synchronous execution.
- `Push(T data, IIndexed<T> existing, ref metadata)`: call `HasChanged` (compiled delegate). If unchanged and existing non-null: dispose metadata, return false. Else `_typedDb.Update(...)`.

### Buffering and dedup invariants

The buffered push/destroy queue is gated by the `_queueEnabled` flag on `SnapshotManager` (set true once the initial snapshot has finished building in `Reset`; cleared on `Reset` so early pushes go straight to the database). While enabled:

- Same item pushed twice while buffering merges metadata into one pending action.
- Push-then-destroy collapses to a single delete; destroy-then-push collapses to a single update (last call wins).
- Different items stay separate.
- `MaxPendingWork = 1024`; if exceeded, warns and forces a drain.
- `allowBuffering: false` bypasses the queue and processes immediately.
- `Drain()` (called by `Snapshot(isForce: true)`): creates a `CooperativeWorkContext`, calls `context.NoInterval()` (sets `CheckInterval = 0` so `WaitForNextTick` never yields), then runs `DoWork(context).ExecuteEnumerable()` — a **forced synchronous run-to-completion** that drains the entire `_work` queue in one tick.

## Snapshot building

`Database.StartSnapshot()` returns an `ISnapshotBuilder` whose `CreateWork()` returns a `RaiseCooperativeWork` (spread across ticks). Each `Table<T>.CreateSnapshot(PendingWorkContext)`:
- If no changes: reuse `_cachedSnapshot`.
- If changes: apply the intent log to the existing snapshot (incremental) or build new from `_data`.
- Swap intent logs (active becomes inactive; new active starts empty).

`SnapshotManager.Snapshot(isForce)`:
- If a pending snapshot is still building and `isForce` is false, returns the existing `ISnapshotBuilder` (no new build started).
- When `isForce` is true (or no pending build), it drains any buffered `PendingUpsert` queue first (so buffered pushes are flushed into the live database before the snapshot is taken), then calls `db.StartSnapshot()`. If finished immediately, finalize. Else create cooperative work and trigger via `IHookManager`.
- `Database.Version` increments only when `HasChanges` is true. Skips snapshot update if `snapshot.Version == _lastVersion`.
- Fires `OnSnapshotTakenTrigger` (forced via `Trigger`, normal via `TriggerDelayed`).

`Reset(configurator, schemaBuilder)`: clears the typed-manager cache and drains pending work, then `Database.Deploy(schemaBuilder)` (clears all tables/listeners/caches and applies the schema), then `DatabaseSnapshot = StartSnapshot().Build()` (initial empty snapshot). The first completed snapshot flips `_queueEnabled` to true, so gatherer pushes before that go straight to the database.

## Listeners

`IDatabaseListener<in T>` ([`Indexing/IDatabaseListener.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/IDatabaseListener.cs)) — `OnUpserting(IWriteableIndexed<T>, ref metadata, IDatabase)`, `OnUpserted(IIndexed<T>, ref metadata, IDatabase)`, `OnDeleting(...)`, `OnDeleted(...)`. `ITableListener<in T>` ([`Indexing/ITableListener.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Indexing/ITableListener.cs)) — same lifecycle but receives `IReadOnlyTable<T>`.

`DelegateDatabaseListener<T>` / `DelegateTableListener<T>` — wrappers exposing public `Action` fields (`onUpserting`, `onUpserted`, etc.) for the schema DSL convenience methods.

Notification order in `Update`: `OnUpserting` (database) → table processing (table listeners fire) → `OnUpserted` (database, only if inserted/changed). In `Delete`: `OnDeleting` → table processing → `OnDeleted`. All listener invocations are wrapped in try/catch with `LogError`; a failing listener does not abort the operation.

## Query and index

`IReadOnlyTable<T>` is `IEnumerable<IIndexed<T>>` and `IEnumerable<T>`. `Query<TSearch>(property, search, indexName)` uses a registered index (`WithIndex<TProperty>`). `IReadOnlyIndex<T, TSearch>.Query(TSearch)` returns `IEnumerable<T>`.

## Focused tests

- `DatabaseTests` (unit): fresh DB `HasChanges == false`; `Deploy` invokes builder with `this` and sets `IsDeploying`; `WithTable` registers; `Upsert` returns false with no table, true with a table and sets `HasChanges`; `Find` returns null for missing / correct `IIndexed<T>` for present; table-level `OnInserting` can enrich metadata; `Delete` removes; `Query` with index returns matches; filtered table rejects non-matching; name-with-separator throws; second deploy clears previous tables; `StartSnapshot().Build()` resets `HasChanges`; dedup (`Upsert` same item twice → one); `GetTable` wrong-type throws; database/table lifecycle callbacks fire in order.
- `SnapshotManagerTests` (unit, Moq): null-arg guards; constructor builds initial snapshot; `Push` with no trackers skips; tracker-reporting-changed calls `Update`; unchanged skips; `AsTyped` cached and reset returns new; `Destroyed` calls `Delete`; `Snapshot` starts builder; `Reset` calls `Deploy`+`StartSnapshot`+`Build`; registered trackers consulted; **buffering dedup** (same item twice → one `Update` with merged metadata; push-then-destroy → one delete; destroy-then-push → one update; different items → two updates; `allowBuffering:false` processes immediately).
- `IndexedTests` (unit): `GetValue` for primitives/fields; metadata conversion (string→bool, numeric→long, int→double, int→char, ISO→DateTime); null handling; case-insensitive; metadata **precedence** over property.
- `IndexMetadataTests` (unit): persistent `Set` transfers via `PersistTo`; non-persistent does not; mixed; type-specific dict routing; `TryGetValue` wrong type false; `Unset` removes persistent key and value; `Dispose` after `PersistTo` works.
- `IndexingConfigurationIntegrationTests` (integration): `ConfigureSchema` fires; `StartIndexing` with null game does not throw; force snapshot produces snapshot; `ReloadOrchestration`/`ReloadManager` reset.
