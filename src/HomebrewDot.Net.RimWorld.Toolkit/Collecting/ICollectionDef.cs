using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Collecting.Models;
using HomebrewDot.Net.RimWorld.Comparing;
using HomebrewDot.Net.RimWorld.Comparing.Models;

namespace HomebrewDot.Net.RimWorld.Collecting
{
    /// <summary>
    /// Contains the conditions that defines a collection.
    /// </summary>
    public interface ICollectionDef
    {
        /// <summary>
        /// The conditions objects must pass to be included in the collection.
        /// Can be null when either <see cref="Inclusions"/> or <see cref="Exclusions"/> are used.
        /// </summary>
        IReadOnlyList<IConditionDef> Conditions { get; }
        /// <summary>
        /// Collections objects must be in to be included in the collection. Can be null when either <see cref="Conditions"/> or <see cref="Exclusions"/> are used. 
        /// If both <see cref="Conditions"/> and <see cref="Inclusions"/> are used, <see cref="InclusionsAreOr"/> determines whether the conditions are combined using OR logic.
        /// </summary>
        IReadOnlyList<ICollectionConditionDef> Inclusions { get; }
        /// <summary>
        /// Defines how to combine the conditions in <see cref="Inclusions"/> when both <see cref="Conditions"/> and <see cref="Inclusions"/> are used. 
        /// If <c>true</c>, both <see cref="Conditions"/> and <see cref="Inclusions"/> must pass.
        /// When <c>false</c>, either <see cref="Conditions"/> or <see cref="Inclusions"/> can pass for an object to be included in the collection.
        /// </summary>
        bool InclusionsAreOr { get; }
        /// <summary>
        /// When defined, objects that pass the exclusion conditions will be excluded from the collection, even if they pass the conditions in <see cref="Conditions"/> or are included in <see cref="Inclusions"/>. 
        /// Can be null when either <see cref="Conditions"/> or <see cref="Inclusions"/> are used.
        /// </summary>
        IReadOnlyList<ICollectionConditionDef> Exclusions { get; }
    }
}
