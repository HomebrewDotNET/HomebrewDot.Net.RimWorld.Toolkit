using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic
{
    /// <summary>
    /// <see cref="ISyncWork"/> that was started.
    /// </summary>
    /// <typeparam name="T">The type of context used by the work</typeparam>
    public interface ISyncRunningWork<out T>: ISyncWork
    {
        /// <summary>
        /// The context the work was started with
        /// </summary>
        T Context { get; }
    }

    /// <summary>
    /// <see cref="ISyncWork"/> that still needs to be started.
    /// </summary>
    /// <typeparam name="T">The type of processing context the work accepts</typeparam>
    public interface ISyncPendingWork<in T> : ISyncWork
    {
        /// <summary>
        /// If the current work was already started.
        /// </summary>
        public bool IsStarted { get; }

        /// <summary>
        /// Starts a new task. 
        /// </summary>
        /// <param name="work">The enumerator used to track work</param>
        /// <param name="context">Context for the task</param>
        /// <returns>True if the work was already completed, otherwise false</returns>
        bool Start(IEnumerator work, T context);

        /// <summary>
        /// Resets the current work so it can be started again.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Arbitrary work that can be run across multiple calls.
    /// </summary>
    public interface ISyncWork
    {
        /// <summary>
        /// If the current work was finished.
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Contiunes the task if it wasn't finished previously.
        /// </summary>
        /// <returns>True if the work was already completed, otherwise false</returns>
        bool Continue();
    }
}
