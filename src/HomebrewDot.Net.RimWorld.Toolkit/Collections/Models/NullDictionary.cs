using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Collections.Models
{
    /// <summary>
    /// Empty implementation of <see cref="IDictionary{TKey, TValue}"/> where all calls will be ignored and return default values.
    /// Used when a dictionary instance is required but no data is available, in that case the singleton instance can be used to avoid unnecessary allocations.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class NullDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// The singleton instance of <see cref="NullDictionary{TKey, TValue}"/> that can be used whenever an empty dictionary is needed.
        /// </summary>
        public static NullDictionary<TKey, TValue> Instance { get; } = new NullDictionary<TKey, TValue>();
        private NullDictionary() { }
        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get {
                _ = key;
                return default;
            }
            set { _ = value; }
        }

        /// <inheritdoc/>
        public ICollection<TKey> Keys => Array.Empty<TKey>();

        /// <inheritdoc/>
        public ICollection<TValue> Values => Array.Empty<TValue>();
        /// <inheritdoc/>
        public int Count => 0;

        /// <inheritdoc/>
        public bool IsReadOnly => true;
        /// <inheritdoc/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Array.Empty<TKey>();
        /// <inheritdoc/>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Array.Empty<TValue>();

        /// <inheritdoc/>
        public void Add(TKey key, TValue value)
        { }

        /// <inheritdoc/>
        public void Add(KeyValuePair<TKey, TValue> item)
        { }

        /// <inheritdoc/>
        public void Clear()
        { }

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        => false;

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        => false;

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        { }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        => false;

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        => false;

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;
            return false;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
