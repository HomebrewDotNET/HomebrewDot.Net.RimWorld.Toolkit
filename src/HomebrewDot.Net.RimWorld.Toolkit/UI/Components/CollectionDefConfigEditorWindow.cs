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
            var sectionHeader = SectionHeaderHeight + RuleListUi.RowGap + 4f;
            var addButtonHeight = RuleListUi.RowHeight + 4f;
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
            RuleListUi.DrawActionButton(buttonRect, "Cancel", () => Close());

            var saveRect = new Rect(buttonRect.xMax + 12f, buttonY, 100f, BottomButtonsHeight);
            RuleListUi.DrawActionButton(saveRect, "Save", () =>
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
            cursorY = headerRect.yMax + RuleListUi.RowGap;

            // List
            var listOutRect = new Rect(content.x, cursorY, content.width, sectionHeight);
            Widgets.DrawMenuSection(listOutRect);
            var innerRect = listOutRect.ContractedBy(6f);

            RuleListUi.Draw(
                innerRect,
                ref _conditionsScroll,
                _conditions,
                "- No conditions defined",
                BuildConditionSummary,
                editIndex => EditorWindowStack.OpenNested(new ConditionDefEditorWindow(_conditions[editIndex], config => { })),
                condition => new ConditionDefConfig(condition),
                condition => condition.IsOr,
                (condition, isOr) => condition.IsOr = isOr);

            cursorY = listOutRect.yMax + 4f;

            // Add button
            var addRect = new Rect(content.x, cursorY, 160f, RuleListUi.RowHeight);
            RuleListUi.DrawActionButton(addRect, "Add Condition", () =>
            {
                EditorWindowStack.OpenNested(new ConditionDefEditorWindow(null, config =>
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
            cursorY = headerRect.yMax + RuleListUi.RowGap;

            // List
            var listOutRect = new Rect(content.x, cursorY, content.width, sectionHeight);
            Widgets.DrawMenuSection(listOutRect);
            var innerRect = listOutRect.ContractedBy(6f);

            RuleListUi.Draw(
                innerRect,
                ref scrollPos,
                items,
                $"- No {title.ToLower()} defined",
                BuildCollectionConditionSummary,
                editIndex => EditorWindowStack.OpenNested(new CollectionConditionDefConfigEditorWindow(items[editIndex], config => { }, _excludeCollectionName)),
                item => new CollectionConditionDefConfig(item),
                item => item.IsOr,
                (item, isOr) => item.IsOr = isOr);

            cursorY = listOutRect.yMax + 4f;

            // Add button
            var addRect = new Rect(content.x, cursorY, 160f, RuleListUi.RowHeight);
            RuleListUi.DrawActionButton(addRect, $"Add {title.TrimEnd('s')}", () =>
            {
                EditorWindowStack.OpenNested(new CollectionConditionDefConfigEditorWindow(null, config =>
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

            return condition.ToCompactString();
        }

        private static string BuildCollectionConditionSummary(CollectionConditionDefConfig condition)
        {
            if (condition == null)
            {
                return "(null)";
            }

            var parts = new List<string>();
            parts.Add((condition.Inverted ? "not " : "") + (condition.Name ?? "(no name)"));

            if (!string.IsNullOrEmpty(condition.By))
            {
                parts.Add($"by: {condition.By}");
            }

            return string.Join(", ", parts);
        }

    }
}
