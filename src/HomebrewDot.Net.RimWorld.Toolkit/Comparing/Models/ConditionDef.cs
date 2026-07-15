using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Extensions;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Referencing;

namespace HomebrewDot.Net.Rimworld.Comparing.Models
{
    /// <summary>
    /// Definition of a condition that compares 2 references using a specific operator. 
    /// </summary>
    public class ConditionDef : IConditionDef, ICacheable
    {
        /// <inheritdoc cref="IConditionDef.Conditions"/>
        public ConditionDef[] Conditions { get; set; }
        /// <inheritdoc/>
        public bool ConditionGroupIsOr { get; set; }

        /// <inheritdoc/>
        public object Compare { get; set; }
        /// <inheritdoc/>
        public object With { get; set; }
        /// <inheritdoc/>
        public object To { get; set; }

        /// <inheritdoc/>
        public bool IsOr { get; set; }
        /// <inheritdoc/>
        IReadOnlyList<IConditionDef> IConditionDef.Conditions => Conditions;

        /// <inheritdoc cref="ConditionDef"/>
        public ConditionDef()
        {

        }

        /// <inheritdoc cref="ConditionDef"/>
        public ConditionDef(IConditionDef conditionDef)
        {
            Conditions = conditionDef.Conditions?.Select(c => new ConditionDef(c)).ToArray();
            ConditionGroupIsOr = conditionDef.ConditionGroupIsOr;
            Compare = conditionDef.Compare;
            With = conditionDef.With;
            To = conditionDef.To;
            IsOr = conditionDef.IsOr;
        }
        /// <inheritdoc/>
        public string GetCacheKey() => ToString(null, true).ToString();

        /// <summary>
        /// Converts the current condition definition to a string representation. This method builds a string that represents the condition in a human-readable format, which can be useful for debugging or logging purposes. The string representation includes the left hand side object, the operator, and the right hand side object, as well as any nested conditions if applicable. The method handles different types of objects, such as references and operators, and formats them accordingly in the resulting string.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/> to append the string representation to. If null, a new <see cref="StringBuilder"/> will be created.</param>
        /// <param name="includeTypeNames">Whether to include type names in the string representation.</param>
        /// <returns>The <see cref="StringBuilder"/> containing the string representation of the condition.</returns>
        public StringBuilder ToString(StringBuilder stringBuilder, bool includeTypeNames)
        {
            stringBuilder ??= new StringBuilder();
            var isCondition = With != null;
            if (Conditions != null && Conditions.Length > 0)
            {
                GroupToString(Conditions, stringBuilder);
                if (isCondition)
                {
                    stringBuilder.Append(ConditionGroupIsOr ? " OR " : " AND ");
                }
            }
            if (isCondition)
            {
                Compare.ToCacheKey(stringBuilder, includeTypeNames);

                stringBuilder.Append(' ');

                With.ToCacheKey(stringBuilder, includeTypeNames);

                stringBuilder.Append(' ');

                To.ToCacheKey(stringBuilder, includeTypeNames);
            }
            return stringBuilder;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null, false).ToString();
        }

        /// <summary>
        /// Converts a group of conditions into a string representation.
        /// </summary>
        /// <param name="conditions">The array of conditions to convert.</param>
        /// <param name="stringBuilder">The StringBuilder to append the string representation to.</param>
        /// <param name="conditionNextLine">Indicates whether each condition should be on a new line.</param>
        /// <param name="includeTypeNames">Indicates whether type names should be included in the string representation.</param>
        /// <returns>The StringBuilder with the appended string representation of the conditions.</returns>
        public static StringBuilder GroupToString(ConditionDef[] conditions, StringBuilder stringBuilder, bool conditionNextLine = true, bool includeTypeNames = false)
        {
            stringBuilder ??= new StringBuilder();
            stringBuilder.Append('(');
            if(conditionNextLine)
            {
                stringBuilder.AppendLine();
            }
            for (int i = 0; i < conditions.Length; i++)
            {
                var isLast = i == conditions.Length - 1;
                var condition = conditions[i];
                stringBuilder = condition.ToString(stringBuilder, includeTypeNames);
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
