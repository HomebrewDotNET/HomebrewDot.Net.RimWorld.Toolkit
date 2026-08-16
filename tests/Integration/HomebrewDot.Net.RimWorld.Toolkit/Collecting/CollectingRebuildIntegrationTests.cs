using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.CollectingIntegration
{
    /// <summary>
    /// Integration tests for <see cref="Toolkit.Collecting.Rebuild"/>, covering collector reuse for compatible
    /// collectors, definition replacement, replacement for incompatible collectors and the no-factory path.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class CollectingRebuildIntegrationTests : IDisposable
    {
        private const string TableName = "RebuildTable";

        /// <summary>
        /// Initializes the toolkit services once per test.
        /// </summary>
        public CollectingRebuildIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        /// <inheritdoc/>
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

        private static Func<ICollectionBuilder, ICollectionBuilder> BuildSnapshotCollector<T>(int value) where T : class
        {
            return x => x
                .Compare.Value(value).With.Equal().To.Value(value)
                .CollectFromSnapshot<ICollectionBuilder, T>(
                    d => d.GetTable<T>(TableName),
                    d => d.GetTable<T>(TableName));
        }

        [Fact]
        public void Collecting_Rebuild_WithCompatibleCollector_ReusesSameInstance()
        {
            // Arrange
            var name = $"Rebuild_Reuse_{Guid.NewGuid()}";
            Toolkit.Collecting.Build(name, BuildSnapshotCollector<Tentity<bool>>(5));
            var originalCollector = Toolkit.Collecting.GetAllCollectors()[name];

            // Act
            var newDef = Toolkit.Collecting.Rebuild(name, BuildSnapshotCollector<Tentity<bool>>(5));

            // Assert - the compatible collector instance is reused and the new definition is registered
            Assert.Same(originalCollector, Toolkit.Collecting.GetAllCollectors()[name]);
            Assert.Same(newDef, Toolkit.Collecting.GetAllDefinitions()[name]);
        }

        [Fact]
        public void Collecting_Rebuild_WithCompatibleCollector_UpdatesDefinition()
        {
            // Arrange
            var name = $"Rebuild_UpdateDef_{Guid.NewGuid()}";
            var firstDef = Toolkit.Collecting.Build(name, BuildSnapshotCollector<Tentity<bool>>(5));

            // Act - rebuild with a different condition value
            var secondDef = Toolkit.Collecting.Rebuild(name, BuildSnapshotCollector<Tentity<bool>>(7));

            // Assert - a new definition replaces the old one and the reused collector picks it up
            Assert.NotSame(firstDef, secondDef);
            Assert.Same(secondDef, Toolkit.Collecting.GetAllDefinitions()[name]);
            Assert.Same(secondDef, Toolkit.Collecting.GetAllCollectors()[name].Definition);
        }

        [Fact]
        public void Collecting_Rebuild_WithDifferentItemType_ReplacesCollector()
        {
            // Arrange
            var name = $"Rebuild_Replace_{Guid.NewGuid()}";
            Toolkit.Collecting.Build(name, BuildSnapshotCollector<Tentity<bool>>(5));
            var originalCollector = Toolkit.Collecting.GetAllCollectors()[name];

            // Act - rebuild with a different collected item type (bool -> float)
            var newDef = Toolkit.Collecting.Rebuild(name, BuildSnapshotCollector<Tentity<float>>(5));

            // Assert - the incompatible collector is replaced with a new instance
            Assert.NotSame(originalCollector, Toolkit.Collecting.GetAllCollectors()[name]);
            Assert.Same(newDef, Toolkit.Collecting.GetAllDefinitions()[name]);
        }

        [Fact]
        public void Collecting_Rebuild_WithoutCollectorFactory_OnlyReplacesDefinition()
        {
            // Arrange
            var name = $"Rebuild_NoFactory_{Guid.NewGuid()}";
            Toolkit.Collecting.Build(name, BuildSnapshotCollector<Tentity<bool>>(5));
            var originalCollector = Toolkit.Collecting.GetAllCollectors()[name];

            // Act - rebuild without a CollectFromSnapshot factory
            var newDef = Toolkit.Collecting.Rebuild(name, x => x.Compare.Value(5).With.Equal().To.Value(5));

            // Assert - only the definition is replaced; no new collector is created
            Assert.Same(newDef, Toolkit.Collecting.GetAllDefinitions()[name]);
            Assert.True(Toolkit.Collecting.GetAllCollectors().ContainsKey(name));
            Assert.Same(originalCollector, Toolkit.Collecting.GetAllCollectors()[name]);
        }

        [Fact]
        public void Collecting_Rebuild_WithNullName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                Toolkit.Collecting.Rebuild(null, x => x));
        }
    }
}
