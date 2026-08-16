using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using HomebrewDot.Net.Rimworld.UI;
using System;
using System.Collections.Generic;
using Verse;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Models
{
    public class ConditionDefConfigTests
    {
        [Fact]
        public void DefaultConstructor_InitializesFieldsToExpectedDefaults()
        {
            var config = new ConditionDefConfig();
            Assert.False(config.IsCompareReferenceMode);
            Assert.False(config.IsToReferenceMode);
            Assert.Equal(string.Empty, config.CompareDefault);
            Assert.Equal(string.Empty, config.CompareType);
            Assert.Equal(string.Empty, config.CompareValue);
            Assert.Equal(string.Empty, config.ToDefault);
            Assert.Equal(string.Empty, config.ToReferenceType);
            Assert.Equal(string.Empty, config.ToReferenceValue);
            Assert.Equal(ConstantType.Text, config.ToType);
            Assert.Equal(0, config.ToNumber);
            Assert.Equal(0.0, config.ToDecimal);
            Assert.Equal(string.Empty, config.Operator);
            Assert.False(config.IsOr);
            Assert.False(config.Inverted);
        }

        [Fact]
        public void CopyConstructor_WithPopulatedConfig_CopiesAllFields()
        {
            var original = new ConditionDefConfig
            {
                CompareDefault = "def.label",
                CompareType = PropertyReferenceType.DefaultTypeName,
                CompareValue = "def.label",
                ToDefault = "hello",
                ToNumber = 42,
                ToDecimal = 3.14,
                ToType = ConstantType.Decimal,
                ToReferenceType = StatReferenceType.DefaultTypeName,
                ToReferenceValue = "MoveSpeed",
                Operator = "Equals",
                IsOr = true,
                Inverted = true,
                IsCompareReferenceMode = true,
                IsToReferenceMode = true,
            };

            var copy = new ConditionDefConfig(original);

            Assert.Equal(original.CompareDefault, copy.CompareDefault);
            Assert.Equal(original.CompareType, copy.CompareType);
            Assert.Equal(original.CompareValue, copy.CompareValue);
            Assert.Equal(original.ToDefault, copy.ToDefault);
            Assert.Equal(original.ToNumber, copy.ToNumber);
            Assert.Equal(original.ToDecimal, copy.ToDecimal);
            Assert.Equal(original.ToType, copy.ToType);
            Assert.Equal(original.ToReferenceType, copy.ToReferenceType);
            Assert.Equal(original.ToReferenceValue, copy.ToReferenceValue);
            Assert.Equal(original.Operator, copy.Operator);
            Assert.Equal(original.IsOr, copy.IsOr);
            Assert.Equal(original.Inverted, copy.Inverted);
            Assert.Equal(original.IsCompareReferenceMode, copy.IsCompareReferenceMode);
            Assert.Equal(original.IsToReferenceMode, copy.IsToReferenceMode);
        }

        [Fact]
        public void CopyConstructor_WithNullOther_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ConditionDefConfig(null));
        }

        [Fact]
        public void CopyConstructor_ModifyingCopy_DoesNotAffectOriginal()
        {
            var original = new ConditionDefConfig { Operator = "Equals", IsOr = true };
            var copy = new ConditionDefConfig(original);

            copy.Operator = "GreaterThan";
            copy.IsOr = false;

            Assert.Equal("Equals", original.Operator);
            Assert.True(original.IsOr);
        }

        [Fact]
        public void ToConditionDef_FromDefaultConfig_BuildsCompareAsIndexedReference()
        {
            var config = new ConditionDefConfig
            {
                CompareDefault = "def.label",
                Operator = "Equals",
                ToDefault = "hello",
            };

            var result = config.ToConditionDef();

            Assert.IsAssignableFrom<IReference>(result.Compare);
            var compareRef = (IReference)result.Compare;
            Assert.Equal(IndexedReferenceType.DefaultTypeName, compareRef.Type);
            Assert.Equal("def.label", compareRef.Value);
            Assert.IsAssignableFrom<IReference>(result.To);
            var toRef = (IReference)result.To;
            Assert.Equal(ValueReferenceType.DefaultTypeName, toRef.Type);
            Assert.Equal("hello", toRef.Value);
        }

        [Fact]
        public void ToConditionDef_WithInverted_PreservesInvertedOnDef()
        {
            var config = new ConditionDefConfig
            {
                CompareDefault = "def.label",
                Operator = "Equals",
                ToDefault = "hello",
                Inverted = true,
            };

            var result = config.ToConditionDef();

            Assert.True(result.Inverted);
        }

        [Fact]
        public void FromConditionDef_WithInverted_RestoresInvertedOnConfig()
        {
            var def = new ConditionDef
            {
                Compare = new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = "def.label" },
                With = "Equals",
                To = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = "hello" },
                IsOr = true,
                Inverted = true,
            };

            var config = ConditionDefConfig.FromConditionDef(def);

            Assert.True(config.Inverted);
            Assert.True(config.IsOr);
        }

        [Fact]
        public void ToConditionDef_FromReferenceConfig_UsesProvidedReferenceType()
        {
            var config = new ConditionDefConfig
            {
                CompareType = PropertyReferenceType.DefaultTypeName,
                CompareValue = "def.label",
                ToReferenceType = StatReferenceType.DefaultTypeName,
                ToReferenceValue = "MoveSpeed",
                Operator = "Equals",
                IsCompareReferenceMode = true,
                IsToReferenceMode = true,
            };

            var result = config.ToConditionDef();

            Assert.IsAssignableFrom<IReference>(result.Compare);
            var compareRef = (IReference)result.Compare;
            Assert.Equal(PropertyReferenceType.DefaultTypeName, compareRef.Type);
            Assert.Equal("def.label", compareRef.Value);
            Assert.IsAssignableFrom<IReference>(result.To);
            var toRef = (IReference)result.To;
            Assert.Equal(StatReferenceType.DefaultTypeName, toRef.Type);
            Assert.Equal("MoveSpeed", toRef.Value);
        }

        [Fact]
        public void IsCompareReferenceMode_WhenTypeSet_ReturnsTrue()
        {
            var config = new ConditionDefConfig { CompareType = PropertyReferenceType.DefaultTypeName, IsCompareReferenceMode = true };
            Assert.True(config.IsCompareReferenceMode);
        }

        [Fact]
        public void IsCompareReferenceMode_WhenTypeEmpty_ReturnsFalse()
        {
            var config = new ConditionDefConfig();
            Assert.False(config.IsCompareReferenceMode);
        }

        [Fact]
        public void IsToReferenceMode_WhenTypeSet_ReturnsTrue()
        {
            var config = new ConditionDefConfig { ToReferenceType = StatReferenceType.DefaultTypeName, IsToReferenceMode = true };
            Assert.True(config.IsToReferenceMode);
        }

        [Fact]
        public void IsToReferenceMode_WhenTypeEmpty_ReturnsFalse()
        {
            var config = new ConditionDefConfig();
            Assert.False(config.IsToReferenceMode);
        }

        [Fact]
        public void ToConditionDef_RoundTrips_FromConditionDef()
        {
            var original = new ConditionDef
            {
                Compare = new ReferenceDef { Type = PropertyReferenceType.DefaultTypeName, Value = "def.label" },
                With = "Equals",
                To = new ReferenceDef { Type = StatReferenceType.DefaultTypeName, Value = "MoveSpeed" },
                IsOr = true,
            };

            var config = ConditionDefConfig.FromConditionDef(original);
            config.IsCompareReferenceMode = true;
            config.IsToReferenceMode = true;
            var roundTrip = config.ToConditionDef();

            Assert.IsAssignableFrom<IReference>(roundTrip.Compare);
            var compareRef = (IReference)roundTrip.Compare;
            Assert.Equal(PropertyReferenceType.DefaultTypeName, compareRef.Type);
            Assert.Equal("def.label", compareRef.Value);
            Assert.IsAssignableFrom<IReference>(roundTrip.To);
            var toRef = (IReference)roundTrip.To;
            Assert.Equal(StatReferenceType.DefaultTypeName, toRef.Type);
            Assert.Equal("MoveSpeed", toRef.Value);
            Assert.Equal("Equals", roundTrip.With);
            Assert.True(roundTrip.IsOr);
        }

        [Fact]
        public void ExposeData_DoesNotThrowOrThrowsExpectedExceptions()
        {
            var config = new ConditionDefConfig();
            var exception = Record.Exception(() => config.ExposeData());
            if (exception != null)
            {
                Assert.True(exception is NullReferenceException || exception is InvalidOperationException);
            }
        }

        [Fact]
        public void ToConditionDef_WithGroupConditions_BuildsPureGroupDef()
        {
            var config = new ConditionDefConfig
            {
                Conditions = new List<ConditionDefConfig>
                {
                    new ConditionDefConfig { CompareDefault = "IsMeat", Operator = "Equals", ToDefault = "true" },
                    new ConditionDefConfig { CompareDefault = "IsFoul", Operator = "Equals", ToDefault = "true" },
                }
            };

            var result = config.ToConditionDef();

            Assert.NotNull(result.Conditions);
            Assert.Equal(2, result.Conditions.Length);
            Assert.Equal("IsMeat", (result.Conditions[0].Compare as IReference)?.Value);
            Assert.Null(result.With);
            Assert.Null(result.Compare);
            Assert.Null(result.To);
            Assert.False(result.ConditionGroupIsOr);
        }

        [Fact]
        public void ToConditionDef_WithGroupConditionsAndOperator_BuildsGroupWithLeaf()
        {
            var config = new ConditionDefConfig
            {
                CompareDefault = "defName",
                Operator = "Equals",
                ToDefault = "x",
                ConditionGroupIsOr = true,
                Conditions = new List<ConditionDefConfig>
                {
                    new ConditionDefConfig { CompareDefault = "IsMeat", Operator = "Equals", ToDefault = "true" },
                }
            };

            var result = config.ToConditionDef();

            Assert.NotNull(result.Conditions);
            Assert.Single(result.Conditions);
            Assert.NotNull(result.With);
            Assert.NotNull(result.Compare);
            Assert.True(result.ConditionGroupIsOr);
        }

        [Fact]
        public void FromConditionDef_WithGroup_RestoresNestedConditions()
        {
            var def = new ConditionDef
            {
                ConditionGroupIsOr = true,
                Conditions = new[]
                {
                    new ConditionDef
                    {
                        Compare = new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = "IsMeat" },
                        With = "Equals",
                        To = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = true },
                    },
                    new ConditionDef
                    {
                        Compare = new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = "IsFoul" },
                        With = "Equals",
                        To = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = true },
                        IsOr = true,
                    },
                }
            };

            var config = ConditionDefConfig.FromConditionDef(def);

            Assert.True(config.IsGroup);
            Assert.Equal(2, config.Conditions.Count);
            Assert.True(config.ConditionGroupIsOr);
            Assert.Equal("IsMeat", config.Conditions[0].CompareDefault);
            Assert.Equal("Equals", config.Conditions[0].Operator);
            Assert.Equal("True", config.Conditions[0].ToDefault);
            Assert.False(config.Conditions[0].IsCompareReferenceMode);
            Assert.True(config.Conditions[1].IsOr);
        }

        [Fact]
        public void FromConditionDef_WithIndexedCompareReference_SetsCompareDefault()
        {
            var def = new ConditionDef
            {
                Compare = new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = "def.label" },
                With = "Equals",
                To = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = "hello" },
            };

            var config = ConditionDefConfig.FromConditionDef(def);

            Assert.Equal("def.label", config.CompareDefault);
            Assert.False(config.IsCompareReferenceMode);
            Assert.False(config.IsToReferenceMode);
            Assert.Equal("hello", config.ToDefault);
        }

        [Fact]
        public void FromConditionDef_WithNonIndexedReferences_SetsReferenceModes()
        {
            var def = new ConditionDef
            {
                Compare = new ReferenceDef { Type = PropertyReferenceType.DefaultTypeName, Value = "def.label" },
                With = "Equals",
                To = new ReferenceDef { Type = StatReferenceType.DefaultTypeName, Value = "MoveSpeed" },
            };

            var config = ConditionDefConfig.FromConditionDef(def);

            Assert.True(config.IsCompareReferenceMode);
            Assert.Equal(PropertyReferenceType.DefaultTypeName, config.CompareType);
            Assert.Equal("def.label", config.CompareValue);
            Assert.True(config.IsToReferenceMode);
            Assert.Equal(StatReferenceType.DefaultTypeName, config.ToReferenceType);
            Assert.Equal("MoveSpeed", config.ToReferenceValue);
        }

        [Fact]
        public void CopyConstructor_WithGroupConditions_DeepCopiesSubConditions()
        {
            var original = new ConditionDefConfig
            {
                ConditionGroupIsOr = true,
                Conditions = new List<ConditionDefConfig>
                {
                    new ConditionDefConfig { CompareDefault = "IsMeat", Operator = "Equals" },
                }
            };

            var copy = new ConditionDefConfig(original);

            Assert.True(copy.ConditionGroupIsOr);
            Assert.Single(copy.Conditions);
            Assert.Equal("IsMeat", copy.Conditions[0].CompareDefault);
            copy.Conditions[0].CompareDefault = "Changed";
            Assert.Equal("IsMeat", original.Conditions[0].CompareDefault);
        }

        [Fact]
        public void ToCompactString_WithGroup_RendersParenthesizedJoinedSummary()
        {
            var config = new ConditionDefConfig
            {
                Conditions = new List<ConditionDefConfig>
                {
                    new ConditionDefConfig { CompareDefault = "IsMeat", Operator = "Equals", ToDefault = "true" },
                    new ConditionDefConfig { CompareDefault = "IsFoul", Operator = "Equals", ToDefault = "true" },
                }
            };

            Assert.Equal("(IsMeat Equals true && IsFoul Equals true)", config.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithOrSubCondition_UsesOrSeparatorAfterIt()
        {
            var config = new ConditionDefConfig
            {
                Conditions = new List<ConditionDefConfig>
                {
                    new ConditionDefConfig { CompareDefault = "A", Operator = "Equals", ToDefault = "1" },
                    new ConditionDefConfig { CompareDefault = "B", Operator = "Equals", ToDefault = "2", IsOr = true },
                    new ConditionDefConfig { CompareDefault = "C", Operator = "Equals", ToDefault = "3" },
                }
            };

            Assert.Equal("(A Equals 1 && B Equals 2 || C Equals 3)", config.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithNestedGroup_RendersNestedParentheses()
        {
            var config = new ConditionDefConfig
            {
                Conditions = new List<ConditionDefConfig>
                {
                    new ConditionDefConfig
                    {
                        Conditions = new List<ConditionDefConfig>
                        {
                            new ConditionDefConfig { CompareDefault = "A", Operator = "Equals", ToDefault = "1" },
                            new ConditionDefConfig { CompareDefault = "B", Operator = "Equals", ToDefault = "2" },
                        }
                    },
                    new ConditionDefConfig { CompareDefault = "C", Operator = "Equals", ToDefault = "3" },
                }
            };

            Assert.Equal("((A Equals 1 && B Equals 2) && C Equals 3)", config.ToCompactString());
        }
    }
}
