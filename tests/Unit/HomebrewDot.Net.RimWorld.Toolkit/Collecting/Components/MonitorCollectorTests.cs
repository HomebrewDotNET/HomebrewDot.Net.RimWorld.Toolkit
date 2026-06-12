using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Collecting.Components
{
    public class MonitorCollectorTests
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyContext =
            new Dictionary<string, object>();

        [Fact]
        public void Constructor_WithNullCollectionDef_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MonitorCollector<string>(null, "MonitoredCollection"));
        }

        [Fact]
        public void Constructor_WithNullMonitoredCollectionName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MonitorCollector<string>(new CollectionDef(), null));
        }

        [Fact]
        public void Constructor_WithEmptyMonitoredCollectionName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new MonitorCollector<string>(new CollectionDef(), ""));
        }

        [Fact]
        public void Constructor_WithCollectionDefHavingNoExclusions_AddsMonitoredExclusion()
        {
            var def = new CollectionDef();

            var sut = new MonitorCollector<string>(def, "MonCol");

            Assert.NotNull(sut.Definition.Exclusions);
            Assert.Single(sut.Definition.Exclusions);
            var exclusion = sut.Definition.Exclusions[0];
            Assert.Equal("MonCol", exclusion.Name);
            Assert.True(exclusion.Inverted);
            Assert.True(exclusion.IsOr);
        }

        [Fact]
        public void Constructor_WithCollectionDefHavingExistingExclusions_PrependsMonitoredExclusion()
        {
            var existingExclusion = new CollectionConditionDef { Name = "Existing", Inverted = false };
            var def = new CollectionDef
            {
                Exclusions = new[] { existingExclusion }
            };

            var sut = new MonitorCollector<string>(def, "MonCol");

            Assert.Equal(2, sut.Definition.Exclusions.Count);
            // Monitored exclusion should be first
            Assert.Equal("MonCol", sut.Definition.Exclusions[0].Name);
            Assert.True(sut.Definition.Exclusions[0].Inverted);
            Assert.True(sut.Definition.Exclusions[0].IsOr);
            // Existing exclusion should be preserved
            Assert.Equal("Existing", sut.Definition.Exclusions[1].Name);
            Assert.False(sut.Definition.Exclusions[1].Inverted);
        }

        [Fact]
        public void Constructor_DoesNotModifyOriginalCollectionDef()
        {
            var def = new CollectionDef
            {
                Exclusions = new[] { new CollectionConditionDef { Name = "Original" } }
            };

            var sut = new MonitorCollector<string>(def, "MonCol");

            // Original def should be unchanged
            Assert.Single(def.Exclusions);
            Assert.Equal("Original", def.Exclusions[0].Name);
            // Sut's definition should have 2 exclusions (original + monitored)
            Assert.Equal(2, sut.Definition.Exclusions.Count);
        }

        [Fact]
        public void Collect_WithMatchingItem_AddsToCollection()
        {
            var def = new CollectionDef();
            var sut = new MonitorCollector<string>(def, "MonCol");
            var comparator = CreateMatchingComparator().Object;
            sut.StartCollecting(comparator, new Dictionary<string, ICollectionDef>());

            sut.Collect("hello", EmptyContext);

            Assert.True(sut.Contains("hello"));
            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public void Collect_WithNonMatchingItem_DoesNotAdd()
        {
            var def = new CollectionDef();
            var sut = new MonitorCollector<string>(def, "MonCol");
            var comparator = CreateNonMatchingComparator().Object;
            sut.StartCollecting(comparator, new Dictionary<string, ICollectionDef>());

            sut.Collect("hello", EmptyContext);

            Assert.False(sut.Contains("hello"));
            Assert.Equal(0, sut.Count);
        }

        // The CollectFromCollection extension method is tested indirectly through
        // the ToolkitCollectingTests integration tests which exercise the full fluent API.

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
    }
}
