using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Helpers for opening editor windows on top of each other.
    /// </summary>
    public static class EditorWindowStack
    {
        private const float CascadeOffset = 30f;

        /// <summary>
        /// Adds a nested window to the stack and cascades it relative to the window that opened it, so its controls
        /// never overlap the parent window's controls. RimWorld centers new windows, so nested editors of the same
        /// size would otherwise sit exactly on top of their parent and overlapping buttons would receive the same
        /// click.
        /// </summary>
        /// <param name="window">The nested window to open.</param>
        public static void OpenNested(Window window)
        {
            if (window == null)
            {
                return;
            }

            var stack = Find.WindowStack;
            var parent = stack.currentlyDrawnWindow;
            stack.Add(window);
            if (parent == null)
            {
                return;
            }

            var rect = window.windowRect;
            rect.x = parent.windowRect.x + CascadeOffset;
            rect.y = parent.windowRect.y + CascadeOffset;
            rect.x = Mathf.Max(20f, Mathf.Min(rect.x, Verse.UI.screenWidth - rect.width - 20f));
            rect.y = Mathf.Max(20f, Mathf.Min(rect.y, Verse.UI.screenHeight - rect.height - 20f));
            window.windowRect = rect;
        }
    }
}
