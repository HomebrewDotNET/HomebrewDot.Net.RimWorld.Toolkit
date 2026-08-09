using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic.Models
{
    /// <summary>
    /// A <see cref="List{T}"/> that is pooled and can be reused to reduce memory allocations.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class PooledList<T> : PooledCollection<List<T>, T>
    {
        /// <inheritdoc cref="PooledList{T}"/>
        public PooledList() : base(new List<T>())
        {
            
        }
    }
}
