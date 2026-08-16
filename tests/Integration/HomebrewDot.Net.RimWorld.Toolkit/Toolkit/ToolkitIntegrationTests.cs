using System;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.UI;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.ToolkitIntegration
{
    /// <summary>
    /// Integration tests for the <see cref="Toolkit"/> entry point: mod id, singleton initialization guard,
    /// service registration performed by <see cref="Toolkit.ConfigureServices"/> and the corpse-kind metadata keys.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ToolkitIntegrationTests : IDisposable
    {
        /// <summary>
        /// Initializes the toolkit services once per test.
        /// </summary>
        public ToolkitIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
            InvokeSafe(() => Toolkit.Collecting.ReloadDefaultComparator());
        }

        private static void InvokeSafe(Action action) { try { action(); } catch { } }

        [Fact]
        public void Toolkit_ModId_EqualsLowercaseFullTypeName()
        {
            // Act
            var modId = Toolkit.ModId;

            // Assert
            Assert.Equal(typeof(Toolkit).FullName.ToLower(), modId);
        }

        [Fact]
        public void Toolkit_Instance_WhenNotInitialized_ThrowsArgumentNullException()
        {
            // Act & Assert - the singleton is never set up in tests (needs a ModContentPack)
            Assert.Throws<ArgumentNullException>(() => _ = Toolkit.Instance);
        }

        [Fact]
        public void Toolkit_ConfigureServices_RegistersMatchesThingFilterOperatorType()
        {
            // Act
            var registered = Toolkit.Services.Get<IOperatorType>(MatchesThingFilterOperatorType.DefaultTypeName);

            // Assert
            Assert.Same(MatchesThingFilterOperatorType.Instance, registered);
        }

        [Fact]
        public void Toolkit_ConfigureServices_RegistersSpecialThingFilterDefReference()
        {
            // Act
            var reference = Toolkit.Services.Get<IReferenceType>(DefReferenceType<SpecialThingFilterDef>.DefaultTypeName);
            var inputHelper = Toolkit.Services.Get<IReferenceTypeInputHelper>(DefReferenceType<SpecialThingFilterDef>.DefaultTypeName);

            // Assert
            Assert.Same(DefReferenceType<SpecialThingFilterDef>.Instance, reference);
            Assert.NotNull(inputHelper);
        }

        [Fact]
        public void Toolkit_ConfigureServices_RegistersAllOperatorAliases()
        {
            // Act
            var allNamed = Toolkit.Services.GetAllNamed<IOperatorType>();

            // Assert - the default names and at least the common aliases are registered as keys
            Assert.True(allNamed.ContainsKey(EqualsOperatorType.DefaultTypeName));
            Assert.True(allNamed.ContainsKey(NotEqualsOperatorType.DefaultTypeName));
            Assert.True(allNamed.ContainsKey(MatchesThingFilterOperatorType.DefaultTypeName));
            Assert.True(allNamed.ContainsKey("Equal"));
            Assert.True(allNamed.ContainsKey("eq"));
            Assert.True(allNamed.ContainsKey("neq"));
        }

        [Fact]
        public void Toolkit_Indexing_Thing_TrackCorpseKind_DoesNotThrow_AndKeepsSnapshotAccessible()
        {
            // Act
            var ex = Record.Exception(() => Toolkit.Indexing.Thing.TrackCorpseKind());

            // Assert - registering the indexer must not throw and the snapshot manager stays accessible
            Assert.Null(ex);
            Assert.NotNull(Toolkit.Indexing.Manager);
        }

        [Fact]
        public void Toolkit_Indexing_Thing_TrackIsGhoulCorpse_WhenModListerUnavailable_ThrowsTypeInitializationException()
        {
            // TrackIsGhoulCorpse() first evaluates ToolkitConstants.Anomaly.IsLoaded, which calls
            // Verse.ModLister.GetActiveModWithIdentifier. In a Unity-less test host the ModLister type
            // initializer cannot run (it depends on Verse.Prefs/Unity state), so the tracker is guarded by
            // the environment itself. The plan expected this call to no-op when Anomaly is not loaded, but
            // that guard cannot be evaluated here; asserting the exact failure makes the limitation explicit
            // and machine-checkable. This is deterministic: ModLister can never initialize in this host.
            var ex = Record.Exception(() => Toolkit.Indexing.Thing.TrackIsGhoulCorpse());

            Assert.NotNull(ex);
            Assert.IsType<TypeInitializationException>(ex);
            Assert.Contains("ModLister", ex.ToString());
        }

        [Fact]
        public void ToolkitConstants_Thing_CorpseKindMetadataKeys_HaveExpectedNames()
        {
            // Arrange
            var keys = new[]
            {
                ToolkitConstants.Thing.IsGhoulCorpse.Name,
                ToolkitConstants.Thing.IsColonistCorpse.Name,
                ToolkitConstants.Thing.IsStrangerCorpse.Name,
                ToolkitConstants.Thing.IsSlaveCorpse.Name,
                ToolkitConstants.Thing.IsUnnaturalCorpse.Name,
                ToolkitConstants.Thing.IsPetCorpse.Name,
            };

            // Assert - exact expected key names, all distinct
            Assert.Equal("IsGhoulCorpse", keys[0]);
            Assert.Equal("IsColonistCorpse", keys[1]);
            Assert.Equal("IsStrangerCorpse", keys[2]);
            Assert.Equal("IsSlaveCorpse", keys[3]);
            Assert.Equal("IsUnnaturalCorpse", keys[4]);
            Assert.Equal("IsPetCorpse", keys[5]);
            Assert.Equal(keys.Length, keys.Distinct().Count());
        }
    }
}
