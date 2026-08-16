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
        // Static
        private static readonly TimeSpan DefaultBudget = new TimeSpan(1000L);
        private static readonly TimeSpan IncrementInterval = new TimeSpan(500L);
        private static readonly TimeSpan DecrementInterval = new TimeSpan(200L);
        private static readonly TimeSpan MaxTickSpike = TimeSpan.FromMilliseconds(1);
        private static readonly int SuccessCyclesNeededToDecrease = 50;
        private static readonly int FailureCyclesNeededToIncrease = 3;
        private static readonly int MinCancelledWorkToIncrease = 5;

        // Fields
        private readonly Game _game;

        // State
        private Queue<RaiseCooperativeWork> _finalize = new Queue<RaiseCooperativeWork>();
        private Queue<RaiseCooperativeWork> _currentCycle = new Queue<RaiseCooperativeWork>();
        private Queue<RaiseCooperativeWork> _nextCycle = new Queue<RaiseCooperativeWork>();
        private int _acceptedThisTick;
        private TimeSpan _currentBudget = DefaultBudget;
        private bool _lastCycleOverBudget;
        private int _currentCycleStreak = 0;
        private int? _lastPendingWork;
        private TimeSpan _highestTimeCurrentCycle;
        private long _lastLogTick = 0;

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
        bool IHook<RaiseCooperativeWork>.GameScoped => true;

        /// <inheritdoc/>
        public bool OnTrigger(RaiseCooperativeWork arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            _nextCycle.Enqueue(arg);
            CooperativeWorkContext.Stats.AcceptedWork++;
            _acceptedThisTick++;
            CooperativeWorkContext.Stats.AcceptedThisBudgetCycle++;
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
            var timer = Stopwatch.StartNew();
            base.GameComponentTick();
            CooperativeWorkContext.Stats.PendingCurrentCycle = _currentCycle.Count;
            CooperativeWorkContext.Stats.PendingNextCycle = _nextCycle.Count;
            CooperativeWorkContext.Stats.PendingFinalize = _finalize.Count;
            var ticks = ++CooperativeWorkContext.Stats.Ticks;
            var calculateBudget = Toolkit.Helpers.GetTickerType(ticks) == TickerType.Long;
            if(calculateBudget)
            {
                AdjustBudget();
            }
            var budget = _currentBudget;
            ExecuteWork(budget);

            if (_currentCycle.Count == 0)
            {
                (_currentCycle, _nextCycle) = (_nextCycle, _currentCycle);
            }
            _acceptedThisTick = 0;
            timer.Stop();
            if(IsPerformanceEnabled)
            {
                bool isLogTick = (ticks - _lastLogTick) % ToolkitConstants.TickRareInterval == 0;
                if(isLogTick)
                {
                    _lastLogTick = ticks;
                    LogPerformance($"Tick {ticks} completed in {timer.Elapsed.TotalMilliseconds}ms (budget={budget.TotalMilliseconds}ms, highest={_highestTimeCurrentCycle.TotalMilliseconds}ms, stats={CooperativeWorkContext.Stats})");
                }
            }
            if(timer.Elapsed > MaxTickSpike)
            {
                _highestTimeCurrentCycle = timer.Elapsed;
            }
        }

        private bool ExecuteWork(TimeSpan budget)
        {
            var stopwatch = Stopwatch.StartNew();
            bool overbudget = false;
            bool workPerformed = false;
            while (_finalize.TryDequeue(out var completed))
            {
                completed?.startedWork?.Context?.LogCycle();
                completed.Complete(this);
                if(completed is IDisposable disposable) disposable.Dispose();
                if (budget < stopwatch.Elapsed)
                {
                    overbudget = true;
                    break;
                }
            }

            if (overbudget)
            {
                return false;
            }

            while (_currentCycle.TryDequeue(out var pending))
            {
                if(pending.IsFinished)
                {
                    if (IsPerformanceEnabled) LogPerformance($"Skipping finished work {pending}");
                    continue;
                }
                workPerformed = true;
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
                pending.startedWork?.Context?.LogCycle();
                if (!returnToQueue)
                {
                    if (IsPerformanceEnabled) LogPerformance($"Completed work {pending.startedWork} ({stopwatch.Elapsed.TotalMilliseconds}ms)");
                    pending.MarkCompleted();
                    if(pending.RequiresCompletion)
                    {
                        _finalize.Enqueue(pending);
                    }
                    else
                    {
                        if (!pending.IsCanceled)
                        {
                            CooperativeWorkContext.Stats.CompletedWork++;
                            CooperativeWorkContext.Stats.CompletedThisBudgetCycle++;
                        }
                        if (pending is IDisposable disposable) disposable.Dispose();
                    }
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
            return workPerformed;
        }

        private void AdjustBudget()
        {
            var acceptedThisCycle = CooperativeWorkContext.Stats.AcceptedThisBudgetCycle-_acceptedThisTick;
            var completedThisCycle = CooperativeWorkContext.Stats.CompletedThisBudgetCycle;
            var canceledThisCycle = CooperativeWorkContext.Stats.CanceledThisBudgetCycle;
            var change = acceptedThisCycle - completedThisCycle - canceledThisCycle;
            var highestTime = _highestTimeCurrentCycle;
            var pendingWork = _currentCycle.Count + _nextCycle.Count + _finalize.Count;
            var overbudget = ShouldIncreaseBudget(_lastPendingWork, pendingWork, canceledThisCycle);
            var canDecrease = !overbudget && highestTime <= MaxTickSpike;

            if (overbudget && _lastCycleOverBudget)
            {
                _currentCycleStreak++;
                if(_currentCycleStreak >= FailureCyclesNeededToIncrease)
                {
                    _currentBudget += IncrementInterval;
                    _currentCycleStreak = 0;
                    Logging.Log($"Could not keep up with growing workload (change={change}, pending={pendingWork}, canceled={canceledThisCycle}, highestTime={highestTime.TotalMilliseconds}ms, stats={CooperativeWorkContext.Stats}) in {FailureCyclesNeededToIncrease} cycles, increasing budget. Current tick budget is {_currentBudget.TotalMilliseconds}ms");
                }
            }
            else if (canDecrease && !_lastCycleOverBudget)
            {
                _currentCycleStreak++;
                if (_currentCycleStreak >= SuccessCyclesNeededToDecrease && _currentBudget - DecrementInterval >= DefaultBudget)
                {
                    _currentBudget -= DecrementInterval;
                    _currentCycleStreak = 0;
                    Logging.Log($"Successfully kept up with current workload (change={change}, pending={pendingWork}, canceled={canceledThisCycle}, highestTime={highestTime.TotalMilliseconds}ms, stats={CooperativeWorkContext.Stats}) in {SuccessCyclesNeededToDecrease} cycles, decreasing budget. Current tick budget is {_currentBudget.TotalMilliseconds}ms");
                }
            }
            else
            {
                _currentCycleStreak = 0;
            }
            _lastCycleOverBudget = overbudget;
            _lastPendingWork = pendingWork;

            CooperativeWorkContext.Stats.AcceptedThisBudgetCycle = 0;
            CooperativeWorkContext.Stats.CanceledThisBudgetCycle = 0;
            CooperativeWorkContext.Stats.CompletedThisBudgetCycle = 0;
            _highestTimeCurrentCycle = TimeSpan.Zero;
        }

        internal static bool ShouldIncreaseBudget(int? previousPendingWork, int pendingWork, int canceledWork)
        {
            return previousPendingWork.HasValue && pendingWork > previousPendingWork.Value || canceledWork >= MinCancelledWorkToIncrease;
        }
    }

    /// <summary>
    /// Base context that <see cref="CooperativeWorkManager"/> uses to coördinate work when running <see cref="SyncPendingWork{T}"/>
    /// </summary>
    public class CooperativeWorkContext
    {
        // Statics
        /// <summary>
        /// Singleton instance of the <see cref="CooperativeWorkManagerStats"/> that can be used to see how many work items are pending and how many ticks have passed since the last reset.
        /// </summary>
        public static CooperativeWorkManagerStats Stats { get; } = new CooperativeWorkManagerStats();

        // Fields
        private int CurrentInterval;
        private bool IsCheckInterval;
        private bool _noInterval;
        private int _cycles;

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

        // Properties
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
        /// How many times the work was invoked by the manager.
        /// </summary>
        public int CyclesPerformed => _cycles;

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

        internal void LogCycle()
        {
            _cycles++;
        }
    }

    /// <summary>
    /// Stats that can be used to see how many work items are pending and how many ticks have passed since the last reset.
    /// </summary>
    public class CooperativeWorkManagerStats
    {
        internal int AcceptedThisBudgetCycle;
        internal int CanceledThisBudgetCycle;
        internal int CompletedThisBudgetCycle;

        /// <summary>
        /// How many pending work is in the current cycle. This is updated each tick by the <see cref="CooperativeWorkManager"/> and can be used to see how much work is pending.
        /// </summary>
        public int PendingCurrentCycle { get; internal set; }
        /// <summary>
        /// How many pending work is in the next cycle. This is updated each tick by the <see cref="CooperativeWorkManager"/> and can be used to see how much work is pending.
        /// </summary>
        public int PendingNextCycle { get; internal set; }
        /// <summary>
        /// How many pending work is in the finalize queue. This is updated each tick by the <see cref="CooperativeWorkManager"/> and can be used to see how much work is pending.
        /// </summary>
        public int PendingFinalize { get; internal set; }

        /// <summary>
        /// How many times the <see cref="CooperativeWorkManager"/> has ticked since the last reset. This is updated each tick by the <see cref="CooperativeWorkManager"/> and can be used to see how many ticks have passed.
        /// </summary>
        public long Ticks { get; internal set; }
        /// <summary>
        /// How many work items have been accepted since the last reset. This is updated each tick by the <see cref="CooperativeWorkManager"/> and can be used to see how many work items have been accepted.
        /// </summary>
        public long AcceptedWork { get; internal set; }
        /// <summary>
        /// How many work items have been completed since the last reset. This is updated each tick by the <see cref="CooperativeWorkManager"/> and can be used to see how many work items have been completed.
        /// </summary>
        public long CompletedWork { get; internal set; }
        /// <summary>
        /// How many work items have been canceled since the last reset. This is updated each tick by the <see cref="CooperativeWorkManager"/> and can be used to see how many work items have been canceled.
        /// </summary>
        public long CanceledWork { get; internal set; }

        /// <summary>
        /// Returns a string representation of the current stats, including pending work, ticks, completed work, and canceled work.
        /// </summary>
        /// <returns>A string representation of the current stats.</returns>
        public override string ToString()
        {
            return $"PendingCurrentCycle={PendingCurrentCycle}, PendingNextCycle={PendingNextCycle}, PendingFinalize={PendingFinalize}, AcceptedThisBudgetCycle={AcceptedThisBudgetCycle}, CompletedThisBudgetCycle={CompletedThisBudgetCycle}, CanceledThisBudgetCycle={CanceledThisBudgetCycle}, Ticks={Ticks}, AcceptedWork={AcceptedWork}, CompletedWork={CompletedWork}, CanceledWork={CanceledWork}";
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
            ResetState();
            startedWork = null;
            startWork = null;
            onCompleted = null;
            next = null;
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
        internal RaiseCooperativeWork next;

        // Properties
        /// <summary>
        /// The sync work if it has been started.
        /// </summary>
        public ISyncRunningWork<CooperativeWorkContext> Started => startedWork;
        /// <summary>
        /// If the current work has been canceled. (not started or finished)
        /// </summary>
        public bool IsCanceled { get; private set; }
        /// <summary>
        /// If the current work has been completed. Stays set after the work is disposed and returned to the pool so completed work stays distinguishable from work that was never started.
        /// </summary>
        internal bool IsCompleted { get; private set; }
        /// <summary>
        /// If the current work has been started and finished or canceled.
        /// </summary>
        public bool IsFinished => IsCanceled || IsCompleted || (startedWork?.IsFinished ?? false);
        /// <summary>
        /// If the current work requires completion.
        /// </summary>
        public bool RequiresCompletion => !IsCanceled && (onCompleted != null || next != null);

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

            Complete(null);
            if(this is IDisposable disposable) disposable.Dispose();
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
        /// Chains another <see cref="RaiseCooperativeWork"/> to be executed after the current work is finished.
        /// </summary>
        /// <param name="next">The next work to be executed</param>
        public void Chain(RaiseCooperativeWork next)
        {
            next = Guard.NotNull(next, nameof(next));
            if(this.next == null)
            {
                this.next = next;
            }
            else
            {
                this.next.Chain(next);
            }
        }

        /// <summary>
        /// Cancels the current work. Will not call <see cref="onCompleted"/> when canceled.
        /// </summary>
        public void Cancel()
        {
            if (IsCanceled || IsFinished)
            {
                return;
            }
            IsCanceled = true;
            CooperativeWorkContext.Stats.CanceledWork++;
            CooperativeWorkContext.Stats.CanceledThisBudgetCycle++;
        }

        /// <summary>
        /// Marks the work as completed so it stays finished after it is disposed and returned to the pool.
        /// </summary>
        internal void MarkCompleted()
        {
            IsCompleted = true;
        }

        /// <summary>
        /// Resets the mutable completion state when the work is returned to the pool.
        /// </summary>
        internal void ResetState()
        {
            IsCanceled = false;
            IsCompleted = false;
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

        internal void Complete(CooperativeWorkManager manager)
        {
            if(startedWork?.IsFinished == true)
            {
                MarkCompleted();
                if (onCompleted != null && !IsCanceled)
                {
                    Invoking.Safe(() => onCompleted());
                }
                if (next != null)
                {
                    Invoking.Safe(() =>
                    {
                        if (manager != null)
                        {
                            manager.OnTrigger(next);
                        }
                        else
                        {
                            next.RunManually();
                        }
                    });
                }

                CooperativeWorkContext.Stats.CompletedWork++;
                CooperativeWorkContext.Stats.CompletedThisBudgetCycle++;
            }
        }
    }
}
