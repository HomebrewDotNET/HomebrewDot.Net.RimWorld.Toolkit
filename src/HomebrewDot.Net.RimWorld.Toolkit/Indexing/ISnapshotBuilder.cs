using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// A builder for creating a database snapshot accross ticks.
    /// </summary>
    public interface ISnapshotBuilder
    {
        // Properties
        /// <summary>
        /// The database being snapshotted.
        /// </summary>
        IDatabase Database { get; }
        /// <summary>
        /// The snapshot once done.
        /// </summary>
        IReadOnlyDatabase Snapshot { get; }
        /// <summary>
        /// True if the <see cref="Snapshot"/> is ready for consumption, otherwise false.
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Returns work that can be used to incrementally build the snapshot.
        /// </summary>
        /// <returns>The work that can be raised with <see cref="IHookManager"/></returns>
        RaiseCooperativeWork CreateWork();
        /// <summary>
        /// Creates works and finishes it.
        /// </summary>
        /// <returns>The snapshot that was created</returns>
        IReadOnlyDatabase Build();
    }
}
