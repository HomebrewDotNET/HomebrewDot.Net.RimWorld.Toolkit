using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Referencing
{
    /// <summary>
    /// Is able to resolve <see cref="IReference"/>s of a specific type to their actual values or objects.
    /// </summary>
    public interface IReferenceType
    {
        /// <summary>
        /// Resolves <paramref name="value"/> to the actual object or value it refers to, using the provided <paramref name="context"/> if necessary. The context can contain any additional information that might be needed for the resolution process, such as the current state of the database, metadata, or other relevant data.
        /// </summary>
        /// <param name="input">The input object that might be needed for the resolution process. Can be null when resolving standalone values.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="context">A dictionary containing additional information that might be needed for the resolution process.</param>
        /// <returns>The resolved object or value.</returns>
        object Resolve(object input, object value, IReadOnlyDictionary<string, object> context);
    }

    /// <summary>
    /// Is able to resolve <see cref="IReference"/>s of a specific type to their actual values or objects, and can be compiled into a delegate for more efficient resolution. Implementing this interface allows for the creation of reference types that can be optimized for performance by compiling the resolution logic into a delegate, which can be invoked directly without the overhead of reflection or other dynamic resolution methods.
    /// </summary>
    public interface IReferenceTypeCompileable : IReferenceType
    {
        /// <summary>
        /// Gets the cache key for the reference type. This key should uniquely identify the reference type in the context of caching, allowing it to be stored and retrieved efficiently. The cache key can be used to store compiled delegates or other optimized resolution logic associated with this reference type, enabling faster resolution of references of this type in the future.
        /// </summary>
        /// <param name="input">The input object that might be needed for the resolution process. Can be null when resolving standalone values.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="context">A dictionary containing additional information that might be needed for the resolution process.</param>
        /// <param name="returnType">The type of the resolved value when compiled into a delegate.</param>
        /// <returns>The cache key for the reference type, or null if the reference type cannot be cached with the given arguments.</returns>
        string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType);

        /// <summary>
        /// Compiles the resolution logic for the reference type into an expression tree, which can then be compiled into a delegate for efficient execution. The expression tree should represent the logic needed to resolve the reference based on the provided parameters, and can utilize the context for any necessary information during the resolution process. This method allows for the creation of optimized delegates that can be invoked directly, improving performance when resolving references of this type repeatedly.
        /// </summary>
        /// <param name="inputParameter">The parameter representing the input object in the expression.</param>
        /// <param name="contextParameter">The parameter representing the context dictionary in the expression.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="context">A dictionary containing additional information that might be needed for the resolution process.</param>
        /// <returns>An expression representing the compiled resolution logic.</returns>
        Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context);
    }
}
