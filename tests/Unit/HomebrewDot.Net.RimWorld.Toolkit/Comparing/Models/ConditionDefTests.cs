using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Models
{
    public class ConditionDefTests
    {
        [Fact]
        public void ToCompactString_WithLeaf_RendersCompareOperatorTo()
        {
            var def = Leaf("def.label", "Equals", "hello");

            Assert.Equal("def.label Equals hello", def.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithInvertedLeaf_RendersNotBeforeOperator()
        {
            var def = Leaf("IsMeat", "Equals", "true");
            def.Inverted = true;

            Assert.Equal("IsMeat not Equals true", def.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithGroup_RendersParenthesizedJoined()
        {
            var def = new ConditionDef
            {
                Conditions = new[]
                {
                    Leaf("A", "Equals", "1"),
                    Leaf("B", "Equals", "2"),
                }
            };

            Assert.Equal("(A Equals 1 && B Equals 2)", def.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithOrSubCondition_UsesOrSeparatorAfterIt()
        {
            var orLeaf = Leaf("B", "Equals", "2");
            orLeaf.IsOr = true;

            var def = new ConditionDef
            {
                Conditions = new[]
                {
                    Leaf("A", "Equals", "1"),
                    orLeaf,
                    Leaf("C", "Equals", "3"),
                }
            };

            Assert.Equal("(A Equals 1 && B Equals 2 || C Equals 3)", def.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithGroupAndLeaf_RendersGroupThenLeaf()
        {
            var def = new ConditionDef
            {
                ConditionGroupIsOr = true,
                Conditions = new[]
                {
                    Leaf("A", "Equals", "1"),
                },
                Compare = Indexed("C"),
                With = "Equals",
                To = Value("3"),
            };

            Assert.Equal("(A Equals 1) || C Equals 3", def.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithNestedGroup_RendersNestedParentheses()
        {
            var def = new ConditionDef
            {
                Conditions = new[]
                {
                    new ConditionDef
                    {
                        Conditions = new[]
                        {
                            Leaf("A", "Equals", "1"),
                            Leaf("B", "Equals", "2"),
                        }
                    },
                    Leaf("C", "Equals", "3"),
                }
            };

            Assert.Equal("((A Equals 1 && B Equals 2) && C Equals 3)", def.ToCompactString());
        }

        [Fact]
        public void ToCompactString_WithOperatorDef_RendersOperatorTypeName()
        {
            var def = Leaf("A", null, "1");
            def.With = new OperatorDef { Type = InOperatorType.DefaultTypeName };

            Assert.Equal("A In 1", def.ToCompactString());
        }

        private static ConditionDef Leaf(string compare, string @operator, string to)
        {
            return new ConditionDef
            {
                Compare = Indexed(compare),
                With = @operator,
                To = Value(to),
            };
        }

        private static ReferenceDef Indexed(string value) =>
            new ReferenceDef { Type = IndexedReferenceType.DefaultTypeName, Value = value };

        private static ReferenceDef Value(object value) =>
            new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = value };
    }
}
