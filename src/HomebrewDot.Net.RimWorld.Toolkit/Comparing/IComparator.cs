using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Models;

namespace HomebrewDot.Net.Rimworld.Comparing
{
    /// <summary>
    /// Defines a contract for comparing conditions against a given context.
    /// </summary>
    public interface IComparator
    {
        /// <summary>
        /// Compares the specified condition against the provided context.
        /// </summary>
        /// <param name="condition">The condition to be evaluated.</param>
        /// <param name="context">A dictionary containing context information that might be needed for the comparison.</param>
        /// <returns>True if the condition is met based on the context; otherwise, false.</returns>
        bool Compare(object input, IConditionDef condition, IReadOnlyDictionary<string, object> context);
        /// <summary>
        /// Compares the specified conditions against the provided context.
        /// </summary>
        /// <param name="conditions">The conditions to be evaluated.</param>
        /// <param name="context">A dictionary containing context information that might be needed for the comparison.</param>
        /// <returns>True if the conditions are met based on the context; otherwise, false.</returns>
        bool Compare(object input, IReadOnlyList<IConditionDef> conditions, IReadOnlyDictionary<string, object> context);
    }

    /// <summary>
    /// Defines a contract for comparing conditions against a given context, with the ability to generate cache keys for efficient comparison results caching. Implementing this interface allows for the creation of comparators that can optimize their comparison logic by caching results based on unique cache keys generated from the input, condition, and context, enabling faster comparisons in scenarios where the same conditions are evaluated multiple times with similar inputs and contexts.
    /// </summary>
    public interface IComparatorCompiler : IComparator
    {
        /// <summary>
        /// Generates a unique cache key for the given input, condition, and context. This key can be used to store and retrieve comparison results efficiently.
        /// </summary>
        /// <param name="input">The input object to be compared.</param>
        /// <param name="condition">The condition to be evaluated.</param>
        /// <param name="context">A dictionary containing context information that might be needed for the comparison.</param>
        /// <returns>A unique string representing the cache key for the given input, condition, and context.</returns>
        string GetCacheKey(object input, IConditionDef condition, IReadOnlyDictionary<string, object> context);

        /// <summary>
        /// Compiles the specified condition into an expression that can be executed against the provided input and context. This method allows for the creation of optimized comparison logic that can be executed efficiently, potentially improving performance when evaluating conditions multiple times with similar inputs and contexts.
        /// </summary>
        /// <param name="inputParameter">The parameter expression representing the input object to be compared.</param>
        /// <param name="condition">The condition to be evaluated.</param>
        /// <param name="contextParameter">The parameter expression representing the context dictionary.</param>
        /// <param name="context">A dictionary containing context information that might be needed for the comparison.</param>
        /// <returns>An expression representing the compiled condition.</returns>
        Expression Compile(ParameterExpression inputParameter, IConditionDef condition, ParameterExpression contextParameter, IReadOnlyDictionary<string, object> context);
    }
}