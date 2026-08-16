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
        private const float WindowWidth = 850f;
        private const float GroupWindowHeight = 720f;
        private const float LeafWindowHeight = 440f;
        private const float GroupListMinHeight = 140f;
        private const float ReservedBelowGroupList = 215f;

        private readonly ConditionDefConfig _config;
        private readonly Action<ConditionDefConfig> _onSave;
        private readonly IReadOnlyDictionary<string, IReferenceType> _referenceTypes;
        private readonly IReadOnlyDictionary<string, IReferenceTypeInputHelper> _referenceTypeInputHelpers;
        private readonly IReadOnlyDictionary<string, IOperatorType> _operatorTypes;
        private readonly ConstantInputField _toInputField;
        private bool _showGroupTab;
        private Vector2 _groupScroll = Vector2.zero;
        private string _error = string.Empty;

        /// <inheritdoc cref="Window"/>
        /// <remarks>The window uses a single height for both tabs, so switching between Condition and Group never
        /// resizes the window. The height is clamped so the window never extends beyond the visible screen area.</remarks>
        public override Vector2 InitialSize
        {
            get
            {
                var targetHeight = Mathf.Min(GroupWindowHeight, Verse.UI.screenHeight - 45f);
                return new Vector2(WindowWidth, Mathf.Max(LeafWindowHeight, targetHeight));
            }
        }

        public ConditionDefEditorWindow(ConditionDefConfig config, Action<ConditionDefConfig> onSave)
        {
            _config = config ?? new ConditionDefConfig();
            _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));
            _referenceTypes = Toolkit.Services.GetAllNamed<IReferenceType>();
            _referenceTypeInputHelpers = Toolkit.Services.GetAllNamed<IReferenceTypeInputHelper>();
            _operatorTypes = Toolkit.Services.GetAllNamed<IOperatorType>();
            _toInputField = new ConstantInputField(_config.ToNumber, _config.ToDecimal);
            _showGroupTab = _config.IsGroup;

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;
            // WindowStack.Add removes existing windows of the same type when they allow only one instance.
            // Group conditions open nested ConditionDefEditorWindows, so multiple instances must be allowed.
            onlyOneOfTypeAllowed = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var content = inRect.ContractedBy(12f);

            // Condition / Group tabs. TabDrawer renders the tabs above the rect it is given
            // (baseRect.y - TabDrawer.TabHeight .. baseRect.y), so the base rect starts one tab
            // height below the top of the content.
            var tabsRect = new Rect(content.x, content.y + TabDrawer.TabHeight, content.width, 0f);
            var tabs = new List<TabRecord>
            {
                new TabRecord("Condition", () => SwitchTab(false), () => !_showGroupTab),
                new TabRecord("Group", () => SwitchTab(true), () => _showGroupTab),
            };
            TabDrawer.DrawTabs(tabsRect, tabs, 200f);

            var line = tabsRect.yMax + 6f;

            if (_showGroupTab)
            {
                // Group conditions list
                line = DrawGroupSection(line, content);
            }
            else
            {
                // Compare row
                DrawCompareRow(line, content);
                line += RowHeight + 8f;

                // With row
                DrawWithRow(line, content);
                line += RowHeight + 8f;

                // To row
                DrawToRow(line, content);
                line += RowHeight + 8f;
            }

            // Chaining checkbox: combines this rule with the rule that follows it in the parent list.
            var isOrRect = new Rect(content.x, line + 6f, content.width, 28f);
            Widgets.CheckboxLabeled(isOrRect, "Combine with next rule using OR", ref _config.IsOr);
            TooltipHandler.TipRegion(isOrRect, "Combines this rule with the rule that follows it in the parent list using OR instead of AND.");
            line += RowHeight + 8f;

            // Group + comparison combination: only matters when this rule has both a group and a comparison.
            if (_showGroupTab)
            {
                var groupCombineRect = new Rect(content.x, line + 6f, content.width, 28f);
                Widgets.CheckboxLabeled(groupCombineRect, "Combine comparison with group using OR", ref _config.ConditionGroupIsOr);
                TooltipHandler.TipRegion(groupCombineRect, "Only applies when this rule has both a comparison and a group defined. When both are present, the rule matches when either the comparison or the group matches, instead of requiring both.");
                line += RowHeight + 8f;
            }

            // Inverted checkbox (only meaningful for the leaf comparison)
            if (!_showGroupTab)
            {
                Widgets.CheckboxLabeled(new Rect(content.x, line + 6f, content.width, 28f), "Inverted (Not)", ref _config.Inverted);
                line += RowHeight + 8f;
            }

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

                if (!_showGroupTab)
                {
                    _config.Conditions = null;
                }

                _onSave(_config);
                Close();
            });
        }

        private void SwitchTab(bool groupTab)
        {
            _showGroupTab = groupTab;
            if (_showGroupTab)
            {
                _config.Conditions ??= new List<ConditionDefConfig>();
                EnsureGroupWindowSize();
            }
        }

        private float DrawGroupSection(float cursorY, Rect content)
        {
            var listHeight = Mathf.Max(GroupListMinHeight, content.yMax - cursorY - ReservedBelowGroupList);
            var listOutRect = new Rect(content.x, cursorY, content.width, listHeight);
            Widgets.DrawMenuSection(listOutRect);
            DrawGroupConditionsList(listOutRect.ContractedBy(6f));
            cursorY = listOutRect.yMax + 6f;

            var addRect = new Rect(content.x, cursorY, 160f, RowHeight);
            DrawActionButton(addRect, "Add Condition", () =>
            {
                _config.Conditions ??= new List<ConditionDefConfig>();
                var subConfig = new ConditionDefConfig();
                EditorWindowStack.OpenNested(new ConditionDefEditorWindow(subConfig, built =>
                {
                    if (!_config.Conditions.Contains(built))
                    {
                        _config.Conditions.Add(built);
                    }
                }));
            });

            return addRect.yMax;
        }

        private void DrawGroupConditionsList(Rect outRect)
        {
            var conditions = _config.Conditions ?? (_config.Conditions = new List<ConditionDefConfig>());

            RuleListUi.Draw(
                outRect,
                ref _groupScroll,
                conditions,
                "- No conditions defined",
                BuildConditionSummary,
                editIndex => EditorWindowStack.OpenNested(new ConditionDefEditorWindow(conditions[editIndex], config => { })),
                condition => new ConditionDefConfig(condition),
                condition => condition.IsOr,
                (condition, isOr) => condition.IsOr = isOr);
        }

        private static string BuildConditionSummary(ConditionDefConfig condition)
        {
            if (condition == null)
            {
                return "(null)";
            }

            return condition.ToCompactString();
        }

        private void EnsureGroupWindowSize()
        {
            var targetHeight = Mathf.Min(GroupWindowHeight, Mathf.Max(LeafWindowHeight, Verse.UI.screenHeight - 45f));
            if (windowRect.height >= targetHeight)
            {
                return;
            }

            windowRect = new Rect(windowRect.x, windowRect.y, windowRect.width, targetHeight);

            // Keep the grown window on screen when it was opened near the bottom edge.
            if (windowRect.yMax > Verse.UI.screenHeight - 10f)
            {
                windowRect = new Rect(
                    windowRect.x,
                    Mathf.Max(10f, Verse.UI.screenHeight - windowRect.height - 10f),
                    windowRect.width,
                    windowRect.height);
            }
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

            // Value text field (only shown when the selected reference type requires a value)
            if (ReferenceTypeRequiresValue(type))
            {
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

        private bool ReferenceTypeRequiresValue(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return true;
            }

            return !_referenceTypes.TryGetValue(type, out var referenceType) || referenceType.RequiresValue;
        }

        private bool ValidateInputs()
        {
            _error = string.Empty;

            if (_showGroupTab)
            {
                if (_config.Conditions == null || _config.Conditions.Count == 0)
                {
                    _error = "A group requires at least one condition.";
                    return false;
                }

                return true;
            }

            if (string.IsNullOrEmpty(_config.Operator))
            {
                _error = "Operator is required";
                return false;
            }

            if (_config.IsCompareReferenceMode)
            {
                if (string.IsNullOrEmpty(_config.CompareType))
                {
                    _error = "Compare type is required";
                    return false;
                }
                if (ReferenceTypeRequiresValue(_config.CompareType) && string.IsNullOrEmpty(_config.CompareValue))
                {
                    _error = "Compare value is required";
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
                if (string.IsNullOrEmpty(_config.ToReferenceType))
                {
                    _error = "To reference type is required";
                    return false;
                }
                if (ReferenceTypeRequiresValue(_config.ToReferenceType) && string.IsNullOrEmpty(_config.ToReferenceValue))
                {
                    _error = "To reference value is required";
                    return false;
                }
            }

            return true;
        }
    }
}
