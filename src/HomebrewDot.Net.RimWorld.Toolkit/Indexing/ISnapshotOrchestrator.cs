using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HomebrewDot.Net.RimWorld.Indexing
{
    /// <summary>
    /// Responsible for orchestrating the gathering of game data and supplying it to the <see cref="ISnapshotManager"/> instances for indexing.
    /// </summary>
    public interface ISnapshotOrchestrator
    {
        /// <summary>
        /// Rebuilds the current index synchronously and starts the orchestration process.
        /// </summary>
        /// <param name="game">The current game instance.</param>
        /// <param name="isGameStartup">Indicates whether the game is starting up.</param>
        /// <param name="snapshotManager">The snapshot manager to which gathered data should be supplied.</param>
        /// <param name="configure">An action to configure the <see cref="ISnapshotOrchestrator"/> instance before starting the orchestration process.</param>
        /// <param name="schemaBuilder">An action to configure the database schema before starting the orchestration process.</param>
        void RebuildIndex(Game game, 
            bool isGameStartup, 
            ISnapshotManager snapshotManager, 
            Action<ISnapshotOrchestratorBuilder> configure,
            Action<ISnapshotManagerConfigurator> configureManager,
            Action<IDatabaseSchemaBuilder> schemaBuilder);
    }

    /// <summary>
    /// Used to configure <see cref="ISnapshotOrchestrator"/> instances.
    /// </summary>
    public interface ISnapshotOrchestratorBuilder
    {
        /// <summary>
        /// Registers an <see cref="IDataGatherer"/> to be used by the <see cref="ISnapshotOrchestrator"/> during the orchestration process.
        /// </summary>
        /// <param name="dataGatherer">The data gatherer to register.</param>
        /// <returns>The current <see cref="ISnapshotOrchestratorBuilder"/> instance for method chaining.</returns>
        ISnapshotOrchestrator With(IDataGatherer dataGatherer);
    }
}
