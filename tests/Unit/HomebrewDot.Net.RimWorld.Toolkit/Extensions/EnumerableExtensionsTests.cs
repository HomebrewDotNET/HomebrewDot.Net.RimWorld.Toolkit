using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Extensions;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Extensions
{
    [Trait("Category", "Unit")]
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void Enumerate_GenericOverload_ReturnsSameInstance()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };

            // Act
            var result = source.Enumerate<int>();

            // Assert
            Assert.Same(source, result);
        }

        [Fact]
        public void Enumerate_NonGenericOverload_OnNull_ReturnsNull()
        {
            // Arrange
            IEnumerable source = null;

            // Act
            var result = source.Enumerate();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Enumerate_NonGenericOverload_OnArray_ReturnsObjectSequence()
        {
            // Arrange
            var source = new[] { "a", "b", "c" };

            // Act
            var result = source.Enumerate().ToArray();

            // Assert
            Assert.Equal(3, result.Length);
            Assert.Equal("a", result[0]);
            Assert.Equal("b", result[1]);
            Assert.Equal("c", result[2]);
        }

        [Fact]
        public void TryEnumerate_WithMatchingGenericType_ReturnsTrueAndTypedSequence()
        {
            // Arrange
            object source = new List<int> { 1, 2, 3 };

            // Act
            var success = source.TryEnumerate<int>(out var result);

            // Assert
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Equal(new[] { 1, 2, 3 }, result.ToArray());
        }

        [Fact]
        public void TryEnumerate_WithString_ReturnsFalse()
        {
            // Arrange
            object source = "hello";

            // Act
            var success = source.TryEnumerate<char>(out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public void TryEnumerate_WithNull_ReturnsFalse()
        {
            // Arrange
            object source = null;

            // Act
            var success = source.TryEnumerate<int>(out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public void TryEnumerate_WithEmptyArray_ReturnsTrue()
        {
            // Arrange
            object source = new int[] { };

            // Act
            var success = source.TryEnumerate<int>(out var result);

            // Assert
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void IsCollection_OnArray_ReturnsTrue()
        {
            // Arrange
            object obj = new[] { 1, 2, 3 };

            // Act
            var result = obj.IsCollection();

            // Assert
            Assert.True(result);
        }
    }
}
