using Verse;
using UnityEngine;
using HomebrewDot.Net.Rimworld.UI.Settings;

namespace HomebrewDot.Net.Rimworld
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

            var slowGathering = Toolkit.Settings.SlowGatheringEnabled;
            listing.CheckboxLabeled(
                "Slow Gathering",
                ref slowGathering,
                "Use TickLong instead of TickRare for snapshots. Lower load, but snapshots are older.");
            var slowGatheringChanged = slowGathering != Toolkit.Settings.SlowGatheringEnabled;
            Toolkit.Settings.SlowGatheringEnabled = slowGathering;

            var verbose = Toolkit.Settings.Verbose;
            listing.CheckboxLabeled(
                "Verbose Logging",
                ref verbose,
                "Enable verbose logging for debugging purposes.");
            var verboseChanged = verbose != Toolkit.Settings.Verbose;
            Toolkit.Settings.Verbose = verbose;

            var perfLogging = Toolkit.Settings.PerformanceLogging;
            listing.CheckboxLabeled(
                "Performance Logging",
                ref perfLogging,
                "Log snapshot timing and throughput metrics without full verbose debug output.");
            var perfLoggingChanged = perfLogging != Toolkit.Settings.PerformanceLogging;
            Toolkit.Settings.PerformanceLogging = perfLogging;

            listing.End();
        }
    }
}
