using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Indexing;

namespace HomebrewDot.Net.Rimworld.Indexing.Models
{
    /// <summary>
    /// An implementation of <see cref="IIndexer"/> that allows for tracking changes in specified metadata values. This indexer can be configured to watch for changes in specific metadata keys, and will update the indexed data accordingly when changes are detected. It also provides a fluent builder interface for defining the metadata keys and their corresponding value extraction functions.
    /// </summary>
    /// <typeparam name="T">The type of the objects being indexed.</typeparam>
    public class TrackedIndexer<T> : IIndexer<T>, IIndexerBuilder<T>, IChangeTracker<T>, IDisposable where T : class
    {
        // Fields
        private readonly HashSet<IndexMetadataKey> _includes = new HashSet<IndexMetadataKey>();
        private readonly Dictionary<IndexMetadataKey, IIndexerBuilderDelegates<T, object>.SetCondition> _watchers = new Dictionary<IndexMetadataKey, IIndexerBuilderDelegates<T, object>.SetCondition>();
        private readonly Dictionary<IndexMetadataKey, IIndexerBuilderDelegates<T, object>.SetGetValue> _getters = new Dictionary<IndexMetadataKey, IIndexerBuilderDelegates<T, object>.SetGetValue>();
        private readonly HashSet<IIndexerBuilderDelegates<T, object>.SetCondition> _conditions = new HashSet<IIndexerBuilderDelegates<T, object>.SetCondition>();

        // Properties
        /// <summary>
        /// Indicates whether this indexer is tracking any changes.
        /// </summary>
        public bool WatchesChanges => _watchers.Count > 0;

