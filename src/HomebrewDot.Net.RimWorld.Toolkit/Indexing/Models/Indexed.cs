using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Extensions;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;
using TLC = HomebrewDot.Net.Rimworld.ToolkitConstants;

namespace HomebrewDot.Net.Rimworld.Indexing.Models
{
    /// <summary>
    /// Default implementation of IIndexed<T>. Uses compiled linq expressions for fast access to value properties and metadata.
    /// </summary>
    /// <typeparam name="T">The type of the indexed object.</typeparam>
    public class Indexed<T> : IIndexed<T> where T : class
    {
        //Statics
        private static readonly IDictionary<string, IDictionary<Type, Delegate>> _propertyAccessors = new Dictionary<string, IDictionary<Type, Delegate>>(StringComparer.OrdinalIgnoreCase);
        private static TValue GetValue<TValue>(string propertyName, T instance, IReadOnlyDictionary<string, object> metadata)
        {
            propertyName = Guard.NotNullOrWhitespace(propertyName, nameof(propertyName));

            IDictionary<Type, Delegate> typeAccessors;
            if (!_propertyAccessors.TryGetValue(propertyName, out typeAccessors))
            {
                lock (_propertyAccessors)
                {
                    if (!_propertyAccessors.TryGetValue(propertyName, out typeAccessors))
                    {
                        typeAccessors = new Dictionary<Type, Delegate>();
                        _propertyAccessors[propertyName] = typeAccessors;
                    }
                }
            }

            Func<T, IReadOnlyDictionary<string, object>, TValue> accessor;
            if (!typeAccessors.TryGetValue(typeof(TValue), out var accessorDelegate))
            {
                lock (typeAccessors)
                {
                    if (!typeAccessors.TryGetValue(typeof(TValue), out accessorDelegate))
                    {
                        accessor = GetAccessor<TValue>(propertyName);
                        typeAccessors[typeof(TValue)] = accessor;
                    }
                    else
                    {
                        accessor = (Func<T, IReadOnlyDictionary<string, object>, TValue>)accessorDelegate;
                    }
                }
            }
            else
            {
                accessor = (Func<T, IReadOnlyDictionary<string, object>, TValue>)accessorDelegate;
            }

            return accessor(instance, metadata);
        }
        private static Func<T, IReadOnlyDictionary<string, object>, TValue> GetAccessor<TValue>(string propertyName)
        {
            propertyName = Guard.NotNullOrWhitespace(propertyName, nameof(propertyName));
            var instanceParameter = Expression.Parameter(typeof(T), "instance");
            var metadataParameter = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "metadata");
            var propertyNameExpression = Expression.Constant(propertyName, typeof(string));
            var resultVariable = Expression.Variable(typeof(TValue), "result");
            var assignDefault = Expression.Assign(resultVariable, Expression.Default(typeof(TValue)));
            var actualValueType = typeof(TValue).GetActualType();
            var valueTypeConstant = Expression.Constant(actualValueType, typeof(Type));
            var valueDefault = Expression.Default(actualValueType);

