using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Models;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Responsible for managing the current and pending snapshots of game data, and processing new data for the pending snapshot.
    /// </summary>
    public interface ISnapshotManager
    {
        /// <summary>
        /// The current synchronous database of indexed data. This database is read-only and represents the current state.
        /// Should NOT be accessed from background threads as it is managed by the main thread.
        /// </summary>
        public IReadOnlyDatabase Database { get; }
        /// <summary>
        /// The current snapshot of indexed data. This snapshot is read-only and represents the state of the indexed data at the last snapshot point.
        /// Can be accessed from background threads.
        /// </summary>
        public IReadOnlyDatabase DatabaseSnapshot { get; }

        /// <summary>
        /// Pushes <paramref name="data"/> to be indexed in the current pending snapshot.
        /// </summary>
        /// <typeparam name="T">The type of the data to be indexed.</typeparam>
        /// <param name="data">The data to be indexed.</param>
        /// <param name="metadata">Optional metadata associated with the data.</param>
        /// <param name="allowBuffering">If set to <c>true</c>, the data can be buffered for later processing to smooth out tps.</param>
        /// <returns><c>true</c> if the data was accepted, <c>false</c> otherwise.</returns>
        bool Push<T>(T data, ref IndexMetadata metadata, bool allowBuffering = true) where T : class;
        /// <summary>
        /// Notifies the snapshot manager that <paramref name="data"/> has been destroyed and should be removed from the pending snapshot if it is present.
        /// </summary>
        /// <typeparam name="T">The type of the data that was destroyed.</typeparam>
        /// <param name="data">The data that was destroyed.</param>
        /// <param name="metadata">Optional metadata associated with the destroyed data.</param>
        /// <param name="allowBuffering">If set to <c>true</c>, the data can be buffered for later processing to smooth out tps.</param>
        /// <returns><c>true</c> if the data was found and marked as destroyed, <c>false</c> otherwise.</returns>
        bool Destroyed<T>(T data, ref IndexMetadata metadata, bool allowBuffering = true) where T : class;

        /// <summary>
        /// Gets or creates a typed snapshot manager for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An <see cref="ISnapshotManager{T}"/> instance.</returns>
        ISnapshotManager<T> AsTyped<T>() where T : class;

        /// <summary>
        /// Starts a snapshot for the currently pending changes or finishes the current one if it's finished.
        /// </summary>
        /// <param name="isForce">If the current snapshot is a forced snapshot</param>
        ISnapshotBuilder Snapshot(bool isForce = false);
        /// <summary>
        /// Resets the snapshot manager, clearing the current pending changes and preparing a new schema for the next snapshot.
        /// </summary>
        /// <param name="configurator">An action to configure the snapshot manager after resetting it.</param>
        /// <param name="schemaBuilder">An action to configure the database schema after resetting the snapshot manager.</param>
        void Reset(Action<ISnapshotManagerConfigurator> configurator, Action<IDatabaseSchemaBuilder> schemaBuilder);
    }
    /// <summary>
    /// Typed snapshot manager for push/destroy operations on <typeparamref name="T"/>.
    /// Provides the same push and destroy semantics as <see cref="ISnapshotManager"/> but without the snapshot lifecycle methods.
    /// </summary>
    /// <typeparam name="T">The entity type this manager handles.</typeparam>
    public interface ISnapshotManager<T> where T : class
    {
        /// <summary>
        /// Pushes <paramref name="data"/> to be indexed in the current pending snapshot.
        /// </summary>
        /// <param name="data">The data to be indexed.</param>
        /// <param name="metadata">Optional metadata associated with the data.</param>
        /// <param name="allowBuffering">If set to <c>true</c>, the data can be buffered for later processing to smooth out tps.</param>
        /// <returns><c>true</c> if the data was accepted, <c>false</c> otherwise.</returns>
        bool Push(T data, ref IndexMetadata metadata, bool allowBuffering = true);
        /// <summary>
        /// Notifies the snapshot manager that <paramref name="data"/> has been destroyed and should be removed from the pending snapshot if it is present.
        /// </summary>
        /// <param name="data">The data that was destroyed.</param>
        /// <param name="metadata">Optional metadata associated with the destroyed data.</param>
        /// <param name="allowBuffering">If set to <c>true</c>, the data can be buffered for later processing to smooth out tps.</param>
        /// <returns><c>true</c> if the data was found and marked as destroyed, <c>false</c> otherwise.</returns>
        bool Destroyed(T data, ref IndexMetadata metadata, bool allowBuffering = true);
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
