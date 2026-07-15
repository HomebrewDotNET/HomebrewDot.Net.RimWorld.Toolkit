using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="Type"/>.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Calculates the inheritance distance between the specified type and the specified base type. The inheritance distance is defined as the number of steps in the inheritance hierarchy between the two types, where a direct subclass has a distance of 1, a subclass of a subclass has a distance of 2, and so on. If the specified type does not inherit from the specified base type, the method returns -1.
        /// </summary>
        /// <param name="type">The type for which to calculate the inheritance distance.</param>
        /// <param name="baseType">The base type to compare against.</param>
        /// <returns>The inheritance distance between the type and the base type, or -1 if the type does not inherit from the base type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="type"/> or <paramref name="baseType"/> is null.</exception>
        public static int GetInheritanceDistance(this Type type, Type baseType)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            int distance = 0;
            Type currentType = type;
            while (currentType != null)
            {
                if (currentType == baseType)
                {
                    return distance;
                }
                currentType = currentType.BaseType;
                distance++;
            }
            return -1; // Not found in the inheritance hierarchy
        }

        /// <summary>
        /// Returns the actual type of the specified type, which is the underlying type if the specified type is a nullable type, or the specified type itself if it is not a nullable type. This method is useful for working with nullable types, as it allows you to easily get the underlying type without having to check if the type is nullable first.
        /// </summary>
        /// <param name="type">The type for which to get the actual type.</param>
        /// <returns>The actual type of the specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
        public static Type GetActualType(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var underlyingType = Nullable.GetUnderlyingType(type);
            return underlyingType ?? type;
        }

        /// <summary>
        /// Determines whether the specified type is a collection type, which is defined as any type that implements the <see cref="System.Collections.IEnumerable"/> interface, excluding the <see cref="string"/> type. This method is useful for checking if a type can be enumerated over, such as in a foreach loop, without having to check for specific collection types like arrays or lists.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a collection type; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
        public static bool IsCollection(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }
    }
}
