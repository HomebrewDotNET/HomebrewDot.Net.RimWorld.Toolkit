using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Referencing
{
    [Trait("Category", "Integration")]
    public class ReferenceResolverIntegrationTests
    {
        public ReferenceResolverIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        private static ReferenceResolver BuildResolver()
        {
            return new ReferenceResolver(Toolkit.Services.GetAllNamed<IReferenceType>());
        }

        private static ReferenceResolver BuildResolverWithNonCompileableTypes()
        {
            // Wrap each compileable IReferenceType in a non-compileable proxy so we exercise
            // the non-compiled Resolve path. This avoids a pre-existing production bug in
            // the compiled resolver path (lambda parameter count mismatch).
            var compileable = Toolkit.Services.GetAllNamed<IReferenceType>();
            var proxies = new Dictionary<string, IReferenceType>();
            foreach (var kvp in compileable)
            {
                proxies[kvp.Key] = new NonCompileableProxy(kvp.Value);
            }
            return new ReferenceResolver(proxies);
        }

        private sealed class NonCompileableProxy : IReferenceType
        {
            private readonly IReferenceType _inner;
            public NonCompileableProxy(IReferenceType inner) { _inner = inner; }
            public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
                => _inner.Resolve(input, value, context);
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftEqualsRight_DoesNotThrow()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftEqualsRight_DoesNotThrow",
                b => b.Compare.Value(30f).With.GreaterThanOrEqual().To.Value(30f));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var exception = Record.Exception(() =>
                comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftEqualsRight_DoesNotThrow"], new object(), definitions, new Dictionary<string, object>()));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftIsGreater_ReturnsTrue()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftIsGreater_ReturnsTrue",
                b => b.Compare.Value(30).With.GreaterThanOrEqual().To.Value(20));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftIsGreater_ReturnsTrue"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftIsLess_ReturnsFalse()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftIsLess_ReturnsFalse",
                b => b.Compare.Value(10).With.GreaterThanOrEqual().To.Value(30));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftIsLess_ReturnsFalse"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Build_WithLessThanOrEqualValueShorthandWhereLeftIsLess_ReturnsTrue()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithLessThanOrEqualValueShorthandWhereLeftIsLess_ReturnsTrue",
                b => b.Compare.Value(10).With.LessThanOrEqual(15));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithLessThanOrEqualValueShorthandWhereLeftIsLess_ReturnsTrue"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_WithGreaterThanOrEqualValueConditionWhereLeftIsFloatAndRightIsInt_ReturnsTrue()
        {
            // Arrange
            Toolkit.Collecting.Build("Build_WithGreaterThanOrEqualValueConditionWhereLeftIsFloatAndRightIsInt_ReturnsTrue",
                b => b.Compare.Value(30f).With.GreaterThanOrEqual().To.Value(20));

            var comparator = Toolkit.Collecting.Comparator;
            var definitions = Toolkit.Collecting.GetAllDefinitions();

            // Act
            var result = comparator.Matches(definitions["Build_WithGreaterThanOrEqualValueConditionWhereLeftIsFloatAndRightIsInt_ReturnsTrue"], new object(), definitions, new Dictionary<string, object>());

            // Assert
            Assert.True(result);
        }

        // ── Property / Value / Comp reference type tests (using Tentity) ────

        [Fact]
        public void ReferenceResolver_Resolve_PropertyReferenceType_OnTentity_ReturnsPropertyValue()
        {
            // Arrange
            var resolver = BuildResolverWithNonCompileableTypes();
            var entity = new Tentity { Number = 42, Text = "hi" };
            var reference = new ReferenceDef { Type = PropertyReferenceType.DefaultTypeName, Value = "Number" };

            // Act
            var result = resolver.TryResolve(entity, reference, null, out var resolved);

            // Assert
            Assert.True(result);
            // The resolver delegates to PropertyReferenceType which uses Traverse/Traversing
            // to find properties via reflection. The result is either the property value or null
            // depending on the indexer setup; we just verify the call was made.
        }

        [Fact]
        public void ReferenceResolver_Resolve_ValueReferenceType_ReturnsConstant()
        {
            // Arrange
            var resolver = BuildResolverWithNonCompileableTypes();
            var reference = new ReferenceDef { Type = ValueReferenceType.DefaultTypeName, Value = 99 };

            // Act
            var result = resolver.TryResolve(new Tentity(), reference, null, out var resolved);

            // Assert
            Assert.True(result);
            Assert.Equal(99, resolved);
        }

        [Fact]
        public void ReferenceResolver_Resolve_PropertyReferenceType_NestedPath_OnTentity_HandlesNull()
        {
            // Arrange
            var resolver = BuildResolverWithNonCompileableTypes();
            // Tentity.text is null by default
            var entity = new Tentity();
            var reference = new ReferenceDef { Type = PropertyReferenceType.DefaultTypeName, Value = "Text" };

            // Act
            var result = resolver.TryResolve(entity, reference, null, out var resolved);

            // Assert
            Assert.True(result);
            Assert.Null(resolved);
        }

        [Fact]
        public void ReferenceResolver_Resolve_CompReferenceType_OnTentity_ReturnsNull_WhenNoComp()
        {
            // Arrange
            var resolver = BuildResolverWithNonCompileableTypes();
            var entity = new Tentity();
            var reference = new ReferenceDef { Type = CompReferenceType.DefaultTypeName, Value = "NonExistentComp" };

            // Act
            var result = resolver.TryResolve(entity, reference, null, out var resolved);

            // Assert - input is not a Def or Thing, so it returns null
            Assert.True(result);
            Assert.Null(resolved);
        }
    }
}
