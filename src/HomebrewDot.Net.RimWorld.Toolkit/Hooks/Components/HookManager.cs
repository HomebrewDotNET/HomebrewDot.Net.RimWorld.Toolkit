using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using Ionic.Zlib;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;

namespace HomebrewDot.Net.Rimworld.Hooks
{
    /// <summary>
    /// Default implementation of <see cref="IHookManager"/>.
    /// </summary>
    public class HookManager : IHookManager, IHook<OnGameUnloadedTrigger>
    {
        // Fields
        private readonly Dictionary<Type, Triggerer> _triggerers = new Dictionary<Type, Triggerer>();
        private readonly Dictionary<object, HashSet<IHandler>> _owners = new Dictionary<object, HashSet<IHandler>>();

        // Properties
        /// <inheritdoc/>
        object IHook<OnGameUnloadedTrigger>.Owner => this;
        /// <inheritdoc/>
        bool IHook<OnGameUnloadedTrigger>.Once => false;
        /// <inheritdoc/>
        bool IHook<OnGameUnloadedTrigger>.GameScoped => false;
        /// <inheritdoc/>
        byte IHandler.Priority => byte.MinValue;

        /// <inheritdoc cref="HookManager"/>
        public HookManager()
        {
            RegisterHook<OnGameUnloadedTrigger>(this);
        }

