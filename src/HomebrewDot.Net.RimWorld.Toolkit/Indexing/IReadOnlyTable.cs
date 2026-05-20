using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.RimWorld.Indexing.Components.Database;

namespace HomebrewDot.Net.RimWorld.Indexing
{
    /// <summary>
    /// Base interface for <see cref="IReadOnlyTable"/>
    /// </summary>
    public interface IReadOnlyTable
    {
        /// <summary>
        /// Unique name of the table. This name is used to identify the table within the database and to access it when performing queries or other operations on the data.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Indicates whether the table is currently filtered. A filtered table may not contain all data, and may be used for specific queries or operations that require a subset of the data.
        /// </summary>
        bool IsFiltered { get; }
        /// <summary>
        /// Sub-tables of the current table. These are tables that contain a subset of the data in the parent table, and can be used for specific queries or operations that require a subset of the data.
        /// </summary>
        IReadOnlyList<IReadOnlyTable> SubTables { get; }

        /// <summary>
        /// Tries to retrieve an indexed item from the table based on the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the item to retrieve.</typeparam>
        /// <param name="data">The data to search for.</param>
        /// <param name="item">The retrieved item, if found.</param>
        /// <returns>True if the item was found; otherwise, false.</returns>
        internal abstract bool TryFind<T>(T data, out IIndexed<T> item) where T : class;
    }
    /// <summary>
    /// A table that contains indexed data of type T. Each table is associated with a specific type of data, and the database can contain multiple tables for different types of data. The ITable interface provides a way to access and manipulate the data in the table, as well as to manage the indexes that are used to optimize queries on the data.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the table.</typeparam>
    public interface IReadOnlyTable<out T> : IReadOnlyTable, IEnumerable<IIndexed<T>> where T : class
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
        IReadOnlyCollection<IIndexed<T>> Query<TSearch>(string property, TSearch search, string indexName = null);
    }
}
