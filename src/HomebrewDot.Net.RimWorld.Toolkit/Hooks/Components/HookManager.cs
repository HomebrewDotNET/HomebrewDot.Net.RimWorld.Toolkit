using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Generic;
using Guard = HomebrewDot.Net.RimWorld.Toolkit.Helpers.Guard;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.RimWorld.Hooks
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
        public void LazyTrigger<T>(Func<T> argFactory)
        {
            argFactory = Guard.NotNull(argFactory, nameof(argFactory));
            IHandler[] handlers;
            if (_orderedHooks.TryGetValue(typeof(T), out handlers) && handlers.Length > 0)
            {
                T arg = argFactory();
                for (int i = 0; i < handlers.Length; i++)
                {
                    var handler = handlers[i];
                    if (handler is IHook<T> hook)
                    {
                        try
                        {
                            if (hook.OnTrigger(arg) && hook.Once)
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
        public void Trigger<T>(T arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));

            IHandler[] handlers;
            if (_orderedHooks.TryGetValue(typeof(T), out handlers))
            {
                for (int i = 0; i < handlers.Length; i++)
                {
                    var handler = handlers[i];
                    if (handler is IHook<T> hook)
                    {
                        try
                        {
                            if (hook.OnTrigger(arg) && hook.Once)
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
    }
}
