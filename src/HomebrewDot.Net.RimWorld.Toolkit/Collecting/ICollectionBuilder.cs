using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Comparing;

namespace HomebrewDot.Net.RimWorld.Collecting
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
    }
    /// <summary>
    /// Fluent builder interface for creating a <see cref="ICollectionDef"/> and optionally a <see cref="ICollector{T}"/> to maintain the collection.
    /// </summary>
    public interface ICollectionBuilder : ICollectionBuilder<ICollectionBuilder>
    {
    }
}
