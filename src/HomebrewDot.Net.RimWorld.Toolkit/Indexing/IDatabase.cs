using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Indexing
{
    /// <summary>
    /// Database containing tables with indexed data.
    /// </summary>
    public interface IDatabase : IReadOnlyDatabase
    {
        /// <summary>
        /// Inserts or updates an item in the database. If an item with the same index already exists, it will be updated with the new value. If no such item exists, a new entry will be created.
        /// When no tables accept the item, false is returned. When the item is successfully inserted or updated, true is returned.
        /// </summary>
        /// <typeparam name="T">The type of data to upsert.</typeparam>
        /// <param name="item">The indexed item to upsert.</param>
        /// <param name="metadata">Optional metadata to be passed to the inserting/upserting callbacks. Will be used as <see cref="IIndexed{T}.Metadata"/></param>
        /// <returns>True if the item was successfully upserted; otherwise, false.</returns>
        bool Upsert<T>(T item, IReadOnlyDictionary<string, object> metadata) where T : class;
        /// <summary>
        /// Deletes an item from the database. If the item is successfully deleted, true is returned. If the item does not exist in the database, false is returned.
        /// </summary>
        /// <typeparam name="T">The type of data to delete.</typeparam>
        /// <param name="item">The indexed item to delete.</param>
        /// <param name="metadata">Optional metadata to be passed to the deleting callbacks.</param>
        /// <returns>True if the item was successfully deleted; otherwise, false.</returns>
        bool Delete<T>(T item, IReadOnlyDictionary<string, object> metadata) where T : class;

        /// <summary>
        /// Sets up the schema using the provided <paramref name="schemaBuilder"/>. 
        /// Acts as a reset for the database, clearing all existing data and replacing it with the new schema defined by the <paramref name="schemaBuilder"/>.
        /// </summary>
        /// <param name="schemaBuilder">Delegate used to configure the database schema.</param>
        void Deploy(Action<IDatabaseSchemaBuilder> schemaBuilder);
        /// <summary>
        /// Converts the current instance of <see cref="IDatabase"/> into a read-only thread safe version. 
        /// The returned <see cref="IReadOnlyDatabase"/> will reflect the current state of the database at the time of conversion, but will not allow any modifications to the data. 
        /// </summary>
        /// <returns></returns>
        IReadOnlyDatabase AsReadOnly();
    }

    /// <summary>
    /// Used to configure the schema of a <see cref="IDatabase"/>.
    /// </summary>
    public interface IDatabaseSchemaBuilder
    {
        /// <summary>
        /// Defines a table in the database. The table will contain items of type T, and the provided <paramref name="tableBuilder"/> will be used to configure the table's indexes and sub-tables.
        /// </summary>
        /// <typeparam name="T">The type of data the table will contain.</typeparam>
        /// <param name="name">The name of the table.</param>
        /// <param name="tableBuilder">An action to configure the table's indexes and sub-tables.</param>
        /// <param name="predicate">Optional filter to create a filtered table</param>
        /// <returns>The current <see cref="IDatabaseSchemaBuilder"/> instance for chaining.</returns>
        IDatabaseSchemaBuilder WithTable<T>(string name, Action<ITableBuilder<T>> tableBuilder = null, Predicate<T> predicate = null) where T : class;
        /// <summary>
        /// Defines a callback to be invoked before an item is inserted into the database.
        /// </summary>
        /// <param name="onInserting">The delegate that will be called before an item is inserted.</param>
        /// <returns>The current <see cref="IDatabaseSchemaBuilder"/> instance for chaining.</returns>
        IDatabaseSchemaBuilder OnInserting(Action<IDatabase, IWriteableIndexed<object>> onInserting);
        /// <summary>
        /// Defines a callback to be invoked after an item has been inserted into the database.
        /// </summary>
        /// <param name="onInserted">The delegate that will be called after an item has been inserted.</param>
        /// <returns>The current <see cref="IDatabaseSchemaBuilder"/> instance for chaining.</returns>
        IDatabaseSchemaBuilder OnInserted(Action<IDatabase, IIndexed<object>> onInserted);
        /// <summary>
        /// Defines a callback to be invoked before an item is deleted from the database.
        /// </summary>
        /// <param name="onDeleting">The delegate that will be called before an item is deleted.</param>
        /// <returns>The current <see cref="IDatabaseSchemaBuilder"/> instance for chaining.</returns>
        IDatabaseSchemaBuilder OnDeleting(Action<IDatabase, IWriteableIndexed<object>, IReadOnlyDictionary<string, object>> onDeleting);
        /// <summary>
        /// Defines a callback to be invoked after an item has been deleted from the database.
        /// </summary>
        /// <param name="onDeleted">The delegate that will be called after an item has been deleted.</param>
        /// <returns>The current <see cref="IDatabaseSchemaBuilder"/> instance for chaining.</returns>
        IDatabaseSchemaBuilder OnDeleted(Action<IDatabase, IIndexed<object>, IReadOnlyDictionary<string, object>> onDeleted);
    }
    /// <summary>
    /// Used to configure the schema of a table in a <see cref="IDatabase"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITableBuilder<T> where T : class
    {
        /// <summary>
        /// Defines a sub-table within the current table. The sub-table will contain items of type TSub, and the provided <paramref name="tableBuilder"/> will be used to configure the sub-table's indexes and further nested sub-tables. The <paramref name="converter"/> function is used to convert an item of type T from the parent table into an item of type TSub for the sub-table.
        /// </summary>
        /// <typeparam name="TSub">The type of items in the sub-table.</typeparam>
        /// <param name="name">The name of the sub-table.</param>
        /// <param name="tableBuilder">An instance of <see cref="ITableBuilder{TSub}"/> to configure the sub-table's schema.</param>
        /// <param name="filter">An optional predicate to filter items for the sub-table.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> WithSubTable<TSub>(string name, Predicate<TSub> filter = null, Action<ITableBuilder<TSub>> tableBuilder = null) where TSub : class, T;
        /// <summary>
        /// Defines a sub-table within the current table. The sub-table will contain items of type TSub, and the provided <paramref name="tableBuilder"/> will be used to configure the sub-table's indexes and further nested sub-tables. The <paramref name="converter"/> function is used to convert an item of type T from the parent table into an item of type TSub for the sub-table.
        /// </summary>
        /// <typeparam name="TSub">The type of items in the sub-table.</typeparam>
        /// <param name="name">The name of the sub-table.</param>
        /// <param name="tableBuilder">An instance of <see cref="ITableBuilder{TSub}"/> to configure the sub-table's schema.</param>
        /// <param name="filter">An optional predicate to filter items for the sub-table.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> WithSubTable(string name, Predicate<T> filter, Action<ITableBuilder<T>> tableBuilder = null);
        /// <summary>
        /// Defines a callback to be invoked before an item is inserted into the table.
        /// </summary>
        /// <param name="onInserting">The delegate that will be called before an item is inserted.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> OnInserting(Action<IDatabase, IReadOnlyTable<T>, IWriteableIndexed<T>> onInserting);
        /// <summary>
        /// Defines a callback to be invoked after an item has been inserted into the table.
        /// </summary>
        /// <param name="onInserted">The delegate that will be called after an item has been inserted.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> OnInserted(Action<IDatabase, IReadOnlyTable<T>, IIndexed<T>> onInserted);
        /// <summary>
        /// Defines a callback to be invoked before an item is deleted from the table.
        /// </summary>
        /// <param name="onDeleting">The delegate that will be called before an item is deleted.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> OnDeleting(Action<IDatabase, IReadOnlyTable<T>, IWriteableIndexed<T>, IReadOnlyDictionary<string, object>> onDeleting);
        /// <summary>
        /// Defines a callback to be invoked after an item has been deleted from the table.
        /// </summary>
        /// <param name="onDeleted">The delegate that will be called after an item has been deleted.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> OnDeleted(Action<IDatabase, IReadOnlyTable<T>, IIndexed<T>, IReadOnlyDictionary<string,object>> onDeleted);
        /// <summary>
        /// Defines an index on the table. The index will be named <paramref name="name"/>, and the provided <paramref name="propertySelector"/> function will be used to extract the indexed property from items in the table. The optional <paramref name="filter"/> predicate can be used to specify a condition that items must satisfy to be included in the index. If no filter is provided, all items will be included in the index.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property to be indexed.</typeparam>
        /// <param name="name">Optional name for the index.</param>
        /// <param name="propertyName">The name of the property to be indexed. This will be used by searches</param>
        /// <param name="propertySelector">A function to extract the indexed property from an item.</param>
        /// <param name="filter">An optional predicate to filter items for the index.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> WithIndex<TProperty>(string propertyName, Func<IIndexed<T>, TProperty> propertySelector, Predicate<T> filter = null, string name = null);
        /// <summary>
        /// Defines a boolean index on the table. The index will be named <paramref name="name"/>, and the provided <paramref name="propertySelector"/> function will be used to determine whether items in the table are included in the index. Items for which the function returns true will be included in the index, while items for which it returns false will be excluded.
        /// Optimized for cases where the indexed property is a boolean condition, allowing for efficient indexing of items that satisfy the condition without needing to extract a specific property value.
        /// </summary>
        /// <param name="name">Optional name for the index.</param>
        /// <param name="propertyName">The name of the property to be indexed. This will be used by searches</param>
        /// <param name="propertySelector">A function to determine whether an item should be included in the index.</param>
        /// <returns>The current <see cref="ITableBuilder{T}"/> instance for chaining.</returns>
        ITableBuilder<T> WithIndex(string propertyName, Func<IIndexed<T>, bool> propertySelector, string name = null);
    }
}
