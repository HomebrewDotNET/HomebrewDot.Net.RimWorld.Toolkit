using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Referencing
{
    /// <summary>
    /// Resolves <see cref="IReference"/>s to their actual values or objects, using the appropriate <see cref="IReferenceTypeCompileable"/> for each reference. The resolver can use the information provided in the reference itself, as well as any additional context that might be needed for the resolution process, such as the current state of the database, metadata, or other relevant data.
    /// </summary>
    public interface IReferenceResolver
    {
        /// <summary>
        /// Tries to resolve the given reference using the appropriate reference type and the provided context. If the resolution is successful, it returns true and outputs the resolved value; otherwise, it returns false and the output value is null or default.
        /// </summary>
        /// <param name="input">The input object that might be needed for the resolution process. Can be null when resolving standalone values.</param>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="context">A dictionary containing additional information that might be needed for the resolution process.</param>
        /// <param name="result">The resolved value if the resolution is successful; otherwise, null or default.</param>
        /// <returns>True if the resolution is successful; otherwise, false.</returns>
        bool TryResolve(object input, IReference reference, IReadOnlyDictionary<string, object> context, out object result);
    }

    /// <summary>
    /// A <see cref="IReferenceResolver"/> that uses <see cref="IReferenceTypeCompileable"/>s to resolve references of specific types. This interface extends the basic reference resolver functionality by providing a method to retrieve the appropriate reference type for a given reference, allowing for more efficient resolution of references by directly accessing the relevant reference type without needing to search through a collection of reference types for each resolution operation.
    /// </summary>
    public interface IReferenceTypeResolver : IReferenceResolver
    {
        /// <summary>
        /// Gets the appropriate <see cref="IReferenceTypeCompileable"/> for the given reference, based on its type and the provided context. This method allows the resolver to quickly identify the correct reference type to use for resolving the reference, which can improve performance and efficiency when dealing with a large number of references or complex resolution logic. The implementation of this method should consider the reference's type, as well as any relevant information in the context, to determine the most suitable reference type for resolution.
        /// </summary>
        /// <param name="input">The input object that might be needed for the resolution process. Can be null when resolving standalone values.</param>
        /// <param name="reference">The reference for which to get the appropriate reference type.</param>
        /// <param name="context">A dictionary containing additional information that might be needed for the resolution process.</param>
        /// <returns>The appropriate <see cref="IReferenceType"/> for the given reference, or null if no suitable reference type is found.</returns>
        IReferenceType GetReferenceType(object input, IReference reference, IReadOnlyDictionary<string, object> context);
    }
}
