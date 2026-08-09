using System;
using System.Collections;
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
    /// Operator type that checks if the left value is contained within the right value, which should be a collection or enumerable. This operator is useful for checking membership in a set of values.
    /// </summary>
    public class InOperatorType : BaseNativeOperatorType, IOperatorTypeCompileable
    {
        // Statics
        /// <summary>
        /// Key used in the arguments to overwrite the operator to use when checking for equality.
        /// </summary>
        public const string NativeOperatorTypeKey = "NativeOperator";
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "In";
        /// <summary>
        /// The singleton instance of the <see cref="InOperatorType"/>. This can be used wherever an instance of this operator type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static readonly InOperatorType Instance = new InOperatorType();

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if (right is null) return false;

            NativeOperatorType nativeOperator = NativeOperatorType.Equal;

            if (arguments?.TryGetValue(NativeOperatorTypeKey, out object nativeOperatorObj) == true && nativeOperatorObj is NativeOperatorType overwriteOperator)
            {
                nativeOperator = overwriteOperator;
            }

            if (right.TryEnumerate<object>(out var collection))
            {
                foreach (var item in collection)
                {
                    if (Compare(left, item, nativeOperator) ?? false)
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
            var loop = Toolkit.Helpers.Expression.CompileLoop(rightInputVariable, rightExpressionType, (current, currentType, loopBreak) =>
            {
                if (loopBreak != null)
                {
                    return Expression.IfThen(
                            Expression.IsTrue(base.TryCompile(inputVariable, current, leftExpressionType, currentType, nativeOperator, false, out var compiledExpression) ? compiledExpression : Expression.Constant(false)),
                            Expression.Block(Expression.Assign(resultVariable, Expression.Constant(true)), Expression.Break(loopBreak))
                           );
                }
                else
                {
                    return Expression.IfThen(
                            Expression.IsTrue(base.TryCompile(inputVariable, current, leftExpressionType, currentType, nativeOperator, false, out var compiledExpression) ? compiledExpression : Expression.Constant(false)),
                            Expression.Assign(resultVariable, Expression.Constant(true))
                           );
                }
            });

            var rightInputIsNotNull = rightExpressionType.IsValueType
                ? (Expression)Expression.Constant(true)
                : Expression.NotEqual(rightInputVariable, Expression.Constant(null, rightExpressionType));

            var block = Expression.Block(
                new[] { inputVariable, rightInputVariable, resultVariable },
                assignInput,
                assignRightInput,
                Expression.Assign(resultVariable, Expression.Constant(false)),
                Expression.IfThen(rightInputIsNotNull, loop),
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
