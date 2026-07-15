using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Extensions;

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
        /// <inheritdoc/>
        public string GetCacheKey()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('{').Append(Type).Append(" => ");
            Arguments.ToCacheKey(sb, true);
            sb.Append('}');
            return sb.ToString();
        }
    }
}