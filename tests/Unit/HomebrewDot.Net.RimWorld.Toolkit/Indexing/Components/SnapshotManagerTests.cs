using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Components
{
    public class SnapshotManagerTests
    {
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<IDatabase<string>> _mockTypedDb;
        private readonly Mock<IReadOnlyDatabase> _mockSnapshot;
        private readonly Mock<IHookManager> _mockHookManager;
        private readonly Mock<ISnapshotBuilder> _mockSnapshotBuilder;

        public SnapshotManagerTests()
        {
            _mockDatabase = new Mock<IDatabase>();
            _mockTypedDb = new Mock<IDatabase<string>>();
            _mockSnapshot = new Mock<IReadOnlyDatabase>();
            _mockHookManager = new Mock<IHookManager>();
            _mockSnapshotBuilder = new Mock<ISnapshotBuilder>();

            _mockSnapshotBuilder.Setup(b => b.Build()).Returns(_mockSnapshot.Object);
            _mockDatabase.Setup(d => d.StartSnapshot()).Returns(_mockSnapshotBuilder.Object);
            _mockDatabase.Setup(d => d.AsTyped<string>()).Returns(_mockTypedDb.Object);
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
            var _md = default(IndexMetadata);
            Assert.Throws<ArgumentNullException>(() =>
                sut.Push<string>(null, ref _md));
        }

        [Fact]
        public void Push_Dict_WhenItemExistsAndNoTrackers_Skips()
        {
            var existing = new Mock<IIndexed<string>>();
            existing.Setup(e => e.Value).Returns("hello");
            _mockDatabase.Setup(d => d.Find("hello")).Returns(existing.Object);
            _mockTypedDb.Setup(d => d.Update("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny))
                        .Returns(true);

            var sut = CreateSut();

            var _md = default(IndexMetadata);
            var result = sut.Push("hello", ref _md);

            Assert.False(result);
            _mockTypedDb.Verify(d => d.Update("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny), Times.Never);
        }

        [Fact]
        public void Push_Dict_WhenTrackerReportsChanged_CallsUpdate()
        {
            var existing = new Mock<IIndexed<string>>();
            existing.Setup(e => e.Value).Returns("hello");
            var snapshottedExisting = new Mock<IIndexed<string>>();
            existing.Setup(x => x.Snapshot).Returns(snapshottedExisting.Object);

            _mockDatabase.Setup(d => d.Find("hello")).Returns(existing.Object);
            _mockSnapshot.Setup(s => s.Find("hello")).Returns(snapshottedExisting.Object);
            _mockTypedDb.Setup(d => d.Update("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny))
                        .Returns(true);

            var tracker = new Mock<IChangeTracker<string>>();
            tracker.Setup(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny)).Returns(true);

            var sut = CreateSut();
            sut.Reset(config => config.WithChangeTracker(tracker.Object), _ => { });

            var _md = default(IndexMetadata);
            sut.Push("hello", ref _md);

            _mockTypedDb.Verify(d => d.Update("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny), Times.Once);
        }

        [Fact]
        public void Push_Dict_WhenTrackerReportsUnchanged_DoesNotCallUpdateOrUpsert()
        {
            var existing = new Mock<IIndexed<string>>();
            existing.Setup(e => e.Value).Returns("hello");
            var snapshottedExisting = new Mock<IIndexed<string>>();

            // Setup: item exists in database
            _mockDatabase.Setup(d => d.Find("hello")).Returns(existing.Object);

            // Setup: snapshot has the item for old-data lookup
            _mockSnapshot.Setup(s => s.Find("hello")).Returns(snapshottedExisting.Object);

            var tracker = new Mock<IChangeTracker<string>>();
            tracker.Setup(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny)).Returns(false);

            var sut = CreateSut();
            sut.Reset(config => config.WithChangeTracker(tracker.Object), _ => { });

            var _md = default(IndexMetadata);
            var result = sut.Push("hello", ref _md);

            Assert.False(result);
            _mockTypedDb.Verify(d => d.Upsert(It.IsAny<string>(), ref It.Ref<IndexMetadata>.IsAny), Times.Never);
            _mockTypedDb.Verify(d => d.Update(It.IsAny<string>(), It.IsAny<IIndexed<string>>(), ref It.Ref<IndexMetadata>.IsAny), Times.Never);
        }

        [Fact]
        public void Push_Dict_WhenUpsertReturnsFalse_ReturnsFalse()
        {
            var sut = CreateSut();
            _mockDatabase.Setup(d => d.Find("hello"))
                         .Returns((IIndexed<string>)null);
            _mockTypedDb.Setup(d => d.Upsert("hello", ref It.Ref<IndexMetadata>.IsAny))
                        .Returns(false);

            var _md = default(IndexMetadata);
            var result = sut.Push("hello", ref _md);

            Assert.False(result);
        }

        [Fact]
        public void Push_Dict_WhenMetadataDiffers_Skips()
        {
            var existing = new Mock<IIndexed<string>>();
            existing.Setup(e => e.Value).Returns("hello");
            var existingMetadata = new Dictionary<string, object> { ["k1"] = "old" };
            existing.Setup(e => e.Metadata).Returns(existingMetadata);

            _mockDatabase.Setup(d => d.Find("hello")).Returns(existing.Object);
            _mockTypedDb.Setup(d => d.Update("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny))
                        .Returns(true);

            var sut = CreateSut();
            var metadata = default(IndexMetadata);

            var result = sut.Push("hello", ref metadata);

            Assert.False(result);
            _mockTypedDb.Verify(d => d.Update("hello", existing.Object, ref metadata), Times.Never);
        }

        // ── AsTyped<T> ─────────────────────────────────────────────────────

        [Fact]
        public void AsTyped_ReturnsCachedInstance()
        {
            var sut = CreateSut();
            var first = sut.AsTyped<string>();
            var second = sut.AsTyped<string>();
            Assert.Same(first, second);
        }

        [Fact]
        public void AsTyped_IsTypedManager_ImplementsCorrectInterface()
        {
            var sut = CreateSut();
            var typed = sut.AsTyped<string>();
            Assert.IsAssignableFrom<ISnapshotManager<string>>(typed);
            Assert.NotNull(typed);
        }

        [Fact]
        public void AsTyped_PushThroughTypedManager_UsesTypedDb()
        {
            var sut = CreateSut();
            _mockDatabase.Setup(d => d.Find("hello"))
                         .Returns((IIndexed<string>)null);
            _mockTypedDb.Setup(d => d.Update("hello", null, ref It.Ref<IndexMetadata>.IsAny))
                        .Returns(true);

            var typed = sut.AsTyped<string>();
            var _md = default(IndexMetadata);
            var result = typed.Push("hello", ref _md);

            Assert.True(result);
            _mockTypedDb.Verify(d => d.Update("hello", null, ref It.Ref<IndexMetadata>.IsAny), Times.Once);
        }

        [Fact]
        public void AsTyped_AfterReset_ReturnsNewInstance()
        {
            var sut = CreateSut();
            var before = sut.AsTyped<string>();
            sut.Reset(_ => { }, _ => { });
            var after = sut.AsTyped<string>();
            Assert.NotSame(before, after);
        }

        // ── Destroyed (overloads) ────────────────────────────────────────────

        [Fact]
        public void Destroyed_WithNullData_ThrowsArgumentNullException()
        {
            var sut = CreateSut();
            var _md = default(IndexMetadata);
            Assert.Throws<ArgumentNullException>(() => sut.Destroyed<string>(null, ref _md));
        }

        [Fact]
        public void Destroyed_CallsDatabaseDelete()
        {
            var sut = CreateSut();
            _mockTypedDb.Setup(d => d.Delete("hello", ref It.Ref<IndexMetadata>.IsAny))
                        .Returns(true);

            var _md = default(IndexMetadata);
            var result = sut.Destroyed("hello", ref _md);

            Assert.True(result);
            _mockTypedDb.Verify(d => d.Delete("hello", ref It.Ref<IndexMetadata>.IsAny), Times.Once);
        }

        [Fact]
        public void Destroyed_WithMetadata_PassesMetadataToDelete()
        {
            var sut = CreateSut();
            var metadata = new IndexMetadata();
            metadata.Set(IndexMetadataKey.Get("reason"), "despawned");
            _mockTypedDb.Setup(d => d.Delete("hello", ref metadata)).Returns(true);

            var result = sut.Destroyed("hello", ref metadata);

            Assert.True(result);
            _mockTypedDb.Verify(d => d.Delete("hello", ref metadata), Times.Once);
        }

        [Fact]
        public void Destroyed_WhenDeleteReturnsFalse_ReturnsFalse()
        {
            var sut = CreateSut();
            _mockTypedDb.Setup(d => d.Delete("hello", ref It.Ref<IndexMetadata>.IsAny))
                        .Returns(false);

            var _md = default(IndexMetadata);
            var result = sut.Destroyed("hello", ref _md);

            Assert.False(result);
        }

        // ── Snapshot ─────────────────────────────────────────────────────────

        [Fact]
        public void Snapshot_StartsNewBuilderWithDefaultMaxRuntime()
        {
            var sut = CreateSut();

            sut.Snapshot();

            _mockDatabase.Verify(d => d.StartSnapshot(), Times.Exactly(2));
            _mockSnapshotBuilder.Verify(b => b.Build(), Times.Once);
        }

        [Fact]
        public void Snapshot_ReturnsExistingBuilder_WhenPendingSnapshotNotFinished()
        {
            var mockSnapshot1 = new Mock<IReadOnlyDatabase>();
            var mockBuilder1 = new Mock<ISnapshotBuilder>();
            mockBuilder1.Setup(b => b.IsFinished).Returns(false);
            mockBuilder1.Setup(b => b.Build()).Returns(mockSnapshot1.Object);
            var mockBuilder2 = new Mock<ISnapshotBuilder>();
            mockBuilder2.Setup(b => b.IsFinished).Returns(false);

            _mockDatabase.SetupSequence(d => d.StartSnapshot())
                .Returns(mockBuilder1.Object)
                .Returns(mockBuilder2.Object);

            var sut = CreateSut();

            var first = sut.Snapshot();
            var second = sut.Snapshot();

            Assert.Same(first, second);
            _mockDatabase.Verify(d => d.StartSnapshot(), Times.Exactly(2));
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
        public void Reset_CallsDatabaseDeployAndStartSnapshot()
        {
            var sut = CreateSut();
            sut.Reset(null, _ => { });

            _mockDatabase.Verify(d => d.Deploy(It.IsAny<Action<IDatabaseSchemaBuilder>>()), Times.Once);
            _mockDatabase.Verify(d => d.StartSnapshot(), Times.Exactly(2)); // constructor + reset
            _mockSnapshotBuilder.Verify(b => b.Build(), Times.Exactly(2)); // constructor + reset
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
        public void RegisteredChangeTracker_IsConsultedWhenPushingExistingItem()
        {
            var existing = new Mock<IIndexed<string>>();
            existing.Setup(e => e.Value).Returns("hello");
            var snapshottedExisting = new Mock<IIndexed<string>>();
            existing.Setup(x => x.Snapshot).Returns(snapshottedExisting.Object);

            // Setup: item exists in database
            _mockDatabase.Setup(d => d.Find("hello")).Returns(existing.Object);

            // Setup: snapshot has the item for old-data lookup in Changed()
            _mockSnapshot.Setup(s => s.Find("hello")).Returns(snapshottedExisting.Object);

            var tracker = new Mock<IChangeTracker<string>>();
            tracker.Setup(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny)).Returns(false);

            var sut = CreateSut();
            sut.Reset(config => config.WithChangeTracker(tracker.Object), _ => { });

            var _md = default(IndexMetadata);
            sut.Push("hello", ref _md);

            tracker.Verify(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny), Times.Once);
        }

        [Fact]
        public void MultipleChangeTrackers_AllConsultedWhenPushingExistingItem()
        {
            var existing = new Mock<IIndexed<string>>();
            existing.Setup(e => e.Value).Returns("hello");
            var snapshottedExisting = new Mock<IIndexed<string>>();
            existing.Setup(x => x.Snapshot).Returns(snapshottedExisting.Object);

            _mockDatabase.Setup(d => d.Find("hello")).Returns(existing.Object);
            _mockSnapshot.Setup(s => s.Find("hello")).Returns(snapshottedExisting.Object);

            var tracker1 = new Mock<IChangeTracker<string>>();
            tracker1.Setup(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny)).Returns(false);

            var tracker2 = new Mock<IChangeTracker<string>>();
            tracker2.Setup(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny)).Returns(true);

            var sut = CreateSut();
            sut.Reset(config =>
                config.WithChangeTracker(tracker1.Object)
                      .WithChangeTracker(tracker2.Object), _ => { });

            var _md = default(IndexMetadata);
            sut.Push("hello", ref _md);

            tracker1.Verify(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny), Times.Once);
            tracker2.Verify(t => t.HasChanged("hello", existing.Object, ref It.Ref<IndexMetadata>.IsAny), Times.Once);
        }
    }
}
