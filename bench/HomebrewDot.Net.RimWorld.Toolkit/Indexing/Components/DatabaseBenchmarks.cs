using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Components;

namespace HomebrewDot.Net.RimWorld.Benchmarks.Indexing.Components
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class DatabaseBenchmarks
    {
        private DatabaseBenchmarkEntity[] _entities;
        private DatabaseBenchmarkEntity[] _findEntities;
        private DatabaseBenchmarkEntity[] _updateEntities;
        private DatabaseBenchmarkEntity[] _snapshotEntities;
        private string[] _queryNames;

        private Database _findDatabase;
        private Database _queryDatabase;
        private Database _updateDatabase;
        private Database _snapshotDatabase;
        private Database _cachedSnapshotDatabase;

        private int _findCursor;
        private int _queryCursor;
        private int _updateCursor;
        private int _snapshotCursor;

        [Params(100)]
        public int ItemCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _entities = Enumerable.Range(0, ItemCount)
                .Select(index => new DatabaseBenchmarkEntity($"entity-{index}", $"group-{index % 10}"))
                .ToArray();

            int middle = ItemCount / 2;

            _findDatabase = CreateIndexedEntityDatabase(_entities);
            _queryDatabase = CreateIndexedEntityDatabase(_entities);
            _updateDatabase = CreateIndexedEntityDatabase(_entities);
            _snapshotDatabase = CreateIndexedEntityDatabase(_entities);
            _cachedSnapshotDatabase = CreateIndexedEntityDatabase(_entities);

            _findEntities = CreateSample(_entities, 0);
            _queryNames = CreateSample(_entities, 1).Select(entity => entity.Name).ToArray();
            _updateEntities = CreateSample(_entities, 2);
            _snapshotEntities = CreateSample(_entities, 3);

            _cachedSnapshotDatabase.AsReadOnly();
        }

        [Benchmark]
        public int BulkUpsert_WithoutIndexes()
        {
            var database = CreateEntityDatabase();
            for (int i = 0; i < _entities.Length; i++)
            {
                database.Upsert(_entities[i], null);
            }

            return database.GetTable<DatabaseBenchmarkEntity>("Items").Count();
        }

        [Benchmark]
        public int BulkUpsert_WithIndexes()
        {
            var database = CreateIndexedEntityDatabase();
            for (int i = 0; i < _entities.Length; i++)
            {
                database.Upsert(_entities[i], null);
            }

            return database.GetTable<DatabaseBenchmarkEntity>("Items").Count();
        }

        [Benchmark]
        public IIndexed<DatabaseBenchmarkEntity> Find_ExistingItem()
        {
            return _findDatabase.Find(Next(_findEntities, ref _findCursor));
        }

        [Benchmark]
        public int Query_ByIndexedName()
        {
            return _queryDatabase.Query<DatabaseBenchmarkEntity, string>(nameof(DatabaseBenchmarkEntity.Name), Next(_queryNames, ref _queryCursor)).Count;
        }

        [Benchmark]
        public bool Upsert_ExistingIndexedItem()
        {
            return _updateDatabase.Upsert(Next(_updateEntities, ref _updateCursor), null);
        }

        [Benchmark]
        public IReadOnlyDatabase CreateSnapshot_AfterUpdate()
        {
            _snapshotDatabase.Upsert(Next(_snapshotEntities, ref _snapshotCursor), null);
            return _snapshotDatabase.AsReadOnly();
        }

        [Benchmark]
        public IReadOnlyDatabase ReuseCachedSnapshot_WithoutChanges()
        {
            return _cachedSnapshotDatabase.AsReadOnly();
        }

        private static Database CreateEntityDatabase()
        {
            var database = new Database();
            database.Deploy(schema =>
                schema.WithTable<DatabaseBenchmarkEntity>("Items", table => { }));
            return database;
        }

        private static Database CreateIndexedEntityDatabase()
        {
            var database = new Database();
            database.Deploy(schema =>
                schema.WithTable<DatabaseBenchmarkEntity>("Items", table =>
                {
                    table.WithIndex<string>(nameof(DatabaseBenchmarkEntity.Name), entity => entity.Value.Name);
                    table.WithIndex<string>(nameof(DatabaseBenchmarkEntity.Group), entity => entity.Value.Group);
                }));
            return database;
        }

        private static Database CreateIndexedEntityDatabase(IEnumerable<DatabaseBenchmarkEntity> entities)
        {
            var database = CreateIndexedEntityDatabase();
            foreach (var entity in entities)
            {
                database.Upsert(entity, null);
            }

            return database;
        }

        private static T[] CreateSample<T>(T[] source, int offset)
        {
            if (source.Length == 0)
            {
                return Array.Empty<T>();
            }

            var sampleCount = Math.Min(16, source.Length);
            var step = Math.Max(1, source.Length / sampleCount);
            var sample = new T[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                sample[i] = source[(offset + (i * step)) % source.Length];
            }
            return sample;
        }

        private static T Next<T>(T[] values, ref int cursor)
        {
            if (values == null || values.Length == 0)
            {
                return default(T);
            }

            if (cursor >= values.Length)
            {
                cursor = 0;
            }

            var value = values[cursor];
            cursor++;
            return value;
        }

        public sealed class DatabaseBenchmarkEntity
        {
            public DatabaseBenchmarkEntity(string name, string group)
            {
                Name = name;
                Group = group;
            }

            public string Name { get; }

            public string Group { get; }
        }
    }
}