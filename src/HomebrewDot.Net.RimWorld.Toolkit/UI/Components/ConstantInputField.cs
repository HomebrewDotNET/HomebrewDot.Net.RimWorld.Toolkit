using System.Globalization;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Provides a UI component that edits a constant value of selectable type (text, integer, or floating-point) with mode-switching buttons.
    /// </summary>
    public class ConstantInputField
    {
        /// <summary>
        /// Width in pixels of each mode-selection button.
        /// </summary>
        private const float ModeButtonWidth = 34f;

        /// <summary>
        /// Horizontal gap in pixels between adjacent mode-selection buttons.
        /// </summary>
        private const float ModeButtonGap = 4f;

        /// <summary>
        /// Height in pixels of the input row.
        /// </summary>
        private const float RowHeight = 28f;

        /// <summary>
        /// String buffer used by <c>Widgets.TextFieldNumeric</c> when the current mode is <see cref="ConstantType.Number"/>.
        /// </summary>
        /// <remarks>
        /// This buffer must be seeded with the current integer value; otherwise the first draw in Number mode shows a stale or zero value.
        /// </remarks>
        private string _numberBuffer;

        /// <summary>
        /// String buffer used by <c>Widgets.TextFieldNumeric</c> when the current mode is <see cref="ConstantType.Decimal"/>.
        /// </summary>
        /// <remarks>
        /// This buffer must be seeded with the current decimal value; otherwise the first draw in Decimal mode shows a stale or zero value.
        /// </remarks>
        private string _decimalBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantInputField"/> class, seeding the numeric edit buffers from the supplied values.
        /// </summary>
        /// <param name="numberValue">The initial integer value used to seed the number edit buffer.</param>
        /// <param name="decimalValue">The initial floating-point value used to seed the decimal edit buffer.</param>
        public ConstantInputField(int numberValue = 0, double decimalValue = 0.0)
        {
            _numberBuffer = numberValue.ToString(CultureInfo.InvariantCulture);
            _decimalBuffer = decimalValue.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Replaces the cached numeric edit buffers with string representations of the supplied values.
        /// </summary>
        /// <remarks>
        /// Call this after external value changes (for example, after loading a saved condition) so the next time the user enters Number or Decimal mode, the buffer matches the current value rather than the stale initial seed.
        /// </remarks>
        /// <param name="numberValue">The integer value whose string representation should replace the number buffer.</param>
        /// <param name="decimalValue">The floating-point value whose string representation should replace the decimal buffer.</param>
        public void SyncBuffers(int numberValue, double decimalValue)
        {
            _numberBuffer = numberValue.ToString(CultureInfo.InvariantCulture);
            _decimalBuffer = decimalValue.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Draws the constant input field with type switching buttons.
        /// </summary>
        /// <param name="inRect">The rectangle to draw within.</param>
        /// <param name="type">The current constant type (Text, Number, or Decimal).</param>
        /// <param name="textValue">The text value when type is Text.</param>
        /// <param name="numberValue">The number value when type is Number.</param>
        /// <param name="decimalValue">The decimal value when type is Decimal.</param>
        public void Draw(Rect inRect, ref ConstantType type, ref string textValue, ref int numberValue, ref double decimalValue)
        {
            var valueWidth = inRect.width - (ModeButtonWidth * 3 + ModeButtonGap * 3);
            var valueRect = new Rect(inRect.x, inRect.y, valueWidth, RowHeight);
            var textBtnRect = new Rect(valueRect.xMax + ModeButtonGap, inRect.y, ModeButtonWidth, RowHeight);
            var numBtnRect = new Rect(textBtnRect.xMax + ModeButtonGap, inRect.y, ModeButtonWidth, RowHeight);
            var decBtnRect = new Rect(numBtnRect.xMax + ModeButtonGap, inRect.y, ModeButtonWidth, RowHeight);

            switch (type)
            {
                case ConstantType.Text:
                    textValue = Widgets.TextField(valueRect, textValue);
                    break;
                case ConstantType.Number:
                    Widgets.TextFieldNumeric(valueRect, ref numberValue, ref _numberBuffer);
                    break;
                case ConstantType.Decimal:
                    Widgets.TextFieldNumeric(valueRect, ref decimalValue, ref _decimalBuffer);
                    break;
            }

            DrawModeButton(textBtnRect, "T", ConstantType.Text, ref type);
            DrawModeButton(numBtnRect, "N", ConstantType.Number, ref type);
            DrawModeButton(decBtnRect, "D", ConstantType.Decimal, ref type);
        }

        /// <summary>
        /// Draws a mode selection button with highlighting and label.
        /// </summary>
        /// <param name="rect">The rectangle for the button.</param>
        /// <param name="label">The label to display on the button.</param>
        /// <param name="mode">The constant type this button represents.</param>
        /// <param name="current">The currently selected constant type.</param>
        private void DrawModeButton(Rect rect, string label, ConstantType mode, ref ConstantType current)
        {
            if (current == mode)
            {
                Widgets.DrawHighlightSelected(rect);
            }

            Widgets.DrawMenuSection(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                current = mode;
            }
            Widgets.Label(rect.ContractedBy(4f), label);
        }
    }
}