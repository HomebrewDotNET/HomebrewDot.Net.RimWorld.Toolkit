using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Window for editing <see cref="CollectionConditionDefConfig"/> settings.
    /// </summary>
    public sealed class CollectionConditionDefConfigEditorWindow : Window
    {
        private const float RowHeight = 32f;
        private const float RowGap = 8f;
        private const float LabelWidth = 90f;
        private const float PickButtonWidth = 34f;

        private readonly CollectionConditionDefConfig _config;
        private readonly Action<CollectionConditionDefConfig> _onSave;
        private readonly string _excludeCollectionName;
        private string _error = string.Empty;

        /// <inheritdoc cref="Window"/>
        public override Vector2 InitialSize => new Vector2(600f, 320f);

        public CollectionConditionDefConfigEditorWindow(CollectionConditionDefConfig config, Action<CollectionConditionDefConfig> onSave, string excludeCollectionName = null)
        {
            _config = config ?? new CollectionConditionDefConfig();
            _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));
            _excludeCollectionName = excludeCollectionName;

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var content = inRect.ContractedBy(12f);
            var line = content.y;

            // Name row
            DrawNameRow(line, content);
            line += RowHeight + RowGap;

            // By row
            DrawByRow(line, content);
            line += RowHeight + RowGap;

            // IsOr checkbox
            Widgets.CheckboxLabeled(new Rect(content.x, line + 6f, content.width, 28f), "Combine with next using OR", ref _config.IsOr);
            line += RowHeight + RowGap;

            // Inverted checkbox
            Widgets.CheckboxLabeled(new Rect(content.x, line + 6f, content.width, 28f), "Inverted", ref _config.Inverted);
            line += RowHeight + RowGap;

            // Error label
            if (!string.IsNullOrEmpty(_error))
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(content.x, line, content.width, 22f), _error);
                GUI.color = Color.white;
                line += 28f;
            }

            // Cancel / Save buttons
            var buttonRect = new Rect(content.x, content.y + content.height - 40f, 100f, 32f);
            DrawActionButton(buttonRect, "Cancel", () => Close());

            var saveRect = new Rect(buttonRect.xMax + 12f, buttonRect.y, 100f, 32f);
            DrawActionButton(saveRect, "Save", () =>
            {
                if (!ValidateInputs())
                {
                    return;
                }

                _onSave(_config);
                Close();
            });
        }

        private void DrawNameRow(float cursorY, Rect content)
        {
            var labelRect = new Rect(content.x, cursorY, LabelWidth - 4f, RowHeight);
            Widgets.Label(labelRect, "Name");

            var pickRect = new Rect(LabelWidth, cursorY, PickButtonWidth, RowHeight);
            var fieldX = pickRect.xMax + 4f;
            var fieldWidth = Mathf.Max(0f, content.xMax - fieldX);
            var fieldRect = new Rect(fieldX, cursorY, fieldWidth, RowHeight);

            _config.Name = Widgets.TextField(fieldRect, _config.Name ?? string.Empty);

            DrawActionButton(pickRect, "...", OpenCollectionPicker);
        }

        private void DrawByRow(float cursorY, Rect content)
        {
            var labelRect = new Rect(content.x, cursorY, LabelWidth - 4f, RowHeight);
            Widgets.Label(labelRect, "By");

            var fieldRect = new Rect(LabelWidth, cursorY, Mathf.Max(0f, content.xMax - LabelWidth), RowHeight);
            _config.By = Widgets.TextField(fieldRect, _config.By ?? string.Empty);
        }

        private void OpenCollectionPicker()
        {
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var currentName = _config.Name;
            var excludeName = _excludeCollectionName;
            var collectionNames = definitions.Keys
                .Where(k => !string.Equals(k, currentName, StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(k, excludeName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var optionsGrid = new Grid<string>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value),
                getTooltip: x => x,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            var selectedGrid = new Grid<string>(
                drawContent: (cellRect, value) => Widgets.Label(cellRect.ContractedBy(4f), value),
                getTooltip: x => x,
                cellWidth: 300f,
                cellHeight: 32f,
                cellGap: 4f);

            var initial = string.IsNullOrEmpty(_config.Name) ? null : new List<string> { _config.Name };

            Find.WindowStack.Add(new SelectionWindow<string>(
                title: "Select Collection",
                options: collectionNames,
                optionsGrid: optionsGrid,
                selectedGrid: selectedGrid,
                onConfirm: selected =>
                {
                    var chosen = selected.FirstOrDefault();
                    if (!string.IsNullOrEmpty(chosen))
                    {
                        _config.Name = chosen;
                    }
                },
                allowMultipleSelection: false,
                enableFiltering: true,
                getFilterStrings: item => new[] { item },
                filterPredicate: null,
                initialSelection: initial));
        }

        private static void DrawActionButton(Rect rect, string label, Action onClick)
        {
            Widgets.DrawMenuSection(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                onClick?.Invoke();
            }
            Widgets.Label(rect.ContractedBy(4f), label);
        }

        private bool ValidateInputs()
        {
            _error = string.Empty;
            if (string.IsNullOrEmpty(_config.Name))
            {
                _error = "Name is required";
                return false;
            }

            return true;
        }
    }
}
