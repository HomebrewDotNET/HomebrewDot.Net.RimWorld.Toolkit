using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.RimWorld
{
    /// <summary>
    /// A generic scrollable icon grid. Each item is rendered as a fixed-size cell;
    /// hovering displays an optional tooltip and clicks are forwarded to configurable delegates.
    /// </summary>
    /// <remarks>
    /// Configure the component once via the constructor, then call <see cref="Draw"/> each frame.
    /// </remarks>
    /// <typeparam name="T">The type of item displayed in the grid.</typeparam>
    public sealed class IconGrid<T>
    {
        private const float DefaultIconSize = 32f;
        private const float DefaultIconGap = 4f;

        private readonly float _iconSize;
        private readonly float _iconGap;

        /// <summary>
        /// Called once per item to render its icon inside the allocated cell rect.
        /// The rect is already correctly positioned; draw inside it directly.
        /// </summary>
        public Action<Rect, T> DrawIcon { get; set; }

        /// <summary>
        /// Returns the tooltip string for the given item, or <see langword="null"/> to suppress the tooltip.
        /// </summary>
        public Func<T, string> GetTooltip { get; set; }

        /// <summary>
        /// Called when the user left-clicks an item cell. May be <see langword="null"/>.
        /// </summary>
        public Action<T> OnClick { get; set; }

        /// <summary>
        /// Called when the user right-clicks an item cell. May be <see langword="null"/>.
        /// </summary>
        public Action<T> OnRightClick { get; set; }

        /// <summary>
        /// Creates a new <see cref="IconGrid{T}"/>.
        /// </summary>
        /// <param name="drawIcon">Delegate used to render each item's icon. Required.</param>
        /// <param name="getTooltip">Optional delegate returning the hover tooltip text for an item.</param>
        /// <param name="onClick">Optional delegate invoked on left-click.</param>
        /// <param name="onRightClick">Optional delegate invoked on right-click.</param>
        /// <param name="iconSize">Width and height of each icon cell in pixels. Defaults to 32.</param>
        /// <param name="iconGap">Gap between cells and between cells and the border, in pixels. Defaults to 4.</param>
        public IconGrid(
            Action<Rect, T> drawIcon,
            Func<T, string> getTooltip = null,
            Action<T> onClick = null,
            Action<T> onRightClick = null,
            float iconSize = DefaultIconSize,
            float iconGap = DefaultIconGap)
        {
            DrawIcon = drawIcon ?? throw new ArgumentNullException(nameof(drawIcon));
            GetTooltip = getTooltip;
            OnClick = onClick;
            OnRightClick = onRightClick;
            _iconSize = iconSize;
            _iconGap = iconGap;
        }

        /// <summary>
        /// Draws the icon grid for the provided items.
        /// </summary>
        /// <param name="outRect">The visible outer rectangle used as the scroll viewport.</param>
        /// <param name="scrollPosition">Scroll position maintained by the caller; updated in place.</param>
        /// <param name="items">Items to display. No-ops when <see langword="null"/> or empty.</param>
        public void Draw(Rect outRect, ref Vector2 scrollPosition, IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var iconsPerRow = Mathf.Max(1, Mathf.FloorToInt((outRect.width - _iconGap) / (_iconSize + _iconGap)));
            var rowCount = Mathf.CeilToInt((float)items.Count / iconsPerRow);
            var contentHeight = _iconGap + rowCount * (_iconSize + _iconGap);

            var viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, contentHeight));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var col = i % iconsPerRow;
                var row = i / iconsPerRow;
                var cellRect = new Rect(
                    _iconGap + col * (_iconSize + _iconGap),
                    _iconGap + row * (_iconSize + _iconGap),
                    _iconSize,
                    _iconSize);

                Widgets.DrawMenuSection(cellRect);

                if (Mouse.IsOver(cellRect))
                {
                    Widgets.DrawHighlight(cellRect);
                }

                DrawIcon(cellRect, item);

                var tooltip = GetTooltip?.Invoke(item);
                if (!string.IsNullOrEmpty(tooltip))
                {
                    TooltipHandler.TipRegion(cellRect, tooltip);
                }

                // Right-click is checked before ButtonInvisible so it can consume the event.
                if (OnRightClick != null
                    && Event.current.type == EventType.MouseDown
                    && Event.current.button == 1
                    && cellRect.Contains(Event.current.mousePosition))
                {
                    OnRightClick(item);
                    Event.current.Use();
                }
                else if (Widgets.ButtonInvisible(cellRect))
                {
                    OnClick?.Invoke(item);
                }
            }

            Widgets.EndScrollView();
        }
    }
}
