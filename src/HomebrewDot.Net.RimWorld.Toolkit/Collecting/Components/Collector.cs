using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Collecting.Models;
using HomebrewDot.Net.RimWorld.Generic.Models;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;

namespace HomebrewDot.Net.RimWorld.Collecting.Components
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
            if(obj == null)
            {
                return false;
            }
            if(_comparer == null || _collections == null)
            {
                return false;
            }
            var itemContext = new Dictionary<string, object>(context?.Count ?? 0);
            if(context != null)
            {
                foreach(var kvp in context)
                {
                    itemContext[kvp.Key] = kvp.Value;
                }
            }
            return _comparer.Matches(_definition, obj, _collections, itemContext);
        }
        /// <inheritdoc/>
        public bool Collect(T obj, IReadOnlyDictionary<string, object> context)
        {
            if(obj == null)
            {
                return false;
            }
            if(_comparer == null || _collections == null)
            {
                return false;
            }
            var itemContext = new Dictionary<string, object>(context?.Count ?? 0);
            if(context != null)
            {
                foreach(var kvp in context)
                {
                    itemContext[kvp.Key] = kvp.Value;
                }
            }
            if (_comparer.Matches(_definition, obj, _collections, itemContext))
            {
                lock (_lock)
                {
                    _collected.Add(obj);
                }
                return true;
            }
            return false;
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
        public void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections)
        {
            lock (_lock)
            {
                _collections = collections;
                _comparer = comparer;
                _collected.Clear();
            }
        }
        /// <inheritdoc/>
        public void StopCollecting()
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
            lock(_lock)
            {
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
