using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Models;
using HomebrewDot.Net.Rimworld.UI;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Window for editing <see cref="ConditionDef"/> settings with configurable references for Compare and To fields.
    /// </summary>
    public sealed class ConditionDefEditorWindow : Window
    {
        private const float ModeButtonWidth = 34f;
        private const float ModeButtonGap = 4f;
        private const float RowHeight = 32f;
        private const float LabelWidth = 90f;

        private readonly ConditionDefConfig _config;
        private readonly Action<ConditionDefConfig> _onSave;
        private readonly IReadOnlyDictionary<string, IReferenceType> _referenceTypes;
        private readonly IReadOnlyDictionary<string, IReferenceTypeInputHelper> _referenceTypeInputHelpers;
        private readonly IReadOnlyDictionary<string, IOperatorType> _operatorTypes;
        private readonly ConstantInputField _toInputField;
        private string _error = string.Empty;

        /// <inheritdoc cref="Window"/>
        public override Vector2 InitialSize => new Vector2(800f, 360f);

        public ConditionDefEditorWindow(ConditionDefConfig config, Action<ConditionDefConfig> onSave)
        {
            _config = config ?? new ConditionDefConfig();
            _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));
            _referenceTypes = Toolkit.Services.GetAllNamed<IReferenceType>();
            _referenceTypeInputHelpers = Toolkit.Services.GetAllNamed<IReferenceTypeInputHelper>();
            _operatorTypes = Toolkit.Services.GetAllNamed<IOperatorType>();
            _toInputField = new ConstantInputField(_config.ToNumber, _config.ToDecimal);

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

            // Compare row
            DrawCompareRow(line, content);
            line += RowHeight + 8f;

            // With row
            DrawWithRow(line, content);
            line += RowHeight + 8f;

            // To row
            DrawToRow(line, content);
            line += RowHeight + 8f;

            // IsOr checkbox
            Widgets.CheckboxLabeled(new Rect(content.x, line + 6f, content.width, 28f), "Combine with next using OR", ref _config.IsOr);
            line += RowHeight + 8f;

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

        private void DrawCompareRow(float cursorY, Rect content)
        {
            var labelRect = new Rect(content.x, cursorY, LabelWidth - 4f, RowHeight);
            Widgets.Label(labelRect, "Compare");

            var dButtonRect = new Rect(LabelWidth, cursorY, ModeButtonWidth, RowHeight);
            var rButtonRect = new Rect(dButtonRect.xMax + ModeButtonGap, cursorY, ModeButtonWidth, RowHeight);
            var fieldX = rButtonRect.xMax + ModeButtonGap;
            var fieldWidth = Mathf.Max(0f, content.xMax - fieldX);
            var fieldRect = new Rect(fieldX, cursorY, fieldWidth, RowHeight);

            if (_config.IsCompareReferenceMode)
            {
                DrawReferenceFieldContents(fieldRect, isCompare: true);
            }
            else
            {
                _config.CompareDefault = Widgets.TextField(fieldRect, _config.CompareDefault ?? string.Empty);
            }

            DrawModeButton(dButtonRect, "D", !_config.IsCompareReferenceMode, () => SwitchCompareMode(false));
            DrawModeButton(rButtonRect, "R", _config.IsCompareReferenceMode, () => SwitchCompareMode(true));
        }

        private void DrawWithRow(float cursorY, Rect content)
        {
            var labelRect = new Rect(content.x, cursorY, LabelWidth - 4f, RowHeight);
            Widgets.Label(labelRect, "With");

            var textWidth = Mathf.Max(0f, content.xMax - LabelWidth - 38f);
            var textRect = new Rect(LabelWidth, cursorY, textWidth, RowHeight);
            _config.Operator = Widgets.TextField(textRect, _config.Operator ?? string.Empty);

            var selectRect = new Rect(textRect.xMax + 4f, cursorY, 34f, RowHeight);
            DrawActionButton(selectRect, "...", OpenOperatorPicker);
        }

        private void DrawToRow(float cursorY, Rect content)
        {
            var labelRect = new Rect(content.x, cursorY, LabelWidth - 4f, RowHeight);
            Widgets.Label(labelRect, "To");

            var dButtonRect = new Rect(LabelWidth, cursorY, ModeButtonWidth, RowHeight);
            var rButtonRect = new Rect(dButtonRect.xMax + ModeButtonGap, cursorY, ModeButtonWidth, RowHeight);
            var fieldX = rButtonRect.xMax + ModeButtonGap;
            var fieldWidth = Mathf.Max(0f, content.xMax - fieldX);
            var fieldRect = new Rect(fieldX, cursorY, fieldWidth, RowHeight);

            if (_config.IsToReferenceMode)
            {
                DrawReferenceFieldContents(fieldRect, isCompare: false);
            }
            else
            {
                _toInputField.Draw(fieldRect, ref _config.ToType, ref _config.ToDefault, ref _config.ToNumber, ref _config.ToDecimal);
            }

            DrawModeButton(dButtonRect, "D", !_config.IsToReferenceMode, () => SwitchToMode(false));
            DrawModeButton(rButtonRect, "R", _config.IsToReferenceMode, () => SwitchToMode(true));
        }

        private void DrawReferenceFieldContents(Rect fieldRect, bool isCompare)
        {
            var type = isCompare ? _config.CompareType : _config.ToReferenceType;

            // Reference type picker button
            var pickerWidth = 120f;
            var pickerRect = new Rect(fieldRect.x, fieldRect.y, pickerWidth, fieldRect.height);

            DrawActionButton(pickerRect, string.IsNullOrEmpty(type) ? "Pick" : type, () =>
            {
                OpenReferenceTypePicker(isCompare);
            });

            var cursorX = pickerRect.xMax + 4f;

            // Input-helper button if an IReferenceTypeInputHelper is registered for this type
            var helperWidth = 28f;
            if (!string.IsNullOrEmpty(type) && _referenceTypes.TryGetValue(type, out var refType) && _referenceTypeInputHelpers.TryGetValue(type, out var inputHelper))
            {
                var helperRect = new Rect(cursorX, fieldRect.y, helperWidth, fieldRect.height);
                DrawActionButton(helperRect, ">", () =>
                {
                    var window = inputHelper.GetInputWindow(type, refType, selectedValue =>
                    {
                        if (isCompare)
                        {
                            _config.CompareValue = selectedValue;
                        }
                        else
                        {
                            _config.ToReferenceValue = selectedValue;
                        }
                    });
                    if (window != null)
                    {
                        Find.WindowStack.Add(window);
                    }
                });
                cursorX = helperRect.xMax + 4f;
            }

            // Value text field
            var valueWidth = Mathf.Max(0f, fieldRect.xMax - cursorX);
            var valueRect = new Rect(cursorX, fieldRect.y, valueWidth, fieldRect.height);

            if (isCompare)
            {
                _config.CompareValue = Widgets.TextField(valueRect, _config.CompareValue ?? string.Empty);
            }
            else
            {
                _config.ToReferenceValue = Widgets.TextField(valueRect, _config.ToReferenceValue ?? string.Empty);
            }
        }

        private void OpenReferenceTypePicker(bool isCompare)
        {
            var type = isCompare ? _config.CompareType : _config.ToReferenceType;

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

            var initial = string.IsNullOrEmpty(type) ? null : new List<string> { type };

            Find.WindowStack.Add(new SelectionWindow<string>(
                title: "Select Reference Type",
                options: _referenceTypes.Keys.ToList(),
                optionsGrid: optionsGrid,
                selectedGrid: selectedGrid,
                onConfirm: selected =>
                {
                    var chosen = selected.FirstOrDefault() ?? string.Empty;
                    if (isCompare)
                    {
                        _config.CompareType = chosen;
                    }
                    else
                    {
                        _config.ToReferenceType = chosen;
                    }
                },
                allowMultipleSelection: false,
                enableFiltering: true,
                getFilterStrings: item => new[] { item },
                filterPredicate: null,
                initialSelection: initial));
        }

        private void DrawModeButton(Rect rect, string label, bool isActive, Action onClick)
        {
            Widgets.DrawMenuSection(rect);
            if (isActive)
            {
                Widgets.DrawHighlightSelected(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                onClick?.Invoke();
            }
            Widgets.Label(rect.ContractedBy(4f), label);
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

        private void SwitchCompareMode(bool toReferenceMode)
        {
            _config.IsCompareReferenceMode = toReferenceMode;
        }

        private void SwitchToMode(bool toReferenceMode)
        {
            _config.IsToReferenceMode = toReferenceMode;
        }

        private void OpenOperatorPicker()
        {
            var operators = _operatorTypes.Keys.ToList();

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

            var initialSelection = string.IsNullOrWhiteSpace(_config.Operator)
                ? null
                : new List<string> { _config.Operator };

            Find.WindowStack.Add(new SelectionWindow<string>(
                title: "Select Operator",
                options: operators,
                optionsGrid: optionsGrid,
                selectedGrid: selectedGrid,
                onConfirm: selected =>
                {
                    if (selected != null && selected.Count > 0)
                    {
                        _config.Operator = selected[0] ?? string.Empty;
                    }
                },
                allowMultipleSelection: false,
                enableFiltering: true,
                getFilterStrings: item => new[] { item },
                filterPredicate: null,
                initialSelection: initialSelection));
        }

        private bool ValidateInputs()
        {
            _error = string.Empty;
            if (string.IsNullOrEmpty(_config.Operator))
            {
                _error = "Operator is required";
                return false;
            }

            if (_config.IsCompareReferenceMode)
            {
                if (string.IsNullOrEmpty(_config.CompareType) || string.IsNullOrEmpty(_config.CompareValue))
                {
                    _error = "Compare type and value are required";
                    return false;
                }
            }
            else if (string.IsNullOrEmpty(_config.CompareDefault))
            {
                _error = "Compare value is required";
                return false;
            }

            if (_config.IsToReferenceMode)
            {
                if (string.IsNullOrEmpty(_config.ToReferenceType) || string.IsNullOrEmpty(_config.ToReferenceValue))
                {
                    _error = "To reference type and value are required";
                    return false;
                }
            }

            return true;
        }
    }
}
