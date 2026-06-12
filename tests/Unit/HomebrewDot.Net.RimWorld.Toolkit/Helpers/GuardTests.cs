using System;
using Xunit;
using Guard = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Guard;

namespace HomebrewDot.Net.Rimworld.Tests.Helpers
{
    public class GuardTests
    {
        #region Is

        [Fact]
        public void Is_WithPassingCondition_ReturnsValue()
        {
            // Arrange
            int value = 42;

            // Act
            int result = Guard.Is(value, v => v > 0);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Is_WithFailingCondition_ThrowsArgumentException()
        {
            // Arrange
            int value = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.Is(value, v => v > 0));
        }

        [Fact]
        public void Is_WithFailingConditionAndExceptionBuilder_ThrowsBuilderException()
        {
            // Arrange
            int value = -1;
            var expectedException = new InvalidOperationException("custom error");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => Guard.Is(value, v => v > 0, () => expectedException));
            Assert.Same(expectedException, ex);
        }

        [Fact]
        public void Is_WithNullCondition_ThrowsArgumentNullException()
        {
            // Arrange
            int value = 42;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Guard.Is(value, null));
        }

        [Fact]
        public void Is_WithNullValue_AndPassingCondition_ReturnsNull()
        {
            // Arrange
            string value = null;

            // Act
            string result = Guard.Is(value, v => v == null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Is_WithFailingConditionAndParameterName_IncludesNameInExceptionMessage()
        {
            // Arrange
            int value = -1;

            // Act
            var ex = Assert.Throws<ArgumentException>(() => Guard.Is(value, v => v > 0, null, "myParam"));

            // Assert
            Assert.Contains("myParam", ex.Message);
        }

        #endregion

        #region NotNull

        [Fact]
        public void NotNull_WithNonNullValue_ReturnsValue()
        {
            // Arrange
            var value = new object();

            // Act
            var result = Guard.NotNull(value, "param");

            // Assert
            Assert.Same(value, result);
        }

        [Fact]
        public void NotNull_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            object value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Guard.NotNull(value, "param"));
        }

        [Fact]
        public void NotNull_WithNullValueAndParameterName_IncludesNameInException()
        {
            // Arrange
            object value = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(value, "myParam"));

            // Assert
            Assert.Equal("myParam", ex.ParamName);
        }

        [Fact]
        public void NotNull_WithStringValue_ReturnsString()
        {
            // Arrange
            string value = "hello";

            // Act
            string result = Guard.NotNull(value, "param");

            // Assert
            Assert.Equal("hello", result);
        }

        #endregion

        #region NotNullOrEmpty

        [Fact]
        public void NotNullOrEmpty_WithValidString_ReturnsString()
        {
            // Arrange
            string value = "hello";

            // Act
            string result = Guard.NotNullOrEmpty(value, "param");

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public void NotNullOrEmpty_WithEmptyString_ThrowsArgumentException()
        {
            // Arrange
            string value = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(value, "param"));
        }

        [Fact]
        public void NotNullOrEmpty_WithNullString_ThrowsArgumentNullException()
        {
            // Arrange
            string value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrEmpty(value, "param"));
        }

        [Fact]
        public void NotNullOrEmpty_WithWhitespaceString_ReturnsString()
        {
            // Arrange
            string value = "   ";

            // Act
            string result = Guard.NotNullOrEmpty(value, "param");

            // Assert
            Assert.Equal("   ", result);
        }

        #endregion

        #region NotNullOrWhitespace

        [Fact]
        public void NotNullOrWhitespace_WithValidString_ReturnsString()
        {
            // Arrange
            string value = "hello";

            // Act
            string result = Guard.NotNullOrWhitespace(value, "param");

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public void NotNullOrWhitespace_WithWhitespaceString_ThrowsArgumentException()
        {
            // Arrange
            string value = "   ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhitespace(value, "param"));
        }

        [Fact]
        public void NotNullOrWhitespace_WithEmptyString_ThrowsArgumentException()
        {
            // Arrange
            string value = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhitespace(value, "param"));
        }

        [Fact]
        public void NotNullOrWhitespace_WithNullString_ThrowsArgumentNullException()
        {
            // Arrange
            string value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrWhitespace(value, "param"));
        }

        #endregion
    }
}