            Expression accessorExpression = null;
            Expression changeTypeCall;
            if (TLC.ObjectCache<T>.IndexedProperties.TryGetValue(propertyName, out var propertyInfo))
            {
                var propertyAccess = Expression.Property(instanceParameter, propertyInfo);
                var propertyType = propertyInfo.PropertyType;
                if (!typeof(TValue).IsAssignableFrom(propertyInfo.PropertyType) || (propertyType.IsValueType && !actualValueType.IsValueType))
                {
                    var changeTypeArgument = Expression.Convert(propertyAccess, typeof(object));
                    changeTypeCall = Expression.Call(TLC.Reflections.ConvertChangeType, changeTypeArgument, valueTypeConstant);
                    if (propertyType.IsValueType)
                    {
                        accessorExpression = Expression.Assign(resultVariable, Expression.Convert(changeTypeCall, actualValueType));
                    }
                    else
                    {
                        var ifNotNull = Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyInfo.PropertyType));
                        var IfNotNullThenConvertElseDefault = Expression.Condition(ifNotNull, changeTypeCall, valueDefault);
                        accessorExpression = Expression.Assign(resultVariable, Expression.Convert(IfNotNullThenConvertElseDefault, actualValueType));
                    }
                }
                else
                {
                    accessorExpression = Expression.Assign(resultVariable, propertyAccess);
                }
            }
            if(accessorExpression == null && TLC.ObjectCache<T>.IndexedFields.TryGetValue(propertyName, out var fieldInfo))
            {
                var fieldAccess = Expression.Field(instanceParameter, fieldInfo);
                var fieldType = fieldInfo.FieldType;
                if (!typeof(TValue).IsAssignableFrom(fieldInfo.FieldType))
                {
                    var changeTypeArgument = Expression.Convert(fieldAccess, typeof(object));
                    changeTypeCall = Expression.Call(TLC.Reflections.ConvertChangeType, changeTypeArgument, valueTypeConstant);
                    if (fieldType.IsValueType)
                    {
                        accessorExpression = Expression.Assign(resultVariable, Expression.Convert(changeTypeCall, actualValueType));
                    }
                    else
                    {

                        var ifNotNull = Expression.NotEqual(fieldAccess, Expression.Constant(null, fieldInfo.FieldType));
                        var IfNotNullThenConvertElseDefault = Expression.Condition(ifNotNull, changeTypeCall, valueDefault);
                        accessorExpression = Expression.Assign(resultVariable, Expression.Convert(IfNotNullThenConvertElseDefault, actualValueType));
                    }
                }
                else
                {
                    accessorExpression = Expression.Assign(resultVariable, fieldAccess);
                }
            }

            var containsKeyCall = Expression.Call(metadataParameter, TLC.Reflections.DictionaryStringObjectContainsKey, propertyNameExpression);
            var getItemCall = Expression.Call(metadataParameter, TLC.Reflections.DictionaryStringObjectGetItem, propertyNameExpression);
            var tempVariable = Expression.Variable(typeof(object), "temp");
            var assignTemp = Expression.Assign(tempVariable, getItemCall);
            var getTempType = Expression.Call(tempVariable, TLC.ObjectCache<object>.GetTypeMethod);
            var testNotNullButNotAssignable = Expression.AndAlso(Expression.NotEqual(tempVariable, TLC.Expressions<Object>.Default), Expression.Not(Expression.Call(getTempType, TLC.Reflections.TypeIsAssignableFrom, Expression.Constant(typeof(TValue), typeof(Type)))));
            changeTypeCall = Expression.Call(TLC.Reflections.ConvertChangeType, tempVariable, Expression.Constant(typeof(TValue), typeof(Type)));
            var convertNotAssignable = Expression.Convert(changeTypeCall, typeof(TValue));
            Expression assignFromMetadata;
            var isNonNullableValueType = actualValueType.IsValueType;
            if (isNonNullableValueType)
            {
                assignFromMetadata = Expression.IfThenElse(
                    Expression.Equal(tempVariable, TLC.Expressions<Object>.Default),
                    Expression.Assign(resultVariable, Expression.Default(typeof(TValue))),
                    Expression.IfThenElse(testNotNullButNotAssignable,
                        Expression.Assign(resultVariable, convertNotAssignable),
                        Expression.Assign(resultVariable, Expression.Convert(tempVariable, typeof(TValue)))
                    )
                );
            }
            else
            {
                assignFromMetadata = Expression.IfThenElse(testNotNullButNotAssignable,
                    Expression.Assign(resultVariable, convertNotAssignable),
                    Expression.Assign(resultVariable, Expression.Convert(tempVariable, typeof(TValue)))
                );
            }

            Expression body;
            if (accessorExpression != null)
            {
                var ifContainsKeyThenTryGetFromMetadataElseFromProperty = Expression.IfThenElse(
                    containsKeyCall,
                          Expression.Block(new[] { tempVariable },
                        assignTemp,
                        assignFromMetadata
                    ),
                    accessorExpression
                );
                body = Expression.Block(new[] { resultVariable }, assignDefault, ifContainsKeyThenTryGetFromMetadataElseFromProperty, resultVariable);
            }
            else
            {
                var ifContainsKeyThenTryGetFromMetadataElseDefault = Expression.IfThenElse(
                    containsKeyCall,
                          Expression.Block(new[] { tempVariable },
                        assignTemp,
                        assignFromMetadata
                    ),
                    assignDefault
                );
                body = Expression.Block(new[] { resultVariable }, ifContainsKeyThenTryGetFromMetadataElseDefault, resultVariable);
            }

            var lambda = Expression.Lambda<Func<T, IReadOnlyDictionary<string, object>, TValue>>(body, instanceParameter, metadataParameter);
            return lambda.Compile();
        }

        // Properties
        /// <inheritdoc/>
        public T Value { get; }
        /// <inheritdoc/>
        public virtual IReadOnlyDictionary<string, object> Metadata { get; }
        /// <inheritdoc/>
        public bool HasSnapshot => Snapshot != null;
        /// <inheritdoc/>
        public virtual bool IsSnapshot => false;
        /// <inheritdoc/>
        public virtual IIndexed<T> Snapshot => null;

        // Constructor
        /// <inheritdoc cref="Indexed{T}"/>
        /// <param name="value"><see cref="Value"/></param>
        /// <param name="metadata"><see cref="Metadata"/></param>
        public Indexed(T value, IReadOnlyDictionary<string, object> metadata)
        {
            Value = Guard.NotNull(value, nameof(value));
            Metadata = Guard.NotNull(metadata, nameof(metadata));
        }
        /// <inheritdoc cref="Indexed{T}"/>
        /// <param name="value"><see cref="Value"/></param>
        protected Indexed(T value)
        {
            Value = Guard.NotNull(value, nameof(value));
        }

        // Methods
        /// <summary>
        /// Retrieves the value of the specified property from the indexed object or its metadata. If the property exists in both, the metadata value takes precedence.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to retrieve.</typeparam>
        /// <param name="propertyName">The name of the property to retrieve.</param>
        /// <returns>The value of the specified property.</returns>
        public virtual TValue GetValue<TValue>(string propertyName)
        {
            propertyName = Guard.NotNullOrWhitespace(propertyName, nameof(propertyName));
            return GetValue<TValue>(propertyName, Value, Metadata);
        }
    }
}
