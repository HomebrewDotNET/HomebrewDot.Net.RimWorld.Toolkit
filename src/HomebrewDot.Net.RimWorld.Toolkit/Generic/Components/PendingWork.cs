using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Generic.Components
{
    /// <inheritdoc cref="IPendingWork{T}"/>
    public class PendingWork<T> : IPendingWork<T>
    {
        // State
        private TaskCompletionSource<bool> _waitHandle;

        // Properties
        /// <inheritdoc/>
        public Task Work { get; private set; }
        /// <inheritdoc/>
        public T Context { get; private set; }
        /// <inheritdoc/>
        public Task Continue()
        {
            if (_waitHandle is null) return Work;
            var currentHandle = _waitHandle;
            _waitHandle = null;
            currentHandle.SetResult(true);
            
            return Work;
        }
        /// <inheritdoc/>
        public Task Start(Func<Task> startWork, T context)
        {
            startWork = Guard.NotNull(startWork);
            Context = context;
            if (Work != null) throw new InvalidProgramException("Work was already started");
            Work = startWork();
            return Work;
        }
        /// <inheritdoc/>
        public Task Yield()
        {
            if(_waitHandle is not null && !_waitHandle.Task.IsCompleted)
            {
                Continue();
            }

            _waitHandle = new TaskCompletionSource<bool>();
            return _waitHandle.Task;
        }
    }
}
