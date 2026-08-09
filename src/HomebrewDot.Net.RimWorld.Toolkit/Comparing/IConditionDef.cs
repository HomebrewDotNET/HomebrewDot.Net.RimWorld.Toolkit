using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Models;

namespace HomebrewDot.Net.Rimworld.Comparing
{
    /// <summary>
    /// Definition of a condition that compares 2 references using a specific operator. 
    /// </summary>
    public interface IConditionDef
    {
        /// <summary>
        /// Makes the current defition a group of conditions that will pass if all of the conditions in the group pass. This allows for complex nested conditions to be defined, where a condition can contain multiple sub-conditions that must all be satisfied for the overall condition to be considered true.
        /// </summary>
        IReadOnlyList<IConditionDef> Conditions { get; }
        /// <summary>
        /// How <see cref="Conditions"/> should compare to the current condition when both are defined. Used to define 'AND' / 'OR' statements between the current condition and the group of conditions defined in <see cref="Conditions"/>. If true, the group of conditions will be compared to the current condition using 'OR', meaning that if either the current condition or any of the conditions in the group pass, the overall condition will be considered true. If false, the group of conditions will be compared to the current condition using 'AND', meaning that both the current condition and all of the conditions in the group must pass for the overall condition to be considered true.
        /// </summary>
        bool ConditionGroupIsOr { get; }

        /// <summary>
        /// The left hand side object to compare. <see cref="IReference"/> will be resolved to the value of the reference, while any other object will be used as is. String values can also be converted to a default <see cref="IReference"/> depending on the context of the comparison.
        /// </summary>
        object Compare { get; }
        /// <summary>
        /// The operator to use when comparing the left and right hand side objects. The operator will determine how the two objects are compared, such as checking for equality, inequality, greater than, less than, etc. <see cref="IOperator"/> will be resolved to the operator type, while string values can also be converted to a default <see cref="IOperator"/> depending on the context of the comparison.
        /// </summary>
        object With { get; }
        /// <summary>
        /// The right hand side object to compare. <see cref="IReference"/> will be resolved to the value of the reference, while any other object will be used as is. String values can also be converted to a default <see cref="IReference"/> depending on the context of the comparison.
        /// </summary>
        object To { get; }

        /// <summary>
        /// How the current condition should be compared to the next condition in the group when applicable. Used to define 'AND' / 'OR' statements.
        /// </summary>
        bool IsOr { get; }

        /// <summary>
        /// Inverts the condition, so it matches when the underlying comparison would not match and vice versa. Allows defining "Not" conditions for any operator (e.g. "not in thing category") without needing dedicated inverted operator types.
        /// </summary>
        bool Inverted { get; }
    }
}
