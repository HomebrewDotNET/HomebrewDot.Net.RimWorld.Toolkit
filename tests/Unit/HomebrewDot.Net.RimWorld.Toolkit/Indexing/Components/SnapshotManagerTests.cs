using System;
using System.Collections.Generic;
using HomebrewDot.Net.RimWorld.Hooks;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Components;
using HomebrewDot.Net.RimWorld.Indexing.Triggers;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing.Components
{
    public class SnapshotManagerTests
    {
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<IReadOnlyDatabase> _mockSnapshot;
        private readonly Mock<IHookManager> _mockHookManager;

        public SnapshotManagerTests()
        {
            _mockDatabase = new Mock<IDatabase>();
            _mockSnapshot = new Mock<IReadOnlyDatabase>();
            _mockHookManager = new Mock<IHookManager>();
            _mockDatabase.Setup(d => d.AsReadOnly()).Returns(_mockSnapshot.Object);
        }

        private SnapshotManager CreateSut() =>
            new SnapshotManager(_mockDatabase.Object, _mockHookManager.Object);

        // ── Constructor ──────────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullDatabase_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SnapshotManager(null, _mockHookManager.Object));
        }

        [Fact]
        public void Constructor_WithNullHookManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SnapshotManager(_mockDatabase.Object, null));
        }

        [Fact]
        public void Constructor_WithValidArgs_InitializesDatabaseSnapshotFromDatabase()
        {
            var sut = CreateSut();
            Assert.Same(_mockSnapshot.Object, sut.DatabaseSnapshot);
        }

        // ── Push (IDictionary overload) ───────────────────────────────────────

        [Fact]
        public void Push_Dict_WithNullData_ThrowsArgumentNullException()
        {
            var sut = CreateSut();
            Assert.Throws<ArgumentNullException>(() =>
                sut.Push<string>(null, (IReadOnlyDictionary<string, object>)null));
        }

        [Fact]
        public void Push_Dict_WhenItemNotInSnapshot_CallsUpsert()
        {
            var sut = CreateSut();
            _mockSnapshot.Setup(s => s.Find<string>(It.IsAny<string>()))
                         .Returns((IIndexed<string>)null);
            _mockDatabase.Setup(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()))
                         .Returns(true);

            var result = sut.Push("hello", (IReadOnlyDictionary<string, object>)null);

            Assert.True(result);
            _mockDatabase.Verify(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public void Push_Dict_WhenItemInSnapshotAndNoTrackers_CallsUpsert()
        {
            var sut = CreateSut();
            var existing = new Mock<IIndexed<string>>();
            _mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(existing.Object);
            sut.Reset(_ => { }, _ => { }); // reset clears trackers

            sut.Push("hello", (IReadOnlyDictionary<string, object>)null);

            _mockDatabase.Verify(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public void Push_Dict_WhenTrackerReportsChanged_CallsUpsert()
        {
            var existing = new Mock<IIndexed<string>>();
            _mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(existing.Object);
            var tracker = new Mock<IChangeTracker<string>>();
            tracker.Setup(t => t.HasChanged("hello", existing.Object)).Returns(true);

            var sut = CreateSut();
            sut.Reset(config => config.WithChangeTracker(tracker.Object), _ => { });

            sut.Push("hello", (IReadOnlyDictionary<string, object>)null);

            _mockDatabase.Verify(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public void Push_Dict_WhenTrackerReportsUnchanged_DoesNotCallUpsert()
        {
            var existing = new Mock<IIndexed<string>>();
            _mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(existing.Object);
            var tracker = new Mock<IChangeTracker<string>>();
            tracker.Setup(t => t.HasChanged("hello", existing.Object)).Returns(false);

            var sut = CreateSut();
            sut.Reset(config => config.WithChangeTracker(tracker.Object), _ => { });

            var result = sut.Push("hello", (IReadOnlyDictionary<string, object>)null);

            Assert.False(result);
            _mockDatabase.Verify(d => d.Upsert(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public void Push_Dict_WhenUpsertReturnsFalse_ReturnsFalse()
        {
            var sut = CreateSut();
            _mockSnapshot.Setup(s => s.Find<string>(It.IsAny<string>()))
                         .Returns((IIndexed<string>)null);
            _mockDatabase.Setup(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()))
                         .Returns(false);

            var result = sut.Push("hello", (IReadOnlyDictionary<string, object>)null);

            Assert.False(result);
        }

        // ── Push (KeyValuePair[] overload) ───────────────────────────────────

        [Fact]
        public void Push_Kvp_WithNullData_ThrowsArgumentNullException()
        {
            var sut = CreateSut();
            Assert.Throws<ArgumentNullException>(() =>
                sut.Push<string>(null, new KeyValuePair<string, object>("k", "v")));
        }

        [Fact]
        public void Push_Kvp_WhenItemNotInSnapshot_CallsUpsertWithBuiltMetadata()
        {
            var sut = CreateSut();
            _mockSnapshot.Setup(s => s.Find<string>(It.IsAny<string>()))
                         .Returns((IIndexed<string>)null);
            IReadOnlyDictionary<string, object> captured = null;
            _mockDatabase
                .Setup(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Callback<string, IReadOnlyDictionary<string, object>>((_, meta) => captured = meta)
                .Returns(true);

            sut.Push("hello",
                new KeyValuePair<string, object>("k1", "v1"),
                new KeyValuePair<string, object>("k2", 42));

            _mockDatabase.Verify(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
            Assert.NotNull(captured);
            Assert.Equal("v1", captured["k1"]);
            Assert.Equal(42, captured["k2"]);
        }

        // ── Push (tuple overload) ────────────────────────────────────────────

        [Fact]
        public void Push_Tuple_WithNullData_ThrowsArgumentNullException()
        {
            var sut = CreateSut();
            Assert.Throws<ArgumentNullException>(() =>
                sut.Push<string>(null, ("k", (object)"v")));
        }

        [Fact]
        public void Push_Tuple_WhenItemNotInSnapshot_CallsUpsertWithBuiltMetadata()
        {
            var sut = CreateSut();
            _mockSnapshot.Setup(s => s.Find<string>(It.IsAny<string>()))
                         .Returns((IIndexed<string>)null);
            IReadOnlyDictionary<string, object> captured = null;
            _mockDatabase
                .Setup(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Callback<string, IReadOnlyDictionary<string, object>>((_, meta) => captured = meta)
                .Returns(true);

            sut.Push("hello", ("key", (object)"value"));

            _mockDatabase.Verify(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
            Assert.NotNull(captured);
            Assert.Equal("value", captured["key"]);
        }

        // ── Destroyed ────────────────────────────────────────────────────────

        [Fact]
        public void Destroyed_WithNullData_ThrowsArgumentNullException()
        {
            var sut = CreateSut();
            Assert.Throws<ArgumentNullException>(() => sut.Destroyed<string>(null));
        }

        [Fact]
        public void Destroyed_WhenItemFoundInDatabase_CallsDelete()
        {
            var sut = CreateSut();
            var indexed = new Mock<IIndexed<string>>();
            _mockDatabase.Setup(d => d.Find<string>("hello")).Returns(indexed.Object);
            _mockDatabase.Setup(d => d.Delete("hello", It.IsAny<IReadOnlyDictionary<string, object>>()))
                         .Returns(true);

            var result = sut.Destroyed("hello");

            Assert.True(result);
            _mockDatabase.Verify(d => d.Delete("hello", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public void Destroyed_WithMetadata_PassesMetadataToDelete()
        {
            var sut = CreateSut();
            var indexed = new Mock<IIndexed<string>>();
            var metadata = new Dictionary<string, object> { ["reason"] = "despawned" };
            _mockDatabase.Setup(d => d.Find<string>("hello")).Returns(indexed.Object);
            _mockDatabase.Setup(d => d.Delete("hello", metadata)).Returns(true);

            var result = sut.Destroyed("hello", metadata);

            Assert.True(result);
            _mockDatabase.Verify(d => d.Delete("hello", metadata), Times.Once);
        }

        [Fact]
        public void Destroyed_WhenDeleteReturnsFalse_ReturnsFalse()
        {
            var sut = CreateSut();
            _mockDatabase.Setup(d => d.Delete("hello", It.IsAny<IReadOnlyDictionary<string, object>>()))
                         .Returns(false);

            var result = sut.Destroyed("hello");

            Assert.False(result);
        }

        // ── Snapshot ─────────────────────────────────────────────────────────

        [Fact]
        public void Snapshot_UpdatesDatabaseSnapshotToLatestReadOnly()
        {
            var newSnapshot = new Mock<IReadOnlyDatabase>();
            _mockDatabase.SetupSequence(d => d.AsReadOnly())
                .Returns(_mockSnapshot.Object)   // constructor call
                .Returns(newSnapshot.Object);     // Snapshot() call

            var sut = CreateSut();
            sut.Snapshot();

            Assert.Same(newSnapshot.Object, sut.DatabaseSnapshot);
        }

        [Fact]
        public void Snapshot_FiresOnSnapshotTakenTriggerViaLazyTrigger()
        {
            var sut = CreateSut();
            OnSnapshotTakenTrigger firedTrigger = null;
            _mockHookManager
                .Setup(h => h.LazyTrigger(It.IsAny<Func<OnSnapshotTakenTrigger>>()))
                .Callback<Func<OnSnapshotTakenTrigger>>(f => firedTrigger = f());

            sut.Snapshot();

            _mockHookManager.Verify(h => h.LazyTrigger(It.IsAny<Func<OnSnapshotTakenTrigger>>()), Times.Once);
            Assert.NotNull(firedTrigger);
            Assert.Same(sut.DatabaseSnapshot, firedTrigger.Snapshot);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Fact]
        public void Reset_WithNullSchemaBuilder_ThrowsArgumentNullException()
        {
            var sut = CreateSut();
            Assert.Throws<ArgumentNullException>(() => sut.Reset(_ => { }, null));
        }

        [Fact]
        public void Reset_WithNullConfigurator_StillCallsDatabaseDeploy()
        {
            var sut = CreateSut();
            sut.Reset(null, _ => { });

            _mockDatabase.Verify(d => d.Deploy(It.IsAny<Action<IDatabaseSchemaBuilder>>()), Times.Once);
        }

        [Fact]
        public void Reset_WithValidSchemaBuilder_CallsDatabaseDeploy()
        {
            var sut = CreateSut();
            sut.Reset(null, _ => { });

            _mockDatabase.Verify(d => d.Deploy(It.IsAny<Action<IDatabaseSchemaBuilder>>()), Times.Once);
        }

        [Fact]
        public void Reset_WithConfigurator_InvokesConfigurator()
        {
            var sut = CreateSut();
            ISnapshotManagerConfigurator capturedConfig = null;
            sut.Reset(config => capturedConfig = config, _ => { });

            Assert.NotNull(capturedConfig);
        }

        [Fact]
        public void Reset_RegisteredChangeTracker_IsConsultedOnSubsequentPush()
        {
            var existing = new Mock<IIndexed<string>>();
            _mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(existing.Object);
            var tracker = new Mock<IChangeTracker<string>>();
            tracker.Setup(t => t.HasChanged(It.IsAny<string>(), It.IsAny<IIndexed<string>>())).Returns(false);

            var sut = CreateSut();
            sut.Reset(config => config.WithChangeTracker(tracker.Object), _ => { });

            sut.Push("hello", (IReadOnlyDictionary<string, object>)null);

            tracker.Verify(t => t.HasChanged("hello", existing.Object), Times.Once);
        }

        [Fact]
        public void Reset_MultipleChangeTrackers_AllConsultedWhenItemExistsInSnapshot()
        {
            var existing = new Mock<IIndexed<string>>();
            _mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(existing.Object);

            var tracker1 = new Mock<IChangeTracker<string>>();
            tracker1.Setup(t => t.HasChanged("hello", existing.Object)).Returns(false);

            var tracker2 = new Mock<IChangeTracker<string>>();
            tracker2.Setup(t => t.HasChanged("hello", existing.Object)).Returns(true);

            var sut = CreateSut();
            sut.Reset(config =>
                config.WithChangeTracker(tracker1.Object)
                      .WithChangeTracker(tracker2.Object), _ => { });

            sut.Push("hello", (IReadOnlyDictionary<string, object>)null);

            // tracker1 returns false so tracker2 is checked; tracker2 returns true so upsert happens
            tracker1.Verify(t => t.HasChanged("hello", existing.Object), Times.Once);
            tracker2.Verify(t => t.HasChanged("hello", existing.Object), Times.Once);
            _mockDatabase.Verify(d => d.Upsert("hello", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
        }
    }
}
