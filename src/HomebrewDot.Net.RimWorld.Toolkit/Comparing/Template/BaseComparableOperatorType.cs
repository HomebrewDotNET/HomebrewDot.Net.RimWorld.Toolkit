using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Comparing.Template
{
    /// <summary>
    /// Base class for operator types using combination of native operator and fallback on IComparable.
    /// </summary>
    public abstract class BaseComparableOperatorType : BaseNativeOperatorType, IOperatorTypeCompileable
    {
        // Fields
        private readonly NativeOperatorType _operatorType;
        private readonly Func<IComparable, object, bool> _comparableFunc;
        private readonly bool _allowPositionSwap;

        /// <inheritdoc cref="BaseComparableOperatorType"/>
        /// <param name="operatorType">The native operator type to use for comparison.</param>
        /// <param name="comparableFunc">The function to use for comparing IComparable objects.</param>
        /// <param name="allowPositionSwap">Whether to allow swapping the positions of the operands if the left side is null but not the right</param>
        protected BaseComparableOperatorType(NativeOperatorType operatorType, Func<IComparable, object, bool> comparableFunc, bool allowPositionSwap = false)
        {
            _operatorType = operatorType;
            _comparableFunc = Guard.NotNull(comparableFunc, nameof(comparableFunc));
            _allowPositionSwap = allowPositionSwap;
        }
        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if(left is null && right is not null && _allowPositionSwap)
            {
                var temp = left;
                left = right;
                right = temp;
            }

            var isMatched = Compare(left, right, _operatorType);
            if(!isMatched.HasValue && left is IComparable comparable)
            {
                isMatched = _comparableFunc(comparable, right);
            }
            return isMatched ?? false;
        }
        /// <inheritdoc/>
        public System.Linq.Expressions.Expression Compile(System.Linq.Expressions.Expression leftValue, Type leftExpressionType, System.Linq.Expressions.Expression rightValue, Type rightExpressionType, ParameterExpression argumentsParameter, ParameterExpression contextParameter, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            _ = TryCompile(leftValue, rightValue, leftExpressionType, rightExpressionType, _operatorType, false, out var compiled);
            return compiled;
        }
        /// <inheritdoc/>
        public string GetCacheKey(Type left, Type right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            return TryCompile(null, null, left, right, _operatorType, true, out _) ? $"{GetType().FullName}:{left.FullName}:{right.FullName}" : null;
        }
    }
}
