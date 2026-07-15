using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Models;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Used by a <see cref="ISnapshotManager"/> to see if any changes have occurred to a <typeparamref name="T"/> since the last snapshot was taken. This is used to determine whether a new snapshot needs to be taken or if the current snapshot can be reused.
    /// </summary>
    /// <typeparam name="T">The type to check for changes.</typeparam>
    public interface IChangeTracker<in T> where T : class
    {
        /// <summary>
        /// Determines if any changes have occurred to the given <paramref name="current"/> value since the last snapshot was taken, as represented by the <paramref name="previous"/> indexed value. If this method returns true, a new snapshot will be taken; if it returns false, the current snapshot will be reused.
        /// </summary>
        /// <param name="current">The current value to check for changes.</param>
        /// <param name="indexed"><paramref name="current"/> indexed if it was added previously, can be null</param>
        /// <param name="metadata">The new metadata for <paramref name="current"/></param>
        /// <returns>True if changes have occurred; otherwise, false.</returns>
        bool HasChanged(T current, IIndexed<T> indexed, ref IndexMetadata metadata);
    }
    /// <summary>
    /// A <see cref="IChangeTracker{T}"/> that can compile it's check into a linq expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IChangeTrackerCompileable<in T> : IChangeTracker<T> where T : class
    {
        /// <summary>
        /// Compiles the current <see cref="IChangeTracker{T}.HasChanged(T, IIndexed{T}, ref IndexMetadata)"/> into a linq expression condition.
        /// </summary>
        /// <param name="current">Expression pointing to the current argument</param>
        /// <param name="indexed">Expression pointing to the indexed argument</param>
        /// <param name="metadata">Expression pointing to the metadata argument></param>
        /// <returns>The compiled version of <see cref="IChangeTracker{T}.HasChanged(T, IIndexed{T}, ref IndexMetadata)"/> as linq</returns>
        public Expression Compile(ParameterExpression current, ParameterExpression indexed,  Expression metadata);
    }
}
