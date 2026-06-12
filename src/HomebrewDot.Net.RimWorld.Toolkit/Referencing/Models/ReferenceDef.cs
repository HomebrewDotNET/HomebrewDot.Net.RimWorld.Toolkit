using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Referencing.Models
{
    /// <summary>
    /// Definition of a reference, which implements the <see cref="IReference"/> interface and can be used to represent a reference to an object or value that can be resolved at a later time using its type and value.
    /// </summary>
    public class ReferenceDef : IReference
    {
        /// <inheritdoc/>
        public string Type { get; set; }
        /// <inheritdoc/>
        public object Value { get; set; }
    }
}
