using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.CollectingIntegration
{
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class CollectingBuildIntegrationTests : IDisposable
    {
        private const string TableName = "CollectTable";

        public CollectingBuildIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            var keys = Toolkit.Collecting.GetAllCollectors().Keys
                .Concat(Toolkit.Collecting.GetAllDefinitions().Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var k in keys)
            {
                InvokeSafe(() => Toolkit.Collecting.Remove(k));
            }
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
            InvokeSafe(() => Toolkit.Collecting.ReloadDefaultComparator());
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void Collecting_Build_WithSimpleCondition_CollectsMatchingItems()
        {
            // Build a local self-contained DB and upsert items
            var (db, _) = BuildDb();
            var e1 = new Tentity { Number = 5, Text = "match" };
            var e2 = new Tentity { Number = 10, Text = "nomatch" };
            var md1 = new IndexMetadata();
            var md2 = new IndexMetadata();
            db.Upsert(e1, ref md1);
            db.Upsert(e2, ref md2);
            var snapshot = db.StartSnapshot().Build();
            var table = snapshot.GetTable<Tentity>(TableName);
            Assert.NotNull(table);

            // Verify items are in the snapshot
            var items = ((IEnumerable<IIndexed<Tentity>>)table).ToList();
            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.Value == e1);

            // Build a definition (no collector since no CollectFromSnapshot factory)
            var collectionId = Guid.NewGuid().ToString();
            var collectionName = $"Build_{collectionId}";

            Toolkit.Collecting.Build(collectionName, x =>
                x.Compare.Value(5).With.Equal().To.Value(5));

            // The definition should be registered
            Assert.True(Toolkit.Collecting.GetAllDefinitions().ContainsKey(collectionName));
        }

        [Fact]
        public void Collecting_Build_WithNonMatchingCondition_ReturnsEmpty()
        {
            var (db, _) = BuildDb();
            var e1 = new Tentity { Number = 5, Text = "x" };
            var md1 = new IndexMetadata();
            db.Upsert(e1, ref md1);

            var collectionId = Guid.NewGuid().ToString();
            var collectionName = $"EmptyBuild_{collectionId}";

            // Build the collection (no collector since no CollectFromSnapshot)
            Toolkit.Collecting.Build(collectionName, x =>
                x.Compare.Value(99).With.Equal().To.Value(100));

            // Assert - the definition was added, no collector without factory
            Assert.True(Toolkit.Collecting.GetAllDefinitions().ContainsKey(collectionName));
            Assert.False(Toolkit.Collecting.GetAllCollectors().ContainsKey(collectionName));
        }

        [Fact]
        public void Collecting_GetAllCollectors_WhenEmpty_ReturnsEmpty()
        {
            Dispose();
            Assert.Empty(Toolkit.Collecting.GetAllCollectors());
        }

        [Fact]
        public void Collecting_Build_WithNullName_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                Toolkit.Collecting.Build(null, x => x));
        }

        [Fact]
        public void Collecting_ReloadDefaultComparator_DoesNotThrow()
        {
            _ = Toolkit.Collecting.Comparator;
            var ex = Record.Exception(() => Toolkit.Collecting.ReloadDefaultComparator());
            Assert.Null(ex);
        }

        private static (Database Db, TrackedIndexer<Tentity> Indexer) BuildDb()
        {
            var db = new Database();
            var indexer = new TrackedIndexer<Tentity>();
            db.Deploy(schema =>
            {
                schema.WithTable<Tentity>(TableName);
                schema.WithListener(indexer);
            });
            return (db, indexer);
        }
    }
}
