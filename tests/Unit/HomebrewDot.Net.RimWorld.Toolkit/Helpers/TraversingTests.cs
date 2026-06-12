using System;
using System.Collections.Generic;
using Xunit;
using TraversingHelper = HomebrewDot.Net.Rimworld.Toolkit.Helpers.Traversing;

namespace HomebrewDot.Net.Rimworld.Tests.Helpers
{
    public class TraversingTests
    {
        private sealed class NestedLeaf
        {
            public string Name { get; set; }
        }

        private sealed class NestedRoot
        {
            public NestedLeaf Leaf { get; set; }
            public NestedLeaf Secondary;
        }

        [Fact]
        public void SplitPath_WithDelimitedPath_ReturnsNormalizedSegments()
        {
            // Arrange
            const string path = " Leaf . Name ";

            // Act
            var result = TraversingHelper.SplitPath(path);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("Leaf", result[0]);
            Assert.Equal("Name", result[1]);
        }

        [Fact]
        public void TraversePath_WithDelimitedPath_ReturnsNestedValue()
        {
            // Arrange
            var instance = new NestedRoot { Leaf = new NestedLeaf { Name = "Oak" } };

            // Act
            var result = TraversingHelper.TraversePath(instance, "Leaf.Name");

            // Assert
            Assert.Equal("Oak", result);
        }

        [Fact]
        public void TraversePath_WithFieldSegment_ReturnsNestedValue()
        {
            // Arrange
            var instance = new NestedRoot { Secondary = new NestedLeaf { Name = "Pine" } };

            // Act
            var result = TraversingHelper.TraversePath(instance, "Secondary.Name");

            // Assert
            Assert.Equal("Pine", result);
        }

        [Fact]
        public void TryTraversePath_WithExistingPath_ReturnsTrueAndValue()
        {
            // Arrange
            var instance = new NestedRoot { Leaf = new NestedLeaf { Name = "Steel" } };

            // Act
            var success = TraversingHelper.TryTraversePath(instance, "Leaf.Name", out var value);

            // Assert
            Assert.True(success);
            Assert.Equal("Steel", value);
        }

        [Fact]
        public void TryTraversePath_WithMissingPath_ReturnsFalseAndNull()
        {
            // Arrange
            var instance = new NestedRoot { Leaf = new NestedLeaf { Name = "Wood" } };

            // Act
            var success = TraversingHelper.TryTraversePath(instance, "Leaf.Unknown", out var value);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }

        [Fact]
        public void TryTraversePath_WithSegmentCollection_ReturnsTrueAndValue()
        {
            // Arrange
            var instance = new NestedRoot { Leaf = new NestedLeaf { Name = "Plasteel" } };

            // Act
            var success = TraversingHelper.TryTraversePath(instance, new[] { "Leaf", "Name" }, out var value);

            // Assert
            Assert.True(success);
            Assert.Equal("Plasteel", value);
        }
    }
}
