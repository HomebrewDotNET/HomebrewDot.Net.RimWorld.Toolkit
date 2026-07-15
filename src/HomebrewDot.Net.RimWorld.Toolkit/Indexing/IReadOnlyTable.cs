using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Indexing.Components.Database;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Base interface for <see cref="IReadOnlyTable"/>
    /// </summary>
    public interface IReadOnlyTable : IDatabaseObject
    {
        /// <summary>
        /// Unique name of the table. This name is used to identify the table within the database and to access it when performing queries or other operations on the data.
        /// Includes the full path of the table within the database, including any parent tables.
        /// </summary>
        string FullName { get; }
        /// <summary>
        /// The short name of the table, which is typically the name of the type of data stored in the table. This name is used for display purposes and may not be unique within the database, as different sub tables can share the same name.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Indicates whether the table is currently filtered. A filtered table may not contain all data, and may be used for specific queries or operations that require a subset of the data.
        /// </summary>
        bool IsFiltered { get; }
        /// <summary>
        /// The types of the data stored in the table. This is typically the type of the entity that the table represents, and is used to ensure that only valid data is stored in the table.
        /// </summary>
        public Type BaseEntityType { get; }
        /// <summary>
        /// Sub-tables of the current table. These are tables that contain a subset of the data in the parent table, and can be used for specific queries or operations that require a subset of the data.
        /// </summary>
        IReadOnlyList<IReadOnlyTable> SubTables { get; }
        /// <summary>
        /// If changes are currently being synchronized with the database. This property can be used to determine whether the table is in a consistent state and whether it is safe to perform queries or other operations on the data.
        /// </summary>
        public bool IsSyncing { get; }

        /// <summary>
        /// Tries to retrieve an indexed item from the table based on the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve.</typeparam>
        /// <param name="data">The data to search for.</param>
        /// <param name="item">The retrieved item, if found.</param>
        /// <returns>True if the item was found; otherwise, false.</returns>
        abstract bool TryFind<T>(T data, out IIndexed<T> item) where T : class;
    }
    /// <summary>
    /// A table that contains indexed data of type T. Each table is associated with a specific type of data, and the database can contain multiple tables for different types of data. The ITable interface provides a way to access and manipulate the data in the table, as well as to manage the indexes that are used to optimize queries on the data.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the table.</typeparam>
    public interface IReadOnlyTable<out T> : IReadOnlyTable, IEnumerable<IIndexed<T>>, IEnumerable<T> where T : class
    {
        /// <summary>
        /// Queries the table for data of type <typeparamref name="T"/> based on the specified search criteria.
        /// </summary>
        /// <typeparam name="T">The type of data to query.</typeparam>
        /// <typeparam name="TSearch">The type of the search criteria.</typeparam>
        /// <param name="property">The property to search on.</param>
        /// <param name="search">The search criteria.</param>
        /// <param name="indexName">Optional. The name of the index to use. If null, the default index will be used.</param>
        /// <returns>A read-only collection of data matching the search criteria.</returns>
        IReadOnlyCollection<T> Query<TSearch>(string property, TSearch search, string indexName = null);
        /// <summary>
        /// Returns a thread-safe snapshot of the current state of the table.
        /// Can be used during syncing to avoid issues with concurrent modifications.
        /// Should be used by background threads to avoid issues with concurrent modifications.
        /// </summary>
        /// <returns>A read-only collection representing the current state of the table.</returns>
        IReadOnlyCollection<IIndexed<T>> GetSnapshot();
    }
}
