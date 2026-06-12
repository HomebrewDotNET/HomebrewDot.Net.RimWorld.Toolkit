using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Components
{
    [Trait("Category", "Integration")]
    public class PropertyChangeTrackerIntegrationTests : IDisposable
    {
        private sealed class TestEntity
        {
            public string Name { get; set; }
            public int Score { get; set; }
        }

        private readonly Action<IDatabaseSchemaBuilder> _schemaConfigHandler;

        static PropertyChangeTrackerIntegrationTests()
        {
            // Initialize Toolkit.Instance using FormatterServices to bypass Mod constructor requirements
            var dummyToolkit = (Toolkit)FormatterServices.GetUninitializedObject(typeof(Toolkit));
            var instanceField = typeof(Toolkit).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            instanceField.SetValue(null, dummyToolkit);

            // Initialize Toolkit._settings to bypass GetSettings() which requires RimWorld runtime
            var dummySettings = new ToolkitSettings();
            var settingsField = typeof(Toolkit).GetField("_settings", BindingFlags.Static | BindingFlags.NonPublic);
            settingsField.SetValue(null, dummySettings);

            // Also configure services so that operators and reference types are registered
            Toolkit.ConfigureServices();
        }

        public PropertyChangeTrackerIntegrationTests()
        {
            // Register the TestEntities table in the schema
            _schemaConfigHandler = builder => builder.WithTable<TestEntity>("TestEntities");
            Toolkit.Indexing.ConfigureSchema += _schemaConfigHandler;
        }

        public void Dispose()
        {
            Toolkit.Indexing.ConfigureSchema -= _schemaConfigHandler;
            Toolkit.Indexing.ReloadManager();
            Toolkit.Indexing.ReloadOrchestration();
        }

        [Fact]
        public void PropertyChangeTracker_EndToEnd_CorrectlyPopulatesMetadataOnPush()
        {
            // Arrange
            // 1. Create the property change tracker
            using (var tracker = new PropertyChangeTracker<TestEntity, string>(e => e.Name, "Name"))
            {
                // 2. Start indexing to deploy the schema and register the tracker's OnInserting callback
                Toolkit.Indexing.StartIndexing(null);

                var entity = new TestEntity { Name = "Alice", Score = 100 };

                // Act
                // 3. Push the entity to the snapshot manager
                Toolkit.Indexing.Manager.Push(entity);

                // 4. Take a snapshot to commit the changes to the DatabaseSnapshot
                Toolkit.Indexing.Manager.Snapshot();

                // Assert
                // 5. Verify that the entity was indexed and the metadata was populated by the tracker
                var snapshot = Toolkit.Indexing.Manager.DatabaseSnapshot;
                Assert.NotNull(snapshot);

                var table = snapshot.GetTable<TestEntity>("TestEntities");
                Assert.NotNull(table);

                Assert.True(table.TryFind(entity, out var indexed));
                Assert.NotNull(indexed);
                Assert.Equal("Alice", indexed.Metadata["Name"]);
            }
        }

        [Fact]
        public void PropertyChangeTracker_HasChanged_DetectsChangesCorrectly()
        {
            using (var tracker = new PropertyChangeTracker<TestEntity, string>(e => e.Name, "Name"))
            {
                Toolkit.Indexing.StartIndexing(null);

                var entity = new TestEntity { Name = "Alice", Score = 100 };
                Toolkit.Indexing.Manager.Push(entity);
                Toolkit.Indexing.Manager.Snapshot();

                var snapshot = Toolkit.Indexing.Manager.DatabaseSnapshot;
                var table = snapshot.GetTable<TestEntity>("TestEntities");
                table.TryFind(entity, out var indexed);

                // 1. Same value -> HasChanged should be false
                Assert.False(tracker.HasChanged(entity, indexed, null));

                // 2. Different value -> HasChanged should be true
                var modifiedEntity = new TestEntity { Name = "Bob", Score = 100 };
                Assert.True(tracker.HasChanged(modifiedEntity, indexed, null));
            }
        }
    }
}
