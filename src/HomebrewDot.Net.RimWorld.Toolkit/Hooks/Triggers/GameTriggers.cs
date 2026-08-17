using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Generic;
using RimWorld;
using Verse;

namespace HomebrewDot.Net.Rimworld.Hooks.Triggers
{
    /// <summary>
    /// Triggers that fires events during the lifecycle of the game, such as when a game is loaded.
    /// </summary>
    [StaticConstructorOnStartup]
    public class GameTriggers : GameComponent
    {
        // Statics
        private static bool _hasTriggered = false;

        // Fields
        private readonly Game _game;

        /// <summary>
        /// Creates the game trigger component for the current game instance.
        /// </summary>
        /// <param name="game">The game instance that owns this component.</param>
        public GameTriggers(Game game)
        {
            _game = Toolkit.Helpers.Guard.NotNull(game, nameof(game));
        }

        /// <summary>
        /// Static constructor to register the trigger with the game's event system.
        /// </summary>
        static GameTriggers()
        {
            var harmony = Toolkit.Harmony;
            //var postfix = AccessTools.Method(typeof(Patches), nameof(Patches.DoSingleTick_Postfix));
            //harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick)), postfix: new HarmonyMethod(postfix));
            var prefix = AccessTools.Method(typeof(Patches), nameof(Patches.LoadGame_Prefix));
            harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.LoadGame)), prefix: new HarmonyMethod(prefix));
            var prefixUnload = AccessTools.Method(typeof(Patches), nameof(Patches.Dispose_Prefix));
            harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.Dispose)), prefix: new HarmonyMethod(prefixUnload));
            var postfixMenu = AccessTools.Method(typeof(Patches), nameof(Patches.MainMenuOnGUI_Postfix));
            harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI)), postfix: new HarmonyMethod(postfixMenu));
        }

        /// <inheritdoc/>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            var requestedTickers = new List<Ticker>
            {
                new Ticker(_game, TickerType.Normal, 1, Toolkit.Hooks.Manager.GetTriggerer<OnGameTickTrigger>()),
                new Ticker(_game, TickerType.Rare, 2, Toolkit.Hooks.Manager.GetTriggerer<OnGameTickTrigger>()),
                new Ticker(_game, TickerType.Long, 3, Toolkit.Hooks.Manager.GetTriggerer<OnGameTickTrigger>())
            };

            foreach (var ticker in requestedTickers)
            {
                var request = new RequestTickManagement(ticker, true);
                var accepted = Toolkit.Hooks.Manager.Trigger(request);
                if(!accepted)
                {
                    Log.Warning($"Ticker of type {ticker.TickerType} was not accepted by the tick manager. Hooks will not be fired");
                }
            }
        }

        /// <inheritdoc/>
        public override void LoadedGame()
        {
            base.LoadedGame();
            Toolkit.Hooks.Manager.Trigger(new OnSaveLoadedTrigger(_game, false));
        }
        /// <inheritdoc/>
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            Toolkit.Hooks.Manager.Trigger(new OnSaveLoadedTrigger(_game, true));
        }

        /// <summary>
        /// Contains Harmony patches related to game lifecycle events, such as ticking, to trigger corresponding events in the toolkit after the original game methods are executed.
        /// </summary>
        public static class Patches
        {
            /// <summary>
            /// Harmony postfix patch for the TickManager's DoSingleTick method to trigger a game tick event after each tick is processed.
            /// </summary>
            /// <param name="__instance"></param>
            public static void DoSingleTick_Postfix(TickManager __instance)
            {
                if(Current.Game != null)
                {
                    var tickerType = TickerType.Normal;

                    if (__instance.TicksGame % ToolkitConstants.TickLongInterval == 0)
                    {
                        tickerType = TickerType.Long;
                    }
                    else if (__instance.TicksGame % ToolkitConstants.TickRareInterval == 0)
                    {
                        tickerType = TickerType.Rare;
                    }
                    Toolkit.Hooks.Manager.Trigger(new OnGameTickTrigger(Current.Game, tickerType));
                }
            }

            /// <summary>
            /// Harmony prefix patch for the Game's LoadGame method to trigger a game loading event before the game starts loading, allowing subscribers to perform actions or preparations before the game data is loaded.
            /// </summary>
            /// <param name="__instance"></param>
            public static void LoadGame_Prefix(Game __instance)
            {
                Toolkit.Hooks.Manager.Trigger(new OnSaveLoadingTrigger(__instance));
            }

            /// <summary>
            /// Harmony prefix patch for the Game's Dispose method to trigger a game unloaded event before the game is torn down, allowing subscribers to still access the game, its maps and world while handling the unload.
            /// </summary>
            /// <param name="__instance">The game instance that is being unloaded.</param>
            public static void Dispose_Prefix(Game __instance)
            {
                Toolkit.Hooks.Manager.Trigger(new OnGameUnloadedTrigger(__instance));
            }

            /// <summary>
            /// Harmony postfix patch for the MainMenuDrawer's MainMenuOnGUI method to trigger a game loaded event after the main menu is drawn, ensuring that the event is only triggered once when the game is first loaded.
            /// </summary>
            public static void MainMenuOnGUI_Postfix()
            {
                if(!_hasTriggered)
                {
                    Toolkit.Hooks.Manager.Trigger(OnGameLoadedTrigger.Instance);
                    _hasTriggered = true;
                }
            }
        }

        private class Ticker : IManagedTickable
        {
            // Fields
            private readonly IHookTriggerer<OnGameTickTrigger> _triggerer;
            private readonly Game _game;
            private readonly TickerType _tickerType;
            private readonly int _hash;

            public int Bucket { get; set; }

            public int Hash => _hash;

            public int Interval { get; }
            public TickerType TickerType => _tickerType;

            public Ticker(Game game, TickerType tickerType, int hash, IHookTriggerer<OnGameTickTrigger> triggerer)
            {
                _game = game;
                _tickerType = tickerType;
                _hash = hash;
                _triggerer = triggerer;
                Interval = tickerType switch
                {
                    TickerType.Normal => ToolkitConstants.TickNormalInterval,
                    TickerType.Rare => ToolkitConstants.TickRareInterval,
                    TickerType.Long => ToolkitConstants.TickLongInterval,
                    _ => throw new ArgumentOutOfRangeException(nameof(tickerType), tickerType, null)
                };
            }

            public void NotifyRemoved()
            {
                Log.Warning($"Ticker of type {_tickerType} was removed from the tick manager. Hooks will not be fired");
            }

            public bool Tick()
            {
                _triggerer.Trigger(new OnGameTickTrigger(_game, _tickerType));
                return true;
            }
        }
    }

    /// <summary>
    /// Fired when the game is loaded for the first time. 
    /// </summary>
    public class OnGameLoadedTrigger
    {
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="OnGameLoadedTrigger"/>, which can be used to reference this trigger without needing to create multiple instances.
        /// </summary>
        public static readonly OnGameLoadedTrigger Instance = new OnGameLoadedTrigger();
    }


    /// <summary>
    /// Fired when the game is loading for the first time. 
    /// </summary>
    public class OnSaveLoadingTrigger
    {
        // Properties
        /// <summary>
        /// The current game instance that is loading, providing access to game data.
        /// </summary>
        public Game Game { get; }

        internal OnSaveLoadingTrigger(Game game)
        {
            Game = Toolkit.Helpers.Guard.NotNull(game, nameof(game));
        }
    }

    /// <summary>
    /// Fired when the current game is unloaded, either by returning to the main menu or by loading another save which replaces the current game. The game instance is about to be torn down, so its data is still accessible when the trigger fires.
    /// </summary>
    public class OnGameUnloadedTrigger
    {
        // Properties
        /// <summary>
        /// The game instance that is being unloaded, providing access to game data before teardown.
        /// </summary>
        public Game Game { get; }

        /// <inheritdoc cref="OnGameUnloadedTrigger"/>
        /// <param name="game"><see cref="Game"/></param>
        internal OnGameUnloadedTrigger(Game game)
        {
            Game = Toolkit.Helpers.Guard.NotNull(game, nameof(game));
        }
    }

    /// <summary>
    /// Trigger that runs each time a save is loaded or when starting a new game.
    /// </summary>
    public class OnSaveLoadedTrigger
    {
        // Properties
        /// <summary>
        /// The current game instance that was loaded, providing access to game data.
        /// </summary>
        public Game Game { get; }
        /// <summary>
        /// Indicates whether the loaded game is a new game.
        /// </summary>
        public bool IsNewGame { get; }

        /// <inheritdoc cref="OnSaveLoadedTrigger"/>
        /// <param name="game"></param>
        /// <param name="isNewGame"></param>
        internal OnSaveLoadedTrigger(Game game, bool isNewGame)
        {
            Game = Toolkit.Helpers.Guard.NotNull(game, nameof(game));
            IsNewGame = isNewGame;
        }
    }

    public class OnGameTickTrigger
    {
        // Properties
        /// <summary>
        /// The current game instance, providing access to game data.
        /// </summary>
        public Game Game { get; }
        /// <summary>
        /// Indicates the type of tick that is occurring, such as a normal tick or a rare tick, allowing subscribers to respond to specific tick types if desired.
        /// </summary>
        public TickerType TickerType { get; }

        internal OnGameTickTrigger(Game game, TickerType tickerType)
        {
            Game = Toolkit.Helpers.Guard.NotNull(game, nameof(game));
            TickerType = tickerType;
        }
    }
}
