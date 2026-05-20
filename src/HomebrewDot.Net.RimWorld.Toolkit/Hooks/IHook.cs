using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Generic;

namespace HomebrewDot.Net.RimWorld.Hooks
{
    /// <summary>
    /// Represents a hook that will be invoked when a certain trigger occurs. Hooks are used to execute custom code in response to specific events or conditions in the game.
    /// </summary>
    /// <typeparam name="T">The type of argument that the hook will receive when invoked.</typeparam>
    public interface IHook<T> : IHandler
    {
        /// <summary>
        /// Who owns/created this hook. Can be used to group hooks together for easier management, such as unregistering all hooks from a specific owner at once.
        /// </summary>
        object Owner { get; }
        /// <summary>
        /// If true, the hook will only be invoked once and then automatically unregistered. If false, the hook will remain registered and continue to be invoked every time the trigger occurs until it is manually unregistered.
        /// Only applied when <see cref="OnTrigger(T)"/> returns true, otherwise the hook will remain registered regardless of this value.
        /// </summary>
        bool Once { get; }

        /// <summary>
        /// Trigger the hook with the specified argument. This method will be called by the hook manager when the corresponding event or condition occurs.
        /// </summary>
        /// <param name="arg">The argument to pass to the hook when it is triggered.</param>
        /// <returns>True if the hook was successfully triggered; otherwise, false.</returns>
        bool OnTrigger(T arg);
    }
}
