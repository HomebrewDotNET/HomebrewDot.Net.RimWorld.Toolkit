using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Template;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator that checks if the left value is less than the right value.
    /// </summary>
    public class LesserOperatorType : BaseComparableOperatorType
    {
        // Constants
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "LessThan";
        /// <summary>
        /// The operator type that this class represents.
        /// </summary>
        public const NativeOperatorType Operator = NativeOperatorType.LessThan;
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="LesserOperatorType"/>, which can be used to reference this operator type without needing to create multiple instances.
        /// </summary>
        public static readonly LesserOperatorType Instance = new LesserOperatorType();
        /// <summary>
        /// The aliases for this operator type, which can be used to reference this operator type in definitions or code.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Aliases = new[] { "Less", "Lesser", "LesserThan", DefaultTypeName, Operator.ToOperatorString() };

        /// <inheritdoc cref="LesserOperatorType"/>
        private LesserOperatorType() : base(Operator, ComparableFunc, allowPositionSwap: false)
        {

        }
        /// <inheritdoc />
        private static bool ComparableFunc(IComparable comparable, object other)
        {
            return comparable.CompareTo(other) < 0;
        }
    }
}
