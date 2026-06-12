using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HomebrewDot.Net.Rimworld;
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
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (Current.Game != null && !_hasTriggered)
                {
                    _hasTriggered = true;
                    Toolkit.Hooks.Manager.Trigger(new OnGameLoadedTrigger(Current.Game));
                }
            });

            var harmony = Toolkit.Harmony;
            var postfix = AccessTools.Method(typeof(Patches), nameof(Patches.DoSingleTick_Postfix));
            harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick)), postfix: new HarmonyMethod(postfix));
            var prefix = AccessTools.Method(typeof(Patches), nameof(Patches.LoadGame_Prefix));
            harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.LoadGame)), prefix: new HarmonyMethod(prefix));
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
        }


        /// <inheritdoc/>
        public override void LoadedGame()
        {
            base.LoadedGame();
            Toolkit.Hooks.Manager.Trigger(new OnSaveLoadedTrigger(Current.Game, false));
        }
        /// <inheritdoc/>
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            Toolkit.Hooks.Manager.Trigger(new OnSaveLoadedTrigger(Current.Game, true));
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
                    Toolkit.Hooks.Manager.LazyTrigger(() => new OnGameTickTrigger(Current.Game, TickerType.Normal));
                    if(__instance.TicksGame % ToolkitConstants.TickRareInterval == 0)
                    {
                        Toolkit.Hooks.Manager.LazyTrigger(() => new OnGameTickTrigger(Current.Game, TickerType.Rare));
                    }
                    if(__instance.TicksGame % ToolkitConstants.TickLongInterval == 0)
                    {
                        Toolkit.Hooks.Manager.LazyTrigger(() => new OnGameTickTrigger(Current.Game, TickerType.Long));
                    }
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
        }
    }

    /// <summary>
    /// Fired when the game is loaded for the first time. 
    /// </summary>
    public class OnGameLoadedTrigger
    {
        // Properties
        /// <summary>
        /// The current game instance that was loaded, providing access to game data.
        /// </summary>
        public Game Game { get; }

        internal OnGameLoadedTrigger(Game game)
        {
            Game = Toolkit.Helpers.Guard.NotNull(game, nameof(game));
        }
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
