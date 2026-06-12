using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Fluent builder interface for constructing indexers.
    /// </summary>
    /// <typeparam name="T">The type of the objects to be indexed.</typeparam>
    public interface IIndexerBuilder<T> where T : class
    {
        /// <summary>
        /// Defines a metadata key and a function to extract its value from an object of type T. Optionally, the indexer can be set to watch for changes in the value.
        /// </summary>
        /// <param name="metadataKey">The key for the metadata.</param>
        /// <param name="valueFunc">A function to extract the value from an object of type T.</param>
        /// <param name="watchForChanges">Indicates whether the indexer should watch for changes in the value.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> Set(string metadataKey, Func<T, object> valueFunc, bool watchForChanges = false);
        /// <summary>
        /// Defines a required metadata key and a function to extract its value from an object of type T. 
        /// The indexer will watch for changes on the value.
        /// Useful when you are calculating things from multiple properties.
        /// </summary>
        /// <param name="metadataKey">The key for the metadata.</param>
        /// <param name="valueFunc">A function to extract the value from an object of type T.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> Requires(string metadataKey, Func<T, object> valueFunc);
    }
}
