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

            // Fire immediately so cached flags (e.g. logging) update without a restart.
            // The save-time trigger in ToolkitSettings.ExposeData can't detect this: the UI
            // already wrote the new values into the settings object, so the old == new diff
            // guard never sees a change.
            if (slowGatheringChanged || verboseChanged || perfLoggingChanged)
            {
                Toolkit.Hooks.Manager.Trigger(new ToolkitSettings.Changed(Toolkit.Settings));
            }
        }
    }
}
