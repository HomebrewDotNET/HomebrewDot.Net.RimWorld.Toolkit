using System;
using System.Runtime.Serialization;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Components;
using Moq;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing.Components
{
    public class HarmonyThingGathererTests
    {
        [Fact]
        public void GatherData_WithSnapshotManager_SetsManagerUsedByDestroyPatch()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = new HarmonyThingGatherer<Pawn>();
            var pawn = CreateUninitialized<Pawn>();

            // Act
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);
            HarmonyThingGatherer<Pawn>.Patches.Destroy_Postfix(pawn, DestroyMode.Vanish);

            // Assert
            snapshotManager.Verify(m => m.Push(
                pawn,
                It.Is<(string Key, object Value)[]>(meta =>
                    meta != null
                    && meta.Length == 1
                    && meta[0].Key == nameof(DestroyMode)
                    && Equals(meta[0].Value, DestroyMode.Vanish))),
                Times.Once);
        }

        [Fact]
        public void Reset_AfterGatherData_ClearsManagerSoDestroyPatchDoesNothing()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = new HarmonyThingGatherer<Pawn>();
            var pawn = CreateUninitialized<Pawn>();

            // Act
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);
            ClearSnapshotManager<Pawn>();
            HarmonyThingGatherer<Pawn>.Patches.Destroy_Postfix(pawn, DestroyMode.KillFinalize);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Pawn>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void DestroyPostfix_WithNullInstance_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = new HarmonyThingGatherer<Pawn>();
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);

            // Act
            HarmonyThingGatherer<Pawn>.Patches.Destroy_Postfix(null, DestroyMode.Vanish);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Pawn>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void SpawnSetupPostfix_WithNullInstance_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = new HarmonyThingGatherer<Pawn>();
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);

            // Act
            HarmonyThingGatherer<Pawn>.Patches.SpawnSetup_Postfix(null, map: null, respawningAfterLoad: false);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Pawn>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void TickPostfix_WithNullInstance_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var sut = new HarmonyThingGatherer<Pawn>();
            sut.GatherData(game: null, snapshotManager: snapshotManager.Object);

            // Act
            HarmonyThingGatherer<Pawn>.Patches.Tick_Postfix(null);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Pawn>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        [Fact]
        public void SpawnSetupPostfix_WhenManagerNotSet_DoesNotPush()
        {
            // Arrange
            var snapshotManager = new Mock<ISnapshotManager>();
            var pawn = CreateUninitialized<Pawn>();

            // Ensure static manager is null for this generic type.
            ClearSnapshotManager<Pawn>();

            // Act
            HarmonyThingGatherer<Pawn>.Patches.SpawnSetup_Postfix(pawn, map: null, respawningAfterLoad: true);

            // Assert
            snapshotManager.Verify(m => m.Push(It.IsAny<Pawn>(), It.IsAny<(string Key, object Value)[]>()), Times.Never);
        }

        private static void ClearSnapshotManager<TThing>() where TThing : Thing
        {
            var field = typeof(HarmonyThingGatherer<TThing>).GetField("_snapshotManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, null);
        }

        private static T CreateUninitialized<T>() where T : class
            => (T)FormatterServices.GetUninitializedObject(typeof(T));
    }
}
