using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Generic scrollable grid component for rendering arbitrary data items.
    /// </summary>
    /// <typeparam name="T">The type of item displayed in the grid.</typeparam>
    public class Grid<T>
    {
        // Constants
        private const float DefaultCellWidth = 32f;
        private const float DefaultCellHeight = 32f;
        private const float DefaultCellGap = 4f;

        // Fields
        private Action<T> _onClick;
        private Action<T> _onRightClick;

        /// <summary>
        /// Called once per item to render its content inside the allocated cell rect.
        /// </summary>
        public virtual Action<Rect, T> DrawContent { get; }

        /// <summary>
        /// Returns the tooltip string for the given item, or null to suppress tooltips.
        /// </summary>
        public Func<T, string> GetTooltip { get; private set; }

        /// <summary>
        /// Width of each cell in pixels.
        /// </summary>
        public float CellWidth { get; set; }

        /// <summary>
        /// Height of each cell in pixels.
        /// </summary>
        public float CellHeight { get; set; }

        /// <summary>
        /// Gap between cells and between cells and the border.
        /// </summary>
        public float CellGap { get; set; }

        /// <summary>
        /// Called when the user left-clicks an item cell. May be null.
        /// </summary>
        public event Action<T> OnClick
        {
            add
            {
                if (_onClick == null)
                {
                    _onClick = value;
                }
                else
                {
                    _onClick += value;
                }
            }
            remove
            {
                if (_onClick != null)
                {
                    _onClick -= value;
                }
            }
        }

        /// <summary>
        /// Called when the user right-clicks an item cell. May be null.
        /// </summary>
        public event Action<T> OnRightClick
        {
            add
            {
                if (_onRightClick == null)
                {
                    _onRightClick = value;
                }
                else
                {
                    _onRightClick += value;
                }
            }
            remove
            {
                if (_onRightClick != null)
                {
                    _onRightClick -= value;
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Grid{T}"/>.
        /// </summary>
        /// <param name="drawContent">Delegate used to render each cell's content. Required.</param>
        /// <param name="getTooltip">Optional delegate returning tooltip text for an item.</param>
        /// <param name="onClick">Optional delegate invoked on left-click.</param>
        /// <param name="onRightClick">Optional delegate invoked on right-click.</param>
        /// <param name="cellWidth">Width of each cell in pixels.</param>
        /// <param name="cellHeight">Height of each cell in pixels.</param>
        /// <param name="cellGap">Gap between cells in pixels.</param>
        public Grid(
            Action<Rect, T> drawContent,
            Func<T, string> getTooltip = null,
            Action<T> onClick = null,
            Action<T> onRightClick = null,
            float cellWidth = DefaultCellWidth,
            float cellHeight = DefaultCellHeight,
            float cellGap = DefaultCellGap)
        {
            DrawContent = drawContent ?? throw new ArgumentNullException(nameof(drawContent));
            GetTooltip = getTooltip;
            _onClick = onClick;
            _onRightClick = onRightClick;
            CellWidth = cellWidth;
            CellHeight = cellHeight;
            CellGap = cellGap;
        }

        /// <summary>
        /// Draws the grid for the provided items.
        /// </summary>
        /// <param name="outRect">Visible outer rectangle used as scroll viewport.</param>
        /// <param name="scrollPosition">Scroll position maintained by caller; updated in place.</param>
        /// <param name="items">Items to display.</param>
        public virtual void Draw(Rect outRect, ref Vector2 scrollPosition, IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var cellsPerRow = Mathf.Max(1, Mathf.FloorToInt((outRect.width - CellGap) / (CellWidth + CellGap)));
            var rowCount = Mathf.CeilToInt((float)items.Count / cellsPerRow);
            var contentHeight = CellGap + rowCount * (CellHeight + CellGap);

            var viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, contentHeight));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var col = i % cellsPerRow;
                var row = i / cellsPerRow;
                var cellRect = new Rect(
                    CellGap + col * (CellWidth + CellGap),
                    CellGap + row * (CellHeight + CellGap),
                    CellWidth,
                    CellHeight);

                Widgets.DrawMenuSection(cellRect);

                if (Mouse.IsOver(cellRect))
                {
                    Widgets.DrawHighlight(cellRect);
                }

                DrawContent(cellRect, item);

                var tooltip = GetTooltip?.Invoke(item);
                if (!string.IsNullOrEmpty(tooltip))
                {
                    TooltipHandler.TipRegion(cellRect, tooltip);
                }

                if (_onRightClick != null
                    && Event.current.type == EventType.MouseDown
                    && Event.current.button == 1
                    && cellRect.Contains(Event.current.mousePosition))
                {
                    _onRightClick(item);
                    Event.current.Use();
                }
                else if (Widgets.ButtonInvisible(cellRect))
                {
                    _onClick?.Invoke(item);
                }
            }

            Widgets.EndScrollView();
        }
    }
}
