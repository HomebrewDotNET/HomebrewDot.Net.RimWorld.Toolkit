using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Collecting.Components
{
    public class CollectorTests
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyContext =
            new Dictionary<string, object>();

        private static ICollectionDef CreateDef() => new CollectionDef();

        private static Mock<ICollectionComparator> CreateMatchingComparator()
        {
            var mock = new Mock<ICollectionComparator>();
            mock.Setup(c => c.Matches(
                    It.IsAny<ICollectionDef>(),
                    It.IsAny<object>(),
                    It.IsAny<IReadOnlyDictionary<string, ICollectionDef>>(),
                    It.IsAny<Dictionary<string, object>>()))
                .Returns(true);
            return mock;
        }

        private static Mock<ICollectionComparator> CreateNonMatchingComparator()
        {
            var mock = new Mock<ICollectionComparator>();
            mock.Setup(c => c.Matches(
                    It.IsAny<ICollectionDef>(),
                    It.IsAny<object>(),
                    It.IsAny<IReadOnlyDictionary<string, ICollectionDef>>(),
                    It.IsAny<Dictionary<string, object>>()))
                .Returns(false);
            return mock;
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullDefinition_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Collector<string>(null));
        }

        [Fact]
        public void Constructor_WithValidDefinition_ExposesDefinition()
        {
            var def = CreateDef();
            var sut = new Collector<string>(def);
            Assert.Same(def, sut.Definition);
        }

        [Fact]
        public void Constructor_WithValidDefinition_CountIsZero()
        {
            var sut = new Collector<string>(CreateDef());
            Assert.Equal(0, sut.Count);
        }

        // ── CanCollect ────────────────────────────────────────────────────────

        [Fact]
        public void CanCollect_WithNullObj_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            Assert.False(sut.CanCollect(null, EmptyContext));
        }

        [Fact]
        public void CanCollect_BeforeStartCollecting_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());

            Assert.False(sut.CanCollect("hello", EmptyContext));
        }

        [Fact]
        public void CanCollect_WhenComparatorMatches_ReturnsTrue()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            Assert.True(sut.CanCollect("hello", EmptyContext));
        }

        [Fact]
        public void CanCollect_WhenComparatorDoesNotMatch_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateNonMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            Assert.False(sut.CanCollect("hello", EmptyContext));
        }

        [Fact]
        public void CanCollect_AfterStopCollecting_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.StopCollecting();

            Assert.False(sut.CanCollect("hello", EmptyContext));
        }

        // ── Collect ───────────────────────────────────────────────────────────

        [Fact]
        public void Collect_WithNullObj_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            Assert.False(sut.Collect(null, EmptyContext));
        }

        [Fact]
        public void Collect_BeforeStartCollecting_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());

            Assert.False(sut.Collect("hello", EmptyContext));
        }

        [Fact]
        public void Collect_WhenComparatorMatches_ReturnsTrueAndAddsToCollection()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            var result = sut.Collect("hello", EmptyContext);

            Assert.True(result);
            Assert.Equal(1, sut.Count);
            Assert.Contains("hello", sut.GetAll());
        }

        [Fact]
        public void Collect_WhenComparatorDoesNotMatch_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateNonMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            var result = sut.Collect("hello", EmptyContext);

            Assert.False(result);
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void Collect_SameItemTwice_OnlyAddsOnce()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            sut.Collect("hello", EmptyContext);
            sut.Collect("hello", EmptyContext);

            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public void Collect_MultipleDistinctItems_AddsAll()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            sut.Collect("a", EmptyContext);
            sut.Collect("b", EmptyContext);
            sut.Collect("c", EmptyContext);

            Assert.Equal(3, sut.Count);
        }

        // ── Contains ──────────────────────────────────────────────────────────

        [Fact]
        public void Contains_WithNullObj_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.Collect("hello", EmptyContext);

            Assert.False(sut.Contains(null));
        }

        [Fact]
        public void Contains_BeforeAnyCollect_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            Assert.False(sut.Contains("hello"));
        }

        [Fact]
        public void Contains_AfterCollect_ReturnsTrue()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.Collect("hello", EmptyContext);

            Assert.True(sut.Contains("hello"));
        }

        [Fact]
        public void Contains_ForItemNotCollected_ReturnsFalse()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.Collect("hello", EmptyContext);

            Assert.False(sut.Contains("world"));
        }

        // ── GetAll ────────────────────────────────────────────────────────────

        [Fact]
        public void GetAll_BeforeAnyCollect_ReturnsEmpty()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            Assert.Empty(sut.GetAll());
        }

        [Fact]
        public void GetAll_AfterCollectingItems_ReturnsAllCollectedItems()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.Collect("a", EmptyContext);
            sut.Collect("b", EmptyContext);

            var all = sut.GetAll();

            Assert.Equal(2, all.Count);
            Assert.Contains("a", all);
            Assert.Contains("b", all);
        }

        // ── StartCollecting / StopCollecting ──────────────────────────────────

        [Fact]
        public void StartCollecting_SetsComparatorAllowingCollection()
        {
            var sut = new Collector<string>(CreateDef());
            // Before start: no-op
            Assert.False(sut.Collect("x", EmptyContext));

            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());

            Assert.True(sut.Collect("x", EmptyContext));
        }

        [Fact]
        public void StopCollecting_AfterCollection_ClearsAllItems()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.Collect("hello", EmptyContext);
            Assert.Equal(1, sut.Count);

            sut.StopCollecting();

            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void StopCollecting_PreventsSubsequentCollection()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.StopCollecting();

            Assert.False(sut.Collect("hello", EmptyContext));
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [Fact]
        public void Clear_RemovesAllCollectedItemsButKeepsComparator()
        {
            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(CreateMatchingComparator().Object, new Dictionary<string, ICollectionDef>());
            sut.Collect("a", EmptyContext);
            sut.Collect("b", EmptyContext);
            Assert.Equal(2, sut.Count);

            ((ICollector)sut).Clear();

            Assert.Equal(0, sut.Count);
            // Can still collect after clear
            Assert.True(sut.Collect("c", EmptyContext));
        }

        // ── Context propagation ───────────────────────────────────────────────

        [Fact]
        public void Collect_WithContext_PassesContextToComparator()
        {
            IReadOnlyDictionary<string, object> capturedContext = null;
            var comparator = new Mock<ICollectionComparator>();
            comparator.Setup(c => c.Matches(
                    It.IsAny<ICollectionDef>(),
                    It.IsAny<object>(),
                    It.IsAny<IReadOnlyDictionary<string, ICollectionDef>>(),
                    It.IsAny<Dictionary<string, object>>()))
                .Callback<ICollectionDef, object, IReadOnlyDictionary<string, ICollectionDef>, Dictionary<string, object>>(
                    (def, obj, cols, ctx) => capturedContext = ctx)
                .Returns(true);

            var sut = new Collector<string>(CreateDef());
            sut.StartCollecting(comparator.Object, new Dictionary<string, ICollectionDef>());

            var context = new Dictionary<string, object> { ["key"] = "value" };
            sut.Collect("hello", context);

            Assert.NotNull(capturedContext);
            Assert.True(capturedContext.ContainsKey("key"));
        }
    }
}
