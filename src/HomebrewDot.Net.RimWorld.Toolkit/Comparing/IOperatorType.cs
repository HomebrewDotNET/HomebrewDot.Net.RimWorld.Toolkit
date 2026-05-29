using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Comparing
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
}
