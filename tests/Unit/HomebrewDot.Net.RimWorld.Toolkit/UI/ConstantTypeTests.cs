using Xunit;
using HomebrewDot.Net.Rimworld.UI;

namespace HomebrewDot.Net.Rimworld.Tests.UI
{
    public class ConstantTypeTests
    {
        [Fact]
        public void ConstantType_Text_HasExpectedValue()
        {
            Assert.Equal(0, (int)ConstantType.Text);
        }

        [Fact]
        public void ConstantType_Number_HasExpectedValue()
        {
            Assert.Equal(1, (int)ConstantType.Number);
        }

        [Fact]
        public void ConstantType_Decimal_HasExpectedValue()
        {
            Assert.Equal(2, (int)ConstantType.Decimal);
        }

        [Fact]
        public void ConstantType_Text_ToString_ReturnsText()
        {
            Assert.Equal("Text", ConstantType.Text.ToString());
        }

        [Fact]
        public void ConstantType_Decimal_ToString_ReturnsDecimal()
        {
            Assert.Equal("Decimal", ConstantType.Decimal.ToString());
        }

        [Fact]
        public void ConstantType_Number_ToString_ReturnsNumber()
        {
            Assert.Equal("Number", ConstantType.Number.ToString());
        }
    }
}