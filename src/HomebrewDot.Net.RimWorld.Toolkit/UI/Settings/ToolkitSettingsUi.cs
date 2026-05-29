using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.RimWorld.UI.Settings
{
    /// <summary>
    /// Renders the Toolkit mod settings window and coordinates tab selection.
    /// </summary>
    internal sealed class ToolkitSettingsUi
    {
        private readonly IToolkitSettingsTab[] _allTabs;
        private readonly IToolkitSettingsTab[] _nonDevTabs;
        private int _selectedTabIndex;

        /// <inheritdoc cref="ToolkitSettingsUi"/>
        public ToolkitSettingsUi()
        {
            _allTabs = new IToolkitSettingsTab[]
            {
                new SettingsUiTab(),
                new DebugSnapshotUiTab(),
                new DebugCollectionsUiTab(),
            };

            _nonDevTabs = new IToolkitSettingsTab[]
            {
                _allTabs[0],
            };

            _selectedTabIndex = 0;
        }

        /// <summary>
        /// Draws the full settings UI for the Toolkit mod.
        /// </summary>
        /// <param name="inRect">The area available for rendering settings content.</param>
        public void Draw(Rect inRect)
        {
            var tabs = Prefs.DevMode ? _allTabs : _nonDevTabs;

            if (_selectedTabIndex >= tabs.Length)
            {
                _selectedTabIndex = 0;
            }

            var contentRect = inRect;
            if (tabs.Length > 1)
            {
                var tabsRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
                DrawTabs(tabsRect, tabs);
                contentRect = new Rect(inRect.x, tabsRect.yMax + 8f, inRect.width, Mathf.Max(0f, inRect.height - 38f));
            }
            else
            {
                _selectedTabIndex = 0;
            }

            tabs[_selectedTabIndex].Draw(contentRect);
        }

        private void DrawTabs(Rect rect, IToolkitSettingsTab[] tabs)
        {
            const float tabGap = 8f;
            var buttonWidth = (rect.width - (tabs.Length - 1) * tabGap) / tabs.Length;

            for (var i = 0; i < tabs.Length; i++)
            {
                var tabRect = new Rect(rect.x + i * (buttonWidth + tabGap), rect.y, buttonWidth, rect.height);

                Widgets.DrawMenuSection(tabRect);
                if (_selectedTabIndex == i)
                {
                    Widgets.DrawHighlightSelected(tabRect);
                }

                if (Widgets.ButtonInvisible(tabRect))
                {
                    _selectedTabIndex = i;
                }

                Widgets.Label(tabRect.ContractedBy(4f), tabs[i].Title);
            }
        }
    }
}