using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using RimWorld;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Comparing
{
    /// <summary>
    /// Fluent builder interface for creating <see cref="IConditionDef"/>(s).
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type</typeparam>
    public interface IConditionBuilder<TReturn> where TReturn : IConditionToRightBuilder<TReturn>,IConditionChainBuilder<TReturn>
    {
        /// <summary>
        /// Returns a builder for selecting the comparison operator and operands for the condition.
        /// </summary>
        IConditionCompareBuilder<TReturn> Compare { get; }
        /// <summary>
        /// Defines a group of conditions that will be evaluated together. This allows for creating complex condition chains with nested groups of conditions. The groupBuilder parameter is a function that takes a new condition builder for the group and returns the built condition chain for the group.
        /// </summary>
        /// <param name="groupBuilder">A function that takes a new condition builder for the group and returns the built condition chain for the group.</param>
        /// <returns>The fluent return type.</returns>
        IConditionChainBuilder<TReturn> Group(Func<IConditionBuilder, IConditionBuilder> groupBuilder);
    }

    /// <summary>
    /// Fluent builder interface for creating <see cref="IConditionDef"/>(s) that selects the comparison operator and operands for the condition.
    /// </summary>
    public interface IConditionBuilder : IConditionBuilder<IConditionBuilder>, IConditionToRightBuilder<IConditionBuilder>, IConditionChainBuilder<IConditionBuilder>
    {
    }

    /// <summary>
    /// Fluent builder interface for creating <see cref="IConditionDef"/>(s) that selects the comparison operand on the left side.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type</typeparam>
    public interface IConditionCompareBuilder<TReturn> : IConditionOperandBuilder<IConditionToOperatorBuilder<TReturn>> where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
    {

    }

    /// <summary>
    /// Builder for selecting the comparison operator for a condition. This is used after selecting the left operand and before selecting the right operand.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type</typeparam>
    public interface IConditionToOperatorBuilder<TReturn> where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
    {
        /// <summary>
        /// Returns a builder for selecting the comparison operator for the condition. The operator defines how the left and right operands will be compared.
        /// </summary>
        IConditionWithBuilder<TReturn> With { get; }
    }

    /// <summary>
    /// Fluent builder interface for creating <see cref="IConditionDef"/>(s) that selects the comparison operator.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type</typeparam>
    public interface IConditionWithBuilder<TReturn> where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
    {
        /// <summary>
        /// Selects the comparison operator for the condition. The operator defines how the left and right operands will be compared.
        /// </summary>
        /// <param name="operator">The comparison operator to use.</param>
        /// <returns>The fluent return type.</returns>
        TReturn Operator(IOperator @operator);
        /// <summary>
        /// Selects the comparison operator for the condition. The operator defines how the left and right operands will be compared.
        /// </summary>
        /// <param name="operator">The comparison operator to use.</param>
        /// <returns>The fluent return type.</returns>
        TReturn Operator(string @operator);
    }
    /// <summary>
    /// Builder for selecting the right operand for a condition. This is used after selecting the left operand and the comparison operator to select the value to compare against.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type</typeparam>
    public interface IConditionToRightBuilder<TReturn> : IConditionChainBuilder<TReturn> where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
    {
        /// <summary>
        /// Returns a builder for selecting the right operand for the condition. This is used after selecting the left operand and the comparison operator.
        /// </summary>
        IConditionToBuilder<TReturn> To { get; }
    }

    /// <summary>
    /// Fluent builder interface for creating <see cref="IConditionDef"/>(s) that selects the comparison operand on the right side.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type</typeparam>
    public interface IConditionToBuilder<TReturn> : IConditionOperandBuilder<TReturn>
    {

    }

    /// <summary>
    /// Builder for chaining multiple conditions together with logical operators. This is used after selecting the right operand for a condition to optionally add more conditions to the chain.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type.</typeparam>
    public interface IConditionChainBuilder<TReturn> where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
    {
        /// <summary>
        /// Continue chaining more conditions to the current condition chain with a logical AND operator. The next condition added to the chain will be combined with the previous conditions using a logical AND, meaning all conditions in the chain must be true for the entire chain to evaluate to true.
        /// </summary>
        IConditionBuilder<TReturn> And { get; }
        /// <summary>
        /// Continue chaining more conditions to the current condition chain with a logical OR operator. The next condition added to the chain will be combined with the previous conditions using a logical OR, meaning at least one condition in the chain must be true for the entire chain to evaluate to true.
        /// </summary>
        IConditionBuilder<TReturn> Or { get; }
        /// <summary>
        /// Continue chaining more conditions to the current condition chain with either a logical AND or OR operator based on the provided boolean value. If the 'and' parameter is true, the next condition will be combined with a logical AND; if false, it will be combined with a logical OR. This allows for dynamically choosing the logical operator when chaining conditions based on runtime logic or parameters.
        /// </summary>
        /// <param name="and">True to use a logical AND, false to use a logical OR.</param>
        /// <returns>The fluent return type.</returns>
        IConditionBuilder<TReturn> AndOr(bool and);
    }

    /// <summary>
    /// Fluent builder interface for creating <see cref="IConditionDef"/>(s) that selects the value to compare.
    /// </summary>
    /// <typeparam name="TReturn">The fluent return type</typeparam>
    public interface IConditionOperandBuilder<TReturn>
    {
        /// <summary>
        /// Selects a reference to an object as the operand for the condition. This is used for comparing against another reference.
        /// </summary>
        /// <param name="reference">The reference to compare against.</param>
        /// <returns>The fluent return type.</returns>
        TReturn Reference(IReference reference);
    }

    /// <summary>
    /// Static class containing extension methods for <see cref="IConditionBuilder{TReturn}"/> and related interfaces to provide additional fluent methods for building conditions."/>
    /// </summary>
    public static class ConditionBuilderExtensions
    {
        // Operands
        /// <summary>
        /// Selects a raw value as the operand for the condition. This is used for comparing against a constant value. This is a convenience method for selecting a raw value without needing to create a reference definition manually.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Value<TReturn>(this IConditionOperandBuilder<TReturn> builder, object value)
            => Guard.NotNull(builder, nameof(builder)).Reference(new ReferenceDef() { Type = ValueReferenceType.DefaultTypeName, Value = value});

        // Operators
        /// <summary>
        /// Selects a native comparison operator for the condition. The operator defines how the left and right operands will be compared. This is a convenience method for selecting common comparison operators without needing to create custom operator definitions.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="operator">The native operator to use.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Native<TReturn>(this IConditionWithBuilder<TReturn> builder, NativeOperatorType @operator) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => Guard.NotNull(builder, nameof(builder)).Operator(@operator.ToOperatorString());
        /// <summary>
        /// Selects the equality operator for the condition. This is a convenience method for selecting the equality operator without needing to create a custom operator definition.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Equal<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.Equal);
        /// <summary>
        /// Selects the equality operator for the condition and the value to compare against. This is a convenience method for selecting the equality operator and value without needing to create a custom operator definition or manually selecting the right operand after selecting the operator.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn Equal<TReturn>(this IConditionWithBuilder<TReturn> builder, object value) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Equal().To.Value(value);
        /// <summary>
        /// Selects the inequality operator for the condition. This is a convenience method for selecting the inequality operator without needing to create a custom operator definition.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn NotEqual<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.NotEqual);
        /// <summary>
        /// Selects the inequality operator for the condition and the value to compare against. This is a convenience method for selecting the inequality operator and value without needing to create a custom operator definition or manually selecting the right operand after selecting the operator.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn NotEqual<TReturn>(this IConditionWithBuilder<TReturn> builder, object value) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.NotEqual().To.Value(value);
        /// <summary>
        /// Selects the greater than operator for the condition. This is a convenience method for selecting the greater than operator without needing to create a custom operator definition.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn GreaterThan<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.GreaterThan);
        /// <summary>
        /// Selects the greater than operator for the condition and the value to compare against. This is a convenience method for selecting the greater than operator and value without needing to create a custom operator definition or manually selecting the right operand after selecting the operator.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn GreaterThan<TReturn>(this IConditionWithBuilder<TReturn> builder, object value) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.GreaterThan().To.Value(value);
        /// <summary>
        /// Selects the greater than or equal to operator for the condition. This is a convenience method for selecting the greater than or equal to operator without needing to create a custom operator definition.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn GreaterThanOrEqual<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.GreaterThanOrEqual);
        /// <summary>
        /// Selects the greater than or equal to operator for the condition and the value to compare against. This is a convenience method for selecting the greater than or equal to operator and value without needing to create a custom operator definition or manually selecting the right operand after selecting the operator.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn GreaterThanOrEqual<TReturn>(this IConditionWithBuilder<TReturn> builder, object value) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.GreaterThanOrEqual().To.Value(value);
        /// <summary>
        /// Selects the less than operator for the condition. This is a convenience method for selecting the less than operator without needing to create a custom operator definition.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn LessThan<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.LessThan);
        /// <summary>
        /// Selects the less than operator for the condition and the value to compare against. This is a convenience method for selecting the less than operator and value without needing to create a custom operator definition or manually selecting the right operand after selecting the operator.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn LessThan<TReturn>(this IConditionWithBuilder<TReturn> builder, object value) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.LessThan().To.Value(value);
        /// <summary>
        /// Selects the less than or equal to operator for the condition. This is a convenience method for selecting the less than or equal to operator without needing to create a custom operator definition.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>  
        public static TReturn LessThanOrEqual<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.LessThanOrEqual);
        /// <summary>
        /// Selects the less than or equal to operator for the condition and the value to compare against. This is a convenience method for selecting the less than or equal to operator and value without needing to create a custom operator definition or manually selecting the right operand after selecting the operator.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <param name="value">The value to compare against.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn LessThanOrEqual<TReturn>(this IConditionWithBuilder<TReturn> builder, object value) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.LessThanOrEqual().To.Value(value);
        /// <summary>
        /// Selects the true operator for the condition. This is a convenience method for selecting the true operator without needing to create a custom operator definition. The true operator evaluates to true if the left operand is considered "true" (e.g. a non-zero number, a non-empty string, etc.) and false otherwise.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn True<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.True);
        /// <summary>
        /// Selects the false operator for the condition. This is a convenience method for selecting the false operator without needing to create a custom operator definition. The false operator evaluates to true if the left operand is considered "false" (e.g. zero, an empty string, etc.) and false otherwise.
        /// </summary>
        /// <typeparam name="TReturn">The fluent return type.</typeparam>
        /// <param name="builder">The condition builder.</param>
        /// <returns>The fluent return type.</returns>
        public static TReturn False<TReturn>(this IConditionWithBuilder<TReturn> builder) where TReturn : IConditionToRightBuilder<TReturn>, IConditionChainBuilder<TReturn>
            => builder.Native(NativeOperatorType.False);
    }
}
