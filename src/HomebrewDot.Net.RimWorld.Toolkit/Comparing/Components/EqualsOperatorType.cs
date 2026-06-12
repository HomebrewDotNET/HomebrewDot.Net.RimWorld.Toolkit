using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Template;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator that checks if the left and right values are equal.
    /// </summary>
    public class EqualsOperatorType : BaseComparableOperatorType
    {
        // Constants
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "Equals";
        /// <summary>
        /// The operator type that this class represents.
        /// </summary>
        public const NativeOperatorType Operator = NativeOperatorType.Equal;
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="EqualsOperatorType"/>, which can be used to reference this operator type without needing to create multiple instances.
        /// </summary>
        public static readonly EqualsOperatorType Instance = new EqualsOperatorType();
        /// <summary>
        /// The aliases for this operator type, which can be used to reference this operator type in definitions or code.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Aliases = new[] { "Equal", DefaultTypeName, Operator.ToOperatorString() };

        /// <inheritdoc cref="EqualsOperatorType"/>
        private EqualsOperatorType() : base(Operator, ComparableFunc, allowPositionSwap: true)
        {

        }
        /// <inheritdoc />
        private static bool ComparableFunc(IComparable comparable, object other)
        {
            return comparable.CompareTo(other) == 0;
        }
    }
}
