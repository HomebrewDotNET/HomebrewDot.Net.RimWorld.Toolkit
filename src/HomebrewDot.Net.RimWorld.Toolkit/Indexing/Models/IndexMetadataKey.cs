using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Indexing.Models
{
    /// <summary>
    /// The keys used for lookup in <see cref="IndexMetadata"/>. Provides better performance over <see cref="string"/>.
    /// Should be stored by ref for faster lookups.
    /// </summary>
    public class IndexMetadataKey
    {
        // Statics
        /// <summary>
        /// The global cache for the metadata keys.
        /// </summary>
        protected readonly static Dictionary<string, IndexMetadataKey> _cache = new Dictionary<string, IndexMetadataKey>(StringComparer.OrdinalIgnoreCase);

        // Properties
        /// <summary>
        /// The name of the key.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc cref="IndexMetadataKey"/>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        internal IndexMetadataKey(string name)
        {
            Name = Guard.NotNullOrEmpty(name, nameof(name));
        }

        /// <summary>
        /// Creates/returns the metadata key for <paramref name="name"/>.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <returns>The metadata key for <paramref name="name"/></returns>
        public static IndexMetadataKey Get(string name)
        {
            if (!_cache.TryGetValue(name, out IndexMetadataKey key))
            {
                if (!_cache.TryGetValue(name, out key))
                {
                    key = new IndexMetadataKey(name);
                    _cache.Add(name, key);
                }
            }
            return key;
        }
    }

    /// <summary>
    /// The keys used for lookup in <see cref="IndexMetadata"/>. Provides better performance over <see cref="string"/>.
    /// Should be stored by ref for faster lookups.
    /// Typed version for selecting overloads in <see cref="IndexMetadata"/>.
    /// </summary>
    public class IndexMetadataKey<T> : IndexMetadataKey
    {
        private IndexMetadataKey(string name) : base(name)
        {
        }

        /// <summary>
        /// Creates/returns the metadata key for <paramref name="name"/>.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <returns>The metadata key for <paramref name="name"/></returns>
        public static new IndexMetadataKey<T> Get(string name)
        {
            if (!_cache.TryGetValue(name, out IndexMetadataKey key))
            {
                if (!_cache.TryGetValue(name, out key))
                {
                    key = new IndexMetadataKey<T>(name);
                    _cache.Add(name, key);
                }
            }
            if (key is not IndexMetadataKey<T> typedKey) throw new InvalidOperationException($"Metadata key {name} is defined but not of expected type {typeof(T)}. The same key might be used accross multiple types"); 
            return typedKey;
        }
    }
}
