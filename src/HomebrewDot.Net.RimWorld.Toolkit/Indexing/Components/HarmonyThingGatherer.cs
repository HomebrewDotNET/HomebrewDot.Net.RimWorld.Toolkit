using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Gathers data using Harmony patches on the Thing class and its subclasses to extract relevant information about in-game objects for indexing.
    /// Mostly used for syncing updates.
    /// </summary>
    public class HarmonyThingGatherer : IDataGatherer
    {
        // Statics
        private static ISnapshotManager _snapshotManager;
        private static ISnapshotManager<Thing> _thingManager;
        private static ISnapshotManager<Def> _defManager;
        /// <summary>
        /// The singleton instance of the <see cref="HarmonyThingGatherer"/>.
        /// </summary>
        public static HarmonyThingGatherer Instance { get; } = new HarmonyThingGatherer();
        /// <summary>
        /// Increments each time research is finished. Used to invalidate caches that contain research-locked things when new research is completed.
        /// </summary>
        public static int ResearchTracker = 0;
        /// <summary>
        /// Over how many ticks the <see cref="HarmonyThingGatherer"/> will spread out its indexing of things to avoid performance spikes. 
        /// This is the number of ticks between each tick hash update, which is used to determine if a thing should be indexed on a given tick. 
        /// The default value is 60 ticks, which is equivalent to 1 second in game time.
        /// </summary>
        public static int TickHashInterval = 60;
        public static TickManager TickManager;

        private HarmonyThingGatherer()
        {

        }

        /// <inheritdoc/>
        public void GatherData(Game game, ISnapshotManager snapshotManager)
        {
            _snapshotManager = snapshotManager;
            _thingManager = snapshotManager.AsTyped<Thing>();
            _defManager = snapshotManager.AsTyped<Def>();
            TickHashInterval = Invoking.Safe(() => Toolkit.Settings.SlowGatheringEnabled ? ToolkitConstants.TickLongInterval : ToolkitConstants.TickRareInterval, ToolkitConstants.TickLongInterval);
            if (IsVerboseEnabled) LogVerbose($"HarmonyThingGatherer for {typeof(Thing).Name} ready to gather data.");
        }
        /// <inheritdoc/>
        public void Initialize(Game game)
        {
            if (IsVerboseEnabled) LogVerbose($"Initializing HarmonyThingGatherer for {typeof(Thing).Name}.");
            var harmony = Toolkit.Harmony;
            TickManager = game?.tickManager;
            var postfix = AccessTools.Method(typeof(Patches), nameof(Patches.SpawnSetup_Postfix));
            var original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.SpawnSetup));
            if (original != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                LogWarning($"Skipping {nameof(Thing.SpawnSetup)} patch for {typeof(Thing).FullName} because no implemented method was found in its type hierarchy.");
            }

            //postfix = AccessTools.Method(typeof(Patches), nameof(Patches.DoTick_Postfix));
            //original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.DoTick));
            //if (original != null)
            //{
            //    harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            //}
            //else
            //{
            //    LogWarning($"Skipping {ToolkitConstants.Thing.TickMethod} patch for {typeof(Thing).FullName} because no implemented method was found in its type hierarchy.");
            //}

            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Destroy_Postfix));
            original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.Destroy));
            if (original != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                LogWarning($"Skipping {nameof(Thing.Destroy)} patch for {typeof(Thing).FullName} because no implemented method was found in its type hierarchy.");
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.DeSpawn_Postfix));
            original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.DeSpawn));
            if (original != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                LogWarning($"Skipping {nameof(Thing.DeSpawn)} patch for {typeof(Thing).FullName} because no implemented method was found in its type hierarchy.");
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.NotifyAdded_PostFix));
            original = AccessTools.Method(typeof(ThingOwner), ToolkitConstants.Thing.NotifyAddedmethod);
            if (original != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                LogWarning($"Skipping {ToolkitConstants.Thing.NotifyAddedmethod} patch for {typeof(ThingOwner).FullName} because the method was not found.");
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.NotifyRemoved_PostFix));
            original = AccessTools.Method(typeof(ThingOwner), ToolkitConstants.Thing.NotifyRemovedMethod);
            if (original != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                LogWarning($"Skipping {ToolkitConstants.Thing.NotifyRemovedMethod} patch for {typeof(ThingOwner).FullName} because the method was not found.");
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.ExposeData_Postfix));
            original = AccessTools.Method(typeof(ThingOwner<Thing>), nameof(ThingOwner<Thing>.ExposeData));
            if (original != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                LogWarning($"Skipping {nameof(ThingOwner<Thing>.ExposeData)} patch for {typeof(ThingOwner<Thing>).FullName} because the method was not found.");
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.ResearchManager_FinishProject_Postfix));
            original = AccessTools.Method(typeof(ResearchManager), nameof(ResearchManager.FinishProject));
            if (original != null)
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                LogWarning($"Skipping {nameof(ResearchManager.FinishProject)} patch for {typeof(ResearchManager).FullName} because the method was not found.");
            }
        }
        /// <inheritdoc/>
        public void Reset()
        {
            if (IsVerboseEnabled) LogVerbose($"Resetting HarmonyThingGatherer for {typeof(Thing).Name}.");
            _snapshotManager = null;
            _thingManager = null;
            _defManager = null;
            var harmony = Toolkit.Harmony;
            var postfix = AccessTools.Method(typeof(Patches), nameof(Patches.SpawnSetup_Postfix));
            var original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.SpawnSetup));
            if (original != null)
            {
                harmony.Unpatch(original, postfix);
            }

            //postfix = AccessTools.Method(typeof(Patches), nameof(Patches.DoTick_Postfix));
            //original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.DoTick));
            //if (original != null)
            //{
            //    harmony.Unpatch(original, postfix);
            //}

            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.Destroy_Postfix));
            original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.Destroy));
            if (original != null)
            {
                harmony.Unpatch(original, postfix);
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.DeSpawn_Postfix));
            original = ResolveImplementedMethod(typeof(Thing), nameof(Thing.DeSpawn));
            if (original != null)
            {
                harmony.Unpatch(original, postfix);
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.NotifyAdded_PostFix));
            original = AccessTools.Method(typeof(ThingOwner), ToolkitConstants.Thing.NotifyAddedmethod);
            if (original != null)
            {
                harmony.Unpatch(original, postfix);
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.NotifyRemoved_PostFix));
            original = AccessTools.Method(typeof(ThingOwner), ToolkitConstants.Thing.NotifyRemovedMethod);
            if (original != null)
            {
                harmony.Unpatch(original, postfix);
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.ExposeData_Postfix));
            original = AccessTools.Method(typeof(ThingOwner<Thing>), nameof(ThingOwner<Thing>.ExposeData));
            if (original != null)
            {
                harmony.Unpatch(original, postfix);
            }
            postfix = AccessTools.Method(typeof(Patches), nameof(Patches.ResearchManager_FinishProject_Postfix));
            original = AccessTools.Method(typeof(ResearchManager), nameof(ResearchManager.FinishProject));
            if (original != null)
            {
                harmony.Unpatch(original, postfix);
            }

            Toolkit.Hooks.Manager.UnregisterAllBy<MapLifecycleTrigger>(this);
        }

        private static MethodInfo ResolveImplementedMethod(Type targetType, string methodName)
        {
            for (var current = targetType; current != null; current = current.BaseType)
            {
                var method = AccessTools.DeclaredMethod(current, methodName);
                if (method != null && !method.IsAbstract && method.GetMethodBody() != null)
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Contains Harmony patches that hook into the Thing class and its subclasses to gather data when relevant events occur, such as when a thing is spawned in the game world. These patches will extract necessary information from the thing and push it to the snapshot manager for indexing.
        /// </summary>
        public static class Patches
        {
            private static readonly IndexMetadataKey<bool> RespawningAfterLoadKey = IndexMetadataKey<bool>.Get("respawningAfterLoad");
            /// <summary>
            /// Indexes <paramref name="__instance"/> the first time it's spawned in the game world.
            /// </summary>
            /// <param name="__instance">The instance being spawned.</param>
            /// <param name="map">The map the instance is being spawned on.</param>
            /// <param name="respawningAfterLoad">Indicates if the instance is respawning after a load.</param>
            public static void SpawnSetup_Postfix(Thing __instance, Map map, bool respawningAfterLoad)
            {
                var manager = _thingManager;
                if (manager != null)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set(ToolkitConstants.Thing.Map, map);
                    metadata.Set(RespawningAfterLoadKey, respawningAfterLoad);
                    manager.Push(__instance, ref metadata);
                }
            }

            
            /// <summary>
            /// Indexes <paramref name="__instance"/> when it despawns from the game world, providing metadata about the mode of despawning for indexing purposes.
            /// </summary>
            /// <param name="__instance">The instance being spawned.</param>
            /// <param name="map">The map the instance is being spawned on.</param>
            public static void DeSpawn_Postfix(Thing __instance, DestroyMode mode)
            {
                var manager = _thingManager;
                if(manager != null)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set(ToolkitConstants.Thing.Map, __instance.Map ?? __instance.MapHeld);
                    metadata.Set(ToolkitConstants.Thing.DestroyMode, mode);
                    manager.Push(__instance, ref metadata);
                }
            }

            /// <summary>
            /// Pushes updates for <paramref name="__instance"/> to the snapshot manager.
            /// </summary>
            /// <param name="__instance">The instance being updated.</param>
            public static void DoTick_Postfix(Thing __instance)
            {
                var isHashIntervalTick = (TickManager.TicksGame+__instance.thingIDNumber) % TickHashInterval == 0;
                var manager = _thingManager;
                if(isHashIntervalTick)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set(ToolkitConstants.Thing.Map, __instance.Map ?? __instance.MapHeld);
                }
            }
            /// <summary>
            /// Notifies the snapshot manager that <paramref name="__instance"/> has been destroyed.
            /// </summary>
            /// <param name="__instance">The instance being destroyed.</param>
            /// <param name="mode">The mode in which the instance is being destroyed.</param>
            public static void Destroy_Postfix(Thing __instance, DestroyMode mode)
            {
                var manager = _thingManager;
                if(manager != null)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set(ToolkitConstants.Thing.Map, __instance.Map ?? __instance.MapHeld);
                    metadata.Set(ToolkitConstants.Thing.DestroyMode, mode);
                    manager.Destroyed(__instance, ref metadata);
                }
            }

            /// <summary>
            /// Notifies the snapshot manager that <paramref name="item"/> has been added to a container, providing metadata about the container for indexing purposes.
            /// </summary>
            /// <param name="__instance">The container to which the thing has been added.</param>
            /// <param name="item">The thing that has been added to the container.</param>
            public static void NotifyAdded_PostFix(ThingOwner __instance, Thing item)
            {
                var manager = _thingManager;
                if(manager != null)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set(ToolkitConstants.Thing.ContainerMetadata, __instance);
                    metadata.Set(ToolkitConstants.Thing.HolderMetadata, __instance.Owner);
                    manager.Push(item, ref metadata);
                }
            }

            /// <summary>
            /// Notifies the snapshot manager that <paramref name="item"/> has been removed from a container, providing metadata about the container for indexing purposes. This can be used to track when things are moved between containers or removed from the game world.
            /// </summary>
            /// <param name="__instance">The container from which the thing has been removed.</param>
            /// <param name="item">The thing that has been removed from the container.</param>
            public static void NotifyRemoved_PostFix(ThingOwner __instance, Thing item)
            {
                var manager = _thingManager;
                if(manager != null)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set(ToolkitConstants.Thing.ContainerMetadata, __instance);
                    metadata.Set(ToolkitConstants.Thing.HolderMetadata, null);
                    manager.Push(item, ref metadata);
                }
            }

            /// <summary>
            /// Notifies the snapshot manager of the current container when it's data is loaded from a save.
            /// </summary>
            /// <param name="__instance">The container whose data is being loaded.</param>
            public static void ExposeData_Postfix(ThingOwner<Thing> __instance)
            {
                if (Scribe.mode == LoadSaveMode.Saving) return;
                var manager = _thingManager;
                if(manager == null) return;
                foreach (var item in __instance)
                {
                    var metadata = new IndexMetadata();
                    metadata.Set(ToolkitConstants.Thing.ContainerMetadata, __instance);
                    metadata.Set(ToolkitConstants.Thing.HolderMetadata, __instance.Owner);
                    manager.Push(item, ref metadata);
                }
            }


            /// <summary>
            /// Notifies the snapshot manager that a research project has been completed, pushing all unlocked defs to the snapshot manager for indexing. This is necessary to track when new things become available in the game world as a result of research completion, which can affect what things are relevant for indexing and syncing with external applications. The research project def is also pushed to the snapshot manager to provide context for the unlocked defs and allow for more comprehensive indexing and querying capabilities.
            /// </summary>
            /// <param name="__instance">The research manager instance.</param>
            /// <param name="proj">The research project definition that has been completed.</param>
            public static void ResearchManager_FinishProject_Postfix(ResearchManager __instance, ResearchProjectDef proj)
            {
                var manager = _defManager;
                if(manager == null) return;
                ResearchTracker++;

                var metadata = new IndexMetadata();
                manager.Push(proj, ref metadata);

                foreach (var thingDef in proj.UnlockedDefs)
                {
                    metadata = new IndexMetadata();
                    manager.Push(thingDef, ref metadata);

                    if (thingDef is BuildableDef buildableDef)
                    {
                        if (buildableDef.CostList != null)
                        {
                            foreach (var cost in buildableDef.CostList)
                            {
                                if (cost.thingDef != null)
                                {
                                    metadata = new IndexMetadata();
                                    manager.Push(cost.thingDef, ref metadata);
                                }
                            }
                        }

                        if (buildableDef.MadeFromStuff)
                        {
                            foreach (var stuffCategory in buildableDef.stuffCategories)
                            {
                                var stuffDefs = DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.stuffCategories != null && td.stuffCategories.Contains(stuffCategory));
                                foreach (var stuffDef in stuffDefs)
                                {
                                    metadata = new IndexMetadata();
                                    manager.Push(stuffDef, ref metadata);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
