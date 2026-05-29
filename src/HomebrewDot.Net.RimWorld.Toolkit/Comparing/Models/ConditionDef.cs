using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Referencing;

namespace HomebrewDot.Net.RimWorld.Comparing.Models
{
    /// <summary>
    /// Definition of a condition that compares 2 references using a specific operator. 
    /// </summary>
    public class ConditionDef : IConditionDef
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

        /// <summary>
        /// Converts the current condition definition to a string representation. This method builds a string that represents the condition in a human-readable format, which can be useful for debugging or logging purposes. The string representation includes the left hand side object, the operator, and the right hand side object, as well as any nested conditions if applicable. The method handles different types of objects, such as references and operators, and formats them accordingly in the resulting string.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/> to append the string representation to. If null, a new <see cref="StringBuilder"/> will be created.</param>
        /// <returns>The <see cref="StringBuilder"/> containing the string representation of the condition.</returns>
        public StringBuilder ToString(StringBuilder stringBuilder)
        {
            stringBuilder ??= new StringBuilder();
            var isCondition = With != null;
            if (Conditions != null && Conditions.Length > 0)
            {
                stringBuilder.Append('(');
                for (int i = 0; i < Conditions.Length; i++)
                {
                    var isLast = i == Conditions.Length - 1;
                    var condition = Conditions[i];
                    stringBuilder = condition.ToString(stringBuilder);
                    if (!isLast)
                    {
                        stringBuilder.Append(condition.IsOr ? " OR " : " AND ");
                    }
                }
                stringBuilder.Append(')');
                if (isCondition)
                {
                    stringBuilder.Append(ConditionGroupIsOr ? " OR " : " AND ");
                }
            }
            if (isCondition)
            {
                if (Compare is null)
                {
                    stringBuilder.Append("null");
                }
                else if (Compare is IReference compareReference)
                {
                    stringBuilder.Append($"{compareReference.Value}[{compareReference.Type}]");
                }
                else
                {
                    stringBuilder.Append(Compare.ToString());
                }

                stringBuilder.Append(' ');

                if (With is null)
                {
                    stringBuilder.Append("null");
                }
                else if (With is IOperator operatorType)
                {
                    stringBuilder.Append(operatorType.Type);
                    if(operatorType.Arguments != null && operatorType.Arguments.Count > 0)
                    {
                        stringBuilder.Append('{');
                        var arguments = operatorType.Arguments.ToArray();
                        for (int i = 0; i < arguments.Length; i++)
                        {
                            var argument = arguments[i];
                            stringBuilder.Append($"{argument.Key}: {argument.Value}");
                            if (i < arguments.Length - 1)
                            {
                                stringBuilder.Append(", ");
                            }
                        }
                        stringBuilder.Append('}');
                    }
                }
                else
                {
                    stringBuilder.Append(With.ToString());
                }

                stringBuilder.Append(' ');

                if (To is null)
                {
                    stringBuilder.Append("null");
                }
                else if (To is IReference toReference)
                {
                    stringBuilder.Append($"{toReference.Value}[{toReference.Type}]");
                }
                else
                {
                    stringBuilder.Append(To.ToString());
                }
            }
            return stringBuilder;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null).ToString();
        }
    }
}
