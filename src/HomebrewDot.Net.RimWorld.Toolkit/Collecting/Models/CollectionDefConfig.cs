using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using Verse;

namespace HomebrewDot.Net.Rimworld.Collecting.Models
{
    /// <summary>
    /// Scribeable configuration class for constructing <see cref="CollectionDef"/>.
    /// </summary>
    public class CollectionDefConfig : IExposable
    {
        /// <summary>
        /// Gets or sets the list of conditions that define the collection.
        /// </summary>
        public List<ConditionDefConfig> Conditions;
        /// <summary>
        /// Gets or sets the inclusion conditions that define the collection.
        /// </summary>
        public List<CollectionConditionDefConfig> Inclusions;
        /// <inheritdoc cref="CollectionDef.InclusionsAreOr"/>
        public bool InclusionsAreOr;
        /// <summary>
        /// Gets or sets the exclusion conditions that define the collection.
        /// </summary>
        public List<CollectionConditionDefConfig> Exclusions;

        /// <summary>
        /// Creates a new instance of <see cref="CollectionDef"/> from the current configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="CollectionDef"/>.</returns>
        public CollectionDef ToDef()
        {
            return new CollectionDef
            {
                Conditions = this.Conditions?.Select(c => c.ToConditionDef())?.ToArray(),
                Inclusions = this.Inclusions?.Select(i => i.ToDef())?.ToArray(),
                InclusionsAreOr = this.InclusionsAreOr,
                Exclusions = this.Exclusions?.Select(e => e.ToDef())?.ToArray()
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="CollectionDefConfig"/> from the specified <see cref="CollectionDef"/>.
        /// </summary>
        /// <param name="collectionDef">The <see cref="CollectionDef"/> to create the configuration from.</param>
        /// <returns>A new instance of <see cref="CollectionDefConfig"/>.</returns>
        public static CollectionDefConfig From(CollectionDef collectionDef)
        {
            return new CollectionDefConfig
            {
                Conditions = collectionDef.Conditions?.Select(c => ConditionDefConfig.FromConditionDef(c))?.ToList(),
                Inclusions = collectionDef.Inclusions?.Select(i => CollectionConditionDefConfig.From(i))?.ToList(),
                InclusionsAreOr = collectionDef.InclusionsAreOr,
                Exclusions = collectionDef.Exclusions?.Select(e => CollectionConditionDefConfig.From(e))?.ToList()
            };
        }
        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Collections.Look(ref Conditions, "Conditions", LookMode.Deep);
            Scribe_Collections.Look(ref Inclusions, "Inclusions", LookMode.Deep);
            Scribe_Values.Look(ref InclusionsAreOr, "InclusionsAreOr");
            Scribe_Collections.Look(ref Exclusions, "Exclusions", LookMode.Deep);
        }
    }

    /// <summary>
    /// Scribeable configuration class for constructing <see cref="CollectionConditionDef"/>.
    /// </summary>
    public class CollectionConditionDefConfig : IExposable
    {
        /// <inheritdoc cref="CollectionConditionDef.Name"/>
        public string Name;
        /// <inheritdoc cref="CollectionConditionDef.IsOr"/>
        public bool IsOr;
        /// <inheritdoc cref="CollectionConditionDef.Inverted"/>
        public bool Inverted;
        /// <inheritdoc cref="CollectionConditionDef.By"/>
        public string By;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionConditionDefConfig"/> class.
        /// </summary>
        public CollectionConditionDefConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionConditionDefConfig"/> class by copying all fields from the specified config.
        /// </summary>
        /// <param name="other">The config to copy.</param>
        public CollectionConditionDefConfig(CollectionConditionDefConfig other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Name = other.Name;
            IsOr = other.IsOr;
            Inverted = other.Inverted;
            By = other.By;
        }

        /// <inheritdoc/>
        public void ExposeData()
        {
            Scribe_Values.Look(ref Name, "Name");
            Scribe_Values.Look(ref IsOr, "IsOr");
            Scribe_Values.Look(ref Inverted, "Inverted");
            Scribe_Values.Look(ref By, "By");
        }

        /// <summary>
        /// Creates a new instance of <see cref="CollectionConditionDef"/> from the current configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="CollectionConditionDef"/>.</returns>
        public CollectionConditionDef ToDef()
        {
            return new CollectionConditionDef
            {
                Name = this.Name,
                IsOr = this.IsOr,
                Inverted = this.Inverted,
                By = this.By
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="CollectionConditionDefConfig"/> from the specified <see cref="CollectionConditionDef"/>.
        /// </summary>
        /// <param name="collectionConditionDef">The <see cref="CollectionConditionDef"/> to create the configuration from.</param>
        /// <returns>A new instance of <see cref="CollectionConditionDefConfig"/>.</returns>
        public static CollectionConditionDefConfig From(CollectionConditionDef collectionConditionDef)
        {
            return new CollectionConditionDefConfig
            {
                Name = collectionConditionDef.Name,
                IsOr = collectionConditionDef.IsOr,
                Inverted = collectionConditionDef.Inverted,
                By = collectionConditionDef.By
            };
        }
    }
}
