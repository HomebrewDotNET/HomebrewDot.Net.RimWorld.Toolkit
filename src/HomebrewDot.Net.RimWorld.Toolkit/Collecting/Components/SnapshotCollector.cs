using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Collecting.Components
{
    /// <summary>
    /// Collector that listens for new snapshots being taken by the <see cref="ISnapshotManager"/> and pushes all items of type <typeparamref name="T"/> from the snapshot to the underlying collector, using the provided function to determine which items to push.
    /// </summary>
    /// <typeparam name="T">The type of items to collect from the snapshot.</typeparam>
    public class SnapshotCollector<T> : Collector<T>, IHook<OnSnapshotTakenTrigger> where T : class
    {
        // Fields
        private readonly ISnapshotManager _snapshotManager;
        private readonly IHookManager _hookmanager;
        private readonly Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> _getThingsToPush;
        private readonly Func<IReadOnlyDatabase, IDatabaseObject> _getDataInfo;
        private bool _static;

        // State
        private int _lastVersion = -1;
        private RaiseCooperativeWork _lastWork;

        // Properties
        /// <summary>
        /// The version of the last snapshot that was loaded. This is used to determine if a new snapshot has been taken when the <see cref="OnTrigger"/> method is called, and can also be used by external code to track which version of the snapshot the collector is currently using. The version is determined by the function provided in the constructor, which defaults to using the <see cref="IReadOnlyDatabase.Version"/> property of the snapshot.
        /// </summary>
        public int Version => _lastVersion;

        private static readonly Dictionary<string, object> CompareContext = new Dictionary<string, object>
        {
            { Comparator.CompareStringToReferenceKey, (Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference>)((condition, ctx, str) => new ReferenceDef()
                {
                    Type = IndexedReferenceType.DefaultTypeName,
                    Value = str
                })
            }
        };

        // Constructor
        /// <inheritdoc cref="SnapshotCollector{T}"/>
        /// <param name="collector">The underlying collector to which items will be pushed.</param>
        /// <param name="hookmanager">The hook manager used to listen for snapshot events.</param>
        /// <param name="getVersion">Returns the version for tracking changes</param>
        /// <param name="getThingsToPush">A function that determines which items to push from the snapshot. If null, all items of type <typeparamref name="T"/> will be pushed.</param>
        /// <param name="isStatic">If true, the collector will only collect items from the first snapshot it receives and will ignore subsequent snapshots. If false, the collector will continue to collect items from new snapshots as they are taken.</param>
        public SnapshotCollector(
            ICollectionDef definition,
            ISnapshotManager snapshotManager,
            IHookManager hookmanager,
            Func<IReadOnlyDatabase, IDatabaseObject> getVersion = null,
            Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> getThingsToPush = null,
            bool isStatic = false) : base(definition)
        {
            _hookmanager = Guard.NotNull(hookmanager, nameof(hookmanager));
            _snapshotManager = Guard.NotNull(snapshotManager, nameof(snapshotManager));
            _getThingsToPush = getThingsToPush ?? GetThingsToPush;
            _getDataInfo = getVersion ?? (db => db);
            _static = isStatic;
        }

        /// <inheritdoc/>
        public object Owner => this;
        /// <inheritdoc/>
        public bool Once => _static;
        /// <inheritdoc/>
        public byte Priority => byte.MaxValue;

        /// <inheritdoc/>
        public bool OnTrigger(OnSnapshotTakenTrigger arg)
        {
            var snapshot = Guard.NotNull(arg?.Snapshot, nameof(arg.Snapshot));
            var newData = _getDataInfo(snapshot);
            if (newData is null)
            {
                if(Logging.IsVerboseEnabled) Logging.LogVerbose($"SnapshotCollector<{typeof(T).Name}> received snapshot taken trigger, but the provided getDataInfo function returned null for snapshot {snapshot.Version}, skipping");
                return false;
            }
            if (newData.Version == _lastVersion)
            {
                if (Logging.IsVerboseEnabled) Logging.LogVerbose($"SnapshotCollector<{typeof(T).Name}> received snapshot taken trigger for snapshot {newData.Version}, but version {_lastVersion} has already been processed, skipping");
                return false;
            }

            var context = new WorkContext();

            if (_lastWork != null && !_lastWork.IsFinished)
            {
                if(!arg.IsForced) Logging.LogWarning($"SnapshotCollector<{typeof(T).Name}> received snapshot taken trigger for snapshot {newData.Version}, but a previous work is still running, can't keep up, cancelling");
                _lastWork.Cancel();
                _lastVersion = -1;
            }
            _lastWork = null;
            if (Once || arg.IsForced)
            {
                _lastVersion = -1;
                context.NoInterval();
                context.snapshot = snapshot;
                context.data = newData;
                LoadFrom(context).ExecuteEnumerable();
            }
            else
            {
                context.snapshot = snapshot;
                context.data = newData;
                var work = RaiseCooperativeWork.From<WorkContext>(() => LoadFrom(context).GetEnumerator(), context);

                bool accepted = _hookmanager.Trigger(work);
                if (!accepted)
                {
                    context.NoInterval();
                    LoadFrom(context).ExecuteEnumerable();
                }
                else
                {
                    _lastWork = work;
                }
            }

            return true;
        }
        /// <summary>
        /// Pushes all items of type <typeparamref name="T"/> from the provided snapshot to the underlying collector, using the provided function to determine which items to push. This is called automatically when a new snapshot is taken, but can also be called manually to load from an existing snapshot. If the provided snapshot is null, this method will do nothing.
        /// </summary>
        /// <param name="snapshot">The snapshot from which to load items.</param>
        private IEnumerable LoadFrom(WorkContext workContext)
        {
            var snapshot = Guard.NotNull(workContext?.snapshot, nameof(workContext.snapshot));
            var data = workContext?.data;
            if (data is null) yield break;
            workContext.CheckInterval = 8;
            var counter = 0;
            var total = 0;
            var fromChanges = data.TrackingChanges && data.Version - 1 == _lastVersion && data.Version > 0;
            if (Logging.IsVerboseEnabled) Logging.LogVerbose($"SnapshotCollector<{typeof(T).Name}> loading {_lastVersion} => {data.Version} (fromChanges={fromChanges})");
            var context = CompareContext;
            if (fromChanges)
            {
                IEnumerable<IIndexed<T>> changedItems = data.Changed?.OfType<IIndexed<T>>().ToArray() ?? Array.Empty<IIndexed<T>>();
                var deletedItems = data.Deleted?.OfType<IIndexed<T>>().ToArray() ?? Array.Empty<IIndexed<T>>();
                var deleted = 0;
                foreach (var (thing, collected) in _comparer.Matches(Definition, changedItems, _collections, context))
                {
                    workContext.LogWork();
                    if (HandleMatch(thing.Value, collected))
                    {
                        counter++;
                    }
                    total++;
                    if (workContext.WaitForNextTick)
                    {
                        yield return null;
                    }
                }

                for(int i = 0; i < deletedItems.Length; i++)
                {
                    var removed = deletedItems[i];
                    total++;
                    workContext.LogWork();
                    if (Remove(removed.Value))
                    {
                        deleted++;
                    }
                    if (workContext.WaitForNextTick)
                    {
                        yield return null;
                    }
                }
                if (Logging.IsVerboseEnabled) Logging.LogVerbose($"SnapshotCollector<{typeof(T).Name}> loaded {counter}/{total} changed items and {deleted} deleted items from snapshot {_lastVersion} changes");
            }
            else
            {
                Clear();
                var thingsToPush = _getThingsToPush(snapshot);
                if (thingsToPush is null)
                {
                    _lastVersion = data.Version;
                    yield break;
                }
                foreach (var (thing, collected) in _comparer.Matches(Definition, thingsToPush, _collections, context))
                {
                    workContext.LogWork();
                    if (HandleMatch(thing.Value, collected))
                    {
                        counter++;
                    }
                    total++;
                    if (workContext.WaitForNextTick)
                    {
                        yield return null;
                    }
                }
                if (Logging.IsVerboseEnabled) Logging.LogVerbose($"SnapshotCollector<{typeof(T).Name}> loaded {counter}/{total} items from snapshot {_lastVersion}");
            }
            _lastVersion = data.Version;
        }

        /// <inheritdoc/>
        public override void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections)
        {
            base.StartCollecting(comparer, collections);
            _hookmanager.RegisterHook(this);
            Autodex();
            var currentSnapshot = _snapshotManager.Database;
            if (currentSnapshot != null)
            {
                OnTrigger(new OnSnapshotTakenTrigger(currentSnapshot, true));
            }
        }
        /// <inheritdoc/>
        public override void StopCollecting()
        {
            base.StopCollecting();

            _hookmanager.UnregisterHook(this);
            _lastWork?.Cancel();
            _lastWork = null;
        }

        private void Autodex()
        {
            foreach (var property in FindProperties(Definition))
            {
                Toolkit.Indexing.Indexers.ByPath(typeof(T), property);
            }
        }

        private IEnumerable<string> FindProperties(ICollectionDef collectionDef)
        {
            collectionDef = Guard.NotNull(collectionDef, nameof(collectionDef));

            if (collectionDef.Conditions is not null)
            {
                foreach (var condition in collectionDef.Conditions)
                {
                    if(condition.Compare is string propertyName)
                    {
                        yield return propertyName;
                    }
                    else if (condition.Compare is IReference reference)
                    {
                        if(IndexedReferenceType.DefaultTypeName == reference.Type && reference.Value is string indexedPropertyName)
                        {
                            yield return indexedPropertyName;
                        }
                    }
                }
            }

            if(collectionDef.Inclusions is not null)
            {
                foreach (var inclusion in collectionDef.Inclusions)
                {
                    if (_collections.TryGetValue(inclusion.Name, out var includedCollection))
                    {
                        foreach (var property in FindProperties(includedCollection))
                        {
                            yield return property;
                        }
                    }
                }
            }

            if(collectionDef.Exclusions is not null)
            {
                foreach (var exclusion in collectionDef.Exclusions)
                {
                    if (_collections.TryGetValue(exclusion.Name, out var excludedCollection))
                    {
                        foreach (var property in FindProperties(excludedCollection))
                        {
                            yield return property;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        private IEnumerable<IIndexed<T>> GetThingsToPush(IReadOnlyDatabase snapshot)
        {
            var tables = snapshot.GetTables<T>();
            foreach (var table in tables)
            {
                foreach (var item in table.GetSnapshot())
                {
                    yield return item;
                }
            }
        }
        private class WorkContext : CooperativeWorkContext
        {
            internal IReadOnlyDatabase snapshot;
            internal IDatabaseObject data;
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
        /// <typeparam name="T">The type of items to collect from the snapshot.</typeparam>
        /// <typeparam name="TReturn">The type of the collection builder.</typeparam>
        /// <param name="builder">The collection builder.</param>
        /// <param name="getDataInfo">A function to get the data info of the snapshot.</param>
        /// <param name="getThingsToPush">A function to get the items to push from the snapshot.</param>
        /// <param name="isStatic">If true, the collector will only collect items from the first snapshot it receives and will ignore subsequent snapshots. If false, the collector will continue to collect items from new snapshots as they are taken.</param>
        /// <returns>The collection builder.</returns>
        public static TReturn CollectFromSnapshot<TReturn, T>(this ICollectionBuilder<TReturn> builder, Func<IReadOnlyDatabase, IDatabaseObject> getDataInfo = null, Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> getThingsToPush = null, bool isStatic = false)
        where TReturn : ICollectionBuilder<TReturn>
        where T : class
        => Guard.NotNull(builder, nameof(builder)).CollectWith(collectionDef => new SnapshotCollector<T>(collectionDef, Toolkit.Indexing.Manager, Toolkit.Hooks.Manager, getDataInfo, getThingsToPush, isStatic));
        /// <summary>
        /// Collects items from the current snapshot of the database, and continues to collect from new snapshots as they are taken. The provided function is used to determine which items to collect from each snapshot. If the function is null, all items of type <typeparamref name="T"/> will be collected from each snapshot.
        /// </summary>
        /// <typeparam name="T">The type of items to collect from the snapshot.</typeparam>
        /// <typeparam name="TReturn">The type of the collection builder.</typeparam>
        /// <param name="builder">The collection builder.</param>
        /// <param name="tableName">The name of the table to get the data info of the snapshot.</param>
        /// <param name="isStatic">If true, the collector will only collect items from the first snapshot it receives and will ignore subsequent snapshots. If false, the collector will continue to collect items from new snapshots as they are taken.</param>
        /// <returns>The collection builder.</returns>
        public static TReturn CollectFromSnapshot<TReturn, T>(this ICollectionBuilder<TReturn> builder, string tableName, bool isStatic = false)
            where TReturn : ICollectionBuilder<TReturn>
            where T : class
        {
            Guard.NotNull(builder, nameof(builder));
            Guard.NotNullOrEmpty(tableName, nameof(tableName));
            return builder.CollectWith(collectionDef => new SnapshotCollector<T>(collectionDef, Toolkit.Indexing.Manager, Toolkit.Hooks.Manager, snapshot => snapshot.GetTable<T>(tableName), snapshot =>
            {
                var table = snapshot.GetTable<T>(tableName);
                if (table is null)
                {
                    return Array.Empty<IIndexed<T>>();
                }
                return table.GetSnapshot();
            }, isStatic));
        }
    }
}
