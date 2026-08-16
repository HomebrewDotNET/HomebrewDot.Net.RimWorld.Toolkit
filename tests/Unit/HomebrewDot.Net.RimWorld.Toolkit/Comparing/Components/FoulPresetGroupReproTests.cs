using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Components
{
    /// <summary>
    /// Reproduces the foul preset condition flow: leaf AND group(OR sub-conditions),
    /// built via ConditionBuilder, split like BuildConditions does, re-added via
    /// CompareFrom, and evaluated through CollectionComparator.
    /// </summary>
    public class FoulPresetGroupReproTests
    {
        private static (CollectionComparator SUT, CollectionDef Collection) BuildAndEvaluateSetup(Action<IConditionBuilder> buildAction)
        {
            Toolkit.ConfigureServices();
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = Services.GetAllNamed<IOperatorType>();
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            var built = ConditionBuilder.Build(buildAction);

            // Split like DynamicFilterPresets.BuildConditions does.
            var split = built.Conditions.Select(x => SimpleFilterPolicyConditionStub.FromDef(x)).ToArray();
            var defs = split.Select(x => x.Condition).ToArray();

            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var cond in defs)
            {
                _ = cBuilder.CompareFrom(cond);
            }
            var collection = collectionBuilder.Collection;

            return (new CollectionComparator(conditionComparator), collection);
        }

        [Fact]
        public void Group_WithLeaf_And_OrGroupThroughCompareFrom_EvaluatesTrue()
        {
            var (sut, collection) = BuildAndEvaluateSetup(builder =>
            {
                builder.Compare.Value(5).With.Equal(5)
                       .And
                       .Group(inner =>
                       {
                           _ = inner.Compare.Value(1).With.Equal(2).Or;
                           _ = inner.Compare.Value(2).With.Equal(2);
                           return inner;
                       });
            });

            var result = sut.Matches(collection, new object(), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>());

            Assert.True(result);
        }

        [Fact]
        public void Group_WithLeaf_And_AndGroupThroughCompareFrom_EvaluatesTrue()
        {
            var (sut, collection) = BuildAndEvaluateSetup(builder =>
            {
                builder.Compare.Value(5).With.Equal(5)
                       .And
                       .Group(inner =>
                       {
                           _ = inner.Compare.Value(1).With.Equal(1).Or;
                           _ = inner.Compare.Value(2).With.Equal(2);
                           return inner;
                       });
            });

            var result = sut.Matches(collection, new object(), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>());

            Assert.True(result);
        }

        [Fact]
        public void Self_Reference_InLeafAndGroup_ThroughCompiledCollection_EvaluatesCorrectly()
        {
            Toolkit.ConfigureServices();
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);

            // Custom operator mimicking InThingCategory: extracts the value from IIndexed<object> and compares.
            var inCat = new DelegateOperatorType((left, right, _, __) =>
            {
                object instance = left;
                if (left is IIndexed<object> indexed)
                {
                    instance = indexed.Value;
                }
                return instance is TestEntity entity && right is string category && entity.Category == category;
            });
            var isMeat = new DelegateOperatorType((left, right, _, __) =>
            {
                object instance = left;
                if (left is IIndexed<object> indexed)
                {
                    instance = indexed.Value;
                }
                return instance is TestEntity entity && entity.IsMeat == (right is bool b && b);
            });

            var operatorTypes = new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
            {
                ["InCat"] = inCat,
                ["IsMeat"] = isMeat,
                ["True"] = TrueOperatorType.Instance,
            };
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            var built = ConditionBuilder.Build(builder =>
            {
                builder.Compare.Self().With.Operator("IsMeat").To.Value(true)
                       .And
                       .Group(inner =>
                       {
                           _ = inner.Compare.Self().With.Operator("InCat").To.Value("Nope").Or;
                           _ = inner.Compare.Self().With.Operator("IsMeat").To.Value(true);
                           return inner;
                       });
            });

            var split = built.Conditions.Select(x => SimpleFilterPolicyConditionStub.FromDef(x)).ToArray();
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var stub in split)
            {
                _ = cBuilder.CompareFrom(stub.Condition);
            }
            var collection = collectionBuilder.Collection;
            var sut = new CollectionComparator(conditionComparator);

            var meatEntity = new TestEntity { IsMeat = true, Category = "MeatBad" };
            var indexedMeat = new TestIndexed(meatEntity);
            var result = sut.Matches(collection, indexedMeat, new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>());

            Assert.True(result);
        }

        [Fact]
        public void TopLevelOr_WithMovedItemNotIsMeat_MatchesViaCategoryBranch()
        {
            // Simulates the Bad Meat Category mod: the item was moved out of MeatRaw so
            // IsMeat is false, but it IS in the MeatBad category. The top-level OR must
            // match it via the category branch.
            Toolkit.ConfigureServices();
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);

            var inCat = new DelegateOperatorType((left, right, _, __) =>
            {
                object instance = left;
                if (left is IIndexed<object> indexed)
                {
                    instance = indexed.Value;
                }
                return instance is TestEntity entity && right is string category && entity.Category == category;
            });
            var isTrue = new DelegateOperatorType((left, right, _, __) =>
            {
                object instance = left;
                if (left is IIndexed<object> indexed)
                {
                    instance = indexed.Value;
                }
                if (instance is not TestEntity entity || right is not string property)
                {
                    return false;
                }
                return property == nameof(TestEntity.IsMeat) ? entity.IsMeat : entity.IsFoul;
            });

            var operatorTypes = new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
            {
                ["InCat"] = inCat,
                ["IsTrue"] = isTrue,
            };
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            // (IsFoul AND IsMeat) OR (in MeatBad) — the fixed top-level OR structure.
            var built = ConditionBuilder.Build(builder =>
            {
                var foulAndMeat = builder.Compare.Self().With.Operator("IsTrue").To.Value(nameof(TestEntity.IsFoul))
                                         .And
                                         .Compare.Self().With.Operator("IsTrue").To.Value(nameof(TestEntity.IsMeat));
                _ = foulAndMeat.Or
                               .Compare.Self().With.Operator("InCat").To.Value("MeatBad");
            });

            var split = built.Conditions.Select(x => SimpleFilterPolicyConditionStub.FromDef(x)).ToArray();
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var stub in split)
            {
                _ = cBuilder.CompareFrom(stub.Condition);
            }
            var collection = collectionBuilder.Collection;
            var sut = new CollectionComparator(conditionComparator);

            // Moved item: IsMeat = false, IsFoul = false, but in MeatBad -> must match via OR branch.
            var movedEntity = new TestEntity { IsMeat = false, IsFoul = false, Category = "MeatBad" };
            Assert.True(sut.Matches(collection, new TestIndexed(movedEntity), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>()));

            // Regular foul meat: IsMeat = true, IsFoul = true, not in MeatBad -> must match via AND branch.
            var foulEntity = new TestEntity { IsMeat = true, IsFoul = true, Category = "MeatRaw" };
            Assert.True(sut.Matches(collection, new TestIndexed(foulEntity), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>()));

            // Regular non-foul meat: IsMeat = true, IsFoul = false, not in MeatBad -> must NOT match.
            var normalEntity = new TestEntity { IsMeat = true, IsFoul = false, Category = "MeatRaw" };
            Assert.False(sut.Matches(collection, new TestIndexed(normalEntity), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>()));
        }

        [Fact]
        public void TopLevelOr_WithMovedItemInDavaiNastyMeat_MatchesViaCategoryBranch()
        {
            // Simulates Davai's Sorted Categories: the item was moved out of MeatRaw into the
            // DavaiNastyMeat category (so IsMeat is false), but it IS in that category. The
            // top-level OR must match it via the category branch.
            Toolkit.ConfigureServices();
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);

            var inCat = new DelegateOperatorType((left, right, _, __) =>
            {
                object instance = left;
                if (left is IIndexed<object> indexed)
                {
                    instance = indexed.Value;
                }
                return instance is TestEntity entity && right is string category && entity.Category == category;
            });
            var isTrue = new DelegateOperatorType((left, right, _, __) =>
            {
                object instance = left;
                if (left is IIndexed<object> indexed)
                {
                    instance = indexed.Value;
                }
                if (instance is not TestEntity entity || right is not string property)
                {
                    return false;
                }
                return property == nameof(TestEntity.IsMeat) ? entity.IsMeat : entity.IsFoul;
            });

            var operatorTypes = new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
            {
                ["InCat"] = inCat,
                ["IsTrue"] = isTrue,
            };
            var conditionComparator = new Comparator(referenceResolver, operatorTypes);

            // (IsFoul AND IsMeat) OR (in MeatBad) OR (in DavaiNastyMeat) — the structure when
            // both Bad Meat Category and Davai's Sorted Categories are loaded.
            var built = ConditionBuilder.Build(builder =>
            {
                var foulAndMeat = builder.Compare.Self().With.Operator("IsTrue").To.Value(nameof(TestEntity.IsFoul))
                                         .And
                                         .Compare.Self().With.Operator("IsTrue").To.Value(nameof(TestEntity.IsMeat));
                _ = foulAndMeat.Or
                               .Compare.Self().With.Operator("InCat").To.Value("MeatBad").Or
                               .Compare.Self().With.Operator("InCat").To.Value("DavaiNastyMeat");
            });

            var split = built.Conditions.Select(x => SimpleFilterPolicyConditionStub.FromDef(x)).ToArray();
            var collectionBuilder = new CollectionBuilder();
            ICollectionBuilder cBuilder = collectionBuilder;
            foreach (var stub in split)
            {
                _ = cBuilder.CompareFrom(stub.Condition);
            }
            var collection = collectionBuilder.Collection;
            var sut = new CollectionComparator(conditionComparator);

            // Moved item: IsMeat = false, IsFoul = false, but in DavaiNastyMeat -> must match via OR branch.
            var nastyEntity = new TestEntity { IsMeat = false, IsFoul = false, Category = "DavaiNastyMeat" };
            Assert.True(sut.Matches(collection, new TestIndexed(nastyEntity), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>()));

            // Item in MeatBad must still match via its OR branch.
            var badEntity = new TestEntity { IsMeat = false, IsFoul = false, Category = "MeatBad" };
            Assert.True(sut.Matches(collection, new TestIndexed(badEntity), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>()));

            // Regular non-foul meat: IsMeat = true, IsFoul = false, not in either bad category -> must NOT match.
            var normalEntity = new TestEntity { IsMeat = true, IsFoul = false, Category = "MeatRaw" };
            Assert.False(sut.Matches(collection, new TestIndexed(normalEntity), new Dictionary<string, ICollectionDef>(), new Dictionary<string, object>()));
        }

        /// <summary>
        /// Minimal stand-in replicating SimpleFilterPolicyCondition.FromDef round-trip so the
        /// repro doesn't depend on the DynamicFilters project.
        /// </summary>
        private sealed class SimpleFilterPolicyConditionStub
        {
            private readonly ConditionDef _staticDef;
            private SimpleFilterPolicyConditionStub(ConditionDef def) => _staticDef = def;
            public ConditionDef Condition => _staticDef;
            public static SimpleFilterPolicyConditionStub FromDef(ConditionDef def) => new SimpleFilterPolicyConditionStub(def);
        }

        private sealed class TestEntity
        {
            public bool IsMeat { get; set; }
            public bool IsFoul { get; set; }
            public string Category { get; set; }
        }

        private sealed class TestIndexed : IIndexed<TestEntity>
        {
            public TestEntity Value { get; }
            public IReadOnlyDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
            public bool HasSnapshot => false;
            public bool IsSnapshot => false;
            public IIndexed<TestEntity> Snapshot => null;

            public bool HasPendingChanges => throw new NotImplementedException();

            public bool IsRemoved => throw new NotImplementedException();

            public bool IsInsert => throw new NotImplementedException();

            public TestIndexed(TestEntity value) => Value = value;
            public TValue GetValue<TValue>(string propertyName)
            {
                if (Metadata.TryGetValue(propertyName, out var metadataValue))
                {
                    return (TValue)metadataValue;
                }
                var property = Value.GetType().GetProperty(propertyName);
                return property != null ? (TValue)property.GetValue(Value) : default;
            }
        }
    }
}
