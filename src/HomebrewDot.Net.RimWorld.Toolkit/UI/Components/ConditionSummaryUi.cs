using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Shared rendering helpers for condition summaries in editor lists.
    /// </summary>
    public static class ConditionSummaryUi
    {
        /// <summary>
        /// Draws a single-line condition summary. When the summary is wider than the rect, a tooltip with the
        /// full text is shown while the cursor hovers over the entry.
        /// </summary>
        /// <param name="rect">The rect the summary is drawn into.</param>
        /// <param name="summary">The summary text.</param>
        public static void DrawSummaryLabel(Rect rect, string summary)
        {
            Widgets.Label(rect, summary);
            if (Mouse.IsOver(rect) && Text.CalcSize(summary).x > rect.width)
            {
                TooltipHandler.TipRegion(rect, summary);
            }
        }
    }
}
