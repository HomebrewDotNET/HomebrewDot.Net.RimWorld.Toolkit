using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator type that checks if a given object is in a specified thing category.
    /// </summary>
    public class InThingCategoryOperatorType : IOperatorType
    {
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "InThingCategory";
        /// <summary>
        /// The singleton instance of the <see cref="InThingCategoryOperatorType"/>. This can be used wherever an instance of this operator type is needed, without the need to create multiple instances since it is stateless and thread-safe.
        /// </summary>
        public static readonly InThingCategoryOperatorType Instance = new InThingCategoryOperatorType();

        private InThingCategoryOperatorType() { }

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if (left == null || right == null)
            {
                return false;
            }

            // Get thing def from left
            object instance = left;
            if(left is IIndexed<object> indexed)
            {
                instance = indexed.Value;
            }
            ThingDef thingDef = null;
            if (instance is ThingDef td)
            {
                thingDef = td;
            }
            else if (instance is Thing thing)
            {
                thingDef = thing.def;
            }
            else
            {
                return false;
            }

            // Get category def from right
            ThingCategoryDef categoryDef = null;
            if(right is ThingCategoryDef tcd)
            {
                categoryDef = tcd;
            }
            else
            {
                return false;
            }

            var result = thingDef.IsWithinCategory(categoryDef);
            return result;
        }
    }
}
