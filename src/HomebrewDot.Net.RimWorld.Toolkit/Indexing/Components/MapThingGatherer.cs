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
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Gathers data for all <see cref="Thing"/> instances on the map.
    /// </summary>
    public class MapThingGatherer : IDataGatherer
    {
        // Statics
        private static ISnapshotManager _snapshotManager;
        /// <summary>
        /// The singleton instance of the <see cref="MapThingGatherer"/>.
        /// </summary>
        public static MapThingGatherer Instance { get; } = new MapThingGatherer();

        private MapThingGatherer()
        {

        }

        /// <inheritdoc/>
        public void GatherData(Game game, ISnapshotManager snapshotManager)
        {
            _snapshotManager = snapshotManager;
            if (IsVerboseEnabled) LogVerbose("MapThingGatherer scanning maps for things");

            if(game?.Maps == null)
            {
                if (IsVerboseEnabled) LogVerbose("Game or game maps were null, skipping MapThingGatherer data gathering");
                return;
            }
            foreach (var map in game.Maps)
            {
                Scan(map);
            }
        }

        private void Scan(Map map)
        {
            Log($"Scanning map {map} for things");
            var thingsPushed = 0;
            var thingsAccepted = 0;
            var seen = new HashSet<Thing>();
            foreach (var thing in map.listerThings.AllThings)
            {
                if (!seen.Add(thing)) continue;
                thingsPushed++;
                var metadata = new IndexMetadata();
                metadata.Set(ToolkitConstants.Thing.Map, map);
                if(_snapshotManager?.Push(thing, ref metadata) == true)
                {
                    thingsAccepted++;
                }
            }
            var thingHolder = map.GetDirectlyHeldThings();
            foreach (var thing in thingHolder)
            {
                if (!seen.Add(thing)) continue;
                thingsPushed++;
                var metadata = new IndexMetadata();
                metadata.Set(ToolkitConstants.Thing.Map, map);
                metadata.Set(ToolkitConstants.Thing.ContainerMetadata, thingHolder);
                metadata.Set(ToolkitConstants.Thing.HolderMetadata, map);
                if (_snapshotManager?.Push(thing, ref metadata) == true)
                {
                    thingsAccepted++;
                }
            }
            var otherHolders = new List<IThingHolder>();
            map.GetChildHolders(otherHolders);
            foreach (var holder in otherHolders)
            {
                var heldThings = holder.GetDirectlyHeldThings();
                if (heldThings != null)
                {
                    foreach (var thing in heldThings)
                    {
                        if (!seen.Add(thing)) continue;
                        thingsPushed++;
                        var metadata = new IndexMetadata();
                        metadata.Set(ToolkitConstants.Thing.Map, map);
                        metadata.Set(ToolkitConstants.Thing.ContainerMetadata, heldThings);
                        metadata.Set(ToolkitConstants.Thing.HolderMetadata, holder);
                        if (_snapshotManager?.Push(thing, ref metadata) == true)
                        {
                            thingsAccepted++;
                        }
                    }
                }
            }
        
            Log($"Pushed {thingsAccepted}/{thingsPushed} things to snapshot manager from map {map}");
            // Force snapshot here so spike is during map generation and not during the next tick(s). Sorry not sorry.
            _ = _snapshotManager?.Snapshot()?.Build();
        }

        /// <inheritdoc/>
        public void Initialize(Game game)
        {
            Toolkit.Hooks.Manager.RegisterHook<MapLifecycleTrigger>(this, e =>
            {
                if (e.Event == MapLifecycleEvent.Generated)
                {
                    Scan(e.Map);
                }
            });
        }
        /// <inheritdoc/>
        public void Reset()
        {
            Toolkit.Hooks.Manager.UnregisterAllBy<MapLifecycleTrigger>(this);
        }
    }
}
