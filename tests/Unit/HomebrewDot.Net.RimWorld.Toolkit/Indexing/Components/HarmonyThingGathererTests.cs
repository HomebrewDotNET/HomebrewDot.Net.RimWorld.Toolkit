using System;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using Moq;
using Verse;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Components
{
    public class HarmonyThingGathererTests
    {
        [Fact]
        public void GatherData_WithSnapshotManager_SetsManagerUsedByDestroyPatch()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = HarmonyThingGatherer.Instance;
            var pawn = CreateUninitialized<Pawn>();

            // Act
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);
            HarmonyThingGatherer.Patches.Destroy_Postfix(pawn, DestroyMode.Vanish);

            // Assert
            snapshotManager.Verify(m => m.Destroyed(
                It.Is<Thing>(x => ReferenceEquals(x, pawn)),
                It.IsAny<(string Key, object Value)[]>()),
                Times.Once);
        }

        [Fact]
        public void Reset_AfterGatherData_ClearsManagerSoDestroyPatchDoesNothing()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = HarmonyThingGatherer.Instance;
            var pawn = CreateUninitialized<Pawn>();

            // Act
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);
            ClearSnapshotManager();
            HarmonyThingGatherer.Patches.Destroy_Postfix(pawn, DestroyMode.KillFinalize);

            // Assert
            snapshotManager.Verify(m => m.Destroyed(It.IsAny<Thing>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void DestroyPostfix_WithNullInstance_DoesNotDestroy()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = HarmonyThingGatherer.Instance;
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);

            // Act
            HarmonyThingGatherer.Patches.Destroy_Postfix(null, DestroyMode.Vanish);

            // Assert
            snapshotManager.Verify(m => m.Destroyed(It.IsAny<Thing>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void SpawnSetupPostfix_WithNullInstance_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = HarmonyThingGatherer.Instance;
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);

            // Act
            HarmonyThingGatherer.Patches.SpawnSetup_Postfix(null, map: null, respawningAfterLoad: false);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Thing>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void TickPostfix_WithNullInstance_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = HarmonyThingGatherer.Instance;
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);

            // Act
            HarmonyThingGatherer.Patches.DoTick_Postfix(null);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Thing>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void SpawnSetupPostfix_WhenManagerNotSet_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var pawn = CreateUninitialized<Pawn>();

            // Ensure static manager is null before invoking patch methods.
            ClearSnapshotManager();

            // Act
            HarmonyThingGatherer.Patches.SpawnSetup_Postfix(pawn, map: null, respawningAfterLoad: true);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Thing>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        private static void ClearSnapshotManager()
        {
            var field = typeof(HarmonyThingGatherer).GetField("_snapshotManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, null);
        }

        private static T CreateUninitialized<T>() where T : class
            => (T)FormatterServices.GetUninitializedObject(typeof(T));
    }
}
