using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine.Pool;
using static HomebrewDot.Net.Rimworld.Indexing.Components.Database;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Indexing.Models
{
    /// <summary>
    /// A bag of metadata that can be used to store extra information on <see cref="IIndexed{T}"/>
    /// </summary>
    public struct IndexMetadata
    {
        // State
        private HashSet<IndexMetadataKey> _persistentKeys;
        private Dictionary<IndexMetadataKey, object> _objMetadata;
        private Dictionary<IndexMetadataKey, int> _intMetadata;
        private Dictionary<IndexMetadataKey, float> _floatMetadata;
        private Dictionary<IndexMetadataKey, bool> _boolMetadata;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Dictionary<IndexMetadataKey, T> CreateMetadata<T>()
        {
            try
            {
                return DictionaryPool<IndexMetadataKey, T>.Get();
            }
            catch
            {
                return new Dictionary<IndexMetadataKey, T>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object GetMetadata<T>(bool create)
        {
            switch (typeof(T))
            {
                case Type t when t == typeof(int):
                    if (_intMetadata is null && create) _intMetadata = CreateMetadata<int>();
                    return _intMetadata;
                case Type t when t == typeof(bool):
                    if (_boolMetadata is null && create) _boolMetadata = CreateMetadata<bool>();
                    return _boolMetadata;
                case Type t when t == typeof(float):
                    if (_floatMetadata is null && create) _floatMetadata = CreateMetadata<float>();
                    return _floatMetadata;
                default:
                    if (_objMetadata is null && create) _objMetadata = CreateMetadata<object>();
                    return _objMetadata;
            }
        }

        /// <summary>
        /// Checks if the current metadata contains <paramref name="key"/> of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the metadata</typeparam>
        /// <param name="key">The key of the metadata</param>
        /// <returns>True when present, otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey<T>(IndexMetadataKey key)
        {
            key = Guard.NotNull(key, nameof(key));
            var dict = GetMetadata<T>(false);
            if (dict is Dictionary<IndexMetadataKey, T> typedDict)
            {
                return typedDict.ContainsKey(key);
            }

            if (dict is Dictionary<IndexMetadataKey, object> genericDict)
            {
                return genericDict.ContainsKey(key);
            }
            return false;
        }
        /// <inheritdoc cref="ContainsKey{T}(IndexMetadataKey)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey<T>(IndexMetadataKey<T> key)
        => ContainsKey<T>((IndexMetadataKey)key);

        /// <summary>
        /// Tries to get value <paramref name="key"/> of type <typeparamref name="T"/> if it exists.
        /// </summary>
        /// <typeparam name="T">The type of the metadata</typeparam>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The metadata value if found</param>
        /// <returns>True if the value was found, otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue<T>(IndexMetadataKey key, out T value)
        {
            key = Guard.NotNull(key, nameof(key));
            var dict = GetMetadata<T>(false);
            value = default;
            if (dict is Dictionary<IndexMetadataKey, T> typedDict)
            {
                return typedDict.TryGetValue(key, out value) ? true : false;
            }

            if (dict is Dictionary<IndexMetadataKey, object> genericDict)
            {
                if(genericDict.TryGetValue(key, out var genericValue))
                {
                    if(genericValue is T typedValue)
                    {
                        value = typedValue;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <inheritdoc cref="TryGetValue{T}(IndexMetadataKey, out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue<T>(IndexMetadataKey<T> key, out T value)
            => TryGetValue<T>((IndexMetadataKey)key, out value);

        /// <summary>
        /// Sets metadata <paramref name="key"/> if type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the metadata</typeparam>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The metadata value to set</param>
        /// <param name="persistent">Set to true to transfer the metadata key to <see cref="IIndexed{T}"/> when upserting. When false it will only remain available during the upsert chain</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(IndexMetadataKey key, T value, bool persistent = false)
        {
            key = Guard.NotNull(key, nameof(key));
            var dict = GetMetadata<T>(true);

            if (persistent)
            {
                _persistentKeys ??= new HashSet<IndexMetadataKey>();
                _ = _persistentKeys.Add(key);
            }

            if (dict is Dictionary<IndexMetadataKey, T> typedDict)
            {
                typedDict[key] = value;
                return;
            }

            if (dict is Dictionary<IndexMetadataKey, object> genericDict)
            {
                genericDict[key] = value;
                return;
            }
        }
        /// <inheritdoc cref="Set{T}(IndexMetadataKey, T, bool)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<T>(IndexMetadataKey<T> key, T value, bool persistent = false)
            => Set<T>((IndexMetadataKey)key, value, persistent);

        /// <summary>
        /// Removes metadata <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The type of the metadata</typeparam>
        /// <param name="key">The key of the metadata</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset<T>(IndexMetadataKey key)
        {
            key = Guard.NotNull(key, nameof(key));
            var dict = GetMetadata<T>(false);

            if (_persistentKeys is not null)
            {
                _ = _persistentKeys.Remove(key);
            }

            if (dict is Dictionary<IndexMetadataKey, T> typedDict)
            {
                _ = typedDict.Remove(key);
                return;
            }

            if (dict is Dictionary<IndexMetadataKey, object> genericDict)
            {
                _ = genericDict.Remove(key);
                return;
            }
        }

        /// <summary>
        /// Marks a metadata key as persistent and will be transfered during upserting.
        /// </summary>
        /// <param name="key"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PersistKey(IndexMetadataKey key)
        {
            key = Guard.NotNull(key, nameof(key));
            _persistentKeys ??= new HashSet<IndexMetadataKey>();
            _ = _persistentKeys.Add(key);
        }

        /// <summary>
        /// Sets all persistent metadata on <paramref name="indexed"/>.
        /// </summary>
        /// <typeparam name="T">The type of the indexed object</typeparam>
        /// <param name="indexed">The index object to persist the metadata on</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PersistTo<T>(IWriteableIndexed<T> indexed) where T : class
        {
            indexed = Guard.NotNull(indexed, nameof(indexed));

            if (_persistentKeys?.Count > 0)
            {
                var intDictionery = GetMetadata<int>(false);
                var floatDictionery = GetMetadata<float>(false);
                var boolDictionery = GetMetadata<bool>(false);
                var objDictionery = GetMetadata<object>(false);

                foreach (var key in _persistentKeys)
                {
                    if(TryPersist<T, int>(intDictionery, key, indexed))
                    {
                        continue;
                    }
                    if (TryPersist<T, bool>(boolDictionery, key, indexed))
                    {
                        continue;
                    }
                    if (TryPersist<T, float>(floatDictionery, key, indexed))
                    {
                        continue;
                    }
                    if (TryPersist<T, object>(objDictionery, key, indexed))
                    {
                        continue;
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryPersist<T, TValue>(object dict, IndexMetadataKey key, IWriteableIndexed<T> indexed) where T : class
        {
            if (dict is Dictionary<IndexMetadataKey, TValue> typedIntDict && typedIntDict.TryGetValue(key, out var intValue))
            {
                indexed.Set(key.Name, intValue);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Release all internal metadata back to the pool.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_intMetadata is not null)
                {
                    DictionaryPool<IndexMetadataKey, int>.Release(_intMetadata);
                    _intMetadata = null;
                }
                if (_boolMetadata is not null)
                {
                    DictionaryPool<IndexMetadataKey, bool>.Release(_boolMetadata);
                    _boolMetadata = null;
                }
                if (_floatMetadata is not null)
                {
                    DictionaryPool<IndexMetadataKey, float>.Release(_floatMetadata);
                    _floatMetadata = null;
                }
                if (_objMetadata is not null)
                {
                    DictionaryPool<IndexMetadataKey, object>.Release(_objMetadata);
                    _objMetadata = null;
                }
            }
            catch{

            }
        }
    }
}
