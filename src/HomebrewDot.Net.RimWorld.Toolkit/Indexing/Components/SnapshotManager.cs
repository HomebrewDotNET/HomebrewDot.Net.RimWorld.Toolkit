using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using RimWorld;
using Verse;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;
using Logger = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Default implementation of <see cref="ISnapshotManager"/>.
    /// </summary>
    public class SnapshotManager : ISnapshotManager
    {
        // Constants
        private const int MaxPendingWork = 1024;

        // Fields
        private readonly IDatabase _database;
        private readonly IHookManager _hookManager;
        private IReadOnlyDatabase _databaseSnapshot;
        private object[] _changeTrackers;

        // State
        private ISnapshotBuilder _snapshotBuilder;
        private int _lastVersion;
        private readonly Dictionary<Type, object[]> _changeTrackerCache = new Dictionary<Type, object[]>();
        private readonly Dictionary<Type, TypedSnapshotManager> _typedManagers = new Dictionary<Type, TypedSnapshotManager>();
        private bool _queueEnabled;
        private IHookTriggerer<RaiseCooperativeWork> _cooperativeWorkTriggerer;
        private IHookTriggerer<OnSnapshotTakenTrigger> _snapshotTakenTriggerer;

        /// <inheritdoc cref="SnapshotManager"/>
        /// <param name="database">The database the snapshot manager will manage.</param>
        /// <param name="hookManager">The hook manager used to trigger events.</param>
        public SnapshotManager(IDatabase database, IHookManager hookManager)
        {
            _database = Guard.NotNull(database, nameof(database));
            _hookManager = Guard.NotNull(hookManager, nameof(hookManager));
            _cooperativeWorkTriggerer = hookManager.GetTriggerer<RaiseCooperativeWork>();
            _snapshotTakenTriggerer = hookManager.GetTriggerer<OnSnapshotTakenTrigger>();
            DatabaseSnapshot = _database.StartSnapshot().Build();
        }
        /// <inheritdoc/>
        public IReadOnlyDatabase DatabaseSnapshot
        {
            get
            {
                return _databaseSnapshot;
            }
            protected set
            {
                _databaseSnapshot = Guard.NotNull(value, nameof(DatabaseSnapshot));
            }
        }
        /// <inheritdoc/>
        public IReadOnlyDatabase Database => _database;

        /// <inheritdoc/>
        public bool Destroyed<T>(T data, ref IndexMetadata metadata, bool allowBuffering = true) where T : class
            => AsTyped<T>().Destroyed(data, ref metadata, allowBuffering);

        /// <inheritdoc/>
        public bool Push<T>(T data, ref IndexMetadata metadata, bool allowBuffering = true) where T : class
            => AsTyped<T>().Push(data, ref metadata, allowBuffering);
        /// <inheritdoc/>
        public bool Update<T>(IIndexed<T> indexed, ref IndexMetadata metadata, bool allowBuffering = true) where T : class
            => AsTyped<T>().Update(indexed, ref metadata, allowBuffering);
        /// <inheritdoc/>
        public bool Delete<T>(IIndexed<T> indexed, ref IndexMetadata metadata, bool allowBuffering = true) where T : class
            => AsTyped<T>().Delete(indexed, ref metadata, allowBuffering);
        /// <inheritdoc/>
        public void Reset(Action<ISnapshotManagerConfigurator> configurator, Action<IDatabaseSchemaBuilder> schemaBuilder)
        {
            schemaBuilder = Guard.NotNull(schemaBuilder, nameof(schemaBuilder));
            Logger.Log("Snapshot manager resetting and redeploying database");
            {
                var config = new ConfigureSnapshotManager();
                configurator?.Invoke(config);
                _changeTrackers = config.changeTrackers?.Distinct()?.ToArray();
                _changeTrackerCache.Clear();
                foreach(var typedManager in _typedManagers.Values)
                {
                    typedManager.Drain();
                }
                _typedManagers.Clear();
                _database.Deploy(schemaBuilder);
                _queueEnabled = false;
                _snapshotBuilder = null;
                _cooperativeWorkTriggerer = _hookManager.GetTriggerer<RaiseCooperativeWork>();
                _snapshotTakenTriggerer = _hookManager.GetTriggerer<OnSnapshotTakenTrigger>();
                DatabaseSnapshot = _database.StartSnapshot().Build();
            }
        }

        /// <summary>
        /// Gets or creates a typed snapshot manager for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An <see cref="ISnapshotManager{T}"/> instance.</returns>
        public ISnapshotManager<T> AsTyped<T>() where T : class
        {
            var type = typeof(T);
            if (!_typedManagers.TryGetValue(type, out var typed))
            {
                typed = new TypedSnapshotManager<T>(this);
                _typedManagers[type] = typed;
            }
            return (ISnapshotManager<T>)typed;
        }
        /// <inheritdoc/>
        public ISnapshotBuilder Snapshot(bool isForce = false)
        {
            foreach (var typedManager in _typedManagers.Values)
            {
                if (isForce)
                {
                    Logger.Log($"Draining pending snapshot queue of size {typedManager.Pending} in {typedManager}");
                    typedManager.Drain();
                }
                else if(typedManager.Pending >= MaxPendingWork)
                {
                    Logger.LogWarning($"Draining pending snapshot queue of size {typedManager.Pending} in {typedManager}");
                }
            }

            if (_snapshotBuilder is not null)
            {
                if (!_snapshotBuilder.IsFinished)
                {
                    if (isForce)
                    {
                        _snapshotBuilder.Build();
                        // Force last one to finish
                        _ = Snapshot(true);
                        return Snapshot(true);
                    }
                    if (Logger.IsVerboseEnabled) Logger.LogVerbose($"Snapshot still pending. Returning previous");
                    return _snapshotBuilder;
                }
                else
                {
                    var snapshot = _snapshotBuilder.Snapshot;
                    if (snapshot.Version == _lastVersion)
                    {
                        var lastBuilder = _snapshotBuilder;
                        _snapshotBuilder = null;
                        if (Logger.IsVerboseEnabled) Logger.LogVerbose("Snapshot manager detected no changes in database since last snapshot. Skipping update.");
                        return lastBuilder;
                    }
                    DatabaseSnapshot = snapshot;
                    if (Logger.IsVerboseEnabled) Logger.LogVerbose($"Snapshot manager finished snapshot of database. Current version {DatabaseSnapshot?.Version ?? '?'}");
                    var builder = _snapshotBuilder;
                    _snapshotBuilder = null;
                    if (isForce)
                    {
                        _snapshotTakenTriggerer.Trigger(new OnSnapshotTakenTrigger(snapshot, isForce));
                    }
                    else
                    {
                        _snapshotTakenTriggerer.TriggerDelayed(new OnSnapshotTakenTrigger(snapshot, isForce));
                    }
                    return builder;
                }
            }

            if (Logger.IsVerboseEnabled) Logger.LogVerbose($"Snapshot manager starting snapshot of database. Current version {DatabaseSnapshot?.Version ?? '?'}");
            _lastVersion = DatabaseSnapshot?.Version ?? -1;
            _snapshotBuilder = _database.StartSnapshot();
            if (_snapshotBuilder.IsFinished)
            {
                if(!isForce)
                {
                    _queueEnabled = true;
                }
                return Snapshot();
            }
            return _snapshotBuilder;
        }   

        private IChangeTracker<T>[] GetChangeTrackers<T>() where T : class
        {
            var changeTrackers = _changeTrackers;
            if (changeTrackers == null || changeTrackers.Length == 0) return Array.Empty<IChangeTracker<T>>();

            if(!_changeTrackerCache.TryGetValue(typeof(T), out var cachedChangeTrackers))
            {
               cachedChangeTrackers = changeTrackers.OfType<IChangeTracker<T>>().ToArray();
                _changeTrackerCache[typeof(T)] = cachedChangeTrackers;
            }

            return (IChangeTracker<T>[])cachedChangeTrackers;
        }

        /// <summary>
        /// Typed snapshot manager for push/destroy operations on <typeparamref name="T"/>.
        /// Caches <see cref="IDatabase{T}"/> for direct mutation calls, avoiding the generic
        /// dispatch overhead in <see cref="Database"/>.
        /// </summary>
        /// <typeparam name="T">The entity type this instance is optimized for.</typeparam>
        internal class TypedSnapshotManager<T> : TypedSnapshotManager, ISnapshotManager<T> where T : class
        {
            // Statics
            private const int MaxPendingWork = 1024;

            delegate bool Changed(T current, IIndexed<T> indexed, ref IndexMetadata metadata);
            private readonly SnapshotManager _manager;
            private readonly IDatabase<T> _typedDb;
            private readonly IChangeTracker<T>[] _changeTrackers;
            private readonly IChangeTracker<T>[] _updateChangeTrackers;
            private readonly Dictionary<T, PendingUpsert> _pending = new Dictionary<T, PendingUpsert>();
            private readonly Queue<PendingUpsert> _work = new Queue<PendingUpsert>();
            private readonly Changed _onInsert;
            private readonly Changed _hasChanged;

            // State
            private int _lastPending;

            public override int Pending => _work.Count;

            public TypedSnapshotManager(SnapshotManager manager)
            {
                _manager = Guard.NotNull(manager, nameof(manager));
                _typedDb = manager._database.AsTyped<T>();
                _changeTrackers = manager.GetChangeTrackers<T>();
                _updateChangeTrackers = _changeTrackers.Where(x => !x.Once).ToArray();
                _onInsert = Compile(_changeTrackers);
                _hasChanged = Compile(_updateChangeTrackers);
            }

            /// <inheritdoc/>
            public bool Push(T data, ref IndexMetadata metadata, bool allowBuffering = true)
            {
                data = Guard.NotNull(data, nameof(data));

                if(!_manager._queueEnabled || !allowBuffering)
                {
                    var existing = _typedDb.Find(data);
                    return Push(data, existing, ref metadata);
                }

                bool accepted = false;
                if(_work.Count == 0)
                {
                    var context = new CooperativeWorkContext();
                    var work = RaiseCooperativeWork.From(() => DoWork(context).GetEnumerator(), context);
                    accepted = _manager._cooperativeWorkTriggerer.Trigger(work);
                }
                else
                {
                    accepted = true;
                }

                if (!accepted)
                {
                    var existing = _typedDb.Find(data);
                    return Push(data, existing, ref metadata);
                }

                if(_pending.TryGetValue(data, out var pending))
                {
                    metadata.MergeInto(ref pending.Metadata);
                    metadata.Dispose();
                    pending.IsDelete = false;
                    return true;
                }

                var pendingWork = Toolkit.Pool<PendingUpsert>.Rent();
                pendingWork.Data = data;
                pendingWork.Metadata = metadata;
                pendingWork.IsDelete = false;
                _work.Enqueue(pendingWork);
                _pending[data] = pendingWork;
                return true;
            }

            private bool Push(T data, IIndexed<T> existing, ref IndexMetadata metadata)
            {
                if(existing is null)
                {
                    _ = _onInsert?.Invoke(data, existing, ref metadata);
                }
                else if (!HasChanged(data, existing, ref metadata))
                {
                    // Release rejected metadata back to the pools
                    metadata.Dispose();
                    return false;
                }
                return _typedDb.Update(data, existing, ref metadata);
            }

            /// <inheritdoc/>
            public bool Destroyed(T data, ref IndexMetadata metadata, bool allowBuffering = true)
            {
                data = Guard.NotNull(data, nameof(data));
                if (!_manager._queueEnabled || !allowBuffering) return ActualDestroyed(data, ref metadata);

                bool accepted = false;
                if (_work.Count == 0)
                {
                    var context = new CooperativeWorkContext();
                    var work = RaiseCooperativeWork.From(() => DoWork(context).GetEnumerator(), context);
                    accepted = _manager._cooperativeWorkTriggerer.Trigger(work);
                }
                else
                {
                    accepted = true;
                }

                if (!accepted)
                {
                    return ActualDestroyed(data, ref metadata);
                }

                if (_pending.TryGetValue(data, out var pending))
                {
                    metadata.MergeInto(ref pending.Metadata);
                    metadata.Dispose();
                    pending.IsDelete = true;
                    return true;
                }

                var pendingWork = Toolkit.Pool<PendingUpsert>.Rent();
                pendingWork.Data = data;
                pendingWork.Metadata = metadata;
                pendingWork.IsDelete = true;
                _work.Enqueue(pendingWork);
                _pending[data] = pendingWork;
                return true;
            }

            /// <inheritdoc/>
            private bool ActualDestroyed(T data, ref IndexMetadata metadata)
            {
                data = Guard.NotNull(data, nameof(data));
                return _typedDb.Delete(data, ref metadata);
            }

            private bool HasChanged(T current, IIndexed<T> data, ref IndexMetadata metadata)
            {
                if (_hasChanged is null) return false;
                return _hasChanged(current, data, ref metadata);
            }

            private IEnumerable DoWork(CooperativeWorkContext context)
            {
                if(context.CheckInterval <= 8) context.CheckInterval = 8;
                if(_work.Count > MaxPendingWork)
                {
                    Logger.LogWarning($"Snapshot manager has {_work.Count} pending work items for {typeof(T).Name}. Queue grew from {_lastPending} to {_work.Count}. Increasing interval to catch-up");
                    context.CheckInterval *= 2;
                }
                while (_work.TryDequeue(out var work)) 
                {
                    var data = work.Data;
                    try
                    {
                        using (work)
                        {
                            context.LogWork();
                            if (work.IsDelete)
                            {
                                ActualDestroyed(work.Data, ref work.Metadata);
                            }
                            else
                            {
                                var existing = _typedDb.Find(work.Data);
                                Push(work.Data, existing, ref work.Metadata);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Snapshot manager failed to process pending work for {typeof(T).Name}: {ex}");
                    }
                    finally
                    {
                        _pending.Remove(data);
                    }
                    if (context.WaitForNextTick)
                    {
                        _lastPending = _work.Count;
                        yield return null;
                    }
                }
            }

            private Changed Compile(IChangeTracker<T>[] changeTrackers)
            {
                var currentParameter = Expression.Parameter(typeof(T), "current");
                var indexParameter = Expression.Parameter(typeof(IIndexed<T>), "index");
                var metadataParameter = Expression.Parameter(typeof(IndexMetadata).MakeByRefType(), "metadata");

                Expression current = null;
                IndexMetadata metadata = default;
                var changedMethod = Toolkit.Helpers.Expression.GetMethod<IChangeTracker<T>>(x => x.HasChanged(default, default, ref metadata));
                for (int i = 0; i < changeTrackers.Length; i++) { 
                    var changeTracker = changeTrackers[i];

                    Expression condition;
                    if(changeTracker is IChangeTrackerCompileable<T> compileable)
                    {
                        condition = compileable.Compile(currentParameter, indexParameter, metadataParameter);
                    }
                    else
                    {
                        condition = Expression.Call(Expression.Constant(changeTracker), changedMethod, currentParameter, indexParameter, metadataParameter);
                    }

                    if(current is null)
                    {
                        current = condition;
                    }
                    else
                    {
                        current = Expression.Or(current, condition);
                    }
                }
                if (current is null) return null;
                return Expression.Lambda<Changed>(current, currentParameter, indexParameter, metadataParameter).Compile();
            }

            public override void Drain()
            {
                if(_work.Count > 0)
                {
                    var context = new CooperativeWorkContext();
                    context.NoInterval();
                    DoWork(context).ExecuteEnumerable();
                }
            }
            /// <inheritdoc/>
            public bool Update(IIndexed<T> indexed, ref IndexMetadata metadata, bool allowBuffering = true)
            {
                indexed = Guard.NotNull(indexed, nameof(indexed));
                return Push(indexed.Value, indexed, ref metadata);
            }
            /// <inheritdoc/>
            public bool Delete(IIndexed<T> indexed, ref IndexMetadata metadata, bool allowBuffering = true)
            {
                indexed = Guard.NotNull(indexed, nameof(indexed));
                return Destroyed(indexed.Value, ref metadata, allowBuffering);
            }

            private class PendingUpsert : IPoolable, IDisposable
            {
                public T Data;
                public IndexMetadata Metadata;
                public bool IsDelete;

                public void Dispose()
                {
                    Toolkit.Pool<PendingUpsert>.Return(this);
                }

                public void Reset()
                {
                    Data = null;
                    Metadata = default;
                    IsDelete = false;
                }
            }
        }

        internal abstract class TypedSnapshotManager {
            public abstract int Pending { get; }
            public abstract void Drain();
        }

        private class ConfigureSnapshotManager : ISnapshotManagerConfigurator
        {
            public HashSet<object> changeTrackers;

            public ISnapshotManagerConfigurator WithChangeTracker<T>(IChangeTracker<T> changeTracker) where T : class
            {
                changeTracker = Guard.NotNull(changeTracker, nameof(changeTracker));
                changeTrackers ??= new HashSet<object>();
                changeTrackers.Add(changeTracker);
                return this;
            }
        }
    }
}
