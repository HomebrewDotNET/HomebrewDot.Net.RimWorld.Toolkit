using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Comparing
{
    /// <summary>
    /// Compares two objects based on provided arguments and global context.
    /// </summary>
    public interface IComparatorOperator
    {
        /// <summary>
        /// Compares the left and right objects using the provided arguments and context. The comparison logic can be defined based on the specific operator implementation, allowing for various types of comparisons (e.g., equality, greater than, less than, etc.) depending on the operator's purpose and the types of objects being compared.
        /// </summary>
        /// <param name="left">The left object to compare.</param>
        /// <param name="right">The right object to compare.</param>
        /// <param name="arguments">A dictionary containing additional arguments that might be needed for the comparison specific to the operator.</param>
        /// <param name="context">A dictionary containing global context information that might be needed for the comparison.</param>
        /// <returns>True if the comparison is successful based on the operator's logic; otherwise, false.</returns>
        bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context);
    }
}
