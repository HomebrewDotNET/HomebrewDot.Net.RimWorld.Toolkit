using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
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

        /// <summary>
        /// Finishes the initial snapshot so the pending work queue is enabled and
        /// cooperative work is accepted, letting pushes/destroys be buffered.
        /// </summary>
        private void EnableBuffering(SnapshotManager sut)
        {
            _mockSnapshotBuilder.Setup(b => b.IsFinished).Returns(true);
            _mockSnapshotBuilder.Setup(b => b.Snapshot).Returns(_mockSnapshot.Object);
            _mockHookManager.Setup(h => h.Trigger(It.IsAny<RaiseCooperativeWork>())).Returns(true);
            sut.Snapshot();
        }

        /// <summary>
        /// Points the snapshot manager's typed database at a <see cref="RecordingTypedDatabase"/>
        /// so tests can observe what reaches the database when the pending queue is drained.
        /// </summary>
        private RecordingTypedDatabase UseRecordingTypedDatabase()
        {
            var recordingDb = new RecordingTypedDatabase();
            _mockDatabase.Setup(d => d.AsTyped<string>()).Returns(recordingDb);
            return recordingDb;
        }

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
            _mockTypedDb.Setup(d => d.Find("hello")).Returns(existing.Object);
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

            _mockTypedDb.Setup(d => d.Find("hello")).Returns(existing.Object);
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
            _mockTypedDb.Setup(d => d.Find("hello")).Returns(existing.Object);

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

            _mockTypedDb.Setup(d => d.Find("hello")).Returns(existing.Object);
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
            _mockTypedDb.Setup(d => d.Find("hello")).Returns(existing.Object);

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

            _mockTypedDb.Setup(d => d.Find("hello")).Returns(existing.Object);
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

        // ── Pending buffering dedup ───────────────────────────────────────────

        [Fact]
        public void Push_SameItemTwiceWhileBuffering_MergesMetadataIntoSinglePendingAction()
        {
            var sut = CreateSut();
            EnableBuffering(sut);
            var recordingDb = UseRecordingTypedDatabase();

            var key1 = IndexMetadataKey.Get("PendingMergeKey1");
            var key2 = IndexMetadataKey.Get("PendingMergeKey2");
            var md1 = new IndexMetadata();
            md1.Set(key1, "value1");
            var md2 = new IndexMetadata();
            md2.Set(key2, "value2");

            var first = sut.Push("hello", ref md1);
            var second = sut.Push("hello", ref md2);

            Assert.True(first);
            Assert.True(second);
            // Both calls collapsed into one pending action — nothing hit the database yet
            Assert.Equal(0, recordingDb.UpdateCalls);

            sut.Snapshot(true); // force drain of the pending queue

            // Only a single update reached the database and it carried the merged metadata
            Assert.Equal(1, recordingDb.UpdateCalls);
            Assert.True(recordingDb.LastUpdateMetadata.TryGetValue<string>(key1, out var v1));
            Assert.Equal("value1", v1);
            Assert.True(recordingDb.LastUpdateMetadata.TryGetValue<string>(key2, out var v2));
            Assert.Equal("value2", v2);
        }

        [Fact]
        public void Push_ThenDestroyed_WhileBuffering_MergesMetadataIntoSinglePendingAction()
        {
            var sut = CreateSut();
            EnableBuffering(sut);
            var recordingDb = UseRecordingTypedDatabase();

            var key1 = IndexMetadataKey.Get("PendingMergeKey3");
            var key2 = IndexMetadataKey.Get("PendingMergeKey4");
            var md1 = new IndexMetadata();
            md1.Set(key1, "pushValue");
            var md2 = new IndexMetadata();
            md2.Set(key2, "destroyValue");

            var push = sut.Push("hello", ref md1);
            var destroy = sut.Destroyed("hello", ref md2);

            Assert.True(push);
            Assert.True(destroy);
            Assert.Equal(0, recordingDb.UpdateCalls);
            Assert.Equal(0, recordingDb.DeleteCalls);

            sut.Snapshot(true);

            // Last call wins: the pending action became a delete, so it drains as a single delete with merged metadata
            Assert.Equal(1, recordingDb.DeleteCalls);
            Assert.Equal(0, recordingDb.UpdateCalls);
            Assert.True(recordingDb.LastDeleteMetadata.TryGetValue<string>(key1, out var v1));
            Assert.Equal("pushValue", v1);
            Assert.True(recordingDb.LastDeleteMetadata.TryGetValue<string>(key2, out var v2));
            Assert.Equal("destroyValue", v2);
        }

        [Fact]
        public void Destroyed_ThenPush_WhileBuffering_MergesMetadataIntoSinglePendingAction()
        {
            var sut = CreateSut();
            EnableBuffering(sut);
            var recordingDb = UseRecordingTypedDatabase();

            var key1 = IndexMetadataKey.Get("PendingMergeKey5");
            var key2 = IndexMetadataKey.Get("PendingMergeKey6");
            var md1 = new IndexMetadata();
            md1.Set(key1, "destroyValue");
            var md2 = new IndexMetadata();
            md2.Set(key2, "pushValue");

            var destroy = sut.Destroyed("hello", ref md1);
            var push = sut.Push("hello", ref md2);

            Assert.True(destroy);
            Assert.True(push);
            Assert.Equal(0, recordingDb.UpdateCalls);
            Assert.Equal(0, recordingDb.DeleteCalls);

            sut.Snapshot(true);

            // Last call wins: the pending action became an upsert, so it drains as a single update with merged metadata
            Assert.Equal(1, recordingDb.UpdateCalls);
            Assert.Equal(0, recordingDb.DeleteCalls);
            Assert.True(recordingDb.LastUpdateMetadata.TryGetValue<string>(key1, out var v1));
            Assert.Equal("destroyValue", v1);
            Assert.True(recordingDb.LastUpdateMetadata.TryGetValue<string>(key2, out var v2));
            Assert.Equal("pushValue", v2);
        }

        [Fact]
        public void Destroyed_SameItemTwiceWhileBuffering_MergesMetadataIntoSinglePendingAction()
        {
            var sut = CreateSut();
            EnableBuffering(sut);
            var recordingDb = UseRecordingTypedDatabase();

            var key1 = IndexMetadataKey.Get("PendingMergeKey7");
            var key2 = IndexMetadataKey.Get("PendingMergeKey8");
            var md1 = new IndexMetadata();
            md1.Set(key1, "value1");
            var md2 = new IndexMetadata();
            md2.Set(key2, "value2");

            var first = sut.Destroyed("hello", ref md1);
            var second = sut.Destroyed("hello", ref md2);

            Assert.True(first);
            Assert.True(second);
            Assert.Equal(0, recordingDb.UpdateCalls);
            Assert.Equal(0, recordingDb.DeleteCalls);

            sut.Snapshot(true);

            Assert.Equal(1, recordingDb.DeleteCalls);
            Assert.Equal(0, recordingDb.UpdateCalls);
            Assert.True(recordingDb.LastDeleteMetadata.TryGetValue<string>(key1, out var v1));
            Assert.Equal("value1", v1);
            Assert.True(recordingDb.LastDeleteMetadata.TryGetValue<string>(key2, out var v2));
            Assert.Equal("value2", v2);
        }

        [Fact]
        public void Push_DifferentItemsWhileBuffering_KeepsSeparatePendingActions()
        {
            var sut = CreateSut();
            EnableBuffering(sut);
            var recordingDb = UseRecordingTypedDatabase();

            var _md = default(IndexMetadata);
            var first = sut.Push("alpha", ref _md);
            var second = sut.Push("beta", ref _md);

            Assert.True(first);
            Assert.True(second);
            Assert.Equal(0, recordingDb.UpdateCalls);

            sut.Snapshot(true);

            Assert.Equal(2, recordingDb.UpdateCalls);
            Assert.Contains("alpha", recordingDb.UpdatedItems);
            Assert.Contains("beta", recordingDb.UpdatedItems);
        }

        [Fact]
        public void Push_WithAllowBufferingFalse_ProcessesImmediatelyWithoutBuffering()
        {
            var sut = CreateSut();
            EnableBuffering(sut);
            var recordingDb = UseRecordingTypedDatabase();

            var _md = default(IndexMetadata);
            var result = sut.Push("hello", ref _md, allowBuffering: false);

            Assert.True(result);
            Assert.Equal(1, recordingDb.UpdateCalls);
        }

        /// <summary>
        /// Lightweight fake typed database that records the calls and metadata it receives,
        /// used to observe what reaches the database when the pending queue is drained.
        /// </summary>
        private sealed class RecordingTypedDatabase : IDatabase<string>
        {
            public int UpdateCalls { get; private set; }
            public int DeleteCalls { get; private set; }
            public IndexMetadata LastUpdateMetadata { get; private set; }
            public IndexMetadata LastDeleteMetadata { get; private set; }
            public List<string> UpdatedItems { get; } = new List<string>();

            public IIndexed<string> Find(string data) => null;
            public IEnumerable<IIndexed<string>> Find(IEnumerable<string> data) => Array.Empty<IIndexed<string>>();
            public IEnumerable<IIndexed<string>> Find(IReadOnlyList<string> data) => Array.Empty<IIndexed<string>>();
            public bool Upsert(string item, ref IndexMetadata metadata) => Update(item, null, ref metadata);
            public bool Update(string item, IIndexed<string> existing, ref IndexMetadata metadata)
            {
                UpdateCalls++;
                UpdatedItems.Add(item);
                LastUpdateMetadata = metadata;
                return true;
            }
            public bool Delete(string item, ref IndexMetadata metadata)
            {
                DeleteCalls++;
                LastDeleteMetadata = metadata;
                return true;
            }
        }
    }
}
