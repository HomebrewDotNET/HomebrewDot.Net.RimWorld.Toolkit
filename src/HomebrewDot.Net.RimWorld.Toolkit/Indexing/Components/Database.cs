using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Eventing.Models;
using HomebrewDot.Net.RimWorld.Extensions;
using HomebrewDot.Net.RimWorld.Generic.Models;
using RimWorld;
using Verse;
using static HomebrewDot.Net.RimWorld.Indexing.Components.Database;
using static HomebrewDot.Net.RimWorld.Toolkit;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.RimWorld.Indexing.Components
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
            if(!_indexedCreator.TryGetValue(type, out var creator))
            {
                lock(_indexedCreator)
                {
                    if(!_indexedCreator.TryGetValue(type, out creator))
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
            var targetConstructor = Expression.GetConstructorForGeneric(type, () => new TrackingIndexed<object>(null, null));
            var newExpression = System.Linq.Expressions.Expression.New(targetConstructor, convertedInput, inputMetadataParameter);

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
        private readonly Dictionary<Type, Table> _tables = new Dictionary<Type, Table>();
        private readonly Dictionary<string, Table> _tablesByName = new Dictionary<string, Table>(StringComparer.OrdinalIgnoreCase);
        private Action<IDatabase, IWriteableIndexed<object>> _onInserting;
        private Action<IDatabase, IIndexed<object>> _onInserted;
        private Action<IDatabase, IWriteableIndexed<object>, IReadOnlyDictionary<string, object>> _onDeleting;
        private Action<IDatabase, IIndexed<object>, IReadOnlyDictionary<string, object>> _onDeleted;
        private IReadOnlyDatabase _cachedSnapshot;

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
        public IIndexed<T> Find<T>(T data) where T : class
        {
            return ExecuteAction(() =>
            {
                foreach (var table in _tables.Values)
                {
                    if (table.TryGet(data, out var item))
                    {
                        return item;
                    }
                }
                return null;
            });
        }
        /// <inheritdoc/>
        public bool Upsert<T>(T item, IReadOnlyDictionary<string, object> metadata) where T : class
        {
            item = Guard.NotNull(item, nameof(item));
            metadata ??= NullDictionary<string, object>.Instance;

            return ExecuteAction(() =>
            {
                bool anyInserted = false;
                var foundItem = Find(item);
                ITrackingIndexed<T> trackedItem = null;
                if (foundItem is ITrackingIndexed<T> existingTracked)
                {
                    trackedItem = existingTracked;
                }
                else
                {
                    var creator = GetCreatorForType(item.GetType());
                    trackedItem = (ITrackingIndexed<T>)creator(item, metadata);
                }
                Invoking.Safe(() => _onInserting?.Invoke(this, trackedItem));
                foreach (var table in _tables.Values)
                {
                    if (table.TryAddOrUpdate(trackedItem))
                    {
                        anyInserted = true;
                    }
                }

                if (anyInserted)
                {
                    HasChanges = true;
                    _cachedSnapshot = null;
                    Invoking.Safe(() => _onInserted?.Invoke(this, trackedItem));
                    return true;
                }
                return false;
            });
        }
        /// <inheritdoc/>
        public bool Delete<T>(T item, IReadOnlyDictionary<string, object> metadata) where T : class
        {
            item = Guard.NotNull(item, nameof(item));
            return ExecuteAction(() =>
            {
                var foundItem = Find(item);
                bool anyDeleted = false;
                TrackingIndexed<T> typedItem = null;
                if (foundItem is TrackingIndexed<T> existingTracked)
                {
                    typedItem = existingTracked;
                    metadata ??= NullDictionary<string, object>.Instance;
                    Invoking.Safe(() => _onDeleting?.Invoke(this, typedItem, metadata));
                    foreach (var table in _tables.Values)
                    {
                        if (table.TryDelete(typedItem, metadata))
                        {
                            anyDeleted = true;
                        }
                    }
                }
                if (anyDeleted)
                {
                    Invoking.Safe(() => _onDeleted?.Invoke(this, typedItem, metadata));
                    HasChanges = true;
                    _cachedSnapshot = null;
                    return true;
                }
                return false;
            });
        }

        /// <inheritdoc/>
        public IReadOnlyTable<T> GetTable<T>(string name) where T : class
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            return ExecuteAction(() =>
            {
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
            });
        }
        /// <inheritdoc/>
        public IReadOnlyTable GetTable(string name)
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            return ExecuteAction(() =>
            {
                if (name.Contains(TableNameSeparator))
                {
                    var subNames = name.Split([TableNameSeparator], StringSplitOptions.RemoveEmptyEntries);
                    if (subNames.Length > 1)
                    {
                        var rootTableName = subNames[0];
                        var rootTable = GetTable(rootTableName);
                        for (int i = 1; i < subNames.Length; i++)
                        {
                            if (rootTable == null)
                            {
                                return null;
                            }
                            rootTable = rootTable.SubTables.FirstOrDefault(st => st.Name.Equals(subNames[i], StringComparison.OrdinalIgnoreCase));
                        }
                        return rootTable;
                    }
                }
                if (_tablesByName.TryGetValue(name, out var table))
                {
                    return table;
                }
                return null;
            });
        }
        /// <inheritdoc/>
        public IEnumerable<IReadOnlyTable> GetTables()
        {
            return ExecuteAction(() => _tables.Values.ToArray());
        }
        /// <inheritdoc/>
        public IEnumerable<IReadOnlyTable<T>> GetTables<T>() where T : class
        {
            var type = typeof(T);

            return ExecuteAction(() =>
                {
                    var assignableTableTypes = _tables.Keys.Where(t => type.IsAssignableFrom(t)).ToArray();
                    var tables = new List<IReadOnlyTable<T>>();
                    for (int i = 0; i < assignableTableTypes.Length; i++)
                    {
                        var tableType = assignableTableTypes[i];
                        if (_tables[tableType] is IReadOnlyTable<T> typedTable)
                        {
                            tables.Add(typedTable);
                        }
                    }
                    return tables;
                });
        }
        /// <inheritdoc/>
        public IReadOnlyCollection<IIndexed<T>> Query<T, TSearch>(string property, TSearch search, string tableName = null, string indexName = null) where T : class
        {
            property = Guard.NotNullOrEmpty(property, nameof(property));

            return ExecuteAction(() =>
            {
                var tables = string.IsNullOrEmpty(tableName) ? GetTables<T>().ToArray() : new[] { GetTable<T>(tableName) };
                HashSet<IIndexed<T>> results = null;
                foreach (var table in tables)
                {
                    var tableResults = table.Query(property, search, indexName);
                    if (tables.Length == 1)
                    {
                        return tableResults;
                    }
                    results ??= new HashSet<IIndexed<T>>();
                    results.UnionWith(tableResults);
                }
                return results;
            });
        }

        /// <inheritdoc/>
        public IReadOnlyDatabase AsReadOnly()
        {
            lock (_lock)
            {
                if (!HasChanges && _cachedSnapshot != null)
                {
                    return _cachedSnapshot;
                }

                var snapshots = new List<IReadOnlyTable>();
                foreach (var table in _tables.Values)
                {
                   snapshots.Add(table.CreateSnapshot());
                }

                var snapshotsByName = new Dictionary<string, IReadOnlyTable>(StringComparer.OrdinalIgnoreCase);
                foreach (var snapshot in snapshots)
                {
                    snapshotsByName[snapshot.Name] = snapshot;
                }

                HasChanges = false;
                Version++;
                _cachedSnapshot = new ReadOnlyDatabaseSnapshot(Version, snapshots, snapshotsByName);
                return _cachedSnapshot;
            }
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
                    _onInserting = null;
                    _onInserted = null;
                    _onDeleting = null;
                    _onDeleted = null;

                    schemaBuilder(this);
                }
            }
            finally
            {
                IsDeploying = false;
            }
        }

        private T ExecuteAction<T>(Func<T> action)
        {
            if (IsDeploying)
            {
                lock (_lock)
                {
                    return action();
                }
            }
            return action();
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
                            ? new Table<T>(this, name, predicate)
                            : new Table<T>(this, name, false);
                        _tablesByName[name] = newTable;
                        _tables[typeof(T)] = newTable;
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
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnInserting(Action<IDatabase, IWriteableIndexed<object>> onInserting)
        {
            onInserting = Guard.NotNull(onInserting, nameof(onInserting));

            if (_onInserting is null)
            {
                _onInserting = onInserting;
            }
            else
            {
                _onInserting += onInserting;
            }
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnInserted(Action<IDatabase, IIndexed<object>> onInserted)
        {
            onInserted = Guard.NotNull(onInserted, nameof(onInserted));
            if (_onInserted is null)
            {
                _onInserted = onInserted;
            }
            else
            {
                _onInserted += onInserted;
            }
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnDeleting(Action<IDatabase, IWriteableIndexed<object>, IReadOnlyDictionary<string, object>> onDeleting)
        {
            onDeleting = Guard.NotNull(onDeleting, nameof(onDeleting));
            if (_onDeleting is null)
            {
                _onDeleting = onDeleting;
            }
            else
            {
                _onDeleting += onDeleting;
            }
            return this;
        }
        /// <inheritdoc/>
        IDatabaseSchemaBuilder IDatabaseSchemaBuilder.OnDeleted(Action<IDatabase, IIndexed<object>, IReadOnlyDictionary<string, object>> onDeleted)
        {
            onDeleted = Guard.NotNull(onDeleted, nameof(onDeleted));
            if (_onDeleted is null)
            {
                _onDeleted = onDeleted;
            }
            else
            {
                _onDeleted += onDeleted;
            }
            return this;
        }

        internal interface ITrackingIndexed<out T> : IIndexed<T>, IWriteableIndexed<T> where T : class
        {
            Dictionary<string, object> IndexedBy { get; }
        }

        internal class TrackingIndexed<T> : Indexed<T>, ITrackingIndexed<T>, IWriteableIndexed<T> where T : class
        {
            // Fields
            private IReadOnlyDictionary<string, object> _metadata;
            private readonly object _lock = new object();
            private Dictionary<string, object> _mutableMetadata;
            private Dictionary<string, object> _indexedBy;
            private int? _hashCode;

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

            public TrackingIndexed(T value, IReadOnlyDictionary<string, object> metadata) : base(value)
            {
                _metadata = metadata;
            }
            /// <inheritdoc/>
            public bool Set<TData>(string propertyName, TData value)
            {
                propertyName = Guard.NotNullOrEmpty(propertyName, nameof(propertyName));
                lock (_lock)
                {
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
            }
            /// <inheritdoc/>
            public bool Unset(string propertyName)
            {
                lock (_lock)
                {
                    if (_metadata is not null)
                    {
                        return false;
                    }

                    return _mutableMetadata.Remove(propertyName);
                }
            }

            public override int GetHashCode()
            {
                if (_hashCode.HasValue) return _hashCode.Value;
                _hashCode = base.GetHashCode();
                return _hashCode.Value;
            }
        }

        private sealed class ReadOnlyDatabaseSnapshot : IReadOnlyDatabase
        {
            private readonly IReadOnlyTable[] _tables;
            private readonly Dictionary<string, IReadOnlyTable> _tablesByName;

            public int Version { get; }

            public ReadOnlyDatabaseSnapshot(int version, IEnumerable<IReadOnlyTable> tables, Dictionary<string, IReadOnlyTable> tablesByName)
            {
                Version = version;
                _tables = tables.ToArray();
                _tablesByName = tablesByName;
            }

            public IIndexed<T> Find<T>(T data) where T : class
            {
                foreach(var table in _tables)
                {
                    if(table.TryFind(data, out var indexed))
                    {
                        return indexed;
                    }
                }
                return null;
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
                if (name.Contains(Database.TableNameSeparator))
                {
                    var subNames = name.Split(new[] { Database.TableNameSeparator }, StringSplitOptions.RemoveEmptyEntries);
                    if (subNames.Length > 1)
                    {
                        IReadOnlyTable current = GetTable(subNames[0]);
                        for (int i = 1; i < subNames.Length; i++)
                        {
                            if (current == null) return null;
                            current = current.SubTables.FirstOrDefault(st => st.Name.Equals(subNames[i], StringComparison.OrdinalIgnoreCase));
                        }
                        return current;
                    }
                }
                _tablesByName.TryGetValue(name, out var table);
                return table;
            }

            public IEnumerable<IReadOnlyTable> GetTables() => _tables;

            public IEnumerable<IReadOnlyTable<T>> GetTables<T>() where T : class
                => _tables.OfType<IReadOnlyTable<T>>();

            public IReadOnlyCollection<IIndexed<T>> Query<T, TSearch>(string property, TSearch search, string tableName = null, string indexName = null) where T : class
            {
                property = Guard.NotNullOrEmpty(property, nameof(property));
                var tables = string.IsNullOrEmpty(tableName) ? GetTables<T>().ToArray() : new[] { GetTable<T>(tableName) };
                var results = new HashSet<IIndexed<T>>();
                foreach (var table in tables)
                {
                    var tableResults = table.Query(property, search, indexName);
                    if (tables.Length == 1)
                        return tableResults;
                    results.UnionWith(tableResults);
                }
                return results;
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
        private readonly List<IReadOnlyTable> _subTables = new List<IReadOnlyTable>();
        private readonly Dictionary<T, ITrackingIndexed<T>> _data = new Dictionary<T, ITrackingIndexed<T>>();
        private readonly Predicate<T> _filter;
        private readonly Dictionary<string, Dictionary<object, HashSet<ITrackingIndexed<T>>>> _indexes = new Dictionary<string, Dictionary<object, HashSet<ITrackingIndexed<T>>>>();
        private readonly Dictionary<string, HashSet<ITrackingIndexed<T>>> _boolIndexes = new Dictionary<string, HashSet<ITrackingIndexed<T>>>();
        private Action<IDatabase, IReadOnlyTable<T>, IWriteableIndexed<T>> _onInserting;
        private Action<IDatabase, IReadOnlyTable<T>, IIndexed<T>> _onInserted;
        private Action<IDatabase, IReadOnlyTable<T>, IWriteableIndexed<T>, IReadOnlyDictionary<string, object>> _onDeleting;
        private Action<IDatabase, IReadOnlyTable<T>, IIndexed<T>, IReadOnlyDictionary<string, object>> _onDeleted;
        private IDatabase _owner;
        private bool _hasChanges = true;
        private SnapshotTable _cachedSnapshot;

        // Properties
        /// <summary>
        /// The parent table of this table, if it is a subtable. If this table is a root table, this property will be <c>null</c>.
        /// </summary>
        public IReadOnlyTable Parent { get; private set; }
        /// <inheritdoc/>
        public override IReadOnlyList<IReadOnlyTable> SubTables => _subTables;

        /// <inheritdoc cref="Table{T}"/>
        /// <param name="owner">The database that owns this table.</param>
        /// <param name="name">The name of the table.</param>
        /// <param name="isFiltered">Indicates whether the table is filtered.</param>
        public Table(IDatabase owner, string name, bool isFiltered) : base(name, isFiltered)
        {
            _owner = Guard.NotNull(owner, nameof(owner));
        }
        /// <inheritdoc cref="Table{T}"/>
        /// <param name="owner">The database that owns this table.</param>
        /// <param name="name">The name of the table.</param>
        /// <param name="filter">Predicate used to filter the data in the table. If provided, only data that satisfies the predicate will be stored in the table.</param>
        public Table(IDatabase owner, string name, Predicate<T> filter) : base(name, filter != null)
        {
            _owner = Guard.NotNull(owner, nameof(owner));
            _filter = filter;
        }

        /// <inheritdoc/>
        internal override bool TryGet<T1>(T1 data, out ITrackingIndexed<T1> item)
        {
            if(data is T typedData)
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
        internal override bool TryAddOrUpdate<T1>(ITrackingIndexed<T1> item)
        {
            item = Guard.NotNull(item, nameof(item));
            if (item is ITrackingIndexed<T> tableItem)
            {

                if (_filter != null && !_filter(tableItem.Value))
                {
                    return TryDelete(tableItem, tableItem.Metadata);
                }
                Invoking.Safe(() => _onInserting?.Invoke(_owner, this, tableItem));
                _data[tableItem.Value] = tableItem;
                _hasChanges = true;
                foreach (var subTable in SubTables.OfType<Table>())
                {
                    _ = subTable.TryAddOrUpdate(tableItem);
                }
                Invoking.Safe(() => _onInserted?.Invoke(_owner, this, tableItem));
                return true;
            }
            return false;
        }
        /// <inheritdoc/>
        internal override bool TryDelete<T1>(ITrackingIndexed<T1> item, IReadOnlyDictionary<string, object> metadata)
        {
            if (item is ITrackingIndexed<T> tableItem)
            {
                return Delete(tableItem, metadata);
            }
            return false;
        }

        private bool Delete(ITrackingIndexed<T> item, IReadOnlyDictionary<string, object> metadata)
        {
            Invoking.Safe(() => _onDeleting?.Invoke(_owner, this, item, metadata));
            if (_data.Remove(item.Value))
            {
                _hasChanges = true;
                foreach (var subTable in SubTables.OfType<Table>())
                {
                    _ = subTable.TryDelete(item, metadata);
                }
                Invoking.Safe(() => _onDeleted?.Invoke(_owner, this, item, metadata));
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IIndexed<T>> Query<TSearch>(string property, TSearch search, string indexName = null)
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
            return Array.Empty<IIndexed<T>>();
        }

        private string GetFullIndexName(string indexName, string propertyName)
        {
            return $"{indexName ?? "default"}{propertyName}";
        }

        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnDeleted(Action<IDatabase, IReadOnlyTable<T>, IIndexed<T>, IReadOnlyDictionary<string, object>> onDeleted)
        {
            onDeleted = Guard.NotNull(onDeleted, nameof(onDeleted));
            if (_onDeleted is null)
            {
                _onDeleted = onDeleted;
            }
            else
            {
                _onDeleted += onDeleted;
            }
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnDeleting(Action<IDatabase, IReadOnlyTable<T>, IWriteableIndexed<T>, IReadOnlyDictionary<string, object>> onDeleting)
        {
            onDeleting = Guard.NotNull(onDeleting, nameof(onDeleting));
            if (_onDeleting is null)
            {
                _onDeleting = onDeleting;
            }
            else
            {
                _onDeleting += onDeleting;
            }
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnInserted(Action<IDatabase, IReadOnlyTable<T>, IIndexed<T>> onInserted)
        {
            onInserted = Guard.NotNull(onInserted, nameof(onInserted));
            if (_onInserted is null)
            {
                _onInserted = onInserted;
            }
            else
            {
                _onInserted += onInserted;
            }
            return this;
        }
        /// <inheritdoc/>
        ITableBuilder<T> ITableBuilder<T>.OnInserting(Action<IDatabase, IReadOnlyTable<T>, IWriteableIndexed<T>> onInserting)
        {
            onInserting = Guard.NotNull(onInserting, nameof(onInserting));
            if (_onInserting is null)
            {
                _onInserting = onInserting;
            }
            else
            {
                _onInserting += onInserting;
            }
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
            _indexes.Add(fullIndexName, new Dictionary<object, HashSet<ITrackingIndexed<T>>>());
            Log($"Added {(filter != null ? "filtered " : string.Empty)}index {fullIndexName} on property {propertyName} to table {Name}");

            var self = ((ITableBuilder<T>)this);
            self.OnInserted((db, table, indexed) =>
            {
                var indexValue = propertySelector(indexed);
                Dictionary<object, HashSet<ITrackingIndexed<T>>> index;
                lock (_indexes)
                {
                    if (!_indexes.TryGetValue(fullIndexName, out index))
                    {
                        return;
                    }
                }
                if (indexed is ITrackingIndexed<T> tracked)
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
                                        existingSet.Remove(tracked);
                                    }
                                }
                            }
                            tracked.IndexedBy.Remove(propertyName);
                        }

                        if(filter != null && !filter(tracked.Value))
                        {
                            return;
                        }

                        if (indexValue is not null)
                        {
                            HashSet<ITrackingIndexed<T>> set;
                            lock (index)
                            {
                                if (!index.TryGetValue(indexValue, out set))
                                {
                                    set = new HashSet<ITrackingIndexed<T>>();
                                    index[indexValue] = set;
                                }
                            }
                            lock (set)
                            {
                                set.Add(tracked);
                            }
                            tracked.IndexedBy.Add(propertyName, indexValue);
                        }
                    }
                }
            }).OnDeleted((db, table, indexed, metadata) =>
            {
                Dictionary<object, HashSet<ITrackingIndexed<T>>> index;
                lock (_indexes)
                {
                    if (!_indexes.TryGetValue(fullIndexName, out index))
                    {
                        return;
                    }
                }
                if (indexed is ITrackingIndexed<T> tracked)
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
                                        existingSet.Remove(tracked);
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
                var newTable = filter != null ? new Table<TSub>(_owner, name, filter) : new Table<TSub>(_owner, name, true);
                _subTables.Add(newTable);
                Log($"Added {(filter != null ? "filtered " : string.Empty)}sub table {name} of type {typeof(TSub).Name} to table {Name} of type {typeof(T).Name}");

                existingSubTable = newTable;
            }
            if (existingSubTable is ITableBuilder<TSub> subTableBuilder)
            {
                tableBuilder?.Invoke(subTableBuilder);
            }
            else
            {
                throw new InvalidOperationException($"Subtable with name '{name}' already exists but is not of the expected type '{typeof(TSub).FullName}'. Multiple source might be using the same name but different types which is a conflict");
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
                var newTable = new Table<T>(_owner, name, filter);
                _subTables.Add(newTable);
                Log($"Added {(filter != null ? "filtered " : string.Empty)}sub table {name} of type {typeof(T).Name} to table {Name} of same type");

                existingSubTable = newTable;
            }
            if (existingSubTable is ITableBuilder<T> subTableBuilder)
            {
                tableBuilder?.Invoke(subTableBuilder);
            }
            else
            {
                throw new InvalidOperationException($"Subtable with name '{name}' already exists but is not of the expected type '{typeof(T).FullName}'. Multiple source might be using the same name but different types which is a conflict");
            }
            return this;
        }
        /// <inheritdoc/>
        IEnumerator<IIndexed<T>> IEnumerable<IIndexed<T>>.GetEnumerator()
            => _data.Values.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => _data.Values.GetEnumerator();

        internal override IReadOnlyTable CreateSnapshot()
        {
            if (!_hasChanges && _cachedSnapshot != null)
            {
                return _cachedSnapshot;
            }

            var dataSnapshot = new Dictionary<T, IIndexed<T>>();
            foreach (var kvp in _data)
            {
                lock (kvp.Value)
                {
                    dataSnapshot[kvp.Key] = kvp.Value;
                }
            }

            var indexSnapshot = new Dictionary<string, Dictionary<object, IIndexed<T>[]>>();
            foreach (var kvp in _indexes)
            {
                var indexCopy = new Dictionary<object, IIndexed<T>[]>();
                foreach (var entry in kvp.Value)
                {
                    lock (entry.Value)
                    {
                        indexCopy[entry.Key] = entry.Value.Cast<IIndexed<T>>().ToArray();
                    }
                }
                indexSnapshot[kvp.Key] = indexCopy;
            }

            var subTableSnapshots = _subTables
                .Select(st => st is Table baseTable ? baseTable.CreateSnapshot() : st)
                .ToList()
                .AsReadOnly();

            _cachedSnapshot = new SnapshotTable(Name, IsFiltered, dataSnapshot, indexSnapshot, subTableSnapshots);
            _hasChanges = false;
            return _cachedSnapshot;
        }
        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _data.Keys.GetEnumerator();
        }

        private sealed class SnapshotTable : IReadOnlyTable<T>
        {
            private readonly Dictionary<T, IIndexed<T>> _data;
            private readonly Dictionary<string, Dictionary<object, IIndexed<T>[]>> _indexes;

            public string Name { get; }
            public bool IsFiltered { get; }
            public IReadOnlyList<IReadOnlyTable> SubTables { get; }

            public SnapshotTable(string name, bool isFiltered, Dictionary<T, IIndexed<T>> data, Dictionary<string, Dictionary<object, IIndexed<T>[]>> indexes, IReadOnlyList<IReadOnlyTable> subTables)
            {
                Name = name;
                IsFiltered = isFiltered;
                _data = data;
                _indexes = indexes;
                SubTables = subTables;
            }

            public IReadOnlyCollection<IIndexed<T>> Query<TSearch>(string property, TSearch search, string indexName = null)
            {
                property = Guard.NotNullOrEmpty(property, nameof(property));
                var fullIndexName = $"{indexName ?? "default"}{property}";
                if (_indexes.TryGetValue(fullIndexName, out var index) && index.TryGetValue(search, out var items))
                    return items;
                return Array.Empty<IIndexed<T>>();
            }

            bool IReadOnlyTable.TryFind<T1>(T1 data, out IIndexed<T1> item)
            {
                if(data is T typedData)
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

            public IEnumerator<IIndexed<T>> GetEnumerator() => _data.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
            /// <inheritdoc/>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return _data.Keys.GetEnumerator();
            }
        }
    }
    /// <summary>
    /// Base class for typed tables.
    /// </summary>
    public abstract class Table : IReadOnlyTable
    {
        /// <inheritdoc/>
        public string Name { get; }
        /// <inheritdoc/>
        public bool IsFiltered { get; }
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
        internal abstract bool TryAddOrUpdate<T>(ITrackingIndexed<T> item) where T : class;
        /// <summary>
        /// Tries to delete an item from the table. If an item with the same data exists, it will be removed and the method will return true. Otherwise, it will return false.
        /// </summary>
        /// <typeparam name="T">The type of the item to delete.</typeparam>
        /// <param name="item">The item to delete.</param>
        /// <param name="metadata">Optional metadata associated with the item.</param>
        /// <returns>True if the item was successfully deleted; otherwise, false.</returns>
        internal abstract bool TryDelete<T>(ITrackingIndexed<T> item, IReadOnlyDictionary<string, object> metadata) where T : class;
        /// <summary>
        /// Tries to retrieve an indexed item from the table based on the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve.</typeparam>
        /// <param name="data">The data to search for.</param>
        /// <param name="item">The retrieved item, if found.</param>
        /// <returns>True if the item was found; otherwise, false.</returns>
        internal abstract bool TryGet<T>(T data, out ITrackingIndexed<T> item) where T : class;
        /// <inheritdoc/>
        bool IReadOnlyTable.TryFind<T>(T data, out IIndexed<T> item)
        {
            if(TryGet(data, out var trackingItem))
            {
                item = trackingItem;
                return true;
            }
            item = null;
            return false;
        }
        /// <inheritdoc/>
        public abstract IReadOnlyList<IReadOnlyTable> SubTables { get; }

        /// <summary>
        /// Creates an immutable snapshot of this table's current state.
        /// </summary>
        internal abstract IReadOnlyTable CreateSnapshot();
    }
}