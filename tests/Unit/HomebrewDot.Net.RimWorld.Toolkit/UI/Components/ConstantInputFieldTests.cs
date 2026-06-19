using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.UI.Components
{
    /// <summary>
    /// Pure-logic tests for <see cref="HomebrewDot.Net.Rimworld.UI.Components.ConstantInputField"/>.
    /// </summary>
    /// <remarks>
    /// Tests that drive <c>Draw</c> directly are not feasible in unit-test context: the underlying Verse/UnityEngine statics (e.g. <c>Widgets.TextField</c>, <c>Widgets.DrawMenuSection</c>) require a live Unity GUI runtime and will throw in a headless xUnit host.
    /// </remarks>
    public class ConstantInputFieldTests
    {
        [Fact]
        public void Constructor_WithDefaults_DoesNotThrow()
        {
            var field = new HomebrewDot.Net.Rimworld.UI.Components.ConstantInputField();
            Assert.NotNull(field);
        }

        [Fact]
        public void Constructor_WithValues_DoesNotThrow()
        {
            var field = new HomebrewDot.Net.Rimworld.UI.Components.ConstantInputField(42, 3.14);
            Assert.NotNull(field);
        }

        [Fact]
        public void SyncBuffers_WithInteger_DoesNotThrow()
        {
            var field = new HomebrewDot.Net.Rimworld.UI.Components.ConstantInputField();
            field.SyncBuffers(123, 0.0);
        }

        [Fact]
        public void SyncBuffers_WithDecimal_DoesNotThrow()
        {
            var field = new HomebrewDot.Net.Rimworld.UI.Components.ConstantInputField();
            field.SyncBuffers(0, 6.28);
        }
    }
}
