using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.UI.Components;
using RimWorld;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld
{
    /// <summary>
    /// Popup that shows details for a collection, including rules and collected contents.
    /// </summary>
    internal sealed class CollectionDetailsWindow : Window
    {
        private readonly string _collectionName;
        private readonly IconGrid<object> _itemGrid;

        private ICollectionDef _definition;
        private ICollector _collector;
        private List<object> _items = new List<object>();
        private List<string> _ruleLines = new List<string>();
        private Vector2 _ruleScroll = Vector2.zero;
        private Vector2 _itemScroll = Vector2.zero;

        /// <summary>
        /// Initializes a new details popup for the provided collection.
        /// </summary>
        /// <param name="collectionName">The collection to inspect.</param>
        public CollectionDetailsWindow(string collectionName)
        {
            _collectionName = Toolkit.Helpers.Guard.NotNullOrWhitespace(collectionName, nameof(collectionName));
            _itemGrid = new IconGrid<object>(
                drawIcon: DrawItemCell,
                getTooltip: GetItemDetails,
                iconSize: 88f,
                iconGap: 8f);

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;

            RefreshData();
        }

        /// <inheritdoc/>
        public override Vector2 InitialSize => new Vector2(950f, 740f);

        /// <inheritdoc/>
        public override void DoWindowContents(Rect inRect)
        {
            var cursorY = inRect.y;

            var titleRect = new Rect(inRect.x, cursorY, inRect.width, 32f);
            Widgets.Label(titleRect, $"Collection: {_collectionName}");
            cursorY = titleRect.yMax + 4f;

            var statsRect = new Rect(inRect.x, cursorY, inRect.width, 24f);
            var conditionCount = _definition?.Conditions?.Count ?? 0;
            var inclusionCount = _definition?.Inclusions?.Count ?? 0;
            var exclusionCount = _definition?.Exclusions?.Count ?? 0;
            var collectedCount = _collector?.Count ?? 0;
            Widgets.Label(statsRect, $"Conditions: {conditionCount} | Inclusions: {inclusionCount} | Exclusions: {exclusionCount} | Collected: {collectedCount}");
            cursorY = statsRect.yMax + 6f;

            var buttonRowRect = new Rect(inRect.x, cursorY, inRect.width, 32f);

            var refreshRect = new Rect(buttonRowRect.x, buttonRowRect.y, 140f, buttonRowRect.height);
            Widgets.DrawMenuSection(refreshRect);
            if (Widgets.ButtonInvisible(refreshRect))
            {
                RefreshData();
            }
            Widgets.Label(refreshRect.ContractedBy(4f), "Refresh");

            var recollectRect = new Rect(refreshRect.xMax + 8f, buttonRowRect.y, 180f, buttonRowRect.height);
            Widgets.DrawMenuSection(recollectRect);
            if (Widgets.ButtonInvisible(recollectRect))
            {
                Toolkit.Collecting.StartCollection();
                RefreshData();
            }
            Widgets.Label(recollectRect.ContractedBy(4f), "Restart Collection");

            var exportRect = new Rect(recollectRect.xMax + 8f, buttonRowRect.y, 180f, buttonRowRect.height);
            Widgets.DrawMenuSection(exportRect);
            if (Widgets.ButtonInvisible(exportRect))
            {
                DebugExportUtility.ExportCollection(_collectionName, _definition, _collector);
            }
            Widgets.Label(exportRect.ContractedBy(4f), "Export Collection");

            var copyRect = new Rect(exportRect.xMax + 8f, buttonRowRect.y, 140f, buttonRowRect.height);
            Widgets.DrawMenuSection(copyRect);
            if (Widgets.ButtonInvisible(copyRect))
            {
                CopyRulesToClipboard();
            }
            Widgets.Label(copyRect.ContractedBy(4f), "Copy Rules");

            cursorY = buttonRowRect.yMax + 8f;

            var rulesLabelRect = new Rect(inRect.x, cursorY, inRect.width, 22f);
            Widgets.Label(rulesLabelRect, "Collection Rules");
            cursorY = rulesLabelRect.yMax + 4f;

            var rulesRect = new Rect(inRect.x, cursorY, inRect.width, 170f);
            DrawRules(rulesRect);
            cursorY = rulesRect.yMax + 10f;

            var itemsLabelRect = new Rect(inRect.x, cursorY, inRect.width, 22f);
            Widgets.Label(itemsLabelRect, "Collected Items");
            cursorY = itemsLabelRect.yMax + 4f;

            var itemsRect = new Rect(inRect.x, cursorY, inRect.width, Mathf.Max(0f, inRect.yMax - cursorY));
            DrawItems(itemsRect);
        }

        private void DrawRules(Rect outRect)
        {
            Widgets.DrawMenuSection(outRect);
            var contentRect = outRect.ContractedBy(8f);
            if (_ruleLines.Count == 0)
            {
                Widgets.Label(contentRect, "(no rules)");
                return;
            }

            var viewRect = new Rect(0f, 0f, contentRect.width - 16f, Mathf.Max(contentRect.height, _ruleLines.Count * 22f + 4f));
            Widgets.BeginScrollView(contentRect, ref _ruleScroll, viewRect);
            var y = 0f;
            for (var i = 0; i < _ruleLines.Count; i++)
            {
                Widgets.Label(new Rect(0f, y, viewRect.width, 22f), _ruleLines[i]);
                y += 22f;
            }
            Widgets.EndScrollView();
        }

        private void DrawItems(Rect outRect)
        {
            Widgets.DrawMenuSection(outRect);
            var contentRect = outRect.ContractedBy(8f);
            if (_items.Count == 0)
            {
                Widgets.Label(contentRect, "(no collected items)");
                return;
            }

            _itemGrid.Draw(contentRect, ref _itemScroll, _items);
        }

        private void DrawItemCell(Rect rect, object item)
        {
            var icon = GetIcon(item);
            if (icon != null)
            {
                Widgets.DrawTextureFitted(rect.ContractedBy(2f), icon, 1f);
                return;
            }

            Widgets.DrawBoxSolid(rect.ContractedBy(2f), new Color(0.25f, 0.25f, 0.25f, 1f));
            var textRect = rect.ContractedBy(4f);
            var titleRect = new Rect(textRect.x, textRect.y, textRect.width, 22f);
            var valueRect = new Rect(textRect.x, titleRect.yMax + 2f, textRect.width, textRect.height - titleRect.height - 2f);

            Widgets.Label(titleRect, GetDisplayName(item));
            Widgets.Label(valueRect, BuildPreview(item));
        }

        private static string GetItemDetails(object item)
        {
            if (item == null)
            {
                return "Item: null";
            }

            return $"Display: {GetDisplayName(item)}\nType: {item.GetType().FullName}\nValue: {BuildPreview(item)}";
        }

        private static string GetDisplayName(object item)
        {
            if (item == null)
            {
                return "null";
            }

            if (item is Def def)
            {
                if (!string.IsNullOrWhiteSpace(def.LabelCap))
                {
                    return def.LabelCap;
                }

                if (!string.IsNullOrWhiteSpace(def.label))
                {
                    return def.label;
                }

                if (!string.IsNullOrWhiteSpace(def.defName))
                {
                    return def.defName;
                }
            }

            if (TryGetStringProperty(item, "Name", out var name))
            {
                return name;
            }

            if (TryGetStringProperty(item, "Title", out var title))
            {
                return title;
            }

            if (TryGetStringProperty(item, "Label", out var label))
            {
                return label;
            }

            if (TryGetStringProperty(item, "label", out var lowerLabel))
            {
                return lowerLabel;
            }

            if (TryGetStringProperty(item, "defName", out var defName))
            {
                return defName;
            }

            return item.GetType().Name;
        }

        private static Texture2D GetIcon(object item)
        {
            if (item == null)
            {
                return BaseContent.BadTex;
            }

            if (item is ThingDef thingDef && thingDef.uiIcon != null)
            {
                return thingDef.uiIcon;
            }

            if (item is Thing thing && thing.def != null && thing.def.uiIcon != null)
            {
                return thing.def.uiIcon;
            }

            if (item is BuildableDef buildableDef && buildableDef.uiIcon != null)
            {
                return buildableDef.uiIcon;
            }

            return TryGetFirstTextureMember(item, "uiIcon", "Icon", "icon", "Texture", "texture") ?? BaseContent.BadTex;
        }

        private static bool TryGetStringProperty(object instance, string propertyName, out string value)
        {
            value = null;
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            try
            {
                var type = instance.GetType();
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null && property.CanRead)
                {
                    value = property.GetValue(instance)?.ToString();
                    return !string.IsNullOrWhiteSpace(value);
                }

                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    value = field.GetValue(instance)?.ToString();
                    return !string.IsNullOrWhiteSpace(value);
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static Texture2D TryGetFirstTextureMember(object instance, params string[] names)
        {
            if (instance == null || names == null)
            {
                return null;
            }

            var type = instance.GetType();
            for (var i = 0; i < names.Length; i++)
            {
                var property = type.GetProperty(names[i], BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanRead)
                {
                    if (property.GetValue(instance) is Texture2D texture)
                    {
                        return texture;
                    }
                }

                var field = type.GetField(names[i], BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    if (field.GetValue(instance) is Texture2D texture)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static string BuildPreview(object item)
        {
            if (item == null)
            {
                return "<null>";
            }

            var text = item.ToString();
            if (string.IsNullOrEmpty(text))
            {
                return "<empty>";
            }

            return text.Length <= 220 ? text : text.Substring(0, 217) + "...";
        }

        private void RefreshData()
        {
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var collectors = Toolkit.Collecting.GetAllCollectors();

            definitions.TryGetValue(_collectionName, out _definition);
            collectors.TryGetValue(_collectionName, out _collector);

            _items = _collector?.GetAll()?.ToList() ?? new List<object>();
            _ruleLines = BuildRuleLines(_definition);
            _itemScroll = Vector2.zero;
        }

        private void CopyRulesToClipboard()
        {
            var text = string.Join(Environment.NewLine, _ruleLines);
            if (string.IsNullOrEmpty(text))
            {
                text = "(no rules)";
            }

            GUIUtility.systemCopyBuffer = text;
            Messages.Message("Copied collection rules to clipboard.", MessageTypeDefOf.TaskCompletion, historical: false);
        }

        private static List<string> BuildRuleLines(ICollectionDef definition)
        {
            var lines = new List<string>();
            if (definition == null)
            {
                lines.Add("Collection definition not found.");
                return lines;
            }
            if (definition is CollectionDef concreteDef)
            {
                return concreteDef.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            }
            if (definition is StaticCollectionDef staticDef)
            {
                return staticDef.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            }

            if (definition.Conditions != null && definition.Conditions.Count > 0)
            {
                lines.Add("Conditions:");
                for (var i = 0; i < definition.Conditions.Count; i++)
                {
                    lines.Add($"- {definition.Conditions[i]}");
                }
            }
            else
            {
                lines.Add("Conditions: (none)");
            }

            if (definition.Inclusions != null && definition.Inclusions.Count > 0)
            {
                lines.Add($"Inclusions ({(definition.InclusionsAreOr ? "OR" : "AND")}):");
                for (var i = 0; i < definition.Inclusions.Count; i++)
                {
                    var inclusion = definition.Inclusions[i];
                    lines.Add($"+ {inclusion?.ToString() ?? "<null>"}");
                }
            }
            else
            {
                lines.Add("Inclusions: (none)");
            }

            if (definition.Exclusions != null && definition.Exclusions.Count > 0)
            {
                lines.Add("Exclusions:");
                for (var i = 0; i < definition.Exclusions.Count; i++)
                {
                    var exclusion = definition.Exclusions[i];
                    lines.Add($"- {exclusion?.ToString() ?? "<null>"}");
                }
            }
            else
            {
                lines.Add("Exclusions: (none)");
            }

            return lines;
        }
    }
}
