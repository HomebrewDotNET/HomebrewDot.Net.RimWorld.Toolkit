using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Comparing;
using HomebrewDot.Net.RimWorld.Comparing.Components;
using HomebrewDot.Net.RimWorld.Generic.Models;
using HomebrewDot.Net.RimWorld.Hooks;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Triggers;
using HomebrewDot.Net.RimWorld.Referencing;
using HomebrewDot.Net.RimWorld.Referencing.Components;
using HomebrewDot.Net.RimWorld.Referencing.Models;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;

namespace HomebrewDot.Net.RimWorld.Collecting.Components
{
    /// <summary>
    /// Collector that listens for new snapshots being taken by the <see cref="ISnapshotManager"/> and pushes all items of type <typeparamref name="T"/> from the snapshot to the underlying collector, using the provided function to determine which items to push.
    /// </summary>
    /// <typeparam name="T">The type of items to collect from the snapshot.</typeparam>
    public class SnapshotCollector<T> : ICollector<T>, ICollector<IIndexed<T>>, IHook<OnSnapshotTakenTrigger> where T : class
    {
        // Fields
        private readonly ICollector<IIndexed<T>> _collector;
        private readonly IHookManager _hookmanager;
        private readonly Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> _getThingsToPush;
        private IReadOnlyDatabase _lastSnapshot;

        // Constructor
        /// <inheritdoc cref="SnapshotCollector{T}"/>
        /// <param name="collector">The underlying collector to which items will be pushed.</param>
        /// <param name="hookmanager">The hook manager used to listen for snapshot events.</param>
        /// <param name="getThingsToPush">A function that determines which items to push from the snapshot. If null, all items of type <typeparamref name="T"/> will be pushed.</param>
        public SnapshotCollector(ICollector<IIndexed<T>> collector, IHookManager hookmanager, Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> getThingsToPush = null)
        {
            _collector = Guard.NotNull(collector, nameof(collector));
            _hookmanager = Guard.NotNull(hookmanager, nameof(hookmanager));
            _getThingsToPush = getThingsToPush ?? GetThingsToPush;
        }
        /// <inheritdoc/>
        public ICollectionDef Definition => _collector.Definition;
        /// <inheritdoc/>
        public int Count => _collector.Count;
        /// <inheritdoc/>
        public object Owner => this;
        /// <inheritdoc/>
        public bool Once => false;
        /// <inheritdoc/>
        public byte Priority => byte.MaxValue;

        /// <inheritdoc/>
        public bool CanCollect(T obj, IReadOnlyDictionary<string, object> context)
        {
            if(_lastSnapshot is null)
            {
                return false;
            }

            var indexed = _lastSnapshot.Find<T>(obj);
            if (indexed is null)
            {
                return false;
            }
            return _collector.CanCollect(indexed, context);
        }
        /// <inheritdoc/>
        public bool CanCollect(IIndexed<T> obj, IReadOnlyDictionary<string, object> context)
        {
            return _collector.CanCollect(obj, context);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_collector)
            {
                _collector.Clear();
            }
        }
        /// <inheritdoc/>
        public bool Collect(T obj, IReadOnlyDictionary<string, object> context)
        {
            lock (_collector)
            {
                if (_lastSnapshot is null)
                {
                    return false;
                }
                var indexed = _lastSnapshot.Find<T>(obj);
                if (indexed is null)
                {
                    return false;
                }
                return _collector.Collect(indexed, context);
            }
        }
        /// <inheritdoc/>
        public bool Collect(IIndexed<T> obj, IReadOnlyDictionary<string, object> context)
        {
            lock(_collector)
            {
                return _collector.Collect(obj, context);
            }
        }

