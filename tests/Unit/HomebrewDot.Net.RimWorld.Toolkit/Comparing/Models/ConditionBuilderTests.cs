using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using RimWorld;
using Verse;
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

        [Fact]
        public void Build_WithContainsOperator_SetsContainsOperator()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Value(new List<string> { "a", "b" })
                 .With.Contains()
                 .To.Value("a"));

            // Assert
            Assert.Equal(ContainsOperatorType.DefaultTypeName, result.With);
        }

        [Fact]
        public void Build_WithContainsAndValue_SetsContainsOperatorAndRightOperand()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Value(new List<string> { "a", "b" })
                 .With.Contains("a"));

            // Assert
            Assert.Equal(ContainsOperatorType.DefaultTypeName, result.With);
            Assert.NotNull(result.To);
        }

        [Fact]
        public void Build_WithInBy_SetsNativeOperatorInOperatorArguments()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Value(6)
                 .With.InBy(NativeOperatorType.GreaterThan)
                 .To.Value(new[] { 1, 3, 7 }));

            // Assert
            var op = Assert.IsType<OperatorDef>(result.With);
            Assert.Equal(InOperatorType.DefaultTypeName, op.Type);
            Assert.Equal(NativeOperatorType.GreaterThan, op.Arguments[InOperatorType.NativeOperatorTypeKey]);
        }

        [Fact]
        public void Build_WithContainsBy_SetsNativeOperatorInOperatorArguments()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Value(new List<int> { 1, 3, 7 })
                 .With.ContainsBy(NativeOperatorType.GreaterThan)
                 .To.Value(5));

            // Assert
            var op = Assert.IsType<OperatorDef>(result.With);
            Assert.Equal(ContainsOperatorType.DefaultTypeName, op.Type);
            Assert.Equal(NativeOperatorType.GreaterThan, op.Arguments[ContainsOperatorType.NativeOperatorTypeKey]);
        }

        [Fact]
        public void Build_WithContainsByStuffCategory_ResolvesStuffCategoryDefName()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Indexed(nameof(ThingDef.stuffCategories))
                 .With.Contains()
                 .To.StuffCategory("Woody"));

            // Assert
            Assert.Equal(ContainsOperatorType.DefaultTypeName, result.With);
            var reference = Assert.IsType<ReferenceDef>(result.To);
            Assert.Equal(DefReferenceType<StuffCategoryDef>.DefaultTypeName, reference.Type);
            Assert.Equal("Woody", reference.Value);
        }

        [Fact]
        public void Build_WithNot_KeepsOperatorAndSetsInverted()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Self()
                 .With.InThingCategory("Test_Category")
                 .Not());

            // Assert
            Assert.Equal(InThingCategoryOperatorType.DefaultTypeName, result.With);
            Assert.True(result.Inverted);
            var reference = Assert.IsType<ReferenceDef>(result.To);
            Assert.Equal(DefReferenceType<ThingCategoryDef>.DefaultTypeName, reference.Type);
            Assert.Equal("Test_Category", reference.Value);
        }

        [Fact]
        public void Build_WithNot_AllowsChainingMoreConditions()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Self()
                 .With.InThingCategory("Test_Category")
                 .Not()
                 .And
                 .Compare.Value(5)
                 .With.Equal()
                 .To.Value(5));

            // Assert
            Assert.NotNull(result.Conditions);
            Assert.Equal(2, result.Conditions.Length);
            Assert.True(result.Conditions[0].Inverted);
            Assert.False(result.Conditions[1].Inverted);
        }

        [Fact]
        public void Build_WithoutNot_SetsInvertedToFalse()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Self()
                 .With.InThingCategory("Test_Category"));

            // Assert
            Assert.Equal(InThingCategoryOperatorType.DefaultTypeName, result.With);
            Assert.False(result.Inverted);
        }

        [Fact]
        public void Build_WithNotBeforeCompare_SetsInverted()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Not()
                 .Compare.Self()
                 .With.InThingCategory("Test_Category"));

            // Assert
            Assert.Equal(InThingCategoryOperatorType.DefaultTypeName, result.With);
            Assert.True(result.Inverted);
        }

        [Fact]
        public void Build_WithNotAfterWith_SetsInverted()
        {
            // Act
            var result = ConditionBuilder.Build(b =>
                b.Compare.Self()
                 .With.Not()
                 .InThingCategory("Test_Category"));

            // Assert
            Assert.Equal(InThingCategoryOperatorType.DefaultTypeName, result.With);
            Assert.True(result.Inverted);
        }

        [Fact]
        public void Build_WithNot_ToStringRendersNotBeforeOperator()
        {
            // Act
            var inverted = ConditionBuilder.Build(b =>
                b.Compare.Self()
                 .With.InThingCategory("Test_Category")
                 .Not());
            var regular = ConditionBuilder.Build(b =>
                b.Compare.Self()
                 .With.InThingCategory("Test_Category"));

            // Assert
            Assert.Contains("not InThingCategory", inverted.ToString());
            Assert.DoesNotContain("not InThingCategory", regular.ToString());
        }

        [Fact]
        public void CompareFrom_PreservesInvertedFlagOnCopiedCondition()
        {
            // Arrange
            var condition = new ConditionDef { Compare = 5, With = "eq", To = 5, Inverted = true };

            // Act
            var result = ConditionBuilder.Build(b => b.CompareFrom(condition));

            // Assert
            Assert.True(result.Inverted);
        }

        [Fact]
        public void Build_WithNotAndCompareFrom_SetsInvertedOnCopiedCondition()
        {
            // Arrange
            var condition = new ConditionDef { Compare = 5, With = "eq", To = 5, Inverted = false };

            // Act
            var result = ConditionBuilder.Build(b => b.Not().CompareFrom(condition));

            // Assert
            Assert.True(result.Inverted);
        }
    }
}
