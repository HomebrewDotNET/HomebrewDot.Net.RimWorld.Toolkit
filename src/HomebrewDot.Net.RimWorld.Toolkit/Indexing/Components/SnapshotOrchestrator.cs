using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Verse;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Default implementation of <see cref="ISnapshotOrchestrator"/> that uses hooks to manage snapshot lifecycles.
    /// </summary>
    public class SnapshotOrchestrator : ISnapshotOrchestrator, ISnapshotOrchestratorBuilder, IDisposable
    {
        // Fields
        private readonly object _lock = new object();
        private readonly HashSet<IDataGatherer> _dataGatherers = new HashSet<IDataGatherer>();
        private readonly IHookManager _hookManager;
        private readonly bool _useLongTicks;
        private ISnapshotManager _snapshotManager;
        private Game _game;

        /// <inheritdoc cref="SnapshotOrchestrator"/>
        /// <param name="hookManager">Used to hook into certain game events.</param>
        /// <param name="useLongTicks">Indicates whether to use long ticks for snapshot orchestration.</param>
        public SnapshotOrchestrator(IHookManager hookManager, bool useLongTicks)
        {
            _hookManager = Guard.NotNull(hookManager, nameof(hookManager));
            _useLongTicks = useLongTicks;
        }

        /// <inheritdoc/>
        public void RebuildIndex(Game game,
            bool isGameStartup,
            ISnapshotManager snapshotManager,
            Action<ISnapshotOrchestratorBuilder> configure,
            Action<ISnapshotManagerConfigurator> configureManager,
            Action<IDatabaseSchemaBuilder> schemaBuilder)
        {
            snapshotManager = Guard.NotNull(snapshotManager, nameof(snapshotManager));

            lock (_lock)
            {
                _game = game;
                _snapshotManager = snapshotManager;

                // Remove all previous hooks to avoid duplicates
                _hookManager.UnregisterAllBy<OnGameTickTrigger>(this);
                _hookManager.UnregisterAllBy<MapLifecycleTrigger>(this);

                // Reset gathers if we have them
                foreach (var gatherer in _dataGatherers)
                {
                    try
                    {
                        gatherer.Reset();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error resetting data gatherer {gatherer.GetType().FullName}: {ex}");
                    }
                }
                _dataGatherers.Clear();

                // Reset snapshot manager
                try
                {
                    snapshotManager.Reset(configureManager, schemaBuilder);
                }
                catch (Exception ex)
                {
                    LogError($"Error resetting snapshot manager: {ex}");
                }

                // Configure the orchestrator
                configure?.Invoke(this);

                // Initialize gatherers
                foreach (var gatherer in _dataGatherers.ToArray())
                {
                    try
                    {
                        gatherer.Initialize(game);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error initializing data gatherer {gatherer.GetType().FullName}: {ex}");
                        _dataGatherers.Remove(gatherer);
                    }
                }

                // Gather initial data immediately on startup
                Log($"Starting snapshot orchestration for game {game} using {_dataGatherers.Count} data gatherers.");
                foreach (var gatherer in _dataGatherers)
                {
                    LogVerbose($"Starting gathering with {gatherer.GetType().FullName}");
                    try
                    {
                        gatherer.GatherData(game, snapshotManager);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error gathering data with {gatherer.GetType().FullName}: {ex}");
                    }
                }

                // Hook into ticks to manage lifecycle
                _hookManager.RegisterHook<OnGameTickTrigger>(this, e =>
                {
                    var tickerType = _useLongTicks ? TickerType.Long : TickerType.Rare;

                    if (e.TickerType != tickerType)
                    {
                        return;
                    }

                    LogVerbose($"Preparing to take new snapshot");
                    // Notify listeners that we're about to take a snapshot
                    _hookManager.LazyTrigger<PreparingSnapshotTrigger>(() => new PreparingSnapshotTrigger(_snapshotManager));
                    // Subscribe to next tick to take snapshot after all preparations are done
                    _hookManager.RegisterHook<OnGameTickTrigger>(this, t =>
                    {
                        if (t.TickerType != TickerType.Normal)
                        {
                            return false;
                        }
                        LogVerbose($"Taking snapshot");
                        try
                        {
                            _snapshotManager.Snapshot();
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error taking snapshot: {ex}");
                        }
                        return true; // Unregister after triggering
                    }, true, priority: byte.MaxValue);
                });
            }

            Log($"Snapshot orchestrator initialized for game {game} with {_dataGatherers.Count} data gatherers.");
        }

        /// <inheritdoc/>
        public void ForceSnapshot()
        {
            Log($"Forcing snapshot for game {_game}. Current version is {_snapshotManager?.DatabaseSnapshot?.Version ?? '?'}");
            _snapshotManager?.Snapshot();
            Log($"Snapshot forced for game {_game}. New version is {_snapshotManager?.DatabaseSnapshot?.Version ?? '?'}");
        }

        /// <inheritdoc/>
        ISnapshotOrchestratorBuilder ISnapshotOrchestratorBuilder.With(IDataGatherer dataGatherer)
        {
            lock(_lock)
            {
                if (!_dataGatherers.Contains(Guard.NotNull(dataGatherer, nameof(dataGatherer))))
                {
                    _dataGatherers.Add(dataGatherer);
                }
            }
            return this;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_lock)
            {
                // Unregister all hooks to clean up
                _hookManager.UnregisterAllBy<OnGameTickTrigger>(this);
                // Reset gatherers
                foreach (var gatherer in _dataGatherers)
                {
                    try
                    {
                        gatherer.Reset();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error resetting data gatherer {gatherer.GetType().FullName} during disposal: {ex}");
                    }
                }
                _dataGatherers.Clear();
                _snapshotManager = null;
                _game = null;
            }
        }
    }
}
