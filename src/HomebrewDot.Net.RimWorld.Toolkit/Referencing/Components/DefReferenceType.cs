using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Expression = System.Linq.Expressions.Expression;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// A generic reference type for returning defs of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of def to reference.</typeparam>
    public class DefReferenceType<T> : IReferenceTypeCompileable where T : Def
    {
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="DefReferenceType{T}"/>. This can be used wherever an instance of this reference type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static DefReferenceType<T> Instance { get; } = new DefReferenceType<T>();
        /// <summary>
        /// The default name for this reference type, which can be used when defining references that should be resolved using this type.
        /// </summary>
        public static readonly string DefaultTypeName = typeof(T).Name;

        protected DefReferenceType() { }

        /// <inheritdoc/>
        public bool RequiresValue => true;

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            if(value is not string defName)
            {
                Logging.LogWarning($"DefReferenceType<{typeof(T).Name}>: Value is not a string. Value: {value}");
                return null;
            }

            return DefDatabase<T>.GetNamedSilentFail(defName);
        }
        /// <inheritdoc/>
        public string GetCacheKey(object input, object value, IReadOnlyDictionary<string, object> context, out Type returnType)
        {
            returnType = typeof(T);
            if(value is not string defName)
            {
                Logging.LogWarning($"DefReferenceType<{typeof(T).Name}>: Value is not a string. Value: {value}");
                return null;
            }
            return $"{typeof(T).FullName}:{defName}";
        }
        /// <inheritdoc/>
        public System.Linq.Expressions.Expression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, object value, IReadOnlyDictionary<string, object> context)
        {
            if(value is not string defName)
            {
                Logging.LogWarning($"DefReferenceType<{typeof(T).Name}>: Value is not a string. Value: {value}");
                return Expression.Constant(null, typeof(T));
            }

            var def = DefDatabase<T>.GetNamedSilentFail(defName);
            if(def == null)
            {
                Logging.LogWarning($"DefReferenceType<{typeof(T).Name}>: Def not found. DefName: {defName}");
                return Expression.Constant(null, typeof(T));
            }
            return Expression.Constant(def, typeof(T));
        }
    }

    /// <summary>
    /// Contains extension methods related to the <see cref="DefReferenceType{T}"/>.
    /// </summary>
    public static class DefReferenceTypeExtensions
    {
        /// <summary>
        /// Fluent syntax for creating a reference definition that uses the <see cref="DefReferenceType{T}"/> to resolve a def value from an object. The provided def type will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="DefReferenceType{T}"/>. This allows for easy creation of references that can access defs of objects in a fluent manner.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="defName">The name of the def to resolve from the object.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn ThingCategory<TReturn>(this IConditionOperandBuilder<TReturn> builder, string defName)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = DefReferenceType<ThingCategoryDef>.DefaultTypeName, Value = Guard.NotNull(defName, nameof(defName)) });
        /// <summary>
        /// Fluent syntax for creating a reference definition that uses the <see cref="DefReferenceType{T}"/> to resolve a stuff category def value from an object. The provided stuff category def name will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="DefReferenceType{StuffCategoryDef}"/>. This allows for easy creation of references that can access stuff category defs of objects in a fluent manner.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="defName">The name of the stuff category def to resolve from the object.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn StuffCategory<TReturn>(this IConditionOperandBuilder<TReturn> builder, string defName)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = DefReferenceType<StuffCategoryDef>.DefaultTypeName, Value = Guard.NotNull(defName, nameof(defName)) });
        /// <summary>
        /// Fluent syntax for creating a reference definition that uses the <see cref="DefReferenceType{T}"/> to resolve a special thing filter def from the def database by defName. The provided def name will be used as the value for the reference definition, and the type will be set to the default type name of the <see cref="DefReferenceType{SpecialThingFilterDef}"/>. This allows for easy creation of references that can access special thing filter defs of objects in a fluent manner.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="defName">The defName of the special thing filter def to resolve from the def database.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn SpecialThingFilter<TReturn>(this IConditionOperandBuilder<TReturn> builder, string defName)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = DefReferenceType<SpecialThingFilterDef>.DefaultTypeName, Value = Guard.NotNull(defName, nameof(defName)) });
    }
}
