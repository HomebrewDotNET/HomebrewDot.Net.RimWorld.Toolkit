using UnityEngine;

namespace HomebrewDot.Net.RimWorld.UI.Settings
{
    /// <summary>
    /// Contract for a renderable settings tab in the Toolkit settings UI.
    /// </summary>
    internal interface IToolkitSettingsTab
    {
        /// <summary>
        /// Label rendered in the tab header.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Draws the tab content in the provided region.
        /// </summary>
        /// <param name="rect">The content area.</param>
        void Draw(Rect rect);
    }
}
