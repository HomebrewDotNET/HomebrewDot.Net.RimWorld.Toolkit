using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Maps to <see cref="ConditionDef.Inverted"/>. Indicates whether the condition should be inverted, matching when the underlying comparison would not match and vice versa.
        /// </summary>
        public bool Inverted;

        // Derived
        /// <summary>
        /// Indicates whether the compare field should be treated as a reference. If true, the <see cref="ConditionDef.Compare"/> will be constructed as a reference using <see cref="CompareType"/> and <see cref="CompareValue"/>. If false, it will use <see cref="CompareDefault"/> as a simple text value.
        /// </summary>
        public bool IsCompareReferenceMode;
        /// <summary>
        /// Indicates whether the "To" field should be treated as a reference. If true, the <see cref="ConditionDef.To"/> will be constructed as a reference using <see cref="ToReferenceType"/> and <see cref="ToReferenceValue"/>. If false, it will use the appropriate "To" value field (<see cref="ToDefault"/>, <see cref="ToNumber"/>, <see cref="ToDecimal"/>) based on <see cref="ToType"/>.
        /// </summary>
        public bool IsToReferenceMode;

        // Group state
        /// <summary>
        /// The nested conditions that make this config a group condition. When non-empty, <see cref="ToConditionDef"/>
        /// builds a condition whose <see cref="ConditionDef.Conditions"/> are the built sub-conditions.
        /// </summary>
        public List<ConditionDefConfig> Conditions;
        /// <summary>
        /// Maps to <see cref="ConditionDef.ConditionGroupIsOr"/>. Indicates whether a group combined with a leaf
        /// comparison uses OR instead of AND when both are present.
        /// </summary>
        public bool ConditionGroupIsOr;

        /// <summary>
        /// Indicates whether this config represents a group condition, i.e. it contains nested conditions.
        /// </summary>
        public bool IsGroup => Conditions != null && Conditions.Count > 0;

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
            Inverted = false;
            Conditions = new List<ConditionDefConfig>();
            ConditionGroupIsOr = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionDefConfig"/> class by copying all fields from the specified config.
        /// </summary>
        /// <param name="other">The config to copy.</param>
        public ConditionDefConfig(ConditionDefConfig other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            CompareDefault = other.CompareDefault;
            CompareType = other.CompareType;
            CompareValue = other.CompareValue;
            ToDefault = other.ToDefault;
            ToNumber = other.ToNumber;
            ToDecimal = other.ToDecimal;
            ToType = other.ToType;
            ToReferenceType = other.ToReferenceType;
            ToReferenceValue = other.ToReferenceValue;
            Operator = other.Operator;
            IsOr = other.IsOr;
            Inverted = other.Inverted;
            IsCompareReferenceMode = other.IsCompareReferenceMode;
            IsToReferenceMode = other.IsToReferenceMode;
            Conditions = other.Conditions == null
                ? null
                : other.Conditions.Where(c => c != null).Select(c => new ConditionDefConfig(c)).ToList();
            ConditionGroupIsOr = other.ConditionGroupIsOr;
        }

        /// <summary>
        /// Builds a <see cref="ConditionDef"/> from this config's current state.
        /// </summary>
        public ConditionDef ToConditionDef()
        {
            var def = new ConditionDef
            {
                IsOr = IsOr,
                Inverted = Inverted,
                ConditionGroupIsOr = ConditionGroupIsOr,
            };

            if (IsGroup)
            {
                def.Conditions = Conditions
                    .Where(c => c != null)
                    .Select(c => c.ToConditionDef())
                    .ToArray();

                // A group without an operator is a pure group: the condition has no leaf comparison of its own.
                if (string.IsNullOrWhiteSpace(Operator))
                {
                    return def;
                }
            }

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

            def.Compare = compareRef;
            def.With = Operator ?? string.Empty;
            def.To = toRef;
            return def;
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

            if (def.Conditions is { Length: > 0 })
            {
                config.Conditions = def.Conditions
                    .Where(c => c != null)
                    .Select(FromConditionDef)
                    .ToList();
                config.ConditionGroupIsOr = def.ConditionGroupIsOr;
            }

            if (def.Compare is IReference compareRef)
            {
                if (string.Equals(compareRef.Type, IndexedReferenceType.DefaultTypeName, StringComparison.Ordinal))
                {
                    config.CompareDefault = compareRef.Value?.ToString() ?? string.Empty;
                }
                else
                {
                    config.CompareType = compareRef.Type;
                    config.CompareValue = compareRef.Value?.ToString();
                    config.IsCompareReferenceMode = true;
                }
            }
            else if (def.Compare != null)
            {
                config.CompareDefault = def.Compare.ToString();
            }

            config.Operator = def.With is OperatorDef operatorDef
                ? operatorDef.Type ?? string.Empty
                : def.With?.ToString() ?? string.Empty;

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
                    config.IsToReferenceMode = true;
                }
            }
            else if (def.To != null)
            {
                config.ToDefault = def.To.ToString() ?? string.Empty;
            }

            config.IsOr = def.IsOr;
            config.Inverted = def.Inverted;
            return config;
        }

        /// <summary>
        /// Builds a single-line, human-readable representation of this config by converting it to a
        /// <see cref="ConditionDef"/> and rendering it compactly. See <see cref="ConditionDef.ToCompactString"/>.
        /// </summary>
        /// <returns>A single-line compact string representation of the condition.</returns>
        public string ToCompactString() => ToConditionDef().ToCompactString();

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
            Scribe_Values.Look(ref Inverted, "Inverted");
            Scribe_Values.Look(ref IsCompareReferenceMode, "IsCompareReferenceMode");
            Scribe_Values.Look(ref IsToReferenceMode, "IsToReferenceMode");
            Scribe_Collections.Look(ref Conditions, "Conditions", LookMode.Deep);
            Scribe_Values.Look(ref ConditionGroupIsOr, "ConditionGroupIsOr");
            if (Conditions == null)
            {
                Conditions = new List<ConditionDefConfig>();
            }
        }
    }
}
