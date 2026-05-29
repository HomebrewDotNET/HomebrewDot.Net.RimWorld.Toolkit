using System;
using HomebrewDot.Net.RimWorld.Collecting;
using HomebrewDot.Net.RimWorld.Collecting.Models;
using HomebrewDot.Net.RimWorld.Comparing;
using HomebrewDot.Net.RimWorld.Comparing.Models;
using HomebrewDot.Net.RimWorld.Comparing.Template;
using HomebrewDot.Net.RimWorld.Referencing;
using HomebrewDot.Net.RimWorld.Referencing.Components;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Collecting.Models
{
    public class CollectionBuilderTests
    {
        [Fact]
        public void Collection_WithCompareWithToValue_BuildsConditionWithExpectedOperands()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;

            builder.Compare.Value("left").With.Native(NativeOperatorType.Equal).To.Value("right");
            var collection = sut.Collection;

            var condition = Assert.Single(collection.Conditions);
            var left = Assert.IsAssignableFrom<IReference>(condition.Compare);
            var right = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(ValueReferenceType.DefaultTypeName, left.Type);
            Assert.Equal("left", left.Value);
            Assert.Equal("==", condition.With);
            Assert.Equal(ValueReferenceType.DefaultTypeName, right.Type);
            Assert.Equal("right", right.Value);
        }

        [Fact]
        public void Collection_WithEqualShortcut_BuildsConditionWithValueOperand()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;

            builder.Compare.Value(42).With.Equal(100);
            var collection = sut.Collection;

            var condition = Assert.Single(collection.Conditions);
            var left = Assert.IsAssignableFrom<IReference>(condition.Compare);
            var right = Assert.IsAssignableFrom<IReference>(condition.To);
            Assert.Equal(42, left.Value);
            Assert.Equal("==", condition.With);
            Assert.Equal(100, right.Value);
        }

        [Fact]
        public void Collection_WithAndChain_BuildsTwoConditionsAndFirstConditionIsNotOr()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;

            builder.Compare.Value("a").With.Equal("a")
                   .And
                   .Compare.Value("b").With.Equal("b");

            var conditions = sut.Collection.Conditions;

            Assert.Equal(2, conditions.Count);
            Assert.False(conditions[0].IsOr);
            Assert.False(conditions[1].IsOr);
        }

        [Fact]
        public void Collection_WithOrChain_BuildsTwoConditionsAndFirstConditionIsOr()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;

            builder.Compare.Value("a").With.Equal("a")
                   .Or
                   .Compare.Value("b").With.Equal("b");

            var conditions = sut.Collection.Conditions;

            Assert.Equal(2, conditions.Count);
            Assert.True(conditions[0].IsOr);
            Assert.False(conditions[1].IsOr);
        }

        [Fact]
        public void Collection_WithConditionGroup_BuildsSingleTopLevelGroupCondition()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;

            builder.Group(g => g.Compare.Value(1).With.Equal(1)
                                .And
                                .Compare.Value(2).With.Equal(2));

            var top = Assert.Single(sut.Collection.Conditions);
            Assert.NotNull(top.Conditions);
            Assert.Equal(2, top.Conditions.Count);
            Assert.Null(top.With);
            Assert.False(top.IsOr);
        }

        [Fact]
        public void Collection_WithGroupAndFollowingCondition_BuildsChainedTopLevelConditions()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;

            builder.Group(g => g.Compare.Value(1).With.Equal(1)
                                .Or
                                .Compare.Value(2).With.Equal(2))
                   .And
                   .Compare.Value(3).With.Equal(3);

            var conditions = sut.Collection.Conditions;
            Assert.Equal(2, conditions.Count);
            Assert.NotEmpty(conditions[0].Conditions);
            Assert.False(conditions[0].IsOr);
            Assert.Equal("==", conditions[1].With);
        }

        [Fact]
        public void Group_WhenConditionIsInProgress_ThrowsInvalidOperationException()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;
            builder.Compare.Value("left");

            Assert.Throws<InvalidOperationException>(() => builder.Group(g => g));
        }

        [Fact]
        public void TryBuildCollector_WithoutFactory_ReturnsFalseAndNullCollector()
        {
            var sut = new CollectionBuilder();

            var result = sut.TryBuildCollector(new CollectionDef(), out var collector);

            Assert.False(result);
            Assert.Null(collector);
        }

        [Fact]
        public void CollectWith_WithFactory_TryBuildCollectorReturnsFactoryCollector()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;
            var mockCollector = new Mock<ICollector>();
            ICollectionDef capturedDef = null;

            builder.CollectWith(def =>
            {
                capturedDef = def;
                return mockCollector.Object;
            });

            var definition = new CollectionDef();
            var result = sut.TryBuildCollector(definition, out var collector);

            Assert.True(result);
            Assert.Same(mockCollector.Object, collector);
            Assert.Same(definition, capturedDef);
        }

        [Fact]
        public void CollectWithGeneric_WithFactory_TryBuildCollectorReturnsFactoryCollector()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;
            var mockCollector = new Mock<ICollector<string>>();
            ICollectionDef capturedDef = null;

            builder.CollectWith<string>(def =>
            {
                capturedDef = def;
                return mockCollector.Object;
            });

            var definition = new CollectionDef();
            var result = sut.TryBuildCollector(definition, out var collector);

            Assert.True(result);
            Assert.Same(mockCollector.Object, collector);
            Assert.Same(definition, capturedDef);
        }

        [Fact]
        public void Collection_WithIncompleteCondition_ThrowsInvalidOperationException()
        {
            var sut = new CollectionBuilder();
            ICollectionBuilder builder = sut;
            builder.Compare.Value("left");

            Assert.Throws<InvalidOperationException>(() =>
            {
                var _ = sut.Collection;
            });
        }
    }
}
