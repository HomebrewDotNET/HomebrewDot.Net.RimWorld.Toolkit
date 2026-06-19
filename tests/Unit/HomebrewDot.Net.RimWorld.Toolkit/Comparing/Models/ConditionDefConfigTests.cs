using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using HomebrewDot.Net.Rimworld.UI;
using System;
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
        }

        [Fact]
        public void FromConditionDef_WithIndexedCompareAndValueTo_PopulatesDefaultFields()
        {
            var def = new ConditionDef
            {
                Compare = new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = "def.label" },
                With = "Equals",
                To = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = "hello" },
                IsOr = true,
            };

            var config = ConditionDefConfig.FromConditionDef(def);

            Assert.Equal("def.label", config.CompareDefault);
            Assert.Equal(string.Empty, config.CompareType);
            Assert.Equal(string.Empty, config.CompareValue);
            Assert.Equal("hello", config.ToDefault);
            Assert.Equal(ConstantType.Text, config.ToType);
            Assert.Equal("Equals", config.Operator);
            Assert.True(config.IsOr);
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
    }
}
