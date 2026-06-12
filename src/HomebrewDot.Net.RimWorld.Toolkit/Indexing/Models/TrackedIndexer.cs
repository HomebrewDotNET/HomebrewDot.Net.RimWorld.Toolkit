using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Indexing.Models
{
    /// <summary>
    /// An implementation of <see cref="IIndexer"/> that allows for tracking changes in specified metadata values. This indexer can be configured to watch for changes in specific metadata keys, and will update the indexed data accordingly when changes are detected. It also provides a fluent builder interface for defining the metadata keys and their corresponding value extraction functions.
    /// </summary>
    /// <typeparam name="T">The type of the objects being indexed.</typeparam>
    public class TrackedIndexer<T> : IIndexer, IIndexerBuilder<T>, IChangeTracker<T>, IDisposable where T : class
    {
        // Fields
        private readonly Dictionary<string, Func<T, object>> _watchers = new Dictionary<string, Func<T, object>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<T, object>> _getters = new Dictionary<string, Func<T, object>>(StringComparer.OrdinalIgnoreCase);

        // Properties
        /// <summary>
        /// Indicates whether this indexer is tracking any changes.
        /// </summary>
        public bool WatchesChanges => _watchers.Count > 0;

        /// <inheritdoc/>
        public bool HasChanged(T current, IIndexed<T> previous, IIndexed<T> snapshot)
        {
            if (_watchers.Count == 0) return false;
            foreach (var watcher in _watchers)
            {
                var metadataKey = watcher.Key;
                var currentValue = watcher.Value(current);
                if (previous.Metadata.TryGetValue(metadataKey, out var previousValue))
                {
                    if (!Equals(currentValue, previousValue))
                    {
                        return true;
                    }
                }
                else if(currentValue is not null)
                {
                    // If the metadata key doesn't exist in the previous index, consider it a change
                    return true;
                }
            }
            return false;
        }
        /// <inheritdoc/>
        public void Index(IDatabase database, IReadOnlyDictionary<string, object> insertMetadata, IWriteableIndexed<object> indexed)
        {
            database = Guard.NotNull(database, nameof(database));
            insertMetadata = Guard.NotNull(insertMetadata, nameof(insertMetadata));
            indexed = Guard.NotNull(indexed, nameof(indexed));

            if(indexed is IIndexed<T> typedIndexed)
            {
                if (WatchesChanges)
                {
                    foreach (var watcher in _watchers)
                    {
                        var metadataKey = watcher.Key;
                        var value = watcher.Value(typedIndexed.Value);
                        if (value == null)
                        {
                            indexed.Unset(metadataKey);
                        }
                        else
                        {
                            indexed.Set(metadataKey, value);
                        }
                    }
                }

                foreach (var getter in _getters)
                {
                    var metadataKey = getter.Key;
                    var value = getter.Value(typedIndexed.Value);
                    if (value == null)
                    {
                        indexed.Unset(metadataKey);
                    }
                    else
                    {
                        indexed.Set(metadataKey, value);
                    }
                }
            }
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
        IIndexerBuilder<T> IIndexerBuilder<T>.Set(string metadataKey, Func<T, object> valueFunc, bool watchForChanges)
        {
            metadataKey = Guard.NotNullOrWhitespace(metadataKey, nameof(metadataKey));
            valueFunc = Guard.NotNull(valueFunc, nameof(valueFunc));
            
            _getters[metadataKey] = valueFunc;
            if(watchForChanges)
            {
                _watchers[metadataKey] = valueFunc;
            }
            return this;
        }
        /// <inheritdoc/>
        IIndexerBuilder<T> IIndexerBuilder<T>.Requires(string metadataKey, Func<T, object> valueFunc)
        {
            metadataKey = Guard.NotNullOrWhitespace(metadataKey, nameof(metadataKey));
            valueFunc = Guard.NotNull(valueFunc, nameof(valueFunc));

            _watchers[metadataKey] = valueFunc;
            return this;
        }
    }
}
