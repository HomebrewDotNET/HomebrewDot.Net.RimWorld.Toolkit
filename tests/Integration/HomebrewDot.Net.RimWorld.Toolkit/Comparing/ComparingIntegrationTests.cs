using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.ComparingIntegration
{
    [Trait("Category", "Integration")]
    public class ComparingIntegrationTests
    {
        public ComparingIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        private static Comparator BuildComparator()
        {
            var referenceTypes = Toolkit.Services.GetAllNamed<IReferenceType>();
            var referenceResolver = new ReferenceResolver(referenceTypes);
            var operatorTypes = Toolkit.Services.GetAllNamed<IOperatorType>();
            return new Comparator(referenceResolver, operatorTypes);
        }

        private static ConditionDef BuildCondition(decimal left, string opName, decimal right)
        {
            return ConditionBuilder.Build(b =>
                b.Compare.Value(left).With.Operator(opName).To.Value(right));
        }

        [Fact]
        public void Comparator_Evaluate_WithEqualsOperator_ReturnsTrueForMatch()
        {
            var comparator = BuildComparator();
            var condition = BuildCondition(5, EqualsOperatorType.DefaultTypeName, 5);
            var result = comparator.Compare(null, condition, null);
            Assert.True(result);
        }

        [Fact]
        public void Comparator_Evaluate_WithNotEqualsOperator_ReturnsTrueForMismatch()
        {
            var comparator = BuildComparator();
            var condition = BuildCondition(5, NotEqualsOperatorType.DefaultTypeName, 6);
            var result = comparator.Compare(null, condition, null);
            Assert.True(result);
        }

        [Fact]
        public void Comparator_Evaluate_WithGreaterOperator_ReturnsTrueForLesserValue()
        {
            var comparator = BuildComparator();
            var condition = BuildCondition(3, GreaterOperatorType.DefaultTypeName, 5);
            var result = comparator.Compare(null, condition, null);
            Assert.False(result);
        }

        [Fact]
        public void Comparator_Evaluate_WithNullOperator_ReturnsTrueWhenNull()
        {
            var entity = new Tentity();
            var condition = ConditionBuilder.Build(b =>
                b.Compare.Indexed("Text").With.Null());
            var comparator = BuildComparator();
            var result = comparator.Compare(entity, condition, null);
            Assert.True(result);
        }

        [Fact]
        public void Comparator_Evaluate_WithNotNullOperator_ReturnsTrueWhenNotNull()
        {
            var entity = new Tentity { Text = "hello" };
            var condition = ConditionBuilder.Build(b =>
                b.Compare.Indexed("Text").With.NotNull());
            var comparator = BuildComparator();
            var result = comparator.Compare(entity, condition, null);
            Assert.True(result);
        }
    }
}
