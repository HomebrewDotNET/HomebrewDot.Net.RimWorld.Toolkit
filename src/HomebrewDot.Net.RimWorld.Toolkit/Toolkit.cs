using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Collecting.Triggers;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Hooks;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.UI.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld
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
        private static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(ModId), true);

        // Fields
        private readonly ToolkitSettingsUi _settingsUi;

        /// <summary>
        /// The unique identifier for this mod.
        /// </summary>
        public static string ModId { get; } = typeof(Toolkit).FullName.ToLower();
        /// <summary>
        /// The Harmony instance used for patching methods.
        /// </summary>
        internal static Harmony Harmony => _harmony.Value;
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
            _settingsUi = new ToolkitSettingsUi();
            ConfigureServices();
        }

        /// <inheritdoc/>
        public override string SettingsCategory()
        {
            return "Homebrewed Toolkit";
        }

        /// <inheritdoc/>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            _settingsUi.Draw(inRect);
        }

        internal static void ConfigureServices()
        {
            // Reference types
            Services.Register<IReferenceType>(IndexedReferenceType.Instance, IndexedReferenceType.DefaultTypeName);
            Services.Register<IReferenceType>(PropertyReferenceType.Instance, PropertyReferenceType.DefaultTypeName);
            Services.Register<IReferenceType>(ValueReferenceType.Instance, ValueReferenceType.DefaultTypeName);

            // Operator types
            foreach (var alias in EqualsOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(EqualsOperatorType.Instance, alias);
            }
            foreach (var alias in NotEqualsOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(NotEqualsOperatorType.Instance, alias);
            }
            foreach (var alias in GreaterOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(GreaterOperatorType.Instance, alias);
            }
            foreach (var alias in GreaterOrEqualOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(GreaterOrEqualOperatorType.Instance, alias);
            }
            foreach (var alias in LesserOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(LesserOperatorType.Instance, alias);
            }
            foreach (var alias in LesserOrEqualOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(LesserOrEqualOperatorType.Instance, alias);
            }
            foreach (var alias in TrueOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(TrueOperatorType.Instance, alias);
            }
            foreach (var alias in FalseOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(FalseOperatorType.Instance, alias);
            }
            foreach (var alias in NullOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(NullOperatorType.Instance, alias);
            }
            foreach (var alias in NotNullOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(NotNullOperatorType.Instance, alias);
            }
            foreach (var alias in MatchOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(MatchOperatorType.Instance, alias);
            }
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
                        if (_manager != null && value is not null)
                        {
                            Invoking.Safe(() => _manager.TransferTo(value));
                        }
                        if (_manager is IDisposable disposable)
                        {
                            Invoking.Safe(() => disposable.Dispose());
                        }
                        _manager = value;
                    }
                }
            }

            /// <summary>
            /// Reloads the hook manager by disposing of the current manager (if it implements IDisposable) and creating a new instance. This can be useful if you have made changes to the hook configuration or want to reset the hooks without restarting the game. It's important to note that calling this method will unregister all existing hooks and start with a fresh manager, so it should be used with caution to avoid potential issues with missing hooks or unintended behavior. If you need to make changes to the hook configuration, consider using the provided hook registration methods and then calling this method to apply those changes without having to manually create a new manager instance.
            /// </summary>
            public static void ReloadManager()
            {
                lock (_lock)
                {
                    if (_manager is HookManager manager)
                    {
                        _manager = null;
                    }
                }
            }
        }

        /// <summary>
        /// Tools for indexing game data using snapshots so it can be accessed in background threads.
        /// </summary>
        public static class Indexing
        {
            // Statics
            static Indexing()
            {
                Invoking.Safe(() =>
                {
                    Hooks.Manager.RegisterHook<OnSaveLoadedTrigger>(Instance, (e) =>
                    {
                        StartIndexing(e.Game, true);
                        // Take snapshot 1 tick after loading to ensure a quick loading of the snapshot.
                        Hooks.Manager.Trigger(new PreparingSnapshotTrigger(Manager));
                        Hooks.Manager.RegisterHook<OnGameTickTrigger>(Instance, (tick) =>
                        {
                            Manager.Snapshot();
                            return true;
                        }, true, priority: 0);
                    }, priority: byte.MaxValue)
                             .RegisterHook<ToolkitSettings.Changed>(Instance, e => StartIndexing(Current.Game));
                });
            }

            // Fields
            private static readonly object _lock = new object();
            private static ISnapshotOrchestrator _orchestrator;
            private static ISnapshotManager _manager;
            private static event Action<ISnapshotOrchestratorBuilder> _orchestratorConfig;
            private static event Action<ISnapshotManagerConfigurator> _managerConfig;
            private static event Action<IDatabaseSchemaBuilder> _schemaConfig;

            // Properties
            /// <summary>
            /// Event that allows for configuring the snapshot orchestrator builder. This event is invoked during the indexing process to allow for dynamic configuration of the orchestrator based on the current game state or other factors. Subscribers to this event can add gatherers, indexers, and other components to the orchestrator builder to customize how the snapshot indexing process works. It's important to note that changes made in this event will only take effect during the next indexing process, so if you need to apply changes immediately, consider using the provided configuration methods and then calling the StartIndexing method to apply those changes without having to wait for the next indexing cycle.
            /// </summary>
            public static event Action<ISnapshotOrchestratorBuilder> ConfigureOrchestrator
            {
                add
                {
                    lock (_lock)
                    {
                        _orchestratorConfig += value;
                    }
                }
                remove
                {
                    lock (_lock)
                    {
                        _orchestratorConfig -= value;
                    }
                }
            }
            /// <summary>
            /// Event that allows for configuring the snapshot manager builder. This event is invoked during the indexing process to allow for dynamic configuration of the manager based on the current game state or other factors. Subscribers to this event can customize how the snapshot manager handles incoming data, manages snapshots, and provides access to indexed data. It's important to note that changes made in this event will only take effect during the next indexing process, so if you need to apply changes immediately, consider using the provided configuration methods and then calling the StartIndexing method to apply those changes without having to wait for the next indexing cycle.
            /// </summary>
            public static event Action<ISnapshotManagerConfigurator> ConfigureManager
            {
                add
                {
                    lock (_lock)
                    {
                        _managerConfig += value;
                    }
                }
                remove
                {
                    lock (_lock)
                    {
                        _managerConfig -= value;
                    }
                }
            }
            /// <summary>
            /// Event that allows for configuring the database schema builder. This event is invoked during the indexing process to allow for dynamic configuration of the database schema based on the current game state or other factors. Subscribers to this event can define tables, columns, indexes, and other aspects of the database schema to customize how data is stored and accessed in the snapshot database. It's important to note that changes made in this event will only take effect during the next indexing process, so if you need to apply changes immediately, consider using the provided configuration methods and then calling the StartIndexing method to apply those changes without having to wait for the next indexing cycle.
            /// </summary>
            public static event Action<IDatabaseSchemaBuilder> ConfigureSchema
            {
                add
                {
                    lock (_lock)
                    {
                        _schemaConfig += value;
                    }
                }
                remove
                {
                    lock (_lock)
                    {
                        _schemaConfig -= value;
                    }
                }
            }

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
                            _orchestrator = new SnapshotOrchestrator(Toolkit.Hooks.Manager, Invoking.Safe(() => Settings.SlowGatheringEnabled, false));
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
            /// Reloads the snapshot orchestration by disposing of the current orchestrator (if it implements IDisposable) and creating a new instance using the current configuration. This can be useful if you have made changes to the orchestrator configuration or want to reset the indexing process without restarting the game. It's important to note that calling this method will interrupt any ongoing indexing process and start a new one, so it should be used with caution to avoid potential issues with incomplete snapshots or data inconsistencies. If you need to make changes to the orchestrator configuration, consider using the provided configuration methods and then calling this method to apply those changes without having to manually create a new orchestrator instance. Additionally, if you want to take a snapshot immediately after reloading the orchestration, you can call the StartIndexing method with the takeSnapshot parameter set to true instead of calling this method directly.
            /// </summary>
            public static void ReloadOrchestration()
            {
                lock (_lock)
                {
                    if (_orchestrator is SnapshotOrchestrator disposable)
                    {
                        Invoking.Safe(() => disposable.Dispose());
                        _orchestrator = null;
                    }
                }
                StartIndexing(Current.Game);
            }
            /// <summary>
            /// Reloads the snapshot manager by disposing of the current manager (if it implements IDisposable) and creating a new instance using the current configuration. This can be useful if you have made changes to the manager configuration or want to reset the snapshot data without restarting the game. It's important to note that calling this method will clear any existing snapshot data and start fresh, so it should be used with caution to avoid potential issues with incomplete snapshots or data inconsistencies. If you need to make changes to the manager configuration, consider using the provided configuration methods and then calling this method to apply those changes without having to manually create a new manager instance. Additionally, if you want to take a snapshot immediately after reloading the manager, you can call the StartIndexing method with the takeSnapshot parameter set to true instead of calling this method directly.
            /// </summary>
            public static void ReloadManager()
            {
                lock (_lock)
                {
                    if (_manager is SnapshotManager manager)
                    {
                        _manager = null;
                    }
                }
                StartIndexing(Current.Game);
            }
            /// <summary>
            /// (Re)starts the snapshot orchestration using the currently configured options.
            /// </summary>
            /// <param name="game">The game instance to start the orchestration for</param>
            /// <param name="takeSnapshot">Whether to take a snapshot immediately after rebuilding the index. If false, the orchestrator will wait for the next scheduled snapshot time to take the first snapshot. This can be useful to avoid taking a snapshot before all data has been gathered and indexed, which can lead to incomplete snapshots and potential issues for users of the snapshot.</param>
            public static void StartIndexing(Game game, bool takeSnapshot = false)
            {
                Helpers.Logging.Log($"Starting indexing process. Is game start: {game == null}");

                var orchestrator = Orchestrator;
                try
                {
                    orchestrator?.RebuildIndex(game, game == null, Manager, _orchestratorConfig ?? (x => { }), _managerConfig ?? (x => { }), _schemaConfig ?? (x => { }));
                    if (takeSnapshot)
                    {
                        Logging.Log("Taking snapshot immediately after indexing");
                        orchestrator?.ForceSnapshot();
                    }
                }
                catch (Exception ex)
                {
                    Helpers.Logging.LogError($"An error occurred during the indexing process: {ex}");
                }
            }

            public static class Indexers
            {
                // Fields
                private readonly static IDictionary<string, (IIndexer Indexer, Action<IDatabaseSchemaBuilder> Configure)> _indexers = new Dictionary<string, (IIndexer Indexer, Action<IDatabaseSchemaBuilder> Configure)>(StringComparer.OrdinalIgnoreCase);

                /// <summary>
                /// Registers an indexer with the given name and configuration. If an indexer with the same name already exists, it will be unregistered and replaced with the new one. The indexer will be initialized and its configuration action will be added to the schema configuration event, allowing it to define its own database schema for indexing data. It's important to note that registering a new indexer with the same name as an existing one will replace the existing indexer and its configuration, so it should be used with caution to avoid potential issues with missing indexes or data inconsistencies. If you need to update an existing indexer, consider unregistering it first using the UnregisterIndexer method and then registering the updated version to ensure a clean replacement without any lingering configuration from the old indexer.
                /// </summary>
                /// <param name="name">The name of the indexer. Mainly just used for deduplication. Name should be the property being indexed so when multiple sources want to index the same property, they can use the same name.</param>
                /// <param name="indexer">The indexer instance to register.</param>
                public static void RegisterIndexer(string name, IIndexer indexer)
                {
                    name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                    indexer = Helpers.Guard.NotNull(indexer, nameof(indexer));
                    lock (_indexers)
                    {
                        if (_indexers.TryGetValue(name, out var existing))
                        {
                            Toolkit.Indexing.ConfigureSchema -= existing.Configure;
                            if (existing.Indexer is IDisposable disposable)
                            {
                                Invoking.Safe(() => disposable.Dispose());
                            }
                        }

                        Invoking.Safe(() =>
                        {
                            indexer.Initialize();
                            var configure = new Action<IDatabaseSchemaBuilder>(x =>
                            {
                                x.OnInserting(indexer.Index);
                            });
                            Toolkit.Indexing.ConfigureSchema += configure;

                            _indexers[name] = (indexer, configure);

                            Indexing.StartIndexing(Current.Game);
                        });
                    }
                }

                /// <summary>
                /// Creates and registers a new indexer using the provided builder action to configure it. The indexer will be initialized and its configuration action will be added to the schema configuration event, allowing it to define its own database schema for indexing data. It's important to note that registering a new indexer with the same name as an existing one will replace the existing indexer and its configuration, so it should be used with caution to avoid potential issues with missing indexes or data inconsistencies. If you need to update an existing indexer, consider unregistering it first using the UnregisterIndexer method and then registering the updated version to ensure a clean replacement without any lingering configuration from the old indexer.
                /// </summary>
                /// <typeparam name="T">The type of the objects being indexed.</typeparam>
                /// <param name="name">The name of the indexer.</param>
                /// <param name="builder">The action to configure the indexer.</param>
                public static void BuildIndexer<T>(string name, Action<IIndexerBuilder<T>> builder) where T : class
                {
                    name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                    builder = Helpers.Guard.NotNull(builder, nameof(builder));
                    var indexer = new TrackedIndexer<T>();
                    builder(indexer);
                    RegisterIndexer(name, indexer);
                }
                /// <summary>
                /// Helper method to create and register a new indexer for a specific property using the provided property expression.
                /// Property will be watched for changes and will store the value of said property in the metadata.
                /// </summary>
                /// <typeparam name="T">The type of the objects being indexed.</typeparam>
                /// <typeparam name="TProperty">The type of the property being indexed.</typeparam>
                /// <param name="propertyExpression">The expression representing the property to index.</param>
                public static void ByProperty<T>(Expression<Func<T, object>> propertyExpression) where T : class
                {
                    propertyExpression = Helpers.Guard.NotNull(propertyExpression, nameof(propertyExpression));
                    var memberInfo = Helpers.Expression.GetMember(propertyExpression);
                    var name = $"{typeof(T).FullName}.{memberInfo.Name}";
                    var lambda = propertyExpression.Compile();
                    BuildIndexer<T>(name, builder => builder.Set(memberInfo.Name, x => lambda(x), true));
                }
                /// <summary>
                /// Helper method to create and register a new indexer for a specific nested property using the provided property expression.
                /// </summary>
                /// <typeparam name="T">The type of the objects being indexed.</typeparam>
                /// <typeparam name="TProperty">The type of the nested property being indexed.</typeparam>
                /// <param name="propertyExpression">The expression representing the nested property to index.</param>
                /// <param name="metadataKey">The key to use for storing the metadata. If null, the name of the last property in the nested path will be used.</param>
                public static void ByNestedProperty<T>(Expression<Func<T, object>> propertyExpression, string metadataKey = null) where T : class
                {
                    propertyExpression = Helpers.Guard.NotNull(propertyExpression, nameof(propertyExpression));
                    var propertyInfos = Helpers.Expression.GetNestedProperties(propertyExpression);
                    var name = $"{typeof(T).FullName}.{string.Join(".", propertyInfos.Select(p => p.Name))}";
                    if (string.IsNullOrEmpty(metadataKey))
                    {
                        metadataKey = propertyInfos.Last().Name;
                    }
                    var lambda = propertyExpression.Compile();
                    BuildIndexer<T>(name, builder => builder.Set(metadataKey, x => lambda(x), true));
                }
            }

            /// <summary>
            /// Helper class for working with the <see cref="Def"/> table in the snapshot database.
            /// </summary>
            public static class Def
            {
                /// <summary>
                /// The name of the root table that contains all defs in the game.
                /// </summary>
                public const string TableName = nameof(Verse.Def);

                /// <summary>
                /// Configures the schema to include the table for defs.
                /// </summary>
                public static void EnsureTable()
                {
                    Indexing.ConfigureSchema += ConfigureSchema;
                }
                /// <summary>
                /// Configures the snapshot orchestrator to include the gatherer for defs, which is responsible for collecting all defs in the game and pushing them to the snapshot manager.
                /// </summary>
                public static void EnsureGatherer()
                {
                    Indexing.ConfigureOrchestrator += ConfigureGathering;
                }
                /// <summary>
                /// Returns the latest snapshot of the table containing all defs in the game.
                /// </summary>
                /// <returns>The latest snapshot of the table containing all defs in the game, or null if the table is not available.</returns>
                public static IReadOnlyTable<Verse.Def> GetTable()
                {
                    return Manager.DatabaseSnapshot?.GetTable<Verse.Def>(TableName);
                }
                /// <summary>
                /// Adds addition configuration for the table.
                /// </summary>
                /// <param name="builder">The table builder to configure.</param>
                public static void ConfigureTable(Action<ITableBuilder<Verse.Def>> builder)
                {
                    builder = Helpers.Guard.NotNull(builder, nameof(builder));
                    EnsureTable();

                    Indexing.ConfigureSchema += b => b.WithTable<Verse.Def>(TableName, builder);
                }
                private static void ConfigureSchema(IDatabaseSchemaBuilder builder)
                {
                    builder.WithTable<Verse.Def>(TableName);
                }
                private static void ConfigureGathering(ISnapshotOrchestratorBuilder builder)
                {
                    builder.With(DefGatherer.Instance);
                }
                /// <summary>
                /// Helper class for working with the <see cref="Verse.ThingDef"/> table in the snapshot database.
                /// </summary>
                public static class Thing
                {
                    /// <summary>
                    /// The name of the root table that contains all thing defs in the game.
                    /// </summary>
                    public const string TableName = nameof(Verse.Thing);
                    /// <summary>
                    /// The fully qualified name of the table
                    /// </summary>
                    public const string FullTableName = $"{Def.TableName}.{TableName}";

                    /// <summary>
                    /// Configures the schema to include the table for thing defs.
                    /// </summary>
                    public static void EnsureTable()
                    {
                        Def.EnsureTable();
                        Def.ConfigureTable(Configure);
                    }
                    /// <summary>
                    /// Adds addition configuration for the table.
                    /// </summary>
                    /// <param name="builder">The table builder to configure.</param>
                    public static void ConfigureTable(Action<ITableBuilder<Verse.ThingDef>> builder)
                    {
                        builder = Helpers.Guard.NotNull(builder, nameof(builder));
                        EnsureTable();

                        Def.ConfigureTable(b => b.WithSubTable(TableName, tableBuilder: builder));
                    }
                    /// <summary>
                    /// Returns the latest snapshot of the table containing all thing defs in the game.
                    /// </summary>
                    /// <returns>The latest snapshot of the table containing all thing defs in the game, or null if the table is not available.</returns>
                    public static IReadOnlyTable<Verse.ThingDef> GetTable()
                    {
                        return Manager.DatabaseSnapshot?.GetTable<Verse.ThingDef>(FullTableName);
                    }
                    private static void Configure(ITableBuilder<Verse.Def> builder)
                    {
                        builder.WithSubTable<Verse.ThingDef>(TableName);
                    }

                    /// <summary>
                    /// Helper class for working with the weapons table.
                    /// </summary>
                    public static class Weapon
                    {
                        /// <summary>
                        /// The name of the root table that contains all weapon defs in the game.
                        /// </summary>
                        public const string TableName = "Weapon";
                        /// <summary>
                        /// The fully qualified name of the table
                        /// </summary>
                        public const string FullTableName = $"{Thing.TableName}.{TableName}";

                        /// <summary>
                        /// Configures the schema to include the table for weapon defs.
                        /// </summary>
                        public static void EnsureTable()
                        {
                            Thing.EnsureTable();
                            Thing.ConfigureTable(Configure);
                        }
                        /// <summary>
                        /// Adds addition configuration for the table.
                        /// </summary>
                        /// <param name="builder">The table builder to configure.</param>
                        public static void ConfigureTable(Action<ITableBuilder<Verse.ThingDef>> builder)
                        {
                            builder = Helpers.Guard.NotNull(builder, nameof(builder));
                            EnsureTable();

                            Thing.ConfigureTable(b => b.WithSubTable(TableName, tableBuilder: builder));
                        }
                        /// <summary>
                        /// Returns the latest snapshot of the table containing all weapon defs in the game.
                        /// </summary>
                        /// <returns>The latest snapshot of the table containing all weapon defs in the game, or null if the table is not available.</returns>
                        public static IReadOnlyTable<Verse.ThingDef> GetTable()
                        {
                            return Manager.DatabaseSnapshot?.GetTable<Verse.ThingDef>(FullTableName);
                        }
                        private static void Configure(ITableBuilder<Verse.ThingDef> builder)
                        {
                            builder.WithSubTable(TableName, x => x.IsWeapon);
                        }

                        /// <summary>
                        /// Helper class for working with the melee weapons table.
                        /// </summary>
                        public static class Melee
                        {
                            /// <summary>
                            /// The name of the root table that contains all melee weapon defs in the game.
                            /// </summary>
                            public const string TableName = "Melee";
                            /// <summary>
                            /// The fully qualified name of the table
                            /// </summary>
                            public const string FullTableName = $"{Weapon.TableName}.{TableName}";

                            /// <summary>
                            /// Configures the schema to include the table for melee weapon defs.
                            /// </summary>
                            public static void EnsureTable()
                            {
                                Weapon.EnsureTable();
                                Weapon.ConfigureTable(Configure);
                            }
                            /// <summary>
                            /// Adds addition configuration for the table.
                            /// </summary>
                            /// <param name="builder">The table builder to configure.</param>
                            public static void ConfigureTable(Action<ITableBuilder<Verse.ThingDef>> builder)
                            {
                                builder = Helpers.Guard.NotNull(builder, nameof(builder));
                                EnsureTable();

                                Weapon.ConfigureTable(b => b.WithSubTable(TableName, tableBuilder: builder));
                            }
                            /// <summary>
                            /// Returns the latest snapshot of the table containing all melee weapon defs in the game.
                            /// </summary>
                            /// <returns>The latest snapshot of the table containing all melee weapon defs in the game, or null if the table is not available.</returns>
                            public static IReadOnlyTable<Verse.ThingDef> GetTable()
                            {
                                return Manager.DatabaseSnapshot?.GetTable<Verse.ThingDef>(FullTableName);
                            }
                            private static void Configure(ITableBuilder<Verse.ThingDef> builder)
                            {
                                builder.WithSubTable(TableName, x => x.IsMeleeWeapon);
                            }
                        }

                        /// <summary>
                        /// Helper class for working with the ranged weapons table.
                        /// </summary>
                        public static class Ranged
                        {
                            /// <summary>
                            /// The name of the root table that contains all ranged weapon defs in the game.
                            /// </summary>
                            public const string TableName = "Ranged";
                            /// <summary>
                            /// The fully qualified name of the table
                            /// </summary>
                            public const string FullTableName = $"{Weapon.TableName}.{TableName}";

                            /// <summary>
                            /// Configures the schema to include the table for ranged weapon defs.
                            /// </summary>
                            public static void EnsureTable()
                            {
                                Weapon.EnsureTable();
                                Weapon.ConfigureTable(Configure);
                            }
                            /// <summary>
                            /// Adds addition configuration for the table.
                            /// </summary>
                            /// <param name="builder">The table builder to configure.</param>
                            public static void ConfigureTable(Action<ITableBuilder<Verse.ThingDef>> builder)
                            {
                                builder = Helpers.Guard.NotNull(builder, nameof(builder));
                                EnsureTable();

                                Weapon.ConfigureTable(b => b.WithSubTable(TableName, tableBuilder: builder));
                            }
                            /// <summary>
                            /// Returns the latest snapshot of the table containing all ranged weapon defs in the game.
                            /// </summary>
                            /// <returns>The latest snapshot of the table containing all reanged weapon defs in the game, or null if the table is not available.</returns>
                            public static IReadOnlyTable<Verse.ThingDef> GetTable()
                            {
                                return Manager.DatabaseSnapshot?.GetTable<Verse.ThingDef>(FullTableName);
                            }
                            private static void Configure(ITableBuilder<Verse.ThingDef> builder)
                            {
                                builder.WithSubTable(TableName, x => x.IsRangedWeapon);
                            }
                        }
                    }

                    /// <summary>
                    /// Helper class for working with the apparel table.
                    /// </summary>
                    public static class Apparel
                    {
                        /// <summary>
                        /// The name of the root table that contains all apparel defs in the game.
                        /// </summary>
                        public const string TableName = "Apparel";
                        /// <summary>
                        /// The fully qualified name of the table
                        /// </summary>
                        public const string FullTableName = $"{Thing.TableName}.{TableName}";

                        /// <summary>
                        /// Configures the schema to include the table for apparel defs.
                        /// </summary>
                        public static void EnsureTable()
                        {
                            Thing.EnsureTable();
                            Thing.ConfigureTable(Configure);
                        }
                        /// <summary>
                        /// Adds addition configuration for the table.
                        /// </summary>
                        /// <param name="builder">The table builder to configure.</param>
                        public static void ConfigureTable(Action<ITableBuilder<Verse.ThingDef>> builder)
                        {
                            builder = Helpers.Guard.NotNull(builder, nameof(builder));
                            EnsureTable();

                            Thing.ConfigureTable(b => b.WithSubTable(TableName, tableBuilder: builder));
                        }
                        /// <summary>
                        /// Returns the latest snapshot of the table containing all apparel defs in the game.
                        /// </summary>
                        /// <returns>The latest snapshot of the table containing all apparel defs in the game, or null if the table is not available.</returns>
                        public static IReadOnlyTable<Verse.ThingDef> GetTable()
                        {
                            return Manager.DatabaseSnapshot?.GetTable<Verse.ThingDef>(FullTableName);
                        }
                        private static void Configure(ITableBuilder<Verse.ThingDef> builder)
                        {
                            builder.WithSubTable(TableName, x => x.IsApparel);
                        }
                    }
                }
            }
            /// <summary>
            /// Helper class for working with the <see cref="Thing"/> table in the snapshot database.
            /// </summary>
            public static class Thing
            {
                /// <summary>
                /// The name of the root table that contains all things on all active maps.
                /// </summary>
                public const string TableName = nameof(Verse.Thing);

                /// <summary>
                /// Configures the schema to include the table for things.
                /// </summary>
                public static void EnsureTable()
                {
                    ConfigureSchema += Configure;
                }
                /// <summary>
                /// Configures the snapshot orchestrator to include the gatherer for things, which is responsible for collecting all things on all active maps and pushing them to the snapshot manager.
                /// </summary>
                public static void EnsureGatherer()
                {
                    ConfigureOrchestrator += ConfigureGathering;
                }
                /// <summary>
                /// Adds addition configuration for the table.
                /// </summary>
                /// <param name="builder">The table builder to configure.</param>
                public static void ConfigureTable(Action<ITableBuilder<Verse.Thing>> builder)
                {
                    builder = Helpers.Guard.NotNull(builder, nameof(builder));
                    EnsureTable();

                    ConfigureSchema += b => b.WithTable(TableName, builder);
                }
                /// <summary>
                /// Returns the latest snapshot of the table containing all things on all active maps.
                /// </summary>
                /// <returns>The latest snapshot of the table containing all things on all active maps, or null if the table is not available.</returns>
                public static IReadOnlyTable<Verse.Thing> GetTable()
                {
                    return Manager.DatabaseSnapshot?.GetTable<Verse.Thing>(TableName);
                }
                private static void Configure(IDatabaseSchemaBuilder builder)
                {
                    builder.WithTable<Verse.Thing>(TableName);
                }
                private static void ConfigureGathering(ISnapshotOrchestratorBuilder builder)
                {
                    builder.With(HarmonyThingGatherer.Instance)
                           .With(MapThingGatherer.Instance);
                }

                /// <summary>
                /// Helper class for working with the filtered <see cref="Thing"/> table for resources in the snapshot database.
                /// </summary>
                public static class Resources
                {
                    /// <summary>
                    /// The name of the root table that contains all resources on all active maps.
                    /// </summary>
                    public const string TableName = "Resources";
                    /// <summary>
                    /// The fully qualified name of the table.
                    /// </summary>
                    public const string FullTableName = $"{Thing.TableName}.{TableName}";

                    /// <summary>
                    /// Configures the schema to include the table for resources.
                    /// </summary>
                    public static void EnsureTable()
                    {
                        Thing.EnsureTable();
                        Thing.ConfigureTable(Configure);
                    }
                    /// <summary>
                    /// Adds addition configuration for the table.
                    /// </summary>
                    /// <param name="builder">The table builder to configure.</param>
                    public static void ConfigureTable(Action<ITableBuilder<Verse.Thing>> builder)
                    {
                        builder = Helpers.Guard.NotNull(builder, nameof(builder));
                        EnsureTable();

                        Thing.ConfigureTable(b => b.WithSubTable(TableName, tableBuilder: builder));
                    }
                    /// <summary>
                    /// Returns the latest snapshot of the table containing all resources on all active maps.
                    /// </summary>
                    /// <returns>The latest snapshot of the table containing all resources on all active maps, or null if the table is not available.</returns>
                    public static IReadOnlyTable<Verse.Thing> GetTable()
                    {
                        return Manager.DatabaseSnapshot?.GetTable<Verse.Thing>(FullTableName);
                    }
                    private static void Configure(ITableBuilder<Verse.Thing> builder)
                    {
                        builder.WithSubTable(TableName, x => x.def.CountAsResource);
                    }
                }
            }
        }

        /// <summary>
        /// Tools for collecting game data into collections based on defined conditions and criteria, allowing for efficient organization and retrieval of related data.
        /// </summary>
        public static class Collecting
        {

            static Collecting()
            {
                Helpers.Invoking.Safe(() =>
                {
                    Toolkit.Hooks.Manager.RegisterHook<OnCollectionsChanged>(Toolkit.Instance, errorHandler =>
                    {
                        ReloadDefaultComparator();
                    }, false);
                });
            }

            // Fields
            private static readonly object _lock = new object();
            private static readonly Dictionary<string, ICollectionDef> _collectionDefinitions = new Dictionary<string, ICollectionDef>(StringComparer.OrdinalIgnoreCase);
            private static readonly Dictionary<string, ICollector> _collectors = new Dictionary<string, ICollector>(StringComparer.OrdinalIgnoreCase);
            private static ICollectionComparator _comparator;

            // Properties
            /// <summary>
            /// The comparator used to evaluate collection conditions and determine whether objects match the criteria for being included in a collection. Accessing this property will initialize the comparator if it hasn't been already, using any registered reference types, operator types, and a reference resolver. The comparator can also be set to a custom implementation if needed. It's important to note that changing the comparator will affect how all collections are evaluated, so it should be done with caution. If you need to use a different comparator for specific collections, consider implementing that logic within the conditions of those collections or by using context values to modify the behavior of the global comparator.
            /// </summary>
            public static ICollectionComparator Comparator
            {
                get
                {
                    if (_comparator != null) return _comparator;
                    lock (_lock)
                    {
                        if (_comparator == null)
                        {
                            var referenceTypes = Services.GetAllNamed<IReferenceType>();
                            var referenceResolver = Services.Get<IReferenceResolver>() ?? new ReferenceResolver(referenceTypes);
                            var operatorTypes = Services.GetAllNamed<IOperatorType>();
                            _comparator = new CollectionComparator(new Comparator(referenceResolver, operatorTypes));
                        }
                    }
                    return _comparator;
                }
                set
                {
                    value = Helpers.Guard.NotNull(value, nameof(value));

                    lock (_lock)
                    {
                        if (_comparator is IDisposable disposable)
                        {
                            Invoking.Safe(() => disposable.Dispose());
                        }
                        _comparator = value;
                    }
                }
            }

            /// <summary>
            /// Reloads the default comparator by clearing the current comparator instance. This will cause the next access to the <see cref="Comparator"/> property to create a new instance of the default comparator using any currently registered reference types, operator types, and reference resolver. This can be useful if you have made changes to the registered services that the default comparator relies on and want to ensure those changes are reflected in the comparator's behavior without having to restart the game or manually create a new comparator instance. It's important to note that calling this method will affect all collections that use the default comparator, so it should be done with caution. If you need to use a different comparator for specific collections, consider implementing that logic within the conditions of those collections or by using context values to modify the behavior of the global comparator instead of relying on this method to change the default comparator's behavior.
            /// </summary>
            public static void ReloadDefaultComparator()
            {
                lock (_lock)
                {
                    if (_comparator is Comparator collectionComparator)
                    {
                        _comparator = null;
                    }
                }
            }

            /// <summary>
            /// (Re)starts the collection process for all registered collectors using the current collection definitions and comparator. This method will stop all collectors, update their collection definitions and comparator, and then start them again to ensure they are using the latest configuration. It's important to note that any changes to collection definitions or the comparator will not take effect until this method is called, so it should be called after making any updates to ensure collectors are using the most up-to-date configuration.
            /// </summary>
            public static void StartCollection()
            {
                lock (_lock)
                {
                    foreach (var collector in _collectors.Values)
                    {
                        Invoking.Safe(() =>
                        {
                            collector.StopCollecting();
                            collector.StartCollecting(Comparator, _collectionDefinitions);
                        });
                    }
                }
            }

            /// <summary>
            /// Adds a new collection definition with the specified name and definition. If a collection with the same name already exists, it will be overwritten with the new definition.
            /// Added without collector so collection can be referenced by other collections.
            /// </summary>
            /// <param name="name">The name of the collection.</param>
            /// <param name="definition">The definition of the collection.</param>
            public static void Set(string name, ICollectionDef definition)
            {
                name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                definition = Helpers.Guard.NotNull(definition, nameof(definition));
                lock (_lock)
                {
                    _collectionDefinitions[name] = definition;
                }
                Toolkit.Hooks.Manager?.LazyTrigger(() => new OnCollectionsChanged(name, definition, null, true));
            }
            /// <summary>
            /// Adds a new collector with the specified name and collector instance. The collection definition associated with the collector will also be added using the same name. If a collector with the same name already exists, it will be overwritten with the new collector and definition.
            /// </summary>
            /// <typeparam name="T">The type of items collected by the collector.</typeparam>
            /// <param name="name">The name of the collector.</param>
            /// <param name="collector">The collector instance.</param>
            /// <param name="startCollecting">Indicates whether the collector should start collecting immediately.</param>
            public static void Set(string name, ICollector collector, bool startCollecting = true)
            {
                name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                collector = Helpers.Guard.NotNull(collector, nameof(collector));
                var collection = Helpers.Guard.NotNull(collector.Definition, nameof(collector.Definition));

                lock (_lock)
                {
                    _collectors[name] = collector;
                    Set(name, collection);
                    if (startCollecting)
                    {
                        Invoking.Safe(() =>
                        {
                            collector.StopCollecting();
                            collector.StartCollecting(Comparator, _collectionDefinitions);
                        });
                    }
                }

                Toolkit.Hooks.Manager?.LazyTrigger(() => new OnCollectionsChanged(name, collection, collector, true));
            }
            /// <summary>
            /// Adds a new collector with the specified name and collector instance. The collection definition associated with the collector will also be added using the same name. If a collector with the same name already exists, it will be overwritten with the new collector and definition.
            /// </summary>
            /// <typeparam name="T">The type of items collected by the collector.</typeparam>
            /// <param name="name">The name of the collector.</param>
            /// <param name="collector">The collector instance.</param>
            /// <param name="startCollecting">Indicates whether the collector should start collecting immediately.</param>
            public static void Set<T>(string name, ICollector<T> collector, bool startCollecting = true) where T : class
            {
                name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                collector = Helpers.Guard.NotNull(collector, nameof(collector));

                Set(name, (ICollector)collector, startCollecting);
            }
            /// <summary>
            /// Removes the collector and collection definition associated with the specified name. If no collector or definition exists with the given name, this method does nothing.
            /// </summary>
            /// <param name="name">The name of the collector and collection definition to remove.</param>
            public static void Remove(string name)
            {
                ICollector collector;
                ICollectionDef collection;
                lock (_lock)
                {
                    if (_collectors.TryGetValue(name, out collector))
                    {
                        Invoking.Safe(() => collector.StopCollecting());
                        if (collector is IDisposable disposable)
                        {
                            Invoking.Safe(() => disposable.Dispose());
                        }
                        _collectors.Remove(name);
                    }
                    if (_collectionDefinitions.TryGetValue(name, out collection))
                    {
                        _collectionDefinitions.Remove(name);
                    }
                }

                Toolkit.Hooks.Manager?.LazyTrigger(() => new OnCollectionsChanged(name, collection, collector, false));
            }

            /// <summary>
            /// Adds a new collection (and optionally a collector) using the specified build action.
            /// </summary>
            /// <param name="name">The name of the collection.</param>
            /// <param name="buildAction">A function that takes a collection builder and returns the built collection.</param>
            /// <param name="startCollecting"></param>
            public static void Build(string name, Func<ICollectionBuilder, ICollectionBuilder> buildAction, bool startCollecting = true)
            {
                name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                buildAction = Helpers.Guard.NotNull(buildAction, nameof(buildAction));

                var builder = new CollectionBuilder();
                _ = buildAction(builder);
                var collection = Guard.NotNull(builder.Collection, nameof(builder.Collection));
                if (builder.TryBuildCollector(collection, out var collector))
                {
                    Set(name, collector, startCollecting);
                    return;
                }
                Set(name, collection);
            }
            /// <summary>
            /// Creates (but doesn't add) a new collection (and optionally a collector) using the specified build action. This can be useful if you want to build a collection and collector but need to perform additional configuration or setup before adding it to the toolkit. The returned collection and collector will not be registered in the toolkit, so you will need to call the appropriate methods to add them if you want to use them within the toolkit's collection management system.
            /// </summary>
            /// <param name="buildAction">A function that takes a collection builder and returns the built collection.</param>
            /// <returns>A tuple containing the collection definition and the collector (if any).</returns>
            public static (ICollectionDef Collection, ICollector Collector) BuildOnly(Func<ICollectionBuilder, ICollectionBuilder> buildAction)
            {
                buildAction = Helpers.Guard.NotNull(buildAction, nameof(buildAction));

                var builder = new CollectionBuilder();
                _ = buildAction(builder);
                var collection = Guard.NotNull(builder.Collection, nameof(builder.Collection));
                if (builder.TryBuildCollector(collection, out var collector))
                {
                    return (collection, collector);
                }
                return (collection, null);
            }
            /// <summary>
            /// Retrieves the collection definition associated with the specified name. If no collection definition exists with the given name, this method returns null.
            /// </summary>
            /// <returns>A read-only dictionary containing all collection definitions.</returns>
            public static IReadOnlyDictionary<string, ICollectionDef> GetAllDefinitions()
            {
                lock (_lock)
                {
                    return new Dictionary<string, ICollectionDef>(_collectionDefinitions, StringComparer.OrdinalIgnoreCase);
                }
            }

            /// <summary>
            /// Retrieves the collector associated with the specified name. If no collector exists with the given name, this method returns null.
            /// </summary>
            /// <returns>A read-only dictionary containing all collectors.</returns>
            public static IReadOnlyDictionary<string, ICollector> GetAllCollectors()
            {
                lock (_lock)
                {
                    return new Dictionary<string, ICollector>(_collectors, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// Used for registering generic services that can be used by other tools.
        /// </summary>
        public static class Services
        {
            // Fields
            private static readonly object _lock = new object();
            private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
            private static readonly Dictionary<Type, object> _serviceCache = new Dictionary<Type, object>();
            private static readonly Dictionary<Type, object> _namedServices = new Dictionary<Type, object>();

            /// <summary>
            /// Registers a service instance that can be used by other tools.
            /// </summary>
            /// <typeparam name="T">The type of the service.</typeparam>
            /// <param name="service">The service instance to register.</param>
            /// <param name="name">The optional name of the service.</param>
            public static void Register<T>(T service, string name = null)
            {
                service = Helpers.Guard.NotNull(service, nameof(service));
                lock (_lock)
                {
                    if (!_services.TryGetValue(typeof(T), out var serviceList))
                    {
                        serviceList = new List<T>();
                        _services[typeof(T)] = serviceList;
                    }
                    var typedServiceList = (List<T>)serviceList;
                    typedServiceList.Add(service);
                    if (_serviceCache.ContainsKey(typeof(T)))
                    {
                        _serviceCache.Remove(typeof(T));
                    }
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        Dictionary<string, T> typedServiceDict;
                        if (!_namedServices.TryGetValue(typeof(T), out var namedServiceDict))
                        {
                            namedServiceDict = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
                            _namedServices[typeof(T)] = namedServiceDict;
                        }
                        typedServiceDict = (Dictionary<string, T>)namedServiceDict;
                        typedServiceDict[name] = service;
                    }
                }
            }
            /// <summary>
            /// Unregisters a previously registered service instance. If the service was not registered, this method does nothing.
            /// </summary>
            /// <typeparam name="T">The type of the service.</typeparam>
            /// <param name="service">The service instance to unregister.</param>
            /// <returns>True if the service was successfully unregistered; otherwise, false.</returns>
            public static bool Unregister<T>(T service)
            {
                lock (_lock)
                {
                    bool wasRemoved = _services.TryGetValue(typeof(T), out var serviceSet) && RemoveService((List<T>)serviceSet, service);
                    if (wasRemoved)
                    {
                        if (_namedServices.TryGetValue(typeof(T), out var namedServiceDict))
                        {
                            var typedServiceDict = (Dictionary<string, T>)namedServiceDict;
                            var namesToRemove = typedServiceDict.Where(kvp => kvp.Value.Equals(service)).Select(kvp => kvp.Key).ToList();
                            foreach (var name in namesToRemove)
                            {
                                typedServiceDict.Remove(name);
                            }
                        }
                    }
                    return wasRemoved;
                }
            }
            /// <summary>
            /// Unregisters a previously registered service instance by its name. If no service is registered with the given name, this method does nothing.
            /// </summary>
            /// <typeparam name="T">The type of the service.</typeparam>
            /// <param name="name">The name of the service to unregister.</param>
            /// <returns>True if the service was successfully unregistered; otherwise, false.</returns>
            public static bool UnregisterByName<T>(string name)
            {
                lock (_lock)
                {
                    if (_namedServices.TryGetValue(typeof(T), out var namedServiceDict) && ((Dictionary<string, T>)namedServiceDict).TryGetValue(name, out var service))
                    {
                        ((Dictionary<string, T>)namedServiceDict).Remove(name);
                        return _services.TryGetValue(typeof(T), out var serviceSet) && RemoveService((List<T>)serviceSet, service);
                    }
                    return false;
                }
            }

            /// <summary>
            /// Retrieves all registered services of the specified type. If no services of the given type are registered, this method returns an empty collection.
            /// </summary>
            /// <typeparam name="T">The type of the services to retrieve.</typeparam>
            /// <returns>An enumerable collection of services of the specified type.</returns>
            public static IEnumerable<T> GetAll<T>(bool includeNamed = false)
            {
                if (!includeNamed)
                {
                    if (_serviceCache.TryGetValue(typeof(T), out var cachedServices))
                    {
                        return (IEnumerable<T>)cachedServices;
                    }
                    else
                    {
                        lock (_lock)
                        {
                            if (!_serviceCache.TryGetValue(typeof(T), out cachedServices))
                            {
                                var services = _services.TryGetValue(typeof(T), out var serviceList)
                                    ? ((IEnumerable<T>)serviceList).ToArray()
                                    : Array.Empty<T>();
                                _serviceCache[typeof(T)] = services;
                                return services;
                            }
                            else
                            {
                                return (IEnumerable<T>)cachedServices;
                            }
                        }
                    }
                }
                lock (_lock)
                {
                    var services = _services.TryGetValue(typeof(T), out var serviceList)
                        ? ((IEnumerable<T>)serviceList).ToList()
                        : new List<T>();
                    if (includeNamed)
                    {
                        if (_namedServices.TryGetValue(typeof(T), out var namedServiceDict))
                        {
                            services.AddRange(((Dictionary<string, T>)namedServiceDict).Values);
                        }
                    }
                    return services;
                }
            }

            /// <summary>
            /// Retrieves all registered named services of the specified type as a dictionary mapping service names to service instances. If no named services of the given type are registered, this method returns an empty dictionary.
            /// </summary>
            /// <typeparam name="T">The type of the services to retrieve.</typeparam>
            /// <returns>A read-only dictionary mapping service names to service instances of the specified type.</returns>
            public static IReadOnlyDictionary<string, T> GetAllNamed<T>()
            {
                lock (_lock)
                {
                    if (_namedServices.TryGetValue(typeof(T), out var namedServiceDict))
                    {
                        return (Dictionary<string, T>)namedServiceDict;
                    }
                }
                return NullDictionary<string, T>.Instance;
            }

            /// <summary>
            /// Retrieves the last registered service of the specified type. If no service of the given type is registered, this method returns null.
            /// </summary>
            /// <typeparam name="T">The type of the service to retrieve.</typeparam>
            /// <param name="name">The optional name of the service to retrieve. If provided, the method will first attempt to find a service with the specified name and type before falling back to searching for any service of the given type.</param>
            /// <returns>The last registered service of the specified type, or null if none is found.</returns>
            public static T Get<T>(string name = null)
            {
                if (name != null)
                {
                    var allNamed = GetAllNamed<T>();
                    if (allNamed.TryGetValue(name, out var namedService))
                    {
                        return namedService;
                    }
                }

                var all = GetAll<T>(false);
                if (all != null)
                {
                    return all.LastOrDefault();
                }

                return default;
            }
            /// <summary>
            /// Retrieves the first registered service of the specified type. If no service of the given type is registered, this method throws an InvalidOperationException.
            /// </summary>
            /// <typeparam name="T">The type of the service to retrieve.</typeparam>
            /// <returns>The first registered service of the specified type.</returns>
            /// <exception cref="InvalidOperationException">Thrown if no service of the specified type is registered.</exception>
            public static T GetRequired<T>(string name = null)
            {
                var service = Get<T>(name);
                if (service == null)
                {
                    throw new InvalidOperationException($"No service of type {typeof(T)}{(name != null ? $" with name '{name}'" : "")} is registered.");
                }
                return service;
            }

            private static bool RemoveService<T>(List<T> serviceSet, T service)
            {
                lock (_lock)
                {
                    if (serviceSet.Remove(service))
                    {
                        _serviceCache.Remove(typeof(T));
                        if (service is IDisposable disposable)
                        {
                            Helpers.Invoking.Safe(() =>
                            {
                                disposable.Dispose();
                            });
                        }
                        return true;
                    }
                    return false;
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

                /// <summary>
                /// Extracts the called constructor from <paramref name="expression"/>. The expression must be a constructor call, otherwise an exception will be thrown.
                /// </summary>
                /// <typeparam name="T">The type of the object being constructed.</typeparam>
                /// <param name="expression">The expression representing the constructor call.</param>
                /// <returns>The <see cref="ConstructorInfo"/> of the called constructor.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static ConstructorInfo GetConstructor<T>(Expression<Func<T>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    if (expression.Body is NewExpression newExpression)
                    {
                        return newExpression.Constructor;
                    }
                    throw new ArgumentException("Expression must be a constructor call.", nameof(expression));
                }

                /// <summary>
                /// Extracts the called constructor from <paramref name="expression"/>. The expression must be a constructor call, otherwise an exception will be thrown.
                /// </summary>
                /// <param name="expression">The expression representing the constructor call.</param>
                /// <returns>The <see cref="ConstructorInfo"/> of the called constructor.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static ConstructorInfo GetConstructor(Expression<Func<object>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    if (expression.Body is NewExpression newExpression)
                    {
                        return newExpression.Constructor;
                    }
                    throw new ArgumentException("Expression must be a constructor call.", nameof(expression));
                }

                /// <summary>
                /// Extracts the constructor from <paramref name="expression"/> and returns the corresponding constructor for the specified generic type. The expression must be a constructor call for a generic type, otherwise an exception will be thrown. The generic type in the constructor parameters will be replaced with the provided target generic type when searching for the corresponding constructor.
                /// </summary>
                /// <param name="targetGenericTypeArgument">The target generic type to replace in the declaring type. For example, if the constructor is for List&lt;T&gt; and the target generic type is int, the resulting constructor will be for List&lt;int&gt;.</param>
                /// <param name="expression">The expression representing the constructor call.</param>
                /// <returns>The <see cref="ConstructorInfo"/> of the corresponding constructor for the specified generic type.</returns>
                /// <exception cref="ArgumentException"></exception>
                public static ConstructorInfo GetConstructorForGeneric(Type targetGenericTypeArgument, Expression<Func<object>> expression)
                {
                    targetGenericTypeArgument = Guard.NotNull(targetGenericTypeArgument, nameof(targetGenericTypeArgument));
                    expression = Guard.NotNull(expression, nameof(expression));

                    var constructor = GetConstructor(expression);
                    if (!constructor.DeclaringType.IsGenericType)
                    {
                        throw new ArgumentException($"Constructor must be for generic type", nameof(expression));
                    }
                    var allContructors = constructor.DeclaringType.GetConstructors();
                    var constructorIndex = Array.IndexOf(allContructors, constructor);
                    var targetGenericType = constructor.DeclaringType.GetGenericTypeDefinition().MakeGenericType(targetGenericTypeArgument);
                    var targetConstructors = targetGenericType.GetConstructors();
                    return targetConstructors[constructorIndex];
                }
                /// <summary>
                /// Extracts the property from <paramref name="expression"/> and returns the corresponding property for the specified generic type. The expression must be a property access for a generic type, otherwise an exception will be thrown. The generic type in the declaring type will be replaced with the provided target generic type when searching for the corresponding property.
                /// </summary>
                /// <typeparam name="T">The type containing the property.</typeparam>
                /// <typeparam name="TProperty">The type of the property.</typeparam>
                /// <param name="expression">The expression representing the property access.</param>
                /// <returns>The <see cref="PropertyInfo"/> of the corresponding property for the specified generic type.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static PropertyInfo GetProperty<T, TProperty>(Expression<Func<T, TProperty>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    if (expression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        return propertyInfo;
                    }
                    throw new ArgumentException("Expression must be a property access.", nameof(expression));
                }
                /// <summary>
                /// Extracts the property from <paramref name="expression"/>. The expression must be a property access, otherwise an exception will be thrown.
                /// </summary>
                /// <typeparam name="TProperty">The type of the property.</typeparam>
                /// <param name="expression">The expression representing the property access.</param>
                /// <returns>The <see cref="PropertyInfo"/> of the corresponding property.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static PropertyInfo GetProperty<TProperty>(Expression<Func<TProperty>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    if (expression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        return propertyInfo;
                    }
                    throw new ArgumentException("Expression must be a property access.", nameof(expression));
                }
                /// <summary>
                /// Extracts the nested properties from <paramref name="expression"/>. The expression must be a nested property access (e.g. x => x.Property1.Property2), otherwise an exception will be thrown. The returned array will contain the properties in the order they are accessed (e.g. [Property1, Property2]). This can be useful for scenarios where you need to access or manipulate nested properties dynamically, such as in data binding or serialization scenarios.
                /// </summary>
                /// <typeparam name="TProperty">The type of the final property being accessed.</typeparam>
                /// <param name="expression">The expression representing the nested property access.</param>
                /// <returns>An array of <see cref="PropertyInfo"/> objects representing the nested properties.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static PropertyInfo[] GetNestedProperties<TProperty>(Expression<Func<TProperty>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    var properties = new List<PropertyInfo>();
                    System.Linq.Expressions.Expression currentExpression = expression.Body;
                    while (currentExpression is MemberExpression memberExpression)
                    {
                        if (memberExpression.Member is PropertyInfo propertyInfo)
                        {
                            properties.Add(propertyInfo);
                            currentExpression = memberExpression.Expression;
                        }
                        else
                        {
                            throw new ArgumentException("Expression must be a nested property access.", nameof(expression));
                        }
                    }
                    properties.Reverse();
                    return properties.ToArray();
                }
                /// <summary>
                /// Extracts the nested properties from <paramref name="expression"/>. The expression must be a nested property access (e.g. x => x.Property1.Property2), otherwise an exception will be thrown. The returned array will contain the properties in the order they are accessed (e.g. [Property1, Property2]). This can be useful for scenarios where you need to access or manipulate nested properties dynamically, such as in data binding or serialization scenarios.
                /// </summary>
                /// <typeparam name="T">The type of the object containing the nested properties.</typeparam>
                /// <typeparam name="TProperty">The type of the final property being accessed.</typeparam>
                /// <param name="expression">The expression representing the nested property access.</param>
                /// <returns>An array of <see cref="PropertyInfo"/> objects representing the nested properties.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static PropertyInfo[] GetNestedProperties<T, TProperty>(Expression<Func<T, TProperty>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    var properties = new List<PropertyInfo>();
                    System.Linq.Expressions.Expression currentExpression = expression.Body;
                    while (currentExpression is MemberExpression memberExpression)
                    {
                        if (memberExpression.Member is PropertyInfo propertyInfo)
                        {
                            properties.Add(propertyInfo);
                            currentExpression = memberExpression.Expression;
                        }
                        else
                        {
                            throw new ArgumentException("Expression must be a nested property access.", nameof(expression));
                        }
                    }
                    properties.Reverse();
                    return properties.ToArray();
                }
                /// <summary>
                /// Extracts the member (property, field, or method) from <paramref name="expression"/>. The expression must be a member access or method call, otherwise an exception will be thrown. This method can be used to retrieve the <see cref="MemberInfo"/> of a member accessed in a lambda expression, which can be useful for scenarios such as data binding, serialization, or dynamic code generation where you need to work with members of a type in a more dynamic way.
                /// </summary>
                /// <typeparam name="T">The type containing the member.</typeparam>
                /// <typeparam name="TMember">The type of the member.</typeparam>
                /// <param name="expression">The expression representing the member access or method call.</param>
                /// <returns>The <see cref="MemberInfo"/> of the accessed member.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static MemberInfo GetMember<T, TMember>(Expression<Func<T, TMember>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));

                    if (expression.Body is MemberExpression memberExpression)
                    {
                        return memberExpression.Member;
                    }
                    else if (expression.Body is MethodCallExpression methodCallExpression)
                    {
                        return methodCallExpression.Method;
                    }
                    else if (expression.Body is UnaryExpression unaryExpression)
                    {
                        if (unaryExpression.Operand is MemberExpression innerMemberExpression)
                        {
                            return innerMemberExpression.Member;
                        }
                        else if (unaryExpression.Operand is MethodCallExpression innerMethodCallExpression)
                        {
                            return innerMethodCallExpression.Method;
                        }
                    }
                    throw new ArgumentException("Expression must be a member call.", nameof(expression));
                }
                /// <summary>
                /// Extracts the member (property, field, or method) from <paramref name="expression"/>. The expression must be a member access or method call, otherwise an exception will be thrown. This method can be used to retrieve the <see cref="MemberInfo"/> of a member accessed in a lambda expression, which can be useful for scenarios such as data binding, serialization, or dynamic code generation where you need to work with members of a type in a more dynamic way.
                /// </summary>
                /// <typeparam name="TMember">The type of the member.</typeparam>
                /// <param name="expression">The expression representing the member access or method call.</param>
                /// <returns>The <see cref="MemberInfo"/> of the accessed member.</returns>
                /// <exception cref="ArgumentNullException"></exception>
                /// <exception cref="ArgumentException"></exception>
                public static MemberInfo GetMember<TMember>(Expression<Func<TMember>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));

                    if (expression.Body is MemberExpression memberExpression)
                    {
                        return memberExpression.Member;
                    }
                    else if (expression.Body is MethodCallExpression methodCallExpression)
                    {
                        return methodCallExpression.Method;
                    }
                    else if (expression.Body is UnaryExpression unaryExpression)
                    {
                        if (unaryExpression.Operand is MemberExpression innerMemberExpression)
                        {
                            return innerMemberExpression.Member;
                        }
                        else if (unaryExpression.Operand is MethodCallExpression innerMethodCallExpression)
                        {
                            return innerMethodCallExpression.Method;
                        }
                    }
                    throw new ArgumentException("Expression must be a member call.", nameof(expression));
                }
                /// <summary>
                /// Extracts the nested members (properties, fields, or methods) from <paramref name="expression"/>. The expression must be a nested member access or method call (e.g. x => x.Member1.Member2()), otherwise an exception will be thrown. The returned array will contain the members in the order they are accessed (e.g. [Member1, Member2]). This can be useful for scenarios where you need to access or manipulate nested members dynamically, such as in data binding, serialization, or dynamic code generation scenarios.
                /// </summary>
                /// <typeparam name="T">The type of the parameter in the expression.</typeparam>
                /// <typeparam name="TProperty">The type of the property or method return value.</typeparam>
                /// <param name="expression">The expression representing the nested member access or method call.</param>
                /// <returns>An array of <see cref="MemberInfo"/> representing the nested members in the order they are accessed.</returns>
                /// <exception cref="ArgumentNullException">Thrown if <paramref name="expression"/> is null.</exception>
                public static MemberInfo[] GetNestedMembers<T, TProperty>(Expression<Func<T, TProperty>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    var members = new List<MemberInfo>();
                    System.Linq.Expressions.Expression currentExpression = expression.Body;
                    while (currentExpression != null)
                    {
                        if (currentExpression is MemberExpression memberExpression)
                        {
                            members.Add(memberExpression.Member);
                            currentExpression = memberExpression.Expression;
                        }
                        else if (currentExpression is MethodCallExpression methodCallExpression)
                        {
                            members.Add(methodCallExpression.Method);
                            currentExpression = methodCallExpression.Object;
                        }
                        else if (currentExpression is UnaryExpression unaryExpression)
                        {
                            currentExpression = unaryExpression.Operand;
                        }
                        else
                        {
                            break;
                        }
                    }
                    members.Reverse();
                    return members.ToArray();
                }
                /// <summary>
                /// Extracts the nested members (properties, fields, or methods) from <paramref name="expression"/>. The expression must be a nested member access or method call (e.g. x => x.Member1.Member2()), otherwise an exception will be thrown. The returned array will contain the members in the order they are accessed (e.g. [Member1, Member2]). This can be useful for scenarios where you need to access or manipulate nested members dynamically, such as in data binding, serialization, or dynamic code generation scenarios.
                /// </summary>
                /// <typeparam name="TProperty">The type of the property or method return value.</typeparam>
                /// <param name="expression">The expression representing the nested member access or method call.</param>
                /// <returns>An array of <see cref="MemberInfo"/> representing the nested members in the order they are accessed.</returns>
                /// <exception cref="ArgumentNullException">Thrown if <paramref name="expression"/> is null.</exception>
                public static MemberInfo[] GetNestedMembers<TProperty>(Expression<Func<TProperty>> expression)
                {
                    if (expression == null) throw new ArgumentNullException(nameof(expression));
                    var members = new List<MemberInfo>();

                    System.Linq.Expressions.Expression currentExpression = expression.Body;
                    while (currentExpression != null)
                    {
                        if (currentExpression is MemberExpression memberExpression)
                        {
                            members.Add(memberExpression.Member);
                            currentExpression = memberExpression.Expression;
                        }
                        else if (currentExpression is MethodCallExpression methodCallExpression)
                        {
                            members.Add(methodCallExpression.Method);
                            currentExpression = methodCallExpression.Object;
                        }
                        else if (currentExpression is UnaryExpression unaryExpression)
                        {
                            currentExpression = unaryExpression.Operand;
                        }
                        else
                        {
                            break;
                        }
                    }
                    members.Reverse();
                    return members.ToArray();
                }
            }

            /// <summary>
            /// Helper class for traversing object hierarchies, such as finding fields or properties of a certain type in an object and its nested objects. The generic version allows for specifying the type to search for, while the non-generic version can be used for more general traversal without a specific target type.
            /// </summary>
            /// <typeparam name="T">The type to search for.</typeparam>
            public static class Traversing<T>
            {
                private static ConcurrentDictionary<string, Func<T, object>> _getters = new ConcurrentDictionary<string, Func<T, object>>();

                /// <summary>
                /// Tries to get a compiled getter function for the specified property name. The method first checks if a getter for the property name already exists in the cache. If it does, it returns it. If not, it attempts to find a property or field with the given name in the type T using reflection. If found, it creates a lambda expression to access that member, compiles it into a delegate, caches it for future use, and returns it. If no matching property or field is found, it caches a null value for that property name and returns false.
                /// </summary>
                /// <param name="propertyName">The name of the property or field to get the getter for.</param>
                /// <param name="getter">The compiled getter function if found; otherwise, null.</param>
                /// <returns>True if a getter was found or created; otherwise, false.</returns>
                public static bool TryGetPropertyGetter(string propertyName, out Func<T, object> getter)
                {
                    if (!_getters.TryGetValue(propertyName, out getter))
                    {
                        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                        System.Linq.Expressions.Expression getMember = null;
                        if (ToolkitConstants.ObjectCache<T>.IndexedProperties.TryGetValue(propertyName, out var propertyInfo))
                        {
                            getMember = System.Linq.Expressions.Expression.Property(parameter, propertyInfo);
                        }
                        else if (ToolkitConstants.ObjectCache<T>.IndexedFields.TryGetValue(propertyName, out var fieldInfo))
                        {
                            getMember = System.Linq.Expressions.Expression.Field(parameter, fieldInfo);
                        }

                        if (getMember == null)
                        {
                            _getters[propertyName] = null;
                            getter = null;
                            return false;
                        }

                        var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, object>>(System.Linq.Expressions.Expression.Convert(getMember, typeof(object)), parameter);
                        getter = lambda.Compile();
                        _getters[propertyName] = getter;
                    }

                    return getter != null;
                }
                /// <summary>
                /// Returns all members (properties and fields) of type T that are indexed in the ObjectCache. This method retrieves the cached properties and fields for the specified type T and yields them as an enumerable collection of MemberInfo objects. It can be useful for scenarios where you need to inspect or manipulate the members of a type dynamically, such as in serialization, data binding, or reflection-based operations.
                /// </summary>
                /// <returns>An enumerable collection of MemberInfo objects representing the indexed members of type T.</returns>
                public static IEnumerable<MemberInfo> GetMembers()
                {
                    foreach (var property in ToolkitConstants.ObjectCache<T>.IndexedProperties.Values)
                    {
                        yield return property;
                    }
                    foreach (var field in ToolkitConstants.ObjectCache<T>.IndexedFields.Values)
                    {
                        yield return field;
                    }
                }
            }

            /// <summary>
            /// Helper class for traversing object hierarchies, such as finding fields or properties of a certain type in an object and its nested objects.
            /// </summary>
            public static class Traversing
            {
                private static ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<object, object>>> _typeGetters = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<object, object>>>();
                private static ConcurrentDictionary<Type, Func<IEnumerable<MemberInfo>>> _memberGetters = new ConcurrentDictionary<Type, Func<IEnumerable<MemberInfo>>>();
                private static readonly BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

                /// <summary>
                /// Traverses the object hierarchy of the provided object to find a property or field with the specified name and returns its value. The method first checks if a getter for the property name already exists in the cache for the object's type. If it does, it uses it to get the value. If not, it attempts to find a property or field with the given name in the object's type using reflection. If found, it creates a lambda expression to access that member, compiles it into a delegate, caches it for future use, and uses it to get the value. If no matching property or field is found, it caches a null value for that property name and returns null.
                /// </summary>
                /// <param name="obj">The object whose hierarchy is to be traversed.</param>
                /// <param name="propertyName">The name of the property or field to find.</param>
                /// <returns>The value of the property or field if found; otherwise, null.</returns>
                public static object Traverse(object obj, string propertyName)
                {
                    if (obj == null) return null;
                    if (string.IsNullOrWhiteSpace(propertyName)) return null;

                    var objType = obj.GetType();
                    var gettersForType = _typeGetters.GetOrAdd(objType, _ => new ConcurrentDictionary<string, Func<object, object>>());
                    var memberName = propertyName.Trim();
                    if (!gettersForType.TryGetValue(memberName, out var getter))
                    {
                        var typedTraversingType = typeof(Traversing<>).MakeGenericType(objType);
                        var tryGetPropertyGetterMethod = typedTraversingType.GetMethod(nameof(Traversing<object>.TryGetPropertyGetter), PublicStatic);
                        if (tryGetPropertyGetterMethod == null)
                        {
                            gettersForType[memberName] = null;
                            return null;
                        }

                        var parameters = new object[] { memberName, null };
                        if ((bool)tryGetPropertyGetterMethod.Invoke(null, parameters) && parameters[1] is Delegate typedGetter)
                        {
                            // Build an object-based wrapper by first casting to Func<T, object> and then invoking it.
                            var typedGetterDelegateType = typeof(Func<,>).MakeGenericType(objType, typeof(object));
                            var objectParameter = System.Linq.Expressions.Expression.Parameter(typeof(object), "x");
                            var castedObject = System.Linq.Expressions.Expression.Convert(objectParameter, objType);
                            var castedTypedGetter = System.Linq.Expressions.Expression.Convert(
                                System.Linq.Expressions.Expression.Constant(typedGetter),
                                typedGetterDelegateType);
                            var invocation = System.Linq.Expressions.Expression.Invoke(castedTypedGetter, castedObject);
                            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(invocation, objectParameter);
                            getter = lambda.Compile();
                        }
                        else
                        {
                            getter = null;
                        }

                        gettersForType[memberName] = getter;
                    }
                    if (getter == null)
                    {
                        return null;
                    }
                    return getter(obj);
                }

                /// <summary>
                /// Traverses the object hierarchy of the provided object to find a property or field with the specified name at each level of the provided property path and returns its value. The method iteratively calls the Traverse method for each property name in the path, starting from the initial object and using the result of each traversal as the input for the next. If at any point a property or field is not found or if any intermediate value is null, the method returns null.
                /// </summary>
                /// <param name="obj">The object whose hierarchy is to be traversed.</param>
                /// <param name="propertyPath">An array of property or field names representing the path to traverse.</param>
                /// <returns>The value of the property or field at the end of the path if found; otherwise, null.</returns>
                public static object TraversePath(object obj, params string[] propertyPath)
                {
                    if (propertyPath == null || propertyPath.Length == 0)
                    {
                        return obj;
                    }

                    object currentObj = obj;
                    foreach (var propertyName in propertyPath)
                    {
                        if (string.IsNullOrWhiteSpace(propertyName))
                        {
                            continue;
                        }

                        if (currentObj == null) return null;
                        currentObj = Traverse(currentObj, propertyName);
                    }
                    return currentObj;
                }

                /// <summary>
                /// Traverses the provided <paramref name="obj"/> using a single delimited path.
                /// </summary>
                /// <param name="obj">The root object to traverse.</param>
                /// <param name="propertyPath">The path to traverse, using '.' as delimiter.</param>
                /// <returns>The value at the end of the path, or null when not found.</returns>
                public static object TraversePath(object obj, string propertyPath)
                {
                    return TraversePath(obj, SplitPath(propertyPath));
                }

                /// <summary>
                /// Attempts to traverse <paramref name="obj"/> using a single delimited path.
                /// </summary>
                /// <param name="obj">The root object to traverse.</param>
                /// <param name="propertyPath">The path to traverse, using '.' as delimiter.</param>
                /// <param name="value">The resolved value if successful; otherwise null.</param>
                /// <returns>True if a non-null value was resolved; otherwise false.</returns>
                public static bool TryTraversePath(object obj, string propertyPath, out object value)
                {
                    value = TraversePath(obj, propertyPath);
                    return value != null;
                }

                /// <summary>
                /// Attempts to traverse <paramref name="obj"/> using path segments.
                /// </summary>
                /// <param name="obj">The root object to traverse.</param>
                /// <param name="propertyPath">Path segments that represent the traversal path.</param>
                /// <param name="value">The resolved value if successful; otherwise null.</param>
                /// <returns>True if a non-null value was resolved; otherwise false.</returns>
                public static bool TryTraversePath(object obj, IEnumerable<string> propertyPath, out object value)
                {
                    value = TraversePath(obj, propertyPath?.ToArray());
                    return value != null;
                }

                /// <summary>
                /// Returns all members (properties and fields) of the specified <paramref name="type"/> that are indexed in the ObjectCache. This method retrieves the cached properties and fields for the specified type and returns them as an enumerable collection of MemberInfo objects. It can be useful for scenarios where you need to inspect or manipulate the members of a type dynamically, such as in serialization, data binding, or reflection-based operations.
                /// </summary>
                /// <param name="type">The type whose members are to be retrieved.</param>
                /// <returns>An enumerable collection of MemberInfo objects representing the indexed members of the specified type.</returns>
                /// <exception cref="ArgumentNullException">Thrown when the provided type is null.</exception>
                public static IEnumerable<MemberInfo> GetMembers(Type type)
                {
                    if (type == null) throw new ArgumentNullException(nameof(type));
                    return _memberGetters.GetOrAdd(type, t =>
                    {
                        var typedTraversingType = typeof(Traversing<>).MakeGenericType(t);
                        var getMembersMethod = typedTraversingType.GetMethod(nameof(Traversing<object>.GetMembers), PublicStatic);

                        var lambda = System.Linq.Expressions.Expression.Lambda<Func<IEnumerable<MemberInfo>>>(
                            System.Linq.Expressions.Expression.Call(getMembersMethod));
                        return lambda.Compile();
                    })();
                }

                /// <summary>
                /// Splits a dot-delimited property path into its segments.
                /// </summary>
                /// <param name="propertyPath">The path to split.</param>
                /// <returns>The normalized path segments.</returns>
                public static string[] SplitPath(string propertyPath)
                {
                    if (string.IsNullOrWhiteSpace(propertyPath))
                    {
                        return Array.Empty<string>();
                    }

                    return propertyPath
                        .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
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

                /// <summary>
                /// Logs a verbose message with fallback to console output.
                /// </summary>
                public static void LogVerbose(string message)
                    => Write(() =>
                    {
                        if (Toolkit.Settings.Verbose)
                        {
                            Verse.Log.Message(message);
                        }
                    }, "DBG", message);

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
                /// <summary>
                /// Tries to invoke the provided function and returns its result, with error handling and logging. If the function throws an exception, it will be caught and logged as an error, and the method will return the specified default value instead. This allows for safe invocation of code that may potentially fail without crashing the game or causing unintended side effects.
                /// </summary>
                /// <typeparam name="T">The type of the return value of the function.</typeparam>
                /// <param name="func">The function to invoke safely.</param>
                /// <param name="defaultValue">The default value to return if the function throws an exception.</param>
                /// <returns>The result of the function, or the default value if an exception occurred.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static T Safe<T>(Func<T> func, T defaultValue = default)
                {
                    try
                    {
                        return func();
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError($"An error occurred while invoking method: {ex}");
                        return defaultValue;
                    }
                }
            }
        }
    }
    /// <summary>
    /// Contains the settings for <see cref="Toolkit"/>
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
        /// <summary>
        /// Enables verbose logging for the toolkit, which can help with debugging and understanding the internal workings of the toolkit. 
        /// This will log detailed information about the gathering process, including what is being gathered and when, as well as any potential issues or errors that occur during gathering. Use this option if you want to get insights into how the toolkit is operating or if you're trying to troubleshoot any problems with data gathering. Keep in mind that enabling verbose logging may result in a large amount of log output, so it's generally recommended to use this option only when needed for debugging purposes.
        /// </summary>
        public bool Verbose = false;

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
            Scribe_Values.Look(ref Verbose, nameof(Verbose), defaultValue: false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Toolkit.Hooks.Manager.Trigger(new Changed(this));
            }
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
