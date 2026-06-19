using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.UI.Components;
using System;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.UI.Components
{
    public class ConditionDefEditorWindowTests
    {
        [Fact]
        public void Constructor_WithNullConfig_ThrowsTypeInitializationOrSucceeds()
        {
            var exception = Record.Exception(() => new ConditionDefEditorWindow(null, _ => { }));
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
            var config = new ConditionDefConfig();
            var exception = Record.Exception(() => new ConditionDefEditorWindow(config, _ => { }));
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
    }
}
