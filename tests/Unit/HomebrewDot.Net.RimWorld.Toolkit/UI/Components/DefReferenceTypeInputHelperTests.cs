using System;
using HomebrewDot.Net.Rimworld.UI.Components;
using Verse;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.UI.Components
{
    /// <summary>
    /// Tests for <see cref="DefReferenceTypeInputHelper{T}"/>.
    /// </summary>
    public class DefReferenceTypeInputHelperTests
    {
        [Fact]
        public void Instance_IsSingleton()
        {
            Assert.Same(DefReferenceTypeInputHelper<ThingCategoryDef>.Instance, DefReferenceTypeInputHelper<ThingCategoryDef>.Instance);
        }

        [Fact]
        public void GetInputWindow_ConstructsWindowOrThrowsExpectedGameDependency()
        {
            var helper = DefReferenceTypeInputHelper<ThingCategoryDef>.Instance;
            var exception = Record.Exception(() => helper.GetInputWindow("ThingCategory", null, _ => { }));
            // SelectionWindow derives from Window, whose base ctor references SoundDefOf.
            // Outside the game, a TypeInitializationException or SecurityException is expected.
            if (exception != null)
            {
                Assert.True(
                    exception is TypeInitializationException ||
                    exception is System.Security.SecurityException,
                    $"Unexpected exception type: {exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}
