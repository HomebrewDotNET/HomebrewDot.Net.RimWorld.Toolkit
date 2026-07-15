using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using RimWorld;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Fluent builder interface for constructing indexers.
    /// </summary>
    /// <typeparam name="T">The type of the objects to be indexed.</typeparam>
    public interface IIndexerBuilder<T> where T : class
    {
        /// <summary>
        /// Adds a condition to the indexer that specifies when it should be applied, based on the provided database, insert metadata, and indexed object.
        /// When defined and none return true <see cref="Set"/> will not be called.
        /// </summary>
        /// <param name="condition">A function that takes the database, insert metadata, and indexed object, and returns a boolean indicating whether the indexer should be applied.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> When(IIndexerBuilderDelegates<T, object>.SetCondition condition);
        /// <summary>
        /// Defines a metadata key and a function to extract its value from an object of type T. Optionally, the indexer can be set to watch for changes in the value.
        /// </summary>
        /// <param name="metadataKey">The key for the metadata.</param>
        /// <param name="valueFunc">A function to extract the value from an object of type T.</param>
        /// <param name="watchForChanges">Indicates whether the indexer should watch for changes in the value.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> Set<TValue>(IndexMetadataKey metadataKey, Func<T, TValue> valueFunc, bool watchForChanges = false);
        /// <summary>
        /// Defines a metadata key and a function to extract its value from an object of type T. Optionally, the indexer can be set to watch for changes in the value.
        /// </summary>
        /// <param name="metadataKey">The key for the metadata.</param>
        /// <param name="valueFunc">A function to extract the value from an object of type T.</param>
        /// <param name="watchForChanges">Indicates whether the indexer should watch for changes in the value.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> Set<TValue>(IndexMetadataKey metadataKey, IIndexerBuilderDelegates<T, TValue>.SetGetValue valueFunc, bool watchForChanges = false);
        /// <summary>
        /// Defines a required metadata key and a function to extract its value from an object of type T. 
        /// The indexer will watch for changes on the value.
        /// Useful when you are calculating things from multiple properties.
        /// </summary>
        /// <param name="metadataKey">The key for the metadata.</param>
        /// <param name="valueFunc">A function to extract the value from an object of type T.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> Requires<TValue>(IndexMetadataKey metadataKey, Func<T, TValue> valueFunc);
        /// <summary>
        /// Defines a required metadata key and a function to extract its value from an object of type T. 
        /// The indexer will watch for changes on the value.
        /// Useful when you are calculating things from multiple properties.
        /// </summary>
        /// <param name="metadataKey">The key for the metadata.</param>
        /// <param name="valueFunc">A function to extract the value from an object of type T.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> Requires<TValue>(IndexMetadataKey metadataKey, IIndexerBuilderDelegates<T, TValue>.SetGetValue valueFunc);
        /// <summary>
        /// Includes metadata from the insert on the item during insertion.
        /// </summary>
        /// <param name="metadataKey">The key of the metadata to copy</param>
        /// <param name="watchForChanges">Indicates whether the indexer should watch for changes in the value.</param>
        /// <returns>The current instance of the indexer builder.</returns>
        IIndexerBuilder<T> Include<TValue>(IndexMetadataKey metadataKey, bool watchForChanges = false);
    }
    /// <summary>
    /// Contains the delegates for <see cref="IIndexerBuilder{T}"/>
    /// </summary>
    public static class IIndexerBuilderDelegates<T,TValue> where T : class
    {
        public delegate bool SetCondition(T current, IIndexed<T> indexed, ref IndexMetadata metadata);
        public delegate TValue SetGetValue(T current, IIndexed<T> indexed, ref IndexMetadata metadata);
    }
}
