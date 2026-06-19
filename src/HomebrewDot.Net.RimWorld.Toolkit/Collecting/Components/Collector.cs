using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Generic.Models;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Collecting.Components
{
    /// <summary>
    /// A collector that collects objects of type T based on a collection definition and a collection comparator. The collector maintains a set of collected objects and provides methods to check if an object can be collected, to collect an object, to check if an object is already collected, and to retrieve all collected objects. The collector can be started and stopped, which will reset its state and allow it to be reused with different collection definitions and comparators.
    /// </summary>
    /// <typeparam name="T">The type of objects to be collected.</typeparam>
    public class Collector<T> : ICollector<T> where T : class
    {
        // Fields
        private readonly object _lock = new object();
        private readonly HashSet<T> _collected = new HashSet<T>();
        private readonly ICollectionDef _definition;
        private IReadOnlyDictionary<string, ICollectionDef> _collections = null;
        private ICollectionComparator _comparer = null;
        private event Action<T> _onCollected;
        private event Action<T> _onRemoved;
        private event Action<IReadOnlyCollection<T>> _onClear;

        // Properties
        /// <inheritdoc/>
        public event Action<T> OnCollected
        {
            add
            {
                lock (_lock)
                {
                    _onCollected += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    _onCollected -= value;
                }
            }
        }
        /// <inheritdoc/>
        public event Action<T> OnRemoved
        {
            add
            {
                lock (_lock)
                {
                    _onRemoved += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    _onRemoved -= value;
                }
            }
        }
        /// <inheritdoc/>
        public event Action<IReadOnlyCollection<T>> OnClear
        {
            add
            {
                lock (_lock)
                {
                    _onClear += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    _onClear -= value;
                }
            }
        }

        // Properties
        /// <inheritdoc/>
        public ICollectionDef Definition => _definition;
        /// <inheritdoc/>
        public int Count => _collected.Count;

        /// <inheritdoc cref="Collector{T}"/>
        /// <param name="definition"><inheritdoc cref="Definition"/></param>
        public Collector(ICollectionDef definition)
        {
            _definition = Guard.NotNull(definition, nameof(definition));
        }

        /// <inheritdoc/>
        public bool CanCollect(T obj, IReadOnlyDictionary<string, object> context)
        {
            if (obj == null)
            {
                return false;
            }
            if (_comparer == null || _collections == null)
            {
                return false;
            }
            var itemContext = new Dictionary<string, object>(context?.Count ?? 0);
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    itemContext[kvp.Key] = kvp.Value;
                }
            }
            return _comparer.Matches(_definition, obj, _collections, itemContext);
        }
        /// <inheritdoc/>
        public virtual bool Collect(T obj, IReadOnlyDictionary<string, object> context)
        {
            if (obj == null)
            {
                return false;
            }
            if (_comparer == null || _collections == null)
            {
                return false;
            }
            var itemContext = new Dictionary<string, object>(context?.Count ?? 0);
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    itemContext[kvp.Key] = kvp.Value;
                }
            }
            if (_comparer.Matches(_definition, obj, _collections, itemContext))
            {
                lock (_lock)
                {
                    if (_collected.Add(obj))
                    {
                        _onCollected?.Invoke(obj);
                    }
                }
                return true;
            }
            else if (Contains(obj))
            {
                lock (_lock)
                {
                    if (_collected.Remove(obj))
                    {
                        _onRemoved?.Invoke(obj);
                    }
                }
            }

            return false;
        }
        /// <inheritdoc/>
        public virtual IEnumerable<(T Obj, bool Collected)> Collect(IEnumerable<T> objects, IReadOnlyDictionary<string, object> context)
        {
            objects = Guard.NotNull(objects, nameof(objects));
            return _comparer.Matches(_definition, objects, _collections, context).Select(x =>
            {
                var castedObj = x.Object as T;
                HandleMatch(castedObj, x.Matches);
                return (castedObj, x.Matches);
            });
        }
        private void HandleMatch(T obj, bool matches)
        {
            if (matches)
            {
                lock (_lock)
                {
                    if (_collected.Add(obj))
                    {
                        _onCollected?.Invoke(obj);
                    }
                }
            }
            else if (Contains(obj))
            {
                lock (_lock)
                {
                    if (_collected.Remove(obj))
                    {
                        _onRemoved?.Invoke(obj);
                    }
                }
            }
        }
        /// <inheritdoc/>
        public virtual bool Remove(T obj)
        {
            if (obj == null)
            {
                return false;
            }
            lock (_lock)
            {
                if (_collected.Remove(obj))
                {
                    _onRemoved?.Invoke(obj);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        /// <inheritdoc/>
        public bool Contains(T obj)
        {
            return obj != null && _collected.Contains(obj);
        }
        /// <inheritdoc/>
        public IReadOnlyCollection<T> GetAll()
        {
            T[] array;
            lock (_lock)
            {
                array = new T[_collected.Count];
                _collected.CopyTo(array);
            }
            return array;
        }
        /// <inheritdoc/>
        public virtual void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections)
        {
            lock (_lock)
            {
                _collections = collections;
                _comparer = comparer;
                _collected.Clear();
            }
        }
        /// <inheritdoc/>
        public virtual void StopCollecting()
        {
            lock (_lock)
            {
                _collections = null;
                _comparer = null;
                _collected.Clear();
            }
        }
        /// <inheritdoc/>
        void ICollector.Clear()
        {
            lock (_lock)
            {
                _onClear?.Invoke(_collected);
                _collected.Clear();
            }
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }
        /// <inheritdoc/>
        IReadOnlyCollection<object> ICollector.GetAll()
        {
            return GetAll();
        }
    }
}
