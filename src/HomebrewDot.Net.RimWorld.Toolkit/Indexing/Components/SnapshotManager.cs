using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Collections.Models;
using HomebrewDot.Net.RimWorld.Hooks;
using HomebrewDot.Net.RimWorld.Indexing.Triggers;
using Guard = HomebrewDot.Net.RimWorld.Toolkit.Helpers.Guard;
using Logger = HomebrewDot.Net.RimWorld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.RimWorld.Indexing.Components
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
        public void Destroyed<T>(T data, IReadOnlyDictionary<string, object> metadata = null) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            _database.Delete(data, metadata);
        }
        /// <inheritdoc/>
        public void Push<T>(T data, IReadOnlyDictionary<string, object> metadata = null) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            if(!Changed(data))
            {
                return;
            }
            _database.Upsert(data, metadata);
        }
        /// <inheritdoc/>
        public void Push<T>(T data, params KeyValuePair<string, object>[] metadata) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            if(!Changed(data))
            {
                return;
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

            _database.Upsert(data, (IReadOnlyDictionary<string, object>)metadataDictionary ?? NullDictionary<string, object>.Instance);
        }
        /// <inheritdoc/>
        public void Push<T>(T data, params (string Key, object Value)[] metadata) where T : class
        {
            data = Guard.NotNull(data, nameof(data));
            if(!Changed(data))
            {
                return;
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

            _database.Upsert(data, (IReadOnlyDictionary<string, object>)metadataDictionary ?? NullDictionary<string, object>.Instance);
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
            }
        }
        /// <inheritdoc/>
        public void Snapshot()
        {
            lock (_lock)
            {
                DatabaseSnapshot = _database.AsReadOnly();
            }
            _hookManager.LazyTrigger(() => new OnSnapshotTakenTrigger(DatabaseSnapshot));
        }

        private bool Changed<T>(T data) where T : class
        {
            var existing = DatabaseSnapshot.Find<T>(data);
            if (existing == null) return true;
            var changeTrackers = _changeTrackers;
            if (changeTrackers == null || changeTrackers.Length == 0) return true;

            for(int i = 0; i < changeTrackers.Length; i++)
            {
                var tracker = changeTrackers[i];
                if (tracker is IChangeTracker<T> typedTracker)
                {
                    if (typedTracker.HasChanged(data,existing))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private class ConfigureSnapshotManager : ISnapshotManagerConfigurator
        {
            public List<object> changeTrackers;

            public ISnapshotManagerConfigurator WithChangeTracker<T>(IChangeTracker<T> changeTracker) where T : class
            {
                changeTracker = Guard.NotNull(changeTracker, nameof(changeTracker));
                changeTrackers ??= new List<object>();
                changeTrackers.Add(changeTracker);
                return this;
            }
        }
    }
}
