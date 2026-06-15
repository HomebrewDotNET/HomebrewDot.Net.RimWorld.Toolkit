using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using HomebrewDot.Net.Rimworld.Extensions;

namespace HomebrewDot.Net.Rimworld.Collecting.Components
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
        private readonly Func<IReadOnlyDatabase, int> _getVersion;
        private IReadOnlyDatabase _lastSnapshot;
        private event Action<T> _onCollected;
        private event Action<T> _onRemoved;
        private event Action<IReadOnlyCollection<T>> _onClear;

        // State
        private int _lastVersion;

		// Properties
		/// <inheritdoc/>
		event Action<IIndexed<T>> ICollector<IIndexed<T>>.OnCollected
		{
			add
			{
				_collector.OnCollected += value;
			}

			remove
			{
				_collector.OnCollected -= value;
			}
		}
		/// <inheritdoc/>
		event Action<IIndexed<T>> ICollector<IIndexed<T>>.OnRemoved
		{
			add
			{
				_collector.OnRemoved += value;
			}

			remove
			{
				_collector.OnRemoved -= value;
			}
		}
		/// <inheritdoc/>
		public event Action<T> OnCollected { add { _onCollected += value; } remove { _onCollected -= value; } }
        /// <inheritdoc/>
		public event Action<T> OnRemoved { add { _onRemoved += value; } remove { _onRemoved -= value; } }
		/// <inheritdoc/>
		public event Action<IReadOnlyCollection<IIndexed<T>>> OnClear
        {
            add
            {
                _collector.OnClear += value;
            }

            remove
            {
                _collector.OnClear -= value;
            }
        }
		/// <inheritdoc/>
		event Action<IReadOnlyCollection<T>> ICollector<T>.OnClear
        {
            add
            {
                _onClear += value;
            }

            remove
            {
                _onClear -= value;
			}
        }

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
        public SnapshotCollector(ICollector<IIndexed<T>> collector, IHookManager hookmanager, Func<IReadOnlyDatabase, int> getVersion = null, Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> getThingsToPush = null)
        {
            _collector = Guard.NotNull(collector, nameof(collector));
            _collector.OnCollected += item =>
            {
                _onCollected?.Invoke(item.Value);
            };
            _collector.OnRemoved += item =>
            {
                _onRemoved?.Invoke(item.Value);
            };
            _collector.OnClear += items =>
            {
                _onClear?.Invoke(items.Select(i => i.Value).ToList());
            };
			_hookmanager = Guard.NotNull(hookmanager, nameof(hookmanager));
            _getThingsToPush = getThingsToPush ?? GetThingsToPush;
            _getVersion = getVersion ?? (snapshot => snapshot.Version);
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
		public bool Remove(T obj)
		{
			if(_lastSnapshot is null)
            {
                return false;
			}
            if(obj is null)
            {
                return false;
			}
            lock(_collector)
            {
                var indexed = _lastSnapshot.Find<T>(obj);
                if (indexed is null)
                {
                    return false;
                }
                return _collector.Remove(indexed);
			}
		}
		/// <inheritdoc/>
		public bool Remove(IIndexed<T> obj)
		{
			return _collector.Remove(obj);
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
        IEnumerable<(IIndexed<T> Obj, bool Collected)> ICollector<IIndexed<T>>.Collect(IEnumerable<IIndexed<T>> objects, IReadOnlyDictionary<string, object> context)
        {
            return _collector.Collect(objects, context);
        }
        /// <inheritdoc/>
        public IEnumerable<(T Obj, bool Collected)> Collect(IEnumerable<T> objects, IReadOnlyDictionary<string, object> context)
        {
            objects = Guard.NotNull(objects, nameof(objects));
            foreach (var obj in objects)
            {
                yield return (obj, Collect(obj, context));
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
            var newVersion = _getVersion(snapshot);
            if(newVersion == _lastVersion)
            {
                Logging.LogVerbose($"SnapshotCollector<{typeof(T).Name}> received snapshot taken trigger for snapshot {snapshot.Version}, but version {_lastVersion} has already been processed, skipping");
                return false;
            }
            _lastVersion = newVersion;
            LoadFrom(snapshot);
            return true;
        }
        /// <summary>
        /// Pushes all items of type <typeparamref name="T"/> from the provided snapshot to the underlying collector, using the provided function to determine which items to push. This is called automatically when a new snapshot is taken, but can also be called manually to load from an existing snapshot. If the provided snapshot is null, this method will do nothing.
        /// </summary>
        /// <param name="snapshot">The snapshot from which to load items.</param>
        public void LoadFrom(IReadOnlyDatabase snapshot)
        {
            snapshot = Guard.NotNull(snapshot, nameof(snapshot));
            _lastSnapshot = snapshot;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var thingsToPush = _getThingsToPush(snapshot);
            if(thingsToPush is null)
            {
                return;
            }
            var counter = 0;
            var total = 0;
            lock (_collector)
            {
                Clear();
                var context = CompareContext;
                foreach(var (thing, collected) in _collector.Collect(thingsToPush, context))
                {
                    if(collected)
                    {
                        counter++;
                    }
                    total++;
                }
            }
            Logging.LogVerbose($"SnapshotCollector<{typeof(T).Name}> loaded {counter}/{total} items from snapshot {_lastVersion} in {stopwatch.ElapsedMilliseconds}ms");
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
                currentSnapshot = Toolkit.Indexing.Manager?.Database;
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
                foreach (var item in table.Enumerate<IIndexed<T>>())
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
        /// <typeparam name="T">The type of items to collect from the snapshot.</typeparam>
        /// <typeparam name="TReturn">The type of the collection builder.</typeparam>
        /// <param name="builder">The collection builder.</param>
        /// <param name="getVersion">A function to get the version of the snapshot.</param>
        /// <param name="getThingsToPush">A function to get the items to push from the snapshot.</param>
        /// <returns>The collection builder.</returns>
        public static TReturn CollectFromSnapshot<TReturn, T>(this ICollectionBuilder<TReturn> builder, Func<IReadOnlyDatabase, int> getVersion = null, Func<IReadOnlyDatabase, IEnumerable<IIndexed<T>>> getThingsToPush = null)
            where TReturn : ICollectionBuilder<TReturn>
            where T : class
            => Guard.NotNull(builder, nameof(builder)).CollectWith(collectionDef => new SnapshotCollector<T>(new Collector<IIndexed<T>>(collectionDef), Toolkit.Hooks.Manager, getVersion, getThingsToPush));
    }
}
