using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.RimWorld.Collecting;
using HomebrewDot.Net.RimWorld.Collecting.Components;
using HomebrewDot.Net.RimWorld.Collecting.Models;
using HomebrewDot.Net.RimWorld.Comparing;
using HomebrewDot.Net.RimWorld.Comparing.Components;
using HomebrewDot.Net.RimWorld.Generic.Models;
using HomebrewDot.Net.RimWorld.Hooks;
using HomebrewDot.Net.RimWorld.Hooks.Triggers;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Components;
using HomebrewDot.Net.RimWorld.Referencing;
using HomebrewDot.Net.RimWorld.Referencing.Components;
using HomebrewDot.Net.RimWorld.UI.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;

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

        // Fields
        private readonly ToolkitSettingsUi _settingsUi;

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
            foreach(var alias in GreaterOrEqualOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(GreaterOrEqualOperatorType.Instance, alias);
            }
            foreach(var alias in LesserOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(LesserOperatorType.Instance, alias);
            }
            foreach(var alias in LesserOrEqualOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(LesserOrEqualOperatorType.Instance, alias);
            }
            foreach(var alias in TrueOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(TrueOperatorType.Instance, alias);
            }
            foreach(var alias in FalseOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(FalseOperatorType.Instance, alias);
            }
            foreach(var alias in NullOperatorType.Aliases)
            {
                Services.Register<IOperatorType>(NullOperatorType.Instance, alias);
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
                        if (_manager is IDisposable disposable)
                        {
                            disposable.Dispose();
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
        public static class Index
        {
            // Statics
            static Index()
            {
                Hooks.Manager.RegisterHook<OnGameLoadedTrigger>(Instance, (e) => StartIndexing(e.Game))
                             .RegisterHook<OnSaveLoadedTrigger>(Instance, (e) => StartIndexing(e.Game))
                             .RegisterHook<ToolkitSettings.Changed>(Instance, e => StartIndexing(Current.Game));
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
                    orchestrator?.RebuildIndex(game, game == null, Manager, _orchestratorConfig, _managerConfig, _schemaConfig);
                    if (takeSnapshot)
                    {
                        Manager.Snapshot();
                    }
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
        /// Tools for collecting game data into collections based on defined conditions and criteria, allowing for efficient organization and retrieval of related data.
        /// </summary>
        public static class Collecting
        {
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
                    if(_comparator is Comparator collectionComparator)
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
            }
            /// <summary>
            /// Adds a new collector with the specified name and collector instance. The collection definition associated with the collector will also be added using the same name. If a collector with the same name already exists, it will be overwritten with the new collector and definition.
            /// </summary>
            /// <typeparam name="T">The type of items collected by the collector.</typeparam>
            /// <param name="name">The name of the collector.</param>
            /// <param name="collector">The collector instance.</param>
            public static void Set(string name, ICollector collector)
            {
                name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                collector = Helpers.Guard.NotNull(collector, nameof(collector));
                var collection = Helpers.Guard.NotNull(collector.Definition, nameof(collector.Definition));

                lock (_lock)
                {
                    _collectors[name] = collector;
                    Set(name, collection);
                }
            }
            /// <summary>
            /// Adds a new collector with the specified name and collector instance. The collection definition associated with the collector will also be added using the same name. If a collector with the same name already exists, it will be overwritten with the new collector and definition.
            /// </summary>
            /// <typeparam name="T">The type of items collected by the collector.</typeparam>
            /// <param name="name">The name of the collector.</param>
            /// <param name="collector">The collector instance.</param>
            public static void Set<T>(string name, ICollector<T> collector) where T : class
            {
                name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                collector = Helpers.Guard.NotNull(collector, nameof(collector));

                Set(name, (ICollector)collector);
            }
            /// <summary>
            /// Removes the collector and collection definition associated with the specified name. If no collector or definition exists with the given name, this method does nothing.
            /// </summary>
            /// <param name="name">The name of the collector and collection definition to remove.</param>
            public static void Remove(string name)
            {
                lock (_lock)
                {
                    if (_collectors.TryGetValue(name, out var collector))
                    {
                        Invoking.Safe(() => collector.StopCollecting());
                        if (collector is IDisposable disposable)
                        {
                            Invoking.Safe(() => disposable.Dispose());
                        }
                        _collectors.Remove(name);
                    }
                    _collectionDefinitions.Remove(name);
                }
            }

            /// <summary>
            /// Adds a new collection (and optionally a collector) using the specified build action.
            /// </summary>
            /// <param name="name">The name of the collection.</param>
            /// <param name="buildAction">A function that takes a collection builder and returns the built collection.</param>
            public static void Build(string name, Func<ICollectionBuilder, ICollectionBuilder> buildAction)
            {
                name = Helpers.Guard.NotNullOrWhitespace(name, nameof(name));
                buildAction = Helpers.Guard.NotNull(buildAction, nameof(buildAction));

                var builder = new CollectionBuilder();
                _ = buildAction(builder);
                var collection = Guard.NotNull(builder.Collection, nameof(builder.Collection));
                if(builder.TryBuildCollector(collection, out var collector))
                {
                    Set(name, collector);
                    return;
                }
                Set(name, collection);
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
            private static readonly Dictionary<Type, List<object>> _services = new Dictionary<Type, List<object>>();
            private static readonly Dictionary<Type, Dictionary<string, object>> _namedServices = new Dictionary<Type, Dictionary<string, object>>();

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
                        serviceList = new List<object>();
                        _services[typeof(T)] = serviceList;
                    }
                    serviceList.Add(service);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (!_namedServices.TryGetValue(typeof(T), out var namedServiceDict))
                        {
                            namedServiceDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            _namedServices[typeof(T)] = namedServiceDict;
                        }
                        namedServiceDict[name] = service;
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
                    bool wasRemoved = _services.TryGetValue(typeof(T), out var serviceSet) && serviceSet.Remove(service);
                    if (wasRemoved)
                    {
                        if(_namedServices.TryGetValue(typeof(T), out var namedServiceDict))
                        {
                            var namesToRemove = namedServiceDict.Where(kvp => kvp.Value.Equals(service)).Select(kvp => kvp.Key).ToList();
                            foreach (var name in namesToRemove)
                            {
                                namedServiceDict.Remove(name);
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
                    if (_namedServices.TryGetValue(typeof(T), out var namedServiceDict) && namedServiceDict.TryGetValue(name, out var service))
                    {
                        namedServiceDict.Remove(name);
                        return _services.TryGetValue(typeof(T), out var serviceSet) && serviceSet.Remove(service);
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
                lock (_lock)
                {
                    var services = _services.TryGetValue(typeof(T), out var serviceList)
                        ? serviceList.OfType<T>().ToList()
                        : new List<T>();
                    if (includeNamed)
                    {
                        if(_namedServices.TryGetValue(typeof(T), out var namedServiceDict))
                        {
                            services.AddRange(namedServiceDict.Values.OfType<T>());
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
                        return namedServiceDict.Where(kvp => kvp.Value is T).ToDictionary(kvp => kvp.Key, kvp => (T)kvp.Value, StringComparer.OrdinalIgnoreCase);
                    }
                    return NullDictionary<string, T>.Instance;
                }
            }

            /// <summary>
            /// Retrieves the first registered service of the specified type. If no service of the given type is registered, this method returns null.
            /// </summary>
            /// <typeparam name="T">The type of the service to retrieve.</typeparam>
            /// <param name="name">The optional name of the service to retrieve. If provided, the method will first attempt to find a service with the specified name and type before falling back to searching for any service of the given type.</param>
            /// <returns>The first registered service of the specified type, or null if none is found.</returns>
            public static T Get<T>(string name = null)
            {
                lock (_lock)
                {
                    if(name != null)
                    {
                        if(_namedServices.TryGetValue(typeof(T), out var namedServiceDict) && namedServiceDict.TryGetValue(name, out var service) && service is T typedService)
                        {
                            return typedService;
                        }
                    }
                    return _services.TryGetValue(typeof(T), out var serviceList)
                        ? serviceList.OfType<T>().LastOrDefault()
                        : default(T);
                }
            }
            /// <summary>
            /// Retrieves the first registered service of the specified type. If no service of the given type is registered, this method throws an InvalidOperationException.
            /// </summary>
            /// <typeparam name="T">The type of the service to retrieve.</typeparam>
            /// <returns>The first registered service of the specified type.</returns>
            /// <exception cref="InvalidOperationException">Thrown if no service of the specified type is registered.</exception>
            public static T GetRequired<T>(string name = null)
            {
                lock (_lock)
                {
                    if(name != null)
                    {
                        if(_namedServices.TryGetValue(typeof(T), out var namedServiceDict) && namedServiceDict.TryGetValue(name, out var service) && service is T typedService)
                        {
                            return typedService;
                        }
                        throw new InvalidOperationException($"No service of type {typeof(T)} with name '{name}' is registered.");
                    }
                    var foundService = _services.TryGetValue(typeof(T), out var serviceList)
                        ? serviceList.OfType<T>().LastOrDefault()
                        : default(T);
                    if (foundService == null)
                    {
                        throw new InvalidOperationException($"No service of type {typeof(T)} is registered.");
                    }
                    return foundService;
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
