using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HomebrewDot.Net.RimWorld.Indexing
{
    /// <summary>
    /// Gather game data and pushes it to the <see cref="ISnapshotManager"/> for indexing.
    /// </summary>
    public interface IDataGatherer
    {
        /// <summary>
        /// Initializes the data gatherer so it can prepare itself to gather data from the game. This is called once at the start of the game and can be used to cache any necessary data for future gathering.
        /// </summary>
        /// <param name="game">The current game instance.</param>
        void Initialize(Game game);
        /// <summary>
        /// Gathers data from the game and pushes it to the snapshot manager.
        /// </summary>
        /// <param name="game">The current game instance.</param>
        /// <param name="snapshotManager">The snapshot manager to receive the data.</param>
        void GatherData(Game game, ISnapshotManager snapshotManager);
        /// <summary>
        /// Resets the data gatherer, clearing any cached data.
        /// </summary>
        void Reset();
    }
}
