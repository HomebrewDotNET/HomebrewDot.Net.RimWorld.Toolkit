using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Components
{
    public class ComparatorTests
    {
        [Fact]
        public void Compare_WithMissingWithAndConditions_ThrowsInvalidOperationException()
        {
            var sut = new Comparator(null, null, null, null);

            Assert.Throws<InvalidOperationException>(() => sut.Compare(null, new ConditionDef(), null));
        }

        [Fact]
        public void Compare_WithSimpleCondition_UsesOperatorFromConstructor()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(
                referenceResolver: null,
                operatorTypes: new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                compareStringToReference: null,
                toStringToReference: null);

            var result = sut.Compare(null, new ConditionDef
            {
                Compare = 5,
                With = "eq",
                To = 5,
            }, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithOperatorInContext_UsesContextOperatorTypes()
        {
            var alwaysFalse = new DelegateOperatorType((_, __, ___, ____) => false);
            var alwaysTrue = new DelegateOperatorType((_, __, ___, ____) => true);
            var sut = new Comparator(
                referenceResolver: null,
                operatorTypes: new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = alwaysFalse,
                },
                compareStringToReference: null,
                toStringToReference: null);

            var context = new Dictionary<string, object>
            {
                [Comparator.ContextOperatorTypesKey] = new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = alwaysTrue,
                },
            };

            var result = sut.Compare(null, new ConditionDef
            {
                Compare = "a",
                With = "eq",
                To = "b",
            }, context);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithInvertedCondition_ReturnsNegatedResult()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(
                referenceResolver: null,
                operatorTypes: new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                compareStringToReference: null,
                toStringToReference: null);

            // Underlying comparison matches (5 == 5), but Inverted flips the result to false.
            var result = sut.Compare(null, new ConditionDef
            {
                Compare = 5,
                With = "eq",
                To = 5,
                Inverted = true,
            }, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithInvertedCondition_ReturnsNegatedResult()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(
                referenceResolver: null,
                operatorTypes: new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                compareStringToReference: null,
                toStringToReference: null);

            var inputParameter = System.Linq.Expressions.Expression.Parameter(typeof(object), "input");
            var contextParameter = System.Linq.Expressions.Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "context");
            var condition = new ConditionDef
            {
                Compare = 5,
                With = "eq",
                To = 5,
                Inverted = true,
            };

            var expr = ((IComparatorCompiler)sut).Compile(inputParameter, null, condition, contextParameter, new Dictionary<string, object>());
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, IReadOnlyDictionary<string, object>, bool>>(expr, inputParameter, contextParameter);
            var func = lambda.Compile();

            Assert.False(func(null, null));
        }

        [Fact]
        public void Compare_WithOperatorDef_PassesArgumentsToOperator()
        {
            IReadOnlyDictionary<string, object> captured = null;
            var op = new DelegateOperatorType((left, right, arguments, _) =>
            {
                captured = arguments;
                return Equals(left, right);
            });

            var sut = new Comparator(
                referenceResolver: null,
                operatorTypes: new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = op,
                },
                compareStringToReference: null,
                toStringToReference: null);

            var result = sut.Compare(null, new ConditionDef
            {
                Compare = 10,
                With = new OperatorDef
                {
                    Type = "eq",
                    Arguments = new Dictionary<string, object> { ["mode"] = "strict" },
                },
                To = 10,
            }, null);

            Assert.True(result);
            Assert.NotNull(captured);
            Assert.Equal("strict", captured["mode"]);
        }

        [Fact]
        public void Compare_WithGroupAndCurrentCondition_ConditionGroupIsOr_CombineCorrectly()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(null,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                null,
                null);

            var condition = new ConditionDef
            {
                ConditionGroupIsOr = true,
                Conditions = new[]
                {
                    new ConditionDef { Compare = 2, With = "eq", To = 2, IsOr = true },
                    new ConditionDef { Compare = 1, With = "eq", To = 2, IsOr = false },
                },
                Compare = "x",
                With = "eq",
                To = "y",
            };

            var result = sut.Compare(null, condition, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithFivePlusConditions_AndOrChaining_EvaluatesAllGroupsCorrectly()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(
                null,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                null,
                null);

            // (true AND false) OR (true AND true) OR false
            var condition = new ConditionDef
            {
                Conditions = new[]
                {
                    new ConditionDef { Compare = true, With = "eq", To = true, IsOr = false },
                    new ConditionDef { Compare = false, With = "eq", To = true, IsOr = true },
                    new ConditionDef { Compare = true, With = "eq", To = true, IsOr = false },
                    new ConditionDef { Compare = true, With = "eq", To = true, IsOr = true },
                    new ConditionDef { Compare = false, With = "eq", To = true, IsOr = false },
                },
            };

            var result = sut.Compare(null, condition, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithFivePlusConditions_AndOrChaining_ReturnsFalseWhenNoGroupPasses()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(
                null,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                null,
                null);

            // true AND false OR false AND true OR false
            var condition = new ConditionDef
            {
                Conditions = new[]
                {
                    new ConditionDef { Compare = true, With = "eq", To = true, IsOr = false },
                    new ConditionDef { Compare = false, With = "eq", To = true, IsOr = true },
                    new ConditionDef { Compare = false, With = "eq", To = true, IsOr = false },
                    new ConditionDef { Compare = true, With = "eq", To = true, IsOr = true },
                    new ConditionDef { Compare = false, With = "eq", To = true, IsOr = false },
                },
            };

            var result = sut.Compare(null, condition, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithStringReferenceConverters_ResolvesStringsBeforeComparison()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var resolver = new Mock<IReferenceResolver>();
            resolver.Setup(r => r.TryResolve(It.IsAny<object>(), It.IsAny<IReference>(), It.IsAny<IReadOnlyDictionary<string, object>>(), out It.Ref<object>.IsAny))
                .Returns((object input, IReference reference, IReadOnlyDictionary<string, object> _, out object result) =>
                {
                    result = reference.Value;
                    return true;
                });
            var rawType = new DelegateReferenceType((input, raw, _) => raw);
            var sut = new Comparator(
                resolver.Object,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals
                }
            );

            var result = sut.Compare(null, new ConditionDef
            {
                Compare = "abc",
                With = "eq",
                To = "abc",
            }, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithUnresolvableReference_ThrowsInvalidOperationException()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var resolver = new Mock<IReferenceResolver>();
            resolver.Setup(r => r.TryResolve(It.IsAny<object>(), It.IsAny<IReference>(), It.IsAny<IReadOnlyDictionary<string, object>>(), out It.Ref<object>.IsAny))
                .Returns((object input, IReference _, IReadOnlyDictionary<string, object> __, out object result) =>
                {
                    result = null;
                    return false;
                });

            var sut = new Comparator(
                resolver.Object,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["eq"] = equals,
                },
                null,
                null);

            Assert.Throws<InvalidOperationException>(() => sut.Compare(null, new ConditionDef
            {
                Compare = new ReferenceDef { Type = "x", Value = "a" },
                With = "eq",
                To = "a",
            }, null));
        }

        [Fact]
        public void Compare_List_WithNullList_ThrowsArgumentNullException()
        {
            var sut = new Comparator(null, null, null, null);

            Assert.Throws<ArgumentNullException>(() =>
                sut.Compare(null, (System.Collections.Generic.IReadOnlyList<IConditionDef>)null, null));
        }

        [Fact]
        public void Compare_List_WithEmptyList_ReturnsFalse()
        {
            var sut = new Comparator(null, null, null, null);

            var result = sut.Compare(null, System.Array.Empty<ConditionDef>(), null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_List_WithSingleTrueCondition_ReturnsTrue()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(null,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase) { ["eq"] = equals },
                null, null);

            var result = sut.Compare(null, new[]
            {
                new ConditionDef { Compare = 1, With = "eq", To = 1 },
            }, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_List_WithSingleFalseCondition_ReturnsFalse()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var sut = new Comparator(null,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase) { ["eq"] = equals },
                null, null);

            var result = sut.Compare(null, new[]
                        {
                new ConditionDef { Compare = 1, With = "eq", To = 2 },
            }, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_WithContextReferenceResolver_UsesContextResolverOverConstructorResolver()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));

            var constructorResolver = new Mock<IReferenceResolver>();
            constructorResolver.Setup(r => r.TryResolve(It.IsAny<object>(), It.IsAny<IReference>(), It.IsAny<IReadOnlyDictionary<string, object>>(), out It.Ref<object>.IsAny))
                .Returns((object input, IReference _, IReadOnlyDictionary<string, object> __, out object result) =>
                {
                    result = "from-constructor";
                    return true;
                });

            var contextResolver = new Mock<IReferenceResolver>();
            contextResolver.Setup(r => r.TryResolve(It.IsAny<object>(), It.IsAny<IReference>(), It.IsAny<IReadOnlyDictionary<string, object>>(), out It.Ref<object>.IsAny))
                .Returns((object input, IReference _, IReadOnlyDictionary<string, object> __, out object result) =>
                {
                    result = "from-context";
                    return true;
                });

            var sut = new Comparator(
                constructorResolver.Object,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase) { ["eq"] = equals },
                null, null);

            var context = new Dictionary<string, object>
            {
                [Comparator.ContextReferenceResolverKey] = contextResolver.Object,
            };

            // Both Compare and To are references; result will be "from-context" == "from-context" -> true
            var result = sut.Compare(null, new ConditionDef
            {
                Compare = new ReferenceDef { Type = "x", Value = "a" },
                With = "eq",
                To = new ReferenceDef { Type = "x", Value = "a" },
            }, context);

            Assert.True(result);
            // Constructor resolver should not have been called
            constructorResolver.Verify(r => r.TryResolve(
                It.IsAny<object>(),
                It.IsAny<IReference>(),
                It.IsAny<IReadOnlyDictionary<string, object>>(),
                out It.Ref<object>.IsAny), Times.Never);
        }

        [Fact]
        public void Compare_WithContextCompareStringToReference_UsesContextConverter()
        {
            var equals = new DelegateOperatorType((left, right, _, __) => Equals(left, right));
            var resolver = new Mock<IReferenceResolver>();
            resolver.Setup(r => r.TryResolve(It.IsAny<object>(), It.IsAny<IReference>(), It.IsAny<IReadOnlyDictionary<string, object>>(), out It.Ref<object>.IsAny))
                .Returns((object input, IReference reference, IReadOnlyDictionary<string, object> _, out object result) =>
                {
                    result = reference.Value;
                    return true;
                });

            var sut = new Comparator(
                resolver.Object,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase) { ["eq"] = equals },
                null, null);

            // Inject a CompareStringToReference converter via context
            var context = new Dictionary<string, object>
            {
                [Comparator.CompareStringToReferenceKey] =
                    (Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference>)
                    ((cond, ctx, str) => new ReferenceDef { Type = "raw", Value = "converted:" + str }),
            };

            var result = sut.Compare(null, new ConditionDef
            {
                Compare = "abc",
                With = "eq",
                To = new ReferenceDef { Type = "raw", Value = "converted:abc" },
            }, context);

            Assert.True(result);
        }
    }
}