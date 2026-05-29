using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Referencing
{
    /// <summary>
    /// Is able to resolve <see cref="IReference"/>s of a specific type to their actual values or objects.
    /// </summary>
    public interface IReferenceType
    {
        /// <summary>
        /// Resolves <paramref name="value"/> to the actual object or value it refers to, using the provided <paramref name="context"/> if necessary. The context can contain any additional information that might be needed for the resolution process, such as the current state of the database, metadata, or other relevant data.
        /// </summary>
        /// <param name="value">The value to resolve.</param>
        /// <param name="context">A dictionary containing additional information that might be needed for the resolution process.</param>
        /// <returns>The resolved object or value.</returns>
        object Resolve(object value, IReadOnlyDictionary<string, object> context);
    }
}
