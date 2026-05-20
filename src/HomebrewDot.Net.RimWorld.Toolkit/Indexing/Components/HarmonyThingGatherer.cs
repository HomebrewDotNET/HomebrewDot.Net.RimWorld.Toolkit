using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.RimWorld.Indexing.Components
{
    /// <summary>
    /// Gathers data using Harmony patches on the Thing class and its subclasses to extract relevant information about in-game objects for indexing.
    /// </summary>
    /// <typeparam name="T">The type of Thing to gather data for.</typeparam>
    public class HarmonyThingGatherer<T> : IDataGatherer
        where T : Thing
    {
        private static ISnapshotManager _snapshotManager;

        /// <inheritdoc/>
        public void GatherData(Game game, ISnapshotManager snapshotManager)
        {
            _snapshotManager = snapshotManager;
            Log($"HarmonyThingGatherer for {typeof(T).Name} ready to gather data.");
        }
        /// <inheritdoc/>
        public void Initialize(Game game)
        {
            Log($"Initializing HarmonyThingGatherer for {typeof(T).Name}.");
            var harmony = Toolkit.Harmony;
            var original = AccessTools.Method(typeof(T), nameof(Thing.SpawnSetup));
            var postfix = AccessTools.Method(typeof(Patches), nameof(Patches.SpawnSetup_Postfix));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            original = AccessTools.Method(typeof(T), ToolkitConstants.Thing.TickMethod);
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Tick_Postfix));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            original = AccessTools.Method(typeof(T), nameof(Thing.Destroy));
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Destroy_Postfix));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }
        /// <inheritdoc/>
        public void Reset()
        {
            Log($"Resetting HarmonyThingGatherer for {typeof(T).Name}.");
            _snapshotManager = null;
            var harmony = Toolkit.Harmony;
            var original = AccessTools.Method(typeof(T), nameof(Thing.SpawnSetup));
            var postfix = AccessTools.Method(typeof(Patches), nameof(Patches.SpawnSetup_Postfix));
            harmony.Unpatch(original, postfix);
            original = AccessTools.Method(typeof(T), ToolkitConstants.Thing.TickMethod);
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Tick_Postfix));
            harmony.Unpatch(original, postfix);
            original = AccessTools.Method(typeof(T), nameof(Thing.Destroy));
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Destroy_Postfix));
            harmony.Unpatch(original, postfix);
        }

        /// <summary>
        /// Contains Harmony patches that hook into the Thing class and its subclasses to gather data when relevant events occur, such as when a thing is spawned in the game world. These patches will extract necessary information from the thing and push it to the snapshot manager for indexing.
        /// </summary>
        public static class Patches
        {
            /// <summary>
            /// Indexes <paramref name="__instance"/> the first time it's spawned in the game world.
            /// </summary>
            /// <param name="__instance">The instance being spawned.</param>
            /// <param name="map">The map the instance is being spawned on.</param>
            /// <param name="respawningAfterLoad">Indicates if the instance is respawning after a load.</param>
            public static void SpawnSetup_Postfix(T __instance, Map map, bool respawningAfterLoad)
            {
                var manager = _snapshotManager;

                if(manager != null && __instance != null && HarmonyGatheringCoordinator.CanGatherThisTick(__instance))
                {
                    manager.Push(__instance, (nameof(map), map), (nameof(respawningAfterLoad), respawningAfterLoad));
                }
            }
            /// <summary>
            /// Pushes updates for <paramref name="__instance"/> to the snapshot manager.
            /// </summary>
            /// <param name="__instance">The instance being updated.</param>
            public static void Tick_Postfix(T __instance)
            {
                var manager = _snapshotManager;
                if(manager != null && __instance != null && HarmonyGatheringCoordinator.CanGatherThisTick(__instance))
                {
                    manager.Push(__instance);
                }
            }
            /// <summary>
            /// Notifies the snapshot manager that <paramref name="__instance"/> has been destroyed.
            /// </summary>
            /// <param name="__instance">The instance being destroyed.</param>
            /// <param name="mode">The mode in which the instance is being destroyed.</param>
            public static void Destroy_Postfix(T __instance, DestroyMode mode)
            {
                var manager = _snapshotManager;
                if(manager != null && __instance != null)
                {
                    manager.Push(__instance, (typeof(DestroyMode).Name, mode));
                }
            }
        }
    }
}
