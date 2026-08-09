using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Template;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Comparing.Components
{
    public class ContainsOperatorTypeTests
    {
        [Fact]
        public void Compare_WithValueInStringArray_ReturnsTrue()
        {
            var result = ContainsOperatorType.Instance.Compare(new string[] { "a", "b", "c" }, "b", null, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithValueNotInStringArray_ReturnsFalse()
        {
            var result = ContainsOperatorType.Instance.Compare(new string[] { "a", "b", "c" }, "z", null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithValueInList_ReturnsTrue()
        {
            var result = ContainsOperatorType.Instance.Compare(new List<string> { "a", "b", "c" }, "b", null, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithValueNotInList_ReturnsFalse()
        {
            var result = ContainsOperatorType.Instance.Compare(new List<string> { "a", "b", "c" }, "z", null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithSingletonArray_ReturnsTrue()
        {
            var result = ContainsOperatorType.Instance.Compare(new string[] { "only" }, "only", null, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithEmptyCollection_ReturnsFalse()
        {
            var result = ContainsOperatorType.Instance.Compare(Array.Empty<string>(), "x", null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithNullLeft_ReturnsFalse()
        {
            var result = ContainsOperatorType.Instance.Compare(null, "x", null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithScalarLeft_ReturnsTrueWhenEqual()
        {
            var result = ContainsOperatorType.Instance.Compare("hello", "hello", null, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithScalarLeft_ReturnsFalseWhenNotEqual()
        {
            var result = ContainsOperatorType.Instance.Compare("hello", "world", null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithNotEqualNativeOperator_ReturnsTrueWhenAnyElementDiffers()
        {
            var arguments = new Dictionary<string, object>
            {
                { ContainsOperatorType.NativeOperatorTypeKey, NativeOperatorType.NotEqual }
            };
            var result = ContainsOperatorType.Instance.Compare(new string[] { "a", "b" }, "c", arguments, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithNotEqualNativeOperator_ReturnsFalseWhenAllElementsMatch()
        {
            var arguments = new Dictionary<string, object>
            {
                { ContainsOperatorType.NativeOperatorTypeKey, NativeOperatorType.NotEqual }
            };
            var result = ContainsOperatorType.Instance.Compare(new string[] { "a" }, "a", arguments, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithGreaterThanNativeOperator_ReturnsTrueWhenAnyElementGreater()
        {
            var arguments = new Dictionary<string, object>
            {
                { ContainsOperatorType.NativeOperatorTypeKey, NativeOperatorType.GreaterThan }
            };
            var result = ContainsOperatorType.Instance.Compare(new decimal[] { 1m, 3m, 7m }, 5m, arguments, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithGreaterThanNativeOperator_ReturnsFalseWhenNoElementGreater()
        {
            var arguments = new Dictionary<string, object>
            {
                { ContainsOperatorType.NativeOperatorTypeKey, NativeOperatorType.GreaterThan }
            };
            var result = ContainsOperatorType.Instance.Compare(new decimal[] { 1m, 3m }, 5m, arguments, null);
            Assert.False(result);
        }

        [Fact]
        public void Compile_WithValueInStringArray_ReturnsTrue()
        {
            var left = Expression.Constant(new string[] { "a", "b", "c" });
            var right = Expression.Constant("b");
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(string[]), right, typeof(string), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compile_WithValueNotInStringArray_ReturnsFalse()
        {
            var left = Expression.Constant(new string[] { "a", "b", "c" });
            var right = Expression.Constant("z");
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(string[]), right, typeof(string), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithEmptyArray_ReturnsFalse()
        {
            var left = Expression.Constant(Array.Empty<object>());
            var right = Expression.Constant("x", typeof(object));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(object[]), right, typeof(object), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithMatchingObjectInObjectArray_ReturnsTrue()
        {
            var left = Expression.Constant(new object[] { "a", "target", "c" });
            var right = Expression.Constant("target", typeof(object));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(object[]), right, typeof(object), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compile_WithValueInStringList_ReturnsTrue()
        {
            var left = Expression.Constant(new List<string> { "x", "y", "z" });
            var right = Expression.Constant("y");
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(List<string>), right, typeof(string), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compile_WithScalarLeft_ReturnsTrueWhenEqual()
        {
            var left = Expression.Constant(42);
            var right = Expression.Constant(42);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(int), right, typeof(int), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compile_WithScalarLeft_ReturnsFalseWhenNotEqual()
        {
            var left = Expression.Constant(42);
            var right = Expression.Constant(100);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(int), right, typeof(int), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithGreaterThanNativeOperator_ReturnsTrueWhenAnyElementGreater()
        {
            var arguments = new Dictionary<string, object>
            {
                { ContainsOperatorType.NativeOperatorTypeKey, NativeOperatorType.GreaterThan }
            };
            var left = Expression.Constant(new decimal[] { 6m, 7m });
            var right = Expression.Constant(5m);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(decimal[]), right, typeof(decimal), args, ctx, arguments, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compile_WithGreaterThanNativeOperator_ReturnsFalseWhenNoElementGreater()
        {
            var arguments = new Dictionary<string, object>
            {
                { ContainsOperatorType.NativeOperatorTypeKey, NativeOperatorType.GreaterThan }
            };
            var left = Expression.Constant(new decimal[] { 1m, 3m });
            var right = Expression.Constant(5m);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(decimal[]), right, typeof(decimal), args, ctx, arguments, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_WithReferenceTypeInList_SameInstance_ReturnsTrue()
        {
            var target = new ReferenceType { Value = "gem" };
            var result = ContainsOperatorType.Instance.Compare(new List<ReferenceType> { new ReferenceType { Value = "wood" }, target }, target, null, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithReferenceTypeInList_DifferentInstance_ReturnsFalse()
        {
            var result = ContainsOperatorType.Instance.Compare(new List<ReferenceType> { new ReferenceType { Value = "wood" } }, new ReferenceType { Value = "wood" }, null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithReferenceTypeNotInList_ReturnsFalse()
        {
            var result = ContainsOperatorType.Instance.Compare(new List<ReferenceType> { new ReferenceType { Value = "wood" } }, new ReferenceType { Value = "stone" }, null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compare_WithNotEqualNativeOperator_ReferenceTypeDifferentInstance_ReturnsTrue()
        {
            var arguments = new Dictionary<string, object>
            {
                { ContainsOperatorType.NativeOperatorTypeKey, NativeOperatorType.NotEqual }
            };
            var result = ContainsOperatorType.Instance.Compare(new List<ReferenceType> { new ReferenceType { Value = "wood" } }, new ReferenceType { Value = "stone" }, arguments, null);
            Assert.True(result);
        }

        [Fact]
        public void Compile_WithReferenceTypeInList_SameInstance_ReturnsTrue()
        {
            var target = new ReferenceType { Value = "gem" };
            var left = Expression.Constant(new List<ReferenceType> { new ReferenceType { Value = "wood" }, target });
            var right = Expression.Constant(target);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(List<ReferenceType>), right, typeof(ReferenceType), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compile_WithReferenceTypeInList_DifferentInstance_ReturnsFalse()
        {
            var left = Expression.Constant(new List<ReferenceType> { new ReferenceType { Value = "wood" } });
            var right = Expression.Constant(new ReferenceType { Value = "wood" });
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(List<ReferenceType>), right, typeof(ReferenceType), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compare_WithEnumValueInEnumArray_ReturnsTrue()
        {
            var result = ContainsOperatorType.Instance.Compare(new[] { SampleEnum.First, SampleEnum.Second }, SampleEnum.First, null, null);
            Assert.True(result);
        }

        [Fact]
        public void Compare_WithEnumValueNotInEnumArray_ReturnsFalse()
        {
            var result = ContainsOperatorType.Instance.Compare(new[] { SampleEnum.First, SampleEnum.Second }, SampleEnum.None, null, null);
            Assert.False(result);
        }

        [Fact]
        public void Compile_WithEnumValueInEnumArray_ReturnsTrue()
        {
            var left = Expression.Constant(new[] { SampleEnum.First, SampleEnum.Second });
            var right = Expression.Constant(SampleEnum.First);
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(SampleEnum[]), right, typeof(SampleEnum), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        [Fact]
        public void Compile_WithNullStringList_ReturnsFalse()
        {
            var left = Expression.Constant(null, typeof(List<string>));
            var right = Expression.Constant("x", typeof(string));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(List<string>), right, typeof(string), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithNullStringArray_ReturnsFalse()
        {
            var left = Expression.Constant(null, typeof(string[]));
            var right = Expression.Constant("x", typeof(string));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(string[]), right, typeof(string), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithNullObjectArray_ReturnsFalse()
        {
            var left = Expression.Constant(null, typeof(object[]));
            var right = Expression.Constant("x", typeof(object));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(object[]), right, typeof(object), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithNullObjectLeft_ReturnsFalse()
        {
            var left = Expression.Constant(null, typeof(object));
            var right = Expression.Constant("x", typeof(object));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(object), right, typeof(object), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithNullRight_ReturnsFalse()
        {
            var left = Expression.Constant(new string[] { "a", "b", "c" });
            var right = Expression.Constant(null, typeof(string));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(string[]), right, typeof(string), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithNullElementAndNullSearchValue_ReturnsFalse()
        {
            var left = Expression.Constant(new object[] { "a", null, "c" });
            var right = Expression.Constant(null, typeof(object));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(object[]), right, typeof(object), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.False(result);
        }

        [Fact]
        public void Compile_WithNullElementAndMatchingValue_ReturnsTrue()
        {
            var left = Expression.Constant(new object[] { "a", null, "target" });
            var right = Expression.Constant("target", typeof(object));
            var args = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "args");
            var ctx = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "ctx");

            var expr = ContainsOperatorType.Instance.Compile(left, typeof(object[]), right, typeof(object), args, ctx, null, null);
            var func = Expression.Lambda<Func<IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool>>(expr, args, ctx).Compile();
            var result = func(null, null);

            Assert.True(result);
        }

        private sealed class ReferenceType
        {
            public string Value { get; set; }
        }

        private enum SampleEnum
        {
            None,
            First,
            Second
        }
    }
}
