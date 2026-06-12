using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HomebrewDot.Net.Rimworld.Hooks.Triggers
{
    /// <summary>
    /// Game component that triggers <see cref="MapTriggers"/>.
    /// </summary>
    public class MapTriggers : MapComponent
    {
        /// <inheritdoc cref="MapTriggers"/>
        /// <param name="map">The map instance that was loaded, providing access to map data.</param>
        public MapTriggers(Map map) : base(map)
        {
        }

        /// <inheritdoc/>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Toolkit.Hooks.Manager.Trigger(new MapLifecycleTrigger(map, MapLifecycleEvent.Loaded));
        }

        /// <inheritdoc/>
        public override void MapGenerated()
        {
            base.MapGenerated();
            Toolkit.Hooks.Manager.Trigger(new MapLifecycleTrigger(map, MapLifecycleEvent.Generated));
        }

        /// <inheritdoc/>
        public override void MapRemoved()
        {
            base.MapRemoved();
            Toolkit.Hooks.Manager.Trigger(new MapLifecycleTrigger(map, MapLifecycleEvent.Removed));
        }
    }
    /// <summary>
    /// Defines the different lifecycle events that can occur for a map, such as when a map is loaded, generated or removed.
    /// </summary>
    public enum MapLifecycleEvent
    {
        /// <summary>
        /// Map is loaded from a save file, providing access to existing map data. This event is triggered after the map has been fully initialized and all data has been loaded.
        /// </summary>
        Loaded,
        /// <summary>
        /// Map is generated for the first time, providing access to the newly created map data. This event is triggered after the map has been fully generated and initialized, allowing access to all map data and components.
        /// </summary>
        Generated,
        /// <summary>
        /// Map is removed from the game, providing an opportunity to clean up any associated data or resources. This event is triggered before the map is fully removed, allowing access to all map data and components.
        /// </summary>
        Removed
    }
    /// <summary>
    /// Trigger that runs during the lifecycle of a map, such as when a map is loaded, generated or removed.
    /// </summary>
    public class MapLifecycleTrigger
    {
        // Properties
        /// <summary>
        /// The current map instance that was loaded, providing access to map data.
        /// </summary>
        public Map Map { get; }
        /// <summary>
        /// The specific lifecycle event that occurred for the map, such as loading, generation or removal. This can be used to determine the appropriate actions to take in response to the event.
        /// </summary>
        public MapLifecycleEvent Event { get; }

        /// <inheritdoc cref="OnMapLoadedTrigger"/>
        /// <param name="map"><see cref="Map"/></param>
        /// <param name="lifecycle"><see cref="MapLifecycleEvent"/></param>
        internal MapLifecycleTrigger(Map map, MapLifecycleEvent lifecycle)
        {
            Map = Toolkit.Helpers.Guard.NotNull(map, nameof(map));
            Event = lifecycle;
        }
    }
}
