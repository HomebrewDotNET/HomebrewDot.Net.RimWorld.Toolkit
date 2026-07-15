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
        private readonly object _lock = new object();
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

        /// <inheritdoc cref="SnapshotManager"/>
        /// <param name="database">The database the snapshot manager will manage.</param>
        /// <param name="hookManager">The hook manager used to trigger events.</param>
        public SnapshotManager(IDatabase database, IHookManager hookManager)
        {
            _database = Guard.NotNull(database, nameof(database));
            _hookManager = Guard.NotNull(hookManager, nameof(hookManager));
            DatabaseSnapshot = _database.StartSnapshot().Build();
        }
        /// <inheritdoc/>
        public IReadOnlyDatabase DatabaseSnapshot
        {
            get
            {
                lock (_lock)
                {
                    return _databaseSnapshot;
                }
            }
            protected set
            {
                lock (_lock)
                {
                    _databaseSnapshot = Guard.NotNull(value, nameof(DatabaseSnapshot));
                }
            }
        }
        /// <inheritdoc/>
        public IReadOnlyDatabase Database => _database;

        /// <inheritdoc/>
        public bool Destroyed<T>(T data, ref IndexMetadata metadata) where T : class
            => AsTyped<T>().Destroyed(data, ref metadata);

        /// <inheritdoc/>
        public bool Push<T>(T data, ref IndexMetadata metadata) where T : class
            => AsTyped<T>().Push(data, ref metadata);

        /// <inheritdoc/>
        public void Reset(Action<ISnapshotManagerConfigurator> configurator, Action<IDatabaseSchemaBuilder> schemaBuilder)
        {
            schemaBuilder = Guard.NotNull(schemaBuilder, nameof(schemaBuilder));
            Logger.Log("Snapshot manager resetting and redeploying database");
            lock (_lock)
            {
                var config = new ConfigureSnapshotManager();
                configurator?.Invoke(config);
                _changeTrackers = config.changeTrackers?.ToArray();
                _changeTrackerCache.Clear();
                foreach(var typedManager in _typedManagers.Values)
                {
                    typedManager.Drain();
                }
                _typedManagers.Clear();
                _database.Deploy(schemaBuilder);
                _queueEnabled = false;
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
                        _hookManager.Trigger(new OnSnapshotTakenTrigger(snapshot, isForce));
                    }
                    else
                    {
                        _hookManager.TriggerDelayed(new OnSnapshotTakenTrigger(snapshot, isForce));
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
            delegate bool Changed(T current, IIndexed<T> indexed, ref IndexMetadata metadata);
            private readonly SnapshotManager _manager;
            private readonly IDatabase<T> _typedDb;
            private readonly IChangeTracker<T>[] _changeTrackers;
            private readonly Queue<PendingUpsert> _work = new Queue<PendingUpsert>();
            private readonly Changed _hasChanged;

            public override int Pending => _work.Count;

            public TypedSnapshotManager(SnapshotManager manager)
            {
                _manager = Guard.NotNull(manager, nameof(manager));
                _typedDb = manager._database.AsTyped<T>();
                _changeTrackers = manager.GetChangeTrackers<T>();
                _hasChanged = Compile();
            }

            /// <inheritdoc/>
            public bool Push(T data, ref IndexMetadata metadata)
            {
                data = Guard.NotNull(data, nameof(data));
                var existing = _manager._database.Find<T>(data);

                if(!_manager._queueEnabled) return Push(data, existing, ref metadata);

                bool accepted = false;
                if(_work.Count == 0)
                {
                    var context = new CooperativeWorkContext();
                    var work = RaiseCooperativeWork.From(() => DoWork(context).GetEnumerator(), context);
                    accepted = _manager._hookManager.Trigger(work);
                }
                else
                {
                    accepted = true;
                }

                if (!accepted)
                {
                    return Push(data, existing, ref metadata);
                }

                var pendingWork = Toolkit.Pool<PendingUpsert>.Rent();
                pendingWork.Data = data;
                pendingWork.Existing = existing;
                pendingWork.Metadata = metadata;
                pendingWork.IsDelete = false;
                _work.Enqueue(pendingWork);
                return true;
            }

            private bool Push(T data, IIndexed<T> existing, ref IndexMetadata metadata)
            {
                if (!HasChanged(data, existing, ref metadata) && existing is not null)
                {
                    return false;
                }
                return _typedDb.Update(data, existing, ref metadata);
            }

            /// <inheritdoc/>
            public bool Destroyed(T data, ref IndexMetadata metadata)
            {
                data = Guard.NotNull(data, nameof(data));
                if (!_manager._queueEnabled) return ActualDestroyed(data, ref metadata);

                bool accepted = false;
                if (_work.Count == 0)
                {
                    var context = new CooperativeWorkContext();
                    var work = RaiseCooperativeWork.From(() => DoWork(context).GetEnumerator(), context);
                    accepted = _manager._hookManager.Trigger(work);
                }
                else
                {
                    accepted = true;
                }

                if (!accepted)
                {
                    return ActualDestroyed(data, ref metadata);
                }

                var pendingWork = Toolkit.Pool<PendingUpsert>.Rent();
                pendingWork.Data = data;
                pendingWork.Existing = null;
                pendingWork.Metadata = metadata;
                pendingWork.IsDelete = true;
                _work.Enqueue(pendingWork);
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
                context.CheckInterval = 4;
                while (_work.TryDequeue(out var work)) 
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
                            Push(work.Data, work.Existing, ref work.Metadata);
                        }
                    }
                    if (context.WaitForNextTick)
                    {
                        yield return null;
                    }
                }
            }

            private Changed Compile()
            {
                var currentParameter = Expression.Parameter(typeof(T), "current");
                var indexParameter = Expression.Parameter(typeof(IIndexed<T>), "index");
                var metadataParameter = Expression.Parameter(typeof(IndexMetadata).MakeByRefType(), "metadata");

                Expression current = null;
                IndexMetadata metadata = default;
                var changedMethod = Toolkit.Helpers.Expression.GetMethod<IChangeTracker<T>>(x => x.HasChanged(default, default, ref metadata));
                for (int i = 0; i < _changeTrackers.Length; i++) { 
                    var changeTracker = _changeTrackers[i];

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

            private class PendingUpsert : IPoolable, IDisposable
            {
                public T Data;
                public IIndexed<T> Existing;
                public IndexMetadata Metadata;
                public bool IsDelete;

                public void Dispose()
                {
                    Toolkit.Pool<PendingUpsert>.Return(this);
                }

                public void Reset()
                {
                    Data = null;
                    Existing = null;
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
