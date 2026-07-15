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
    public class SnapshotCollectorTests
    {
        private readonly Mock<IHookManager> _mockHookManager;
        private readonly Mock<ISnapshotManager> _mockSnapshotManager;
        private readonly ICollectionDef _def;

        public SnapshotCollectorTests()
        {
            _def = new CollectionDef();
            _mockHookManager = new Mock<IHookManager>();
            _mockSnapshotManager = new Mock<ISnapshotManager>();
        }

        private SnapshotCollector<string> CreateSut(
            Func<IReadOnlyDatabase, IEnumerable<IIndexed<string>>> getThingsToPush = null)
        {
            return new SnapshotCollector<string>(_def, _mockSnapshotManager.Object, _mockHookManager.Object, s => s, getThingsToPush);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullDefinition_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SnapshotCollector<string>(null, _mockSnapshotManager.Object, _mockHookManager.Object));
        }

        [Fact]
        public void Constructor_WithNullSnapshotManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SnapshotCollector<string>(_def, null, _mockHookManager.Object));
        }

        [Fact]
        public void Constructor_WithNullHookManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SnapshotCollector<string>(_def, _mockSnapshotManager.Object, null));
        }

        [Fact]
        public void Constructor_WithValidArgs_ExposesDefinitionFromInnerCollector()
        {
            var sut = CreateSut();
            Assert.Same(_def, sut.Definition);
        }

        // ── Before snapshot: guards ───────────────────────────────────────────

        [Fact]
        public void CanCollect_BeforeSnapshot_ReturnsFalse()
        {
            var sut = CreateSut();
            Assert.False(sut.CanCollect("hello", new Dictionary<string, object>()));
        }

        [Fact]
        public void Collect_BeforeSnapshot_ReturnsFalse()
        {
            var sut = CreateSut();
            Assert.False(sut.Collect("hello", new Dictionary<string, object>()));
        }

        [Fact]
        public void Contains_BeforeSnapshot_ReturnsFalse()
        {
            var sut = CreateSut();
            Assert.False(sut.Contains("hello"));
        }

        [Fact]
        public void GetAll_BeforeSnapshot_ReturnsEmpty()
        {
            var sut = CreateSut();
            Assert.Empty(sut.GetAll());
        }

        // ── OnTrigger ─────────────────────────────────────────────────────────

        [Fact]
        public void OnTrigger_WithNullTrigger_ThrowsArgumentNullException()
        {
            var sut = CreateSut();
            Assert.Throws<ArgumentNullException>(() => sut.OnTrigger(null));
        }

        [Fact]
        public void OnTrigger_PushesEachItemFromSnapshotToInnerCollector()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Version).Returns(1);
            var item1 = new Mock<IIndexed<string>>();
            item1.Setup(i => i.Value).Returns("hello");
            var item2 = new Mock<IIndexed<string>>();
            item2.Setup(i => i.Value).Returns("world");
            var items = new[] { item1.Object, item2.Object };

            var sut = CreateSut(snapshot => items);
            sut.StartCollecting(new MatchingComparator(), new Dictionary<string, ICollectionDef>());

            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object, true));

            Assert.True(sut.Contains("hello"));
            Assert.True(sut.Contains("world"));
        }

        [Fact]
        public void OnTrigger_ReturnsTrue()
        {
            _mockSnapshotManager.Setup(m => m.Database).Returns((IReadOnlyDatabase)null);
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Version).Returns(1);
            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.StartCollecting(new MatchingComparator(), new Dictionary<string, ICollectionDef>());

            var result = sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object, true));

            Assert.True(result);
        }

        [Fact]
        public void OnTrigger_SetsSnapshotEnablingSubsequentCanCollect()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Version).Returns(1);
            var indexed = new Mock<IIndexed<string>>();
            indexed.Setup(i => i.Value).Returns("hello");
            mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(indexed.Object);

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.StartCollecting(new MatchingComparator(), new Dictionary<string, ICollectionDef>());

            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object, true));

            Assert.True(sut.CanCollect("hello", new Dictionary<string, object>()));
        }

        [Fact]
        public void CanCollect_WhenComparatorReturnsFalse_ReturnsFalse()
        {
            _mockSnapshotManager.Setup(m => m.Database).Returns((IReadOnlyDatabase)null);
            var comparator = new Mock<ICollectionComparator>();
            comparator.Setup(c => c.Matches(
                    It.IsAny<ICollectionDef>(),
                    It.IsAny<object>(),
                    It.IsAny<IReadOnlyDictionary<string, ICollectionDef>>(),
                    It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Returns(false);

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.StartCollecting(comparator.Object, new Dictionary<string, ICollectionDef>());

            Assert.False(sut.CanCollect("hello", new Dictionary<string, object>()));
        }

        // ── Contains ──────────────────────────────────────────────────────────

        [Fact]
        public void Contains_WhenSnapshotFindsItemAndInnerCollectorContainsIt_ReturnsTrue()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Version).Returns(1);
            var indexed = new Mock<IIndexed<string>>();
            indexed.Setup(i => i.Value).Returns("hello");

            var sut = CreateSut(_ => new[] { indexed.Object });
            sut.StartCollecting(new MatchingComparator(), new Dictionary<string, ICollectionDef>());

            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object, true));

            Assert.True(sut.Contains("hello"));
        }

        [Fact]
        public void Contains_WhenSnapshotCannotFindItem_ReturnsFalse()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Version).Returns(1);
            mockSnapshot.Setup(s => s.Find<string>("hello")).Returns((IIndexed<string>)null);

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.StartCollecting(new MatchingComparator(), new Dictionary<string, ICollectionDef>());

            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object, true));

            Assert.False(sut.Contains("hello"));
        }

        // ── GetAll ────────────────────────────────────────────────────────────

        [Fact]
        public void GetAll_ReturnsUnwrappedValuesFromInnerCollector()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Version).Returns(1);
            var indexed1 = new Mock<IIndexed<string>>();
            indexed1.Setup(i => i.Value).Returns("hello");
            var indexed2 = new Mock<IIndexed<string>>();
            indexed2.Setup(i => i.Value).Returns("world");

            var sut = CreateSut(_ => new[] { indexed1.Object, indexed2.Object });
            sut.StartCollecting(new MatchingComparator(), new Dictionary<string, ICollectionDef>());

            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object, true));

            var all = sut.GetAll();

            Assert.Equal(2, all.Count);
            Assert.Contains("hello", all);
            Assert.Contains("world", all);
        }

        // ── StartCollecting / StopCollecting ──────────────────────────────────

        [Fact]
        public void StartCollecting_DelegatesToInnerCollectorAndRegistersHook()
        {
            var sut = CreateSut();
            var comparator = new Mock<ICollectionComparator>();
            var collections = new Dictionary<string, ICollectionDef>();

            sut.StartCollecting(comparator.Object, collections);

            _mockHookManager.Verify(h => h.RegisterHook(sut), Times.Once);
        }

        [Fact]
        public void StopCollecting_UnregistersHookAndDelegatesToInnerCollector()
        {
            var sut = CreateSut();

            sut.StopCollecting();

            _mockHookManager.Verify(h => h.UnregisterHook(sut), Times.Once);
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [Fact]
        public void Clear_RemovesAllCollectedItems()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Version).Returns(1);
            var indexed = new Mock<IIndexed<string>>();
            indexed.Setup(i => i.Value).Returns("hello");

            var sut = CreateSut(_ => new[] { indexed.Object });
            sut.StartCollecting(new MatchingComparator(), new Dictionary<string, ICollectionDef>());
            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object, true));

            Assert.True(sut.Contains("hello"));

            sut.Clear();

            Assert.False(sut.Contains("hello"));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// A comparator that always returns true for any match.
        /// </summary>
        private sealed class MatchingComparator : ICollectionComparator
        {
            public bool Matches<T>(ICollectionDef collection, T obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
                => true;

            public IEnumerable<(T Object, bool Matches)> Matches<T>(ICollectionDef collection, IEnumerable<T> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
                => objects.Select(o => (o, true));
        }
    }
}
