using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;

namespace HomebrewDot.Net.Rimworld.Collecting
{
    /// <summary>
    /// Fluent builder interface for creating a <see cref="ICollectionDef"/> and optionally a <see cref="ICollector{T}"/> to maintain the collection.
    /// </summary>
    /// <typeparam name="TReturn">The type returned by the builder methods for fluent chaining.</typeparam>
    public interface ICollectionBuilder<TReturn> : IConditionBuilder<TReturn>, IConditionToRightBuilder<TReturn> 
        where TReturn : ICollectionBuilder<TReturn>
    {
        /// <summary>
        /// Enables the creation of a <see cref="ICollector"/> to maintain the collection defined by this builder, using the provided factory method.
        /// </summary>
        /// <param name="collectorFactory">A factory method that creates an <see cref="ICollector"/> for the collection.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn CollectWith(Func<ICollectionDef, ICollector> collectorFactory);
        /// <summary>
        /// Enables the creation of a <see cref="ICollector{T}"/> to maintain the collection defined by this builder, using the provided factory method.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collectorFactory">A factory method that creates an <see cref="ICollector{T}"/> for the collection.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn CollectWith<T>(Func<ICollectionDef, ICollector<T>> collectorFactory) where T : class;
        /// <summary>
        /// Items are included in the collection if they satisfy the conditions defined in the provided builder and any condition defined on the current collection.
        /// </summary>
        /// <param name="builder">A builder that defines additional conditions for including items in the collection.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn IncludeFrom(Func<ICollectionConditionBuilder, ICollectionConditionBuilderChain<ICollectionConditionBuilder>> builder);
        /// <summary>
        /// Items are included in the collection if they satisfy the conditions defined in the provided condition and any condition defined on the current collection.
        /// </summary>
        /// <param name="condition">The condition definition to use for including items in the collection.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn IncludeFrom(IReadOnlyList<ICollectionConditionDef> condition);
        /// <summary>
        /// Items are included in the collection if they satisfy the conditions defined in the provided builder or any condition defined on the current collection.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        TReturn OrIncludeFrom(Func<ICollectionConditionBuilder, ICollectionConditionBuilderChain<ICollectionConditionBuilder>> builder);
        /// <summary>
        /// Items are included in the collection if they satisfy the conditions defined in the provided condition or any condition defined on the current collection.
        /// </summary>
        /// <param name="condition">The condition definition to use for including items in the collection.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn OrIncludeFrom(IReadOnlyList<ICollectionConditionDef> condition);
        /// <summary>
        /// Items are excluded from the collection if they satisfy the conditions defined in the provided builder.
        /// </summary>
        /// <param name="builder">A builder that defines conditions for excluding items from the collection.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn ExcludeFrom(Func<ICollectionConditionBuilder, ICollectionConditionBuilderChain<ICollectionConditionBuilder>> builder);
        /// <summary>
        /// Items are excluded from the collection if they satisfy the conditions defined in the provided condition.
        /// </summary>
        /// <param name="condition">The condition definition to use for excluding items from the collection.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn ExcludeFrom(IReadOnlyList<ICollectionConditionDef> condition);

        /// <summary>
        /// Imports the definition of a collection from an existing <see cref="ICollectionDef"/>, allowing you to reuse previously defined collection conditions and settings.
        /// </summary>
        /// <param name="collectionDef">The collection definition to import.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        TReturn FromDef(ICollectionDef collectionDef);
    }

    /// <summary>
    /// Fluent builder interface for defining conditions on a collection property, allowing you to specify conditions that must be met for items in the collection.
    /// </summary>
    public interface ICollectionConditionBuilder
    {
        /// <summary>
        /// Begins defining a condition on a collection property with the specified name, allowing you to specify conditions that must be met for items in the collection.
        /// </summary>
        /// <param name="name">The name of the collection</param>
        /// <returns>Builder for defining conditions on the specified collection.</returns>
        ICollectionConditionBuilderAdditionalBuilder Collection(string name);
        /// <summary>
        /// Begins defining a condition on a collection property with the specified name, allowing you to specify conditions that must be met for items in the collection.
        /// Sets <see cref="ICollectionConditionDef.Inverted"/> to true for the condition.
        /// </summary>
        /// <param name="name">The name of the collection</param>
        /// <returns>Builder for defining conditions on the specified collection.</returns>
        ICollectionConditionBuilderAdditionalBuilder NotCollection(string name);
    }
    /// <summary>
    /// Fluent builder interface for defining conditions on a collection, allowing chaining of multiple conditions with 'AND' / 'OR' logic or defining a sub property path to compare on.
    /// </summary>
    public interface ICollectionConditionBuilderAdditionalBuilder : ICollectionConditionBuilderChain<ICollectionConditionBuilder>
    {
        /// <summary>
        /// Compare collection using a sub-property path, allowing you to specify conditions that must be met for items in the collection based on a specific property of those items.
        /// </summary>
        /// <param name="propertyPath">The path to the sub-property within the collection items.</param>
        /// <returns>Builder for defining conditions on the specified sub-property.</returns>
        ICollectionConditionBuilderChain<ICollectionConditionBuilder> By(string propertyPath);
    }
    /// <summary>
    /// Fluent builder interface for defining conditions on a collection, allowing chaining of multiple conditions with 'AND' / 'OR' logic.
    /// </summary>
    public interface ICollectionConditionBuilderChain<TReturn>
    {
        /// <summary>
        /// Provides access to the next condition builder in the chain, allowing you to define additional conditions that must all be satisfied (logical 'AND').
        /// </summary>
        TReturn And { get; }
        /// <summary>
        /// Provides access to the next condition builder in the chain, allowing you to define additional conditions where at least one must be satisfied (logical 'OR').
        /// </summary>
        TReturn Or { get; }
    }
    /// <summary>
    /// Fluent builder interface for creating a <see cref="ICollectionDef"/> and optionally a <see cref="ICollector{T}"/> to maintain the collection.
    /// </summary>
    public interface ICollectionBuilder : ICollectionBuilder<ICollectionBuilder>
    {
    }
}
