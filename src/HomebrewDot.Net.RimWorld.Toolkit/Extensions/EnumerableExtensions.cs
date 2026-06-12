using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="IEnumerable{T}"/> and related types.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Helper method to allow for easier selecting of the correct wanted type when the source type implements multiple <see cref="IEnumerable{T}"/> interfaces.
        /// </summary>
        /// <typeparam name="T">The wanted type of the elements in the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <returns>An enumerable of the specified type.</returns>
        public static IEnumerable<T> Enumerate<T>(this IEnumerable<T> source)
        {
            return source;
        }
    }
}
