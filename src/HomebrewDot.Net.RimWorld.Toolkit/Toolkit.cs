using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.RimWorld.Hooks;
using HomebrewDot.Net.RimWorld.Hooks.Triggers;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Components;
using RimWorld;
using Verse;

namespace HomebrewDot.Net.RimWorld
{
    /// <summary>
    /// Central access point for all the tools in the HomebrewDot.Net library.
    /// </summary>
    public class Toolkit : Mod
    {
        // Statics
        private static object _lock = new object();
        private static Toolkit _instance;
        private static ToolkitSettings _settings;

        /// <summary>
        /// The unique identifier for this mod.
        /// </summary>
        public static string ModId { get; } = typeof(Toolkit).FullName;
        /// <summary>
        /// The Harmony instance used for patching methods.
        /// </summary>
        internal static Harmony Harmony { get; } = new Harmony(ModId);
        /// <summary>
        /// Singleton instance of the <see cref="Toolkit"/> class.
        /// </summary>
        public static Toolkit Instance { get => _instance ?? throw new ArgumentNullException($"Tried to access {nameof(Toolkit)} instance before it was initialized."); private set => _instance = value; }
        /// <summary>
        /// Contains the settings for the <see cref="Toolkit"/>. Accessing this property will initialize the settings if they haven't been already.
        /// </summary>
        public static ToolkitSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;
                lock (_lock)
                {
                    if (_settings == null)
                    {
                        _settings = Instance.GetSettings<ToolkitSettings>();
                    }
                }
                return _settings;
            }
        }

        /// <inhericdoc cref="Toolkit">
        /// <param name="content"></param>
        public Toolkit(ModContentPack content) : base(content)
        {
            Instance = this;
        }

        /// <summary>
        /// Tools for executing code at specific points during the game's execution using hooks.
        /// </summary>
        public static class Hooks
        {
            // Fields
            private static readonly object _lock = new object();
            private static IHookManager _manager;

            // Properties
            public static IHookManager Manager
            {
                get
                {
                    if (_manager != null) return _manager;
                    lock (_lock)
                    {
                        if (_manager == null)
                        {
                            _manager = new HookManager();
                        }
                    }
                    return _manager;
                }
                set
                {
                    lock (_lock)
                    {
                        if (_manager is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        _manager = value;
                    }
                }
            }
        }

        /// <summary>
        /// Tools for indexing game data using snapshots so it can be accessed in background threads.
        /// </summary>
        public static class Index
        {
            // Statics
            static Index()
            {
                Hooks.Manager.RegisterHook<OnGameLoadedTrigger>(Instance, (e) => StartIndexing(e.Game, true))
                             .RegisterHook<OnSaveLoadedTrigger>(Instance, (e) => StartIndexing(e.Game, false))
                             .RegisterHook<ToolkitSettings.Changed>(Instance, e => StartIndexing(Current.Game, false));
            }

            // Fields
            private static readonly object _lock = new object();
            private static ISnapshotOrchestrator _orchestrator;
            private static ISnapshotManager _manager;
            private static Action<ISnapshotOrchestratorBuilder> _orchestratorConfig = builder => { };
            private static Action<ISnapshotManagerConfigurator> _managerConfig = configurator => { };
            private static Action<IDatabaseSchemaBuilder> _schemaConfig = builder => { };

            // Properties
            /// <summary>
            /// The orchestrator responsible for managing the snapshot indexing process. Accessing this property will initialize the orchestrator if it hasn't been already.
            /// </summary>
            public static ISnapshotOrchestrator Orchestrator
            {
                get
                {
                    if (_orchestrator != null) return _orchestrator;
                    lock (_lock)
                    {
                        if (_orchestrator == null)
                        {
                            _orchestrator = new SnapshotOrchestrator(Toolkit.Hooks.Manager, Settings.SlowGatheringEnabled);
                        }
                    }
                    return _orchestrator;
                }
                set
                {
                    lock (_lock)
                    {
                        if (_orchestrator is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        _orchestrator = value;
                    }
                }
            }

            /// <summary>
            /// The manager responsible for handling snapshots and providing access to the current snapshot of indexed data. Accessing this property will initialize the manager if it hasn't been already.
            /// </summary>
            public static ISnapshotManager Manager 
            {
                get
                {
                    if (_manager != null) return _manager;
                    lock (_lock)
                    {
                        if (_manager == null)
                        {
                            _manager = new SnapshotManager(new Database(), Hooks.Manager);
                        }
                    }
                    return _manager;
                }
                set
                {
                    lock (_lock)
                    {
                        if (_manager is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        _manager = value;
                    }
                }
            }

            // Methods
            /// <summary>
            /// (Re)starts the snapshot orchestration using the currently configured options.
            /// </summary>
            /// <param name="game">The game instance to start the orchestration for</param>
            /// <param name="isGameStart">True if running for the first time after the game has started</param>
            public static void StartIndexing(Game game, bool isGameStart)
            {
                Helpers.Logging.Log($"Starting indexing process. Is game start: {isGameStart}");

                var orchestrator = Orchestrator;
                try
                {
                    orchestrator?.RebuildIndex(game, isGameStart, Manager, _orchestratorConfig, _managerConfig, _schemaConfig);
                }
                catch (Exception ex)
                {
                    Helpers.Logging.LogError($"An error occurred during the indexing process: {ex}");
                }
            }

            /// <summary>
            /// Configures the snapshot orchestrator using the provided configuration action.
            /// </summary>
            /// <param name="configure">The configuration action to apply to the orchestrator builder.</param>
            public static void Configure(Action<ISnapshotOrchestratorBuilder> configure)
            {
                lock (_lock)
                {
                    _orchestratorConfig = _orchestratorConfig += Helpers.Guard.NotNull(configure, nameof(configure));
                }
            }
            /// <summary>
            /// Configures the snapshot manager using the provided configuration action.
            /// </summary>
            /// <param name="configure">The configuration action to apply to the manager builder.</param>
            public static void ConfigureManager(Action<ISnapshotManagerConfigurator> configure)
            {
                lock (_lock)
                {
                    _managerConfig = _managerConfig += Helpers.Guard.NotNull(configure, nameof(configure));
                }
            }
            /// <summary>
            /// Configures the database schema using the provided configuration action.
            /// </summary>
            /// <param name="configure">The configuration action to apply to the schema builder.</param>
            public static void ConfigureSchema(Action<IDatabaseSchemaBuilder> configure)
            {
                lock (_lock)
                {
                    _schemaConfig = _schemaConfig += Helpers.Guard.NotNull(configure, nameof(configure));
                }
            }
        }

        /// <summary>
        /// Contains non rimworld related helper methods
        /// </summary>
        public static class Helpers
        {
            /// <summary>
            /// Helper class for validating method arguments and throwing appropriate exceptions when validation fails.
            /// </summary>
            public static class Guard
            {
                /// <summary>
                /// Returns the value if the provided condition is true; otherwise, throws an exception. The condition is provided as an expression, allowing for more informative error messages.
                /// </summary>
                /// <typeparam name="T">The type of the value being validated.</typeparam>
                /// <param name="value">The value to validate.</param>
                /// <param name="condition">The condition to validate against.</param>
                /// <param name="exceptionBuilder">A function that returns the exception to throw if the condition is not met.</param>
                /// <param name="parameterName">The name of the parameter being validated.</param>
                /// <returns>The validated value.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static T Is<T>(T value, Expression<Predicate<T>> condition, Func<Exception> exceptionBuilder = null, string parameterName = null)
                {
                    if (condition == null)
                    {
                        throw new ArgumentNullException(nameof(condition));
                    }
                    var compiledCondition = condition.Compile();
                    if (!compiledCondition(value))
                    {
                        throw (exceptionBuilder?.Invoke() ?? throw new ArgumentException($"Condition <{condition}> did not pass for {parameterName ?? value?.ToString() ?? "null"}"));
                    }
                    return value;
                }

                /// <summary>
                /// Returns only non-null values; otherwise, throws an ArgumentNullException.
                /// </summary>
                /// <typeparam name="T">The type of the value being validated.</typeparam>
                /// <param name="value">The value to validate.</param>
                /// <param name="parameterName">The name of the parameter being validated.</param>
                /// <returns>The validated value.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static T NotNull<T>(T value, string parameterName = null)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(parameterName);
                    }

                    return value;
                }

                /// <summary>
                /// Returns only non-null and non-empty string values; otherwise, throws an ArgumentException.
                /// </summary>
                /// <param name="value">The string value to validate.</param>
                /// <param name="parameterName">The name of the parameter being validated.</param>
                /// <returns>The validated string value.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static string NotNullOrEmpty(string value, string parameterName = null)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(parameterName);
                    }

                    if (value.Length == 0)
                    {
                        throw new ArgumentException("Value cannot be empty.", parameterName);
                    }

                    return value;
                }

                /// <summary>
                /// Returns only non-null, non-empty, and non-whitespace string values; otherwise, throws an ArgumentException.
                /// </summary>
                /// <param name="value">The string value to validate.</param>
                /// <param name="parameterName">The name of the parameter being validated.</param>
                /// <returns>The validated string value.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static string NotNullOrWhitespace(string value, string parameterName = null)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(parameterName);
                    }

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
                    }

                    return value;
                }
            }

            /// <summary>
            /// Helper class for working with linq expressions.
            /// </summary>
            public static class Expression
            {
                /// <summary>
                /// Returns the <see cref="MethodInfo"/> of the method called in <paramref name="expression"/>. The expression must be a static method call, otherwise an exception will be thrown.
                /// </summary>
                /// <param name="expression">The expression representing the method call.</param>
                /// <returns>The <see cref="MethodInfo"/> of the called method.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static MethodInfo GetMethod(Expression<Action> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    if (expression.Body is MethodCallExpression methodCall)
                    {
                        return methodCall.Method;
                    }
                    throw new ArgumentException("Expression must be a method call.", nameof(expression));
                }

                /// <summary>
                /// Extracs the called method from <paramref name="expression"/> that doesn't have a return value. The expression must be a method call, otherwise an exception will be thrown.
                /// </summary>
                /// <typeparam name="T">The type of the object on which the method is called.</typeparam>
                /// <param name="expression">The expression representing the method call.</param>
                /// <returns>The <see cref="MethodInfo"/> of the called method.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static MethodInfo GetMethod<T>(Expression<Action<T>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    if (expression.Body is MethodCallExpression methodCall)
                    {
                        return methodCall.Method;
                    }
                    throw new ArgumentException("Expression must be a method call.", nameof(expression));
                }

                /// <summary>
                /// Extracs the called method from <paramref name="expression"/> that has a return value. The expression must be a method call, otherwise an exception will be thrown.
                /// </summary>
                /// <typeparam name="T">The type of the object on which the method is called.</typeparam>
                /// <typeparam name="TResult">The return type of the method.</typeparam>
                /// <param name="expression">The expression representing the method call.</param>
                /// <returns>The <see cref="MethodInfo"/> of the called method.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static MethodInfo GetMethod<T, TResult>(Expression<Func<T, TResult>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    if (expression.Body is MethodCallExpression methodCall)
                    {
                        return methodCall.Method;
                    }
                    throw new ArgumentException("Expression must be a method call.", nameof(expression));
                }
            }

            /// <summary>
            /// Helper class for logging with fallback to console output.
            /// </summary>
            public static class Logging
            {
                /// <summary>
                /// Logs an informational message with fallback to console output.
                /// </summary>
                public static void Log(string message)
                    => Write(() => Verse.Log.Message(message), "INFO", message);

                /// <summary>
                /// Logs a warning message with fallback to console output.
                /// </summary>
                public static void LogWarning(string message)
                    => Write(() => Verse.Log.Warning(message), "WARN", message);

                /// <summary>
                /// Logs an error message with fallback to console output.
                /// </summary>
                public static void LogError(string message)
                    => Write(() => Verse.Log.Error(message), "ERROR", message);

                private static void Write(Action verseLogger, string level, string message)
                {
                    try
                    {
                        verseLogger();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{level}] {message}");
                        Console.WriteLine($"[{level}] Verse logging failed: {ex}");
                    }
                }
            }

            /// <summary>
            /// Helper class for invoking code.
            /// </summary>
            public static class Invoking
            {
                /// <summary>
                /// Fire and forget invocation of the provided action with error handling and logging. Any exceptions thrown by the action will be caught and logged as errors, preventing them from crashing the game or causing unintended side effects.
                /// </summary>
                /// <param name="action">The action to invoke safely.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void Safe(Action action)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError($"An error occurred while invoking method: {ex}");
                    }
                }
            }
        }
    }
    /// <summary>
    /// Contains the settings for <see cref="ToolKit"/>
    /// </summary>
    public class ToolkitSettings : ModSettings
    {
        /// <summary>
        /// Instead of pushing all data in a single tick, pushing is done over a period between each snapshot by calculating all ticking things and pushing a portion of them each tick.
        /// Helps reduce lag spikes when a lot of things are being gathered.
        /// Experimental feature as it can cause data inconsistencies depending on how snapshots are used.
        /// </summary>
        public bool DynamicGatheringEnabled = false;
        /// <summary>
        /// Instead of using TickRare to take snapshots, TickLong will be used.
        /// Reduces the frequency of snapshots and can help with performance.
        /// Can cause issues since snapshots can be really outdated by the time they're used. So can cause users of the snapshot to make decisions based on outdated information which can lead to bad/unintended outcomes.
        /// </summary>
        public bool SlowGatheringEnabled = false;

        /// <inheritdoc cref="ToolkitSettings"/>
        public ToolkitSettings()
        {
            
        }

        /// <summary>
        /// Takes a copy of the settings.
        /// </summary>
        /// <param name="settings">The instance of <see cref="ToolkitSettings"/> to copy.</param>
        internal ToolkitSettings(ToolkitSettings settings)
        {
            DynamicGatheringEnabled = Toolkit.Helpers.Guard.NotNull(settings, nameof(settings)).DynamicGatheringEnabled;
            SlowGatheringEnabled = settings.SlowGatheringEnabled;
        }

        /// <inheritdoc/>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref DynamicGatheringEnabled, nameof(DynamicGatheringEnabled), defaultValue: false);
            Scribe_Values.Look(ref SlowGatheringEnabled, nameof(SlowGatheringEnabled), defaultValue: false);

            Toolkit.Hooks.Manager.Trigger(new Changed(this));
        }

        /// <summary>
        /// Fired when the settings are changed and saved.
        /// </summary>
        public class Changed
        {
            /// <summary>
            /// The updated settings after the change.
            /// </summary>
            public ToolkitSettings Settings { get; }

            internal Changed(ToolkitSettings settings)
            {
                Settings = new ToolkitSettings(Toolkit.Helpers.Guard.NotNull(settings, nameof(settings)));
            }
        }
    }
}
