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
        /// <returns>If a hook was triggered, otherwise false</returns>
        bool Trigger<T>(T arg);
        /// <summary>
        /// Triggers event <typeparamref name="T"/>, invoking all registered hooks that respond to this event type with the provided argument <paramref name="arg"/>.
        /// Delays the trigger to next tick.
        /// </summary>
        /// <typeparam name="T">The type of event to trigger.</typeparam>
        /// <param name="arg">The event being triggered.</param>
        /// <returns>If a hook was triggered, otherwise false</returns>
        void TriggerDelayed<T>(T arg);
        /// <summary>
        /// Transfers all hooks from this manager to <paramref name="newManager"/>, effectively moving the responsibility of managing and invoking these hooks to the new manager. After this operation, this manager will no longer have any registered hooks, and all hooks will be managed by the new manager.
        /// </summary>
        /// <param name="newManager">The new manager for the hooks.</param>
        /// <returns>If a hook was triggered, otherwise false</returns>
        void TransferTo(IHookManager newManager);

        /// <summary>
        /// Gets a triggerer for event type <typeparamref name="T"/>, which can be used to trigger events of that type more efficiently than using the <see cref="IHookManager"/> directly.
        /// </summary>
        /// <typeparam name="T">The type of event that the triggerer will respond to.</typeparam>
        /// <returns>A triggerer for the specified event type.</returns>
        IHookTriggerer<T> GetTriggerer<T>();
    }

    /// <summary>
    /// Used to raise trigger hooks listening for a specific event type <typeparamref name="T"/>.
    /// More optimitzed than using <see cref="IHookManager"/> directly, but less flexible.
    /// </summary>
    /// <typeparam name="T">The type of event that the triggerer will respond to.</typeparam>
    public interface IHookTriggerer<T>
    {
        /// <summary>
        /// Triggers event <typeparamref name="T"/>, invoking all registered hooks that respond to this event type with the provided argument <paramref name="arg"/>.
        /// </summary>
        /// <typeparam name="T">The type of event to trigger.</typeparam>
        /// <param name="arg">The event being triggered.</param>
        /// <returns>If a hook was triggered, otherwise false</returns>
        bool Trigger(T arg);
        /// <summary>
        /// Triggers event <typeparamref name="T"/>, invoking all registered hooks that respond to this event type with the provided argument <paramref name="arg"/>.
        /// Delays the trigger to next tick.
        /// </summary>
        /// <typeparam name="T">The type of event to trigger.</typeparam>
        /// <param name="arg">The event being triggered.</param>
        /// <returns>If a hook was triggered, otherwise false</returns>
        void TriggerDelayed(T arg);
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
        /// <param name="gameScoped"><inheritdoc cref="IHook{T}.GameScoped"/></param>
        public static IHookManager RegisterHook<T>(this IHookManager hookManager, object owner, Action<T> action, bool once = false, Func<Exception, T, bool> errorHandler = null, byte priority = 128, bool gameScoped = false) where T : class
        {
            hookManager = Toolkit.Helpers.Guard.NotNull(hookManager, nameof(hookManager));
            hookManager.RegisterHook(new SimpleHook<T>(owner, action,once, errorHandler, priority, gameScoped));
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
