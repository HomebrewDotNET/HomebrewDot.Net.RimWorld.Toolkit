using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Collecting.Components
{
    /// <summary>
    /// Tests for <see cref="Toolkit.Collecting.Rebuild"/>, which rebuilds a collection in place by reusing an
    /// existing compatible collector instance instead of constructing, registering and hooking a new one.
    /// </summary>
    public class CollectingRebuildTests
    {
        public CollectingRebuildTests()
        {
            ClearCollections();
        }

        [Fact]
        public void Rebuild_WithExistingCompatibleCollector_ReusesSameInstance()
        {
            var snapshotManager = new Mock<ISnapshotManager>();
            var hookManager = new Mock<IHookManager>();

            Toolkit.Collecting.Build("rebuild_reuse", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));
            var first = Toolkit.Collecting.GetAllCollectors()["rebuild_reuse"];

            Toolkit.Collecting.Rebuild("rebuild_reuse", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));

            var second = Toolkit.Collecting.GetAllCollectors()["rebuild_reuse"];
            Assert.Same(first, second);
        }

        [Fact]
        public void Rebuild_WithExistingCompatibleCollector_ReplacesDefinition()
        {
            var snapshotManager = new Mock<ISnapshotManager>();
            var hookManager = new Mock<IHookManager>();

            Toolkit.Collecting.Build("rebuild_def", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));
            var firstDefinition = Toolkit.Collecting.GetAllDefinitions()["rebuild_def"];

            Toolkit.Collecting.Rebuild("rebuild_def", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));

            var secondDefinition = Toolkit.Collecting.GetAllDefinitions()["rebuild_def"];
            var collector = Toolkit.Collecting.GetAllCollectors()["rebuild_def"];
            Assert.NotSame(firstDefinition, secondDefinition);
            Assert.Same(secondDefinition, collector.Definition);
        }

        [Fact]
        public void Rebuild_WithExistingCollectorOfDifferentItemType_CreatesNewInstance()
        {
            var snapshotManager = new Mock<ISnapshotManager>();
            var hookManager = new Mock<IHookManager>();

            Toolkit.Collecting.Build("rebuild_type", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));
            var first = Toolkit.Collecting.GetAllCollectors()["rebuild_type"];

            Toolkit.Collecting.Rebuild("rebuild_type", b => b.CollectWith<object>(def => new SnapshotCollector<object>(def, snapshotManager.Object, hookManager.Object, s => s, s => Array.Empty<IIndexed<object>>())));

            var second = Toolkit.Collecting.GetAllCollectors()["rebuild_type"];
            Assert.NotSame(first, second);
        }

        [Fact]
        public void Rebuild_WithoutExistingCollector_AddsCollectorAndDefinition()
        {
            var snapshotManager = new Mock<ISnapshotManager>();
            var hookManager = new Mock<IHookManager>();

            Toolkit.Collecting.Rebuild("rebuild_new", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));

            Assert.True(Toolkit.Collecting.GetAllDefinitions().ContainsKey("rebuild_new"));
            Assert.True(Toolkit.Collecting.GetAllCollectors().ContainsKey("rebuild_new"));
        }

        [Fact]
        public void Rebuild_WithoutCollectorFactory_AddsDefinitionOnly()
        {
            Toolkit.Collecting.Rebuild("rebuild_defonly", b => b);

            Assert.True(Toolkit.Collecting.GetAllDefinitions().ContainsKey("rebuild_defonly"));
            Assert.False(Toolkit.Collecting.GetAllCollectors().ContainsKey("rebuild_defonly"));
        }

        [Fact]
        public void Rebuild_WithExistingCompatibleCollector_UnregistersStaleHook()
        {
            var snapshotManager = new Mock<ISnapshotManager>();
            var hookManager = new Mock<IHookManager>();

            Toolkit.Collecting.Build("rebuild_hook", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));

            Toolkit.Collecting.Rebuild("rebuild_hook", b => b.CollectWith<string>(def => CreateCollector(def, snapshotManager, hookManager)));

            // The reused collector is stopped (unregistering its old hook) and restarted, so the stale hook of
            // the previous registration must not be left behind.
            hookManager.Verify(m => m.UnregisterHook(It.IsAny<IHook<OnSnapshotTakenTrigger>>()), Times.AtLeastOnce());
        }

        private static SnapshotCollector<string> CreateCollector(ICollectionDef definition, Mock<ISnapshotManager> snapshotManager, Mock<IHookManager> hookManager)
            => new SnapshotCollector<string>(definition, snapshotManager.Object, hookManager.Object, s => s, s => Array.Empty<IIndexed<string>>());

        private static void ClearCollections()
        {
            var allKeys = Toolkit.Collecting.GetAllDefinitions().Keys
                .Concat(Toolkit.Collecting.GetAllCollectors().Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (var i = 0; i < allKeys.Length; i++)
            {
                Toolkit.Collecting.Remove(allKeys[i]);
            }
        }
    }
}
