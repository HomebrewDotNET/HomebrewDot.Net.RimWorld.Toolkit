using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HomebrewDot.Net.Rimworld.Collecting
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
        /// The property or field name to compare on the current collection's items. This is used to determine which property or field of the items in the current collection should be compared against the referenced collection's items.
        /// For example, if the current collection is a list of type <see cref="Thing"/> but you want to compare against a collection of <see cref="ThingDef"/>, you could specify "def" here to compare the <see cref="Thing.def"/> property against the ThingDef collection.
        /// </summary>
        public string By { get; }
		/// <summary>
		/// Instead of including items that match the condition, exclude them. Allows you to define "Not" collections without needing to define separate collection definitions with inverted conditions.
		/// </summary>
		public bool Inverted { get; }
		/// <summary>
		/// How to compare the current collection to the previous when defined. Acts as an 'AND' / 'OR' operator between collections.
		/// </summary>
		public bool IsOr { get; }
    }
}
