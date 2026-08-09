using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using RimWorld;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Expression = System.Linq.Expressions.Expression;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// Reference type that just returns the input object.
    /// </summary>
    public class SelfReferenceType : IReferenceTypeCompileable
    {
        // Constants
        /// <summary>
        /// The default name for this reference type, which can be used when defining references that should be resolved using this type.
        /// </summary>
        public const string DefaultTypeName = "Self";
        /// <summary>
        /// The singleton instance of the <see cref="SelfReferenceType"/>. This can be used wherever an instance of this reference type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static SelfReferenceType Instance { get; } = new SelfReferenceType();

        private SelfReferenceType() { }

        /// <inheritdoc/>
        public bool RequiresValue => false;

        /// <inheritdoc/>
        public Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context)
        {
            return inputParameter;
        }
        /// <inheritdoc/>
        public string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType)
        {
            returnType = input?.GetType() ?? typeof(object);
            return $"{DefaultTypeName}:{returnType.FullName}";
        }

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            return input;
        }
    }
}

/// <summary>
/// Contains extension methods related to the <see cref="SelfReferenceType"/>.
/// </summary>
public static class SelfReferenceTypeExtensions
{
    /// <summary>
    /// Fluent syntax for creating a reference definition that uses the <see cref="SelfReferenceType"/> to resolve a stat value from an object. The provided stat name will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="SelfReferenceType"/>. This allows for easy creation of references that can access stats of objects in a fluent manner.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type.</typeparam>
    /// <param name="builder">The condition builder.</param>
    /// <param name="statName">The name of the stat to resolve from the object.</param>
    /// <returns>The fluent return type.</returns>
    public static TReturn Self<TReturn>(this IConditionOperandBuilder<TReturn> builder)
        => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = SelfReferenceType.DefaultTypeName });
}
