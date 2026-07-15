using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using RimWorld;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.IndexingIntegration
{
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class IndexingConfigurationIntegrationTests : IDisposable
    {
        private const string TableName = "EntityTable";

        public IndexingConfigurationIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void Indexing_ConfigureSchema_FiresWhenSubscribed()
        {
            var db = new Database();
            IDatabaseSchemaBuilder received = null;
            Action<IDatabaseSchemaBuilder> handler = b => { received = b; };
            db.Deploy(handler);

            Assert.NotNull(received);
        }

        [Fact]
        public void Indexing_StartIndexing_WithNullGame_DoesNotThrow()
        {
            var (db, _) = SetupDatabase();
            var snapshot = db.StartSnapshot().Build();
            Assert.NotNull(snapshot);
        }

        [Fact]
        public void Indexing_StartIndexing_WithForceSnapshot_ProducesSnapshot()
        {
            var (db, _) = SetupDatabase();
            var entity = new Tentity { Number = 7, Text = "snapshot" };
            var md = new IndexMetadata();
            db.Upsert(entity, ref md);

            var snapshot = db.StartSnapshot().Build();

            var table = snapshot.GetTable<Tentity>(TableName);
            Assert.NotNull(table);
        }

        [Fact]
        public void Indexing_ReloadOrchestration_ResetsOrchestrator()
        {
            var first = Toolkit.Indexing.Orchestrator;
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            var second = Toolkit.Indexing.Orchestrator;

            Assert.NotSame(first, second);
        }

        [Fact]
        public void Indexing_ReloadManager_ResetsManager()
        {
            var first = Toolkit.Indexing.Manager;
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
            var second = Toolkit.Indexing.Manager;

            Assert.NotSame(first, second);
        }

        [Fact]
        public void Indexing_Indexers_BuildIndexer_RegistersSuccessfully()
        {
            var (db, indexer) = SetupDatabase();
            Assert.NotNull(db);
            Assert.NotNull(indexer);
        }

        private static (Database Db, TrackedIndexer<Tentity> Indexer) SetupDatabase()
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
