using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Comparing
{
    /// <summary>
    /// Compares 2 values based on the provided arguments and global context. The operator type can be used to determine the specific comparison logic that should be applied when comparing the left and right values, allowing for various types of comparisons (e.g., equality, greater than, less than, etc.) depending on the operator's purpose and the types of objects being compared.
    /// </summary>
    public interface IOperatorType
    {
        /// <summary>
        /// Compares the left and right objects using the provided arguments and context. The comparison logic can be defined based on the specific operator type implementation, allowing for various types of comparisons (e.g., equality, greater than, less than, etc.) depending on the operator's purpose and the types of objects being compared.
        /// </summary>
        /// <param name="left">The left object to compare.</param>
        /// <param name="right">The right object to compare.</param>
        /// <param name="arguments">A dictionary of arguments that may influence the comparison logic.</param>
        /// <param name="context">A dictionary representing the global context for the comparison.</param>
        /// <returns>True if the comparison is successful based on the operator type logic; otherwise, false.</returns>
        bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context);
    }
    /// <summary>
    /// Compares 2 values based on the provided arguments and global context, and can be compiled into a delegate for more efficient comparisons. Implementing this interface allows for the creation of operator types that can be optimized for performance by compiling the comparison logic into a delegate, which can be invoked directly without the overhead of reflection or other dynamic comparison methods.
    /// </summary>
    public interface IOperatorTypeCompileable : IOperatorType
    {
        /// <summary>
        /// Gets the cache key for the operator type based on the provided left and right objects, arguments, and context. This key should uniquely identify the specific comparison logic that would be applied for these inputs, allowing it to be stored and retrieved efficiently from a cache. The cache key can be used to store compiled delegates or other optimized comparison logic associated with this operator type, enabling faster comparisons in the future when the same inputs are encountered again.
        /// </summary>
        /// <param name="left">The type of the left object to compare.</param>
        /// <param name="right">The type of the right object to compare.</param>
        /// <param name="arguments">A dictionary of arguments that may influence the comparison logic.</param>
        /// <param name="context">A dictionary representing the global context for the comparison.</param>
        /// <returns>The cache key for the operator type, or null if the operator type cannot be cached with the given inputs.</returns>
        string GetCacheKey(Type left, Type right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context);

        /// <summary>
        /// Compiles the comparison logic for the operator type into an expression tree, which can then be compiled into a delegate for efficient execution. The expression tree should represent the logic needed to compare the left and right values based on the provided parameters, and can utilize the arguments and context for any necessary information during the comparison process. This method allows for the creation of optimized delegates that can be invoked directly, improving performance when comparing values of this type repeatedly. The compiled expression should return a boolean value indicating the result of the comparison based on the operator type logic.
        /// </summary>
        /// <param name="leftValue">Expression pointing to the left value to compare</param>
        /// <param name="leftExpressionType">The type of the left expression</param>
        /// <param name="rightValue">Expression pointing to the right value to compare</param>
        /// <param name="rightExpressionType">The type of the right expression</param>
        /// <param name="argumentsParameter">Parameter expression representing the arguments dictionary</param>
        /// <param name="contextParameter">Parameter expression representing the context dictionary</param>
        /// <param name="arguments">A dictionary of arguments that may influence the comparison logic.</param>
        /// <param name="context">A dictionary representing the global context for the comparison.</param>
        /// <returns>An expression representing the compiled comparison logic.</returns>
        Expression Compile(Expression leftValue, Type leftExpressionType, Expression rightValue, Type rightExpressionType, ParameterExpression argumentsParameter, ParameterExpression contextParameter, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context);
    }
}
