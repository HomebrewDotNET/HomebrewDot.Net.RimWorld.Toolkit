using System;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using HomebrewDot.Net.Rimworld.UI;
using Verse;

namespace HomebrewDot.Net.Rimworld.Comparing.Models
{
    /// <summary>
    /// Scribeable configuration class for constructing <see cref="ConditionDef"/> instances. This class captures the necessary state to build a condition definition, including comparison and target values, operator, and reference modes.
    /// </summary>
    public class ConditionDefConfig : IExposable
    {
        // Compare field state
        /// <summary>
        /// The text value for the <see cref="ConditionDef.Compare"/> field when not in reference mode. This is used when <see cref="IsCompareReferenceMode"/> is false.
        /// </summary>
        public string CompareDefault;
        /// <summary>
        /// The reference type for the <see cref="ConditionDef.Compare"/> field when in reference mode. This is used when <see cref="IsCompareReferenceMode"/> is true.
        /// Maps to <see cref="IReference.Type"/> of the compare reference.
        /// </summary>
        public string CompareType;
        /// <summary>
        /// The reference type value for the <see cref="ConditionDef.Compare"/> field when in reference mode. This is used when <see cref="IsCompareReferenceMode"/> is true.
        /// Maps to <see cref="IReference.Value"/> of the compare reference.
        /// </summary>
        public string CompareValue;

        // To field state
        /// <summary>
        /// The text value for the <see cref="ConditionDef.To"/> field when not in reference mode. This is used when <see cref="IsToReferenceMode"/> is false.
        /// Used when <see cref="ToType"/> is <see cref="ConstantType.Text"/>.
        /// </summary>
        public string ToDefault;
        /// <summary>
        /// The numeric value for the <see cref="ConditionDef.To"/> field when not in reference mode. This is used when <see cref="IsToReferenceMode"/> is false.
        /// Used when <see cref="ToType"/> is <see cref="ConstantType.Number"/>.
        /// </summary>
        public int ToNumber;
        /// <summary>
        /// The decimal value for the <see cref="ConditionDef.To"/> field when not in reference mode. This is used when <see cref="IsToReferenceMode"/> is false.
        /// Used when <see cref="ToType"/> is <see cref="ConstantType.Decimal"/>.
        /// </summary>
        public double ToDecimal;
        /// <summary>
        /// Determines the type of the "To" value when not in reference mode. This is used when <see cref="IsToReferenceMode"/> is false to determine which of the above fields (<see cref="ToDefault"/>, <see cref="ToNumber"/>, <see cref="ToDecimal"/>) should be used to populate the <see cref="ConditionDef.To"/> reference value.
        /// </summary>
        public ConstantType ToType;
        /// <summary>
        /// The reference type for the <see cref="ConditionDef.To"/> field when in reference mode. This is used when <see cref="IsToReferenceMode"/> is true.
        /// Maps to <see cref="IReference.Type"/> of the "To" reference.
        /// </summary>
        public string ToReferenceType;
        /// <summary>
        /// The reference type value for the <see cref="ConditionDef.To"/> field when in reference mode. This is used when <see cref="IsToReferenceMode"/> is true.
        /// Maps to <see cref="IReference.Value"/> of the "To" reference.
        /// </summary>
        public string ToReferenceValue;

        // Shared
        /// <summary>
        /// Maps to <see cref="ConditionDef.With"/>. Represents the operator used for comparison in the condition definition.
        /// </summary>
        public string Operator;
        /// <summary>
        /// Maps to <see cref="ConditionDef.IsOr"/>. Indicates whether the condition should be evaluated with an "OR" logic instead of "AND" when combined with other conditions.
        /// </summary>
        public bool IsOr;

        // Derived
        /// <summary>
        /// Indicates whether the compare field should be treated as a reference. If true, the <see cref="ConditionDef.Compare"/> will be constructed as a reference using <see cref="CompareType"/> and <see cref="CompareValue"/>. If false, it will use <see cref="CompareDefault"/> as a simple text value.
        /// </summary>
        public bool IsCompareReferenceMode;
        /// <summary>
        /// Indicates whether the "To" field should be treated as a reference. If true, the <see cref="ConditionDef.To"/> will be constructed as a reference using <see cref="ToReferenceType"/> and <see cref="ToReferenceValue"/>. If false, it will use the appropriate "To" value field (<see cref="ToDefault"/>, <see cref="ToNumber"/>, <see cref="ToDecimal"/>) based on <see cref="ToType"/>.
        /// </summary>
        public bool IsToReferenceMode;

