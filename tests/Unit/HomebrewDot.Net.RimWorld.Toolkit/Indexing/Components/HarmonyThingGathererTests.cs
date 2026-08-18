using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Moq;
using Verse;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Components
{
    public class HarmonyThingGathererTests
    {
        [Fact]
        public void Reset_AfterGatherData_ClearsManagerSoDestroyPatchDoesNothing()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var typedThingManager = new Mock<ISnapshotManager<Thing>>();
            snapshotManager.Setup(m => m.AsTyped<Thing>()).Returns(typedThingManager.Object);
            var sut = HarmonyThingGatherer.Instance;
            var pawn = CreateUninitialized<Pawn>();

            // Act
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);
            ClearSnapshotManager();
            HarmonyThingGatherer.Patches.Destroy_Postfix(pawn, DestroyMode.KillFinalize);

            // Assert
            typedThingManager.Verify(m => m.Destroyed(It.IsAny<Thing>(), ref It.Ref<IndexMetadata>.IsAny, It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void SpawnSetupPostfix_WhenManagerNotSet_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var typedThingManager = new Mock<ISnapshotManager<Thing>>();
            snapshotManager.Setup(m => m.AsTyped<Thing>()).Returns(typedThingManager.Object);
            var pawn = CreateUninitialized<Pawn>();

            // Ensure static manager is null before invoking patch methods.
            ClearSnapshotManager();

            // Act
            HarmonyThingGatherer.Patches.SpawnSetup_Postfix(pawn, map: null, respawningAfterLoad: true);

            // Assert
            typedThingManager.Verify(m => m.Push(It.IsAny<Thing>(), ref It.Ref<IndexMetadata>.IsAny, It.IsAny<bool>()), Times.Never);
        }

        private static void ClearSnapshotManager()
        {
            var field = typeof(HarmonyThingGatherer).GetField("_snapshotManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, null);
            var thingField = typeof(HarmonyThingGatherer).GetField("_thingManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(thingField);
            thingField.SetValue(null, null);
            var defField = typeof(HarmonyThingGatherer).GetField("_defManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(defField);
            defField.SetValue(null, null);
        }

        private static T CreateUninitialized<T>() where T : class
            => (T)FormatterServices.GetUninitializedObject(typeof(T));
    }
}
