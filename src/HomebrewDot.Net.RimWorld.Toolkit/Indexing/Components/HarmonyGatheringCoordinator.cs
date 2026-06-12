using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Verse;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Coordinates the Harmony patches gating game data.
    /// </summary>
    public static class HarmonyGatheringCoordinator
    {
        static HarmonyGatheringCoordinator()
        {
            Toolkit.Hooks.Manager.RegisterHook<ToolkitSettings.Changed>(Toolkit.Instance, e =>
            {
                lock (_lock)
                {
                    _newSettings = e.Settings;
                }
            });
            Toolkit.Hooks.Manager.RegisterHook<OnSnapshotTakenTrigger>(Toolkit.Instance, OnSnapshotTaken);
            Toolkit.Hooks.Manager.RegisterHook<PreparingSnapshotTrigger>(Toolkit.Instance, errorHandler => TickedDynamicGroups = DynamicTickGroups); // Force data gathering since snapshot is imminant.

            _currentSettings = Toolkit.Settings;
        }

        // Fields
        private static readonly object _lock = new object();
        private static ToolkitSettings _currentSettings;
        private static ToolkitSettings _newSettings;

        // Properties
        /// <summary>
        /// How many ticking things were gathering data during the last snapshot.
        /// </summary>
        public static int TickingLastSnapshot { get; private set; }
        /// <summary>
        /// How many things have ticked since the last snapshot.
        /// </summary>
        public static HashSet<object> TickedThings { get; } = new HashSet<object>();
        /// <summary>
        /// How many ticks have passed since the last snapshot was taken.
        /// </summary>
        public static long LastSnapshotTick { get; private set; }
        /// <summary>
        /// How many tick groups will be used to push data to the current snapshot.
        /// </summary>
        public static int DynamicTickGroups { get; private set; }
        /// <summary>
        /// How many tick groups have been ticked since the last snapshot was taken.
        /// </summary>
        public static int TickedDynamicGroups { get; private set; }
        /// <summary>
        /// How many things have been allowed to gather data during the current tick, gated by the dynamic tick group system.
        /// </summary>
        public static int CurrentDynamicGroupTicked { get; private set; }
        /// <summary>
        /// The last tick the current group reached its maximum number of allowed ticks, used to determine when to reset the dynamic group counters and allow more things to gather data.
        /// </summary>
        public static int CurrentDynamicGroupMaxReachedTick { get; private set; }
        /// <summary>
        /// The maximum number of ticks allowed for a dynamic group.
        /// </summary>
        public static int DynamicGroupMaxTicks { get; private set; }

        /// <inheritdoc cref="ToolkitSettings.DynamicGatheringEnabled"/>
        public static bool IsDynamicTickingEnabled => _currentSettings.DynamicGatheringEnabled;
        /// <inheritdoc cref="ToolkitSettings.SlowGatheringEnabled"/>
        public static bool IsSlowTicking => _currentSettings.SlowGatheringEnabled;

        public static bool CanGatherThisTick(object thing)
        {
            if (!IsDynamicTickingEnabled && TickedThings.Add(thing))
            {
                return true;
            }
            // Last group so allow everyting as to not lose data.
            if(TickedDynamicGroups >= DynamicTickGroups)
            {
                _ = TickedThings.Add(thing);
                return true;
            }
            var currentTick = Find.TickManager.TicksGame;
            if (CurrentDynamicGroupMaxReachedTick != currentTick)
            {
                CurrentDynamicGroupTicked = 0;
                TickedDynamicGroups++;
                CurrentDynamicGroupMaxReachedTick = currentTick;
            }
            if (CurrentDynamicGroupTicked < DynamicGroupMaxTicks && TickedThings.Add(thing))
            {
                CurrentDynamicGroupTicked++;
                if(CurrentDynamicGroupTicked >= DynamicGroupMaxTicks)
                {
                }
                return true;
            }
            return false;
        }

        private static void OnSnapshotTaken(OnSnapshotTakenTrigger @event)
        {
            lock (_lock)
            {
                if (_newSettings != null)
                {
                    _currentSettings = _newSettings;
                    _newSettings = null;
                }

                // Reset all counters and update settings for the next snapshot
                TickingLastSnapshot = TickedThings.Count;
                TickedThings.Clear();
                var lastSnapshotTick = LastSnapshotTick;
                LastSnapshotTick = Find.TickManager.TicksGame;
                var ticksSinceLastSnapshot = LastSnapshotTick - lastSnapshotTick;
                DynamicTickGroups = IsDynamicTickingEnabled && TickingLastSnapshot > 0 ? (int)Math.Ceiling((double)TickingLastSnapshot / ticksSinceLastSnapshot) : 0;
                TickedDynamicGroups = 0;
                CurrentDynamicGroupTicked = 0;
                DynamicGroupMaxTicks = DynamicTickGroups > 0 ? TickingLastSnapshot / DynamicTickGroups : 0;
            }
        }
    }
}
