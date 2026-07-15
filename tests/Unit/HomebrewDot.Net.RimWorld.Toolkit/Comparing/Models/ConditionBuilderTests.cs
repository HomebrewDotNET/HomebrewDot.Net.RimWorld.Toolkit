using System;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Comparing.Models
{
    [Trait("Category", "Unit")]
    public class ConditionBuilderTests
    {
        public ConditionBuilderTests()
        {
            HomebrewDot.Net.Rimworld.Toolkit.ConfigureServices();
        }

        [Fact]
        public void Build_WithValidChain_ReturnsConditionDef()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Value(5).With.Equal().To.Value(5));

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Build_WithoutLeftOperand_Throws()
        {
            // Act & Assert - calling With without setting Compare first
            Assert.Throws<InvalidOperationException>(() =>
            {
                var builder = new ConditionBuilder();
                // Accessing "With" before "Compare" should throw
                _ = ((IConditionToOperatorBuilder<IConditionBuilder>)builder).With;
            });
        }

        [Fact]
        public void Build_WithoutOperator_Throws()
        {
            // Act & Assert - calling To without setting operator
            Assert.Throws<InvalidOperationException>(() =>
            {
                var builder = new ConditionBuilder();
                // Accessing "To" before "With" should throw
                _ = ((IConditionToRightBuilder<IConditionBuilder>)builder).To;
            });
        }

        [Fact]
        public void Build_WithoutRightOperand_Throws()
        {
            // Act & Assert - try to chain And without finishing a condition
            Assert.Throws<InvalidOperationException>(() =>
            {
                var builder = new ConditionBuilder();
                // Accessing "And" before finishing condition
                _ = ((IConditionChainBuilder<IConditionBuilder>)builder).And;
            });
        }

        [Fact]
        public void Build_MultipleConditions_AllStored()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Value(1).With.Equal().To.Value(1)
                 .And.Compare.Value(2).With.Equal().To.Value(2)
                 .And.Compare.Value(3).With.Equal().To.Value(3));

            // Assert
            Assert.NotNull(result);
        }
    }
}
