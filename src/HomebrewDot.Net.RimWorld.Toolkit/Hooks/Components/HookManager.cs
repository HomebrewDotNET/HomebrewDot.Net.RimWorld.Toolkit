using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using System.Collections;

namespace HomebrewDot.Net.Rimworld.Hooks
{
    /// <summary>
    /// Default implementation of <see cref="IHookManager"/>.
    /// </summary>
    public class HookManager : IHookManager
    {
        // Fields
        private readonly Dictionary<Type, HashSet<IHandler>> _hooks = new Dictionary<Type, HashSet<IHandler>>();
        private readonly Dictionary<object, HashSet<IHandler>> _owners = new Dictionary<object, HashSet<IHandler>>(); 
        private readonly Dictionary<Type, IHandler[]> _orderedHooks = new Dictionary<Type, IHandler[]>();

        /// <inheritdoc/>
        public IHook<T>[] GetOwnerBy<T>(object owner)
        {
            owner = Guard.NotNull(owner, nameof(owner));

            IHandler[] handlers = Array.Empty<IHandler>();
            if (_owners.TryGetValue(owner, out var ownerHooks))
            {
                lock (ownerHooks)
                {
                    handlers = new IHandler[ownerHooks.Count];
                    ownerHooks.CopyTo(handlers);
                }
            }
            List<IHook<T>> hooks = new List<IHook<T>>();
            for (int i = 0; i < handlers.Length; i++)
            {
                var handler = handlers[i];
                if (handler is IHook<T> hook)
                {
                    hooks.Add(hook);
                }
            }
            return [.. hooks];
        }
        /// <inheritdoc/>
        public bool LazyTrigger<T>(Func<T> argFactory)
        {
            argFactory = Guard.NotNull(argFactory, nameof(argFactory));
            IHandler[] handlers;
            bool anyReplied = false;
            if (_orderedHooks.TryGetValue(typeof(T), out handlers) && handlers.Length > 0)
            {
                T arg = argFactory();
                bool log = !(arg is OnGameTickTrigger tickTrigger && tickTrigger.TickerType == Verse.TickerType.Normal);
                if(log) if (IsVerboseEnabled) LogVerbose($"Lazily triggering hooks of type {typeof(T).FullName}");
                for (int i = 0; i < handlers.Length; i++)
                {
                    var handler = handlers[i];
                    if (handler is IHook<T> hook)
                    {
                        if (log) if (IsVerboseEnabled) LogVerbose($"Lazily triggering hook of type {typeof(T).FullName} owned by {hook.Owner} with priority {hook.Priority}");
                        try
                        {
                            var handled = hook.OnTrigger(arg);
                            if (handled)
                            {
                                anyReplied = true;
                            }
                            if (handled && hook.Once)
                            {
                                UnregisterHook(hook);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError($"Exception occurred while triggering hook of type {typeof(T).FullName} owned by {hook.Owner}: {ex}");
                        }
                    }
                }
            }

            return anyReplied;
        }
        /// <inheritdoc/>
        public void RegisterHook<T>(IHook<T> hook)
        {
            hook = Guard.NotNull(hook, nameof(hook));

            HashSet<IHandler> hooks;
            if (!_hooks.TryGetValue(typeof(T), out hooks))
            {
                lock (_hooks) { 
                    if (!_hooks.TryGetValue(typeof(T), out hooks))
                    {
                        hooks = new HashSet<IHandler>();
                        _hooks.Add(typeof(T), hooks);
                    }
                }
            }

            lock (hooks)
            {
                if (IsVerboseEnabled) LogVerbose($"Registering hook of type {typeof(T).FullName} owned by {hook.Owner} with priority {hook.Priority}");
                var owner = Guard.NotNull(hook.Owner, nameof(hook.Owner));
                HashSet<IHandler> ownerHooks;
                if (!_owners.TryGetValue(owner, out ownerHooks))
                {
                    lock (_owners)
                    {
                        if (!_owners.TryGetValue(owner, out ownerHooks))
                        {
                            ownerHooks = new HashSet<IHandler>();
                            _owners.Add(owner, ownerHooks);
                        }
                    }
                }
                lock(ownerHooks) {
                    ownerHooks.Add(hook);
                }                 
                hooks.Add(hook);
                var orderedHooks = hooks.OrderBy(h => h.Priority).ToArray();
                if(!_orderedHooks.ContainsKey(typeof(T)))
                {
                    _orderedHooks.Add(typeof(T), orderedHooks);
                }
                else
                {
                    _orderedHooks[typeof(T)] = orderedHooks;
                }                 
            }
        }
        /// <inheritdoc/>
        public void TransferTo(IHookManager newManager)
        {
            newManager = Guard.NotNull(newManager, nameof(newManager));

            foreach (var kvp in _hooks)
            {
                var hookType = kvp.Key;
                var handlers = kvp.Value;
                foreach (var handler in handlers)
                {
                    if (handler is IHandler hook)
                    {
                        var registerMethod = typeof(IHookManager).GetMethod("RegisterHook").MakeGenericMethod(hookType);
                        registerMethod.Invoke(newManager, new object[] { hook });
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool Trigger<T>(T arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));

            IHandler[] handlers;

            bool anyReplied = false;
            if (_orderedHooks.TryGetValue(typeof(T), out handlers))
            {
                bool log = !(arg is OnGameTickTrigger tickTrigger && tickTrigger.TickerType == Verse.TickerType.Normal);
                if (log && IsVerboseEnabled) LogVerbose($"Triggering hooks of type {typeof(T).FullName} with argument: {arg}");
                for (int i = 0; i < handlers.Length; i++)
                {
                    var handler = handlers[i];
                    if (handler is IHook<T> hook)
                    {
                        if(log && IsVerboseEnabled) LogVerbose($"Triggering hook of type {typeof(T).FullName} owned by {hook.Owner} with priority {hook.Priority}");
                        try
                        {
                            var handled = hook.OnTrigger(arg);
                            if (handled)
                            {
                                anyReplied = true;
                            }
                            if (handled && hook.Once)
                            {
                                UnregisterHook(hook);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError($"Exception occurred while triggering hook of type {typeof(T).FullName} owned by {hook.Owner}: {ex}");
                        }
                    }
                }
            }
            return anyReplied;
        }
        /// <inheritdoc/>
        public void TriggerDelayed<T>(T arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            var workContext = new DelayedTrigger<T>();
            workContext.arg = arg;
            var work = RaiseCooperativeWork.From<DelayedTrigger<T>>(() => Trigger(workContext).GetEnumerator(), workContext);
            if (Trigger(work))
            {
                return;
            }
            else
            {
                Trigger(arg);
            }
        }

        /// <inheritdoc/>
        private IEnumerable Trigger<T>(DelayedTrigger<T> work)
        {
            var arg = Guard.NotNull(work.arg, nameof(work.arg));

            IHandler[] handlers;

            if (_orderedHooks.TryGetValue(typeof(T), out handlers))
            {
                bool log = !(arg is OnGameTickTrigger tickTrigger && tickTrigger.TickerType == Verse.TickerType.Normal);
                if (log && IsVerboseEnabled) LogVerbose($"Triggering hooks of type {typeof(T).FullName} with argument: {arg}");
                for (int i = 0; i < handlers.Length; i++)
                {
                    var handler = handlers[i];
                    if (handler is IHook<T> hook)
                    {
                        if (log && IsVerboseEnabled) LogVerbose($"Triggering hook of type {typeof(T).FullName} owned by {hook.Owner} with priority {hook.Priority}");
                        try
                        {
                            var handled = hook.OnTrigger(arg);
                            if (handled && hook.Once)
                            {
                                UnregisterHook(hook);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError($"Exception occurred while triggering hook of type {typeof(T).FullName} owned by {hook.Owner}: {ex}");
                        }
                    }
                    if (work.IsOverRunTime)
                    {
                        yield break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IHook<T>[] UnregisterAllBy<T>(object owner)
        {
            owner = Guard.NotNull(owner, nameof(owner));

            IHandler[] handlers = Array.Empty<IHandler>();
            if (_owners.TryGetValue(owner, out var ownerHooks))
            {
                lock (ownerHooks)
                {
                    handlers = new IHandler[ownerHooks.Count];
                    ownerHooks.CopyTo(handlers);
                }
            }
            List<IHook<T>> unregisteredHooks = new List<IHook<T>>();
            for (int i = 0; i < handlers.Length; i++)
            {
                var handler = handlers[i];
                if (handler is IHook<T> hook)
                {
                    UnregisterHook(hook);
                    unregisteredHooks.Add(hook);
                }
            }
            return [.. unregisteredHooks];
        }
        /// <inheritdoc/>
        public void UnregisterHook<T>(IHook<T> hook)
        {
            hook = Guard.NotNull(hook, nameof(hook));

            var owner = Guard.NotNull(hook.Owner, nameof(hook.Owner));

            HashSet<IHandler> hooks;
            if (_hooks.TryGetValue(typeof(T), out hooks))
            {
                lock (hooks)
                {
                    if (IsVerboseEnabled) LogVerbose($"Unregistering hook of type {typeof(T).FullName} owned by {hook.Owner} with priority {hook.Priority}");
                    hooks.Remove(hook);

                    HashSet<IHandler> ownerHooks;
                    if (_owners.TryGetValue(owner, out ownerHooks))
                    {
                        lock (ownerHooks)
                        {
                            ownerHooks.Remove(hook);
                        }
                    }
                }
                var orderedHooks = hooks.OrderBy(h => h.Priority).ToArray();
                if(!_orderedHooks.ContainsKey(typeof(T)))
                {
                    _orderedHooks.Add(typeof(T), orderedHooks);
                }
                else
                {
                    _orderedHooks[typeof(T)] = orderedHooks;
                }
            }
        }

        private class DelayedTrigger<T> : CooperativeWorkContext
        {
            public T arg;
        }
    }
}
