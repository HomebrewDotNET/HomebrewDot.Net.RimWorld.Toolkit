using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Referencing
{
    /// <summary>
    /// Resolves <see cref="IReference"/>s to their actual values or objects, using the appropriate <see cref="IReferenceType"/> for each reference. The resolver can use the information provided in the reference itself, as well as any additional context that might be needed for the resolution process, such as the current state of the database, metadata, or other relevant data.
    /// </summary>
    public interface IReferenceResolver
    {
        /// <summary>
        /// Tries to resolve the given reference using the appropriate reference type and the provided context. If the resolution is successful, it returns true and outputs the resolved value; otherwise, it returns false and the output value is null or default.
        /// </summary>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="context">A dictionary containing additional information that might be needed for the resolution process.</param>
        /// <param name="result">The resolved value if the resolution is successful; otherwise, null or default.</param>
        /// <returns>True if the resolution is successful; otherwise, false.</returns>
        bool TryResolve(IReference reference, IReadOnlyDictionary<string, object> context, out object result);
    }
}
