using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Database containing tables with indexed data.
    /// </summary>
    public interface IReadOnlyDatabase : IDatabaseObject
    {
        /// <summary>
        /// Gets all tables in the database.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve tables for.</typeparam>
        /// <returns>A read-only table containing data of the specified type.</returns>
        IEnumerable<IReadOnlyTable> GetTables();
        /// <summary>
        /// Gets all tables in the database that contain data of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve tables for.</typeparam>
        /// <returns>A read-only table containing data of the specified type.</returns>
        IEnumerable<IReadOnlyTable<T>> GetTables<T>() where T : class;
        /// <summary>
        /// Retrieves a read-only table with the specified name that contains data of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of data stored in the table.</typeparam>
        /// <param name="name">The name of the table to retrieve. Sub tables can be accessed using a dot notation, e.g., "ParentTable.SubTable".</param>
        /// <returns>A read-only table with the specified name containing data of the specified type or null if no such table exists.</returns>
        IReadOnlyTable<T> GetTable<T>(string name) where T : class;

        /// <summary>
        /// Tries to find the specified data in the database and returns an indexed reference to it if found.
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <param name="data">The instance of the data to find.</param>
        /// <returns>An indexed reference to the data if found; otherwise, null.</returns>
        IIndexed<T> Find<T>(T data) where T : class;
        /// <summary>
        /// Tries to find the specified data in the database and returns an indexed reference to it if found.
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <param name="data">The instance of the data to find.</param>
        /// <returns>An enumerable of indexed references to the data if found; otherwise, an empty enumerable.</returns>
        IEnumerable<IIndexed<T>> Find<T>(IEnumerable<T> data) where T : class;
        /// <summary>
        /// Tries to find the specified data in the database and returns an indexed reference to it if found.
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <param name="data">The instance of the data to find.</param>
        /// <returns>An enumerable of indexed references to the data if found; otherwise, an empty enumerable.</returns>
        IEnumerable<IIndexed<T>> Find<T>(IReadOnlyList<T> data) where T : class;
        /// <summary>
        /// Queries the database for data of type <typeparamref name="T"/> based on the specified search criteria.
        /// </summary>
        /// <typeparam name="T">The type of data to query.</typeparam>
        /// <typeparam name="TSearch">The type of the search criteria.</typeparam>
        /// <param name="property">The property to search on.</param>
        /// <param name="search">The search criteria.</param>
        /// <param name="tableName">Optional. The name of the table to query, using dot notation for sub-tables, e.g., "ParentTable.SubTable". If null, all tables will be queried.</param>
        /// <param name="indexName">Optional. The name of the index to use. If null, the default index will be used.</param>
        /// <returns>A read-only collection of data matching the search criteria.</returns>
        IReadOnlyCollection<T> Query<T, TSearch>(string property, TSearch search, string tableName = null, string indexName = null) where T : class;
    }
}
