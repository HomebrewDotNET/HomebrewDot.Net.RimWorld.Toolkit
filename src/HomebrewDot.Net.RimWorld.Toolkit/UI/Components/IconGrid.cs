using System;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// A generic scrollable icon grid. Each item is rendered as a fixed-size cell;
    /// hovering displays an optional tooltip and clicks are forwarded to configurable delegates.
    /// </summary>
    /// <remarks>
    /// Configure the component once via the constructor, then call <see cref="Draw"/> each frame.
    /// </remarks>
    /// <typeparam name="T">The type of item displayed in the grid.</typeparam>
    public class IconGrid<T> : Grid<T>
    {
        // Constants
        private const float DefaultIconSize = 32f;
        private const float DefaultIconGap = 4f;

        /// <summary>
        /// Called once per item to render its icon inside the allocated cell rect.
        /// </summary>
        public virtual Action<Rect, T> DrawIcon { get; }

        /// <summary>
        /// Icon grids render through the generic <see cref="Grid{T}.DrawContent"/> hook.
        /// </summary>
        public override Action<Rect, T> DrawContent => DrawIcon;

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
            : base(
                drawContent: drawIcon,
                getTooltip: getTooltip,
                onClick: onClick,
                onRightClick: onRightClick,
                cellWidth: iconSize,
                cellHeight: iconSize,
                cellGap: iconGap)
        {
            DrawIcon = drawIcon ?? throw new ArgumentNullException(nameof(drawIcon));
        }

        public static IconGrid<T> FromTexture(
            Func<T, Texture2D> getTexture,
            Func<T, string> fallbackText = null,
            Func<T, string> getTooltip = null,
            Action<T> onClick = null,
            Action<T> onRightClick = null,
            float iconSize = DefaultIconSize,
            float iconGap = DefaultIconGap)
        {
            if (getTexture == null) throw new ArgumentNullException(nameof(getTexture));
            return new IconGrid<T>(
                drawIcon: (rect, item) =>
                {
                    Texture2D texture = null;
                    try
                    {
                        texture = getTexture(item);
                    }
                    catch
                    {
                        texture = BaseContent.BadTex;
                    }
                    if (texture != null)
                    {
                        if(texture == BaseContent.BadTex && fallbackText != null)
                        {
                            Widgets.Label(rect, fallbackText(item));
                        }
                        else
                        {
                            Widgets.DrawTextureFitted(rect, texture, 1f);
                        }
                    }
                },
                getTooltip: getTooltip,
                onClick: onClick,
                onRightClick: onRightClick,
                iconSize: iconSize,
                iconGap: iconGap);
        }
    }
}
