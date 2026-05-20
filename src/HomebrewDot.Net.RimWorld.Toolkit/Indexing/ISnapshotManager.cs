using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Indexing
{
    /// <summary>
    /// Responsible for managing the current and pending snapshots of game data, and processing new data for the pending snapshot.
    /// </summary>
    public interface ISnapshotManager
    {
        /// <summary>
        /// The current snapshot of indexed data. This snapshot is read-only and represents the state of the indexed data at the last snapshot point.
        /// </summary>
        public IReadOnlyDatabase DatabaseSnapshot { get; }

        /// <summary>
        /// Pushes <paramref name="data"/> to be indexed in the current pending snapshot.
        /// </summary>
        /// <typeparam name="T">The type of the data to be indexed.</typeparam>
        /// <param name="data">The data to be indexed.</param>
        /// <param name="metadata">Optional metadata associated with the data.</param>
        void Push<T>(T data, IReadOnlyDictionary<string, object> metadata = null) where T : class;
        /// <summary>
        /// Pushes <paramref name="data"/> to be indexed in the current pending snapshot.
        /// </summary>
        /// <typeparam name="T">The type of the data to be indexed.</typeparam>
        /// <param name="data">The data to be indexed.</param>
        /// <param name="metadata">Optional metadata associated with the data.</param>
        void Push<T>(T data, params KeyValuePair<string, object>[] metadata) where T : class;
        /// <summary>
        /// Pushes <paramref name="data"/> to be indexed in the current pending snapshot.
        /// </summary>
        /// <typeparam name="T">The type of the data to be indexed.</typeparam>
        /// <param name="data">The data to be indexed.</param>
        /// <param name="metadata">Optional metadata associated with the data.</param>
        void Push<T>(T data, params (string Key, object Value)[] metadata) where T : class;
        /// <summary>
        /// Notifies the snapshot manager that <paramref name="data"/> has been destroyed and should be removed from the pending snapshot if it is present.
        /// </summary>
        /// <typeparam name="T">The type of the data that was destroyed.</typeparam>
        /// <param name="data">The data that was destroyed.</param>
        /// <param name="metadata">Optional metadata associated with the destroyed data.</param>
        void Destroyed<T>(T data, IReadOnlyDictionary<string, object> metadata = null) where T : class;

        /// <summary>
        /// Takes a snapshot of the current pending data, making it the new current snapshot and clearing the pending snapshot for new data to be gathered.
        /// </summary>
        void Snapshot();
        /// <summary>
        /// Resets the snapshot manager, clearing the current pending changes and preparing a new schema for the next snapshot.
        /// </summary>
        /// <param name="configurator">An action to configure the snapshot manager after resetting it.</param>
        /// <param name="schemaBuilder">An action to configure the database schema after resetting the snapshot manager.</param>
        void Reset(Action<ISnapshotManagerConfigurator> configurator, Action<IDatabaseSchemaBuilder> schemaBuilder);
    }
    /// <summary>
    /// Interface used to configure <see cref="ISnapshotManager"/> instances before the orchestration process begins.
    /// </summary>
    public interface ISnapshotManagerConfigurator
    {
        /// <summary>
        /// Registers <paramref name="changeTracker"/> so the snapshot manager can detect changes.
        /// Otherwise no updates will occur.
        /// </summary>
        /// <typeparam name="T">The type of data to track changes for.</typeparam>
        /// <param name="changeTracker">The change tracker instance to use for detecting changes.</param>
        /// <returns>The configurator instance for chaining.</returns>
        ISnapshotManagerConfigurator WithChangeTracker<T>(IChangeTracker<T> changeTracker) where T : class;
    }
}
