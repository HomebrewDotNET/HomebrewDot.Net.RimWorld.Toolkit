using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// A database object that stores data.
    /// </summary>
    public interface IDatabaseObject
    {
        /// <summary>
        /// The current version of the database object.
        /// Gives an indication if the state of the database object has changed since the last time it was accessed, allowing for caching of query results and other optimizations based on the stability of the database object state.
        /// </summary>
        int Version { get; }
        /// <summary>
        /// Indicates if the database object is currently tracking changes to its data. If true, the database object will keep track of changed and deleted data each snapshot.
        /// </summary>
        bool TrackingChanges { get; }
        /// <summary>
        /// Gets a read-only collection of all data that has been added or updated from the database object since the last snapshot. This collection is only populated if <see cref="TrackingChanges"/> is true.
        /// </summary>
        public IReadOnlyCollection<IIndexed<object>> Changed { get; }
        /// <summary>
        /// Gets a read-only collection of all data that has been deleted from the database object since the last snapshot. This collection is only populated if <see cref="TrackingChanges"/> is true.
        /// </summary>
        public IReadOnlyCollection<IIndexed<object>> Deleted { get; }
    }
}
