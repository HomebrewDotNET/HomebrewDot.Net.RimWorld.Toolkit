using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using Moq;
using RimWorld;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Components
{
    /// <summary>
    /// Regression coverage for comparing enum values (e.g. <see cref="QualityCategory"/> resolved from a comp
    /// such as <c>CompQuality|Quality</c>) against numeric values. Previously the native operators could not
    /// resolve an operator for enum operands and the <see cref="IComparable"/> fallback threw
    /// <see cref="ArgumentException"/> when the other operand was a numeric type of a different kind
    /// (e.g. <c>Byte.CompareTo(Int32)</c>), so a filter such as <c>CompQuality|Quality LessThan 2</c> never
    /// matched anything. The fix coerces enum operands to a comparable numeric type.
    /// </summary>
    public class EnumComparisonOperatorTests
    {
        [Theory]
        [InlineData(QualityCategory.Awful, true)]
        [InlineData(QualityCategory.Poor, true)]
        [InlineData(QualityCategory.Normal, false)]
        [InlineData(QualityCategory.Good, false)]
        [InlineData(QualityCategory.Excellent, false)]
        public void LesserOperatorType_Compare_WithQualityCategoryAndIntTwo_MatchesUnderNormal(QualityCategory quality, bool expected)
        {
            var result = LesserOperatorType.Instance.Compare(quality, 2, null, null);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void LesserOperatorType_Compare_WithEnumLeftAndEnumRight_ReturnsTrue()
        {
            var result = LesserOperatorType.Instance.Compare(SampleQuality.Poor, SampleQuality.Normal, null, null);

            Assert.True(result);
        }

        [Fact]
        public void LesserOperatorType_Compare_WithEnumLeftAndIntRight_ReturnsTrue()
        {
            var result = LesserOperatorType.Instance.Compare(SampleQuality.Awful, 2, null, null);

            Assert.True(result);
        }

        [Fact]
        public void LesserOperatorType_Compare_WithEnumLeftAndIntRightAboveValue_ReturnsFalse()
        {
            var result = LesserOperatorType.Instance.Compare(SampleQuality.Good, 2, null, null);

            Assert.False(result);
        }

        [Fact]
        public void LesserOperatorType_Compare_WithIntLeftAndEnumRight_ReturnsTrue()
        {
            var result = LesserOperatorType.Instance.Compare(1, SampleQuality.Normal, null, null);

            Assert.True(result);
        }

        [Fact]
        public void GreaterOperatorType_Compare_WithEnumLeftAndIntRight_ReturnsTrue()
        {
            var result = GreaterOperatorType.Instance.Compare(SampleQuality.Good, 2, null, null);

            Assert.True(result);
        }

        [Fact]
        public void GreaterOperatorType_Compare_WithEnumLeftAndIntRightBelowValue_ReturnsFalse()
        {
            var result = GreaterOperatorType.Instance.Compare(SampleQuality.Awful, 2, null, null);

            Assert.False(result);
        }

        [Fact]
        public void GreaterOrEqualOperatorType_Compare_WithEnumLeftAndIntRightEqual_ReturnsTrue()
        {
            var result = GreaterOrEqualOperatorType.Instance.Compare(SampleQuality.Normal, 2, null, null);

            Assert.True(result);
        }

        [Fact]
        public void LesserOrEqualOperatorType_Compare_WithEnumLeftAndIntRightEqual_ReturnsTrue()
        {
            var result = LesserOrEqualOperatorType.Instance.Compare(SampleQuality.Normal, 2, null, null);

            Assert.True(result);
        }

        [Fact]
        public void EqualsOperatorType_Compare_WithEnumLeftAndIntRightEqual_ReturnsTrue()
        {
            var result = EqualsOperatorType.Instance.Compare(SampleQuality.Normal, 2, null, null);

            Assert.True(result);
        }

        [Fact]
        public void NotEqualsOperatorType_Compare_WithEnumLeftAndIntRightEqual_ReturnsFalse()
        {
            var result = NotEqualsOperatorType.Instance.Compare(SampleQuality.Normal, 2, null, null);

            Assert.False(result);
        }

        [Fact]
        public void LesserOperatorType_GetCacheKey_WithEnumLeftAndIntRight_ReturnsCacheKey()
        {
            var cacheKey = LesserOperatorType.Instance.GetCacheKey(typeof(SampleQuality), typeof(int), null, null);

            Assert.NotNull(cacheKey);
        }

        [Fact]
        public void LesserOperatorType_Compile_WithEnumLeftAndIntRight_ReturnsTrue()
        {
            var left = Expression.Constant(SampleQuality.Awful);
            var right = Expression.Constant(2);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = LesserOperatorType.Instance.Compile(left, typeof(SampleQuality), right, typeof(int), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void LesserOperatorType_Compile_WithEnumLeftAndEnumRight_ReturnsTrue()
        {
            var left = Expression.Constant(SampleQuality.Awful);
            var right = Expression.Constant(SampleQuality.Poor);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = LesserOperatorType.Instance.Compile(left, typeof(SampleQuality), right, typeof(SampleQuality), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void GreaterOperatorType_Compile_WithEnumLeftAndIntRight_ReturnsTrue()
        {
            var left = Expression.Constant(SampleQuality.Good);
            var right = Expression.Constant(2);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = GreaterOperatorType.Instance.Compile(left, typeof(SampleQuality), right, typeof(int), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compare_WithEnumReferenceAndIntValue_ReturnsTrue()
        {
            var resolver = new Mock<IReferenceResolver>();
            resolver.Setup(r => r.TryResolve(It.IsAny<object>(), It.IsAny<IReference>(), It.IsAny<IReadOnlyDictionary<string, object>>(), out It.Ref<object>.IsAny))
                .Returns((object input, IReference reference, IReadOnlyDictionary<string, object> _, out object result) =>
                {
                    result = reference.Value;
                    return true;
                });
            var sut = new Comparator(
                resolver.Object,
                new Dictionary<string, IOperatorType>(StringComparer.OrdinalIgnoreCase)
                {
                    [LesserOperatorType.DefaultTypeName] = LesserOperatorType.Instance,
                });

            var result = sut.Compare(null, new ConditionDef
            {
                Compare = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = SampleQuality.Awful },
                With = LesserOperatorType.DefaultTypeName,
                To = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = 2 },
            }, null);

            Assert.True(result);
        }

        /// <summary>
        /// Mirrors <see cref="QualityCategory"/>'s byte backing so the tests cover a byte-backed enum
        /// without depending on the game assembly.
        /// </summary>
        private enum SampleQuality : byte
        {
            Awful = 0,
            Poor = 1,
            Normal = 2,
            Good = 3,
            Excellent = 4,
        }
    }
}
