using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing
{
    [Trait("Category", "Integration")]
    public class IndexerIntegrationTests
    {
        private const string TableName = "EntityTable";

        private static readonly IndexMetadataKey<int> NumberKey = IndexMetadataKey<int>.Get("Number");
        private static readonly IndexMetadataKey<string> TextKey = IndexMetadataKey<string>.Get("Text");
        private static readonly IndexMetadataKey<bool> ConditionalKey = IndexMetadataKey<bool>.Get("Conditional");

        public IndexerIntegrationTests()
        {
            ConfigureServices();
        }

        /// <summary>
        /// Creates a self-contained database with a table and indexer listener,
        /// completely isolated from the global <c>ConfigureSchema</c> event system.
        /// </summary>
        private static (Database Db, TrackedIndexer<Tentity> Indexer) SetupDatabase(
            Action<IIndexerBuilder<Tentity>> configureIndexer)
        {
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            configureIndexer(indexer);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            return (db, indexer);
        }

        private static IIndexed<Tentity> FindInSnapshot(IReadOnlyDatabase snapshot, Tentity entity)
        {
            var table = snapshot.GetTable<Tentity>(TableName);
            Assert.NotNull(table);
            var indexed = ((IEnumerable<IIndexed<Tentity>>)table).FirstOrDefault(i => i.Value == entity);
            Assert.NotNull(indexed);
            return indexed;
        }

        // ── Full pipeline: indexer → upsert → snapshot → read ─────────────

        [Fact]
        public void IndexerWithSet_PushesEntity_ProducesEnrichedSnapshot()
        {
            var (db, _) = SetupDatabase(x =>
                x.Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false));

            var entity = new Tentity { Number = 42, Text = "hello" };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            var snapshot = db.StartSnapshot().Build();
            var indexed = FindInSnapshot(snapshot, entity);
            Assert.Equal(42, indexed.GetValue<int>(NumberKey.Name));
        }

        [Fact]
        public void IndexerWithMultipleSets_PushesEntity_AllValuesInSnapshot()
        {
            var (db, _) = SetupDatabase(x =>
            {
                x.Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false);
                x.Set(TextKey, (Tentity t) => t.Text, watchForChanges: false);
            });

            var entity = new Tentity { Number = 7, Text = "world" };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            var snapshot = db.StartSnapshot().Build();
            var indexed = FindInSnapshot(snapshot, entity);
            Assert.Equal(7, indexed.GetValue<int>(NumberKey.Name));
            Assert.Equal("world", indexed.GetValue<string>(TextKey.Name));
        }

        // ── When conditions ────────────────────────────────────────────────

        [Fact]
        public void IndexerWithWhenTrue_PushesEntity_MetadataSet()
        {
            var (db, _) = SetupDatabase(x =>
                x.When((Tentity current, IIndexed<Tentity> indexed, ref IndexMetadata metadata) => true)
                 .Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false));

            var entity = new Tentity { Number = 99 };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            var snapshot = db.StartSnapshot().Build();
            var indexed = FindInSnapshot(snapshot, entity);
            Assert.Equal(99, indexed.GetValue<int>(NumberKey.Name));
        }

        [Fact]
        public void IndexerWithWhenFalse_PushesEntity_MetadataNotSet()
        {
            var (db, _) = SetupDatabase(x =>
                x.When((Tentity current, IIndexed<Tentity> indexed, ref IndexMetadata metadata) => false)
                 .Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false));

            var entity = new Tentity { Number = 99 };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            var snapshot = db.StartSnapshot().Build();
            var indexed = FindInSnapshot(snapshot, entity);
            Assert.False(indexed.Metadata.ContainsKey(NumberKey.Name));
        }

        [Fact]
        public void IndexerWithWhenUsesUpsertMetadata_CanConditionallyEnrich()
        {
            var (db, _) = SetupDatabase(x =>
                x.When((Tentity current, IIndexed<Tentity> indexed, ref IndexMetadata metadata) =>
                        metadata.TryGetValue(ConditionalKey, out bool should) && should)
                 .Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false));

            // Entity 1: condition passes
            var entity1 = new Tentity { Number = 10 };
            var md1 = new IndexMetadata();
            md1.Set(ConditionalKey, true);
            db.Upsert(entity1, ref md1);

            // Entity 2: condition fails (key missing → TryGetValue returns false)
            var entity2 = new Tentity { Number = 20 };
            var md2 = new IndexMetadata();
            db.Upsert(entity2, ref md2);

            var snapshot = db.StartSnapshot().Build();

            var indexed1 = FindInSnapshot(snapshot, entity1);
            Assert.Equal(10, indexed1.GetValue<int>(NumberKey.Name));

            var indexed2 = FindInSnapshot(snapshot, entity2);
            Assert.False(indexed2.Metadata.ContainsKey(NumberKey.Name));
        }

        // ── Requires (PropertyChangeTracker) ───────────────────────────────

        [Fact]
        public void IndexerWithRequires_InitialPush_SetsMetadata()
        {
            var (db, _) = SetupDatabase(x =>
                x.Requires(NumberKey, (Tentity t) => t.Number));

            var entity = new Tentity { Number = 55 };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            var snapshot = db.StartSnapshot().Build();
            var indexed = FindInSnapshot(snapshot, entity);
            Assert.Equal(55, indexed.GetValue<int>(NumberKey.Name));
        }

        [Fact]
        public void IndexerWithRequires_PropertyChanged_UpdatesMetadataOnSubsequentPush()
        {
            var (db, _) = SetupDatabase(x =>
                x.Requires(NumberKey, (Tentity t) => t.Number));

            var entity = new Tentity { Number = 10 };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            // Change the entity and push again
            entity.Number = 20;
            var md2 = new IndexMetadata();
            db.Upsert(entity, ref md2);

            var snapshot = db.StartSnapshot().Build();
            var indexed = FindInSnapshot(snapshot, entity);
            Assert.Equal(20, indexed.GetValue<int>(NumberKey.Name));
        }

        // ── Include (copies existing metadata) ────────────────────────────

        [Fact]
        public void IndexerWithInclude_CopiesExistingMetadataToIndexedItem()
        {
            var (db, _) = SetupDatabase(x =>
                x.Include<int>(NumberKey, watchForChanges: false));

            // Push with the key already set in upsert metadata
            var entity = new Tentity { Number = 0 }; // entity value doesn't matter for Include
            var md = new IndexMetadata();
            md.Set(NumberKey, 123, persistent: true);
            db.Upsert(entity, ref md);

            var snapshot = db.StartSnapshot().Build();
            var indexed = FindInSnapshot(snapshot, entity);
            Assert.Equal(123, indexed.GetValue<int>(NumberKey.Name));
        }
    }
}
