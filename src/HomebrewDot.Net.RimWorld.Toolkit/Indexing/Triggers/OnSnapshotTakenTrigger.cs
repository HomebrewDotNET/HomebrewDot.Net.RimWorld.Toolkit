using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Indexing.Triggers
{
    /// <summary>
    /// Fired when a snapshot is taken by the <see cref="ISnapshotManager"/>.
    public class OnSnapshotTakenTrigger
    {
        /// <summary>
        /// The snapshot that was taken.
        /// </summary>
        public IReadOnlyDatabase Snapshot { get; }

        /// <inheritdoc cref="OnSnapshotTakenTrigger"/>
        /// <param name="snapshot"><see cref="Snapshot"/></param>
        public OnSnapshotTakenTrigger(IReadOnlyDatabase snapshot)
        {
            Snapshot = Toolkit.Helpers.Guard.NotNull(snapshot, nameof(snapshot));
        }
    }
}
