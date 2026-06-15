using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using UnityEngine;
using static HomebrewDot.Net.Rimworld.Toolkit;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// A <see cref="IReferenceTypeCompileable"/> that resolves property values from <see cref="IIndexed{T}"/>.
    /// </summary>
    public class IndexedReferenceType : IReferenceType
    {
        // Constants
        /// <summary>
        /// The default name for this reference type, which can be used when defining references that should be resolved using this type.
        /// </summary>
        public const string DefaultTypeName = "Indexed";
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="IndexedReferenceType"/>. This can be used wherever an instance of this reference type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static IndexedReferenceType Instance { get; } = new IndexedReferenceType();

        private IndexedReferenceType()
        {
            
        }

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            if(value is null)
            {
                return null;
            }
            if(context is null)
            {
                return null;
            }

            var propertyName = value.ToString();
            if (input != null)
            {
                var splitProperties = Helpers.Traversing.SplitPath(propertyName);
                if (input is IIndexed<object> indexed)
                {
                    if (splitProperties.Length == 1)
                    {
                        return indexed.GetValue<object>(propertyName);
                    }
                    else
                    {
                        if (indexed.GetValue<object>(splitProperties[0]) is object nestedObj)
                        {
                            var remainingPath = splitProperties.Skip(1).ToArray();
                            return Helpers.Traversing.TraversePath(nestedObj, remainingPath);
                        }
                    }
                }
                else
                {
                    Logging.LogVerbose($"Input object is not of type IIndexed<object>. Falling back on property reference.");
                    return Helpers.Traversing.TraversePath(input, splitProperties);
                }
            }
            else
            {
                Logging.LogVerbose($"Input object is required for resolving property '{value}'.");
                return null;
            }
            return null;
        }
    }

    /// <summary>
    /// Contains extension methods related to the <see cref="IndexedReferenceType"/>.
    /// </summary>
    public static class IndexedReferenceTypeExtensions
    {
        /// <summary>
        /// Fluent syntax for creating a reference definition that uses the <see cref="IndexedReferenceType"/> to resolve a property value from an indexed object. The provided property name will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="IndexedReferenceType"/>. This allows for easy creation of references that can access properties of indexed objects in a fluent manner.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="propertyName">The name of the property to resolve from the indexed object.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Indexed<TReturn>(this IConditionOperandBuilder<TReturn> builder, string propertyName)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = IndexedReferenceType.DefaultTypeName, Value = Guard.NotNullOrEmpty(propertyName, nameof(propertyName)) });
    }
}
