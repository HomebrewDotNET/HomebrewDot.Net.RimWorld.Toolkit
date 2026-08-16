using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Collecting.Triggers;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Indexing;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Collecting.Components
{
    /// <summary>
    /// Collector that monitors another collection and collects objects of type T based on the state of the monitored collection and adds more filtering based on additional conditions.
    /// Essentially acts as a sub collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MonitorCollector<T> : Collector<T>, IHook<OnCollectionsChanged>, IDisposable where T : class
    {
        // Statics
        private static ICollectionDef CreateFullCollectionDef(ICollectionDef collectionDef, string monitoredCollectionName)
        {
            collectionDef = Guard.NotNull(collectionDef, nameof(collectionDef));
            monitoredCollectionName = Guard.NotNullOrEmpty(monitoredCollectionName, nameof(monitoredCollectionName));

            var existingExclusions = collectionDef.Exclusions ?? Array.Empty<ICollectionConditionDef>();
            var monitoredCollectionExclusion = new CollectionConditionDef() { Name = monitoredCollectionName, Inverted = true, IsOr = true };
            var newExclusions = new ICollectionConditionDef[existingExclusions.Count + 1];
            newExclusions[0] = monitoredCollectionExclusion;
            var counter = 1;
            foreach (var exclusion in existingExclusions)
            {
                newExclusions[counter] = exclusion;
                counter++;
            }

            return new StaticCollectionDef(new CollectionDef(collectionDef)
            {
                Exclusions = newExclusions.Select(e => new CollectionConditionDef(e)).ToArray(),
            });
        }

        // Fields
        private readonly string _monitoredCollectionName;
        private readonly ICollectionDef _collectionDef;

        // State
        private ICollector<T> _monitored;

        // Properties
        /// <inheritdoc/>
        public object Owner => this;
        /// <inheritdoc/>
        public bool Once => false;
        /// <inheritdoc/>
        public byte Priority => 100;
        /// <inheritdoc/>
        public bool GameScoped => false;

        /// <inheritdoc cref="MonitorCollector{T}"/>
        /// <param name="collectionDef">The definition of conditions in addition to being in collection <paramref name="monitoredCollectionName"/></param>
        /// <param name="monitoredCollectionName">The name of the collection to monitor.</param>
        public MonitorCollector(ICollectionDef collectionDef, string monitoredCollectionName) : base(CreateFullCollectionDef(collectionDef, monitoredCollectionName))
        {
            _monitoredCollectionName = Guard.NotNullOrEmpty(monitoredCollectionName, nameof(monitoredCollectionName));
            _collectionDef = Guard.NotNull(collectionDef, nameof(collectionDef));
            Toolkit.Hooks.Manager.RegisterHook<OnCollectionsChanged>(this);
            SubstribeToMonitoredCollection();
        }

        /// <inheritdoc/>
        public bool OnTrigger(OnCollectionsChanged arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            if(arg.Name != _monitoredCollectionName)
            {
                return false;
            }
            UnsubscribeFromMonitoredCollection();
            if(arg.Added)
            {
                SubstribeToMonitoredCollection();
            }
             return true;
        }

        private void SubstribeToMonitoredCollection()
        {
            var collectors = Toolkit.Collecting.GetAllCollectors();
            if(!collectors.TryGetValue(_monitoredCollectionName, out var monitored) || monitored is not ICollector<T> typedMonitored)
            {
                Logging.LogWarning($"MonitorCollector for collection '{Definition}' is trying to monitor collection '{_monitoredCollectionName}' but it does not exist. MonitorCollector will not collect anything until the monitored collection is added.");
                return;
            }

            _monitored = typedMonitored;
            _monitored.OnCollected += OnMonitoredCollected;
            _monitored.OnRemoved += OnMonitoredRemoved;
        }
        private void UnsubscribeFromMonitoredCollection()
        {
            if(_monitored is null)
            {
                return;
            }
            _monitored.OnCollected -= OnMonitoredCollected;
            _monitored.OnRemoved -= OnMonitoredRemoved;
            _monitored = null;
        }

        /// <inheritdoc/>
        public override void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections)
        {
            base.StartCollecting(comparer, collections);
            if (_monitored is null)
            {
                return;
            }
            // Pull in any items the monitored collection already contains so this monitor is
            // not empty until the next snapshot makes the monitored collection re-add them.
            foreach (var item in _monitored.GetAll())
            {
                OnMonitoredCollected(item);
            }
        }

        private void OnMonitoredCollected(T obj)
        {
            if (obj is null)
            {
                return;
            }
            if (_comparer == null || _collections == null)
            {
                // Not started yet (or stopped); ignore until StartCollecting wires up the comparator.
                return;
            }
            if(_comparer.Matches(_collectionDef, obj, _collections, NullDictionary<string, object>.Instance))
            {
                _ = _collected.Add(obj);
            }
        }
        private void OnMonitoredRemoved(T obj)
        {
            if (obj is null)
            {
                return;
            }
            _ = _collected.Remove(obj);
        }


        /// <inheritdoc/>
        public void Dispose()
        {
            Toolkit.Hooks.Manager.UnregisterAllBy<OnCollectionsChanged>(this);
        }
    }

    /// <summary>
    /// Contains exntesion method related to <see cref="SnapshotCollector{T}"/>
    /// </summary>
    public static class MonitorCollectorExtensions
    {

        public static TReturn CollectFromCollection<TReturn, T>(this ICollectionBuilder<TReturn> builder, string monitoredCollectionName)
            where TReturn : ICollectionBuilder<TReturn>
            where T : class
            => Guard.NotNull(builder, nameof(builder)).CollectWith(collectionDef => new MonitorCollector<T>(collectionDef, monitoredCollectionName));
    }
}
