using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Comparing.Template;

namespace HomebrewDot.Net.RimWorld.Comparing.Components
{
    /// <summary>
    /// Checks if the left operand is null.
    /// </summary>
    public class NullOperatorType : DelegateOperatorType
    {
        // Constants
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "Null";
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="NullOperatorType"/>, which can be used to reference this operator type without needing to create multiple instances.
        /// </summary>
        public static readonly NullOperatorType Instance = new NullOperatorType();
        /// <summary>
        /// The aliases for this operator type, which can be used to reference this operator type in definitions or code.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Aliases = new[] { DefaultTypeName, "IsNull", "Undefined", "None" };
        private NullOperatorType() : base((left, right, args, ctx) => left == null)
        {
            
        }
    }
}
