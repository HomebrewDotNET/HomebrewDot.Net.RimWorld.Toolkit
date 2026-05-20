using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Eventing.Models
{
    /// <summary>
    /// Allows for subscribing to and invoking callbacks with arguments of type <typeparamref name="TArgs"/>.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments passed to the callback.</typeparam>
    public class Callback<TArgs>
    {
        // Fields
        private event Action<TArgs> _callback;

        // Properties
        /// <summary>
        /// If there are any subscribers to this callback.
        /// </summary>
        public bool HasSubscribers => _callback != null;
        /// <summary>
        /// How many subscribers are currently subscribed to this callback.
        /// </summary>
        public int SubscriberCount => _callback?.GetInvocationList().Length ?? 0;
        /// <summary>
        /// Used to subscribe to or unsubscribe from this callback. When invoked, all subscribed actions will be called with the provided arguments.
        /// </summary>
        public event Action<TArgs> Action
        {
            add
            {
                _callback += value;
            }
            remove
            {
                _callback -= value;
            }
        }

        /// <summary>
        /// Invoke this callback with the provided arguments, calling all subscribed actions.
        /// </summary>
        /// <param name="args">The arguments to pass to the subscribed actions.</param>
        public void Invoke(TArgs args)
        {
            _callback?.Invoke(args);
        }
    }

    /// <summary>
    /// Allows for subscribing to and invoking callbacks with arguments of types <typeparamref name="TArgs1"/> and <typeparamref name="TArgs2"/>.
    /// </summary>
    /// <typeparam name="TArgs1">The type of the first argument passed to the callback.</typeparam>
    /// <typeparam name="TArgs2">The type of the second argument passed to the callback.</typeparam>
    public class Callback<TArgs1, TArgs2>
    {
        // Fields
        private event Action<TArgs1, TArgs2> _callback;

        // Properties
        /// <summary>
        /// If there are any subscribers to this callback.
        /// </summary>
        public bool HasSubscribers => _callback != null;
        /// <summary>
        /// How many subscribers are currently subscribed to this callback.
        /// </summary>
        public int SubscriberCount => _callback?.GetInvocationList().Length ?? 0;
        /// <summary>
        /// Used to subscribe to or unsubscribe from this callback. When invoked, all subscribed actions will be called with the provided arguments.
        /// </summary>
        public event Action<TArgs1, TArgs2> Action
        {
            add
            {
                if (_callback == null)
                {
                    _callback = value;
                }
                else
                {
                    _callback += value;
                }
            }
            remove
            {
                if( _callback == value || _callback == null)
                {
                    _callback = null;
                }
                else
                {
                    _callback -= value;
                }
            }
        }

        /// <summary>
        /// Invoke this callback with the provided arguments, calling all subscribed actions.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the subscribed actions.</param>
        /// <param name="arg2">The second argument to pass to the subscribed actions.</param>
        public void Invoke(TArgs1 arg1, TArgs2 arg2)
        {
            _callback?.Invoke(arg1, arg2);
        }
    }

    /// <summary>
    /// Allows for subscribing to and invoking callbacks with arguments of types <typeparamref name="TArgs1"/>, <typeparamref name="TArgs2"/>, and <typeparamref name="TArgs3"/>.
    /// </summary>
    /// <typeparam name="TArgs1">The type of the first argument passed to the callback.</typeparam>
    /// <typeparam name="TArgs2">The type of the second argument passed to the callback.</typeparam>
    /// <typeparam name="TArgs3">The type of the third argument passed to the callback.</typeparam>
    public class Callback<TArgs1, TArgs2, TArgs3>
    {
        // Fields
        private event Action<TArgs1, TArgs2, TArgs3> _callback;
        // Properties
        /// <summary>
        /// If there are any subscribers to this callback.
        /// </summary>
        public bool HasSubscribers => _callback != null;
        /// <summary>
        /// How many subscribers are currently subscribed to this callback.
        /// </summary>
        public int SubscriberCount => _callback?.GetInvocationList().Length ?? 0;
        /// <summary>
        /// Used to subscribe to or unsubscribe from this callback. When invoked, all subscribed actions will be called with the provided arguments.
        /// </summary>
        public event Action<TArgs1, TArgs2, TArgs3> Action
        {
            add
            {
                _callback += value;
            }
            remove
            {
                _callback -= value;
            }
        }
        /// <summary>
        /// Invoke this callback with the provided arguments, calling all subscribed actions.
        /// </summary>
        /// <param name="arg1">The first argument to pass to the subscribed actions.</param>
        /// <param name="arg2">The second argument to pass to the subscribed actions.</param>
        /// <param name="arg3">The third argument to pass to the subscribed actions.</param>
        public void Invoke(TArgs1 arg1, TArgs2 arg2, TArgs3 arg3)
        {
            _callback?.Invoke(arg1, arg2, arg3);
        }
    }
}
