using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Verse;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;
using System.Diagnostics;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Default implementation of <see cref="ISnapshotOrchestrator"/> that uses hooks to manage snapshot lifecycles.
    /// </summary>
    public class SnapshotOrchestrator : ISnapshotOrchestrator, ISnapshotOrchestratorBuilder, IDisposable
    {
        // Fields
        private readonly HashSet<IDataGatherer> _dataGatherers = new HashSet<IDataGatherer>();
        private readonly IHookManager _hookManager;
        private readonly bool _useLongTicks;
        private ISnapshotManager _snapshotManager;
        private Game _game;

        // State 
        private ISnapshotBuilder _pending;

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
                    if (IsVerboseEnabled) LogVerbose($"Starting gathering with {gatherer.GetType().FullName}");
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
                    var useLongTicks = Invoking.Safe(() => Toolkit.Settings.SlowGatheringEnabled, _useLongTicks);
                    var tickerType = useLongTicks ? TickerType.Long : TickerType.Rare;

                    var isSnapshotWindow = e.TickerType == tickerType;

					if (_pending is not null)
                    {
                        if (!_pending.IsFinished)
                        {
							if (isSnapshotWindow)
							{
								LogWarning("Orchestrator can not keep up with changes. Forcing finialize");
								_ = _pending.Build();
								_pending = null;
								_ = _snapshotManager.Snapshot().Build();
							}
                        }
                        else
                        {
                            _pending = null;
                            // Snapshot work finished so notify manager to finalize
                            _ = _snapshotManager.Snapshot();
                        }
                    }

                    if (!isSnapshotWindow)
                    {
                        return;
                    }

                    if (IsVerboseEnabled) LogVerbose("Starting snapshot");
                    try
                    {
                        _pending = _snapshotManager.Snapshot();
                        if (_pending.IsFinished)
                        {
                            _pending = null;
                        }
                        else
                        {
                            var work = _pending.CreateWork();
                            _hookManager.Trigger(work);
                        }
					}
                    catch (Exception ex)
                    {
                        LogError($"Error starting snapshot: {ex}");
                    }
                });
            }

            Log($"Snapshot orchestrator initialized for game {game} with {_dataGatherers.Count} data gatherers.");
        }

        /// <inheritdoc/>
        public void ForceSnapshot()
        {
            Log($"Forcing snapshot for game {_game}. Current version is {_snapshotManager?.DatabaseSnapshot?.Version ?? '?'}");
            _ = _snapshotManager?.Snapshot(true).Build();
            Log($"Snapshot forced for game {_game}. New version is {_snapshotManager?.DatabaseSnapshot?.Version ?? '?'}");
        }

        /// <inheritdoc/>
        ISnapshotOrchestratorBuilder ISnapshotOrchestratorBuilder.With(IDataGatherer dataGatherer)
        {
            if (!_dataGatherers.Contains(Guard.NotNull(dataGatherer, nameof(dataGatherer))))
            {
                _dataGatherers.Add(dataGatherer);
            }
            return this;
        }

        /// <inheritdoc />
        public void Dispose()
        {
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
