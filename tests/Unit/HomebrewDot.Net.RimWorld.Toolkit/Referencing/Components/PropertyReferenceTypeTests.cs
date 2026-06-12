using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Referencing.Components
{
    public class PropertyReferenceTypeTests
    {
        private sealed class TestObject
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        private sealed class NestedRoot
        {
            public TestObject Child { get; set; }
        }

        [Fact]
        public void Resolve_WithNullValue_ReturnsNull()
        {
            var result = PropertyReferenceType.Instance.Resolve(null, null, new Dictionary<string, object>());
            Assert.Null(result);
        }

        [Fact]
        public void Resolve_WithNullContext_ReturnsNull()
        {
            var result = PropertyReferenceType.Instance.Resolve(null, "Name", null);
            Assert.Null(result);
        }

        [Fact]
        public void Resolve_WithItemObjectKey_ResolvesProperty()
        {
            var obj = new TestObject { Name = "Alice" };

            var result = PropertyReferenceType.Instance.Resolve(obj, "Name", new Dictionary<string, object>());

            Assert.Equal("Alice", result);
        }

        [Fact]
        public void Resolve_WithObjectContextKey_ResolvesProperty()
        {
            var obj = new TestObject { Name = "Bob" };

            var result = PropertyReferenceType.Instance.Resolve(obj, "Name", new Dictionary<string, object>());

            Assert.Equal("Bob", result);
        }

        [Fact]
        public void Resolve_WithInstanceContextKey_ResolvesProperty()
        {
            var obj = new TestObject { Name = "Charlie" };

            var result = PropertyReferenceType.Instance.Resolve(obj, "Name", new Dictionary<string, object>());

            Assert.Equal("Charlie", result);
        }

        [Fact]
        public void Resolve_WithNoMatchingContextKey_ReturnsNull()
        {
            var obj = new TestObject { Name = "Dave" };

            var result = PropertyReferenceType.Instance.Resolve(obj, "Name", new Dictionary<string, object>());

            Assert.Equal("Dave", result);
        }

        [Fact]
        public void Resolve_WithNullObjectInContext_ReturnsNull()
        {
            var result = PropertyReferenceType.Instance.Resolve(null, "Name", new Dictionary<string, object>());

            Assert.Null(result);
        }

        [Fact]
        public void Resolve_WithMultipleMatchingKeys_FirstMatchWins()
        {
            var first = new TestObject { Name = "First" };

            var result = PropertyReferenceType.Instance.Resolve(first, "Name", new Dictionary<string, object>());

            Assert.Equal("First", result);
        }

        [Fact]
        public void Resolve_WithSingleLevelProperty_ResolvesViaTraverse()
        {
            var obj = new TestObject { Name = "Nested" };

            var result = PropertyReferenceType.Instance.Resolve(obj, "Name", new Dictionary<string, object>());

            Assert.Equal("Nested", result);
        }

        [Fact]
        public void Resolve_WithIntegerProperty_ReturnsIntegerValue()
        {
            var obj = new TestObject { Value = 42 };

            var result = PropertyReferenceType.Instance.Resolve(obj, "Value", new Dictionary<string, object>());

            Assert.Equal(42, result);
        }

        [Fact]
        public void Instance_ReturnsSameObject()
        {
            Assert.Same(PropertyReferenceType.Instance, PropertyReferenceType.Instance);
        }

        // Extension method tests
        [Fact]
        public void PropertyExtension_CallsBuilderReferenceWithCorrectReferenceDef()
        {
            var builder = new Mock<HomebrewDot.Net.Rimworld.Comparing.IConditionOperandBuilder<object>>();
            HomebrewDot.Net.Rimworld.Referencing.IReference capturedRef = null;
            builder.Setup(b => b.Reference(It.IsAny<HomebrewDot.Net.Rimworld.Referencing.IReference>()))
                   .Callback<HomebrewDot.Net.Rimworld.Referencing.IReference>(r => capturedRef = r)
                   .Returns(builder.Object);

            builder.Object.Property("MyProp");

            Assert.NotNull(capturedRef);
            var refDef = Assert.IsType<ReferenceDef>(capturedRef);
            Assert.Equal("Property", refDef.Type);
            Assert.Equal("MyProp", refDef.Value);
        }

        [Fact]
        public void PropertyExtension_WithNullBuilder_ThrowsArgumentNullException()
        {
            HomebrewDot.Net.Rimworld.Comparing.IConditionOperandBuilder<object> builder = null;

            Assert.Throws<ArgumentNullException>(() => builder.Property("MyProp"));
        }

        [Fact]
        public void PropertyExtension_WithNullPropertyName_ThrowsArgumentNullException()
        {
            var builder = new Mock<HomebrewDot.Net.Rimworld.Comparing.IConditionOperandBuilder<object>>().Object;

            Assert.Throws<ArgumentNullException>(() => builder.Property(null));
        }

        [Fact]
        public void PropertyExtension_WithEmptyPropertyName_ThrowsArgumentException()
        {
            var builder = new Mock<HomebrewDot.Net.Rimworld.Comparing.IConditionOperandBuilder<object>>().Object;

            Assert.Throws<ArgumentException>(() => builder.Property(""));
        }
    }
}