        public ConditionDefConfig()
        {
            CompareDefault = string.Empty;
            CompareType = string.Empty;
            CompareValue = string.Empty;
            ToDefault = string.Empty;
            ToNumber = 0;
            ToDecimal = 0.0;
            ToType = ConstantType.Text;
            ToReferenceType = string.Empty;
            ToReferenceValue = string.Empty;
            Operator = string.Empty;
            IsOr = false;
        }

        /// <summary>
        /// Builds a <see cref="ConditionDef"/> from this config's current state.
        /// </summary>
        public ConditionDef ToConditionDef()
        {
            object compareRef = IsCompareReferenceMode
                ? (object)new ReferenceDef { Type = CompareType ?? string.Empty, Value = CompareValue ?? string.Empty }
                : (object)new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = CompareDefault ?? string.Empty };

            object toRef;
            if (IsToReferenceMode)
            {
                toRef = new ReferenceDef { Type = ToReferenceType ?? string.Empty, Value = ToReferenceValue ?? string.Empty };
            }
            else
            {
                object rawValue;
                switch (ToType)
                {
                    case ConstantType.Number:
                        rawValue = ToNumber;
                        break;
                    case ConstantType.Decimal:
                        rawValue = ToDecimal;
                        break;
                    default:
                        rawValue = ToDefault ?? string.Empty;
                        break;
                }

                toRef = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = rawValue };
            }

            return new ConditionDef
            {
                Compare = compareRef,
                With = Operator ?? string.Empty,
                To = toRef,
                IsOr = IsOr,
            };
        }

        /// <summary>
        /// Reconstructs a <see cref="ConditionDefConfig"/> from an existing <see cref="ConditionDef"/>.
        /// </summary>
        public static ConditionDefConfig FromConditionDef(ConditionDef def)
        {
            var config = new ConditionDefConfig();

            if (def == null)
            {
                return config;
            }

            if(def.Conditions?.Length > 0)
            {
                throw new InvalidOperationException("Nested conditions are not supported in ConditionDefConfig.");
            }

            if (def.Compare is IReference compareRef)
            {
                config.CompareType = compareRef.Type;
                config.CompareValue = compareRef.Value?.ToString();
            }
            else if (def.Compare != null)
            {
                config.CompareDefault = def.Compare.ToString();
            }

            config.Operator = def.With?.ToString() ?? string.Empty;

            if (def.To is IReference toRef)
            {
                if (string.Equals(toRef.Type, ValueReferenceType.DefaultTypeName, StringComparison.Ordinal))
                {
                    config.ToDefault = toRef.Value?.ToString() ?? string.Empty;
                    config.ToType = ConstantType.Text;
                    if (toRef.Value is int n)
                    {
                        config.ToNumber = n;
                        config.ToType = ConstantType.Number;
                    }
                    else if (toRef.Value is double || toRef.Value is float)
                    {
                        config.ToDecimal = Convert.ToDouble(toRef.Value);
                        config.ToType = ConstantType.Decimal;
                    }
                }
                else
                {
                    config.ToReferenceType = toRef.Type ?? string.Empty;
                    config.ToReferenceValue = toRef.Value?.ToString() ?? string.Empty;
                }
            }
            else if (def.To != null)
            {
                config.ToDefault = def.To.ToString() ?? string.Empty;
            }

            config.IsOr = def.IsOr;
            return config;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref CompareDefault, "CompareDefault");
            Scribe_Values.Look(ref CompareType, "CompareType");
            Scribe_Values.Look(ref CompareValue, "CompareValue");
            Scribe_Values.Look(ref ToDefault, "ToDefault");
            Scribe_Values.Look(ref ToNumber, "ToNumber");
            Scribe_Values.Look(ref ToDecimal, "ToDecimal");
            Scribe_Values.Look(ref ToType, "ToType");
            Scribe_Values.Look(ref ToReferenceType, "ToReferenceType");
            Scribe_Values.Look(ref ToReferenceValue, "ToReferenceValue");
            Scribe_Values.Look(ref Operator, "Operator");
            Scribe_Values.Look(ref IsOr, "IsOr");
            Scribe_Values.Look(ref IsCompareReferenceMode, "IsCompareReferenceMode");
            Scribe_Values.Look(ref IsToReferenceMode, "IsToReferenceMode");
        }
    }
}
