using System;
using System.Collections.Generic;
using HomebrewDot.Net.RimWorld.Collecting.Components;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Referencing.Components;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Referencing.Components
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
            var sut = new DelegateReferenceType((v, ctx) =>
            {
                called = true;
                return v;
            });

            sut.Resolve("input", null);

            Assert.True(called);
        }

        [Fact]
        public void Resolve_ForwardsValueAndContextToDelegate()
        {
            object capturedValue = null;
            IReadOnlyDictionary<string, object> capturedContext = null;

            var sut = new DelegateReferenceType((v, ctx) =>
            {
                capturedValue = v;
                capturedContext = ctx;
                return v;
            });

            var context = new Dictionary<string, object> { ["k"] = "v" };
            sut.Resolve("my-value", context);

            Assert.Equal("my-value", capturedValue);
            Assert.Same(context, capturedContext);
        }

        [Fact]
        public void Resolve_ReturnsWhateverDelegateReturns()
        {
            var expected = new object();
            var sut = new DelegateReferenceType((_, __) => expected);

            var result = sut.Resolve(null, null);

            Assert.Same(expected, result);
        }
    }

    public class IndexedReferenceTypeTests
    {
        // ── Resolve: null guards ──────────────────────────────────────────────

        [Fact]
        public void Resolve_WithNullValue_ReturnsNull()
        {
            var result = IndexedReferenceType.Instance.Resolve(null, new Dictionary<string, object>());
            Assert.Null(result);
        }

        [Fact]
        public void Resolve_WithNullContext_ReturnsNull()
        {
            var result = IndexedReferenceType.Instance.Resolve("Property", null);
            Assert.Null(result);
        }

        // ── Resolve: no ObjectKey in context ──────────────────────────────────

        [Fact]
        public void Resolve_WithNoObjectKeyInContext_ReturnsNull()
        {
            var context = new Dictionary<string, object> { ["other"] = "value" };
            var result = IndexedReferenceType.Instance.Resolve("Name", context);
            Assert.Null(result);
        }

        // ── Resolve: ObjectKey present with IIndexed<object> ─────────────────

        [Fact]
        public void Resolve_WithObjectKeyContainingIIndexed_CallsGetValue()
        {
            var indexed = new Mock<IIndexed<object>>();
            indexed.Setup(i => i.GetValue<object>("Name")).Returns("Alice");

            var context = new Dictionary<string, object>
            {
                [CollectionComparator.ObjectKey] = indexed.Object,
            };

            var result = IndexedReferenceType.Instance.Resolve("Name", context);

            Assert.Equal("Alice", result);
        }

        [Fact]
        public void Resolve_WithObjectKeyContainingIIndexed_PassesPropertyNameFromValue()
        {
            string capturedProperty = null;
            var indexed = new Mock<IIndexed<object>>();
            indexed.Setup(i => i.GetValue<object>(It.IsAny<string>()))
                .Callback<string>(p => capturedProperty = p)
                .Returns((object)null);

            var context = new Dictionary<string, object>
            {
                [CollectionComparator.ObjectKey] = indexed.Object,
            };

            IndexedReferenceType.Instance.Resolve("MyProperty", context);

            Assert.Equal("MyProperty", capturedProperty);
        }

        [Fact]
        public void Resolve_WithObjectKeyContainingNonIIndexed_ReturnsNull()
        {
            var context = new Dictionary<string, object>
            {
                [CollectionComparator.ObjectKey] = "not-an-indexed-object",
            };

            var result = IndexedReferenceType.Instance.Resolve("Name", context);

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
