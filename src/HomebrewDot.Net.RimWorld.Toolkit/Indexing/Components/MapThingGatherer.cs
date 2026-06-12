using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
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
            LogVerbose($"MapThingGatherer scanning maps for things");

            if(game?.Maps == null)
            {
                LogVerbose($"Game or game maps were null, skipping MapThingGatherer data gathering");
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
            foreach (var thing in map.listerThings.AllThings)
            {
                thingsPushed++;
                if(_snapshotManager?.Push(thing, (nameof(thing.Map), thing.Map)) == true)
                {
                    thingsAccepted++;
                }
            }
            var thingHolder = map.GetDirectlyHeldThings();
            foreach (var thing in thingHolder)
            {
                thingsPushed++;
                if(_snapshotManager?.Push(thing, (nameof(thing.Map), thing.Map), (ToolkitConstants.Thing.ContainerMetadata, thingHolder), (ToolkitConstants.Thing.HolderMetadata, map)) == true)
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
                        thingsPushed++;
                        if(_snapshotManager?.Push(thing, (nameof(thing.Map), thing.Map), (ToolkitConstants.Thing.ContainerMetadata, heldThings), (ToolkitConstants.Thing.HolderMetadata, holder)) == true)
                        {
                            thingsAccepted++;
                        }
                    }
                }
            }
        
            Log($"Pushed {thingsAccepted}/{thingsPushed} things to snapshot manager from map {map}");
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
