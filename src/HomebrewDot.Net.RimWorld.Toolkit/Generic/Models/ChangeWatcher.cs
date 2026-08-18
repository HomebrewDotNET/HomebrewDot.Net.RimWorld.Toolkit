using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Generic.Models
{
    /// <summary>
    /// Monitors for changes on an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChangeWatcher<T> : IChangeWatcherBuilder<T>
    {
        // Fields
        private readonly Dictionary<Func<T, object>, object> _watchers = new Dictionary<Func<T, object>, object>();

        // State
        private T _instance;

        // Properties
        /// <summary>
        /// The current instance being monitored.
        /// </summary>
        public T Watched => _instance;
        /// <summary>
        /// True if the 
        /// </summary>
        public bool HasChanged => HasInstanceChanged(_instance, false);

        public ChangeWatcher(T instance, Action<IChangeWatcherBuilder<T>> configure)
        {
            configure = Guard.NotNull(configure, nameof(configure));
            configure(this);
            _ = Update(instance);
        }

        /// <inheritdoc/>
        IChangeWatcherBuilder<T> IChangeWatcherBuilder<T>.Monitor(Func<T, object> value)
        {
            value = Guard.NotNull(value, nameof(value));
            _watchers.Add(value, value);
            return this;
        }

        /// <summary>
        /// Updates <see cref="Watched"/> and returns if <paramref name="instance"/> has changed since the last update.
        /// </summary>
        /// <param name="instance">The new instance to monitor</param>
        /// <returns>True if <paramref name="instance"/> changed, otherwise false</returns>
        public bool Update(T instance)
        {
            _instance = Guard.NotNull(instance, nameof(instance));
            return HasInstanceChanged(instance, true);
        }

        private bool HasInstanceChanged(T instance, bool update)
        {
            bool anyChanged = false;
            foreach(var watcherValue in _watchers.ToArray())
            {
                var current = watcherValue.Key(instance);
                bool changed = current != watcherValue.Value;
                if (changed)
                {
                    anyChanged = true;
                }
                if(update) _watchers[watcherValue.Key] = current;
                else if(changed) return true;
            }
            return anyChanged;
        }
    }

    /// <summary>
    /// Fluent api for configuring <see cref="ChangeWatcher{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IChangeWatcherBuilder<T>
    {
        /// <summary>
        /// Watches for changes on the value returned by <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Delegate that selects the value on <typeparamref name="T"/> to watch for changes</param>
        /// <returns>Current builder for method chaining</returns>
        IChangeWatcherBuilder<T> Monitor(Func<T, object> value);
    }
}
