using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic;

namespace HomebrewDot.Net.Rimworld.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="object"/> class.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Generates a cache key for the given object, optionally including type names.
        /// </summary>
        /// <param name="obj">The object for which to generate a cache key.</param>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/> to append the cache key to. If null, a new <see cref="StringBuilder"/> will be created.</param>
        /// <param name="includeTypeNames">Whether to include type names in the cache key.</param>
        /// <returns>The <see cref="StringBuilder"/> containing the cache key.</returns>
        public static StringBuilder ToCacheKey(this object obj, StringBuilder stringBuilder, bool includeTypeNames)
        {
            stringBuilder ??= new StringBuilder();
            if(includeTypeNames) stringBuilder.Append("(").Append(obj?.GetType().FullName ?? typeof(object).FullName).Append(") ");
            if(obj is null)
            {
                stringBuilder.Append("null");
            }
            else if(obj is ICacheable cacheable)
            {
                stringBuilder.Append(cacheable.GetCacheKey());
            }
            else if (obj is KeyValuePair<string, object> kvp)
            {
                stringBuilder.Append('{');
                stringBuilder = kvp.Key.ToCacheKey(stringBuilder, includeTypeNames);
                stringBuilder.Append(": ");
                stringBuilder = kvp.Value.ToCacheKey(stringBuilder, includeTypeNames);
                stringBuilder.Append('}');
            }
            else if (obj.TryEnumerate<object>(out var elements))
            {
                stringBuilder.Append('[');
                var elementArray = elements.ToArray();
                for(int i = 0; i < elementArray.Length; i++)
                {
                    stringBuilder = elementArray[i].ToCacheKey(stringBuilder, includeTypeNames);
                    if(i < elementArray.Length - 1)
                    {
                        stringBuilder.Append(", ");
                    }
                }
                stringBuilder.Append(']');
            }
            else
            {
                stringBuilder.Append(obj.ToString());
            }

            return stringBuilder;
        }
    }
}
