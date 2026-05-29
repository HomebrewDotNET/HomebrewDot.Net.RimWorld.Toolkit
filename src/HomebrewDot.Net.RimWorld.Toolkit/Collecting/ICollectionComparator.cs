using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Collecting.Models;

namespace HomebrewDot.Net.RimWorld.Collecting
{
    /// <summary>
    /// Defines a contract for comparing a collection definition against a given object and context to determine if they match.
    /// </summary>
    public interface ICollectionComparator
    {
        /// <summary>
        /// Determines if the specified collection definition matches the given object and context.
        /// </summary>
        /// <param name="collection">The collection definition to compare.</param>
        /// <param name="obj">The object to compare against the collection definition.</param>
        /// <param name="collections">A dictionary of all collection definitions, keyed by their names.</param>
        /// <param name="context">A dictionary representing the current context, which may contain additional information relevant to the comparison.</param>
        /// <returns>True if the collection definition matches the object and context; otherwise, false.</returns>
        bool Matches(ICollectionDef collection, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, Dictionary<string, object> context);
    }
}
