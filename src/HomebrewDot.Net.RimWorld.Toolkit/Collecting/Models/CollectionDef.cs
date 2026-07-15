using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <inheritdoc cref="ICollectionDef"/>
    public class CollectionDef : ICollectionDef, ICacheable
    {
        // Fields
        private ConditionDef _combinedConditions;

        /// <inheritdoc cref="CollectionDef"/>
        public CollectionDef()
        {

        }

        /// <inheritdoc cref="CollectionDef"/>
        /// <param name="collectionDef">The collection definition to copy the properties from.</param>
        public CollectionDef(ICollectionDef collectionDef)
        {
            Conditions = collectionDef.Conditions?.Select(c => new ConditionDef(c)).ToArray();
            Inclusions = collectionDef.Inclusions?.Select(i => new CollectionConditionDef(i)).ToArray();
            InclusionsAreOr = collectionDef.InclusionsAreOr;
            Exclusions = collectionDef.Exclusions?.Select(e => new CollectionConditionDef(e)).ToArray();
        }

        /// <inheritdoc cref="ICollectionDef.Conditions"/>
        public ConditionDef[] Conditions { get; set; }
        /// <inheritdoc cref="ICollectionDef.Inclusions"/>
        public CollectionConditionDef[] Inclusions { get; set; }
        /// <inheritdoc cref="ICollectionDef.InclusionsAreOr"/>
        public bool InclusionsAreOr { get; set; }
        /// <inheritdoc cref="ICollectionDef.Exclusions"/>
        public CollectionConditionDef[] Exclusions { get; set; }
        /// <inheritdoc cref="ICollectionDef.CombinedConditions"/>
        public IConditionDef CombinedConditions { 
            get
            {
                if(_combinedConditions != null)
                {
                    return _combinedConditions;
                }
                if(Conditions == null || Conditions.Length == 0)
                {
                    return null;
                }

                _combinedConditions = new ConditionDef()
                {
                    Conditions = Conditions,
                };
                return _combinedConditions;
            } 
        }

        /// <inheritdoc/>
        IReadOnlyList<IConditionDef> ICollectionDef.Conditions => Conditions;
        /// <inheritdoc/>
        IReadOnlyList<ICollectionConditionDef> ICollectionDef.Inclusions => Inclusions;
        /// <inheritdoc/>
        IReadOnlyList<ICollectionConditionDef> ICollectionDef.Exclusions => Exclusions;
        /// <inheritdoc/>
        public string GetCacheKey() => ToString(null, true).ToString();

        /// <summary>
        /// Converts the current collection definition to a string representation. This method builds a string that represents the collection in a human-readable format, which can be useful for debugging or logging purposes. The string representation includes the left hand side object, the operator, and the right hand side object, as well as any nested conditions if applicable. The method handles different types of objects, such as references and operators, and formats them accordingly in the resulting string.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/> to append the string representation to. If null, a new <see cref="StringBuilder"/> will be created.</param>
        /// <param name="includeTypeNames">Whether to include type names in the string representation.</param>
        /// <returns>The <see cref="StringBuilder"/> containing the string representation of the collection.</returns>
        public StringBuilder ToString(StringBuilder stringBuilder, bool includeTypeNames = false)
        {
            stringBuilder ??= new StringBuilder();

            var currentLength = stringBuilder.Length;
            if (Conditions?.Length > 0)
            {
                stringBuilder.Append("IF ");
                ConditionDef.GroupToString(Conditions, stringBuilder, includeTypeNames: includeTypeNames);
                if (Inclusions?.Length > 0)
                {
                    stringBuilder.AppendLine().Append(InclusionsAreOr ? " OR " : " AND THEN ");
                }
            }
            if(currentLength != stringBuilder.Length)
            {
                stringBuilder.AppendLine();
            }
            currentLength = stringBuilder.Length;
            if (Inclusions?.Length > 0)
            {
                stringBuilder.Append("INCLUDE FROM COLLECTIONS WHEN ");
                CollectionConditionDef.GroupToString(Inclusions, stringBuilder, false);
                stringBuilder.AppendLine();
            }
            if (currentLength != stringBuilder.Length)
            {
                stringBuilder.AppendLine();
            }
            if (Exclusions?.Length > 0)
            {
                stringBuilder.Append("EXCLUDE FROM COLLECTIONS WHEN ");
                CollectionConditionDef.GroupToString(Exclusions, stringBuilder, false);
                stringBuilder.AppendLine();
            }

            return stringBuilder;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null).ToString();
        }
    }

    /// <inheritdoc cref="ICollectionConditionDef"/>
    public class CollectionConditionDef : ICollectionConditionDef
    {
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public bool IsOr { get; set; }
        /// <inheritdoc/>
        public bool Inverted { get; set; }
        /// <inheritdoc/>
        public string By { get; set; }

        /// <inheritdoc cref="CollectionConditionDef"/>
        public CollectionConditionDef()
        {

        }

        /// <inheritdoc cref="CollectionConditionDef"/>
        /// <param name="collectionConditionDef">The collection condition definition to copy the properties from.</param>
        public CollectionConditionDef(ICollectionConditionDef collectionConditionDef)
        {
            Name = collectionConditionDef?.Name;
            IsOr = collectionConditionDef?.IsOr ?? false;
            Inverted = collectionConditionDef?.Inverted ?? false;
            By = collectionConditionDef?.By;
        }

        /// <summary>
        /// Converts the current condition definition to a string representation. This method builds a string that represents the condition in a human-readable format, which can be useful for debugging or logging purposes. The string representation includes the left hand side object, the operator, and the right hand side object, as well as any nested conditions if applicable. The method handles different types of objects, such as references and operators, and formats them accordingly in the resulting string.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/> to append the string representation to. If null, a new <see cref="StringBuilder"/> will be created.</param>
        /// <returns>The <see cref="StringBuilder"/> containing the string representation of the condition.</returns>
        public StringBuilder ToString(StringBuilder stringBuilder)
        {
            stringBuilder ??= new StringBuilder();

            if (Inverted)
            {
                stringBuilder.Append("NOT ");
            }
            stringBuilder.Append("IN ")
                         .Append(Name);
            if (!string.IsNullOrEmpty(By))
            {
                stringBuilder.Append(" BY ").Append(By);
            }

            return stringBuilder;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null).ToString();
        }
        /// <summary>
        /// Converts a group of conditions into a string representation.
        /// </summary>
        /// <param name="conditions">The array of conditions to convert.</param>
        /// <param name="stringBuilder">The StringBuilder to append the string representation to.</param>
        /// <param name="conditionNextLine">Indicates whether each condition should be on a new line.</param>
        /// <returns>The StringBuilder with the appended string representation of the conditions.</returns>
        public static StringBuilder GroupToString(CollectionConditionDef[] conditions, StringBuilder stringBuilder, bool conditionNextLine = true)
        {
            stringBuilder ??= new StringBuilder();
            stringBuilder.Append('(');
            if (conditionNextLine)
            {
                stringBuilder.AppendLine();
            }
            for (int i = 0; i < conditions.Length; i++)
            {
                var isLast = i == conditions.Length - 1;
                var condition = conditions[i];
                stringBuilder = condition.ToString(stringBuilder);
                if (!isLast)
                {
                    stringBuilder.Append(condition.IsOr ? " OR " : " AND ");
                    if (conditionNextLine)
                    {
                        stringBuilder.AppendLine();
                    }
                }
            }
            if (conditionNextLine)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append(')');
            return stringBuilder;
        }
    }
}

