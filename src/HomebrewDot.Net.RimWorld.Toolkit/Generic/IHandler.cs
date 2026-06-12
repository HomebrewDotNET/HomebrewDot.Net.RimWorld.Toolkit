using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic
{
    /// <summary>
    /// Base interface with common functionality for handlers that manage the lifecycle of components, such as indexes or hooks.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// The priority of this handler. Handlers with higher priority will be executed before handlers with lower priority.
        /// </summary>
        byte Priority { get; }
    }
}
