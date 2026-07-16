using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using RimWorld;
using UnityEngine.Windows;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;
using Expression = System.Linq.Expressions.Expression;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// Reference type that resolves a reference to a CompProperties or ThingComp from a given input, which can be either a <see cref="Def"/> or a <see cref="Thing"/>. The value is expected to be either the name of the comp type to resolve, or the full type name of the comp type, optionally followed by a pipe character and a path to traverse on the resolved comp. The reference will first attempt to resolve the comp from the def if the input is a def, and if that fails, it will attempt to resolve it from the thing if the input is a thing. If both attempts fail, it will return null.
    /// </summary>
    public class CompReferenceType : IReferenceTypeCompileable
    {
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="CompReferenceType"/>. This can be used wherever an instance of this reference type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static CompReferenceType Instance { get; } = new CompReferenceType();
        /// <summary>
        /// The default name for this reference type, which can be used when defining references that should be resolved using this type.
        /// </summary>
        public const string DefaultTypeName = "Comp";
        /// <summary>
        /// The separator character used to separate the comp type name and the path to traverse in the value string.
        /// This allows the selecting of sub properties/fields.
        /// </summary>
        public const char PathSeparator = '|';

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            if(value is null) return null;
            Type compType = value as Type;
            string properties = null;
            if (compType is null)
            {
                var reference = value?.ToString();
                if (string.IsNullOrWhiteSpace(reference)) return null;

                if (reference.Contains(PathSeparator))
                {
                    var split = reference.Split(PathSeparator);
                    var compTypeName = split[0];
                    compType = Toolkit.Cache<string, Type>.GetOrSet(compTypeName, () => Toolkit.Helpers.TryGetType(compTypeName), true);
                    properties = split[1];
                }
                else
                {
                    compType = Toolkit.Cache<string, Type>.GetOrSet(reference, () => Toolkit.Helpers.TryGetType(reference), true);
                }
            }

            if(compType is null)
            {
                if(IsVerboseEnabled) LogVerbose($"CompReferenceType: Could not find comp type with name '{value}'");
                return null;
            }

            object returnObject = null;
            // Try def first
            Verse.Def def = null;
            if (input is IIndexed<Def> indexed)
            {
                def = indexed.Value;
            }
            else if (input is Def d)
            {
                def = d;
            }

            if(def is not null)
            {
                var isCompProperties = typeof(CompProperties).IsAssignableFrom(compType);
                if(!isCompProperties)
                {
                    if(IsVerboseEnabled) LogVerbose($"CompReferenceType: Type '{compType.FullName}' is not a CompProperties type. Required when input object is Def");
                    return null;
                }

                if (def is ThingDef thingDef)
                {
                    var getCompProperties = Toolkit.Cache<Type, Func<ThingDef, CompProperties>>.GetOrSet(compType, () =>
                    {
                        var method = ToolkitConstants.Reflections.GetCompProperties.GetGenericMethodDefinition().MakeGenericMethod(compType);
                        var inputParameter = Expression.Parameter(typeof(ThingDef), "thingDef");
                        var call = Expression.Call(inputParameter, method);
                        var lambda = Expression.Lambda<Func<ThingDef, CompProperties>>(call, inputParameter);
                        return lambda.Compile();
                    });
                    
                    returnObject = getCompProperties(thingDef);
                }
            }

            // Try Thing next
            if(def is null)
            {
                Thing thing = null;
                if (input is IIndexed<Thing> indexedThing)
                {
                    thing = indexedThing.Value;
                }
                else if (input is Thing t)
                {
                    thing = t;
                }
                if(thing is not null)
                {
                    var isThingComp = typeof(ThingComp).IsAssignableFrom(compType);
                    if(!isThingComp)
                    {
                        if(IsVerboseEnabled) LogVerbose($"CompReferenceType: Type '{compType.FullName}' is not a ThingComp type. Required when input object is Thing");
                        return null;
                    }
                    var tryGetComp = Toolkit.Cache<Type, Func<Thing, ThingComp>>.GetOrSet(compType, () =>
                    {
                        var method = ToolkitConstants.Reflections.TryGetComp.GetGenericMethodDefinition().MakeGenericMethod(compType);
                        var inputParameter = Expression.Parameter(typeof(Thing), "thing");
                        var call = Expression.Call(method, inputParameter);
                        var lambda = Expression.Lambda<Func<Thing, ThingComp>>(call, inputParameter);
                        return lambda.Compile();
                    });
                    returnObject = tryGetComp(thing);
                }
            }

            if (!string.IsNullOrWhiteSpace(properties))
            {
                var paths = Toolkit.Helpers.Traversing.SplitPath(properties);
                return Toolkit.Helpers.Traversing.TraversePath(returnObject, paths);
            }

            return returnObject;
        }
        /// <inheritdoc/>
        public string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType)
        {
            returnType = typeof(object);
            if (input is null) return null;
            if (value is null) return null;
            if (!(input is IIndexed<Def> || input is Def || input is IIndexed<Thing> || input is Thing)) return null;
            if (!(value is string || value is Type)) return null;

            var compType = TryGetCompType(value, out var properties);
            if(compType == null) return null;
            if (properties is not null)
            {
                returnType = Toolkit.Helpers.Traversing.TryWalkPath(compType, properties);
            }
            else
            {
                returnType = compType;
            }

            return $"{input.GetType()}:{value.GetType()}";
        }
        /// <inheritdoc/>
        public Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context)
        {
            if (input is null) return null;
            if (value is null) return null;
            var compType = TryGetCompType(value, out var properties);
            if (compType == null) return null;
            Type returnType;
            if (properties is not null)
            {
                returnType = Toolkit.Helpers.Traversing.TryWalkPath(compType, properties);
            }
            else
            {
                returnType = compType;
            }

            var inputType = input.GetType();
            Expression getInput = inputParameter;
            bool inputIsDef = false;
            if(input is Def)
            {
                inputIsDef = true;
                getInput = Expression.Convert(getInput, typeof(Def));
            }
            else if(input is IIndexed<Def> indexedDef)
            {
                inputIsDef = true;
                getInput = Expression.Property(Expression.Convert(inputParameter, typeof(IIndexed<Def>)), nameof(indexedDef.Value));
            }
            else if (input is Thing)
            {
                getInput = Expression.Convert(getInput, typeof(Thing));
            }
            else if (input is IIndexed<Thing> thing)
            {
                getInput = Expression.Property(Expression.Convert(inputParameter, typeof(IIndexed<Thing>)), nameof(indexedDef.Value));
            }

            Expression getComp;
            if (inputIsDef)
            {
                var isCompProperties = typeof(CompProperties).IsAssignableFrom(compType);
                if (!isCompProperties)
                {
                    if (IsVerboseEnabled) LogVerbose($"CompReferenceType: Type '{compType.FullName}' is not a CompProperties type. Required when input object is Def");
                    return null;
                }

                var thingDefVariable = Expression.Variable(typeof(ThingDef));
                var compPropertiesVariable = Expression.Variable(compType);
                var assignThingDef = Expression.Assign(thingDefVariable, Expression.Convert(getInput, typeof(ThingDef)));

                var getMethod = ToolkitConstants.Reflections.GetCompProperties.GetGenericMethodDefinition().MakeGenericMethod(compType);
                getComp = Expression.Call(thingDefVariable, getMethod);

                var ifThingDefNotNullGetElseDefault = Expression.IfThen(Expression.NotEqual(thingDefVariable, Expression.Default(typeof(ThingDef))),
                                                                           Expression.Assign(compPropertiesVariable, getComp));
                getComp = Expression.Block([thingDefVariable, compPropertiesVariable], assignThingDef, ifThingDefNotNullGetElseDefault, compPropertiesVariable);
            }
            else
            {
                var isThingComp = typeof(ThingComp).IsAssignableFrom(compType);
                if (!isThingComp)
                {
                    if (IsVerboseEnabled) LogVerbose($"CompReferenceType: Type '{compType.FullName}' is not a ThingComp type. Required when input object is Thing");
                    return null;
                }

                var getMethod = ToolkitConstants.Reflections.TryGetComp.GetGenericMethodDefinition().MakeGenericMethod(compType);
                getComp = Expression.Call(getInput, getMethod);
            }

            if(properties is null)
            {
                return getComp;
            }

            return Toolkit.Helpers.Traversing.GenerateFullGetter(getComp, compType, properties);
        }

        private Type TryGetCompType(object value, out string properties)
        {
            Type compType = value as Type;
            properties = null;
            if (compType is null)
            {
                var reference = value?.ToString();
                if (string.IsNullOrWhiteSpace(reference)) return null;

                if (reference.Contains(PathSeparator))
                {
                    var split = reference.Split(PathSeparator);
                    var compTypeName = split[0];
                    compType = Toolkit.Cache<string, Type>.GetOrSet(compTypeName, () => Toolkit.Helpers.TryGetType(compTypeName), true);
                    properties = split[1];
                }
                else
                {
                    compType = Toolkit.Cache<string, Type>.GetOrSet(reference, () => Toolkit.Helpers.TryGetType(reference), true);
                }
            }

            if (compType is null)
            {
                if (IsVerboseEnabled) LogVerbose($"CompReferenceType: Could not find comp type with name '{value}'");
                return null;
            }
            return compType;
        }
    }

    /// <summary>
    /// Contains extension methods related to the <see cref="CompReferenceType"/>.
    /// </summary>
    public static class CompReferenceTypeExtensions
    {
        /// <summary>
        /// Fluent syntax for creating a reference definition that uses the <see cref="CompReferenceType"/> to resolve a comp value from an object. The provided comp type will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="CompReferenceType"/>. This allows for easy creation of references that can access comps of objects in a fluent manner.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="compType">The type of the comp to resolve from the object.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Comp<TReturn>(this IConditionOperandBuilder<TReturn> builder, Type compType)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = CompReferenceType.DefaultTypeName, Value = Guard.NotNull(compType, nameof(compType)) });
        /// <summary>
        /// Fluent syntax for creating a reference definition that uses the <see cref="CompReferenceType"/> to resolve a comp value from an object. The provided comp name will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="CompReferenceType"/>. This allows for easy creation of references that can access comps of objects in a fluent manner, using just the name of the comp type. The comp name can be either the simple name or the full name of the comp type, and it will be resolved to a Type when the reference is resolved.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="compName">The name of the comp type to resolve from the object.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Comp<TReturn>(this IConditionOperandBuilder<TReturn> builder, string compName)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = CompReferenceType.DefaultTypeName, Value = Guard.NotNullOrEmpty(compName, nameof(compName)) });
    }
}
