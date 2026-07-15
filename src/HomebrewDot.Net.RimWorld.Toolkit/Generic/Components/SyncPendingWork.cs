using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic.Components
{
    /// <inheritdoc cref="ISyncPendingWork{T}"/>
    public class SyncPendingWork<T> : ISyncPendingWork<T>, ISyncRunningWork<T>, IPoolable
    {
        // State
        /// <summary>
        /// Optional delegate that will be used to fetch stopwatch with the current elapsed time when running <see cref="Continue"/>.
        /// </summary>
        public Func<T, Stopwatch> trackerSelector;
        /// <summary>
        /// Optional delegate that will be used to fetch a timeout when running <see cref="Continue"/>.
        /// </summary>
        public Func<T, TimeSpan?> timeoutSelector;

        // State
        private bool _currentFinished = true;
        private readonly Stack<IEnumerator> _pendingWork = new Stack<IEnumerator>(8);

        // Properties
        /// <inheritdoc/>
        public T Context { get; private set; }
        /// <inheritdoc/>
        public bool IsFinished { get; private set; }
        /// <inheritdoc/>
        public bool IsStarted => Context != null;

        /// <inheritdoc cref="SyncPendingWork{T}"/>
        public SyncPendingWork()
        {
            
        }
        /// <inheritdoc cref="SyncPendingWork{T}"/>
        /// <param name="timeoutSelector"><inheritdoc cref="timeoutSelector"/></param>
        /// <param name="trackerSelector"><inheritdoc cref="trackerSelector"/></param>
        public SyncPendingWork(Func<T, TimeSpan?> timeoutSelector, Func<T, Stopwatch> trackerSelector = null)
        {
            this.timeoutSelector = timeoutSelector;
            this.trackerSelector = trackerSelector;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _currentFinished = true;
            _pendingWork.Clear();
            Context = default;
            IsFinished = false;
        }
        /// <inheritdoc/>
        public bool Continue()
        {
            if (_currentFinished) return true;
            var timeout = timeoutSelector?.Invoke(Context);
            var tracker = trackerSelector?.Invoke(Context);
            while(_pendingWork.Count > 0) {
                if (tracker is not null && timeout.HasValue && tracker.Elapsed >= timeout) {
                    return false;
                }

                var current = _pendingWork.Peek();
                if (current != null)
                {
                    if (current.MoveNext())
                    {
                        if (current.Current is IEnumerable nextWorkToStart)
                        {
                            _pendingWork.Push(nextWorkToStart.GetEnumerator());
                        }
                        else if (current.Current is IEnumerator nextWork)
                        {
                            _pendingWork.Push(nextWork);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        _ = _pendingWork.Pop();
                    }
                }
            }
            _currentFinished = _pendingWork.Count == 0;
            IsFinished = _currentFinished;
            return _currentFinished;
        }
        /// <inheritdoc/>
        public bool Start(IEnumerator work, T context)
        {
            if (!_currentFinished) throw new InvalidOperationException("A task was already started");
            Context = context;
            _currentFinished = false;
            IsFinished = false;
            _pendingWork.Push(work);

            return Continue();
        }
        /// <inheritdoc/>
        void IPoolable.Reset()
            => Clear();
    }
}
