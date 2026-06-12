using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using Moq;
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
}
