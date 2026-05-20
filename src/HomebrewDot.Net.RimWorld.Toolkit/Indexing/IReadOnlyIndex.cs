using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Indexing
{
    /// <summary>
    /// Represents a read-only index that provides access to indexed data of type <typeparamref name="T"/> based on input data of type <typeparamref name="TSearch"/>. The index is designed to be immutable and thread-safe, allowing for concurrent read operations without the need for synchronization. It may also support filtering, which allows it to contain only a subset of the data based on specific criteria.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the index.</typeparam>
    /// <typeparam name="TSearch">The type of input data used to query the index.</typeparam>
    public interface IReadOnlyIndex<out T, in TSearch> : IReadOnlyIndex where T : class
    {
        /// <summary>
        /// Queries the index for data of type <typeparamref name="T"/> based on the specified search criteria of type <typeparamref name="TSearch"/>.
        /// </summary>
        /// <param name="data">The search criteria.</param>
        /// <returns>A collection of data matching the search criteria.</returns>
        IEnumerable<T> Query(TSearch data);
    }
    /// <summary>
    /// Base interface for <see cref="IReadOnlyIndex{T,TSearch}"/>.
    /// </summary>
    public interface IReadOnlyIndex
    {
        /// <summary>
        /// Unique name of an index.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Indicates whether the index is currently filtered. A filtered index may not contain all data, and may be used for specific queries or operations that require a subset of the data.
        /// </summary>
        bool IsFiltered { get; }
    }
}

