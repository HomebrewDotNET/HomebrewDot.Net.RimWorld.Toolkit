using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Models
{
    [Trait("Category", "Unit")]
    public class TrackedIndexerTests
    {
        #region Test helpers

        public class TestItem
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public float Score { get; set; }
        }

        private static readonly IndexMetadataKey<string> NameKey = IndexMetadataKey<string>.Get("Name");
        private static readonly IndexMetadataKey<int> AgeKey = IndexMetadataKey<int>.Get("Age");
        private static readonly IndexMetadataKey<float> ScoreKey = IndexMetadataKey<float>.Get("Score");
        private static readonly IndexMetadataKey<string> IncludeKey = IndexMetadataKey<string>.Get("IncludeValue");

        private static Mock<IIndexed<T>> MockIndexed<T>(T value, IReadOnlyDictionary<string, object> metadata = null) where T : class
        {
            var mock = new Mock<IIndexed<T>>();
            mock.Setup(m => m.Value).Returns(value);
            mock.Setup(m => m.Metadata).Returns(metadata ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            return mock;
        }

        #endregion

        // ── WatchesChanges ──────────────────────────────────────────────────

        [Fact]
        public void WatchesChanges_WithNoWatchers_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            Assert.False(sut.WatchesChanges);
        }

        [Fact]
        public void WatchesChanges_AfterAddingSetWithWatch_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            ((IIndexerBuilder<TestItem>)sut).Set(NameKey, (TestItem x) => x.Name, watchForChanges: true);
            Assert.True(sut.WatchesChanges);
        }

        [Fact]
        public void WatchesChanges_AfterAddingRequires_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            ((IIndexerBuilder<TestItem>)sut).Requires(NameKey, (TestItem x) => x.Name);
            Assert.True(sut.WatchesChanges);
        }

        [Fact]
        public void WatchesChanges_AfterAddingSetWithoutWatch_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            ((IIndexerBuilder<TestItem>)sut).Set(NameKey, (TestItem x) => x.Name, watchForChanges: false);
            Assert.False(sut.WatchesChanges);
        }

        // ── Set with watch (watcher return values) ──────────────────────────

        [Fact]
        public void Set_WithWatch_WhenValueChanged_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 31 };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Age"] = 30 });
            ((IIndexerBuilder<TestItem>)sut).Set(AgeKey, (TestItem x) => x.Age, watchForChanges: true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        [Fact]
        public void Set_WithWatch_WhenValueUnchanged_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 30 };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Age"] = 30 });
            ((IIndexerBuilder<TestItem>)sut).Set(AgeKey, (TestItem x) => x.Age, watchForChanges: true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.False(result);
        }

        [Fact]
        public void Set_WithWatch_WhenKeyNotInPreviousAndValueNotNull_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 30 };
            var indexed = MockIndexed(item);
            ((IIndexerBuilder<TestItem>)sut).Set(AgeKey, (TestItem x) => x.Age, watchForChanges: true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        // ── Set metadata-aware with watch (watcher return values) ───────────

        [Fact]
        public void Set_MetadataAwareWithWatch_WhenValueChanged_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 31 };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Age"] = 30 });
            ((IIndexerBuilder<TestItem>)sut).Set(AgeKey, (TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => current.Age, watchForChanges: true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        [Fact]
        public void Set_MetadataAwareWithWatch_WhenValueUnchanged_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 30 };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Age"] = 30 });
            ((IIndexerBuilder<TestItem>)sut).Set(AgeKey, (TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => current.Age, watchForChanges: true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.False(result);
        }

        // ── Requires (watcher return values) ────────────────────────────────

        [Fact]
        public void Requires_WhenValueChanged_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Bob" };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Alice" });
            ((IIndexerBuilder<TestItem>)sut).Requires(NameKey, (TestItem x) => x.Name);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        [Fact]
        public void Requires_WhenValueUnchanged_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Alice" };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Alice" });
            ((IIndexerBuilder<TestItem>)sut).Requires(NameKey, (TestItem x) => x.Name);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.False(result);
        }

        [Fact]
        public void Requires_WhenKeyNotInPreviousAndValueNotNull_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Charlie" };
            var indexed = MockIndexed(item);
            ((IIndexerBuilder<TestItem>)sut).Requires(NameKey, (TestItem x) => x.Name);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        [Fact]
        public void Requires_MetadataAware_WhenValueChanged_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Bob" };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Alice" });
            ((IIndexerBuilder<TestItem>)sut).Requires(NameKey, (TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => current.Name);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        // ── HasChanged basic ────────────────────────────────────────────────

        [Fact]
        public void HasChanged_WithNoWatchers_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Alice" };
            var indexed = MockIndexed(item);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.False(result);
        }

        [Fact]
        public void HasChanged_WithNoWatchersButHasGetters_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 30 };
            var indexed = MockIndexed(item);
            ((IIndexerBuilder<TestItem>)sut).Set(AgeKey, (TestItem x) => x.Age, watchForChanges: false);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.False(result);
        }

        // ── Include (watcher registration) ─────────────────────────────────

        [Fact]
        public void WatchesChanges_AfterIncludeWithWatch_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            ((IIndexerBuilder<TestItem>)sut).Include<string>(IncludeKey, true);
            Assert.True(sut.WatchesChanges);
        }

        [Fact]
        public void WatchesChanges_AfterIncludeWithoutWatch_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            ((IIndexerBuilder<TestItem>)sut).Include<string>(IncludeKey, false);
            Assert.False(sut.WatchesChanges);
        }

        [Fact]
        public void Include_WithWatch_OnInsert_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Alice" };
            ((IIndexerBuilder<TestItem>)sut).Include<string>(IncludeKey, true);

            var md = new IndexMetadata();
            md.Set(IncludeKey, "value");
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, null, ref md);

            Assert.True(result);
        }

        [Fact]
        public void Include_WithWatch_WhenValueChanged_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Alice" };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["IncludeValue"] = "old" });
            ((IIndexerBuilder<TestItem>)sut).Include<string>(IncludeKey, true);

            var md = new IndexMetadata();
            md.Set(IncludeKey, "new");
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        [Fact]
        public void Include_WithWatch_WhenValueUnchanged_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Alice" };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["IncludeValue"] = "same" });
            ((IIndexerBuilder<TestItem>)sut).Include<string>(IncludeKey, true);

            var md = new IndexMetadata();
            md.Set(IncludeKey, "same");
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.False(result);
        }

        [Fact]
        public void Include_WithWatch_WhenKeyNotInPrevious_ReturnsTrue()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Alice" };
            var indexed = MockIndexed(item);
            ((IIndexerBuilder<TestItem>)sut).Include<string>(IncludeKey, true);

            var md = new IndexMetadata();
            md.Set(IncludeKey, "value");
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        [Fact]
        public void Include_WithWatch_WhenNoIncomingValue_ReturnsFalse()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Name = "Alice" };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["IncludeValue"] = "value" });
            ((IIndexerBuilder<TestItem>)sut).Include<string>(IncludeKey, true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.False(result);
        }

        // ── When (conditions do not affect watchers) ────────────────────────

        [Fact]
        public void When_ConditionReturningFalse_WatcherStillEvaluates()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 31 };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Age"] = 30 });
            var builder = (IIndexerBuilder<TestItem>)sut;
            builder.When((TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => false);
            builder.Set(AgeKey, (TestItem x) => x.Age, watchForChanges: true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        [Fact]
        public void When_ConditionReturningTrue_WatcherStillEvaluates()
        {
            var sut = new TrackedIndexer<TestItem>();
            var item = new TestItem { Age = 31 };
            var indexed = MockIndexed(item,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["Age"] = 30 });
            var builder = (IIndexerBuilder<TestItem>)sut;
            builder.When((TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => true);
            builder.Set(AgeKey, (TestItem x) => x.Age, watchForChanges: true);

            var md = new IndexMetadata();
            var result = ((IChangeTracker<TestItem>)sut).HasChanged(item, indexed.Object, ref md);

            Assert.True(result);
        }

        // ── Index guards ────────────────────────────────────────────────────

        [Fact]
        public void Index_WithArgs_DoesNotThrow()
        {
            var sut = new TrackedIndexer<TestItem>();
            var metadata = new IndexMetadata();
            sut.OnUpserting(new Mock<IWriteableIndexed<TestItem>>().Object, ref metadata, new Mock<IDatabase>().Object);
        }

        // ── Initialize / Dispose ────────────────────────────────────────────

        [Fact]
        public void Initialize_DoesNotThrow()
        {
            new TrackedIndexer<TestItem>().Initialize();
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            new TrackedIndexer<TestItem>().Dispose();
        }

        // ── Fluent builder returns self ─────────────────────────────────────

        [Fact]
        public void Set_ReturnsSelf()
        {
            var sut = new TrackedIndexer<TestItem>();
            var result = ((IIndexerBuilder<TestItem>)sut).Set(NameKey, (TestItem x) => x.Name);
            Assert.Same(sut, result);
        }

        [Fact]
        public void Set_MetadataAware_ReturnsSelf()
        {
            var sut = new TrackedIndexer<TestItem>();
            var result = ((IIndexerBuilder<TestItem>)sut).Set(NameKey, (TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => current.Name);
            Assert.Same(sut, result);
        }

        [Fact]
        public void Set_WithWatch_ReturnsSelf()
        {
            var sut = new TrackedIndexer<TestItem>();
            var result = ((IIndexerBuilder<TestItem>)sut).Set(AgeKey, (TestItem x) => x.Age, watchForChanges: true);
            Assert.Same(sut, result);
        }

        [Fact]
        public void Requires_ReturnsSelf()
        {
            var sut = new TrackedIndexer<TestItem>();
            var result = ((IIndexerBuilder<TestItem>)sut).Requires(NameKey, (TestItem x) => x.Name);
            Assert.Same(sut, result);
        }

        [Fact]
        public void Requires_MetadataAware_ReturnsSelf()
        {
            var sut = new TrackedIndexer<TestItem>();
            var result = ((IIndexerBuilder<TestItem>)sut).Requires(NameKey, (TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => current.Name);
            Assert.Same(sut, result);
        }

        [Fact]
        public void When_ReturnsSelf()
        {
            var sut = new TrackedIndexer<TestItem>();
            var result = ((IIndexerBuilder<TestItem>)sut).When((TestItem current, IIndexed<TestItem> indexed, ref IndexMetadata metadata) => true);
            Assert.Same(sut, result);
        }

        [Fact]
        public void Include_ReturnsSelf()
        {
            var sut = new TrackedIndexer<TestItem>();
            var result = ((IIndexerBuilder<TestItem>)sut).Include<string>(NameKey);
            Assert.Same(sut, result);
        }
    }
}
