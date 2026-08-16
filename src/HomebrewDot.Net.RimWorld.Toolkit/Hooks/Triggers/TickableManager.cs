using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Hooks.Triggers
{
    /// <summary>
    /// Accepts tickable objects and manages their ticking based on their specified intervals. This component is responsible for invoking the Tick method of each registered IManagedTickable object at the appropriate time, ensuring that they are ticked according to their defined intervals. It also handles the removal of tickable objects when they indicate that they should no longer be managed.
    /// </summary>
    public class TickableManager : GameComponent, IHook<RequestTickManagement>
    {
        // Fields
        private readonly List<(IManagedTickable tickable, List<IManagedTickable> bucket)> _tickablesToAdd = new();
        private readonly List<(IManagedTickable tickable, List<IManagedTickable> bucket)> _tickablesToRemove = new();
        private readonly Dictionary<int, List<IManagedTickable>[]> _buckets = new();
        private readonly Stopwatch _stopwatch = new();

        // State
        private long _currentTick = 0;
        private long _nextLogTick = ToolkitConstants.TickRareInterval;
        /// <inheritdoc/>
        object IHook<RequestTickManagement>.Owner => this;
        /// <inheritdoc/>
        bool IHook<RequestTickManagement>.Once => false;
        /// <inheritdoc/>
        byte IHandler.Priority => byte.MinValue;
        /// <inheritdoc/>
        bool IHook<RequestTickManagement>.GameScoped => true;

        /// <inheritdoc cref="TickableManager"/>
        /// <param name="game">The game instance that owns this component.</param>
        public TickableManager(Game game) : base()
        {
            Toolkit.Hooks.Manager.RegisterHook<RequestTickManagement>(this);
        }

        /// <inheritdoc/>
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            // Manager tickables
            _currentTick++;
            for (int i = _tickablesToAdd.Count - 1; i >= 0; i--)
            {
                var (tickable, bucket) = _tickablesToAdd[i];
                if(bucket != null)
                {
                    bucket.Add(tickable);
                }
            }
            _tickablesToAdd.Clear();
            for (int i = _tickablesToRemove.Count - 1; i >= 0; i--)
            {
                var (tickable, bucket) = _tickablesToRemove[i];
                if (bucket != null && bucket.Remove(tickable))
                {
                    tickable.NotifyRemoved();
                    tickable.Bucket = -1;
                }
            }
            _tickablesToRemove.Clear();

            // Tick all buckets per their intervals
            bool isLogTick = Logging.IsPerformanceEnabled && _currentTick >= _nextLogTick;
            foreach (var (interval, buckets) in _buckets)
            {
                if (isLogTick)
                {
                    _stopwatch.Restart();
                }
                var tickList = buckets;
                var ticked = TickList(tickList);

                if (isLogTick)
                {
                    _stopwatch.Stop();
                    if (ticked > 0)
                    {
                        _nextLogTick = _currentTick + ToolkitConstants.TickRareInterval;
                        Logging.LogPerformance($"TickableManager: Ticked {ticked} tickables with interval {interval} in {_stopwatch.ElapsedMilliseconds}ms.");
                    }
                }
            }
        }
        /// <inheritdoc/>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
        }

        private int TickList(List<IManagedTickable>[] tickList)
        {
            var buckets = tickList.Length;
            var bucketIndexToTick = (int)(_currentTick % buckets);
            var bucket = tickList[bucketIndexToTick];
            if (bucket != null)
            {
                TickThings(bucket);
                return bucket.Count;
            }
            return 0;
        }

        private void TickThings(List<IManagedTickable> tickables)
        {
            for (int i = 0; i < tickables.Count; i++)
            {
                var tickable = tickables[i];
                try
                {
                    if(tickable != null)
                    {
                        if(!tickable.Tick())
                        {
                            _tickablesToRemove.Add((tickable, tickables));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception occurred while ticking {tickable.GetType().Name}: {ex}");
                }
            }
        }

        private List<IManagedTickable> BucketOf(IManagedTickable tickable)
        {
            tickable = Guard.NotNull(tickable, nameof(tickable));
            var interval = tickable.Interval;
            if(interval < 1)
            {
                throw new ArgumentException($"Tickable {tickable.GetType().Name} has an invalid interval of {interval}. Interval must be greater than 0.");
            }
            if (!_buckets.TryGetValue(interval, out var bucketList))
            {
                bucketList = new List<IManagedTickable>[interval];
                _buckets[interval] = bucketList;
            }
            if(tickable.Bucket >= 0)
            {
                return bucketList[tickable.Bucket];
            }
            var hash = tickable.Hash;
            if(hash < 0)
            {
                throw new ArgumentException($"Tickable {tickable.GetType().Name} has an invalid hash of {hash}. Hash must be greater than or equal to 0.");
            }
            var bucketIndex = hash % interval;
            tickable.Bucket = bucketIndex;
            var bucket = bucketList[bucketIndex];
            if(bucket == null)
            {
                bucket = new List<IManagedTickable>();
                bucketList[bucketIndex] = bucket;
            }
            return bucket;
        }
        /// <inheritdoc/>
        bool IHook<RequestTickManagement>.OnTrigger(RequestTickManagement arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            if (arg.Add)
            {
                arg.Tickable.Bucket = -1;
                var bucket = BucketOf(arg.Tickable);
                _tickablesToAdd.Add((arg.Tickable, bucket));
            }
            else
            {
                var bucket = BucketOf(arg.Tickable);
                _tickablesToRemove.Add((arg.Tickable, bucket));
            }
            return true;
        }
    }

    /// <summary>
    /// Represents a request to add or remove a tickable object from the TickableManager. This class encapsulates the tickable object and the action (add or remove) to be performed on it, allowing for deferred management of tickable objects within the TickableManager's tick cycle.
    /// </summary>
    public class RequestTickManagement
    {
        /// <summary>
        /// The instance to manage.
        /// </summary>
        public IManagedTickable Tickable { get; }
        /// <summary>
        /// If the thing should be added or removed from the manager.
        /// </summary>
        public bool Add { get; }

        /// <inheritdoc cref="RequestTickManagement"/>
        /// <param name="tickable"><inheritdoc cref="Tickable"/></param>
        /// <param name="add"><inheritdoc cref="Add"/></param>
        public RequestTickManagement(IManagedTickable tickable, bool add)
        {
            Tickable = Guard.NotNull(tickable, nameof(tickable));
            Add = add;
        }
    }
}
