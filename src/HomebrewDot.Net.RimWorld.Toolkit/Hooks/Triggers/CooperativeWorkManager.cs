using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Eventing.Models;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Generic.Components;
using HomebrewDot.Net.Rimworld.Indexing;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Hooks.Triggers
{
    /// <summary>
    /// Game component that receives work via hooks and tries to spread the load across ticks.
    /// </summary>
    public class CooperativeWorkManager : GameComponent, IHook<RaiseCooperativeWork>
    {
        // Fields
        private readonly Game _game;

        // State
        private Queue<RaiseCooperativeWork> _finalize = new Queue<RaiseCooperativeWork>();
        private Queue<RaiseCooperativeWork> _currentCycle = new Queue<RaiseCooperativeWork>();
        private Queue<RaiseCooperativeWork> _nextCycle = new Queue<RaiseCooperativeWork>();

        /// <summary>
        /// Creates the work manager component for the current game instance.
        /// </summary>
        /// <param name="game">The game instance that owns this component.</param>
        public CooperativeWorkManager(Game game)
        {
            _game = Toolkit.Helpers.Guard.NotNull(game, nameof(game));
        }

        /// <inheritdoc/>
        object IHook<RaiseCooperativeWork>.Owner => this;
        /// <inheritdoc/>
        bool IHook<RaiseCooperativeWork>.Once => false;
        /// <inheritdoc/>
        byte IHandler.Priority => byte.MinValue;
        /// <inheritdoc/>
        bool IHook<RaiseCooperativeWork>.OnTrigger(RaiseCooperativeWork arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            _nextCycle.Enqueue(arg);
            if (IsVerboseEnabled) LogVerbose($"Accepted new work to run next cycle");
            return true;
        }
        /// <inheritdoc/>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Toolkit.Hooks.Manager.RegisterHook<RaiseCooperativeWork>(this);
        }
        /// <inheritdoc/>
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            var budget = new TimeSpan(1000L);
            ExecuteWork(budget);
        }

        private void ExecuteWork(TimeSpan budget)
        {
            var stopwatch = Stopwatch.StartNew();
            if (_currentCycle.Count == 0)
            {
                (_currentCycle, _nextCycle) = (_nextCycle, _currentCycle);
            }

            bool overbudget = false;
            while(_finalize.TryDequeue(out var completed))
            {
                completed.Complete();
                if(completed is IDisposable disposable) disposable.Dispose();
                if (budget < stopwatch.Elapsed)
                {
                    overbudget = true;
                    break;
                }
            }

            if (overbudget)
            {
                return;
            }

            while (_currentCycle.TryDequeue(out var pending))
            {
                bool returnToQueue = true;
                bool started = false;
                if (pending.startedWork is null)
                {
                    returnToQueue = Invoking.Safe(() =>
                    {
                        pending.startedWork = pending.startWork(budget, stopwatch);
                        started = true;
                        return !pending.startedWork.IsFinished;
                    }, false);
                }
                else
                {
                    pending.startedWork.Context.Prepare(budget, stopwatch);
                    returnToQueue = Invoking.Safe(() =>
                    {
                        return !pending.startedWork.Continue();
                    });
                }
                if (!returnToQueue)
                {
                    if (IsPerformanceEnabled) LogPerformance($"Completed work {pending.startedWork} ({stopwatch.Elapsed.TotalMilliseconds}ms)");
                    if(pending.onCompleted != null)
                    {
                        _finalize.Enqueue(pending);
                    }
                    else if (pending is IDisposable disposable) disposable.Dispose();
                }
                else
                {
                    if (IsPerformanceEnabled)
                    {
                        if (started)
                        {
                            LogPerformance($"Started work {pending.startedWork} ({stopwatch.Elapsed.TotalMilliseconds}ms)");
                        }
                        else
                        {
                            LogPerformance($"Continued work {pending.startedWork} ({stopwatch.Elapsed.TotalMilliseconds}ms)");
                        }
                    }
                    _nextCycle.Enqueue(pending);
                }
                if(budget < stopwatch.Elapsed)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Base context that <see cref="CooperativeWorkManager"/> uses to coördinate work when running <see cref="SyncPendingWork{T}"/>
    /// </summary>
    public class CooperativeWorkContext
    {
        private int CurrentInterval;
        private bool IsCheckInterval;
        private bool _noInterval;

        /// <summary>
        /// How any actions were executed this tick.
        /// </summary>
        public int CurrentActions;
        /// <summary>
        /// How often <see cref="Stopwatch"/> will be called based on calls to <see cref="LogWork"/> in <see cref="WaitForNextTick"/>.
        /// If work is fast this should be higher since checking the stopwatch also takes time.
        /// </summary>
        public int CheckInterval = 4;
        /// <summary>
        /// Total number of actions executed across all ticks.
        /// </summary>
        public int TotalActions;

        /// <summary>
        /// Stopwatch that will be set by the manager to limit the runtime of work each tick.
        /// </summary>
        public Stopwatch Stopwatch;
        /// <summary>
        /// The maximum time that will be set by the manager that pending work should spend this tick.
        /// </summary>
        public TimeSpan MaxRuntime;

        /// <summary>
        /// If the current method call should yield the control back to the caller using. (yield return null)
        /// Should be called inside tight loops together with <see cref="LogWork"/> to keep track of actions.
        /// </summary>
        public bool WaitForNextTick => !_noInterval && IsCheckInterval && IsOverRunTime;
        /// <summary>
        /// If the current method call should yield the control back to the caller using. (yield return null)
        /// Should be called outside loops after large amount of work was performed. (cpu heavy stuff)
        /// </summary>
        public bool IsOverRunTime => !_noInterval && Stopwatch?.Elapsed >= MaxRuntime;

        /// <summary>
        /// Logs arbitrary work done in loops.
        /// </summary>
        public void LogWork()
        {
            CurrentActions++;
            TotalActions++;
            if (_noInterval) return;
            if (CurrentInterval == 0)
            {
                IsCheckInterval = false;
            }
            CurrentInterval++;
            if (CurrentInterval == CheckInterval)
            {
                IsCheckInterval = true;
                CurrentInterval = 0;
            }
        }

        /// <summary>
        /// Can be called so work completes in one go by returning false on <see cref="WaitForNextTick"/> and <see cref="IsOverRunTime"/>.
        /// </summary>
        public void NoInterval()
        {
            _noInterval = true;
            MaxRuntime = TimeSpan.MaxValue;
            Stopwatch = null;
        }

        internal void Prepare(TimeSpan budget, Stopwatch stopwatch)
        {
            _noInterval = false;
            Stopwatch = stopwatch;
            CurrentActions = 0;
            CurrentInterval = 0;
            MaxRuntime = budget;
        }
    }

    /// <summary>
    /// Types version of <see cref="RaiseCooperativeWork"/> that can be pooled.
    /// </summary>
    /// <typeparam name="T">Type of work context used</typeparam>
    public class RaiseCooperativeWork<T> : RaiseCooperativeWork, IDisposable, IPoolable
    {
        /// <summary>
        /// Releases the work back to the pool.
        /// </summary>
        public void Dispose()
        {
            if (startedWork is SyncPendingWork<T> pendingWork)
            {
                Toolkit.Pool<SyncPendingWork<T>>.Return(pendingWork);
                startedWork = null;
            }
        }
        /// <inheritdoc/>
        public void Reset()
        {
            startedWork = null;
            startWork = null;
            onCompleted = null;
        }
    }
    /// <summary>
    /// Event/work that can be raised to execute <see cref="ISyncWork"/> each tick until done to spread out load.
    /// </summary>
    public class RaiseCooperativeWork
    {
        internal ISyncRunningWork<CooperativeWorkContext> startedWork;
        internal Func<TimeSpan, Stopwatch, ISyncRunningWork<CooperativeWorkContext>> startWork;
        internal Action onCompleted;

        // Properties
        /// <summary>
        /// The sync work if it has been started.
        /// </summary>
        public ISyncRunningWork<CooperativeWorkContext> Started { get; }

        protected RaiseCooperativeWork()
        {

        }

        /// <summary>
        /// Runs the work manually.
        /// </summary>
        public void RunManually()
        {
            if (startedWork == null) {
                startedWork = startWork(TimeSpan.MaxValue, null);
            }

            while (!startedWork.IsFinished)
            {
                startedWork.Context.NoInterval();
                startedWork.Continue();
            }

            Complete();
        }

        /// <summary>
        /// Adds a delegate that will be called when the current work is finished.
        /// </summary>
        /// <param name="onCompleted">The delegate to raise</param>
        public void OnCompleted(Action onCompleted)
        {
            onCompleted = Guard.NotNull(onCompleted, nameof(onCompleted));

            if(this.onCompleted == null)
            {
                this.onCompleted = onCompleted;
            }
            else
            {
                this.onCompleted += onCompleted;
            }
        }

        /// <summary>
        /// Creates a new instance with pending work to start.
        /// </summary>
        /// <typeparam name="T">The type of work context used by the work</typeparam>
        /// <param name="startWork">Delegate that returns the <see cref="IEnumerator"/> used to start the work. Will be invoked by the manager when the tick has time budget left to execute</param>
        /// <param name="context">The context that will be used once the work is started</param>
        /// <param name="onCompletion">Optional delegate that will be called when the work is finished</param>
        /// <returns>The event that can be raised with <see cref="IHookManager"/> so the manager can accept it</returns>
        public static RaiseCooperativeWork From<T>(Func<IEnumerator> startWork, T context, Action<T> onCompletion = null) where T : CooperativeWorkContext
        {
            startWork = Guard.NotNull(startWork, nameof(startWork));
            context = Guard.NotNull(context, nameof(context));

            var work = Toolkit.Pool<RaiseCooperativeWork<T>>.Rent();
            work.startWork = (t, s) =>
            {
                var pendingWork = Toolkit.Pool<SyncPendingWork<T>>.Rent();
                pendingWork.timeoutSelector = x => x.MaxRuntime;
                pendingWork.trackerSelector = x => x.Stopwatch;
                context.Prepare(t, s);
                context.TotalActions = 0;
                _ = pendingWork.Start(startWork(), context);
                return pendingWork;
            };
            if (onCompletion != null)
            {
                work.onCompleted = () => onCompletion(context);
            }
            return work;
        }

        /// <summary>
        /// Creates a new instance with pending work to start.
        /// </summary>
        /// <typeparam name="T">The type of work context used by the work</typeparam>
        /// <param name="startWork">Delegate that returns the <see cref="IEnumerator"/> used to start the work. Will be invoked by the manager when the tick has time budget left to execute</param>
        /// <param name="onCompletion">Optional delegate that will be called when the work is finished</param>
        /// <returns>The event that can be raised with <see cref="IHookManager"/> so the manager can accept it</returns>
        public static RaiseCooperativeWork From(Func<IEnumerator> startWork, Action onCompletion = null)
        {
            startWork = Guard.NotNull(startWork, nameof(startWork));

            var context = new CooperativeWorkContext();
            var work = From<CooperativeWorkContext>(startWork, context);
            if (onCompletion != null)
            {
                work.onCompleted = onCompletion;
            }
            return work;
        }

        internal void Complete()
        {
            if(onCompleted != null)
            {
                Invoking.Safe(() => onCompleted());
            }
        }
    }
}