        /// <inheritdoc/>
        public bool Contains(T obj)
        {
            lock(_collector)
            {
                if (_lastSnapshot is null)
                {
                    return false;
                }
                var indexed = _lastSnapshot.Find<T>(obj);
                if (indexed is null)
                {
                    return false;
                }
                return _collector.Contains(indexed);
            }
        }
        /// <inheritdoc/>
        public bool Contains(IIndexed<T> obj)
        {
            return _collector.Contains(obj);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<T> GetAll()
        {
            lock (_collector)
            {
                return _collector.GetAll().Select(indexed => indexed.Value).ToList();
            }
        }
        /// <inheritdoc/>
        public IEnumerator GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }

        /// <inheritdoc/>
        public bool OnTrigger(OnSnapshotTakenTrigger arg)
        {
            var snapshot = Guard.NotNull(arg?.Snapshot, nameof(arg.Snapshot));
            LoadFrom(snapshot);
            return true;
        }

        public void LoadFrom(IReadOnlyDatabase snapshot)
        {
            snapshot = Guard.NotNull(snapshot, nameof(snapshot));
            _lastSnapshot = snapshot;
            var thingsToPush = _getThingsToPush(snapshot);
            if(thingsToPush is null)
            {
                return;
            }
            lock (_collector)
            {
                var context = new Dictionary<string, object>
                {
                    { "Snapshot", snapshot },
                    { Comparator.CompareStringToReferenceKey, (Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference>)((condition, ctx, str) => new ReferenceDef()
                        {
                            Type = IndexedReferenceType.DefaultTypeName,
                            Value = str
                        })
                    }
                };
                foreach (var thing in thingsToPush)
                {
                    _collector.Collect(thing, context);
                }
            }
        }

        /// <inheritdoc/>
        public void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections)
        {
            lock (_collector)
            {
                _collector.StartCollecting(comparer, collections);
                _hookmanager.RegisterHook(this);
            }
            IReadOnlyDatabase currentSnapshot = null;
            try
            {
                currentSnapshot = Toolkit.Index.Manager?.DatabaseSnapshot;
            }
            catch (Exception ex)
            {
                Toolkit.Helpers.Logging.LogWarning($"Unable to load current snapshot during collector startup: {ex.Message}");
            }
            if (currentSnapshot != null)
            {
                LoadFrom(currentSnapshot);
            }
        }
        /// <inheritdoc/>
        public void StopCollecting()
        {
            lock (_collector)
            {
                _hookmanager.UnregisterHook(this);
                _collector.StopCollecting();
            }
        }
        /// <inheritdoc/>
        IReadOnlyCollection<IIndexed<T>> ICollector<IIndexed<T>>.GetAll()
        {
            return _collector.GetAll();
        }
        /// <inheritdoc/>
        IReadOnlyCollection<object> ICollector.GetAll()
        {
            return GetAll();
        }

        /// <inheritdoc/>
        private IEnumerable<IIndexed<T>> GetThingsToPush(IReadOnlyDatabase snapshot)
        {
            var tables = snapshot.GetTables<T>();
            foreach (var table in tables)
            {
                foreach (var item in (IEnumerable<IIndexed<T>>)table)
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// Contains exntesion method related to <see cref="SnapshotCollector{T}"/>
    /// </summary>
    public static class SnapshotCollectorExtensions
    {
        /// <summary>
        /// Collects items from the current snapshot of the database, and continues to collect from new snapshots as they are taken. The provided function is used to determine which items to collect from each snapshot. If the function is null, all items of type <typeparamref name="T"/> will be collected from each snapshot.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="builder"></param>
        /// <param name="getThingsToPush"></param>
        /// <returns></returns>
        public static TReturn CollectFromSnapshot<TReturn, T>(this ICollectionBuilder<TReturn> builder, Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> getThingsToPush = null)
            where TReturn : ICollectionBuilder<TReturn>
            where T : class
            => Guard.NotNull(builder, nameof(builder)).CollectWith(collectionDef => new SnapshotCollector<T>(new Collector<IIndexed<T>>(collectionDef), Toolkit.Hooks.Manager, getThingsToPush));
    }
}
