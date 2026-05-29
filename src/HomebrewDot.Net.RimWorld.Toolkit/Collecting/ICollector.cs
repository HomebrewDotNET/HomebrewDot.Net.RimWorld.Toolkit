using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Collecting.Models;

namespace HomebrewDot.Net.RimWorld.Collecting
{
    /// <summary>
    /// Collects objects of type <typeparamref name="T"/> based on a <see cref="CollectionDef"/>
    /// </summary>
    /// <typeparam name="T">The type of objects to collect. Must be a reference type.</typeparam>
    public interface ICollector<T> : ICollector where T : class
    {
        /// <summary>
        /// Collects the specified object of type <typeparamref name="T"/> if it matches the collection definition. The context parameter provides additional information that may be needed for collection, such as the source of the object or any relevant metadata. Returns true if the object was collected; otherwise, returns false.
        /// </summary>
        /// <param name="obj">The object to collect.</param>
        /// <param name="context">A dictionary of additional information that may be needed for collection.</param>
        /// <returns>True if the object was collected; otherwise, false.</returns>
        bool Collect(T obj, IReadOnlyDictionary<string, object> context);
        /// <summary>
        /// Returns all collected objects of type <typeparamref name="T"/> so far. The collection should not be modified by the caller.
        /// </summary>
        /// <returns>A read-only collection of all collected objects of type <typeparamref name="T"/>.</returns>
        new IReadOnlyCollection<T> GetAll();
        /// <summary>
        /// Returns true if the collector has collected the specified object of type <typeparamref name="T"/>. Otherwise, returns false.
        /// </summary>
        /// <param name="obj">The object to check for.</param>
        /// <returns>True if the object has been collected; otherwise, false.</returns>
        bool Contains(T obj);
        /// <summary>
        /// Checks if the collector can collect the specified object of type <typeparamref name="T"/> based on the collection definition. 
        /// </summary>
        /// <param name="obj">The object to check for.</param>
        /// <param name="context">A dictionary of additional information that may be needed for the check.</param>
        /// <returns>True if the object can be collected; otherwise, false.</returns>
        bool CanCollect(T obj, IReadOnlyDictionary<string, object> context);
    }

    /// <summary>
    /// Base interface <see cref="ICollector{T}"/>.
    /// </summary>
    public interface ICollector : IEnumerable
    {
        /// <summary>
        /// The definition that describes which objects of type <typeparamref name="T"/> this collector should collect. This definition is used when <see cref="StartCollecting"/> is called to determine which objects to collect.
        /// </summary>
        ICollectionDef Definition { get; }
        /// <summary>
        /// How many objects of type <typeparamref name="T"/> this collector has collected so far.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Starts collecting objects of type <typeparamref name="T"/> based on <see cref="Definition"/>
        /// Clear the collection as well.
        /// </summary>
        /// <param name="comparer">The comparator used to determine if objects match the collection definition.</param>
        /// <param name="collections">A dictionary of collection definitions keyed by their names. Used to resolve references between collections.</param>
        void StartCollecting(ICollectionComparator comparer, IReadOnlyDictionary<string, ICollectionDef> collections);
        /// <summary>
        /// Stops collecting objects of type <typeparamref name="T"/>. After this method is called, the collector should not collect any more objects until <see cref="StartCollecting"/> is called again.
        /// </summary>
        void StopCollecting();
        /// <summary>
        /// Removes all collected objects of type <typeparamref name="T"/> from the collector. After this method is called, the collector should be empty and <see cref="Count"/> should return 0.
        /// </summary>
        void Clear();
        IReadOnlyCollection<object> GetAll();
    }
}
