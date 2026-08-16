using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Components
{
    [Trait("Category", "Unit")]
    public class NestedConditionResolutionTests
    {
        private static Comparator CreateComparator()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            return new Comparator(
                referenceResolver: null,
                operatorTypes: new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                compareStringToReference: null,
                toStringToReference: null);
        }

        private static ConditionDef Leaf(object compare, object to, bool isOr = false)
            => new ConditionDef { Compare = compare, With = "eq", To = to, IsOr = isOr };

        [Fact]
        public void Compare_WithPureGroup_AllLeavesMatch_ReturnsTrue()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(5, 5), Leaf(1, 1) },
            };

            var result = sut.Compare(null, group, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithPureGroup_OneLeafDoesNotMatch_ReturnsFalse()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(5, 5), Leaf(1, 2) },
            };

            var result = sut.Compare(null, group, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_WithPureGroup_OrChainedLeaves_ReturnsTrueWhenAnyMatches()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(1, 2, isOr: true), Leaf(2, 2) },
            };

            var result = sut.Compare(null, group, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithPureGroup_OrChainedLeaves_ReturnsFalseWhenNoneMatch()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(1, 2, isOr: true), Leaf(3, 4) },
            };

            var result = sut.Compare(null, group, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_WithGroupAndLeaf_AndGroup_ReturnsTrueWhenBothMatch()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(5, 5) },
                ConditionGroupIsOr = false,
                Compare = 1,
                With = "eq",
                To = 1,
            };

            var result = sut.Compare(null, group, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithGroupAndLeaf_AndGroup_ReturnsFalseWhenLeafDoesNotMatch()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(5, 5) },
                ConditionGroupIsOr = false,
                Compare = 1,
                With = "eq",
                To = 2,
            };

            var result = sut.Compare(null, group, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_WithGroupAndLeaf_OrGroup_ReturnsTrueWhenGroupMatchesAndLeafDoesNot()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(5, 5) },
                ConditionGroupIsOr = true,
                Compare = 1,
                With = "eq",
                To = 2,
            };

            var result = sut.Compare(null, group, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithGroupAndLeaf_OrGroup_ReturnsFalseWhenNeitherMatches()
        {
            var sut = CreateComparator();
            var group = new ConditionDef
            {
                Conditions = new[] { Leaf(1, 2) },
                ConditionGroupIsOr = true,
                Compare = 3,
                With = "eq",
                To = 4,
            };

            var result = sut.Compare(null, group, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_WithNestedGroups_ResolvesRecursively()
        {
            var sut = CreateComparator();
            var innerGroup = new ConditionDef
            {
                Conditions = new[] { Leaf(5, 5), Leaf(1, 2) },
                IsOr = true,
            };
            var outerGroup = new ConditionDef
            {
                Conditions = new[] { innerGroup, Leaf(2, 2) },
            };

            var result = sut.Compare(null, outerGroup, null);

            // Inner group is false, second leaf is true, chained with OR on the inner group.
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithNestedGroups_AllFalse_ReturnsFalse()
        {
            var sut = CreateComparator();
            var innerGroup = new ConditionDef
            {
                Conditions = new[] { Leaf(5, 6) },
                IsOr = true,
            };
            var outerGroup = new ConditionDef
            {
                Conditions = new[] { innerGroup, Leaf(3, 4) },
            };

            var result = sut.Compare(null, outerGroup, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_List_WithOrChaining_ReturnsTrueWhenSecondMatches()
        {
            var sut = CreateComparator();
            IConditionDef[] conditions = { Leaf(1, 2, isOr: true), Leaf(3, 3) };

            var result = sut.Compare(null, conditions, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_List_WithOrChaining_ReturnsFalseWhenNoneMatch()
        {
            var sut = CreateComparator();
            IConditionDef[] conditions = { Leaf(1, 2, isOr: true), Leaf(3, 4) };

            var result = sut.Compare(null, conditions, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_List_WithAndChaining_ReturnsFalseWhenOneDoesNotMatch()
        {
            var sut = CreateComparator();
            IConditionDef[] conditions = { Leaf(5, 5), Leaf(1, 2) };

            var result = sut.Compare(null, conditions, null);

            Assert.False(result);
        }
    }
}
