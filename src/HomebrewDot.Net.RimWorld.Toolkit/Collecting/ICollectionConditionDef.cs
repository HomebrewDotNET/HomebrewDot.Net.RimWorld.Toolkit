using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Collecting
{
    /// <summary>
    /// Defines a condition based on a collection, which can be used in the <see cref="ICollectionDef"/> to specify inclusion or exclusion criteria based on other collections.
    /// </summary>
    public interface ICollectionConditionDef
    {
        /// <summary>
        /// The name of the collection to reference.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// How to compare the current collection to the previous when defined. Acts as an 'AND' / 'OR' operator between collections.
        /// </summary>
        public bool IsOr { get; }
    }
}
