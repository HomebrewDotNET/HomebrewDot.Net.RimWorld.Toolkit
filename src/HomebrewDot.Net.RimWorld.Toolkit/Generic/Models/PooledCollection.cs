using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Generic.Models
{
    /// <summary>
    /// A wrapper for collections so they can be used in <see cref="Toolkit.Pool{T}"/>.
    /// </summary>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    public class PooledCollection<TCollection, TElement> : IPoolable where TCollection : ICollection<TElement>
    {
        // Properties
        /// <summary>
        /// The collection that is being pooled.
        /// </summary>
        public TCollection Collection { get; protected set; }

        /// <inheritdoc cref="PooledCollection{TCollection, TElement}"/>
        /// <param name="collection">The collection to be pooled.</param>
        public PooledCollection(TCollection collection)
        {
            Collection = Guard.NotNull(collection, nameof(collection));
        }

        /// <inheritdoc/>
        public void Reset()
        {
            Collection.Clear();
        }
    }
}
