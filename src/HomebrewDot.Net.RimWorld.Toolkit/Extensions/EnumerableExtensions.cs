using System;
using System.Collections;
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

        /// <summary>
        /// Helpers method for turning a non-generic <see cref="IEnumerable"/> into a generic <see cref="IEnumerable{T}"/> of type <see cref="object"/>. This is useful for working with collections that do not have a specific type, allowing for easier manipulation and iteration over the elements.
        /// </summary>
        /// <param name="source">The source enumerable.</param>
        /// <returns>An enumerable of type <see cref="object"/>.</returns>
        public static IEnumerable<object> Enumerate(this IEnumerable source)
        {
            return source?.Cast<object>();
        }
        /// <summary>
        /// Helper method to allow for easier selecting of the correct wanted type when the source type implements multiple <see cref="IEnumerable{T}"/> interfaces. This method attempts to cast the source object to an <see cref="IEnumerable{T}"/> of the specified type. If the cast is successful, it returns true and sets the result parameter to the casted enumerable. If the cast fails, it returns false and sets the result parameter to null.
        /// </summary>
        /// <typeparam name="T">The wanted type of the elements in the enumerable.</typeparam>
        /// <param name="source">The source object.</param>
        /// <param name="result">When this method returns, contains the casted enumerable if the cast was successful; otherwise, null.</param>
        /// <returns>True if the cast was successful; otherwise, false.</returns>
        public static bool TryEnumerate<T>(this object source, out IEnumerable<T> result)
        {
            if (source is null)
            {
                result = null;
                return false;
            }
            if(source is string)
            {
                result = null;
                return false;
            }
            if (source is IEnumerable<T> typedSource)
            {
                result = typedSource;
                return true;
            }
            else if(source is IEnumerable enumerable)
            {
                result = enumerable.OfType<T>();
                return true;
            }
            result = null;
            return false;
        }
        /// <summary>
        /// Determines whether the specified object is a collection, which is defined as any object that implements the <see cref="System.Collections.IEnumerable"/> interface, excluding the <see cref="string"/> type. This method is useful for checking if an object can be enumerated over, such as in a foreach loop, without having to check for specific collection types like arrays or lists.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is a collection; otherwise, false.</returns>
        public static bool IsCollection(this object obj)
        {
            if (obj is null) return false;
            if (obj is string) return false;
            return obj is System.Collections.IEnumerable;
        }
    }
}
