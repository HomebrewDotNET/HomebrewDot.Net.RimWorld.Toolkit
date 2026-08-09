using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using HomebrewDot.Net.Rimworld.Extensions;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Expression = System.Linq.Expressions.Expression;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator type that checks if a collection contains a specific element. This operator is used in comparison operations to determine if a given collection includes a specified value, based on the provided arguments and comparison logic.
    /// </summary>
    public class ContainsOperatorType : BaseNativeOperatorType, IOperatorTypeCompileable
    {
        // Statics
        /// <summary>
        /// Key used in the arguments to overwrite the operator to use when checking for equality.
        /// </summary>
        public const string NativeOperatorTypeKey = "NativeOperator";
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "Contains";
        /// <summary>
        /// The singleton instance of the <see cref="ContainsOperatorType"/>. This can be used wherever an instance of this operator type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static readonly ContainsOperatorType Instance = new ContainsOperatorType();

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if (left is null) return false;

            NativeOperatorType nativeOperator = NativeOperatorType.Equal;

            if (arguments?.TryGetValue(NativeOperatorTypeKey, out object nativeOperatorObj) == true && nativeOperatorObj is NativeOperatorType overwriteOperator)
            {
                nativeOperator = overwriteOperator;
            }

            if (left.TryEnumerate<object>(out var collection))
            {
                foreach (var item in collection)
                {
                    if (Compare(item, right, nativeOperator) ?? false)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (Compare(left, right, nativeOperator) ?? false)
                {
                    return true;
                }
            }

            return false;
        }
        /// <inheritdoc/>
        public Expression Compile(Expression leftValue, Type leftExpressionType, Expression rightValue, Type rightExpressionType, ParameterExpression argumentsParameter, ParameterExpression contextParameter, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            leftValue = Guard.NotNull(leftValue, nameof(leftValue));
            rightValue = Guard.NotNull(rightValue, nameof(rightValue));
            leftExpressionType = Guard.NotNull(leftExpressionType, nameof(leftExpressionType));
            rightExpressionType = Guard.NotNull(rightExpressionType, nameof(rightExpressionType));

            var nativeOperator = NativeOperatorType.Equal;
            if (arguments?.TryGetValue(NativeOperatorTypeKey, out object nativeOperatorObj) == true && nativeOperatorObj is NativeOperatorType overwriteOperator)
            {
                nativeOperator = overwriteOperator;
            }

            var inputVariable = Expression.Variable(leftExpressionType, "input");
            var assignInput = Expression.Assign(inputVariable, leftValue);
            var rightInputVariable = Expression.Variable(rightExpressionType, "rightInput");
            var assignRightInput = Expression.Assign(rightInputVariable, rightValue);
            var resultVariable = Expression.Variable(typeof(bool), "result");
            var loop = Toolkit.Helpers.Expression.CompileLoop(inputVariable, leftExpressionType, (current, currentType, loopBreak) =>
            {
                if (loopBreak != null)
                {
                    return Expression.IfThen(
                            Expression.IsTrue(base.TryCompile(current, rightInputVariable, currentType, rightExpressionType, nativeOperator, false, out var compiledExpression) ? compiledExpression : Expression.Constant(false)),
                            Expression.Block(Expression.Assign(resultVariable, Expression.Constant(true)), Expression.Break(loopBreak))
                           );
                }
                else
                {
                    return Expression.IfThen(
                            Expression.IsTrue(base.TryCompile(current, rightInputVariable, currentType, rightExpressionType, nativeOperator, false, out var compiledExpression) ? compiledExpression : Expression.Constant(false)),
                            Expression.Assign(resultVariable, Expression.Constant(true))
                           );
                }
            });

            var inputIsNotNull = leftExpressionType.IsValueType
                ? (Expression)Expression.Constant(true)
                : Expression.NotEqual(inputVariable, Expression.Constant(null, leftExpressionType));

            var block = Expression.Block(
                new[] { inputVariable, rightInputVariable, resultVariable },
                assignInput,
                assignRightInput,
                Expression.Assign(resultVariable, Expression.Constant(false)),
                Expression.IfThen(inputIsNotNull, loop),
                resultVariable
            );
            return block;
        }
        /// <inheritdoc/>
        public string GetCacheKey(Type left, Type right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            var nativeOperator = NativeOperatorType.Equal;
            if (arguments?.TryGetValue(NativeOperatorTypeKey, out object nativeOperatorObj) == true && nativeOperatorObj is NativeOperatorType overwriteOperator)
            {
                nativeOperator = overwriteOperator;
            }
            return $"{GetType().FullName}:{left.FullName}:{right.FullName}:{nativeOperator}";
        }
    }
}
