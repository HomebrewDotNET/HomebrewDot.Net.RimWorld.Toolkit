using System ;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic;

namespace HomebrewDot.Net.Rimworld.Comparing
{
    /// <summary>
    /// Defines how to compare 2 objects using a specific operator and arguments. The <see cref="Type"/> property indicates the kind of comparison to perform, while the <see cref="Arguments"/> property provides any additional information needed for the comparison specific to that operator. This interface can be implemented by various classes that define specific operators and their associated arguments for comparison logic.
    /// </summary>
    public interface IOperator : ICacheable
    {
        /// <summary>
        /// The type of operator, which can be used to determine how to perform the comparison. This could be a string that indicates the kind of comparison (e.g., "Equals", "GreaterThan", "LessThan", etc.) or any other identifier that helps to understand the purpose of the operator and how it should be applied in the comparison logic.
        /// </summary>
        string Type { get; }
        /// <summary>
        /// A dictionary containing additional arguments that might be needed for the comparison specific to the operator.
        /// </summary>
        IReadOnlyDictionary<string, object> Arguments { get; }
    }
}
