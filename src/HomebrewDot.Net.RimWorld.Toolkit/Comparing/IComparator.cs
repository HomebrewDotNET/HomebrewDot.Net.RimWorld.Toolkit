using System;
using System.Collections.Generic;
using System.Linq;
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
}