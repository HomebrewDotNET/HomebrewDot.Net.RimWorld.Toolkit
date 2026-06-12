using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Verse;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;
using Logger = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Default implementation of <see cref="ISnapshotManager"/>.
    /// </summary>
    public class SnapshotManager : ISnapshotManager
    {
        // Fields
        private readonly object _lock = new object();
        private readonly IDatabase _database;
        private readonly IHookManager _hookManager;
        private IReadOnlyDatabase _databaseSnapshot;
        private object[] _changeTrackers;

        /// <inheritdoc cref="SnapshotManager"/>
        /// <param name="database">The database the snapshot manager will manage.</param>
        /// <param name="hookManager">The hook manager used to trigger events.</param>
        public SnapshotManager(IDatabase database, IHookManager hookManager)
        {
            _database = Guard.NotNull(database, nameof(database));
            _hookManager = Guard.NotNull(hookManager, nameof(hookManager));
            DatabaseSnapshot = _database.AsReadOnly();
        }
        /// <inheritdoc/>
        public IReadOnlyDatabase DatabaseSnapshot
        {
            get
            {
                lock (_lock)
                {
                    return _databaseSnapshot;
                }
            }
            protected set
            {
                lock (_lock)
                {
                    _databaseSnapshot = Guard.NotNull(value, nameof(DatabaseSnapshot));
                }
            }
        }
        /// <inheritdoc/>
        public IReadOnlyDatabase Database => _database;

        /// <inheritdoc/>
        public bool Destroyed<T>(T data, IReadOnlyDictionary<string, object> metadata = null) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            return _database.Delete(data, metadata);
        }

        public bool Destroyed<T>(T data, params KeyValuePair<string, object>[] metadata) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            Dictionary<string, object> metadataDictionary = null;
            if (metadata != null && metadata.Length > 0)
            {
                metadataDictionary ??= new Dictionary<string, object>(metadata.Length, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < metadata.Length; i++)
                {
                    var kvp = metadata[i];
                    metadataDictionary[kvp.Key] = kvp.Value;
                }
            }
            return _database.Delete(data, (IReadOnlyDictionary<string, object>)metadataDictionary ?? NullDictionary<string, object>.Instance);
        }
        /// <inheritdoc/>
        public bool Destroyed<T>(T data, params (string Key, object Value)[] metadata) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            Dictionary<string, object> metadataDictionary = null;
            if (metadata != null && metadata.Length > 0)
            {
                metadataDictionary ??= new Dictionary<string, object>(metadata.Length, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < metadata.Length; i++)
                {
                    var kvp = metadata[i];
                    metadataDictionary[kvp.Key] = kvp.Value;
                }
            }
            return _database.Delete(data, (IReadOnlyDictionary<string, object>)metadataDictionary ?? NullDictionary<string, object>.Instance);
        }

        /// <inheritdoc/>
        public bool Push<T>(T data, IReadOnlyDictionary<string, object> metadata = null) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            if (!Changed(data, () => metadata))
            {
                return false;
            }
            return _database.Upsert(data, metadata);
        }
        /// <inheritdoc/>
        public bool Push<T>(T data, params KeyValuePair<string, object>[] metadata) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            if (!Changed(data, () => metadata))
            {
                return false;
            }
            Dictionary<string, object> metadataDictionary = null;
            if (metadata != null && metadata.Length > 0)
            {
                metadataDictionary ??= new Dictionary<string, object>(metadata.Length, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < metadata.Length; i++)
                {
                    var kvp = metadata[i];
                    metadataDictionary[kvp.Key] = kvp.Value;
                }
            }

            return _database.Upsert(data, (IReadOnlyDictionary<string, object>)metadataDictionary ?? NullDictionary<string, object>.Instance);
        }
        /// <inheritdoc/>
        public bool Push<T>(T data, params (string Key, object Value)[] metadata) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            if (!Changed(data, () => metadata != null ? metadata.Select(m => new KeyValuePair<string, object>(m.Key, m.Value)) : null))
            {
                return false;
            }
            Dictionary<string, object> metadataDictionary = null;
            if (metadata != null && metadata.Length > 0)
            {
                metadataDictionary ??= new Dictionary<string, object>(metadata.Length, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < metadata.Length; i++)
                {
                    var kvp = metadata[i];
                    metadataDictionary[kvp.Key] = kvp.Value;
                }
            }

            return _database.Upsert(data, (IReadOnlyDictionary<string, object>)metadataDictionary ?? NullDictionary<string, object>.Instance);
        }
        /// <inheritdoc/>
        public void Reset(Action<ISnapshotManagerConfigurator> configurator, Action<IDatabaseSchemaBuilder> schemaBuilder)
        {
            schemaBuilder = Guard.NotNull(schemaBuilder, nameof(schemaBuilder));
            Logger.Log("Snapshot manager resetting and redeploying database");
            lock (_lock)
            {
                var config = new ConfigureSnapshotManager();
                configurator?.Invoke(config);
                _changeTrackers = config.changeTrackers?.ToArray();
                _database.Deploy(schemaBuilder);
                DatabaseSnapshot = _database.AsReadOnly();
            }
        }
        /// <inheritdoc/>
        public void Snapshot()
        {
            Logger.LogVerbose($"Snapshot manager taking snapshot of database. Current version {DatabaseSnapshot?.Version ?? '?'}");
            var snapshot = _database.AsReadOnly();
            lock (_lock)
            {
                if(snapshot.Version == DatabaseSnapshot?.Version)
                {
                    Logger.LogVerbose("Snapshot manager detected no changes in database since last snapshot. Skipping update.");
                    return;
                }
                DatabaseSnapshot = snapshot;
            }
            _hookManager.LazyTrigger(() => new OnSnapshotTakenTrigger(snapshot));
            Logger.LogVerbose($"Snapshot manager completed snapshot of database. New version {snapshot?.Version ?? '?'}");
        }

        private bool Changed<T>(T data, Func<IEnumerable<KeyValuePair<string, object>>> metadataProvider) where T : class
        {
            var existing = Database.Find<T>(data);
            if (existing == null) return true;
            var changeTrackers = _changeTrackers;
            if (changeTrackers == null || changeTrackers.Length == 0) return true;
            var metadata = metadataProvider?.Invoke();
            if (metadata != null)
            {
                foreach (var pair in metadata)
                {
                    if (!existing.Metadata.TryGetValue(pair.Key, out var existingValue) || !Equals(existingValue, pair.Value))
                    {
                        return true;
                    }
                }
            }

            for (int i = 0; i < changeTrackers.Length; i++)
            {
                var tracker = changeTrackers[i];
                if (tracker is IChangeTracker<T> typedTracker)
                {
                    if (typedTracker.HasChanged(data, existing, DatabaseSnapshot.Find<T>(data)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private class ConfigureSnapshotManager : ISnapshotManagerConfigurator
        {
            public HashSet<object> changeTrackers;

            public ISnapshotManagerConfigurator WithChangeTracker<T>(IChangeTracker<T> changeTracker) where T : class
            {
                changeTracker = Guard.NotNull(changeTracker, nameof(changeTracker));
                changeTrackers ??= new HashSet<object>();
                changeTrackers.Add(changeTracker);
                return this;
            }
        }
    }
}
