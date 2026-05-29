using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Comparing;
using HomebrewDot.Net.RimWorld.Comparing.Models;

namespace HomebrewDot.Net.RimWorld.Collecting.Models
{
    /// <inheritdoc cref="ICollectionDef"/>
    public class CollectionDef : ICollectionDef
    {
        /// <inheritdoc cref="ICollectionDef.Conditions"/>
        public ConditionDef[] Conditions { get; set; }
        /// <inheritdoc cref="ICollectionDef.Inclusions"/>
        public CollectionConditionDef[] Inclusions { get; set; }
        /// <inheritdoc cref="ICollectionDef.InclusionsAreOr"/>
        public bool InclusionsAreOr { get; set; }
        /// <inheritdoc cref="ICollectionDef.Exclusions"/>
        public CollectionConditionDef[] Exclusions { get; set; }
        /// <inheritdoc/>
        IReadOnlyList<IConditionDef> ICollectionDef.Conditions => Conditions;
        /// <inheritdoc/>
        IReadOnlyList<ICollectionConditionDef> ICollectionDef.Inclusions => Inclusions;
        /// <inheritdoc/>
        IReadOnlyList<ICollectionConditionDef> ICollectionDef.Exclusions => Exclusions;
    }
    /// <inheritdoc cref="ICollectionConditionDef"/>
    public class CollectionConditionDef : ICollectionConditionDef
    {
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public bool IsOr { get; set; }
    }
}

