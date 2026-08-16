# Files

- [Indexing Database and Snapshots](database-and-snapshots.md) - The mutable Database and Table internals, the immutable IReadOnlyDatabase snapshot model, the Indexed/IndexMetadata metadata system, the SnapshotManager buffering/dedup pipeline, and database/table listeners.
- [Indexing Gatherers and Indexers](gatherers-and-indexers.md) - The data acquisition layer — DefGatherer, HarmonyThingGatherer, MapThingGatherer; TrackedIndexer and IIndexerBuilder enrichment; IChangeTracker/PropertyChangeTracker change detection; TypedSnapshotManager compiled tracker delegates; SnapshotOrchestrator lifecycle.
- [Indexing Subsystem Overview](overview.md) - The snapshot-based, incrementally-updated, thread-safe query index over live RimWorld game data — the Toolkit.Indexing facade, the snapshot lifecycle, and the table helper hierarchy.
