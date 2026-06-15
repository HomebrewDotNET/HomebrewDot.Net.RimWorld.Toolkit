using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Moq;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.Rimworld.Tests.Collecting.Components
{
    public class CollectionComparatorTests
    {
        [Fact]
        public void Matches_WithMissingCollectionReference_ThrowsInvalidOperationException()
        {
            var comparator = new Mock<IComparator>();
            var sut = new CollectionComparator(comparator.Object);
            var root = new CollectionDef
            {
                Inclusions = new[] { new CollectionConditionDef { Name = "missing" } },
            };

            Assert.Throws<InvalidOperationException>(() => sut.Matches(root, new object(), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>()));
        }

        [Fact]
        public void Matches_WithExclusionMatch_ReturnsFalseEvenWhenConditionsPass()
        {
            var comparator = new Mock<IComparator>();
            comparator.Setup(c => c.Compare(It.IsAny<object>(), It.IsAny<ConditionDef>(), It.IsAny<IReadOnlyDictionary<string, object>>())).Returns(true);

            var excluded = new CollectionDef { Conditions = new[] { new ConditionDef { Compare = 1, With = "eq", To = 1 } } };
            var root = new CollectionDef
            {
                Conditions = new[] { new ConditionDef { Compare = 1, With = "eq", To = 1 } },
                Exclusions = new[] { new CollectionConditionDef { Name = "excluded" } },
            };

            var collections = new Dictionary<string, ICollectionDef>
            {
                ["excluded"] = excluded,
            };

            var sut = new CollectionComparator(comparator.Object);
            var result = sut.Matches(root, new object(), collections, new Dictionary<string, object>());

            Assert.False(result);
        }

        [Fact]
        public void Matches_WithConditionsAndInclusions_AndMode_RequiresBoth()
        {
            var conditionComparator = new Mock<IComparator>();
            conditionComparator
                .SetupSequence(c => c.Compare(It.IsAny<object>(), It.IsAny<ConditionDef>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Returns(false)
                .Returns(true);

            var subCollection = new CollectionDef
            {
                Conditions = new[] { new ConditionDef { Compare = 1, With = "eq", To = 1 } },
            };
            var root = new CollectionDef
            {
                Conditions = new[] { new ConditionDef { Compare = 1, With = "eq", To = 1 } },
                Inclusions = new[] { new CollectionConditionDef { Name = "sub" } },
                InclusionsAreOr = false,
            };

            var collections = new Dictionary<string, ICollectionDef>
            {
                ["sub"] = subCollection,
            };

            var sut = new CollectionComparator(conditionComparator.Object);
            var result = sut.Matches(root, new object(), collections, new Dictionary<string, object>());

            Assert.False(result);
        }

        [Fact]
        public void Matches_WithConditionsAndInclusions_OrMode_AllowsEitherSide()
        {
            Toolkit.ConfigureServices();
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            var subCollection = new CollectionDef
            {
                Conditions = new[] { new ConditionDef { Compare = 1, With = "eq", To = 1 } },
            };
            var root = new CollectionDef
            {
                Conditions = new[] { new ConditionDef { Compare = 1, With = "eq", To = 0 } },
                Inclusions = new[] { new CollectionConditionDef { Name = "sub" } },
                InclusionsAreOr = true,
            };

            var collections = new Dictionary<string, ICollectionDef>
            {
                ["sub"] = subCollection,
            };

            var sut = new CollectionComparator(conditionComparator);
            var subIsMatch = sut.Matches(subCollection, new object(), collections, new Dictionary<string, object>());
            var result = sut.Matches(root, new object(), collections, new Dictionary<string, object>());

            Assert.True(subIsMatch);
            Assert.True(result);
        }

        [Fact]
        public void Matches_WithInclusionCollectionConditions_EvaluatesAndOrCombinations()
        {
            Toolkit.ConfigureServices();
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            var subA = new CollectionDef
            {
                Conditions = new[] { new ConditionDef { Compare = 0, With = "eq", To = 1 } },
            };
            var subB = new CollectionDef
            {
                Conditions = new[] { new ConditionDef { Compare = 1, With = "eq", To = 1 } },
            };

            var root = new CollectionDef
            {
                Inclusions = new[]
                {
                    new CollectionConditionDef { Name = "A", IsOr = true },
                    new CollectionConditionDef { Name = "B", IsOr = false },
                },
            };

            var collections = new Dictionary<string, ICollectionDef>
            {
                ["A"] = subA,
                ["B"] = subB,
            };

            var sut = new CollectionComparator(conditionComparator);
            var result = sut.Matches(root, new object(), collections, new Dictionary<string, object>());

            Assert.True(result);
        }

        [Fact]
        public void Matches_WithFivePlusInclusions_AndOrChaining_EvaluatesGroupLogicCorrectly()
        {
            var sut = new CollectionComparator(CreateBoolComparator());
            var collections = new Dictionary<string, ICollectionDef>
            {
                ["A"] = CreateStaticBoolCollection(true),
                ["B"] = CreateStaticBoolCollection(false),
                ["C"] = CreateStaticBoolCollection(true),
                ["D"] = CreateStaticBoolCollection(true),
                ["E"] = CreateStaticBoolCollection(false),
            };

            // (A AND B) OR (C AND D) OR E
            var root = new CollectionDef
            {
                Inclusions = new[]
                {
                    new CollectionConditionDef { Name = "A", IsOr = false },
                    new CollectionConditionDef { Name = "B", IsOr = true },
                    new CollectionConditionDef { Name = "C", IsOr = false },
                    new CollectionConditionDef { Name = "D", IsOr = true },
                    new CollectionConditionDef { Name = "E", IsOr = false },
                },
            };

            var result = sut.Matches(root, new object(), collections, new Dictionary<string, object>());

            Assert.True(result);
        }

        [Fact]
        public void Matches_WithNestedInclusions_ResolvesRecursively()
        {
            var sut = new CollectionComparator(CreateBoolComparator());

            var collections = new Dictionary<string, ICollectionDef>
            {
                ["Leaf"] = CreateStaticBoolCollection(true),
                ["Mid"] = new CollectionDef
                {
                    Inclusions = new[]
                    {
                        new CollectionConditionDef { Name = "Leaf" },
                    },
                },
            };

            var root = new CollectionDef
            {
                Inclusions = new[]
                {
                    new CollectionConditionDef { Name = "Mid" },
                },
            };

            var result = sut.Matches(root, new object(), collections, new Dictionary<string, object>());

            Assert.True(result);
        }

        [Fact]
        public void Matches_WithNestedExclusion_ExcludesWhenNestedChainMatches()
        {
            var sut = new CollectionComparator(CreateBoolComparator());

            var collections = new Dictionary<string, ICollectionDef>
            {
                ["Leaf"] = CreateStaticBoolCollection(true),
                ["MidExcluded"] = new CollectionDef
                {
                    Inclusions = new[]
                    {
                        new CollectionConditionDef { Name = "Leaf" },
                    },
                },
            };

            var root = new CollectionDef
            {
                Conditions = new[]
                {
                    new ConditionDef { Compare = true, With = "eq", To = true },
                },
                Exclusions = new[]
                {
                    new CollectionConditionDef { Name = "MidExcluded" },
                },
            };

            var result = sut.Matches(root, new object(), collections, new Dictionary<string, object>());

            Assert.False(result);
        }

        private static IComparator CreateBoolComparator()
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

        private static CollectionDef CreateStaticBoolCollection(bool value)
        {
            return new CollectionDef
            {
                Conditions = new[]
                {
                    new ConditionDef { Compare = value, With = "eq", To = true },
                },
            };
        }
    }
}