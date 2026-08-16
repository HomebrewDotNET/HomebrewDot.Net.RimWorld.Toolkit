using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using Expression = System.Linq.Expressions.Expression;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator type that checks whether the left thing passes the right <see cref="SpecialThingFilterDef"/>,
    /// i.e. the exact check the vanilla stockpile UI performs for special thing filters such as "allow fresh",
    /// "allow colonist corpses", or "allow clean apparel". The left operand is the thing itself (or an indexed
    /// thing), and the right operand is the special thing filter def. Semantics are 1:1 with the game because the
    /// def's worker <see cref="SpecialThingFilterWorker.Matches(Thing)"/> is invoked directly, so per-instance
    /// state (rot stage, deadman's apparel, biocoding, pawn gender, corpse faction, ...) is evaluated exactly as
    /// the vanilla filter would, and modded defs with custom workers work unchanged.
    /// </summary>
    public class MatchesThingFilterOperatorType : IOperatorType, IOperatorTypeCompileable
    {
        /// <summary>
        /// The default name of the operator, used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "MatchesThingFilter";

        /// <summary>
        /// The singleton instance of the <see cref="MatchesThingFilterOperatorType"/>. This can be used wherever an
        /// instance of this operator type is needed, since it is stateless and thread-safe.
        /// </summary>
        public static readonly MatchesThingFilterOperatorType Instance = new MatchesThingFilterOperatorType();

        private static readonly MemberInfo WorkerClassMember = Toolkit.Helpers.Expression.GetMember<SpecialThingFilterDef, Type>(x => x.workerClass);
        private static readonly MemberInfo WorkerMember = Toolkit.Helpers.Expression.GetMember<SpecialThingFilterDef, SpecialThingFilterWorker>(x => x.Worker);
        private static readonly MethodInfo MatchesMethod = Toolkit.Helpers.Expression.GetMethod<SpecialThingFilterWorker>(x => x.Matches(default(Thing)));

        private MatchesThingFilterOperatorType() { }

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if (left == null || right == null)
            {
                return false;
            }

            // Resolve the thing from the left operand.
            object instance = left;
            if (left is IIndexed<object> indexed)
            {
                instance = indexed.Value;
            }
            if (instance is not Thing thing)
            {
                return false;
            }

            // The right operand is the special thing filter def. Defs without a worker class (a config error)
            // can never match.
            if (right is not SpecialThingFilterDef filterDef || filterDef.workerClass == null)
            {
                return false;
            }

            return filterDef.Worker?.Matches(thing) == true;
        }

        /// <inheritdoc/>
        public string GetCacheKey(Type left, Type right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            return $"{DefaultTypeName}:{left?.FullName}:{right?.FullName}";
        }

        /// <inheritdoc/>
        public Expression Compile(Expression leftValue, Type leftExpressionType, Expression rightValue, Type rightExpressionType, ParameterExpression argumentsParameter, ParameterExpression contextParameter, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            leftValue = Guard.NotNull(leftValue, nameof(leftValue));
            rightValue = Guard.NotNull(rightValue, nameof(rightValue));

            // The left expression may be typed as the object input parameter even when the reference reports a more
            // specific type (e.g. Self), so resolve the thing with a safe cast that yields null for non-things.
            var thingVariable = Expression.Variable(typeof(Thing), "thing");
            var assignThing = Expression.Assign(thingVariable, Expression.TypeAs(leftValue, typeof(Thing)));

            var defVariable = Expression.Variable(typeof(SpecialThingFilterDef), "filterDef");
            var assignDef = Expression.Assign(defVariable,
                rightExpressionType == typeof(SpecialThingFilterDef) ? rightValue : Expression.TypeAs(rightValue, typeof(SpecialThingFilterDef)));

            var rightInvalid = Expression.OrElse(
                Expression.Equal(defVariable, Expression.Constant(null, typeof(SpecialThingFilterDef))),
                Expression.Equal(Expression.MakeMemberAccess(defVariable, WorkerClassMember), Expression.Constant(null, typeof(Type))));

            var matches = Expression.Call(Expression.MakeMemberAccess(defVariable, WorkerMember), MatchesMethod, thingVariable);

            return Expression.Block(
                new[] { thingVariable, defVariable },
                assignThing,
                assignDef,
                Expression.Condition(
                    Expression.OrElse(Expression.Equal(thingVariable, Expression.Constant(null, typeof(Thing))), rightInvalid),
                    Expression.Constant(false),
                    matches));
        }
    }

    /// <summary>
    /// Fluent extensions for selecting the <see cref="MatchesThingFilterOperatorType"/> operator in conditions,
    /// e.g. <c>builder.Compare.Self().With.MatchesThingFilter().To.SpecialThingFilter("AllowFresh")</c>.
    /// </summary>
    public static class MatchesThingFilterOperatorTypeExtensions
    {
        /// <summary>
        /// Selects the MatchesThingFilter operator for the condition. The left operand (the thing) is checked
        /// against the special thing filter selected on the right side.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn MatchesThingFilter<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => Guard.NotNull(builder, nameof(builder)).Operator(MatchesThingFilterOperatorType.DefaultTypeName);

        /// <summary>
        /// Selects the MatchesThingFilter operator and the special thing filter def name to compare against.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="defName">The defName of the <see cref="SpecialThingFilterDef"/> to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn MatchesThingFilter<TReturn>(this IConditionWithBuilder<TReturn> builder, string defName) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.MatchesThingFilter().To.SpecialThingFilter(defName);

        /// <summary>
        /// Selects the MatchesThingFilter operator and the special thing filter def to compare against.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="def">The <see cref="SpecialThingFilterDef"/> to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn MatchesThingFilter<TReturn>(this IConditionWithBuilder<TReturn> builder, SpecialThingFilterDef def) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
        {
            def = Guard.NotNull(def, nameof(def));
            return builder.MatchesThingFilter().To.SpecialThingFilter(def.defName);
        }
    }
}
