using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Shared renderer for rule lists (conditions, inclusions, exclusions) used across editor windows.
    /// Each rule is drawn as a row with a summary label and Copy (C), Edit (E), Delete (X), and logic toggle
    /// (AND/OR) buttons. Deleting and copying mutate the supplied list.
    /// </summary>
    public static class RuleListUi
    {
        /// <summary>
        /// The height of a single rule row.
        /// </summary>
        public const float RowHeight = 28f;
        /// <summary>
        /// The vertical gap between rule rows.
        /// </summary>
        public const float RowGap = 4f;
        /// <summary>
        /// The width of the small per-row action buttons (Copy, Edit, Delete).
        /// </summary>
        public const float SmallButtonWidth = 34f;
        /// <summary>
        /// The width of the AND/OR logic toggle button.
        /// </summary>
        public const float LogicButtonWidth = 54f;

        /// <summary>
        /// Draws a scrollable list of rules with per-row actions.
        /// </summary>
        /// <typeparam name="TRule">The rule type.</typeparam>
        /// <param name="outRect">The rect the list is drawn into.</param>
        /// <param name="scrollPos">The scroll position, owned by the caller.</param>
        /// <param name="rules">The rules to draw. The list is mutated by Delete and Copy.</param>
        /// <param name="emptyLabel">The label shown when no rules are defined.</param>
        /// <param name="buildSummary">Builds the summary text for a rule.</param>
        /// <param name="onEdit">Invoked when Edit is clicked for the rule at the given index.</param>
        /// <param name="cloneRule">Creates an independent copy of a rule, used by the Copy button.</param>
        /// <param name="getIsOr">Gets whether the rule chains with the next rule using OR.</param>
        /// <param name="setIsOr">Sets whether the rule chains with the next rule using OR.</param>
        public static void Draw<TRule>(
            Rect outRect,
            ref Vector2 scrollPos,
            IList<TRule> rules,
            string emptyLabel,
            Func<TRule, string> buildSummary,
            Action<int> onEdit,
            Func<TRule, TRule> cloneRule,
            Func<TRule, bool> getIsOr,
            Action<TRule, bool> setIsOr)
        {
            var viewHeight = Mathf.Max(outRect.height, rules.Count * (RowHeight + RowGap) + 4f);
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            if (rules.Count == 0)
            {
                Widgets.Label(new Rect(0f, 0f, viewRect.width, 22f), emptyLabel);
                Widgets.EndScrollView();
                return;
            }

            var y = 0f;
            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                var rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                var copyRect = new Rect(rowRect.xMax - 136f, rowRect.y, SmallButtonWidth, RowHeight);
                var editRect = new Rect(rowRect.xMax - 98f, rowRect.y, SmallButtonWidth, RowHeight);
                var deleteRect = new Rect(rowRect.xMax - 60f, rowRect.y, SmallButtonWidth, RowHeight);
                var logicRect = new Rect(rowRect.xMax - 200f, rowRect.y, LogicButtonWidth, RowHeight);
                var textWidth = rowRect.width - (i < rules.Count - 1 ? 208f : 144f);
                var textRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, textWidth, RowHeight - 8f);

                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                Widgets.DrawMenuSection(rowRect);
                ConditionSummaryUi.DrawSummaryLabel(textRect, buildSummary(rule));

                if (i < rules.Count - 1)
                {
                    var isOr = getIsOr(rule);
                    DrawActionButton(logicRect, isOr ? "OR" : "AND", () => setIsOr(rule, !isOr));
                }

                var editIndex = i;
                DrawActionButton(editRect, "E", () => onEdit(editIndex));

                var copyIndex = i;
                DrawActionButton(copyRect, "C", () =>
                {
                    rules.Insert(copyIndex + 1, cloneRule(rules[copyIndex]));
                });

                DrawActionButton(deleteRect, "X", () =>
                {
                    rules.RemoveAt(copyIndex);
                });

                y += RowHeight + RowGap;
            }

            Widgets.EndScrollView();
        }

        /// <summary>
        /// Draws a menu-styled button with a centered label.
        /// </summary>
        /// <param name="rect">The rect of the button.</param>
        /// <param name="label">The label of the button.</param>
        /// <param name="onClick">Invoked when the button is clicked.</param>
        public static void DrawActionButton(Rect rect, string label, Action onClick)
        {
            Widgets.DrawMenuSection(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                onClick?.Invoke();
            }
            var labelSize = Text.CalcSize(label);
            var labelX = rect.x + (rect.width - labelSize.x) * 0.5f;
            var labelY = rect.y + (rect.height - labelSize.y) * 0.5f;
            Widgets.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), label);
        }
    }
}
