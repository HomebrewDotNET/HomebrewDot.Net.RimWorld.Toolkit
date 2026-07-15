using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Extensions;

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

        /// <inheritdoc/>
        public string GetCacheKey()
        {
            var sb = new StringBuilder();
            sb.Append('<').Append(Type).Append(" -> ");
            Value.ToCacheKey(sb, true);
            sb.Append('>');
            return sb.ToString();
        }
    }
}
