using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// A reference type that simply returns the value it is given.
    /// </summary>
    public class ValueReferenceType : IReferenceType
    {
        // Constants
        /// <summary>
        /// The default name for this reference type.
        /// </summary>
        public const string DefaultTypeName = "Value";
        // Statics
        /// <summary>
        /// The singleton instance of this reference type. Since it has no state, only one instance is needed.
        /// </summary>
        public static ValueReferenceType Instance { get; } = new ValueReferenceType();

        private ValueReferenceType()
        {
            
        }

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
            => value;
    }
}
