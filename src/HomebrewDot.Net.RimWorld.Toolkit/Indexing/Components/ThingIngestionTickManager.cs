using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Listens to new new ingested ticking things and schedules periodic pushes to <see cref="ISnapshotManager"/> in case changes need to be synced.
    /// </summary>
    public class ThingIngestionTickManager : IDatabaseListener<Thing>
    {
        // Constats
        /// <summary>
        /// Contains the mapping from <see cref="TickerType"/> to the corresponding tick interval they will be ticked at.
        /// </summary>
        public static readonly IReadOnlyDictionary<TickerType, int> TickerTypeToInterval = new Dictionary<TickerType, int>
        {
            { TickerType.Normal, ToolkitConstants.TickRareInterval },
            { TickerType.Rare, ToolkitConstants.TickLongInterval },
            { TickerType.Long, ToolkitConstants.TickLongInterval * 2 }
        };
        public static readonly IReadOnlyDictionary<TickerType, int> TickerTypeToSlowInterval = new Dictionary<TickerType, int>
        {
            { TickerType.Normal, ToolkitConstants.TickLongInterval },
            { TickerType.Rare, ToolkitConstants.TickLongInterval * 2 },
            { TickerType.Long, ToolkitConstants.TickLongInterval * 4 }
        };

        // Fields
        private readonly IHookManager _hookManager;
        private readonly ISnapshotManager _snapshotManager;
        private readonly IReadOnlyDictionary<TickerType, int> _tickerTypeToInterval;

        // State
        private ISnapshotManager<Thing> _snapshotThingManager;
        private IHookTriggerer<RequestTickManagement> _requestTriggerer;

        /// <inheritdoc cref="ThingIngestionTickManager"/>
        /// <param name="hookManager">Used to request tick management for ingested things.</param>
        /// <param name="snapshotManager">Used to manage snapshots of ingested things.</param>
        public ThingIngestionTickManager(IHookManager hookManager, ISnapshotManager snapshotManager)
        {
            _hookManager = Guard.NotNull(hookManager, nameof(hookManager));
            _snapshotManager = Guard.NotNull(snapshotManager, nameof(snapshotManager));
            var isSlowGathering = Toolkit.Helpers.Invoking.Safe(() => Toolkit.Settings.SlowGatheringEnabled, false);
            _tickerTypeToInterval = isSlowGathering ? TickerTypeToSlowInterval : TickerTypeToInterval;
        }

        /// <inheritdoc/>
        public void OnDeleted(IIndexed<Thing> indexed, ref IndexMetadata metadata, IDatabase database)
        {}
        /// <inheritdoc/>
        public void OnDeleting(IIndexed<Thing> indexed, ref IndexMetadata metadata, IDatabase database)
        {}
        /// <inheritdoc/>
        public void OnUpserted(IIndexed<Thing> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            indexed = Guard.NotNull(indexed, nameof(indexed));
            if (!indexed.IsInsert) return;
            var tickerType = indexed.Value.def.tickerType;
            if (tickerType == TickerType.Never) return;
            if(!_tickerTypeToInterval.TryGetValue(tickerType, out var interval)) return;

            var managed = Toolkit.Pool<ManagedTickingIndexedThing>.Rent();
            _snapshotThingManager ??= _snapshotManager.AsTyped<Thing>();
            managed.Set(indexed, _snapshotThingManager, interval);
            var request = new RequestTickManagement(managed, true);
            _requestTriggerer ??= _hookManager.GetTriggerer<RequestTickManagement>();
            bool accepted = _requestTriggerer.Trigger(request);
            if(!accepted)
            {
                managed.NotifyRemoved();
            }
        }

        public void OnUpserting(IWriteableIndexed<Thing> indexed, ref IndexMetadata metadata, IDatabase database)
        {}

        private class ManagedTickingIndexedThing : IManagedTickable, IPoolable
        {
            private IIndexed<Thing> _indexed;
            private ISnapshotManager<Thing> _snapshotManager;
            private int _interval;
            private int _hash;

            public ManagedTickingIndexedThing()
            {
            }
            public int Interval => _interval;

            public int Bucket { get; set; }

            public int Hash => _hash;

            public void Set(IIndexed<Thing> indexed, ISnapshotManager<Thing> snapshotManager, int interval)
            {
                _indexed = Guard.NotNull(indexed, nameof(indexed));
                _snapshotManager = Guard.NotNull(snapshotManager, nameof(snapshotManager));
                _interval = interval;
                _hash = indexed.Value.thingIDNumber;
                if(_hash < 0)
                {
                    _hash = _hash & 0x7FFFFFFF;
                }
            }

            public bool Tick()
            {
                if(_indexed.IsRemoved)
                {
                    return false;
                }
                // If item already has pending changes, don't snapshot again
                if (!_indexed.HasPendingChanges)
                {
                    var metadata = new IndexMetadata();
                    _snapshotManager.Update(_indexed, ref metadata);
                }
                return !_indexed.IsRemoved;
            }

            public void NotifyRemoved()
            {
                Toolkit.Pool<ManagedTickingIndexedThing>.Return(this);
            }

            public void Reset()
            {
                _indexed = null;
                _snapshotManager = null;
                _interval = 0;
                _hash = 0;
                Bucket = -1;
            }
        }
    }
}
