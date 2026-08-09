using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Components
{
    public class OperatorTypesTests
    {
        [Fact]
        public void EqualsOperatorType_Compare_WithEqualValues_ReturnsTrue()
        {
            var result = EqualsOperatorType.Instance.Compare(5, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void NotEqualsOperatorType_Compare_WithDifferentValues_ReturnsTrue()
        {
            var result = NotEqualsOperatorType.Instance.Compare(5, 6, null, null);
            Assert.True(result);
        }

        [Fact]
        public void GreaterOperatorType_Compare_WithLeftGreaterThanRight_ReturnsTrue()
        {
            var result = GreaterOperatorType.Instance.Compare(6, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void GreaterOrEqualOperatorType_Compare_WithEqualValues_ReturnsTrue()
        {
            var result = GreaterOrEqualOperatorType.Instance.Compare(5, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void LesserOperatorType_Compare_WithLeftLessThanRight_ReturnsTrue()
        {
            var result = LesserOperatorType.Instance.Compare(4, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void LesserOrEqualOperatorType_Compare_WithEqualValues_ReturnsTrue()
        {
            var result = LesserOrEqualOperatorType.Instance.Compare(5, 5, null, null);
            Assert.True(result);
        }

        [Fact]
        public void TrueOperatorType_Compare_WithBooleanTrue_ReturnsTrue()
        {
            var result = TrueOperatorType.Instance.Compare(true, null, null, null);
            Assert.True(result);
        }

        [Fact]
        public void FalseOperatorType_Compare_WithBooleanFalse_ReturnsTrue()
        {
            var result = FalseOperatorType.Instance.Compare(false, null, null, null);
            Assert.True(result);
        }

        [Fact]
        public void NullOperatorType_Compare_WithNullLeft_ReturnsTrue()
        {
            var result = NullOperatorType.Instance.Compare(null, 123, null, null);
            Assert.True(result);
        }

        [Fact]
        public void OperatorAliases_ContainExpectedSymbolAliases()
        {
            Assert.Contains("==", EqualsOperatorType.Aliases);
            Assert.Contains("!=", NotEqualsOperatorType.Aliases);
            Assert.Contains(">", GreaterOperatorType.Aliases);
            Assert.Contains(">=", GreaterOrEqualOperatorType.Aliases);
            Assert.Contains("<", LesserOperatorType.Aliases);
            Assert.Contains("<=", LesserOrEqualOperatorType.Aliases);
            Assert.Contains("true", TrueOperatorType.Aliases);
            Assert.Contains("false", FalseOperatorType.Aliases);
        }

        [Fact]
        public void MatchOperatorType_Compare_WithMatchingRegex_ReturnsTrue()
        {
            var result = MatchOperatorType.Instance.Compare("Hello World", @"^Hello.*", null, null);
            Assert.True(result);
        }

        [Fact]
        public void MatchOperatorType_Compare_WithNonMatchingRegex_ReturnsFalse()
        {
            var result = MatchOperatorType.Instance.Compare("Hello World", @"^Goodbye.*", null, null);
            Assert.False(result);
        }

        [Fact]
        public void MatchOperatorType_Compare_WithNullLeft_ReturnsFalse()
        {
            var result = MatchOperatorType.Instance.Compare(null, @"^Test", null, null);
            Assert.False(result);
        }

        [Fact]
        public void MatchOperatorType_Compare_WithNullRight_ReturnsFalse()
        {
            var result = MatchOperatorType.Instance.Compare("Test", null, null, null);
            Assert.False(result);
        }

        [Fact]
        public void MatchOperatorType_Compare_WithBothNull_ReturnsFalse()
        {
            var result = MatchOperatorType.Instance.Compare(null, null, null, null);
            Assert.False(result);
        }

        [Fact]
        public void MatchOperatorType_Aliases_ContainExpectedAliases()
        {
            Assert.Contains("Match", MatchOperatorType.Aliases);
            Assert.Contains("Matches", MatchOperatorType.Aliases);
            Assert.Contains("Regex", MatchOperatorType.Aliases);
        }

        [Fact]
        public void NotNullOperatorType_Compare_WithNonNullLeft_ReturnsTrue()
        {
            var result = NotNullOperatorType.Instance.Compare("something", null, null, null);
            Assert.True(result);
        }

        [Fact]
        public void NotNullOperatorType_Compare_WithNullLeft_ReturnsFalse()
        {
            var result = NotNullOperatorType.Instance.Compare(null, null, null, null);
            Assert.False(result);
        }

        [Fact]
        public void NotNullOperatorType_Compare_WithValueType_ReturnsTrue()
        {
            var result = NotNullOperatorType.Instance.Compare(42, null, null, null);
            Assert.True(result);
        }

        [Fact]
        public void NotNullOperatorType_Aliases_ContainExpectedAliases()
        {
            Assert.Contains("NotNull", NotNullOperatorType.Aliases);
            Assert.Contains("IsNotNull", NotNullOperatorType.Aliases);
            Assert.Contains("Defined", NotNullOperatorType.Aliases);
            Assert.Contains("Any", NotNullOperatorType.Aliases);
        }

        [Fact]
        public void InOperatorType_Compare_WithValueInArray_ReturnsTrue()
        {
            var result = InOperatorType.Instance.Compare("b", new string[] { "a", "b", "c" }, null, null);
            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithValueNotInArray_ReturnsFalse()
        {
            var result = InOperatorType.Instance.Compare("z", new string[] { "a", "b", "c" }, null, null);
            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithValueInList_ReturnsTrue()
        {
            var result = InOperatorType.Instance.Compare("b", new List<string> { "a", "b", "c" }, null, null);
            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithValueNotInList_ReturnsFalse()
        {
            var result = InOperatorType.Instance.Compare("z", new List<string> { "a", "b", "c" }, null, null);
            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithSingletonArray_ReturnsTrue()
        {
            var result = InOperatorType.Instance.Compare("only", new string[] { "only" }, null, null);
            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithEmptyCollection_ReturnsFalse()
        {
            var result = InOperatorType.Instance.Compare("x", Array.Empty<string>(), null, null);
            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithNullCollection_ReturnsFalse()
        {
            var result = InOperatorType.Instance.Compare("x", null, null, null);
            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithScalarRight_ReturnsTrueWhenEqual()
        {
            var result = InOperatorType.Instance.Compare("hello", "hello", null, null);
            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithScalarRight_ReturnsFalseWhenNotEqual()
        {
            var result = InOperatorType.Instance.Compare("hello", "world", null, null);
            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithNotEqualNativeOperator_ReturnsTrueWhenAnyElementDiffers()
        {
            var arguments = new Dictionary<string, object>
            {
                { InOperatorType.NativeOperatorTypeKey, NativeOperatorType.NotEqual }
            };
            var result = InOperatorType.Instance.Compare("c", new string[] { "a", "b" }, arguments, null);
            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithNotEqualNativeOperator_ReturnsFalseWhenAllElementsMatch()
        {
            var arguments = new Dictionary<string, object>
            {
                { InOperatorType.NativeOperatorTypeKey, NativeOperatorType.NotEqual }
            };
            var result = InOperatorType.Instance.Compare("a", new string[] { "a" }, arguments, null);
            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithGreaterThanNativeOperator_ReturnsTrueWhenAnyElementGreater()
        {
            var arguments = new Dictionary<string, object>
            {
                { InOperatorType.NativeOperatorTypeKey, NativeOperatorType.GreaterThan }
            };
            var result = InOperatorType.Instance.Compare(5m, new decimal[] { 1m, 3m, 7m }, arguments, null);
            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compare_WithGreaterThanNativeOperator_ReturnsFalseWhenNoElementGreater()
        {
            var arguments = new Dictionary<string, object>
            {
                { InOperatorType.NativeOperatorTypeKey, NativeOperatorType.GreaterThan }
            };
            var result = InOperatorType.Instance.Compare(0m, new decimal[] { 1m, 3m, 7m }, arguments, null);
            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithValueInStringArray_ReturnsTrue()
        {
            var left = Expression.Constant("b");
            var right = Expression.Constant(new string[] { "a", "b", "c" });
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(string), right, typeof(string[]), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithValueNotInStringArray_ReturnsFalse()
        {
            var left = Expression.Constant("z");
            var right = Expression.Constant(new string[] { "a", "b", "c" });
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(string), right, typeof(string[]), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithEmptyArray_ReturnsFalse()
        {
            var left = Expression.Constant("x", typeof(object));
            var right = Expression.Constant(Array.Empty<object>());
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(object), right, typeof(object[]), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithMatchingObjectInObjectArray_ReturnsTrue()
        {
            var left = Expression.Constant("target", typeof(object));
            var right = Expression.Constant(new object[] { "a", "target", "c" });
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(object), right, typeof(object[]), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithScalarRight_ReturnsTrueWhenEqual()
        {
            var left = Expression.Constant(42);
            var right = Expression.Constant(42);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(int), right, typeof(int), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithScalarRight_ReturnsFalseWhenNotEqual()
        {
            var left = Expression.Constant(42);
            var right = Expression.Constant(100);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(int), right, typeof(int), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithValueInStringList_ReturnsTrue()
        {
            var left = Expression.Constant("x");
            var right = Expression.Constant(new List<string> { "x", "y", "z" });
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(string), right, typeof(List<string>), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithNullStringArray_ReturnsFalse()
        {
            var left = Expression.Constant("x");
            var right = Expression.Constant(null, typeof(string[]));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(string), right, typeof(string[]), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithNullStringList_ReturnsFalse()
        {
            var left = Expression.Constant("x");
            var right = Expression.Constant(null, typeof(List<string>));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(string), right, typeof(List<string>), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithNullElementAndNullSearchValue_ReturnsFalse()
        {
            var left = Expression.Constant(null, typeof(object));
            var right = Expression.Constant(new object[] { "a", null, "c" });
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(object), right, typeof(object[]), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void InOperatorType_Compile_WithNullElementAndMatchingValue_ReturnsTrue()
        {
            var left = Expression.Constant("b", typeof(object));
            var right = Expression.Constant(new object[] { "a", null, "b" });
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = InOperatorType.Instance.Compile(left, typeof(object), right, typeof(object[]), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }
    }
}
