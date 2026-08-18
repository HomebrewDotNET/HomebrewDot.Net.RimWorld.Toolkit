using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic
{
    /// <summary>
    /// Represents a tickable object that can be managed by a tick manager. The object is expected to perform some action on each tick and can be notified when it is removed from management.
    /// </summary>
    public interface IManagedTickable
    {
        /// <summary>
        /// The display name of the tickable. Used in logging.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// The bucket index that this tickable object is assigned to.
        /// </summary>
        int Bucket { get; set; }
        /// <summary>
        /// Optional stats set by the manager for logging purposes.
        /// </summary>
        public ManagedTickableStats Stats { get; set; }
        /// <summary>
        /// The hash code of the tickable object, used for identification and management within the tick manager.
        /// </summary>
        int Hash { get; }
        /// <summary>
        /// How often the tickable object should be ticked, in terms of game ticks. For example, an interval of 1 means the object is ticked every game tick, while an interval of 2 means it is ticked every other game tick.
        /// </summary>
        int Interval { get; }

        /// <summary>
        /// Performs arbritrary logic for the tickable object on each tick. Returns true if the object should continue to be managed, or false if it should be removed from management.
        /// </summary>
        /// <returns>True if the object should continue to be managed, false otherwise.</returns>
        bool Tick();
        /// <summary>
        /// Notifies the tickable object that it has been removed from management.
        /// Can be used to release resources or perform cleanup operations when the object is no longer being managed by the tick manager.
        /// </summary>
        void NotifyRemoved();
    }

    /// <summary>
    /// Some statistics that will be maintained by the manager in relation to the tickable object.
    /// </summary>
    public class ManagedTickableStats {
        /// <summary>
        /// The longest time this instance took to tick.
        /// </summary>
        public TimeSpan MaxTickTime { get; set; } = TimeSpan.MinValue;
        /// <summary>
        /// How many times this instance went over budget when ticking.
        /// </summary>
        public int TimesOverBudget { get; set; }
        /// <summary>
        /// The last tick the instance went over it's maximum.
        /// </summary>
        public long LastOffendingTick { get; set; }
        /// <summary>
        /// The next tick we will start monitoring for perf violations again.
        /// </summary>
        public long NextCheckTick { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"MaxTickTime: {MaxTickTime}, TimesOverBudget: {TimesOverBudget}, LastOffendingTick: {LastOffendingTick}, NextCheckTick: {NextCheckTick}";
        }
    }
}
