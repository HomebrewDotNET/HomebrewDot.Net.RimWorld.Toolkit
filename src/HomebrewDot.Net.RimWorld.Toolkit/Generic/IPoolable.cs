using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic
{
    /// <summary>
    /// Reresents an objects that can be managed as in a pool of objects.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when returned to the pool. Used to reset state.
        /// </summary>
        void Reset();
    }
}
