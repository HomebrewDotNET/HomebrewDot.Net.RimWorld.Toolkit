using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Template;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator that checks if the left operand evaluates to false.
    /// </summary>
    public class FalseOperatorType : BaseNativeOperatorType, IOperatorType
    {
        // Constants
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "False";
        /// <summary>
        /// The operator type that this class represents.
        /// </summary>
        public const NativeOperatorType Operator = NativeOperatorType.False;
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="FalseOperatorType"/>, which can be used to reference this operator type without needing to create multiple instances.
        /// </summary>
        public static readonly FalseOperatorType Instance = new FalseOperatorType();
        /// <summary>
        /// The aliases for this operator type, which can be used to reference this operator type in definitions or code.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Aliases = new[] { "False", "N", "No", DefaultTypeName, Operator.ToOperatorString() };
        private FalseOperatorType()
        {
            
        }

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            var matches = Compare(left, right, NativeOperatorType.True);
            if (matches is null)
            {
                if(left is bool leftBool)
                {
                    return !leftBool;
                }
                else if(right is bool rightBool)
                {
                    return !rightBool;
                }
                else if(left is int leftInt)
                {
                    return leftInt <= 0;
                }
                else if(right is int rightInt)
                {
                    return rightInt <= 0;
                }
            }
            else
            {
                return matches.Value;
            }

            return false;
        }

        /// <inheritdoc/>
        public System.Linq.Expressions.Expression Compile(System.Linq.Expressions.Expression leftValue, Type leftExpressionType, System.Linq.Expressions.Expression rightValue, Type rightExpressionType, ParameterExpression argumentsParameter, ParameterExpression contextParameter, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            _ = TryCompile(leftValue, rightValue, leftExpressionType, rightExpressionType, NativeOperatorType.False, false, out var compiled);
            return compiled;
        }
        /// <inheritdoc/>
        public string GetCacheKey(Type left, Type right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            return TryCompile(null, null, left, right, NativeOperatorType.False, true, out _) ? $"{GetType().FullName}:{left.FullName}:{right.FullName}" : null;
        }
    }
}