        /// <inheritdoc/>
        public IHook<T>[] GetOwnerBy<T>(object owner)
        {
            owner = Guard.NotNull(owner, nameof(owner));

            IHandler[] handlers = Array.Empty<IHandler>();
            if (_owners.TryGetValue(owner, out var ownerHooks))
            {
                handlers = new IHandler[ownerHooks.Count];
                ownerHooks.CopyTo(handlers);
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
        public IHookTriggerer<T> GetTriggerer<T>()
        {
            if(!_triggerers.TryGetValue(typeof(T), out var triggerer))
            {
                triggerer = new Triggerer<T>(this);
                _triggerers.Add(typeof(T), triggerer);
            }
            return triggerer as IHookTriggerer<T>;
        }

        /// <inheritdoc/>
        public bool LazyTrigger<T>(Func<T> argFactory)
        {
            argFactory = Guard.NotNull(argFactory, nameof(argFactory));
            bool anyReplied = false;

            if(_triggerers.TryGetValue(typeof(T), out var triggerer) && triggerer is Triggerer<T> typedTriggerer)
            {
                var arg = argFactory();
                anyReplied = typedTriggerer.Trigger(arg);
            }

            return anyReplied;
        }
        /// <inheritdoc/>
        public void RegisterHook<T>(IHook<T> hook)
        {
            hook = Guard.NotNull(hook, nameof(hook));

            if(!_triggerers.TryGetValue(typeof(T), out var triggerer))
            {
                triggerer = new Triggerer<T>(this);
                _triggerers.Add(typeof(T), triggerer);
            }

            if (IsVerboseEnabled) LogVerbose($"Registering hook of type {typeof(T).FullName} owned by {hook.Owner} with priority {hook.Priority}");
            var owner = Guard.NotNull(hook.Owner, nameof(hook.Owner));
            HashSet<IHandler> ownerHooks;
            if (!_owners.TryGetValue(owner, out ownerHooks))
            {
                if (!_owners.TryGetValue(owner, out ownerHooks))
                {
                    ownerHooks = new HashSet<IHandler>();
                    _owners.Add(owner, ownerHooks);
                }
            }
            ownerHooks.Add(hook);
            triggerer.NotifyAdded(hook);
        }
        /// <inheritdoc/>
        public void TransferTo(IHookManager newManager)
        {
            newManager = Guard.NotNull(newManager, nameof(newManager));

            foreach (var kvp in _owners)
            {
                var owner = kvp.Key;
                var hooks = kvp.Value;
                foreach (var hook in hooks)
                {
                    if (hook is IHandler handler)
                    {
                        var registerMethod = typeof(IHookManager).GetMethod("RegisterHook").MakeGenericMethod(hook.GetType().GetGenericArguments()[0]);
                        registerMethod.Invoke(newManager, new object[] { hook });
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool Trigger<T>(T arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));

            if(_triggerers.TryGetValue(typeof(T), out var triggerer) && triggerer is Triggerer<T> typedTriggerer)
            {
                return typedTriggerer.Trigger(arg);
            }
            return false;
        }
        /// <inheritdoc/>
        public void TriggerDelayed<T>(T arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            
            if(_triggerers.TryGetValue(typeof(T), out var triggerer) && triggerer is Triggerer<T> typedTriggerer)
            {
                typedTriggerer.TriggerDelayed(arg);
            }
        }

        /// <inheritdoc/>
        public IHook<T>[] UnregisterAllBy<T>(object owner)
        {
            owner = Guard.NotNull(owner, nameof(owner));

            IHandler[] handlers = Array.Empty<IHandler>();
            if (_owners.TryGetValue(owner, out var ownerHooks))
            {
                handlers = new IHandler[ownerHooks.Count];
                ownerHooks.CopyTo(handlers);
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

            if(!_triggerers.TryGetValue(typeof(T), out var triggerer))
            {
                return;
            }
            triggerer.NotifyRemoved(hook);

            if (_owners.TryGetValue(owner, out var ownerHooks))
            {
                ownerHooks.Remove(hook);
            }
        }
        /// <inheritdoc/>
        bool IHook<OnGameUnloadedTrigger>.OnTrigger(OnGameUnloadedTrigger arg)
        {
            arg = Guard.NotNull(arg, nameof(arg));
            foreach(var triggerer in _triggerers.Values)
            {
                triggerer.NotifyGameEnded();
            }
            return true;
        }

        private class DelayedTrigger<T> : CooperativeWorkContext
        {
            public T arg;
        }

        private abstract class Triggerer
        {
            internal abstract void NotifyAdded<T>(IHook<T> hook);

            internal abstract void NotifyRemoved<T>(IHook<T> hook);

            internal abstract void NotifyGameEnded();
        }

        private class Triggerer<T> : Triggerer, IHookTriggerer<T>
        {
            private readonly HookManager _manager;

            // State
            internal HashSet<IHook<T>> _hooks = new HashSet<IHook<T>>();
            private IHook<T>[] _orderedHooks = Array.Empty<IHook<T>>();

            public Triggerer(HookManager manager)
            {
                _manager = Guard.NotNull(manager, nameof(manager));
            }
            public bool Trigger(T arg)
            {
                bool anyReplied = false;
                bool log = !(arg is OnGameTickTrigger tickTrigger && tickTrigger.TickerType == Verse.TickerType.Normal);

                if (_orderedHooks.Length > 0)
                {
                    anyReplied = TriggerHooks(arg, _orderedHooks, log);
                }

                return anyReplied;
            }

            public void TriggerDelayed(T arg)
            {
                var workContext = new DelayedTrigger<T>();
                workContext.arg = arg;
                var work = RaiseCooperativeWork.From<DelayedTrigger<T>>(() => TriggerHooks(workContext).GetEnumerator(), workContext);
                if (_manager.Trigger(work))
                {
                    return;
                }
                else
                {
                    Trigger(arg);
                }
            }

            private bool TriggerHooks(T arg, IReadOnlyList<IHook<T>> hooks, bool log)
            {
                if (log) LogVerbose($"Lazily triggering hooks of type {typeof(T).FullName}");
                bool anyReplied = false;
                List<IHook<T>> hooksToRemove = null;
                for (int i = 0; i < hooks.Count; i++)
                {
                    var hook = hooks[i];
                    if(TriggerHook(arg, hook, log))
                    {
                        if(hook.Once)
                        {
                            hooksToRemove ??= new List<IHook<T>>();
                            hooksToRemove.Add(hook);
                        }
                        anyReplied = true;
                    }
                }
                if(hooksToRemove != null && hooksToRemove.Count > 0)
                {
                    foreach (var hook in hooksToRemove)
                    {
                        _hooks.Remove(hook);
                    }
                    _orderedHooks = _hooks.OrderBy(h => h.Priority).ToArray();
                }
                return anyReplied;
            }
            private IEnumerable TriggerHooks(DelayedTrigger<T> arg)
            {
                bool log = !(arg is OnGameTickTrigger tickTrigger && tickTrigger.TickerType == Verse.TickerType.Normal);
                if (log) LogVerbose($"Lazily triggering hooks of type {typeof(T).FullName}");
                List<IHook<T>> hooksToRemove = null;
                for (int i = 0; i < _orderedHooks.Length; i++)
                {
                    var hook = _orderedHooks[i];
                    if(TriggerHook(arg.arg, hook, log))
                    {
                        if(hook.Once)
                        {
                            hooksToRemove ??= new List<IHook<T>>();
                            hooksToRemove.Add(hook);
                        }
                    }
                    if(arg.IsOverRunTime) yield return null;
                }
                if (hooksToRemove != null && hooksToRemove.Count > 0)
                {

                    foreach (var hook in hooksToRemove)
                    {
                        _hooks.Remove(hook);
                    }
                    _orderedHooks = _hooks.OrderBy(h => h.Priority).ToArray();
                }
            }
            private bool TriggerHook(T arg, IHook<T> hook, bool log)
            {
                if (log) LogVerbose($"Lazily triggering hook of type {typeof(T).FullName} owned by {hook.Owner} with priority {hook.Priority}");
                try
                {
                    var handled = hook.OnTrigger(arg);
                    return handled;
                }
                catch (Exception ex)
                {
                    LogError($"Exception occurred while triggering hook of type {typeof(T).FullName} owned by {hook.Owner}: {ex}");
                    return false;
                }
            }

            internal override void NotifyAdded<T1>(IHook<T1> hook)
            {
                if(hook is IHook<T> typedHook)
                {
                    if (_hooks.Add(typedHook))
                    {
                        _orderedHooks = _hooks.OrderBy(h => h.Priority).ToArray();
                    }
                }
            }

            internal override void NotifyRemoved<T1>(IHook<T1> hook)
            {
                if (hook is IHook<T> typedHook)
                {
                    _hooks.Remove(typedHook);
                    _orderedHooks = _hooks.OrderBy(h => h.Priority).ToArray();
                }
            }

            internal override void NotifyGameEnded()
            {
                var hooksToRemove = _hooks.Where(h => h.GameScoped).ToArray();
                foreach (var hook in hooksToRemove)
                {
                    _manager.UnregisterHook(hook);
                }
            }
        }
    }
}
