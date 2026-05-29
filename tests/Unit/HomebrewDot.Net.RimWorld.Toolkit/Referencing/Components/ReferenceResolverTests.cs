using System;
using System.Collections.Generic;
using HomebrewDot.Net.RimWorld.Referencing;
using HomebrewDot.Net.RimWorld.Referencing.Components;
using HomebrewDot.Net.RimWorld.Referencing.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Referencing.Components
{
    public class ReferenceResolverTests
    {
        private static ReferenceDef Ref(string type, object value = null) =>
            new ReferenceDef { Type = type, Value = value };

        private static Mock<IReferenceType> MockType(object returns)
        {
            var mock = new Mock<IReferenceType>();
            mock.Setup(t => t.Resolve(It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Returns(returns);
            return mock;
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullReferenceTypes_DoesNotThrow()
        {
            // Should not throw; falls back to empty dictionary
            var sut = new ReferenceResolver(null);
            var result = sut.TryResolve(Ref("anything"), null, out _);
            Assert.False(result);
        }

        // ── TryResolve ────────────────────────────────────────────────────────

        [Fact]
        public void TryResolve_WithNullReference_ThrowsArgumentNullException()
        {
            var sut = new ReferenceResolver(null);
            Assert.Throws<ArgumentNullException>(() => sut.TryResolve(null, null, out _));
        }

        [Fact]
        public void TryResolve_WithUnknownType_ReturnsFalse()
        {
            var sut = new ReferenceResolver(new Dictionary<string, IReferenceType>());
            var result = sut.TryResolve(Ref("unknown"), null, out var resolved);

            Assert.False(result);
            Assert.Null(resolved);
        }

        [Fact]
        public void TryResolve_WithKnownTypeInConstructor_ResolvesAndReturnsTrue()
        {
            var refType = MockType("resolved-value");
            var sut = new ReferenceResolver(new Dictionary<string, IReferenceType>
            {
                ["mytype"] = refType.Object,
            });

            var result = sut.TryResolve(Ref("mytype", "input"), null, out var resolved);

            Assert.True(result);
            Assert.Equal("resolved-value", resolved);
        }

        [Fact]
        public void TryResolve_WithContextTypes_ContextTakesPrecedenceOverConstructor()
        {
            var constructorType = MockType("from-constructor");
            var contextType = MockType("from-context");

            var sut = new ReferenceResolver(new Dictionary<string, IReferenceType>
            {
                ["mytype"] = constructorType.Object,
            });

            var context = new Dictionary<string, object>
            {
                [ReferenceResolver.ContextReferenceTypesKey] = new Dictionary<string, IReferenceType>
                {
                    ["mytype"] = contextType.Object,
                },
            };

            sut.TryResolve(Ref("mytype"), context, out var resolved);

            Assert.Equal("from-context", resolved);
        }

        [Fact]
        public void TryResolve_WhenTypeNotInContextButInConstructor_FallsBackToConstructor()
        {
            var constructorType = MockType("from-constructor");
            var sut = new ReferenceResolver(new Dictionary<string, IReferenceType>
            {
                ["mytype"] = constructorType.Object,
            });

            // Context has reference types, but not for "mytype"
            var context = new Dictionary<string, object>
            {
                [ReferenceResolver.ContextReferenceTypesKey] = new Dictionary<string, IReferenceType>
                {
                    ["othertype"] = MockType("other").Object,
                },
            };

            var result = sut.TryResolve(Ref("mytype"), context, out var resolved);

            Assert.True(result);
            Assert.Equal("from-constructor", resolved);
        }

        [Fact]
        public void TryResolve_TypeNameIsTrimmed_WhitespaceIsStripped()
        {
            var refType = MockType("result");
            var sut = new ReferenceResolver(new Dictionary<string, IReferenceType>
            {
                ["mytype"] = refType.Object,
            });

            var result = sut.TryResolve(Ref("  mytype  "), null, out var resolved);

            Assert.True(result);
            Assert.Equal("result", resolved);
        }

        [Fact]
        public void TryResolve_PassesValueAndContextToReferenceType()
        {
            object capturedValue = null;
            IReadOnlyDictionary<string, object> capturedContext = null;

            var refType = new Mock<IReferenceType>();
            refType.Setup(t => t.Resolve(It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Callback<object, IReadOnlyDictionary<string, object>>((v, ctx) =>
                {
                    capturedValue = v;
                    capturedContext = ctx;
                })
                .Returns("result");

            var sut = new ReferenceResolver(new Dictionary<string, IReferenceType>
            {
                ["mytype"] = refType.Object,
            });

            var context = new Dictionary<string, object> { ["key"] = "val" };
            sut.TryResolve(Ref("mytype", "the-value"), context, out _);

            Assert.Equal("the-value", capturedValue);
            Assert.Same(context, capturedContext);
        }

        [Fact]
        public void TryResolve_WithContextTypesKeyPresentButWrongType_FallsBackToConstructor()
        {
            var constructorType = MockType("from-constructor");
            var sut = new ReferenceResolver(new Dictionary<string, IReferenceType>
            {
                ["mytype"] = constructorType.Object,
            });

            // Wrong type for context reference types key
            var context = new Dictionary<string, object>
            {
                [ReferenceResolver.ContextReferenceTypesKey] = "not-a-dictionary",
            };

            var result = sut.TryResolve(Ref("mytype"), context, out var resolved);

            Assert.True(result);
            Assert.Equal("from-constructor", resolved);
        }
    }
}
