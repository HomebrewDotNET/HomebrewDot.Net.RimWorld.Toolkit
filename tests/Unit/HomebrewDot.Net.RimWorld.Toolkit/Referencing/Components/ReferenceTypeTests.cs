using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using Moq;
using RimWorld;
using Verse;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Referencing.Components
{
    public class DelegateReferenceTypeTests
    {
        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullResolver_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DelegateReferenceType(null));
        }

        // ── Resolve ───────────────────────────────────────────────────────────

        [Fact]
        public void Resolve_InvokesProvidedDelegate()
        {
            var called = false;
            var sut = new DelegateReferenceType((input, v, ctx) =>
            {
                called = true;
                return v;
            });

            sut.Resolve("input", null, null);

            Assert.True(called);
        }

        [Fact]
        public void Resolve_ForwardsValueAndContextToDelegate()
        {
            object capturedInput = null;
            object capturedValue = null;
            IReadOnlyDictionary<string, object> capturedContext = null;

            var sut = new DelegateReferenceType((input, v, ctx) =>
            {
                capturedInput = input;
                capturedValue = v;
                capturedContext = ctx;
                return v;
            });

            var context = new Dictionary<string, object> { ["k"] = "v" };
            sut.Resolve("my-input", "my-value", context);

            Assert.Equal("my-input", capturedInput);
            Assert.Equal("my-value", capturedValue);
            Assert.Same(context, capturedContext);
        }

        [Fact]
        public void Resolve_ReturnsWhateverDelegateReturns()
        {
            var expected = new object();
            var sut = new DelegateReferenceType((_, __, ___) => expected);

            var result = sut.Resolve(null, null, null);

            Assert.Same(expected, result);
        }
    }

    public class IndexedReferenceTypeTests
    {
        // ── Resolve: null guards ──────────────────────────────────────────────

        [Fact]
        public void Resolve_WithNullValue_ReturnsNull()
        {
            var result = IndexedReferenceType.Instance.Resolve(null, null, new Dictionary<string, object>());
            Assert.Null(result);
        }

        [Fact]
        public void Resolve_WithNullContext_ReturnsNull()
        {
            var result = IndexedReferenceType.Instance.Resolve(null, "Property", null);
            Assert.Null(result);
        }

        // ── Resolve: no input ─────────────────────────────────────────────────

        [Fact]
        public void Resolve_WithNoInput_ReturnsNull()
        {
            var result = IndexedReferenceType.Instance.Resolve(null, "Name", new Dictionary<string, object>());
            Assert.Null(result);
        }

        // ── Resolve: input present with IIndexed<object> ─────────────────

        [Fact]
        public void Resolve_WithIIndexedInput_CallsGetValue()
        {
            var indexed = new Mock<IIndexed<object>>();
            indexed.Setup(i => i.GetValue<object>("Name")).Returns("Alice");

            var result = IndexedReferenceType.Instance.Resolve(indexed.Object, "Name", new Dictionary<string, object>());

            Assert.Equal("Alice", result);
        }

        [Fact]
        public void Resolve_WithIIndexedInput_PassesPropertyNameFromValue()
        {
            string capturedProperty = null;
            var indexed = new Mock<IIndexed<object>>();
            indexed.Setup(i => i.GetValue<object>(It.IsAny<string>()))
                .Callback<string>(p => capturedProperty = p)
                .Returns((object)null);

            IndexedReferenceType.Instance.Resolve(indexed.Object, "MyProperty", new Dictionary<string, object>());

            Assert.Equal("MyProperty", capturedProperty);
        }

        [Fact]
        public void Resolve_WithNonIIndexedInput_ReturnsNull()
        {
            var result = IndexedReferenceType.Instance.Resolve("not-an-indexed-object", "Name", new Dictionary<string, object>());

            Assert.Null(result);
        }

        // ── Singleton ─────────────────────────────────────────────────────────

        [Fact]
        public void Instance_ReturnsSameObject()
        {
            Assert.Same(IndexedReferenceType.Instance, IndexedReferenceType.Instance);
        }

        [Fact]
        public void DefaultTypeName_IsIndexed()
        {
            Assert.Equal("Indexed", IndexedReferenceType.DefaultTypeName);
        }
    }

    public class ReferenceTypeRequiresValueTests
    {
        [Fact]
        public void SelfReferenceType_DoesNotRequireValue()
        {
            Assert.False(SelfReferenceType.Instance.RequiresValue);
        }

        [Fact]
        public void ValueReferenceType_RequiresValue()
        {
            Assert.True(ValueReferenceType.Instance.RequiresValue);
        }

        [Fact]
        public void IndexedReferenceType_RequiresValue()
        {
            Assert.True(IndexedReferenceType.Instance.RequiresValue);
        }

        [Fact]
        public void PropertyReferenceType_RequiresValue()
        {
            Assert.True(PropertyReferenceType.Instance.RequiresValue);
        }

        [Fact]
        public void CompReferenceType_RequiresValue()
        {
            Assert.True(CompReferenceType.Instance.RequiresValue);
        }

        [Fact]
        public void StatReferenceType_RequiresValue()
        {
            Assert.True(StatReferenceType.Instance.RequiresValue);
        }

        [Fact]
        public void DelegateReferenceType_RequiresValue()
        {
            var sut = new DelegateReferenceType((input, value, context) => value);
            Assert.True(sut.RequiresValue);
        }
    }

    /// <summary>
    /// Regression coverage for compiling <see cref="CompReferenceType"/> getters when the input is an
    /// <see cref="IIndexed{T}"/>. The metadata lookup branch used the static-only
    /// <c>Expression.Call(MethodInfo, Expression, Expression)</c> overload for the instance methods
    /// <c>ContainsKey</c>/<c>get_Item</c> on <see cref="IIndexed{T}.Metadata"/>, which threw
    /// <c>ArgumentException: Static method requires null instance, non-static method requires non-null instance</c>
    /// while compiling a quality condition (e.g. <c>CompQuality|Quality</c> with Max set to Good) against a
    /// snapshot. The getters must compile against indexed inputs and read the cached comp from metadata when present.
    /// </summary>
    public class CompReferenceTypeTests
    {
        private sealed class TestComp : ThingComp
        {
            public int TestValue { get; set; }
        }

        [Fact]
        public void Compile_WithIndexedThingInput_CompilesWithoutThrowing()
        {
            // Arrange
            var input = new Mock<IIndexed<Thing>>().Object;
            var inputParameter = Expression.Parameter(typeof(IIndexed<Thing>), "input");

            // Act
            var expr = CompReferenceType.Instance.Compile(inputParameter, input, null, typeof(TestComp), null);

            // Assert
            Assert.NotNull(expr);
            var func = Expression.Lambda<Func<IIndexed<Thing>, object>>(Expression.Convert(expr, typeof(object)), inputParameter).Compile();
            Assert.NotNull(func);
        }

        [Fact]
        public void Compile_WithIndexedThingInputAndCachedCompInMetadata_ReturnsCachedComp()
        {
            // Arrange
            var cachedComp = new TestComp();
            var metadata = new Dictionary<string, object>
            {
                [$"CompReferenceType:{typeof(TestComp).FullName}"] = cachedComp
            };
            var indexed = new Mock<IIndexed<Thing>>();
            indexed.Setup(i => i.Metadata).Returns(metadata);

            var inputParameter = Expression.Parameter(typeof(IIndexed<Thing>), "input");
            var expr = CompReferenceType.Instance.Compile(inputParameter, indexed.Object, null, typeof(TestComp), null);
            var func = Expression.Lambda<Func<IIndexed<Thing>, object>>(Expression.Convert(expr, typeof(object)), inputParameter).Compile();

            // Act
            var result = func(indexed.Object);

            // Assert
            Assert.Same(cachedComp, result);
        }

        [Fact]
        public void Compile_WithIndexedThingInputAndPropertyReference_CompilesWithoutThrowing()
        {
            // Arrange
            var input = new Mock<IIndexed<Thing>>().Object;
            var inputParameter = Expression.Parameter(typeof(IIndexed<Thing>), "input");
            var reference = $"{typeof(TestComp).FullName}{CompReferenceType.PathSeparator}{nameof(TestComp.TestValue)}";

            // Act
            var expr = CompReferenceType.Instance.Compile(inputParameter, input, null, reference, null);

            // Assert
            Assert.NotNull(expr);
            var func = Expression.Lambda<Func<IIndexed<Thing>, object>>(Expression.Convert(expr, typeof(object)), inputParameter).Compile();
            Assert.NotNull(func);
        }

        [Fact]
        public void TryAutodex_WithPropertyReference_DoesNotThrow()
        {
            // Regression: TryAutodex compiled the property-value getter with an Indexed<T> parameter but passed
            // the raw Thing as the compile-time input, so Compile tried Expression.Convert(Indexed<T>, Thing)
            // and threw "No coercion operator is defined between types 'Indexed`1[Verse.Thing]' and 'Verse.Thing'".
            // This is exactly the autodex call the quality condition (CompQuality|Quality) triggers on the first
            // snapshot load, which broke the quality filter entirely. The getter must compile against the
            // IIndexed<T> wrapper so it can read the cached comp from metadata.
            // Arrange
            var instance = (Thing)FormatterServices.GetUninitializedObject(typeof(ThingWithComps));
            var reference = new ReferenceDef
            {
                Type = CompReferenceType.DefaultTypeName,
                Value = $"{typeof(CompQuality).FullName}{CompReferenceType.PathSeparator}{nameof(CompQuality.Quality)}"
            };

            // Act & Assert
            CompReferenceType.TryAutodex(instance, reference);
        }
    }
}
