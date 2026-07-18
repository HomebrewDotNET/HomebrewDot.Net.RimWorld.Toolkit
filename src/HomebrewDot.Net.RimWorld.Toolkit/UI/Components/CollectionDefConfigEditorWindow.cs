using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Window for editing <see cref="CollectionDefConfig"/> settings including conditions, inclusions, and exclusions.
    /// </summary>
    public sealed class CollectionDefConfigEditorWindow : Window
    {
        private const float RowHeight = 28f;
        private const float RowGap = 4f;
        private const float SmallButtonWidth = 34f;
        private const float LogicButtonWidth = 54f;
        private const float BottomButtonsHeight = 32f;
        private const float SectionHeaderHeight = 24f;
        private const float MinListHeight = 60f;

        private readonly CollectionDefConfig _config;
        private readonly Action<CollectionDefConfig> _onSave;
        private readonly string _excludeCollectionName;

        private Vector2 _conditionsScroll = Vector2.zero;
        private Vector2 _inclusionsScroll = Vector2.zero;
        private Vector2 _exclusionsScroll = Vector2.zero;

        private List<ConditionDefConfig> _conditions;
        private List<CollectionConditionDefConfig> _inclusions;
        private List<CollectionConditionDefConfig> _exclusions;

        /// <inheritdoc cref="Window"/>
        public override Vector2 InitialSize => new Vector2(800f, 700f);

        public CollectionDefConfigEditorWindow(CollectionDefConfig config, Action<CollectionDefConfig> onSave, string excludeCollectionName = null)
        {
            _config = config ?? new CollectionDefConfig();
            _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));
            _excludeCollectionName = excludeCollectionName;

            _conditions = _config.Conditions?.ToList() ?? new List<ConditionDefConfig>();
            _inclusions = _config.Inclusions?.ToList() ?? new List<CollectionConditionDefConfig>();
            _exclusions = _config.Exclusions?.ToList() ?? new List<CollectionConditionDefConfig>();

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var content = inRect.ContractedBy(12f);
            var cursorY = content.y;

            // Calculate available height for each section
            var totalHeight = content.height - BottomButtonsHeight - 12f;
            var sectionCount = 3;
            var inBetweenGap = 8f;
            var totalGaps = (sectionCount + 1) * inBetweenGap;
            var sectionHeader = SectionHeaderHeight + RowGap + 4f;
            var addButtonHeight = RowHeight + 4f;
            var availableForLists = totalHeight - (sectionCount * (sectionHeader + addButtonHeight)) - totalGaps;
            var sectionHeight = Mathf.Max(MinListHeight, availableForLists / sectionCount);

            // Conditions section
            cursorY += inBetweenGap;
            cursorY = DrawConditionSection(cursorY, content, sectionHeight);

            // Inclusions section
            cursorY += inBetweenGap;
            cursorY = DrawCollectionConditionSection(cursorY, content, sectionHeight, "Inclusions", _inclusions, ref _inclusionsScroll);

            // Exclusions section
            cursorY += inBetweenGap;
            cursorY = DrawCollectionConditionSection(cursorY, content, sectionHeight, "Exclusions", _exclusions, ref _exclusionsScroll);

            cursorY += 8f;

            // InclusionsAreOr checkbox
            Widgets.CheckboxLabeled(new Rect(content.x, cursorY, content.width, 24f), "Inclusions are OR", ref _config.InclusionsAreOr);

            // Cancel / Save buttons (positioned at bottom of window)
            var buttonY = content.y + content.height - BottomButtonsHeight - 4f;
            var buttonRect = new Rect(content.x, buttonY, 100f, BottomButtonsHeight);
            DrawActionButton(buttonRect, "Cancel", () => Close());

            var saveRect = new Rect(buttonRect.xMax + 12f, buttonY, 100f, BottomButtonsHeight);
            DrawActionButton(saveRect, "Save", () =>
            {
                _config.Conditions = _conditions.Count > 0 ? _conditions.ToList() : null;
                _config.Inclusions = _inclusions.Count > 0 ? _inclusions.ToList() : null;
                _config.Exclusions = _exclusions.Count > 0 ? _exclusions.ToList() : null;
                _onSave(_config);
                Close();
            });
        }

        private float DrawConditionSection(float cursorY, Rect content, float sectionHeight)
        {
            // Section header
            var headerRect = new Rect(content.x, cursorY, content.width, SectionHeaderHeight);
            Widgets.Label(headerRect, "Conditions");
            cursorY = headerRect.yMax + RowGap;

            // List
            var listOutRect = new Rect(content.x, cursorY, content.width, sectionHeight);
            Widgets.DrawMenuSection(listOutRect);
            var innerRect = listOutRect.ContractedBy(6f);

            if (_conditions.Count == 0)
            {
                Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 22f), "- No conditions defined");
            }
            else
            {
                var viewHeight = Mathf.Max(innerRect.height, _conditions.Count * (RowHeight + RowGap) + 4f);
                var viewRect = new Rect(0f, 0f, innerRect.width - 16f, viewHeight);
                Widgets.BeginScrollView(innerRect, ref _conditionsScroll, viewRect);

                var y = 0f;
                for (var i = 0; i < _conditions.Count; i++)
                {
                    var condition = _conditions[i];
                    var rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                    var editRect = new Rect(rowRect.xMax - 98f, rowRect.y, SmallButtonWidth, RowHeight);
                    var deleteRect = new Rect(rowRect.xMax - 60f, rowRect.y, SmallButtonWidth, RowHeight);
                    var logicRect = new Rect(rowRect.xMax - 162f, rowRect.y, LogicButtonWidth, RowHeight);
                    var textWidth = rowRect.width - (i < _conditions.Count - 1 ? 166f : 102f) - 8f;
                    var textRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, textWidth, RowHeight - 8f);

                    if (Mouse.IsOver(rowRect))
                    {
                        Widgets.DrawHighlight(rowRect);
                    }

                    Widgets.DrawMenuSection(rowRect);
                    Widgets.Label(textRect, BuildConditionSummary(condition));

                    if (i < _conditions.Count - 1)
                    {
                        var logicLabel = condition.IsOr ? "OR" : "AND";
                        DrawActionButton(logicRect, logicLabel, () => condition.IsOr = !condition.IsOr);
                    }

                    var editIndex = i;
                    DrawActionButton(editRect, "E", () =>
                    {
                        Find.WindowStack.Add(new ConditionDefEditorWindow(condition, config => { }));
                    });

                    DrawActionButton(deleteRect, "X", () =>
                    {
                        _conditions.RemoveAt(editIndex);
                    });

                    y += RowHeight + RowGap;
                }

                Widgets.EndScrollView();
            }

            cursorY = listOutRect.yMax + 4f;

            // Add button
            var addRect = new Rect(content.x, cursorY, 160f, RowHeight);
            DrawActionButton(addRect, "Add Condition", () =>
            {
                Find.WindowStack.Add(new ConditionDefEditorWindow(null, config =>
                {
                    _conditions.Add(config);
                }));
            });
            cursorY = addRect.yMax;

            return cursorY;
        }

        private float DrawCollectionConditionSection(float cursorY, Rect content, float sectionHeight, string title, List<CollectionConditionDefConfig> items, ref Vector2 scrollPos)
        {
            // Section header
            var headerRect = new Rect(content.x, cursorY, content.width, SectionHeaderHeight);
            Widgets.Label(headerRect, title);
            cursorY = headerRect.yMax + RowGap;

            // List
            var listOutRect = new Rect(content.x, cursorY, content.width, sectionHeight);
            Widgets.DrawMenuSection(listOutRect);
            var innerRect = listOutRect.ContractedBy(6f);

            if (items.Count == 0)
            {
                Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 22f), $"- No {title.ToLower()} defined");
            }
            else
            {
                var viewHeight = Mathf.Max(innerRect.height, items.Count * (RowHeight + RowGap) + 4f);
                var viewRect = new Rect(0f, 0f, innerRect.width - 16f, viewHeight);
                Widgets.BeginScrollView(innerRect, ref scrollPos, viewRect);

                var y = 0f;
                for (var i = 0; i < items.Count; i++)
                {
                    var condition = items[i];
                    var rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                    var editRect = new Rect(rowRect.xMax - 98f, rowRect.y, SmallButtonWidth, RowHeight);
                    var deleteRect = new Rect(rowRect.xMax - 60f, rowRect.y, SmallButtonWidth, RowHeight);
                    var textWidth = rowRect.width - 102f;
                    var textRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, textWidth, RowHeight - 8f);

                    if (Mouse.IsOver(rowRect))
                    {
                        Widgets.DrawHighlight(rowRect);
                    }

                    Widgets.DrawMenuSection(rowRect);
                    Widgets.Label(textRect, BuildCollectionConditionSummary(condition));

                    var editIndex = i;
                    DrawActionButton(editRect, "E", () =>
                    {
                        Find.WindowStack.Add(new CollectionConditionDefConfigEditorWindow(condition, config => { }, _excludeCollectionName));
                    });

                    DrawActionButton(deleteRect, "X", () =>
                    {
                        items.RemoveAt(editIndex);
                    });

                    y += RowHeight + RowGap;
                }

                Widgets.EndScrollView();
            }

            cursorY = listOutRect.yMax + 4f;

            // Add button
            var addRect = new Rect(content.x, cursorY, 160f, RowHeight);
            DrawActionButton(addRect, $"Add {title.TrimEnd('s')}", () =>
            {
                Find.WindowStack.Add(new CollectionConditionDefConfigEditorWindow(null, config =>
                {
                    items.Add(config);
                }, _excludeCollectionName));
            });
            cursorY = addRect.yMax;

            return cursorY;
        }

        private static string BuildConditionSummary(ConditionDefConfig condition)
        {
            if (condition == null)
            {
                return "(null)";
            }

            var compare = condition.IsCompareReferenceMode
                ? $"[{condition.CompareType}:{condition.CompareValue}]"
                : (condition.CompareDefault ?? "(empty)");

            var to = condition.IsToReferenceMode
                ? $"[{condition.ToReferenceType}:{condition.ToReferenceValue}]"
                : (condition.ToDefault ?? condition.ToNumber.ToString() ?? condition.ToDecimal.ToString() ?? "(empty)");

            return $"{compare} {condition.Operator ?? "?"} {to}";
        }

        private static string BuildCollectionConditionSummary(CollectionConditionDefConfig condition)
        {
            if (condition == null)
            {
                return "(null)";
            }

            var parts = new List<string>();
            parts.Add(condition.Name ?? "(no name)");

            if (!string.IsNullOrEmpty(condition.By))
            {
                parts.Add($"by: {condition.By}");
            }

            if (condition.Inverted)
            {
                parts.Add("inverted");
            }

            return string.Join(", ", parts);
        }

        private static void DrawActionButton(Rect rect, string label, Action onClick)
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
