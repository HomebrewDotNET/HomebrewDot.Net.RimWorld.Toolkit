using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Eventing.Models;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Generic.Components;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using RimWorld;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.Rimworld.Indexing.Components.Database;
using static HomebrewDot.Net.Rimworld.Toolkit;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;
using static HomebrewDot.Net.Rimworld.Toolkit.Indexing;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Default implementation of <see cref="IDatabase"/>. This class is responsible for storing and managing indexed data, as well as providing methods for querying and manipulating that data.
    /// </summary>
    public class Database : IDatabase, IDatabaseSchemaBuilder
    {
        // Statics
        private static readonly Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, ITrackingIndexed<object>>> _indexedCreator = new Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, ITrackingIndexed<object>>>();
        private static Func<object, IReadOnlyDictionary<string, object>, ITrackingIndexed<object>> GetCreatorForType(Type type)
        {
            if (!_indexedCreator.TryGetValue(type, out var creator))
            {
                lock (_indexedCreator)
                {
                    if (!_indexedCreator.TryGetValue(type, out creator))
                    {
                        creator = CreateCreatorForType(type);
                        _indexedCreator[type] = creator;
                    }
                }
            }
            return creator;
        }
        private static Func<object, IReadOnlyDictionary<string, object>, ITrackingIndexed<object>> CreateCreatorForType(Type type)
        {
            var inputParameter = System.Linq.Expressions.Expression.Parameter(typeof(object), "input");
            var inputMetadataParameter = System.Linq.Expressions.Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "metadata");
            var convertedInput = System.Linq.Expressions.Expression.Variable(type, "convertedInput");
            var castInputToConverted = System.Linq.Expressions.Expression.Convert(inputParameter, type);
            var assignConvertedInput = System.Linq.Expressions.Expression.Assign(convertedInput, castInputToConverted);
            var targetConstructor = Expression.GetConstructorForGeneric(type, () => new TrackingIndexed<object>(null, null, false));
            var newExpression = System.Linq.Expressions.Expression.New(targetConstructor, convertedInput, inputMetadataParameter, System.Linq.Expressions.Expression.Constant(false));

            var body = System.Linq.Expressions.Expression.Block(new[] { convertedInput }, assignConvertedInput, newExpression);

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, IReadOnlyDictionary<string, object>, ITrackingIndexed<object>>>(body, inputParameter, inputMetadataParameter);
            return lambda.Compile();
        }

        // Constants
        /// <summary>
        /// The char used to define sub tables in the database. For example, if you have a table named "Things" and a subtable named "Weapons", the full name of the subtable would be "Things.Weapons" using this separator.
        /// </summary>
        public const char TableNameSeparator = '.';

        // Fields
        private readonly object _lock = new object();
        private readonly List<Table> _tables = new List<Table>();
        private readonly Dictionary<string, Table> _tablesByName = new Dictionary<string, Table>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Type, IReadOnlyList<Table>> _tablesByAssignableTypeCache = new Dictionary<Type, IReadOnlyList<Table>>();
        private readonly Dictionary<Type, object> _typedDatabases = new Dictionary<Type, object>();
        private readonly HashSet<object> _listeners = new HashSet<object>();


        // State
        private ReadOnlyDatabaseSnapshot _cachedSnapshot;
        private HashSet<IIndexed<object>> _changedItems;
        private HashSet<IIndexed<object>> _deletedItems;
        private SnapshotBuilder _snapshotBuilder;
        private Queue<DatabaseAction> _intentLog = new Queue<DatabaseAction>();
        private Queue<DatabaseAction> _snapshotIntentLog;

        // Properties
        /// <inheritdoc/>
        public int Version { get; private set; }
        /// <summary>
        /// If the database is currently being deployed.
        /// </summary>
        public bool IsDeploying { get; private set; }
        /// <summary>
        /// If any changes have been made to the database since the last time <see cref="AsReadOnly"/> was called.
        /// </summary>
        public bool HasChanges { get; private set; }
        /// <inheritdoc/>
        public bool TrackingChanges { get; private set; }
        /// <inheritdoc/>
        public IReadOnlyCollection<IIndexed<object>> Changed => (IReadOnlyCollection<IIndexed<object>>)_changedItems ?? Array.Empty<IIndexed<object>>();
        /// <inheritdoc/>
        public IReadOnlyCollection<IIndexed<object>> Deleted => (IReadOnlyCollection<IIndexed<object>>)_deletedItems ?? Array.Empty<IIndexed<object>>();

        /// <inheritdoc/>
        public IIndexed<T> Find<T>(T data) where T : class
            => AsTyped<T>().Find(data);
        /// <inheritdoc/>
        public IEnumerable<IIndexed<T>> Find<T>(IEnumerable<T> data) where T : class
            => AsTyped<T>().Find(data);
        /// <inheritdoc/>
        public IEnumerable<IIndexed<T>> Find<T>(IReadOnlyList<T> data) where T : class
        => AsTyped<T>().Find(data);
        /// <inheritdoc/>
        public bool Upsert<T>(T item, ref IndexMetadata metadata) where T : class
            => AsTyped<T>().Upsert(item, ref metadata);

        /// <inheritdoc/>
        public bool Update<T>(T item, IIndexed<T> existing, ref IndexMetadata metadata) where T : class
            => AsTyped<T>().Update(item, existing, ref metadata);

        /// <inheritdoc/>
        public bool Delete<T>(T item, ref IndexMetadata metadata) where T : class
            => AsTyped<T>().Delete(item, ref metadata);

        /// <inheritdoc/>
        public IReadOnlyTable<T> GetTable<T>(string name) where T : class
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            var table = GetTable(name);
            if (table == null)
            {
                return null;
            }
            if (table is IReadOnlyTable<T> typedTable)
            {
                return typedTable;
            }
            else
            {
                throw new InvalidOperationException($"Table with name '{name}' exists but is not of the expected type '{typeof(T).FullName}'. Multiple source might be using the same name but different types which is a conflict");
            }
        }
        /// <inheritdoc/>
        public IReadOnlyTable GetTable(string name)
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            if (_tablesByName.TryGetValue(name, out var table))
            {
                return table;
            }
            return null;
        }
        /// <inheritdoc/>
        public IEnumerable<IReadOnlyTable> GetTables()
        {
            return _tables;
        }
        /// <inheritdoc/>
        public IEnumerable<IReadOnlyTable<T>> GetTables<T>() where T : class
        {
            return GetDbTables<T>().OfType<IReadOnlyTable<T>>();
        }

        /// <inheritdoc/>
        private IReadOnlyList<Table> GetDbTables<T>() where T : class
        {
            var type = typeof(T);

            if (!_tablesByAssignableTypeCache.TryGetValue(type, out var cachedTables))
            {
                cachedTables = _tables.Where(t => t.BaseEntityType.IsAssignableFrom(type)).ToArray();
                _tablesByAssignableTypeCache[type] = cachedTables;
            }

            return (IReadOnlyList<Table>)cachedTables;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<T> Query<T, TSearch>(string property, TSearch search, string tableName = null, string indexName = null) where T : class
        {
            property = Guard.NotNullOrEmpty(property, nameof(property));

            if (tableName != null)
            {
                var table = GetTable<T>(tableName);
                return table?.Query(property, search, indexName) ?? Array.Empty<T>();
            }
            HashSet<T> results = null;
            foreach (var table in GetTables<T>())
            {
                var tableResults = table.Query(property, search, indexName);
                results ??= new HashSet<T>();
                results.UnionWith(tableResults);
            }
            return results;
        }
        /// <inheritdoc/>
        public void Deploy(Action<IDatabaseSchemaBuilder> schemaBuilder)
        {
            schemaBuilder = Guard.NotNull(schemaBuilder, nameof(schemaBuilder));
            try
            {
                IsDeploying = true;
                lock (_lock)
                {
                    _tables.Clear();
                    _tablesByName.Clear();
                    _listeners.Clear();
                    TrackingChanges = false;
                    _changedItems?.Clear();
                    _deletedItems?.Clear();
                    _tablesByAssignableTypeCache.Clear();
                    _typedDatabases.Clear();
                    _intentLog?.Clear();
                    Version = 0;
                    _cachedSnapshot = null;

                    schemaBuilder(this);
                }
            }
            finally
            {
                IsDeploying = false;
            }
        }

        /// <inheritdoc/>
        public ISnapshotBuilder StartSnapshot()
        {
            if (_snapshotBuilder is not null)
            {
                if (!_snapshotBuilder.IsFinished)
                {
                    if (Logging.IsVerboseEnabled) Logging.LogVerbose($"Database snapshot already active.");
                    return _snapshotBuilder;
                }
                else
                {
                    _snapshotBuilder.Reset();
                }
            }

            _snapshotBuilder ??= new SnapshotBuilder(this);
            return _snapshotBuilder;
        }

        /// <inheritdoc/>
        public IDatabase<T> AsTyped<T>() where T : class
        {
            var type = typeof(T);
            if (!_typedDatabases.TryGetValue(type, out var typedDb))
            {
                lock (_lock)
                {
                    if (!_typedDatabases.TryGetValue(type, out typedDb))
                    {
                        typedDb = new TypedDatabase<T>(this);
                        _typedDatabases[type] = typedDb;
                    }
                }
            }
            return (IDatabase<T>)typedDb;
        }

        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.WithTable<T>(string name, Action<ITableBuilder<T>> tableBuilder, Predicate<T> predicate) where T : class
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            name = Guard.Is(name, name => !name.Contains(TableNameSeparator), exceptionBuilder: () => new ArgumentException($"Table name cannot contain the separator character '{TableNameSeparator}'", nameof(name)));
            var table = GetTable<T>(name);
            if (table is null)
            {
                lock (_lock)
                {
                    if (!_tablesByName.ContainsKey(name))
                    {
                        var newTable = predicate != null
                            ? new Table<T>(this, null, name, predicate, (name, table) => _tablesByName[name] = table)
                            : new Table<T>(this, null, name, false, (name, table) => _tablesByName[name] = table);
                        _tables.Add(newTable);
                        _tablesByName[name] = newTable;
                        Log($"Added {(predicate != null ? "filtered " : string.Empty)}table {name} of type {typeof(T).Name} to database schema");
                        tableBuilder?.Invoke((ITableBuilder<T>)newTable);
                    }
                }
            }
            else
            {
                tableBuilder?.Invoke((ITableBuilder<T>)_tablesByName[name]);
            }
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnInserting(Action<IWriteableIndexed<object>, IndexMetadata, IDatabase> onInserting)
        {
            onInserting = Guard.NotNull(onInserting, nameof(onInserting));

            var listener = new DelegateDatabaseListener<object>();
            listener.onUpserting = onInserting;
            _listeners.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnInserted(Action<IIndexed<object>, IndexMetadata, IDatabase> onInserted)
        {
            onInserted = Guard.NotNull(onInserted, nameof(onInserted));
            var listener = new DelegateDatabaseListener<object>();
            listener.onUpserted = onInserted;
            _listeners.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnDeleting(Action<IIndexed<object>, IndexMetadata, IDatabase> onDeleting)
        {
            onDeleting = Guard.NotNull(onDeleting, nameof(onDeleting));
            var listener = new DelegateDatabaseListener<object>();
            listener.onDeleting = onDeleting;
            _listeners.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnDeleted(Action<IIndexed<object>, IndexMetadata, IDatabase> onDeleted)
        {
            onDeleted = Guard.NotNull(onDeleted, nameof(onDeleted));
            var listener = new DelegateDatabaseListener<object>();
            listener.onDeleted = onDeleted;
            _listeners.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.TrackChanges()
        {
            _changedItems ??= new HashSet<IIndexed<object>>();
            _deletedItems ??= new HashSet<IIndexed<object>>();
            if (!TrackingChanges)
            {
                _changedItems.Clear();
                _deletedItems.Clear();
                ((IDatabaseSchemaBuilder)this).OnInserted((i,m,d) =>
                {
                    lock (_changedItems)
                    {
                        _changedItems.Add(i);
                    }
                });
                ((IDatabaseSchemaBuilder)this).OnDeleted((i, m, d) =>
                {
                    lock (_deletedItems)
                    {
                        _deletedItems.Add(i);
                    }
                });
            }
            TrackingChanges = true;
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.WithListener<T>(IDatabaseListener<T> listener)
        {
            _listeners.Add(Guard.NotNull(listener, nameof(listener)));
            return this;
        }

        internal interface ITrackingIndexed<out T> : IIndexed<T>, IWriteableIndexed<T> where T : class
        {
            Dictionary<string, object> IndexedBy { get; }

            IIndexed<T> Clone();

            IIndexed<T> TakeSnapshot();

            bool HasChanges { get; }
            bool IsInsert { get; }

            void Commit();
        }

        internal class TrackingIndexed<T> : Indexed<T>, ITrackingIndexed<T>, IWriteableIndexed<T> where T : class
        {
            // Fields
            private IReadOnlyDictionary<string, object> _metadata;
            private readonly object _lock = new object();
            private Dictionary<string, object> _mutableMetadata;
            private Dictionary<string, object> _indexedBy;
            private int _hashCode;

            // State
            private TrackingIndexed<T> _snapshot;

            // Properties
            /// <inheritdoc/>
            public override IReadOnlyDictionary<string, object> Metadata => _metadata ?? _mutableMetadata;
            public Dictionary<string, object> IndexedBy
            {
                get
                {
                    if (_indexedBy == null)
                    {
                        lock (_lock)
                        {
                            if (_indexedBy == null)
                            {
                                _indexedBy = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            }
                        }
                    }
                    return _indexedBy;
                }
            }
            public override bool IsSnapshot { get; }
            public override IIndexed<T> Snapshot => _snapshot;

            public bool HasChanges { get; private set; }

            public bool IsInsert { get; private set; }

            public TrackingIndexed(T value, IReadOnlyDictionary<string, object> metadata, bool isSnapshot = false) : base(value)
            {
                _mutableMetadata = metadata != null ? metadata.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                IsSnapshot = isSnapshot;
                HasChanges = true;
                IsInsert = true;
            }

            /// <inheritdoc/>
            public bool Set<TData>(string propertyName, TData value)
            {
                propertyName = Guard.NotNullOrEmpty(propertyName, nameof(propertyName));
                if (_metadata != null && _metadata.TryGetValue(propertyName, out var existingValue) && Equals(existingValue, value))
                {
                    return true;
                }
                _mutableMetadata ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (_metadata is not null)
                {
                    foreach (var kvp in _metadata)
                    {
                        _mutableMetadata.Add(kvp.Key, kvp.Value);
                    }
                    _metadata = null;
                }

                HasChanges = true;
                if (_mutableMetadata.ContainsKey(propertyName))
                {
                    _mutableMetadata[propertyName] = value;
                    return true;
                }
                else
                {
                    _mutableMetadata.Add(propertyName, value);
                    return false;
                }
            }
            /// <inheritdoc/>
            public bool Unset(string propertyName)
            {
                if (_metadata is not null)
                {
                    return false;
                }

                return _mutableMetadata.Remove(propertyName);
            }

            public override int GetHashCode()
            {
                if (_hashCode > 0) return _hashCode;
                _hashCode = base.GetHashCode();
                return _hashCode;
            }
            /// <inheritdoc/>
            public IIndexed<T> Clone()
                => new Indexed<T>(Value, _mutableMetadata != null ? new ReadOnlyDictionary<string, object>(_mutableMetadata) : _metadata);

            public IIndexed<T> TakeSnapshot()
            {
                if (IsSnapshot) return this;
                if(_snapshot == null)
                {
                    _snapshot = new TrackingIndexed<T>(Value, Metadata, true);
                }
                else
                {
                    foreach(var kvp in _mutableMetadata)
                    {
                        _snapshot._mutableMetadata[kvp.Key] = kvp.Value;
                    }

                    foreach(var key in _snapshot._mutableMetadata.Keys.ToArray())
                    {
                        if (!_mutableMetadata.ContainsKey(key))
                        {
                            _snapshot._mutableMetadata.Remove(key);
                        }
                    }
                }
                return _snapshot;
            }

            public void Commit()
            {
                HasChanges = false;
                IsInsert = false;
            }
        }

        private sealed class ReadOnlyDatabaseSnapshot : IReadOnlyDatabase
        {
            internal readonly List<IReadOnlyTable> _tables;
            internal readonly Dictionary<string, IReadOnlyTable> _tablesByName;
            internal readonly Dictionary<Type, IReadOnlyList<IReadOnlyTable<object>>> _tablesByAssignableTypeCache = new Dictionary<Type, IReadOnlyList<IReadOnlyTable<object>>>();
            internal readonly HashSet<IIndexed<object>> _changedItems = new HashSet<IIndexed<object>>();
            internal readonly HashSet<IIndexed<object>> _deletedItems = new HashSet<IIndexed<object>>();

            public int Version { get; internal set; }

            public bool TrackingChanges { get; }

            public bool IsSyncing { get; internal set; }

            public IReadOnlyCollection<IIndexed<object>> Changed => _changedItems;

            public IReadOnlyCollection<IIndexed<object>> Deleted => _deletedItems;

            public ReadOnlyDatabaseSnapshot(IDatabaseObject database, IEnumerable<IReadOnlyTable> tables, Dictionary<string, IReadOnlyTable> tablesByName)
            {
                Version = database.Version;
                _tables = tables.ToList();
                _tablesByName = tablesByName;
                TrackingChanges = database.TrackingChanges;
                _changedItems = database.TrackingChanges && database.Changed?.Count > 0 ? new HashSet<IIndexed<object>>(database.Changed) : new HashSet<IIndexed<object>>();
                _deletedItems = database.TrackingChanges && database.Deleted?.Count > 0 ? new HashSet<IIndexed<object>>(database.Deleted) : new HashSet<IIndexed<object>>();
            }

            public IIndexed<T> Find<T>(T data) where T : class
            {
                for (int i = 0; i < _tables.Count; i++)
                {
                    var table = _tables[i];
                    if (table.TryFind(data, out var indexed))
                    {
                        return indexed;
                    }
                }
                return null;
            }
            public IEnumerable<IIndexed<T>> Find<T>(IEnumerable<T> data) where T : class
            {
                foreach (var item in data)
                {
                    for (int i = 0; i < _tables.Count; i++)
                    {
                        var table = _tables[i];
                        if (table.TryFind(item, out var indexed))
                        {
                            yield return indexed;
                        }
                    }
                }
            }

            public IEnumerable<IIndexed<T>> Find<T>(IReadOnlyList<T> data) where T : class
            {
                for (int j = 0; j < data.Count; j++)
                {
                    var item = data[j];
                    for (int i = 0; i < _tables.Count; i++)
                    {
                        var table = _tables[i];
                        if (table.TryFind(item, out var indexed))
                        {
                            yield return indexed;
                        }
                    }
                }
            }
            public IReadOnlyTable<T> GetTable<T>(string name) where T : class
            {
                name = Guard.NotNullOrEmpty(name, nameof(name));
                var table = GetTable(name);
                if (table == null)
                    return null;
                if (table is IReadOnlyTable<T> typedTable)
                    return typedTable;
                throw new InvalidOperationException($"Table with name '{name}' exists but is not of the expected type '{typeof(T).FullName}'. Multiple source might be using the same name but different types which is a conflict");
            }

            private IReadOnlyTable GetTable(string name)
            {
                _tablesByName.TryGetValue(name, out var table);
                return table;
            }

            public IEnumerable<IReadOnlyTable> GetTables() => _tables;

            public IEnumerable<IReadOnlyTable<T>> GetTables<T>() where T : class
            {
                var type = typeof(T);
                if (!_tablesByAssignableTypeCache.TryGetValue(type, out var cachedTables))
                {
                    cachedTables = _tables.Where(t => type.IsAssignableFrom(t.BaseEntityType)).OfType<IReadOnlyTable<T>>().ToArray();
                    _tablesByAssignableTypeCache[type] = cachedTables;
                }
                return (IReadOnlyList<IReadOnlyTable<T>>)cachedTables;
            }

            public IReadOnlyCollection<T> Query<T, TSearch>(string property, TSearch search, string tableName = null, string indexName = null) where T : class
            {
                property = Guard.NotNullOrEmpty(property, nameof(property));
                var tables = string.IsNullOrEmpty(tableName) ? GetTables<T>().ToArray() : new[] { GetTable<T>(tableName) };
                var results = new HashSet<T>();
                foreach (var table in tables)
                {
                    var tableResults = table.Query(property, search, indexName);
                    if (tables.Length == 1)
                        return tableResults;
                    results.UnionWith(tableResults);
                }
                return results;
            }

            public IEnumerable Apply(IDatabaseObject table, Queue<DatabaseAction> intentLog, PendingWorkContext pendingWork)
            {
                if (intentLog is null || intentLog.Count == 0)
                {
                    pendingWork.LastSuccess = true;
                    yield break;
                }
                IsSyncing = true;
                _changedItems.Clear();
                _deletedItems.Clear();
                while (intentLog.TryDequeue(out var action))
                {
                    try
                    {
                        using (action)
                        {
                            pendingWork.LogWork();
                            switch (action.LogType)
                            {
                                case LogType.Upsert:
                                    _changedItems.Add(action.Entity);
                                    break;
                                case LogType.Delete:
                                    _deletedItems.Add(action.Entity);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error applying intent log to database: {ex}");
                        intentLog.Clear();
                        pendingWork.LastSuccess = false;
                        yield break;
                    }
                    if (pendingWork.WaitForNextTick)
                    {
                        yield return null;
                    }
                }

                IsSyncing = false;
                if (IsVerboseEnabled) LogVerbose($"Applied changes to database snapshot");
                pendingWork.LastSuccess = true;
            }
        }

        private class DatabaseAction : IPoolable, IDisposable
        {
            public LogType LogType;
            public IIndexed<object> Entity;

            public void Dispose()
            {
                Toolkit.Pool<DatabaseAction>.Return(this);
            }

            public void Reset()
            {
                Entity = null;
            }
        }

        /// <summary>
        /// Typed wrapper around <see cref="Database"/> that provides optimized access for a specific type <typeparamref name="T"/>.
        /// Caches the relevant table list and bypasses type-dispatch overhead in hot paths.
        /// </summary>
        /// <typeparam name="T">The entity type this instance is optimized for.</typeparam>
        internal class TypedDatabase<T> : IDatabase<T> where T : class
        {
            private readonly Database _database;
            private readonly IReadOnlyList<Table> _tables;
            private readonly IDatabaseListener<T>[] _listeners;

            public TypedDatabase(Database database)
            {
                _database = Guard.NotNull(database, nameof(database));
                _tables = _database.GetDbTables<T>();
                _listeners = _database._listeners.OfType<IDatabaseListener<T>>().ToArray();
            }
            
            /// <inheritdoc/>
            public IIndexed<T> Find(T data)
            {
                for (int i = 0; i < _tables.Count; i++)
                {
                    var table = _tables[i];
                    if (table.TryGet(data, out ITrackingIndexed<T> indexed))
                    {
                        return indexed;
                    }
                }
                return null;
            }
            /// <inheritdoc/>
            public IEnumerable<IIndexed<T>> Find(IEnumerable<T> data)
            {
                foreach (var item in data)
                {
                    for (int i = 0; i < _tables.Count; i++)
                    {
                        var table = _tables[i];
                        if (table.TryGet(item, out ITrackingIndexed<T> indexed))
                        {
                            yield return indexed;
                        }
                    }
                }
            }

            /// <inheritdoc/>
            public IEnumerable<IIndexed<T>> Find(IReadOnlyList<T> data)
            {
                for (int j = 0; j < data.Count; j++)
                {
                    var item = data[j];
                    for (int i = 0; i < _tables.Count; i++)
                    {
                        var table = _tables[i];
                        if (table.TryGet(item, out ITrackingIndexed<T> indexed))
                        {
                            yield return indexed;
                        }
                    }
                }
            }

            /// <inheritdoc/>
            public bool Upsert(T item, ref IndexMetadata metadata)
            {
                item = Guard.NotNull(item, nameof(item));

                ITrackingIndexed<T> trackedItem = null;
                for (int i = 0; i < _tables.Count; i++)
                {
                    if (_tables[i].TryGet(item, out trackedItem))
                    {
                        break;
                    }
                }

                if (trackedItem == null)
                {
                    var creator = GetCreatorForType(item.GetType());
                    trackedItem = (ITrackingIndexed<T>)creator(item, NullDictionary<string, object>.Instance);
                }

                return Update(item, trackedItem, ref metadata);
            }

            /// <inheritdoc/>
            public bool Update(T item, IIndexed<T> existing, ref IndexMetadata metadata)
            {
                item = Guard.NotNull(item, nameof(item));

                if (existing is not ITrackingIndexed<T> trackedItem)
                {
                    var creator = GetCreatorForType(item.GetType());
                    trackedItem = (ITrackingIndexed<T>)creator(item, NullDictionary<string, object>.Instance);
                }

                bool anyInserted = false;
                for (int i = 0; i < _listeners.Length; i++)
                {
                    var handler = _listeners[i];
                    try
                    {
                        handler.OnUpserting(trackedItem, ref metadata, _database);
                    }
                    catch { Logging.LogError($"Failed to execute {handler}{nameof(IDatabaseListener<T>.OnUpserting)} listener"); }
                }

                for (int i = 0; i < _tables.Count; i++)
                {
                    if (_tables[i].TryAddOrUpdate(trackedItem, metadata))
                    {
                        anyInserted = true;
                    }
                }

                metadata.PersistTo(trackedItem);
                metadata.Dispose();

                if (anyInserted || trackedItem.HasChanges)
                {
                    if(_database._cachedSnapshot != null)
                    {
                        var action = Toolkit.Pool<DatabaseAction>.Rent();
                        action.LogType = LogType.Upsert;
                        action.Entity = trackedItem;
                        _database._intentLog.Enqueue(action);
                    }
                    _database.HasChanges = true;
                    for (int i = 0; i < _listeners.Length; i++)
                    {
                        var handler = _listeners[i];
                        try
                        {
                            handler.OnUpserted(trackedItem, ref metadata, _database);
                        }
                        catch { Logging.LogError($"Failed to execute {handler}{nameof(IDatabaseListener<T>.OnUpserted)} listener"); }
                    }
                    trackedItem.Commit();
                }
                return anyInserted;
            }

            /// <inheritdoc/>
            public bool Delete(T item, ref IndexMetadata metadata)
            {
                item = Guard.NotNull(item, nameof(item));

                ITrackingIndexed<T> foundItem = null;
                for (int i = 0; i < _tables.Count; i++)
                {
                    if (_tables[i].TryGet(item, out foundItem))
                    {
                        break;
                    }
                }

                if (foundItem is not TrackingIndexed<T> typedItem)
                {
                    return false;
                }

                bool anyDeleted = false;
                for (int i = 0; i < _listeners.Length; i++)
                {
                    var handler = _listeners[i];
                    try
                    {
                        handler.OnDeleting(foundItem, ref metadata, _database);
                    }
                    catch { Logging.LogError($"Failed to execute {handler}{nameof(IDatabaseListener<T>.OnDeleting)} listener"); }
                }
                for (int i = 0; i < _tables.Count; i++)
                {
                    if (_tables[i].TryDelete(typedItem, metadata))
                    {
                        anyDeleted = true;
                    }
                }

                if (anyDeleted)
                {
                    if(_database._cachedSnapshot != null)
                    {
                        var action = Toolkit.Pool<DatabaseAction>.Rent();
                        action.LogType = LogType.Delete;
                        action.Entity = typedItem;
                        _database._intentLog.Enqueue(action);
                    }
                    for (int i = 0; i < _listeners.Length; i++)
                    {
                        var handler = _listeners[i];
                        try
                        {
                            handler.OnDeleted(typedItem, ref metadata, _database);
                        }
                        catch { Logging.LogError($"Failed to execute {handler}{nameof(IDatabaseListener<T>.OnDeleted)} listener"); }
                    }
                    _database.HasChanges = true;
                    metadata.Dispose();
                    return true;
                }
                metadata.Dispose();
                return false;
            }
        }

        private class SnapshotBuilder : ISnapshotBuilder
        {
            private readonly Database _database;

            private RaiseCooperativeWork _pendingWork;

            public IDatabase Database => _database;

            public IReadOnlyDatabase Snapshot { get; private set; }

            public bool IsFinished => Snapshot != null;

            public SnapshotBuilder(Database database)
            {
                _database = Guard.NotNull(database, nameof(database));
            }

            private IEnumerable DoWork(PendingWorkContext context)
            {
                if (_database._cachedSnapshot != null)
                {
                    if (IsVerboseEnabled) LogVerbose($"Updating snapshot of database at version {_database.Version} with {_database._tables.Count} tables{(_database.TrackingChanges ? $" ({_database.Changed.Count} Changed/{_database.Deleted.Count} Deleted)" : string.Empty)}");

                    if (_database._intentLog.Count > 0 && _database._snapshotIntentLog is null)
                    {
                        _database._snapshotIntentLog = _database._intentLog;
                        _database._intentLog = new Queue<DatabaseAction>(_database._snapshotIntentLog.Count);
                    }
                    yield return _database._cachedSnapshot.Apply(_database, _database._snapshotIntentLog, context);
                    var result = context.LastSuccess;

                    if (!result)
                    {
                        LogWarning($"Failed to apply intent log to snapshot of database at version {_database.Version}. Creating new snapshot.");
                        _database._snapshotIntentLog = null;
                    }
                    else
                    {
                        for (int i = 0; i < _database._tables.Count; i++)
                        {
                            var table = _database._tables[i];
                            var oldSnapshot = _database._cachedSnapshot._tables[i];
                            yield return table.CreateSnapshot(context);
                            var snapshot = context.LastSnapshot;
                            if (snapshot != oldSnapshot)
                            {
                                _database._cachedSnapshot._tables[i] = snapshot;
                                _database._cachedSnapshot._tablesByName[snapshot.FullName] = snapshot;
                                _database._cachedSnapshot._tablesByAssignableTypeCache.Clear();
                            }
                        }
                        _database.HasChanges = false;
                        _database._changedItems?.Clear();
                        _database._deletedItems?.Clear();
                        if (IsVerboseEnabled) LogVerbose($"Updated snapshot of database at version {_database.Version} with {_database._tables.Count} tables{(_database.TrackingChanges ? $" ({_database.Changed.Count} Changed/{_database.Deleted.Count} Deleted)" : string.Empty)}");
                        _database.Version++;
                        _database._cachedSnapshot.Version = _database.Version;
                        Snapshot = _database._cachedSnapshot;
                        _database._snapshotIntentLog = null;
                        yield break;
                    }
                }

                var snapshots = new List<IReadOnlyTable>();
                foreach (var table in _database._tables)
                {
                    yield return table.CreateSnapshot(context);
                    var snapshot = context.LastSnapshot;
                    snapshots.Add(snapshot);
                }

                var snapshotsByName = new Dictionary<string, IReadOnlyTable>(StringComparer.OrdinalIgnoreCase);
                foreach (var snapshot in snapshots)
                {
                    snapshotsByName[snapshot.Name] = snapshot;
                    var subTables = snapshot.SubTables;
                    while (subTables?.Count > 0)
                    {
                        var newSubTables = new List<IReadOnlyTable>();
                        foreach (var subTable in subTables)
                        {
                            snapshotsByName[subTable.FullName] = subTable;
                            if (subTable.SubTables?.Count > 0)
                            {
                                newSubTables.AddRange(subTable.SubTables);
                            }
                        }
                        subTables = newSubTables;
                    }
                }

                _database.HasChanges = false;
                _database.Version++;
                _database._cachedSnapshot = new ReadOnlyDatabaseSnapshot(_database, snapshots, snapshotsByName);
                _database._changedItems?.Clear();
                _database._deletedItems?.Clear();
                if (IsVerboseEnabled) LogVerbose($"Created new snapshot of database at version {_database.Version} with {snapshots.Count} tables{(_database.TrackingChanges ? $" ({_database.Changed.Count} Changed/{_database.Deleted.Count} Deleted)" : string.Empty)}");
                Snapshot = _database._cachedSnapshot;
            }

            internal void Reset()
            {
                Snapshot = null;
                _pendingWork = null;
            }

            public RaiseCooperativeWork CreateWork()
            {
                if(_pendingWork?.Started?.IsFinished != false)
                {
                    var context = new PendingWorkContext();
                    _pendingWork = RaiseCooperativeWork.From<PendingWorkContext>(() => DoWork(context).GetEnumerator(), context);
                }
                return _pendingWork;
            }

            public IReadOnlyDatabase Build()
            {
                var work = CreateWork();
                work.RunManually();
                return Snapshot;
            }
        }
    }

    /// <summary>
    /// Represents a table within the database, providing access to its data and metadata.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the table.</typeparam>
    public class Table<T> : Table, IReadOnlyTable<T>, ITableBuilder<T> where T : class
    {
        // Fields
        private readonly List<Table> _subTables = new List<Table>();
        private readonly Dictionary<T, ITrackingIndexed<T>> _data = new Dictionary<T, ITrackingIndexed<T>>();
        private readonly HashSet<ITableListener<T>> _listenersSet = new HashSet<ITableListener<T>>();
        private readonly Predicate<T> _filter;
        private readonly Dictionary<string, Dictionary<object, HashSet<T>>> _indexes = new Dictionary<string, Dictionary<object, HashSet<T>>>();
        private readonly Dictionary<string, HashSet<T>> _boolIndexes = new Dictionary<string, HashSet<T>>();
        private readonly Action<string, Table> _onSubTableAdded;
        private readonly IDatabase _owner;

        // State
        private bool _hasChanges = true;
        private SnapshotTable _cachedSnapshot;
        private Queue<TableAction> _intentLog = new Queue<TableAction>();
        private Queue<TableAction> _snapshotIntentLog = new Queue<TableAction>();
        private ITableListener<T>[] _listeners;

        // Properties
        /// <summary>
        /// The parent table of this table, if it is a subtable. If this table is a root table, this property will be <c>null</c>.
        /// </summary>
        public IReadOnlyTable Parent { get; private set; }
        /// <inheritdoc/>
        public override IReadOnlyList<IReadOnlyTable> SubTables => _subTables;
        /// <inheritdoc/>
        public override string FullName => $"{Parent?.FullName}{(Parent != null ? TableNameSeparator.ToString() : string.Empty)}{Name}";
        /// <inheritdoc/>
        public override Type BaseEntityType => typeof(T);

        /// <inheritdoc cref="Table{T}"/>
        /// <param name="owner">The database that owns this table.</param>
        /// <param name="name">The name of the table.</param>
        /// <param name="isFiltered">Indicates whether the table is filtered.</param>
        public Table(IDatabase owner, IReadOnlyTable parent, string name, bool isFiltered, Action<string, Table> onSubTableAdded) : base(name, isFiltered)
        {
            _owner = Guard.NotNull(owner, nameof(owner));
            _onSubTableAdded = onSubTableAdded;
            Parent = parent;
        }
        /// <inheritdoc cref="Table{T}"/>
        /// <param name="owner">The database that owns this table.</param>
        /// <param name="name">The name of the table.</param>
        /// <param name="filter">Predicate used to filter the data in the table. If provided, only data that satisfies the predicate will be stored in the table.</param>
        public Table(IDatabase owner, IReadOnlyTable parent, string name, Predicate<T> filter, Action<string, Table> onSubTableAdded) : base(name, filter != null)
        {
            _owner = Guard.NotNull(owner, nameof(owner));
            _filter = filter;
            _onSubTableAdded = onSubTableAdded;
            Parent = parent;
        }

        /// <inheritdoc/>
        internal override bool TryGet<T1>(T1 data, out ITrackingIndexed<T1> item)
        {
            if (data is T typedData)
            {
                if (_data.TryGetValue(typedData, out var indexed))
                {
                    item = (ITrackingIndexed<T1>)(object)indexed;
                    return true;
                }
            }
            item = null;
            return false;
        }
        /// <inheritdoc/>
        internal override bool TryAddOrUpdate<T1>(ITrackingIndexed<T1> item, IndexMetadata metadata)
        {
            item = Guard.NotNull(item, nameof(item));
            if (item is ITrackingIndexed<T> tableItem)
            {

                if (_filter != null && !_filter(tableItem.Value))
                {
                    return TryDelete(tableItem, metadata);
                }

                bool added = false;
                if (!_data.ContainsKey(tableItem.Value))
                {
                    _data[tableItem.Value] = tableItem;
                    added = true;
                }
                _listeners ??= _listenersSet.ToArray();
                for (int i = 0; i < _listeners.Length; i++)
                {
                    var handler = _listeners[i];
                    try
                    {
                        handler.OnUpserting(tableItem, ref metadata, this);
                    }
                    catch { Logging.LogError($"Failed to execute {handler}{nameof(ITableListener<T>.OnDeleted)} listener"); }
                }

                bool changed = added || tableItem.HasChanges || tableItem.IsInsert;
                if (changed)
                {
                    _hasChanges = true;
                }

                if (changed && _cachedSnapshot != null)
                {
                    var action = Toolkit.Pool<TableAction>.Rent();
                    action.LogType = LogType.Upsert;
                    action.Entity = (ITrackingIndexed<T>)tableItem;
                    _intentLog.Enqueue(action);
                }
                bool subChanged = false;
                if (_subTables.Count > 0)
                {
                    foreach (var subTable in _subTables)
                    {
                        if (subTable.TryAddOrUpdate(tableItem, metadata))
                        {
                            subChanged = true;
                        }
                    }
                }
                if (changed)
                {
                    for (int i = 0; i < _listeners.Length; i++)
                    {
                        var handler = _listeners[i];
                        try
                        {
                            handler.OnUpserted(tableItem, ref metadata, this);
                        }
                        catch { Logging.LogError($"Failed to execute {handler}{nameof(ITableListener<T>.OnDeleted)} listener"); }
                    }
                }
                return changed || subChanged;
            }
            return false;
        }
        /// <inheritdoc/>
        internal override bool TryDelete<T1>(ITrackingIndexed<T1> item, IndexMetadata metadata)
        {
            if (item is ITrackingIndexed<T> tableItem)
            {
                return Delete(tableItem, metadata);
            }
            return false;
        }

        private bool Delete(ITrackingIndexed<T> item, IndexMetadata metadata)
        {
            if (_data.Remove(item.Value))
            {

                if(_cachedSnapshot != null)
                {
                    var action = Toolkit.Pool<TableAction>.Rent();
                    action.LogType = LogType.Delete;
                    action.Entity = (ITrackingIndexed<T>)item;
                    _intentLog.Enqueue(action);
                }
                _hasChanges = true;
                for (int i = 0; i < _listeners.Length; i++)
                {
                    var handler = _listeners[i];
                    try
                    {
                        handler.OnDeleting(item, ref metadata, this);
                    }
                    catch { Logging.LogError($"Failed to execute {handler}{nameof(ITableListener<T>.OnDeleting)} listener"); }
                }
                foreach (var subTable in SubTables.OfType<Table>())
                {
                    _ = subTable.TryDelete(item, metadata);
                }
                _listeners ??= _listenersSet.ToArray();
                for (int i = 0; i < _listeners.Length; i++)
                {
                    var handler = _listeners[i];
                    try
                    {
                        handler.OnDeleted(item, ref metadata, this);
                    }
                    catch { Logging.LogError($"Failed to execute {handler}{nameof(ITableListener<T>.OnDeleted)} listener"); }
                }
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<T> Query<TSearch>(string property, TSearch search, string indexName = null)
        {
            property = Guard.NotNullOrEmpty(property, nameof(property));
            var fullIndexName = GetFullIndexName(indexName, property);

            if (_indexes.TryGetValue(fullIndexName, out var index))
            {
                if (index.TryGetValue(search, out var indexedSet))
                {
                    lock (indexedSet)
                    {
                        return indexedSet.ToArray();
                    }
                }
            }
            return Array.Empty<T>();
        }

        private string GetFullIndexName(string indexName, string propertyName)
        {
            return $"{indexName ?? "default"}{propertyName}";
        }

        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnDeleted(Action<IIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onDeleted)
        {
            onDeleted = Guard.NotNull(onDeleted, nameof(onDeleted));
            var listener = new DelegateTableListener<T>();
            listener.onDeleted = onDeleted;
            _listenersSet.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnDeleting(Action<IIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onDeleting)
        {
            onDeleting = Guard.NotNull(onDeleting, nameof(onDeleting));
            var listener = new DelegateTableListener<T>();
            listener.onDeleting = onDeleting;
            _listenersSet.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnInserted(Action<IIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onInserted)
        {
            onInserted = Guard.NotNull(onInserted, nameof(onInserted));
            var listener = new DelegateTableListener<T>();
            listener.onUpserted = onInserted;
            _listenersSet.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnInserting(Action<IWriteableIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onInserting)
        {
            onInserting = Guard.NotNull(onInserting, nameof(onInserting));
            var listener = new DelegateTableListener<T>();
            listener.onUpserting = onInserting;
            _listenersSet.Add(listener);
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.WithIndex<TProperty>(string propertyName, Func<IIndexed<T>, TProperty> propertySelector, Predicate<T> filter, string name)
        {
            propertyName = Guard.NotNullOrEmpty(propertyName, nameof(propertyName));
            propertySelector = Guard.NotNull(propertySelector, nameof(propertySelector));

            var fullIndexName = GetFullIndexName(name, propertyName);
            if (_indexes.ContainsKey(fullIndexName))
            {
                return this;
            }
            _indexes.Add(fullIndexName, new Dictionary<object, HashSet<T>>());
            Log($"Added {(filter != null ? "filtered " : string.Empty)}index {fullIndexName} on property {propertyName} to table {Name}");

            var self = ((ITableBuilder<T>)this);
            self.OnInserted((i, m, t) =>
            {
                var indexValue = propertySelector(i);
                Dictionary<object, HashSet<T>> index;
                lock (_indexes)
                {
                    if (!_indexes.TryGetValue(fullIndexName, out index))
                    {
                        return;
                    }
                }
                if (i is ITrackingIndexed<T> tracked)
                {
                    lock (tracked)
                    {
                        if (tracked.IndexedBy.TryGetValue(propertyName, out var existingIndexValue))
                        {
                            if (Equals(existingIndexValue, indexValue))
                            {
                                return;
                            }
                            // Remove from old index value
                            lock (index)
                            {
                                if (index.TryGetValue(existingIndexValue, out var existingSet))
                                {
                                    lock (existingSet)
                                    {
                                        existingSet.Remove(tracked.Value);
                                    }
                                    if(_cachedSnapshot != null)
                                    {
                                        var action = Toolkit.Pool<TableAction>.Rent();
                                        action.LogType = LogType.IndexRemove;
                                        action.Entity = tracked;
                                        action.IndexName = fullIndexName;
                                        action.IndexValue = tracked.Value;
                                        _intentLog.Enqueue(action);
                                    }
                                }
                            }
                            tracked.IndexedBy.Remove(propertyName);
                        }

                        if (filter != null && !filter(tracked.Value))
                        {
                            return;
                        }

                        if (indexValue is not null)
                        {
                            HashSet<T> set;
                            lock (index)
                            {
                                if (!index.TryGetValue(indexValue, out set))
                                {
                                    set = new HashSet<T>();
                                    index[indexValue] = set;
                                }
                            }
                            lock (set)
                            {
                                set.Add(tracked.Value);
                            }
                            if(_cachedSnapshot != null)
                            {
                                var action = Toolkit.Pool<TableAction>.Rent();
                                action.LogType = LogType.IndexUpdate;
                                action.Entity = tracked;
                                action.IndexName = fullIndexName;
                                action.IndexValue = tracked.Value;
                                _intentLog.Enqueue(action);
                            }
                            tracked.IndexedBy.Add(propertyName, indexValue);
                        }
                    }
                }
            }).OnDeleted((i, m, t) =>
            {
                Dictionary<object, HashSet<T>> index;
                lock (_indexes)
                {
                    if (!_indexes.TryGetValue(fullIndexName, out index))
                    {
                        return;
                    }
                }
                if (i is ITrackingIndexed<T> tracked)
                {
                    lock (tracked)
                    {
                        if (tracked.IndexedBy.TryGetValue(propertyName, out var existingIndexValue))
                        {
                            lock (index)
                            {
                                if (index.TryGetValue(existingIndexValue, out var existingSet))
                                {
                                    lock (existingSet)
                                    {
                                        existingSet.Remove(tracked.Value);
                                    }
                                    if(_cachedSnapshot != null)
                                    {
                                        var action = Toolkit.Pool<TableAction>.Rent();
                                        action.LogType = LogType.IndexRemove;
                                        action.Entity = tracked;
                                        action.IndexName = fullIndexName;
                                        action.IndexValue = tracked.Value;
                                        _intentLog.Enqueue(action);
                                    }
                                }
                            }
                            tracked.IndexedBy.Remove(propertyName);
                        }
                    }
                }
            });

            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.WithIndex(string propertyName, Func<IIndexed<T>, bool> propertySelector, string name)
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            propertyName = Guard.NotNullOrEmpty(propertyName, nameof(propertyName));
            propertySelector = Guard.NotNull(propertySelector, nameof(propertySelector));
            return ((ITableBuilder<T>)this).WithIndex<bool>(propertyName, propertySelector, null, name);
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.WithSubTable<TSub>(string name, Predicate<TSub> filter, Action<ITableBuilder<TSub>> tableBuilder)
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            var existingSubTable = _subTables.FirstOrDefault(st => st.Name == name);
            if (existingSubTable is null)
            {
                var newTable = filter != null ? new Table<TSub>(_owner, this, name, filter, _onSubTableAdded) : new Table<TSub>(_owner, this, name, true, _onSubTableAdded);
                _subTables.Add(newTable);
                _onSubTableAdded?.Invoke(newTable.FullName, newTable);
                Log($"Added {(filter != null ? "filtered " : string.Empty)}sub table {name} of type {typeof(TSub).Name} to table {Name} of type {typeof(T).Name}");

                existingSubTable = newTable;
            }
            if (existingSubTable is ITableBuilder<TSub> subTableBuilder)
            {
                tableBuilder?.Invoke(subTableBuilder);
            }
            else
            {
                throw new InvalidOperationException($"Subtable with name '{name}' already exists but is not of the expected type '{typeof(TSub).FullName}'. Multiple source might be using the same name but different types which is a conflict. Found table {existingSubTable.GetType().FullName}");
            }
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.WithSubTable(string name, Predicate<T> filter, Action<ITableBuilder<T>> tableBuilder)
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            filter = Guard.NotNull(filter, nameof(filter));
            var existingSubTable = _subTables.FirstOrDefault(st => st.Name == name);
            if (existingSubTable is null)
            {
                var newTable = new Table<T>(_owner, this, name, filter, _onSubTableAdded);
                _subTables.Add(newTable);
                _onSubTableAdded?.Invoke(newTable.FullName, newTable);
                Log($"Added {(filter != null ? "filtered " : string.Empty)}sub table {name} of type {typeof(T).Name} to table {Name} of same type");

                existingSubTable = newTable;
            }
            if (existingSubTable is ITableBuilder<T> subTableBuilder)
            {
                tableBuilder?.Invoke(subTableBuilder);
            }
            else
            {
                throw new InvalidOperationException($"Subtable with name '{name}' already exists but is not of the expected type '{typeof(T).FullName}'. Multiple source might be using the same name but different types which is a conflict. Found table {existingSubTable.GetType().FullName}");
            }
            return this;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IIndexed<T>> GetSnapshot()
        {
            return _data.Values.ToArray();
        }
        /// <inheritdoc/>
        IEnumerator<IIndexed<T>> IEnumerable<IIndexed<T>>.GetEnumerator()
            => _data.Values.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => _data.Values.GetEnumerator();
        /// <inheritdoc/>
        internal override IEnumerable CreateSnapshot(PendingWorkContext pendingWork)
        {
            if (!_hasChanges && _cachedSnapshot != null)
            {
                if (IsVerboseEnabled) LogVerbose($"Using cached snapshot for table {FullName}");
                pendingWork.LastSnapshot = _cachedSnapshot;
                yield break;
            }

            if (_cachedSnapshot != null)
            {
                if (IsVerboseEnabled) LogVerbose($"Applying intent log on snapshot for table {FullName} of version {Version} with {_data.Count} items{(TrackingChanges ? $" ({Changed.Count} Changed/{Deleted.Count} Deleted)" : string.Empty)}, {_indexes.Count} indexes and {_subTables.Count} subtables ({pendingWork.CurrentActions}<{pendingWork.TotalActions})");
                if (_intentLog.Count > 0)
                {
                    if (_snapshotIntentLog.Count > 0) throw new InvalidOperationException("Inactive snapshot queue is not empty. Expected it to be consumed");
                    (_intentLog, _snapshotIntentLog) = (_snapshotIntentLog, _intentLog);
                    if (IsVerboseEnabled) LogVerbose($"Prepared intent log for syncing changes on snapshot for table {FullName}");
                    if (pendingWork.IsOverRunTime)
                    {
                        yield return null;
                    }
                }
                yield return _cachedSnapshot.Apply(this, _snapshotIntentLog, pendingWork);
                var result = pendingWork.LastSuccess;
                if (!result)
                {
                    LogWarning($"Failed to apply intent log on snapshot for table {FullName} of version {Version}, creating new snapshot");
                    _snapshotIntentLog.Clear();
                }
                else
                {
                    if (IsVerboseEnabled) LogVerbose($"Applied intent log on snapshot for table {FullName} of version {Version} ({pendingWork.CurrentActions}<{pendingWork.TotalActions})");
                    _hasChanges = false;
                    _changedItems?.Clear();
                    _deletedItems?.Clear();
                    for (int i = 0; i < _subTables.Count; i++)
                    {
                        var subTable = _subTables[i];
                        var oldSnapshot = _cachedSnapshot.SubTables[i];
                        yield return subTable.CreateSnapshot(pendingWork);
                        var newSubTable = pendingWork.LastSnapshot;
                        if (newSubTable != oldSnapshot)
                        {
                            _cachedSnapshot._subTables[i] = newSubTable;
                        }
                        if (pendingWork.IsOverRunTime)
                        {
                            yield return null;
                        }
                    }
                    Version = _cachedSnapshot.Version;
                    pendingWork.LastSnapshot = _cachedSnapshot;
                    yield break;
                }
            }

            var subTableSnapshots = new List<IReadOnlyTable>();
            for (int i = 0; i < _subTables.Count; i++)
            {
                var table = _subTables[i];
                yield return table.CreateSnapshot(pendingWork);
                var snapshotTable = pendingWork.LastSnapshot;
                subTableSnapshots.Add(snapshotTable);
            }
            Version++;
            if (IsVerboseEnabled) LogVerbose($"Created new snapshot for table {FullName} of version {Version} with {_data.Count} items{(TrackingChanges ? $" ({Changed.Count} Changed/{Deleted.Count} Deleted)" : string.Empty)}, {_indexes.Count} indexes and {subTableSnapshots.Count} subtables");
            var data = _data.ToDictionary<KeyValuePair<T, ITrackingIndexed<T>>, T, IIndexed<T>>(kvp => kvp.Key, kvp => kvp.Value);
            var indexes = _indexes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(innerKvp => innerKvp.Key, innerKvp => new HashSet<T>(innerKvp.Value)));
            _cachedSnapshot = new SnapshotTable(this, subTableSnapshots, data, indexes);
            _hasChanges = false;
            _changedItems?.Clear();
            _deletedItems?.Clear();
            pendingWork.LastSnapshot = _cachedSnapshot;
        }
        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _data.Keys.GetEnumerator();
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.TrackChanges()
        {
            _changedItems ??= new HashSet<IIndexed<object>>();
            _deletedItems ??= new HashSet<IIndexed<object>>();
            if (!TrackingChanges)
            {
                _changedItems.Clear();
                _deletedItems.Clear();
                ((ITableBuilder<T>)this).OnInserted((i, m, t) =>
                {
                    lock (_changedItems)
                    {
                        _changedItems.Add(i);
                    }
                });
                ((ITableBuilder<T>)this).OnDeleted((i, m, t) =>
                {
                    lock (_deletedItems)
                    {
                        _deletedItems.Add(i);
                    }
                });
            }
            TrackingChanges = true;
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.WithListener(ITableListener<T> listener)
        {
            _listenersSet.Add(Guard.NotNull(listener, nameof(listener)));
            return this;
        }

        private sealed class SnapshotTable : IReadOnlyTable<T>
        {
            // Fields
            internal readonly Dictionary<T, IIndexed<T>> _data;
            internal readonly Dictionary<string, Dictionary<object, HashSet<T>>> _indexes;
            internal readonly HashSet<IIndexed<object>> _changedItems = new HashSet<IIndexed<object>>();
            internal readonly HashSet<IIndexed<object>> _deletedItems = new HashSet<IIndexed<object>>();
            internal readonly List<IReadOnlyTable> _subTables;

            // State
            private IIndexed<T>[] _snapshotData;

            // Properties
            public string Name { get; }
            public bool IsFiltered { get; }
            public IReadOnlyList<IReadOnlyTable> SubTables => _subTables;

            public string FullName { get; }

            public int Version { get; private set; }

            public bool TrackingChanges { get; private set; }

            public IReadOnlyCollection<IIndexed<object>> Changed => _changedItems;

            public IReadOnlyCollection<IIndexed<object>> Deleted => _deletedItems;

            public Type BaseEntityType => typeof(T);

            public bool IsSyncing { get; internal set; }

            public SnapshotTable(IReadOnlyTable table, List<IReadOnlyTable> subTables, Dictionary<T, IIndexed<T>> data, Dictionary<string, Dictionary<object, HashSet<T>>> indexes)
            {
                Name = table.Name;
                FullName = table.FullName;
                Version = table.Version;
                IsFiltered = table.IsFiltered;
                _subTables = subTables;
                TrackingChanges = table.TrackingChanges;
                _changedItems = table.TrackingChanges && table.Changed?.Count > 0 ? new HashSet<IIndexed<object>>(table.Changed) : new HashSet<IIndexed<object>>();
                _deletedItems = table.TrackingChanges && table.Deleted?.Count > 0 ? new HashSet<IIndexed<object>>(table.Deleted) : new HashSet<IIndexed<object>>();
                _data = data;
                _indexes = indexes;
            }

            public IReadOnlyCollection<T> Query<TSearch>(string property, TSearch search, string indexName = null)
            {
                property = Guard.NotNullOrEmpty(property, nameof(property));
                var fullIndexName = $"{indexName ?? "default"}{property}";
                if (_indexes.TryGetValue(fullIndexName, out var index) && index.TryGetValue(search, out var items))
                    return items;
                return Array.Empty<T>();
            }

            bool IReadOnlyTable.TryFind<T1>(T1 data, out IIndexed<T1> item)
            {
                if (data is T typedData)
                {
                    if (_data.TryGetValue(typedData, out var indexed))
                    {
                        item = (IIndexed<T1>)indexed;
                        return true;
                    }
                }
                item = null;
                return false;
            }

            public IEnumerable Apply(IDatabaseObject table, Queue<TableAction> intentLog, PendingWorkContext pendingWork)
            {
                if (intentLog is null || intentLog.Count == 0)
                {
                    pendingWork.LastSuccess = true;
                    yield break;
                }
                IsSyncing = true;
                _changedItems.Clear();
                _deletedItems.Clear();
                while (intentLog.TryDequeue(out var action))
                {
                    try
                    {
                        using (action)
                        {
                            pendingWork.LogWork();
                            switch (action.LogType)
                            {
                                case LogType.Upsert:
                                    var entity = action.Entity.TakeSnapshot();
                                    _data[action.Entity.Value] = entity;
                                    if (TrackingChanges)
                                    {
                                        _changedItems.Add(entity);
                                    }
                                    break;
                                case LogType.Delete:
                                    _data.Remove(action.Entity.Value);
                                    if (TrackingChanges)
                                    {
                                        var entityToDelete = action.Entity.TakeSnapshot();
                                        _deletedItems.Add(entityToDelete);
                                    }
                                    break;
                                case LogType.IndexUpdate:
                                    var index = _indexes[action.IndexName];
                                    if (!index.TryGetValue(action.IndexValue, out var set))
                                    {
                                        set = new HashSet<T>();
                                        index[action.IndexValue] = set;
                                    }
                                    set.Add(action.Entity.Value);
                                    break;
                                case LogType.IndexRemove:
                                    var removeIndex = _indexes[action.IndexName];
                                    if (removeIndex != null)
                                    {
                                        if (removeIndex.TryGetValue(action.IndexValue, out var removeSet))
                                        {
                                            _ = removeSet.Remove(action.Entity.Value);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error applying intent log to snapshot table {FullName}: {ex}");
                        intentLog.Clear();
                        pendingWork.LastSuccess = false;
                        yield break;
                    }

                    if (pendingWork.WaitForNextTick)
                    {
                        pendingWork.LastSuccess = true;
                        yield return null;
                    }
                }
                IsSyncing = false;
                _snapshotData = null;
                Version++;
                if (IsVerboseEnabled) LogVerbose($"Applied changes to table {FullName} snapshot");
                pendingWork.LastSuccess = true;
            }

            public IEnumerator<IIndexed<T>> GetEnumerator() => _data.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
            /// <inheritdoc/>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return _data.Keys.GetEnumerator();
            }
            /// <inheritdoc/>
            public IReadOnlyCollection<IIndexed<T>> GetSnapshot()
            {
                if (_snapshotData != null)
                {
                    return _snapshotData;
                }
                lock (_data)
                {
                    if (_snapshotData != null)
                    {
                        return _snapshotData;
                    }
                    _snapshotData = _data.Values.ToArray();
                    return _snapshotData;
                }
            }
        }

        private class TableAction : IPoolable, IDisposable
        {
            public LogType LogType;
            public ITrackingIndexed<T> Entity;
            public string IndexName;
            public object IndexValue;
            public void Dispose()
            {
                Toolkit.Pool<TableAction>.Return(this);
            }

            public void Reset()
            {
                Entity = null;
                IndexName = null;
                IndexValue = null;
            }
        }
    }
    /// <summary>
    /// Base class for typed tables.
    /// </summary>
    public abstract class Table : IReadOnlyTable
    {
        // State
        protected HashSet<IIndexed<object>> _changedItems;
        protected HashSet<IIndexed<object>> _deletedItems;

        // Properties
        /// <inheritdoc/>
        public abstract string FullName { get; }
        /// <inheritdoc/>
        public string Name { get; }
        /// <inheritdoc/>
        public bool IsFiltered { get; }
        /// <inheritdoc/>
        public int Version { get; protected set; }
        /// <inheritdoc/>
        public bool TrackingChanges { get; protected set; }
        /// <inheritdoc/>
        public abstract IReadOnlyList<IReadOnlyTable> SubTables { get; }
        /// <inheritdoc/>
        public abstract Type BaseEntityType { get; }
        /// <inheritdoc/>
        public IReadOnlyCollection<IIndexed<object>> Changed => (IReadOnlyCollection<IIndexed<object>>)_changedItems ?? Array.Empty<IIndexed<object>>();
        /// <inheritdoc/>
        public IReadOnlyCollection<IIndexed<object>> Deleted => (IReadOnlyCollection<IIndexed<object>>)_deletedItems ?? Array.Empty<IIndexed<object>>();
        /// <inheritdoc/>
        public bool IsSyncing => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class with the specified name and filter status.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="isFiltered">Indicates whether the table is filtered.</param>
        public Table(string name, bool isFiltered)
        {
            Name = Guard.NotNullOrEmpty(name, nameof(name));
            IsFiltered = isFiltered;
        }

        /// <summary>
        /// Tries to add a new item to the table. If an item with the same data already exists, it will be updated with the new data and the method will return true. If the item is successfully added, it will also return true.
        /// </summary>
        /// <typeparam name="T">The type to attempt to insert.</typeparam>
        /// <param name="item">The item to insert.</param>
        /// <returns>True if the item was successfully added or updated; otherwise, false.</returns>
        internal abstract bool TryAddOrUpdate<T>(ITrackingIndexed<T> item, IndexMetadata metadata) where T : class;

        /// <summary>
        /// Tries to add a new item to the table. If an item with the same data already exists, it will be updated with the new data and the method will return true. If the item is successfully added, it will also return true.
        /// </summary>
        /// <typeparam name="T">The type to attempt to insert.</typeparam>
        /// <param name="item">The item to insert.</param>
        /// <returns>True if the item was successfully added or updated; otherwise, false.</returns>
        /// <summary>
        /// Tries to delete an item from the table. If an item with the same data exists, it will be removed and the method will return true. Otherwise, it will return false.
        /// </summary>
        /// <typeparam name="T">The type of the item to delete.</typeparam>
        /// <param name="item">The item to delete.</param>
        /// <param name="metadata">Optional metadata associated with the item.</param>
        /// <returns>True if the item was successfully deleted; otherwise, false.</returns>
        internal abstract bool TryDelete<T>(ITrackingIndexed<T> item, IndexMetadata metadata) where T : class;
        /// <summary>
        /// Tries to retrieve an indexed item from the table based on the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve.</typeparam>
        /// <param name="data">The data to search for.</param>
        /// <param name="item">The retrieved item, if found.</param>
        /// <returns>True if the item was found; otherwise, false.</returns>
        internal abstract bool TryGet<T>(T data, out ITrackingIndexed<T> item) where T : class;
        /// <inheritdoc/>
        public bool TryFind<T>(T data, out IIndexed<T> item) where T : class
        {
            if (TryGet(data, out var trackingItem))
            {
                item = trackingItem;
                return true;
            }
            item = null;
            return false;
        }

        /// <summary>
        /// Creates an immutable snapshot of this table's current state.
        /// </summary>
        internal abstract IEnumerable CreateSnapshot(PendingWorkContext pendingWork);
    }

    internal enum LogType
    {
        Upsert,
        Delete,
        IndexUpdate,
        IndexRemove
    }
    internal class PendingWorkContext : CooperativeWorkContext
    {
        public bool LastSuccess;
        public IReadOnlyTable LastSnapshot;
    }
}