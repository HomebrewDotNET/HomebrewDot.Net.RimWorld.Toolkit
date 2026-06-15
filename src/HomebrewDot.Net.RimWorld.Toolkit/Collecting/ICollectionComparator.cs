using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Models;

namespace HomebrewDot.Net.Rimworld.Collecting
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
        bool Matches(ICollectionDef collection, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context);
        /// <summary>
        /// Determines if the specified collection definition matches each of the given objects and context, returning a list of tuples containing the object and whether it matches or not.
        /// </summary>
        /// <param name="collection">The collection definition to compare.</param>
        /// <param name="objects">The objects to compare against the collection definition.</param>
        /// <param name="collections">A dictionary of all collection definitions, keyed by their names.</param>
        /// <param name="context">A dictionary representing the current context, which may contain additional information relevant to the comparison.</param>
        /// <returns>An enumerable of tuples where each tuple contains the object and a boolean indicating whether it matches the collection definition.</returns>
        IEnumerable<(object Object, bool Matches)> Matches(ICollectionDef collection, IEnumerable<object> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context);
    }
}
