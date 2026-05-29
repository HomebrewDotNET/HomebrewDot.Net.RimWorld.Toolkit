using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Referencing
{
    /// <summary>
    /// Represents a reference to an object or value that can be resolved at a later time using its type and value.
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// Gives an indication about the type of reference this is, which can be used to determine how to resolve it. 
        /// </summary>
        string Type { get; }
        /// <summary>
        /// The value of the reference, which can be used to resolve the actual object or value it refers to.
        /// </summary>
        object Value { get; }
    }
}
