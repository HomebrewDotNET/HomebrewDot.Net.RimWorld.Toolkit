using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Represents an <typeparamref name="T"/> that was indexed and allows for updating the metadata of the indexed object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IWriteableIndexed<out T> : IIndexed<T> where T : class
    {
        /// <summary>
        /// Updates <see cref="IIndexed{T}.Metadata"/> with the specified property name and value.
        /// </summary>
        /// <typeparam name="TData">The type of the value to set.</typeparam>
        /// <param name="propertyName">The name of the property to update.</param>
        /// <param name="value">The value to set for the specified property.</param>
        /// <returns>True if the property was successfully updated; otherwise, false if added</returns>
        bool Set<TData>(string propertyName, TData value);
        /// <summary>
        /// Removes the specified property from <see cref="IIndexed{T}.Metadata"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to remove.</param>
        /// <returns>True if the property was successfully removed; otherwise, false if the property did not exist.</returns>
        bool Unset(string propertyName);
    }
    /// <summary>
    /// Represents an <typeparamref name="T"/> that was indexed.
    /// </summary>
    /// <typeparam name="T">The type of the indexed object.</typeparam>
    public interface IIndexed<out T> where T : class
    {
        /// <summary>
        /// The instance of <typeparamref name="T"/> that was indexed.
        /// Not thread-safe, as the indexed object may be mutable and shared across multiple threads.
        /// Property values can be indexed in <see cref="Metadata"/>.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Extra metadata set by any available enrichers.
        /// </summary>
        IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// If the current instance has pending changes waiting to be synced to the snapshot, this will be true. Otherwise, it will be false.
        /// </summary>
        bool HasPendingChanges { get; }
        /// <summary>
        /// True if the current instance was snapshotted.
        /// </summary>
        bool HasSnapshot { get; }
        /// <summary>
        /// True if the current instance is a snapshot (thus can't have snapshots itself)
        /// </summary>
        bool IsSnapshot { get; }
        /// <summary>
        /// The current snapshot of the current item if it was taken already.
        /// </summary>
        IIndexed<T> Snapshot { get; }
        /// <summary>
        /// If the current instance was removed from the database. When true all references to the current instance should be considered invalid and should be removed.
        /// </summary>
        public bool IsRemoved { get; }
        /// <summary>
        /// If the current item was/is inserted, otherwise updated.
        /// </summary>
        bool IsInsert { get; }

        /// <summary>
        /// Retrieves the value of the specified property from the indexed object or its metadata. If the property exists in both, the metadata value takes precedence.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to retrieve.</typeparam>
        /// <param name="propertyName">The name of the property to retrieve.</param>
        /// <returns>The value of the specified property.</returns>
        TValue GetValue<TValue>(string propertyName);
    }
}
