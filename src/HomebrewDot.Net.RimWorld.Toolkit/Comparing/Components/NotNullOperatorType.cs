using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Template;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Checks if the left operand is not null.
    /// </summary>
    public class NotNullOperatorType : DelegateOperatorType
    {
        // Constants
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "NotNull";
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="NotNullOperatorType"/>, which can be used to reference this operator type without needing to create multiple instances.
        /// </summary>
        public static readonly NotNullOperatorType Instance = new NotNullOperatorType();
        /// <summary>
        /// The aliases for this operator type, which can be used to reference this operator type in definitions or code.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Aliases = new[] { DefaultTypeName, "IsNotNull", "Defined", "Any" };
        private NotNullOperatorType() : base((left, right, args, ctx) => left != null)
        {
            
        }
    }
}
