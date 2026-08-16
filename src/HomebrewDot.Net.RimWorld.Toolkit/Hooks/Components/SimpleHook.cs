using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;

namespace HomebrewDot.Net.Rimworld.Hooks
{
    /// <summary>
    /// Simple hook that uses delegates to trigger an action when a specific event occurs and contains basic error handling. This is intended for simple hooks that don't require complex state management or multiple triggers.
    /// </summary>
    /// <typeparam name="T">The type of argument that the hook will receive when invoked.</typeparam>
    public class SimpleHook<T> : IHook<T>
        where T : class
    {
        // Fields
        private readonly Func<T,bool> _action;
        private readonly Func<Exception, T, bool> _errorHandler;

        // Properties
        /// <inheritdoc/>
        public object Owner { get; }
        /// <inheritdoc/>
        public bool Once { get; }

        /// <inheritdoc/>
        public byte Priority { get; }
        /// <inheritdoc/>
        public bool GameScoped => false;

        /// <inheritdoc cref="SimpleHook{T}"/>
        /// <param name="owner"><see cref="Owner"/></param>
        /// <param name="action">Delegate to be invoked when the hook is triggered.</param>
        /// <param name="once"><see cref="Once"/></param>
        /// <param name="errorHandler">Delegate to handle any exceptions that occur during the hook's execution.</param>
        public SimpleHook(object owner, Action<T> action, bool once = false, Func<Exception, T, bool> errorHandler = null, byte priority = 128) : this(owner, WrapAction(action), once, errorHandler, priority)
        {
        }
        private static Func<T, bool> WrapAction(Action<T> action)
        {
            Guard.NotNull(action, nameof(action));
            return arg => { action(arg); return true; };
        }

        /// <inheritdoc cref="SimpleHook{T}"/>
        /// <param name="owner"><see cref="Owner"/></param>
        /// <param name="action">Delegate to be invoked when the hook is triggered.</param>
        /// <param name="once"><see cref="Once"/></param>
        /// <param name="errorHandler">Delegate to handle any exceptions that occur during the hook's execution.</param>
        public SimpleHook(object owner, Func<T,bool> action, bool once = false, Func<Exception, T, bool> errorHandler = null, byte priority = 128)
        {
            Owner = Guard.NotNull(owner, nameof(owner));
            _action = Guard.NotNull(action, nameof(action));
            Once = once;
            _errorHandler = errorHandler;
            Priority = priority;
        }

        /// <inheritdoc/>
        public bool OnTrigger(T arg)
        {
            try
            {
                return _action(arg);
            }
            catch (Exception ex)
            {
                if (_errorHandler != null)
                {
                    return _errorHandler(ex, arg);
                }
                else
                {
                    // If no error handler is provided, rethrow the exception
                    throw;
                }
            }
        }
    }
}
