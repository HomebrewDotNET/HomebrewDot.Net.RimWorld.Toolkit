using Verse;
using UnityEngine;
using HomebrewDot.Net.RimWorld.UI.Settings;

namespace HomebrewDot.Net.RimWorld
{
    /// <summary>
    /// User-facing settings tab.
    /// </summary>
    internal sealed class SettingsUiTab : IToolkitSettingsTab
    {
        /// <inheritdoc/>
        public string Title => "Settings";

        /// <inheritdoc/>
        public void Draw(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            var dynamicGathering = Toolkit.Settings.DynamicGatheringEnabled;
            listing.CheckboxLabeled(
                "Dynamic Gathering",
                ref dynamicGathering,
                "Spread data gathering over ticks between snapshots to reduce spikes. Experimental.");
            Toolkit.Settings.DynamicGatheringEnabled = dynamicGathering;

            var slowGathering = Toolkit.Settings.SlowGatheringEnabled;
            listing.CheckboxLabeled(
                "Slow Gathering",
                ref slowGathering,
                "Use TickLong instead of TickRare for snapshots. Lower load, but snapshots are older.");
            Toolkit.Settings.SlowGatheringEnabled = slowGathering;

            listing.End();
        }
    }
}
