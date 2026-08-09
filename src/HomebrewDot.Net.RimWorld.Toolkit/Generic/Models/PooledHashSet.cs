using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic.Models
{
    /// <summary>
    /// A <see cref="HashSet{T}"/> that is pooled and can be reused to reduce memory allocations.
    /// </summary>
    /// <typeparam name="T">The type of elements in the hash set.</typeparam>
    public class PooledHashSet<T> : PooledCollection<HashSet<T>, T>
    {
        /// <inheritdoc cref="PooledHashSet{T}"/>
        public PooledHashSet() : base(new HashSet<T>())
        {
            
        }
    }
}
