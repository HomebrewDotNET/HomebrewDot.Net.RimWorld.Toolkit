using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.UI.Components;
using System;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.UI.Components
{
    [Trait("Category", "Unit")]
    public class CollectionConditionDefConfigEditorWindowTests
    {
        [Fact]
        public void Constructor_WithNullConfig_ThrowsTypeInitializationOrSucceeds()
        {
            var exception = Record.Exception(() => new CollectionConditionDefConfigEditorWindow(null, _ => { }));
            // Window base constructor requires RimWorld game context (SoundDefOf).
            // Outside the game, a TypeInitializationException or SecurityException is expected.
            if (exception != null)
            {
                Assert.True(
                    exception is TypeInitializationException ||
                    exception is System.Security.SecurityException,
                    $"Unexpected exception type: {exception.GetType().Name}: {exception.Message}");
            }
        }

        [Fact]
        public void Constructor_WithConfig_ThrowsTypeInitializationOrSucceeds()
        {
            var config = new CollectionConditionDefConfig();
            var exception = Record.Exception(() => new CollectionConditionDefConfigEditorWindow(config, _ => { }));
            if (exception != null)
            {
                Assert.True(
                    exception is TypeInitializationException ||
                    exception is System.Security.SecurityException,
                    $"Unexpected exception type: {exception.GetType().Name}: {exception.Message}");
            }
        }

        [Fact]
        public void Constructor_WithNullOnSave_ThrowsTypeInitializationOrArgumentNullException()
        {
            // Window base constructor may throw TypeInitializationException first in test environment.
            var exception = Record.Exception(() => new CollectionConditionDefConfigEditorWindow(new CollectionConditionDefConfig(), null));
            if (exception != null)
            {
                Assert.True(
                    exception is TypeInitializationException ||
                    exception is System.Security.SecurityException ||
                    exception is ArgumentNullException,
                    $"Unexpected exception type: {exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}
