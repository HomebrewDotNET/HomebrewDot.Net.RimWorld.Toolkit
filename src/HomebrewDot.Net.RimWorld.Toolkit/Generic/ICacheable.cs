using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Generic
{
    /// <summary>
    /// Interface for objects that can be cached. Implementing this interface allows an object to provide a unique cache key, which can be used to store and retrieve the object from a cache.
    /// </summary>
    public interface ICacheable
    {
        /// <summary>
        /// Gets a unique cache key for the object. This key should uniquely identify the object in the context of the cache, allowing it to be stored and retrieved efficiently.
        /// </summary>
        /// <returns>A unique cache key for the object.</returns>
        string GetCacheKey();
    }
}
