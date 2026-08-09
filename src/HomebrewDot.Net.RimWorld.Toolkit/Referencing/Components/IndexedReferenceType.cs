using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using UnityEngine;
using static HomebrewDot.Net.Rimworld.Toolkit;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Expression = System.Linq.Expressions.Expression;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// A <see cref="IReferenceTypeCompileable"/> that resolves property values from <see cref="IIndexed{T}"/>.
    /// </summary>
    public class IndexedReferenceType : IReferenceTypeCompileable
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
        public bool RequiresValue => true;

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
            if(string.IsNullOrWhiteSpace(propertyName))
            {
                if(IsVerboseEnabled) LogVerbose($"Property name for resolving indexed reference is null or whitespace. Value: '{value}'.");
                return null;
            }
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
                    if(IsVerboseEnabled) LogVerbose($"Input object is not of type IIndexed<object>. Falling back on property reference.");
                    return Helpers.Traversing.TraversePath(input, splitProperties);
                }
            }
            else
            {
                if(IsVerboseEnabled) LogVerbose($"Input object is required for resolving property '{value}'.");
                return null;
            }
            return null;
        }
        /// <inheritdoc/>
        public string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType)
        {
            returnType = typeof(object);
            if (input is null) return null;
            if (value is null) return null;

            returnType = Helpers.Traversing.TryWalkIndexedPath(input.GetType(), value.ToString());
            return $"{input.GetType().FullName}:{value}";
        }
        /// <inheritdoc/>
        public System.Linq.Expressions.Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context)
        {
            inputParameter = Guard.NotNull(inputParameter, nameof(inputParameter));
            input = Guard.NotNull(input, nameof(input));
            contextParameter = Guard.NotNull(contextParameter, nameof(contextParameter));
            value = Guard.NotNull(value, nameof(value));
            var propertyPath = Helpers.Traversing.SplitPath(value.ToString());
            var inputType = input.GetType();

            if (input is IIndexed<object>)
            {
                var metadataKey = propertyPath[0];
                var metadataType = Helpers.Traversing.TryGetIndexedMetadataType(inputType, metadataKey);
                var valueType = inputType.GetGenericArguments().Single();
                var metadataVariable = Expression.Variable(metadataType, "metadata");
                var fullIndexedType = typeof(IIndexed<>).MakeGenericType(valueType);
                var inputVariable = Expression.Variable(fullIndexedType, "input");
                var assignConvertedInput = Expression.Assign(inputVariable, Expression.Convert(inputParameter, fullIndexedType));
                var members = Helpers.Traversing.GetMembers(valueType).ToDictionary(m => m.Name, m => m);
                var actions = new List<Expression> { assignConvertedInput };
                if(members.TryGetValue(metadataKey, out var member))
                {
                    var getValueGenericProperty = Helpers.Expression.GetProperty<IIndexed<object>>(x => x.Value);
                    var getValueProperty = Helpers.Traversing.GetMembers(fullIndexedType).OfType<PropertyInfo>().First(m => m.Name == getValueGenericProperty.Name);
                    var getValueExpression = Expression.Property(inputVariable, getValueProperty);

                    if (member is PropertyInfo property)
                    {
                        actions.Add(Expression.Assign(metadataVariable, Expression.Property(getValueExpression, property)));
                    }
                    else if(member is FieldInfo field)
                    {
                        actions.Add(Expression.Assign(metadataVariable, Expression.Field(getValueExpression, field)));
                    }
                }
                var getMetadataGenericProperty = Helpers.Expression.GetProperty<IIndexed<object>>(x => x.Metadata);
                var getMetadataProperty = Helpers.Traversing.GetMembers(fullIndexedType).OfType<PropertyInfo>().First(m => m.Name == getMetadataGenericProperty.Name);
                var getMetadataExpression = Expression.Property(inputVariable, getMetadataProperty);
                var containsKeyMethod = ToolkitConstants.Reflections.DictionaryStringObjectContainsKey;
                var getItemMethod = ToolkitConstants.Reflections.DictionaryStringObjectGetItem;
                var containsKeyCall = Expression.Call(getMetadataExpression, containsKeyMethod, Expression.Constant(metadataKey));
                var getItemCall = Expression.Call(getMetadataExpression, getItemMethod, Expression.Constant(metadataKey));
                var castItem = Expression.Convert(getItemCall, metadataType);
                var ifContainsKey = Expression.IfThen(containsKeyCall, Expression.Assign(metadataVariable, castItem));
                actions.Add(ifContainsKey);
                Type blockReturnType;
                if (propertyPath.Length > 1)
                {
                    var remainingPath = propertyPath.Skip(1).ToArray();
                    var traverseExpression = Helpers.Traversing.GenerateFullGetter(metadataVariable, metadataType, remainingPath);
                    actions.Add(traverseExpression);
                    blockReturnType = traverseExpression.Type;
                }
                else
                {
                    actions.Add(metadataVariable);
                    blockReturnType = metadataVariable.Type;
                }

                var block = Expression.Block(blockReturnType, new[] { metadataVariable, inputVariable }, actions);
                return block;
            }
            else
            {
                return Helpers.Traversing.GenerateFullGetter(Expression.Convert(inputParameter, inputType), inputType, propertyPath);
            }
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
