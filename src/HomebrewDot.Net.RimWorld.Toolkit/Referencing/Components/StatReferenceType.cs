using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing;
using RimWorld;
using Verse;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// Reference type for resolving stats from a given input, which can be either a <see cref="Def"/> or a <see cref="Thing"/>. The value is expected to be the name of the stat to resolve. The reference will first attempt to resolve the stat from the def if the input is a def, and if that fails, it will attempt to resolve it from the thing if the input is a thing. If both attempts fail, it will return null.
    /// </summary>
    public class StatReferenceType : IReferenceType
    {
        // Constants
        /// <summary>
        /// The default name for this reference type, which can be used when defining references that should be resolved using this type.
        /// </summary>
        public const string DefaultTypeName = "Stat";
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="StatReferenceType"/>. This can be used wherever an instance of this reference type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static StatReferenceType Instance { get; } = new StatReferenceType();

        private StatReferenceType()
        {

        }

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            var stat = value?.ToString();
            if (string.IsNullOrWhiteSpace(stat)) return null;
            var statDef = StatDef.Named(stat);
            if (statDef is null)
            {
                Toolkit.Helpers.Logging.LogVerbose($"StatReferenceType: Could not find stat with name '{stat}'");
                return false;
            }

            // Try def first
            Verse.Def def = null;
            if (input is IIndexed<Def> indexed)
            {
                def = indexed.Value;
            }
            else if (input is Def d)
            {
                def = d;
            }

            if (def != null)
            {
                if (def is BuildableDef buildableDef)
                {
                    return statDef.Worker?.GetValueAbstract(buildableDef);
                }
                return null;
            }

            // Try thing last
            Thing thing = null;
            if (input is IIndexed<Thing> indexedThing)
            {
                thing = indexedThing.Value;
            }
            else if (input is Thing t)
            {
                thing = t;
            }

            if (thing != null) 
            { 
                return statDef.Worker?.GetValue(thing);
            }

            return null;
        }
    }
}
