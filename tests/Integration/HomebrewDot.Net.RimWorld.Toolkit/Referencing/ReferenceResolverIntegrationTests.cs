using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Comparing;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Referencing
{
    [Trait("Category", "Integration")]
    public class ReferenceResolverIntegrationTests
    {
        public ReferenceResolverIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftEqualsRight_DoesNotThrow()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftEqualsRight_DoesNotThrow",
                b => b.Compare.Value(30f).With.GreaterThanOrEqual().To.Value(30f));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var exception = Record.Exception(() =>
                comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftEqualsRight_DoesNotThrow"], new object(), definitions, new Dictionary<string, object>()));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftIsGreater_ReturnsTrue()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftIsGreater_ReturnsTrue",
                b => b.Compare.Value(30).With.GreaterThanOrEqual().To.Value(20));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftIsGreater_ReturnsTrue"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftIsLess_ReturnsFalse()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftIsLess_ReturnsFalse",
                b => b.Compare.Value(10).With.GreaterThanOrEqual().To.Value(30));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftIsLess_ReturnsFalse"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Build_WithLessThanOrEqualValueShorthandWhereLeftIsLess_ReturnsTrue()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithLessThanOrEqualValueShorthandWhereLeftIsLess_ReturnsTrue",
                b => b.Compare.Value(10).With.LessThanOrEqual(15));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithLessThanOrEqualValueShorthandWhereLeftIsLess_ReturnsTrue"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftIsFloatAndRightIsInt_ReturnsTrue()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftIsFloatAndRightIsInt_ReturnsTrue",
                b => b.Compare.Value(30f).With.GreaterThanOrEqual().To.Value(20));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftIsFloatAndRightIsInt_ReturnsTrue"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.True(result);
        }
    }
}
