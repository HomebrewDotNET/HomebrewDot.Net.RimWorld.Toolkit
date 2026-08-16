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
using HomebrewDot.Net.Rimworld.Referencing.Components;

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
        public bool Inverted { get; set; }
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
            Inverted = conditionDef.Inverted;
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

                if (Inverted)
                {
                    stringBuilder.Append("not ");
                }

                With.ToCacheKey(stringBuilder, includeTypeNames);

                if(To is not null)
                {
                    stringBuilder.Append(' ');

                    To.ToCacheKey(stringBuilder, includeTypeNames);
                }
            }
            return stringBuilder;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null, false).ToString();
        }

        /// <summary>
        /// Converts the condition to a single-line, human-readable string. Groups are rendered as a parenthesized
        /// expression of their sub-conditions, e.g. "(A && B)" or "(A || B)". Sub-conditions are joined with "&&"
        /// or "||" based on the preceding condition's <see cref="IsOr"/> flag, mirroring the evaluation order.
        /// </summary>
        /// <returns>A single-line compact string representation of the condition.</returns>
        public string ToCompactString()
        {
            var stringBuilder = new StringBuilder();
            AppendCompactString(stringBuilder);
            return stringBuilder.ToString();
        }

        private void AppendCompactString(StringBuilder stringBuilder)
        {
            var hasGroup = Conditions != null && Conditions.Length > 0;
            var hasLeaf = With != null;

            if (hasGroup)
            {
                stringBuilder.Append('(');
                for (var i = 0; i < Conditions.Length; i++)
                {
                    if (i > 0)
                    {
                        stringBuilder.Append(Conditions[i - 1].IsOr ? " || " : " && ");
                    }
                    Conditions[i]?.AppendCompactString(stringBuilder);
                }
                stringBuilder.Append(')');
            }

            if (!hasLeaf)
            {
                return;
            }

            if (hasGroup)
            {
                stringBuilder.Append(ConditionGroupIsOr ? " || " : " && ");
            }

            AppendCompactValue(stringBuilder, Compare);
            stringBuilder.Append(' ');
            if (Inverted)
            {
                stringBuilder.Append("not ");
            }
            AppendCompactValue(stringBuilder, With);
            if (To != null)
            {
                stringBuilder.Append(' ');
                AppendCompactValue(stringBuilder, To);
            }
        }

        private static void AppendCompactValue(StringBuilder stringBuilder, object value)
        {
            if (value is IReference reference)
            {
                if (string.Equals(reference.Type, IndexedReferenceType.DefaultTypeName, StringComparison.Ordinal)
                    || string.Equals(reference.Type, ValueReferenceType.DefaultTypeName, StringComparison.Ordinal))
                {
                    AppendCompactRawValue(stringBuilder, reference.Value);
                }
                else
                {
                    stringBuilder.Append('[').Append(reference.Type).Append(':');
                    AppendCompactRawValue(stringBuilder, reference.Value);
                    stringBuilder.Append(']');
                }
                return;
            }

            if (value is OperatorDef operatorDef)
            {
                stringBuilder.Append(string.IsNullOrWhiteSpace(operatorDef.Type) ? "?" : operatorDef.Type);
                return;
            }

            AppendCompactRawValue(stringBuilder, value);
        }

        private static void AppendCompactRawValue(StringBuilder stringBuilder, object value)
        {
            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                stringBuilder.Append('[');
                var first = true;
                foreach (var item in enumerable)
                {
                    if (!first)
                    {
                        stringBuilder.Append(", ");
                    }
                    first = false;
                    AppendCompactValue(stringBuilder, item);
                }
                stringBuilder.Append(']');
                return;
            }

            var text = value?.ToString();
            stringBuilder.Append(string.IsNullOrEmpty(text) ? "(empty)" : text);
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
                    if (conditionNextLine)
                    {
                        stringBuilder.AppendLine();
                    }
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
