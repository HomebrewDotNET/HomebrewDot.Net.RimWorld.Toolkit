using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Extensions
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
    }
}
