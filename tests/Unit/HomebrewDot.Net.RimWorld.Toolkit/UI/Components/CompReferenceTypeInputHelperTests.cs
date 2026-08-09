using System.Linq;
using System.Reflection;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.UI.Components;
using RimWorld;
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.UI.Components
{
    public class CompReferenceTypeInputHelperTests
    {
        [Fact]
        public void ScanCompTypes_ReturnsOnlyConcreteCompTypes()
        {
            var types = CompReferenceTypeInputHelper.ScanCompTypes();

            Assert.NotEmpty(types);
            Assert.All(types, t =>
            {
                Assert.True(t.IsClass);
                Assert.False(t.IsAbstract);
                Assert.False(t.IsGenericTypeDefinition);
                Assert.True(typeof(ThingComp).IsAssignableFrom(t) || typeof(CompProperties).IsAssignableFrom(t));
            });
        }

        [Fact]
        public void ScanCompTypes_ContainsThingCompAndCompProperties()
        {
            var types = CompReferenceTypeInputHelper.ScanCompTypes();

            Assert.Contains(types, t => typeof(ThingComp).IsAssignableFrom(t));
            Assert.Contains(types, t => typeof(CompProperties).IsAssignableFrom(t));
        }

        [Fact]
        public void ScanCompTypes_ContainsKnownVanillaComps()
        {
            var types = CompReferenceTypeInputHelper.ScanCompTypes();

            Assert.Contains(types, t => t == typeof(CompGlower));
            Assert.Contains(types, t => t == typeof(CompProperties_Explosive));
        }

        [Fact]
        public void BuildValue_WithoutMember_ReturnsCompTypeName()
        {
            Assert.Equal(nameof(CompGlower), CompReferenceTypeInputHelper.BuildValue(typeof(CompGlower), null));
        }

        [Fact]
        public void BuildValue_WithMember_UsesPathSeparator()
        {
            var compClass = typeof(CompProperties).GetField(nameof(CompProperties.compClass), BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(compClass);

            var value = CompReferenceTypeInputHelper.BuildValue(typeof(CompProperties), compClass);

            Assert.Equal($"{nameof(CompProperties)}{CompReferenceType.PathSeparator}{nameof(CompProperties.compClass)}", value);
        }

        [Fact]
        public void BuildValue_WithNullCompType_ReturnsNull()
        {
            Assert.Null(CompReferenceTypeInputHelper.BuildValue(null, null));
        }

        [Fact]
        public void TraversingMembers_OfCompProperties_IncludeCompClass()
        {
            var members = Toolkit.Helpers.Traversing.GetMembers(typeof(CompProperties));

            Assert.Contains(members, m => m.Name == nameof(CompProperties.compClass));
        }
    }
}
