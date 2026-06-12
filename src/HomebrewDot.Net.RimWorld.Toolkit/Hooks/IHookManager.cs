using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Hooks
{
    /// <summary>
    /// Manages the registration and invocation of hooks within the toolkit, allowing external code to subscribe to various game events and trigger custom behavior in response.
    /// </summary>
    public interface IHookManager
    {
        /// <summary>
        /// Registers <paramref name="hook"/> that will be invoked when event <typeparamref name="T"/> is triggered.
        /// </summary>
        /// <typeparam name="T">The type of event that the hook will respond to.</typeparam>
        /// <param name="hook">The hook to register.</param>
        void RegisterHook<T>(IHook<T> hook);
        /// <summary>
        /// Unregisters <paramref name="hook"/> so that it will no longer be invoked when event <typeparamref name="T"/> is triggered.
        /// </summary>
        /// <typeparam name="T">The type of event that the hook will no longer respond to.</typeparam>
        /// <param name="hook">The hook to unregister.</param>
        void UnregisterHook<T>(IHook<T> hook);
        /// <summary>
        /// Gets all hooks owned by <paramref name="owner"/>.
        /// </summary>
        /// <typeparam name="T">The type of event that the hooks respond to.</typeparam>
        /// <param name="owner">The owner whose hooks are to be retrieved.</param>
        /// <returns>An array of hooks owned by the specified owner.</returns>
        IHook<T>[] GetOwnerBy<T>(object owner);
        /// <summary>
        /// Unregisters all hooks owned by <paramref name="owner"/>.
        /// </summary>
        /// <typeparam name="T">The type of event that the hooks respond to.</typeparam>
        /// <param name="owner">The owner whose hooks are to be unregistered.</param>
        /// <returns>An array of hooks that were unregistered.</returns>
        IHook<T>[] UnregisterAllBy<T>(object owner);
        /// <summary>
        /// Triggers event <typeparamref name="T"/>, invoking all registered hooks that respond to this event type with the provided argument <paramref name="arg"/>.
        /// </summary>
        /// <typeparam name="T">The type of event to trigger.</typeparam>
        /// <param name="arg">The event being triggered.</param>
        void Trigger<T>(T arg);
        /// <summary>
        /// Uses <paramref name="argFactory"/> to create the argument for event <typeparamref name="T"/> and triggers the event, invoking all registered hooks that respond to this event type. This allows for lazy evaluation of the event argument, which can be useful if the argument is expensive to create or if it should only be created if there are hooks that will respond to it.
        /// </summary>
        /// <typeparam name="T">The type of event to trigger.</typeparam>
        /// <param name="argFactory">A function that creates the event argument.</param>
        void LazyTrigger<T>(Func<T> argFactory);
        /// <summary>
        /// Transfers all hooks from this manager to <paramref name="newManager"/>, effectively moving the responsibility of managing and invoking these hooks to the new manager. After this operation, this manager will no longer have any registered hooks, and all hooks will be managed by the new manager.
        /// </summary>
        /// <param name="newManager">The new manager for the hooks.</param>
        void TransferTo(IHookManager newManager);
    }

    /// <summary>
    /// Contains extension methods for <see cref="IHookManager"/> to provide additional functionality and convenience methods for working with hooks.
    /// </summary>
    public static class IHookManagerExtensions
    {
        /// <summary>
        /// Registers a simple hook that uses delegates to trigger an action when a specific event occurs and contains basic error handling. This is intended for simple hooks that don't require complex state management or multiple triggers.
        /// </summary>
        /// <typeparam name="T">The type of event that the hook will respond to.</typeparam>
        /// <param name="hookManager">The hook manager to register the hook with.</param>
        /// <param name="owner">The owner of the hook.</param>
        /// <param name="action">The action to execute when the event is triggered.</param>
        /// <param name="once">Indicates whether the hook should be triggered only once.</param>
        /// <param name="errorHandler">A function to handle errors that occur during the execution of the hook.</param>
        /// <param name="priority">The priority of the hook, determining the order in which hooks are executed.</param>
        public static IHookManager RegisterHook<T>(this IHookManager hookManager, object owner, Action<T> action, bool once = false, Func<Exception, T, bool> errorHandler = null, byte priority = 128) where T : class
        {
            hookManager = Toolkit.Helpers.Guard.NotNull(hookManager, nameof(hookManager));
            hookManager.RegisterHook(new SimpleHook<T>(owner, action,once, errorHandler, priority));
            return hookManager;
        }
        /// <summary>
        /// Registers a simple hook that uses delegates to trigger an action when a specific event occurs and contains basic error handling. This is intended for simple hooks that don't require complex state management or multiple triggers.
        /// </summary>
        /// <typeparam name="T">The type of event that the hook will respond to.</typeparam>
        /// <param name="hookManager">The hook manager to register the hook with.</param>
        /// <param name="owner">The owner of the hook.</param>
        /// <param name="action">The action to execute when the event is triggered.</param>
        /// <param name="once">Indicates whether the hook should be triggered only once.</param>
        /// <param name="errorHandler">A function to handle errors that occur during the execution of the hook.</param>
        /// <param name="priority">The priority of the hook, determining the order in which hooks are executed.</param>
        public static IHookManager RegisterHook<T>(this IHookManager hookManager, object owner, Func<T, bool> action, bool once = false, Func<Exception, T, bool> errorHandler = null, byte priority = 128) where T : class
        {
            hookManager = Toolkit.Helpers.Guard.NotNull(hookManager, nameof(hookManager));
            hookManager.RegisterHook(new SimpleHook<T>(owner, action, once, errorHandler, priority));
            return hookManager;
        }
    }
}
