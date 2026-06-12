using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Moq;
using Verse;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Components
{
    public class SnapshotOrchestratorTests
    {
        private static readonly Action<ISnapshotManagerConfigurator> NoopManagerConfig = _ => { };
        private static readonly Action<IDatabaseSchemaBuilder> NoopSchemaConfig = _ => { };

        [Fact]
        public void RebuildIndex_WithNullSnapshotManager_ThrowsArgumentNullException()
        {
            // Arrange
            var hookManager = new Mock<IHookManager>();
            var game = CreateGame();
            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => sut.RebuildIndex(game, false, null, null, NoopManagerConfig, NoopSchemaConfig));
        }

        [Fact]
        public void RebuildIndex_WithValidInputs_ResetsSnapshotAndRegistersTickHook()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var registeredTickHooks = new List<IHook<OnGameTickTrigger>>();
            hookManager
                .Setup(h => h.RegisterHook<OnGameTickTrigger>(It.IsAny<IHook<OnGameTickTrigger>>()))
                .Callback<IHook<OnGameTickTrigger>>(h => registeredTickHooks.Add(h));

            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);

            // Act
            sut.RebuildIndex(game, false, snapshotManager.Object, null, NoopManagerConfig, NoopSchemaConfig);

            // Assert
            snapshotManager.Verify(s => s.Reset(It.IsAny<Action<ISnapshotManagerConfigurator>>(), It.IsAny<Action<IDatabaseSchemaBuilder>>()), Times.Once);
            hookManager.Verify(h => h.UnregisterAllBy<OnGameTickTrigger>(sut), Times.Once);
            Assert.Single(registeredTickHooks);
            Assert.Same(sut, registeredTickHooks[0].Owner);
        }

        [Fact]
        public void RebuildIndex_WithConfiguredGatherers_InitializesAndGathersData()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var gatherer = new Mock<IDataGatherer>();
            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);

            // Act
            sut.RebuildIndex(game, false, snapshotManager.Object, b => b.With(gatherer.Object), NoopManagerConfig, NoopSchemaConfig);

            // Assert
            gatherer.Verify(g => g.Initialize(game), Times.Once);
            gatherer.Verify(g => g.GatherData(game, snapshotManager.Object), Times.Once);
        }

        [Fact]
        public void RebuildIndex_WhenGathererInitializeThrows_SkipsGatherForFailingGatherer()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var failingGatherer = new Mock<IDataGatherer>();
            failingGatherer.Setup(g => g.Initialize(game)).Throws(new InvalidOperationException("init failed"));
            var healthyGatherer = new Mock<IDataGatherer>();
            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);

            // Act
            sut.RebuildIndex(game, false, snapshotManager.Object, b =>
            {
                b.With(failingGatherer.Object);
                b.With(healthyGatherer.Object);
            }, NoopManagerConfig, NoopSchemaConfig);

            // Assert
            failingGatherer.Verify(g => g.GatherData(It.IsAny<Game>(), It.IsAny<ISnapshotManager>()), Times.Never);
            healthyGatherer.Verify(g => g.GatherData(game, snapshotManager.Object), Times.Once);
        }

        [Fact]
        public void RebuildIndex_WhenCalledAgain_ResetsPreviouslyRegisteredGatherers()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var gatherer1 = new Mock<IDataGatherer>();
            var gatherer2 = new Mock<IDataGatherer>();
            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);

            // Act
            sut.RebuildIndex(game, false, snapshotManager.Object, b => b.With(gatherer1.Object), NoopManagerConfig, NoopSchemaConfig);
            sut.RebuildIndex(game, false, snapshotManager.Object, b => b.With(gatherer2.Object), NoopManagerConfig, NoopSchemaConfig);

            // Assert
            gatherer1.Verify(g => g.Reset(), Times.Once);
            gatherer2.Verify(g => g.Reset(), Times.Never);
            snapshotManager.Verify(s => s.Reset(It.IsAny<Action<ISnapshotManagerConfigurator>>(), It.IsAny<Action<IDatabaseSchemaBuilder>>()), Times.Exactly(2));
        }

        [Fact]
        public void RebuildIndex_WhenUseLongTicksFalse_RareTickSchedulesSnapshotAndNormalTickTakesSnapshot()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var registeredTickHooks = new List<IHook<OnGameTickTrigger>>();
            var preparingTriggers = new List<PreparingSnapshotTrigger>();

            hookManager
                .Setup(h => h.RegisterHook<OnGameTickTrigger>(It.IsAny<IHook<OnGameTickTrigger>>()))
                .Callback<IHook<OnGameTickTrigger>>(h => registeredTickHooks.Add(h));
            hookManager
                .Setup(h => h.LazyTrigger<PreparingSnapshotTrigger>(It.IsAny<Func<PreparingSnapshotTrigger>>()))
                .Callback<Func<PreparingSnapshotTrigger>>(factory => preparingTriggers.Add(factory()));

            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);
            sut.RebuildIndex(game, false, snapshotManager.Object, null, NoopManagerConfig, NoopSchemaConfig);

            var orchestrationHook = Assert.Single(registeredTickHooks);

            // Act - normal tick should not schedule anything
            orchestrationHook.OnTrigger(CreateOnGameTickTrigger(game, TickerType.Normal));

            // Assert
            Assert.Empty(preparingTriggers);
            Assert.Single(registeredTickHooks);

            // Act - rare tick should schedule snapshot and register one-time normal hook
            orchestrationHook.OnTrigger(CreateOnGameTickTrigger(game, TickerType.Rare));

            // Assert
            Assert.Single(preparingTriggers);
            Assert.Same(snapshotManager.Object, preparingTriggers[0].SnapshotManager);
            Assert.Equal(2, registeredTickHooks.Count);

            var snapshotHook = registeredTickHooks[1];
            Assert.True(snapshotHook.Once);
            Assert.Equal(byte.MaxValue, snapshotHook.Priority);

            // Act - non-normal tick should not snapshot
            var nonNormalResult = snapshotHook.OnTrigger(CreateOnGameTickTrigger(game, TickerType.Rare));

            // Assert
            Assert.False(nonNormalResult);
            snapshotManager.Verify(s => s.Snapshot(), Times.Never);

            // Act - normal tick should snapshot and unregister once hook
            var normalResult = snapshotHook.OnTrigger(CreateOnGameTickTrigger(game, TickerType.Normal));

            // Assert
            Assert.True(normalResult);
            snapshotManager.Verify(s => s.Snapshot(), Times.Once);
        }

        [Fact]
        public void RebuildIndex_WhenUseLongTicksTrue_LongTickSchedulesSnapshot()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var registeredTickHooks = new List<IHook<OnGameTickTrigger>>();
            var preparingTriggers = new List<PreparingSnapshotTrigger>();

            hookManager
                .Setup(h => h.RegisterHook<OnGameTickTrigger>(It.IsAny<IHook<OnGameTickTrigger>>()))
                .Callback<IHook<OnGameTickTrigger>>(h => registeredTickHooks.Add(h));
            hookManager
                .Setup(h => h.LazyTrigger<PreparingSnapshotTrigger>(It.IsAny<Func<PreparingSnapshotTrigger>>()))
                .Callback<Func<PreparingSnapshotTrigger>>(factory => preparingTriggers.Add(factory()));

            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: true);
            sut.RebuildIndex(game, false, snapshotManager.Object, null, NoopManagerConfig, NoopSchemaConfig);

            var orchestrationHook = Assert.Single(registeredTickHooks);

            // Act - rare tick should be ignored in long-tick mode
            orchestrationHook.OnTrigger(CreateOnGameTickTrigger(game, TickerType.Rare));

            // Assert
            Assert.Empty(preparingTriggers);
            Assert.Single(registeredTickHooks);

            // Act - long tick should schedule snapshot
            orchestrationHook.OnTrigger(CreateOnGameTickTrigger(game, TickerType.Long));

            // Assert
            Assert.Single(preparingTriggers);
            Assert.Equal(2, registeredTickHooks.Count);
        }

        [Fact]
        public void RebuildIndex_WithDuplicateGathererOnlyRegistersItOnce()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var gatherer = new Mock<IDataGatherer>();
            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);

            // Act
            sut.RebuildIndex(game, false, snapshotManager.Object, b =>
            {
                b.With(gatherer.Object);
                b.With(gatherer.Object);
            }, NoopManagerConfig, NoopSchemaConfig);

            // Assert
            gatherer.Verify(g => g.Initialize(game), Times.Once);
            gatherer.Verify(g => g.GatherData(game, snapshotManager.Object), Times.Once);
        }

        [Fact]
        public void Dispose_AfterRebuild_UnregistersHooksAndResetsGatherers()
        {
            // Arrange
            var game = CreateGame();
            var hookManager = new Mock<IHookManager>();
            var snapshotManager = new Mock<ISnapshotManager>();
            var gatherer = new Mock<IDataGatherer>();
            var sut = new SnapshotOrchestrator(hookManager.Object, useLongTicks: false);
            sut.RebuildIndex(game, false, snapshotManager.Object, b => b.With(gatherer.Object), NoopManagerConfig, NoopSchemaConfig);

            // Act
            sut.Dispose();

            // Assert
            gatherer.Verify(g => g.Reset(), Times.Once);
            hookManager.Verify(h => h.UnregisterAllBy<OnGameTickTrigger>(sut), Times.Exactly(2));
        }

        private static Game CreateGame()
        {
            return (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        }

        private static OnGameTickTrigger CreateOnGameTickTrigger(Game game, TickerType tickerType)
        {
            var ctor = typeof(OnGameTickTrigger).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Game), typeof(TickerType) },
                modifiers: null);

            Assert.NotNull(ctor);
            return (OnGameTickTrigger)ctor.Invoke(new object[] { game, tickerType });
        }
    }
}
