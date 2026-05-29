using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.RimWorld.Collecting;
using HomebrewDot.Net.RimWorld.Collecting.Components;
using HomebrewDot.Net.RimWorld.Collecting.Models;
using HomebrewDot.Net.RimWorld.Hooks;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Triggers;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Collecting.Components
{
    public class SnapshotCollectorTests
    {
        private readonly Mock<ICollector<IIndexed<string>>> _mockCollector;
        private readonly Mock<IHookManager> _mockHookManager;
        private readonly ICollectionDef _def;

        public SnapshotCollectorTests()
        {
            _def = new CollectionDef();
            _mockCollector = new Mock<ICollector<IIndexed<string>>>();
            _mockCollector.Setup(c => c.Definition).Returns(_def);
            _mockCollector.Setup(c => c.Count).Returns(0);
            _mockCollector.Setup(c => c.GetAll()).Returns(Array.Empty<IIndexed<string>>());
            _mockHookManager = new Mock<IHookManager>();
        }

        private SnapshotCollector<string> CreateSut(
            Func<IReadOnlyDatabase, IEnumerable<IIndexed<string>>> getThingsToPush = null)
        {
            return new SnapshotCollector<string>(_mockCollector.Object, _mockHookManager.Object, getThingsToPush);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullCollector_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SnapshotCollector<string>(null, _mockHookManager.Object));
        }

        [Fact]
        public void Constructor_WithNullHookManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SnapshotCollector<string>(_mockCollector.Object, null));
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
            _mockCollector.Setup(c => c.GetAll()).Returns(Array.Empty<IIndexed<string>>());
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
            var item1 = new Mock<IIndexed<string>>();
            var item2 = new Mock<IIndexed<string>>();
            var items = new[] { item1.Object, item2.Object };

            var sut = CreateSut(snapshot => items);

            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object));

            _mockCollector.Verify(c => c.Collect(item1.Object, It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
            _mockCollector.Verify(c => c.Collect(item2.Object, It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public void OnTrigger_ReturnsTrue()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());

            var result = sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object));

            Assert.True(result);
        }

        [Fact]
        public void OnTrigger_SetsSnapshotEnablingSubsequentCanCollect()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            var indexed = new Mock<IIndexed<string>>();
            mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(indexed.Object);
            _mockCollector.Setup(c => c.CanCollect(indexed.Object, It.IsAny<IReadOnlyDictionary<string, object>>())).Returns(true);

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object));

            Assert.True(sut.CanCollect("hello", new Dictionary<string, object>()));
        }

        [Fact]
        public void OnTrigger_WhenSnapshotCannotFindItem_CanCollectReturnsFalse()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Find<string>("hello")).Returns((IIndexed<string>)null);

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object));

            Assert.False(sut.CanCollect("hello", new Dictionary<string, object>()));
        }

        // ── Contains ──────────────────────────────────────────────────────────

        [Fact]
        public void Contains_WhenSnapshotFindsItemAndInnerCollectorContainsIt_ReturnsTrue()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            var indexed = new Mock<IIndexed<string>>();
            mockSnapshot.Setup(s => s.Find<string>("hello")).Returns(indexed.Object);
            _mockCollector.Setup(c => c.Contains(indexed.Object)).Returns(true);

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object));

            Assert.True(sut.Contains("hello"));
        }

        [Fact]
        public void Contains_WhenSnapshotCannotFindItem_ReturnsFalse()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            mockSnapshot.Setup(s => s.Find<string>("hello")).Returns((IIndexed<string>)null);

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object));

            Assert.False(sut.Contains("hello"));
        }

        // ── GetAll ────────────────────────────────────────────────────────────

        [Fact]
        public void GetAll_ReturnsUnwrappedValuesFromInnerCollector()
        {
            var mockSnapshot = new Mock<IReadOnlyDatabase>();
            var indexed1 = new Mock<IIndexed<string>>();
            indexed1.Setup(i => i.Value).Returns("hello");
            var indexed2 = new Mock<IIndexed<string>>();
            indexed2.Setup(i => i.Value).Returns("world");

            _mockCollector.Setup(c => c.GetAll()).Returns(new[] { indexed1.Object, indexed2.Object });

            var sut = CreateSut(_ => Enumerable.Empty<IIndexed<string>>());
            sut.OnTrigger(new OnSnapshotTakenTrigger(mockSnapshot.Object));

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

            _mockCollector.Verify(c => c.StartCollecting(comparator.Object, collections), Times.Once);
            _mockHookManager.Verify(h => h.RegisterHook(sut), Times.Once);
        }

        [Fact]
        public void StopCollecting_UnregistersHookAndDelegatesToInnerCollector()
        {
            var sut = CreateSut();

            sut.StopCollecting();

            _mockHookManager.Verify(h => h.UnregisterHook(sut), Times.Once);
            _mockCollector.Verify(c => c.StopCollecting(), Times.Once);
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [Fact]
        public void Clear_DelegatesToInnerCollector()
        {
            var sut = CreateSut();

            sut.Clear();

            _mockCollector.Verify(c => c.Clear(), Times.Once);
        }
    }
}
