using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using Xunit;
using static HomebrewDot.Net.Rimworld.Toolkit;

namespace HomebrewDot.Net.Rimworld.Tests.CollectingIntegration
{
    /// <summary>
    /// Integration coverage for the reported "drone corpses stay selected in the Crematorium collection"
    /// bug: three nested collections (Crematorium excludes Robotic Corpses; Robotic Corpses includes
    /// Mechanoid Corpses) registered in the real <see cref="Toolkit.Collecting"/> registry and evaluated
    /// through the default comparator. Also covers the policy-save scenario, where a sub-collection is
    /// re-registered under the same name while a parent collection definition stays untouched.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class CollectionComparatorNestedExclusionIntegrationTests : IDisposable
    {
        private const string MechanoidCorpsesName = "ReproInt_Mechanoid Corpses";
        private const string RoboticCorpsesName = "ReproInt_Robotic Corpses";
        private const string ButcheryName = "ReproInt_Butchery";
        private const string CrematoriumName = "ReproInt_Crematorium";

        private static readonly string[] RegisteredNames = { MechanoidCorpsesName, RoboticCorpsesName, ButcheryName, CrematoriumName };

        public CollectionComparatorNestedExclusionIntegrationTests()
        {
            Toolkit.ConfigureServices();
            Services.Register<IOperatorType>(new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is bool b && c.IsCorpse == b), "ReproInt_IsCorpse");
            Services.Register<IOperatorType>(new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is bool b && c.IsMechanoid == b), "ReproInt_IsMechanoid");
            Services.Register<IOperatorType>(new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is bool b && c.IsHumanlike == b), "ReproInt_IsHumanlike");
            Services.Register<IOperatorType>(new DelegateOperatorType((left, right, _, __) => left is FakeCorpse c && right is string category && c.Categories.Contains(category)), "ReproInt_InCat");
            // The default comparator caches its operator registrations: force a rebuild so the fake
            // operators above are visible.
            Toolkit.Collecting.ReloadDefaultComparator();
            if (Toolkit.Collecting.Comparator is CollectionComparator comparator)
            {
                comparator.ClearCache();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var name in RegisteredNames)
            {
                InvokeSafe(() => Toolkit.Collecting.Remove(name));
            }
            InvokeSafe(() =>
            {
                if (Toolkit.Collecting.Comparator is CollectionComparator comparator)
                {
                    comparator.ClearCache();
                }
            });
            InvokeSafe(() => Toolkit.Collecting.ReloadDefaultComparator());
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void Collecting_WithNestedExclusionThroughRegistry_ExcludesDirectAndNestedMembers()
        {
            // Arrange
            RegisterCollections(includeVanillaDroneCategory: true);
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var mechanoidCorpse = new FakeCorpse { IsCorpse = true, IsMechanoid = true };
            var humanlikeCorpse = new FakeCorpse { IsCorpse = true, IsHumanlike = true };
            var animalCorpse = new FakeCorpse { IsCorpse = true };

            // Act
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var crematorium = definitions[CrematoriumName];
            var drone = Toolkit.Collecting.Comparator.Matches(crematorium, droneCorpse, definitions, new Dictionary<string, object>());
            var mechanoid = Toolkit.Collecting.Comparator.Matches(crematorium, mechanoidCorpse, definitions, new Dictionary<string, object>());
            var humanlike = Toolkit.Collecting.Comparator.Matches(crematorium, humanlikeCorpse, definitions, new Dictionary<string, object>());
            var animal = Toolkit.Collecting.Comparator.Matches(crematorium, animalCorpse, definitions, new Dictionary<string, object>());

            // Assert
            Assert.False(drone);
            Assert.False(mechanoid);
            Assert.False(humanlike);
            Assert.True(animal);
        }

        [Fact]
        public void Collecting_AfterSubCollectionReRegistered_ParentEvaluationUsesUpdatedDefinition()
        {
            // Arrange - register Robotic Corpses without the CorpsesDrone category, then evaluate the
            // untouched Crematorium once so its compiled expression is cached.
            RegisterCollections(includeVanillaDroneCategory: false);
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var crematorium = definitions[CrematoriumName];
            Assert.True(Toolkit.Collecting.Comparator.Matches(crematorium, droneCorpse, definitions, new Dictionary<string, object>()));

            // Act - policy-save equivalent: a new Robotic Corpses definition instance replaces the old
            // one under the same name. In game this relies on the OnCollectionsChanged ->
            // WarmupCache(true) hook registered on save load to clear the compiled parent trees; that
            // hook cannot fire in tests (OnSaveLoadedTrigger is internal and game-bound).
            Toolkit.Collecting.Set(RoboticCorpsesName, CreateRoboticCorpses(includeVanillaDroneCategory: true));
            Toolkit.Collecting.WarmupCache(true);
            var result = Toolkit.Collecting.Comparator.Matches(crematorium, droneCorpse, Toolkit.Collecting.GetAllDefinitions(), new Dictionary<string, object>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Collecting_AfterSubCollectionReRegisteredAndCacheCleared_ParentEvaluationUsesUpdatedDefinition()
        {
            // Arrange - control: same flow as the policy-save repro, with the explicit cache clear the
            // in-game hook performs.
            RegisterCollections(includeVanillaDroneCategory: false);
            var droneCorpse = new FakeCorpse { IsCorpse = true, Categories = new[] { "CorpsesDrone" } };
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var crematorium = definitions[CrematoriumName];
            Assert.True(Toolkit.Collecting.Comparator.Matches(crematorium, droneCorpse, definitions, new Dictionary<string, object>()));

            // Act
            Toolkit.Collecting.Set(RoboticCorpsesName, CreateRoboticCorpses(includeVanillaDroneCategory: true));
            ((CollectionComparator)Toolkit.Collecting.Comparator).ClearCache();
            var result = Toolkit.Collecting.Comparator.Matches(crematorium, droneCorpse, Toolkit.Collecting.GetAllDefinitions(), new Dictionary<string, object>());

            // Assert
            Assert.False(result);
        }

        private static void RegisterCollections(bool includeVanillaDroneCategory)
        {
            Toolkit.Collecting.Set(MechanoidCorpsesName, CreateMechanoidCorpses());
            Toolkit.Collecting.Set(ButcheryName, CreateButchery());
            Toolkit.Collecting.Set(RoboticCorpsesName, CreateRoboticCorpses(includeVanillaDroneCategory));
            Toolkit.Collecting.Set(CrematoriumName, CreateCrematorium());
        }

        private static CollectionDef CreateMechanoidCorpses()
        {
            return new CollectionDef
            {
                Conditions = BuildConditions(b =>
                {
                    _ = b.Compare.Self().With.Operator("ReproInt_IsCorpse").To.Value(true)
                         .And.Compare.Self().With.Operator("ReproInt_IsMechanoid").To.Value(true);
                }),
            };
        }

        private static CollectionDef CreateRoboticCorpses(bool includeVanillaDroneCategory)
        {
            var conditions = BuildConditions(b =>
            {
                var chain = b.Compare.Self().With.Operator("ReproInt_InCat").To.Value("BS_RobotCorpses")
                             .Or.Compare.Self().With.Operator("ReproInt_InCat").To.Value("VQE_CorpsesDrone");
                if (includeVanillaDroneCategory)
                {
                    _ = chain.Or.Compare.Self().With.Operator("ReproInt_InCat").To.Value("CorpsesDrone");
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
                    _ = b.Compare.Self().With.Operator("ReproInt_IsCorpse").To.Value(true)
                         .And.Compare.Self().With.Operator("ReproInt_IsHumanlike").To.Value(true);
                }),
            };
        }

        private static CollectionDef CreateCrematorium()
        {
            return new CollectionDef
            {
                Conditions = BuildConditions(b =>
                {
                    _ = b.Compare.Self().With.Operator("ReproInt_IsCorpse").To.Value(true);
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
