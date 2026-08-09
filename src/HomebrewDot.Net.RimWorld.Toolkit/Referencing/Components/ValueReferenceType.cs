using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// A reference type that simply returns the value it is given.
    /// </summary>
    public class ValueReferenceType : IReferenceTypeCompileable
    {
        // Constants
        /// <summary>
        /// The default name for this reference type.
        /// </summary>
        public const string DefaultTypeName = "Value";
        // Statics
        /// <summary>
        /// The singleton instance of this reference type. Since it has no state, only one instance is needed.
        /// </summary>
        public static ValueReferenceType Instance { get; } = new ValueReferenceType();

        private ValueReferenceType()
        {
            
        }

        /// <inheritdoc/>
        public bool RequiresValue => true;

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
            => value;
        /// <inheritdoc/>
        public string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType)
        {
            returnType = value?.GetType() ?? typeof(object);
            return $"{DefaultTypeName}:{returnType.FullName}";
        }

        /// <inheritdoc/>
        public Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context)
        {
            return value is null ? ToolkitConstants.Expressions<object>.Default : Expression.Constant(value);
        }
    }
}
