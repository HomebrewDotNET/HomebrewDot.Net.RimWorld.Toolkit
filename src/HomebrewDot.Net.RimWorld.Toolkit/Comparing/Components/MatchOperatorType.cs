using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator that compares the string version of the left value against the string version of the right value, where he right value is a regex.
    /// </summary>
    public class MatchOperatorType : IOperatorType
    {
        // Constants
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "Match";
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="MatchOperatorType"/>, which can be used to reference this operator type without needing to create multiple instances.
        /// </summary>
        public static readonly MatchOperatorType Instance = new MatchOperatorType();
        /// <summary>
        /// The aliases for this operator type, which can be used to reference this operator type in definitions or code.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Aliases = new[] { DefaultTypeName, "Matches", "Regex" };

        private MatchOperatorType()
        {
            
        }

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if(left == null || right == null)
                return false;

            var leftStr = left.ToString();
            var rightStr = right.ToString();

            if(Regex.IsMatch(leftStr, rightStr))
                return true;

            return false;
        }
    }
}
