using System;
using Xunit;
using HomebrewDot.Net.RimWorld.Comparing.Components;

namespace HomebrewDot.Net.RimWorld.Tests.Comparing.Components
{
    public class OperatorTypesTests
    {
        [Fact]
        public void EqualsOperatorType_Compare_WithEqualValues_ReturnsTrue()
        {
            var result = EqualsOperatorType.Instance.Compare(5, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void NotEqualsOperatorType_Compare_WithDifferentValues_ReturnsTrue()
        {
            var result = NotEqualsOperatorType.Instance.Compare(5, 6, null, null);
            Assert.True(result);
        }

        [Fact]
        public void GreaterOperatorType_Compare_WithLeftGreaterThanRight_ReturnsTrue()
        {
            var result = GreaterOperatorType.Instance.Compare(6, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void GreaterOrEqualOperatorType_Compare_WithEqualValues_ReturnsTrue()
        {
            var result = GreaterOrEqualOperatorType.Instance.Compare(5, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void LesserOperatorType_Compare_WithLeftLessThanRight_ReturnsTrue()
        {
            var result = LesserOperatorType.Instance.Compare(4, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void LesserOrEqualOperatorType_Compare_WithEqualValues_ReturnsTrue()
        {
            var result = LesserOrEqualOperatorType.Instance.Compare(5, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void TrueOperatorType_Compare_WithBooleanTrue_ReturnsTrue()
        {
            var result = TrueOperatorType.Instance.Compare(true, null, null, null);
            Assert.True(result);
        }

        [Fact]
        public void FalseOperatorType_Compare_WithBooleanFalse_ReturnsTrue()
        {
            var result = FalseOperatorType.Instance.Compare(false, null, null, null);
            Assert.True(result);
        }

        [Fact]
        public void NullOperatorType_Compare_WithNullLeft_ReturnsTrue()
        {
            var result = NullOperatorType.Instance.Compare(null, 123, null, null);
            Assert.True(result);
        }

        [Fact]
        public void OperatorAliases_ContainExpectedSymbolAliases()
        {
            Assert.Contains("==", EqualsOperatorType.Aliases);
            Assert.Contains("!=", NotEqualsOperatorType.Aliases);
            Assert.Contains(">", GreaterOperatorType.Aliases);
            Assert.Contains(">=", GreaterOrEqualOperatorType.Aliases);
            Assert.Contains("<", LesserOperatorType.Aliases);
            Assert.Contains("<=", LesserOrEqualOperatorType.Aliases);
            Assert.Contains("true", TrueOperatorType.Aliases);
            Assert.Contains("false", FalseOperatorType.Aliases);
        }
    }
}
