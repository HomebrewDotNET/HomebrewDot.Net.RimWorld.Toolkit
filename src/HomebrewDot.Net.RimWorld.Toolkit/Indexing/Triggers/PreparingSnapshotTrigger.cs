using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing.Triggers
{
    /// <summary>
    /// Fired the tick before a snapshot is taken by the <see cref="ISnapshotManager"/>. This can be used to prepare any necessary data before the snapshot is taken.
    /// </summary>
    public class PreparingSnapshotTrigger
    {
        /// <summary>
        /// The snapshot manager to prepare for. This can be used to push any necessary data to the snapshot before it is taken.
        /// </summary>
        public ISnapshotManager SnapshotManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreparingSnapshotTrigger"/> class.
        /// </summary>
        /// <param name="snapshotManager">The snapshot manager to prepare for.</param>
        public PreparingSnapshotTrigger(ISnapshotManager snapshotManager)
        {
            SnapshotManager = Toolkit.Helpers.Guard.NotNull(snapshotManager, nameof(snapshotManager));
        }   
    }
}
