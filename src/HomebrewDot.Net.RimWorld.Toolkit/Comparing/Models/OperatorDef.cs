using System.Collections.Generic;

namespace HomebrewDot.Net.Rimworld.Comparing.Models
{
    /// <summary>
    /// Definition of an operator, which implements the <see cref="IOperator"/> interface and can be used to represent an operator that compares 2 objects using a specific type and arguments.
    /// </summary>
    public class OperatorDef : IOperator
    {
        /// <inheritdoc/>
        public string Type { get; set; }
        /// <inheritdoc/>
        public IReadOnlyDictionary<string, object> Arguments { get; set; }
    }
}