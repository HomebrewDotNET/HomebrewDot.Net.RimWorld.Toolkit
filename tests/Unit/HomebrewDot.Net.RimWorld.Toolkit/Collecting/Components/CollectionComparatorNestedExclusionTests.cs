using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.Rimworld.Tests.Collecting.Components
{
    /// <summary>
    /// Reproduces the reported "drone corpses stay selected in the Crematorium collection" bug with three
    /// nested collections, matching the user's definitions:
    /// <list type="bullet">
    /// <item>Mechanoid Corpses: IsCorpse AND IsMechanoid</item>
    /// <item>Robotic Corpses: (InCat BS_RobotCorpses OR InCat VQE_CorpsesDrone OR InCat CorpsesDrone) OR INCLUDE Mechanoid Corpses</item>
    /// <item>Crematorium: IsCorpse EXCLUDE (Butchery OR Robotic Corpses)</item>
    /// </list>
    /// Evaluation goes through the compiled collection cache path, which is what runs in game for
    /// <see cref="StaticCollectionDef"/>-backed collections.
    /// </summary>
    public class CollectionComparatorNestedExclusionTests : IDisposable
    {
        private const string MechanoidCorpsesName = "Repro_Mechanoid Corpses";
        private const string RoboticCorpsesName = "Repro_Robotic Corpses";
        private const string ButcheryName = "Repro_Butchery";

        private readonly CollectionComparator _sut;

        public CollectionComparatorNestedExclusionTests()
        {
            Toolkit.ConfigureServices();
            var referenceTypes = Services.GetAllNamed<IReferenceType>();
            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
            var operatorTypes = new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in Services.GetAllNamed<IOperatorType>())
            {
                operatorTypes[kvp.Key] = kvp.Value;
            }
            operatorTypes["IsCorpse"] = new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is bool b && c.IsCorpse == b);
            operatorTypes["IsMechanoid"] = new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is bool b && c.IsMechanoid == b);
            operatorTypes["IsHumanlike"] = new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is bool b && c.IsHumanlike == b);
            operatorTypes["InCat"] = new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is string category && c.Categories.Contains(category));
            _sut = new CollectionComparator(new Comparator(referenceResolver, operatorTypes));
            // The compiled collection caches are static: isolate this fixture from other tests.
            _sut.ClearCache();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _sut.ClearCache();
        }

        [Fact]
        public void Matches_WithDroneCorpseMatchingExcludedCategory_ReturnsFalse()
        {
            // Arrange
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var collections = CreateCollections();

            // Act
            var result = _sut.Matches(CreateCrematorium(), droneCorpse, collections, new Dictionary<string, object>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Matches_WithMechanoidCorpseMatchedThroughNestedInclusion_ReturnsFalse()
        {
            // Arrange - mechanoid corpse has no drone category, so Robotic Corpses only matches it
            // through its nested "INCLUDE FROM COLLECTIONS WHEN (IN Mechanoid Corpses)".
            var mechanoidCorpse = new FakeCorpse { IsCorpse = true, IsMechanoid = true };
            var collections = CreateCollections();

            // Act
            var roboticMatches = _sut.Matches(collections[RoboticCorpsesName], mechanoidCorpse, collections, new Dictionary<string, object>());
            var crematoriumMatches = _sut.Matches(CreateCrematorium(), mechanoidCorpse, collections, new Dictionary<string, object>());

            // Assert
            Assert.True(roboticMatches);
            Assert.False(crematoriumMatches);
        }

        [Fact]
        public void Matches_WithHumanlikeCorpse_ReturnsFalseThroughOtherExclusion()
        {
            // Arrange
            var humanlikeCorpse = new FakeCorpse { IsCorpse = true, IsHumanlike = true };
            var collections = CreateCollections();

            // Act
            var result = _sut.Matches(CreateCrematorium(), humanlikeCorpse, collections, new Dictionary<string, object>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Matches_WithUnmatchedCorpse_ReturnsTrue()
        {
            // Arrange - plain animal corpse: not robotic, not mechanoid, not humanlike.
            var animalCorpse = new FakeCorpse { IsCorpse = true };
            var collections = CreateCollections();

            // Act
            var result = _sut.Matches(CreateCrematorium(), animalCorpse, collections, new Dictionary<string, object>());

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Matches_WithBatchInput_AppliesNestedExclusionPerItem()
        {
            // Arrange
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var mechanoidCorpse = new FakeCorpse { IsCorpse = true, IsMechanoid = true };
            var animalCorpse = new FakeCorpse { IsCorpse = true };
            var collections = CreateCollections();

            // Act
            var results = _sut.Matches<FakeCorpse>(CreateCrematorium(), new[] { droneCorpse, mechanoidCorpse, animalCorpse }, collections, new Dictionary<string, object>()).ToArray();

            // Assert
            Assert.False(results.Single(x => ReferenceEquals(x.Object, droneCorpse)).Matches);
            Assert.False(results.Single(x => ReferenceEquals(x.Object, mechanoidCorpse)).Matches);
            Assert.True(results.Single(x => ReferenceEquals(x.Object, animalCorpse)).Matches);
        }

        [Fact]
        public void Matches_AfterExcludedCollectionRedefined_UsesUpdatedDefinition()
        {
            // Arrange - Robotic Corpses without the CorpsesDrone category: the drone corpse is not
            // excluded yet, so Crematorium matches it. This compiles and caches Crematorium with the
            // old Robotic Corpses definition inlined.
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var crematorium = CreateCrematorium();
            var collections = CreateCollections(CreateRoboticCorpses(includeVanillaDroneCategory: false));
            Assert.True(_sut.Matches(crematorium, droneCorpse, collections, new Dictionary<string, object>()));

            // Act - the user edits Robotic Corpses to cover CorpsesDrone and saves the policy, which
            // registers a new definition instance under the same name. The Crematorium definition is
            // untouched, so its cache key does not change.
            collections[RoboticCorpsesName] = CreateRoboticCorpses(includeVanillaDroneCategory: true);
            _sut.ClearCache();
            var result = _sut.Matches(crematorium, droneCorpse, collections, new Dictionary<string, object>());

            // Assert - the redefined exclusion must take effect.
            Assert.False(result);
        }

        [Fact]
        public void Matches_AfterExcludedCollectionRedefinedAndCacheCleared_UsesUpdatedDefinition()
        {
            // Arrange - control for the staleness repro: same flow, but with the explicit cache clear
            // that the OnCollectionsChanged -> WarmupCache(true) hook performs in game.
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var crematorium = CreateCrematorium();
            var collections = CreateCollections(CreateRoboticCorpses(includeVanillaDroneCategory: false));
            Assert.True(_sut.Matches(crematorium, droneCorpse, collections, new Dictionary<string, object>()));

            // Act
            collections[RoboticCorpsesName] = CreateRoboticCorpses(includeVanillaDroneCategory: true);
            _sut.ClearCache();
            var result = _sut.Matches(crematorium, droneCorpse, collections, new Dictionary<string, object>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Matches_WithInvertedExclusionReference_ExcludesNonMembers()
        {
            // Arrange - "EXCLUDE FROM COLLECTIONS WHEN (not IN Robotic Corpses)": corpses that are NOT
            // robotic must be excluded. The runtime MatchesCollections path honors Inverted; this pins
            // the same behavior for the compiled path.
            var crematorium = new CollectionDef
            {
                Conditions = BuildConditions(b =>
                {
                    _ = b.Compare.Self().With.Operator("IsCorpse").To.Value(true);
                }),
                Exclusions = new[]
                {
                    new CollectionConditionDef { Name = RoboticCorpsesName, Inverted = true },
                },
            };
            var animalCorpse = new FakeCorpse { IsCorpse = true };
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var collections = CreateCollections();

            // Act
            var nonMemberResult = _sut.Matches(crematorium, animalCorpse, collections, new Dictionary<string, object>());
            var memberResult = _sut.Matches(crematorium, droneCorpse, collections, new Dictionary<string, object>());

            // Assert
            Assert.False(nonMemberResult);
            Assert.True(memberResult);
        }

        private static Dictionary<string, ICollectionDef> CreateCollections(CollectionDef roboticCorpses = null)
        {
            return new Dictionary<string, ICollectionDef>(StringComparer.OrdinalIgnoreCase)
            {
                [MechanoidCorpsesName] = CreateMechanoidCorpses(),
                [ButcheryName] = CreateButchery(),
                [RoboticCorpsesName] = roboticCorpses ?? CreateRoboticCorpses(),
            };
        }

        private static CollectionDef CreateMechanoidCorpses()
        {
            return new CollectionDef
            {
                Conditions = BuildConditions(b =>
                {
                    _ = b.Compare.Self().With.Operator("IsCorpse").To.Value(true)
                         .And.Compare.Self().With.Operator("IsMechanoid").To.Value(true);
                }),
            };
        }

        private static CollectionDef CreateRoboticCorpses(bool includeVanillaDroneCategory = true)
        {
            var conditions = BuildConditions(b =>
            {
                var chain = b.Compare.Self().With.Operator("InCat").To.Value("BS_RobotCorpses")
                             .Or.Compare.Self().With.Operator("InCat").To.Value("VQE_CorpsesDrone");
                if (includeVanillaDroneCategory)
                {
                    _ = chain.Or.Compare.Self().With.Operator("InCat").To.Value("CorpsesDrone");
                }
            });
            return new CollectionDef
            {
                Conditions = conditions,
                Inclusions = new[] { new CollectionConditionDef { Name = MechanoidCorpsesName } },
                InclusionsAreOr = true,
            };
        }

        private static CollectionDef CreateButchery()
        {
            return new CollectionDef
            {
                Conditions = BuildConditions(b =>
                {
                    _ = b.Compare.Self().With.Operator("IsCorpse").To.Value(true)
                         .And.Compare.Self().With.Operator("IsHumanlike").To.Value(true);
                }),
            };
        }

        private static CollectionDef CreateCrematorium()
        {
            return new CollectionDef
            {
                Conditions = BuildConditions(b =>
                {
                    _ = b.Compare.Self().With.Operator("IsCorpse").To.Value(true);
                }),
                Exclusions = new[]
                {
                    new CollectionConditionDef { Name = ButcheryName, IsOr = true },
                    new CollectionConditionDef { Name = RoboticCorpsesName },
                },
            };
        }

        private static ConditionDef[] BuildConditions(Action<IConditionBuilder> buildAction)
        {
            return ConditionBuilder.Build(buildAction).Conditions.ToArray();
        }

        private sealed class FakeCorpse
        {
            public bool IsCorpse { get; set; }
            public bool IsMechanoid { get; set; }
            public bool IsHumanlike { get; set; }
            public string[] Categories { get; set; } = Array.Empty<string>();
        }
    }
}
