using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using static HomebrewDot.Net.Rimworld.Toolkit;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Expression = System.Linq.Expressions.Expression;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// A <see cref="IReferenceTypeCompileable"/> that resolves property paths from objects used to select sub objects.
    /// </summary>
    public class PropertyReferenceType : IReferenceTypeCompileable
    {
        // Constants
        /// <summary>
        /// The default name for this reference type, which can be used when defining references that should be resolved using this type.
        /// </summary>
        public const string DefaultTypeName = "Property";
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="PropertyReferenceType"/>. This can be used wherever an instance of this reference type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static PropertyReferenceType Instance { get; } = new PropertyReferenceType();

        /// <inheritdoc cref="PropertyReferenceType"/>
        protected PropertyReferenceType()
        {

        }

        /// <inheritdoc/>
        public bool RequiresValue => true;

        /// <inheritdoc/>
        public virtual object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            if (value is null)
            {
                return null;
            }
            if (context is null)
            {
                return null;
            }
            if (input is null)
            {
                return null;
            }

            var propertyName = value.ToString();
            var paths = Helpers.Traversing.SplitPath(propertyName);
            return Helpers.Traversing.TraversePath(input, paths);
        }
        /// <inheritdoc/>
        public string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType)
        {
            returnType = typeof(object);
            if (input is null) return null;
            if (value is null) return null;

            returnType = Helpers.Traversing.TryWalkPath(input.GetType(), value.ToString());
            return $"{input.GetType().FullName}:{value}";
        }
        /// <inheritdoc/>
        public System.Linq.Expressions.Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context)
        {
            inputParameter = Guard.NotNull(inputParameter, nameof(inputParameter));
            input = Guard.NotNull(input, nameof(input));
            contextParameter = Guard.NotNull(contextParameter, nameof(contextParameter));
            value = Guard.NotNull(value, nameof(value));
            var inputType = input.GetType();

            return Helpers.Traversing.GenerateFullGetter(Expression.Convert(inputParameter, inputType), inputType, value.ToString());
        }
    }

    /// <summary>
    /// Contains extension methods related to the <see cref="PropertyReferenceType"/>.
    /// </summary>
    public static class PropertyReferenceTypeExtensions
    {
        /// <summary>
        /// Fluent syntax for creating a reference definition that uses the <see cref="PropertyReferenceType"/> to resolve a property value from an object. The provided property name will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="PropertyReferenceType"/>. This allows for easy creation of references that can access properties of objects in a fluent manner.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="propertyName">The name of the property to resolve from the object.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Property<TReturn>(this IConditionOperandBuilder<TReturn> builder, string propertyName)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = PropertyReferenceType.DefaultTypeName, Value = Guard.NotNullOrEmpty(propertyName, nameof(propertyName)) });
    }
}
