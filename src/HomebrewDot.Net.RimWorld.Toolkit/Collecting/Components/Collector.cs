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
        /// <summary>
        /// The current set of collected objects.
        /// </summary>
        protected readonly HashSet<T> _collected = new HashSet<T>();
        private readonly ICollectionDef _definition;
        /// <summary>
        /// A dictionary of collection definitions keyed by their names. Used to resolve references between collections.
        /// </summary>
        protected IReadOnlyDictionary<string, ICollectionDef> _collections = null;
        /// <summary>
        /// The comparator used to determine if objects match the collection definition.
        /// </summary>
        protected ICollectionComparator _comparer = null;
        /// <summary>
        /// Raised when an object is collected. The event handler receives the collected object as a parameter.
        /// </summary>
        protected event Action<T> _onCollected;
        /// <summary>
        /// Raised when an object is removed from the collection. The event handler receives the removed object as a parameter.
        /// </summary>
        protected event Action<T> _onRemoved;
        /// <summary>
        /// Raised when the collection is cleared. The event handler receives a read-only collection of the objects that were cleared as a parameter.
        /// </summary>
        protected event Action<IReadOnlyCollection<T>> _onClear;

        // Properties
        /// <inheritdoc/>
        public event Action<T> OnCollected
        {
            add
            {
                _onCollected += value;
            }
            remove
            {
                _onCollected -= value;
            }
        }
        /// <inheritdoc/>
        public event Action<T> OnRemoved
        {
            add
            {
                _onRemoved += value;
            }
            remove
            {
                _onRemoved -= value;
            }
        }
        /// <inheritdoc/>
        public event Action<IReadOnlyCollection<T>> OnClear
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
            return _comparer.Matches(_definition, obj, _collections, context);
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
            var matches = _comparer.Matches(_definition, obj, _collections, context);
            return HandleMatch(obj, matches);
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
        /// <summary>
        /// Handles the result of a match on <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object that was checked</param>
        /// <param name="matches">The result of the match</param>
        /// <returns>True if added, otherwise false</returns>
        protected bool HandleMatch(T obj, bool matches)
        {
            if (matches)
            {
                if (_collected.Add(obj))
                {
                    _onCollected?.Invoke(obj);
                    return true;
                }
            }
            else
            {
                Remove(obj);
            }
            return false;
        }
        /// <inheritdoc/>
        public virtual bool Remove(T obj)
        {
            if (obj == null)
            {
                return false;
            }
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
        /// <inheritdoc/>
        public bool Contains(T obj)
        {
            return obj != null && _collected.Contains(obj);
        }
        /// <inheritdoc/>
        public IReadOnlyCollection<T> GetAll()
        {
            T[] array;
            array = new T[_collected.Count];
            _collected.CopyTo(array);
            return array;
        }
        /// <inheritdoc/>
        public virtual void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections)
        {
            _collections = collections;
            _comparer = comparer;
            _collected.Clear();
        }
        /// <inheritdoc/>
        public virtual void StopCollecting()
        {
            _collections = null;
            _comparer = null;
            _collected.Clear();
        }
        /// <inheritdoc/>
        public void Clear()
        {
            _onClear?.Invoke(_collected);
            _collected.Clear();
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
