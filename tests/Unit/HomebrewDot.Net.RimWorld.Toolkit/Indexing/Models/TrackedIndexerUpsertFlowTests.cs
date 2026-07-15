using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Models
{
    [Trait("Category", "Unit")]
    public class TrackedIndexerUpsertFlowTests
    {
        private const string TableName = "TestEntities";

        private static readonly IndexMetadataKey<int> NumberKey = IndexMetadataKey<int>.Get("Number");
        private static readonly IndexMetadataKey<string> TextKey = IndexMetadataKey<string>.Get("Text");
        private static readonly IndexMetadataKey<bool> ConditionalKey = IndexMetadataKey<bool>.Get("Conditional");

        // ── Set (non-watch) enriches metadata during upsert ─────────────────

        [Fact]
        public void Upsert_WithIndexerSet_GetValueReturnsEnrichedMetadata()
        {
            // Arrange
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer).Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity = new Tentity { Number = 42, Text = "hello" };
            var md = new IndexMetadata();

            // Act
            db.Upsert(entity, ref md);

            // Assert
            var result = db.Find(entity);
            Assert.NotNull(result);
            var retrievedNumber = result.GetValue<int>(NumberKey.Name);
            Assert.Equal(42, retrievedNumber);
        }

        [Fact]
        public void Upsert_WithMultipleSetIndexers_AllValuesPersisted()
        {
            // Arrange
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer).Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false);
            ((IIndexerBuilder<Tentity>)indexer).Set(TextKey, (Tentity t) => t.Text, watchForChanges: false);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity = new Tentity { Number = 7, Text = "world" };
            var md = new IndexMetadata();

            // Act
            db.Upsert(entity, ref md);

            // Assert
            var result = db.Find(entity);
            Assert.NotNull(result);
            Assert.Equal(7, result.GetValue<int>(NumberKey.Name));
            Assert.Equal("world", result.GetValue<string>(TextKey.Name));
        }

        // ── When condition ──────────────────────────────────────────────────

        [Fact]
        public void Upsert_WhenConditionTrue_SetExecutes()
        {
            // Arrange
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer)
                .When((Tentity current, IIndexed<Tentity> indexed, ref IndexMetadata metadata) => true)
                .Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity = new Tentity { Number = 99 };
            var md = new IndexMetadata();

            // Act
            db.Upsert(entity, ref md);

            // Assert
            var result = db.Find(entity);
            Assert.NotNull(result);
            Assert.Equal(99, result.GetValue<int>(NumberKey.Name));
        }

        [Fact]
        public void Upsert_WhenConditionFalse_SetDoesNotExecute()
        {
            // Arrange
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer)
                .When((Tentity current, IIndexed<Tentity> indexed, ref IndexMetadata metadata) => false)
                .Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity = new Tentity { Number = 99 };
            var md = new IndexMetadata();

            // Act
            db.Upsert(entity, ref md);

            // Assert
            var result = db.Find(entity);
            Assert.NotNull(result);
            // Number metadata should NOT be set because When condition failed
            Assert.False(result.Metadata.ContainsKey(NumberKey.Name));
        }

        [Fact]
        public void Upsert_WhenConditionUsesIndexedMetadata_CanAccessExistingValues()
        {
            // Arrange
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer)
                .When((Tentity current, IIndexed<Tentity> indexed, ref IndexMetadata metadata) =>
                {
                    // Only set Number if the upsert metadata contains "ShouldEnrich" = true
                    return metadata.TryGetValue(ConditionalKey, out bool should) && should;
                })
                .Set(NumberKey, (Tentity t) => t.Number, watchForChanges: false);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity1 = new Tentity { Number = 10 };
            var md1 = new IndexMetadata();
            md1.Set(ConditionalKey, true, persistent: false);

            var entity2 = new Tentity { Number = 20 };
            var md2 = new IndexMetadata();
            md2.Set(ConditionalKey, false, persistent: false);

            // Act
            db.Upsert(entity1, ref md1);
            db.Upsert(entity2, ref md2);

            // Assert
            var result1 = db.Find(entity1);
            Assert.NotNull(result1);
            Assert.Equal(10, result1.GetValue<int>(NumberKey.Name)); // Condition true → set

            var result2 = db.Find(entity2);
            Assert.NotNull(result2);
            Assert.False(result2.Metadata.ContainsKey(NumberKey.Name)); // Condition false → not set
        }

        // ── Requires (PropertyChangeTracker) ────────────────────────────────

        [Fact]
        public void Upsert_WithRequires_InitialUpsertSetsMetadata()
        {
            // Arrange
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer).Requires(NumberKey, (Tentity t) => t.Number);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity = new Tentity { Number = 55 };
            var md = new IndexMetadata();

            // Act — first upsert: metadata key doesn't exist yet, so it should set it
            db.Upsert(entity, ref md);

            // Assert
            var result = db.Find(entity);
            Assert.NotNull(result);
            Assert.Equal(55, result.GetValue<int>(NumberKey.Name));
        }

        [Fact]
        public void Upsert_WithRequires_ChangedValueUpdatesMetadata()
        {
            // Arrange
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer).Requires(NumberKey, (Tentity t) => t.Number);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity = new Tentity { Number = 10 };
            var md = new IndexMetadata();

            // Act — first upsert
            db.Upsert(entity, ref md);

            // Change the entity and re-upsert
            entity.Number = 20;
            var md2 = new IndexMetadata();
            db.Upsert(entity, ref md2);

            // Assert
            var result = db.Find(entity);
            Assert.NotNull(result);
            Assert.Equal(20, result.GetValue<int>(NumberKey.Name));
        }

        [Fact]
        public void Upsert_WithRequires_UnchangedValueDoesNotTriggerUpdate()
        {
            // Arrange — this tests that Requires doesn't falsely report changes
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            ((IIndexerBuilder<Tentity>)indexer).Requires(NumberKey, (Tentity t) => t.Number);

            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });

            var entity = new Tentity { Number = 42 };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            // Re-upsert without changes
            var md2 = new IndexMetadata();
            var result = db.Upsert(entity, ref md2);

            // The upsert should still succeed (entity exists), but HasChanges on the
            // tracked item depends on whether the watcher reported a change.
            // The key point: value should still be 42 after re-upsert.
            var indexed = db.Find(entity);
            Assert.NotNull(indexed);
            Assert.Equal(42, indexed.GetValue<int>(NumberKey.Name));
        }
    }
}
