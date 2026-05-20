using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Indexing
{
    /// <summary>
    /// Used by a <see cref="ISnapshotManager"/> to see if any changes have occurred to a <typeparamref name="T"/> since the last snapshot was taken. This is used to determine whether a new snapshot needs to be taken or if the current snapshot can be reused.
    /// </summary>
    /// <typeparam name="T">The type to check for changes.</typeparam>
    public interface IChangeTracker<in T> where T : class
    {
        /// <summary>
        /// Determines if any changes have occurred to the given <paramref name="current"/> value since the last snapshot was taken, as represented by the <paramref name="previous"/> indexed value. If this method returns true, a new snapshot will be taken; if it returns false, the current snapshot will be reused.
        /// </summary>
        /// <param name="current">The current value to check for changes.</param>
        /// <param name="previous">The previous indexed value to compare against.</param>
        /// <returns>True if changes have occurred; otherwise, false.</returns>
        bool HasChanged(T current, IIndexed<T> previous);
    }
}
