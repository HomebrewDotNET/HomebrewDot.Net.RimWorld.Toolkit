using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic
{
    /// <summary>
    /// Unit of work for spreading out work across multiple ticks by (mis)using TCS
    /// </summary>
    /// <typeparam name="T">The context for the work</typeparam>
    public interface IPendingWork<T>
    {
        /// <summary>
        /// The task with the work progress when started.
        /// </summary>
        Task Work { get; }
        /// <summary>
        /// The context for the work.
        /// </summary>
        T Context { get; }

        /// <summary>
        /// Starts the new task
        /// </summary>
        /// <param name="startWork"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        Task Start(Func<Task> startWork, T context);

        /// <summary>
        /// Continues the work method until <see cref="Yield"/> is called again.
        /// </summary>
        /// <returns><see cref="Work"/></returns>
        Task Continue();
        /// <summary>
        /// Stops the current method from working and allows caller to continue afterwards.
        /// </summary>
        /// <returns>Task that work method should await</returns>
        Task Yield();
    }
}
