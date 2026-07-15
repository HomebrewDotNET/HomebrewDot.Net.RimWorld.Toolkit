using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Referencing;
using Verse;
using Verse.Noise;
using static RimWorld.PsychicRitualRoleDef;

namespace HomebrewDot.Net.Rimworld.Collecting.Models
{
    /// <summary>
    /// A read only version of <see cref="CollectionDef"/> that is optimized for caching and performance.
    /// </summary>
    public class StaticCollectionDef : ICollectionDef, ICacheable
    {
        // Fields
        private Lazy<string> _cacheKey;
        private string _toString;

        /// <inheritdoc cref="StaticCollectionDef"/>
        /// <param name="collectionDef">The collection definition to copy the properties from.</param>
        public StaticCollectionDef(CollectionDef collectionDef)
        {
            Conditions = collectionDef.Conditions?.Select(c => new ConditionDef(c)).ToArray();
            Inclusions = collectionDef.Inclusions?.Select(i => new CollectionConditionDef(i)).ToArray();
            InclusionsAreOr = collectionDef.InclusionsAreOr;
            Exclusions = collectionDef.Exclusions?.Select(e => new CollectionConditionDef(e)).ToArray();
            CombinedConditions = collectionDef.CombinedConditions != null ? new ConditionDef(collectionDef.CombinedConditions) : null;

            _cacheKey = new Lazy<string>(() =>
            {
                var cacheKey = collectionDef.GetCacheKey();
                using (var hasher = MD5.Create())
                {
                    var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(cacheKey));
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            });
            _toString = collectionDef.ToString();
        }

        public IReadOnlyList<IConditionDef> Conditions { get; }

        public IConditionDef CombinedConditions { get; }

        public IReadOnlyList<ICollectionConditionDef> Inclusions { get; }

        public bool InclusionsAreOr { get; }

        public IReadOnlyList<ICollectionConditionDef> Exclusions { get; }


        /// <inheritdoc/>
        public string GetCacheKey() => _cacheKey.Value;

        /// <inheritdoc/>
        public override string ToString() => _toString;
    }
}

