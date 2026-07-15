using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing.Triggers
{
    /// <summary>
    /// Fired when a snapshot is taken by the <see cref="ISnapshotManager"/>.
    public class OnSnapshotTakenTrigger
    {
        /// <summary>
        /// The snapshot that was taken.
        /// </summary>
        public IReadOnlyDatabase Snapshot { get; }
        /// <summary>
        /// If the current snapshot is a forced snapshot.
        /// </summary>
        public bool IsForced { get; }

        /// <inheritdoc cref="OnSnapshotTakenTrigger"/>
        /// <param name="snapshot"><see cref="Snapshot"/></param>
        public OnSnapshotTakenTrigger(IReadOnlyDatabase snapshot, bool isForced)
        {
            Snapshot = Toolkit.Helpers.Guard.NotNull(snapshot, nameof(snapshot));
            IsForced = isForced;
        }
    }
}