        /// <inheritdoc/>
        public bool HasChanged(T current, IIndexed<T> indexed, ref IndexMetadata metadata)
        {
            if (_watchers.Count == 0) return false;
            var anyChanged = false;
            foreach (var watcher in _watchers)
            {
                var result = watcher.Value(current, indexed, ref metadata);
                if (result)
                {
                    anyChanged = true;
                }
            }
            if (!anyChanged && _conditions.Count > 0) { 
                foreach (var condition in _conditions)
                {
                    if(condition(current, indexed, ref metadata))
                    {
                        anyChanged = true;
                    }
                }
            }
            return anyChanged;
        }
        /// <inheritdoc/>
        public void OnUpserting(IWriteableIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            foreach (var include in _includes)
            {
                metadata.PersistKey(include);
            }

            if (_getters.Count == 0) return;
            bool matchesConditions = true;

            foreach (var condition in _conditions)
            {
                if (!condition(indexed.Value, indexed, ref metadata))
                {
                    matchesConditions = false;
                    break;
                }
            }

            if (matchesConditions)
            {
                foreach (var getter in _getters)
                {
                    getter.Value(indexed.Value, indexed, ref metadata);
                }
            }
        }
        /// <inheritdoc/>
        public void OnUpserted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
        }
        /// <inheritdoc/>
        public void OnDeleting(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
        }
        /// <inheritdoc/>
        public void OnDeleted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
        }
        /// <inheritdoc/>
        public void Initialize()
        {
            if (WatchesChanges)
            {
                Toolkit.Indexing.ConfigureManager += Configure;
            }
        }

        private void Configure(ISnapshotManagerConfigurator configurator)
        {
            configurator = Guard.NotNull(configurator, nameof(configurator));

            if (WatchesChanges)
            {
                configurator = configurator.WithChangeTracker(this);
            }
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Toolkit.Indexing.ConfigureManager -= Configure;
        }
        /// <inheritdoc/>
        IIndexerBuilder<T> IIndexerBuilder<T>.Set<TValue>(IndexMetadataKey metadataKey, Func<T, TValue> valueFunc, bool watchForChanges)
        {
            metadataKey = Guard.NotNull(metadataKey, nameof(metadataKey));
            valueFunc = Guard.NotNull(valueFunc, nameof(valueFunc));
            
            if(watchForChanges)
            {
                _watchers[metadataKey] = (T v, IIndexed<T> i, ref IndexMetadata m) =>
                {
                    var currentValue = valueFunc(v);
                    if(i is null)
                    {
                        // Insert so always set
                        m.Set<TValue>(metadataKey, currentValue, true);
                        return true;
                    }
                    if (i.Metadata.TryGetValue(metadataKey.Name, out var previousValue))
                    {
                        if (!Equals(currentValue, previousValue))
                        {
                            m.Set<TValue>(metadataKey, currentValue, true);
                            return true;
                        }
                    }
                    else if (currentValue is not null)
                    {
                        // If the metadata key doesn't exist in the previous index, consider it a change
                        m.Set<TValue>(metadataKey, currentValue, true);
                        return true;
                    }
                    return false;
                };
            }
            else
            {
                _getters[metadataKey] = (T v, IIndexed<T> i, ref IndexMetadata m) =>
                {
                    var currentValue = valueFunc(i.Value);
                    m.Set(metadataKey, currentValue, true);
                    return null;
                };
            }
            return this;
        }
        /// <inheritdoc/>
        IIndexerBuilder<T> IIndexerBuilder<T>.Requires<TValue>(IndexMetadataKey metadataKey, Func<T, TValue> valueFunc)
        {
            metadataKey = Guard.NotNull(metadataKey, nameof(metadataKey));
            valueFunc = Guard.NotNull(valueFunc, nameof(valueFunc));

            _watchers[metadataKey] = (T v, IIndexed<T> i, ref IndexMetadata m) =>
            {
                var currentValue = valueFunc(v);
                if (i is null)
                {
                    // Insert so always set
                    m.Set<TValue>(metadataKey, currentValue, true);
                    return true;
                }
                if (i.Metadata.TryGetValue(metadataKey.Name, out var previousValue))
                {
                    if (!Equals(currentValue, previousValue))
                    {
                        m.Set<TValue>(metadataKey, currentValue, true);
                        return true;
                    }
                }
                else if (currentValue is not null)
                {
                    // If the metadata key doesn't exist in the previous index, consider it a change
                    m.Set<TValue>(metadataKey, currentValue, true);
                    return true;
                }
                return false;
            };
            return this;
        }
        /// <inheritdoc/>
        IIndexerBuilder<T> IIndexerBuilder<T>.When(IIndexerBuilderDelegates<T, object>.SetCondition condition)
        {
            condition = Guard.NotNull(condition, nameof(condition));
            _conditions.Add(condition);
            return this;
        }
        /// <inheritdoc/>
        IIndexerBuilder<T> IIndexerBuilder<T>.Set<TValue>(IndexMetadataKey metadataKey, IIndexerBuilderDelegates<T, TValue>.SetGetValue valueFunc, bool watchForChanges)
        {
            metadataKey = Guard.NotNull(metadataKey, nameof(metadataKey));
            valueFunc = Guard.NotNull(valueFunc, nameof(valueFunc));

            if (watchForChanges)
            {
                _watchers[metadataKey] = (T v, IIndexed<T> i, ref IndexMetadata m) =>
                {
                    var currentValue = valueFunc(v, i, ref m);
                    if (i is null)
                    {
                        // Insert so always set
                        m.Set<TValue>(metadataKey, currentValue, true);
                        return true;
                    }
                    if (i.Metadata.TryGetValue(metadataKey.Name, out var previousValue))
                    {
                        if (!Equals(currentValue, previousValue))
                        {
                            m.Set<TValue>(metadataKey, currentValue, true);
                            return true;
                        }
                    }
                    else if (currentValue is not null)
                    {
                        // If the metadata key doesn't exist in the previous index, consider it a change
                        m.Set<TValue>(metadataKey, currentValue, true);
                        return true;
                    }
                    return false;
                };
            }
            else
            {
                _getters[metadataKey] = (T v, IIndexed<T> i, ref IndexMetadata m) => valueFunc(v, i, ref m);
            }
            return this;
        }
        /// <inheritdoc/>
        IIndexerBuilder<T> IIndexerBuilder<T>.Requires<TValue>(IndexMetadataKey metadataKey, IIndexerBuilderDelegates<T, TValue>.SetGetValue valueFunc)
        {
            metadataKey = Guard.NotNull(metadataKey, nameof(metadataKey));
            valueFunc = Guard.NotNull(valueFunc, nameof(valueFunc));

            _watchers[metadataKey] = (T v, IIndexed<T> i, ref IndexMetadata m) =>
            {
                var currentValue = valueFunc(v, i, ref m);
                if (i is null)
                {
                    // Insert so always set
                    m.Set<TValue>(metadataKey, currentValue, true);
                    return true;
                }
                if (i.Metadata.TryGetValue(metadataKey.Name, out var previousValue))
                {
                    if (!Equals(currentValue, previousValue))
                    {
                        m.Set<TValue>(metadataKey, currentValue, true);
                        return true;
                    }
                }
                else if (currentValue is not null)
                {
                    // If the metadata key doesn't exist in the previous index, consider it a change
                    m.Set<TValue>(metadataKey, currentValue, true);
                    return true;
                }
                return false;
            };
            return this;
        }
        /// <inheritdoc/>
        IIndexerBuilder<T> IIndexerBuilder<T>.Include<TValue>(IndexMetadataKey metadataKey, bool watchForChanges)
        {
            metadataKey = Guard.NotNull(metadataKey, nameof(metadataKey));

            _includes.Add(metadataKey);

            if (watchForChanges)
            {
                // The value is supplied through the insert metadata (e.g. by a gatherer). TValue is
                // the metadata value type, so read it back typed to detect changes.
                _watchers[metadataKey] = (T v, IIndexed<T> i, ref IndexMetadata m) =>
                {
                    if (i is null)
                    {
                        // Insert: persist the incoming value.
                        m.PersistKey(metadataKey);
                        return true;
                    }
                    if (m.TryGetValue<TValue>(metadataKey, out var currentValue))
                    {
                        if (i.Metadata.TryGetValue(metadataKey.Name, out var previousValue))
                        {
                            if (!Equals(currentValue, previousValue))
                            {
                                m.PersistKey(metadataKey);
                                return true;
                            }
                        }
                        else
                        {
                            // Key absent from the previous index but present in the incoming metadata.
                            m.PersistKey(metadataKey);
                            return true;
                        }
                    }
                    return false;
                };
            }
            return this;
        }
    }
}
