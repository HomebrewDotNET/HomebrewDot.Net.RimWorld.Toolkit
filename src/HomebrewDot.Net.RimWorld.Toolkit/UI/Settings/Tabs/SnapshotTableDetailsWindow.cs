using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HomebrewDot.Net.RimWorld.Indexing;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.RimWorld
{
    /// <summary>
    /// Popup that shows details for a snapshot table, including its sub-tables and table contents.
    /// </summary>
    internal class SnapshotTableDetailsWindow : Window
    {
        private const int MaxDisplayedRows = 500;
        private static readonly IReadOnlyDictionary<string, object> EmptyMetadata = new Dictionary<string, object>();

        private readonly Stack<IReadOnlyTable> _history = new Stack<IReadOnlyTable>();
        private readonly IconGrid<IReadOnlyTable> _subTableGrid;
        private readonly IconGrid<IIndexed<object>> _rowGrid;

        private IReadOnlyTable _currentTable;
        private List<IIndexed<object>> _rows = new List<IIndexed<object>>();
        private bool _rowsTruncated;
        private Vector2 _rowGridScroll = Vector2.zero;
        private Vector2 _subTableScroll = Vector2.zero;

        /// <summary>
        /// Initializes a new details popup for the provided table.
        /// </summary>
        /// <param name="table">The table to inspect.</param>
        public SnapshotTableDetailsWindow(IReadOnlyTable table)
        {
            _currentTable = table ?? throw new ArgumentNullException(nameof(table));
            _subTableGrid = new IconGrid<IReadOnlyTable>(
                drawIcon: DrawSubTableCell,
                getTooltip: BuildSubTableTooltip,
                onClick: NavigateToSubTable,
                iconSize: 92f,
                iconGap: 8f);
            _rowGrid = new IconGrid<IIndexed<object>>(
                drawIcon: DrawRowCell,
                getTooltip: GetDetails,
                iconSize: 88f,
                iconGap: 8f);

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;

            RefreshRows();
        }

        /// <inheritdoc/>
        public override Vector2 InitialSize => new Vector2(950f, 740f);

        /// <inheritdoc/>
        public override void DoWindowContents(Rect inRect)
        {
            var cursorY = inRect.y;

            var titleRect = new Rect(inRect.x, cursorY, inRect.width, 32f);
            Widgets.Label(titleRect, $"Table: {_currentTable.Name}");
            cursorY = titleRect.yMax + 4f;

            var statsRect = new Rect(inRect.x, cursorY, inRect.width, 24f);
            var countText = _rowsTruncated
                ? $"Rows shown: {_rows.Count} / {TryCount(_currentTable)?.ToString() ?? "unknown"}"
                : $"Rows: {_rows.Count}";
            Widgets.Label(statsRect, countText);
            cursorY = statsRect.yMax + 6f;

            var buttonRowRect = new Rect(inRect.x, cursorY, inRect.width, 32f);
            var backRect = new Rect(buttonRowRect.x, buttonRowRect.y, 140f, buttonRowRect.height);
            Widgets.DrawMenuSection(backRect);
            if (_history.Count == 0)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                Widgets.Label(backRect.ContractedBy(4f), "Back");
                GUI.color = Color.white;
            }
            else
            {
                if (Widgets.ButtonInvisible(backRect))
                {
                    _currentTable = _history.Pop();
                    RefreshRows();
                    _subTableScroll = Vector2.zero;
                }

                Widgets.Label(backRect.ContractedBy(4f), "Back");
            }

            var refreshRect = new Rect(backRect.xMax + 8f, buttonRowRect.y, 140f, buttonRowRect.height);
            Widgets.DrawMenuSection(refreshRect);
            if (Widgets.ButtonInvisible(refreshRect))
            {
                RefreshRows();
            }
            Widgets.Label(refreshRect.ContractedBy(4f), "Refresh");

            cursorY = buttonRowRect.yMax + 8f;

            var subTablesLabelRect = new Rect(inRect.x, cursorY, inRect.width, 22f);
            Widgets.Label(subTablesLabelRect, "Sub-Tables");
            cursorY = subTablesLabelRect.yMax + 4f;

            var subTablesRect = new Rect(inRect.x, cursorY, inRect.width, 124f);
            Widgets.DrawMenuSection(subTablesRect);
            var subTables = _currentTable.SubTables?.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList()
                ?? new List<IReadOnlyTable>();
            if (subTables.Count == 0)
            {
                Widgets.Label(subTablesRect.ContractedBy(8f), "(none)");
            }
            else
            {
                _subTableGrid.Draw(subTablesRect.ContractedBy(4f), ref _subTableScroll, subTables);
            }

            cursorY = subTablesRect.yMax + 10f;

            var contentsLabelRect = new Rect(inRect.x, cursorY, inRect.width, 22f);
            Widgets.Label(contentsLabelRect, "Table Contents");
            cursorY = contentsLabelRect.yMax + 4f;

            var contentsRect = new Rect(inRect.x, cursorY, inRect.width, Mathf.Max(0f, inRect.yMax - cursorY));
            DrawContentsGrid(contentsRect);
        }

        private static string BuildSubTableTooltip(IReadOnlyTable table)
        {
            var rowCount = TryCount(table);
            return rowCount.HasValue
                ? $"{table.Name}\nRows: {rowCount.Value}\nClick to inspect"
                : $"{table.Name}\nRows: unknown\nClick to inspect";
        }

        private static void DrawSubTableCell(Rect rect, IReadOnlyTable table)
        {
            Widgets.Label(rect.ContractedBy(6f), table.Name);
        }

        private void DrawContentsGrid(Rect outRect)
        {
            Widgets.DrawMenuSection(outRect);
            var contentRect = outRect.ContractedBy(8f);
            if (_rows.Count == 0)
            {
                Widgets.Label(contentRect, "(no rows)");
                return;
            }

            _rowGrid.Draw(contentRect, ref _rowGridScroll, _rows);
        }

        private void DrawRowCell(Rect rect, IIndexed<object> row)
        {
            var icon = GetIcon(row);
            
            if (icon != null)
            {
                // Icon available: show only the icon, centered and filling most of the square
                var padding = 2f;
                Widgets.DrawTextureFitted(rect.ContractedBy(padding), icon, 1f);
            }
            else
            {
                // No icon: show placeholder box and text details
                Widgets.DrawBoxSolid(rect.ContractedBy(2f), new Color(0.25f, 0.25f, 0.25f, 1f));
                
                var textRect = rect.ContractedBy(4f);
                var titleRect = new Rect(textRect.x, textRect.y, textRect.width, 22f);
                var valueRect = new Rect(textRect.x, titleRect.yMax + 2f, textRect.width, textRect.height - titleRect.height - 2f);
                
                Widgets.Label(titleRect, GetDisplayName(row));
                Widgets.Label(valueRect, BuildRowPreview(row));
            }
        }

        /// <summary>
        /// Gets the display name for a row in the contents grid.
        /// </summary>
        /// <param name="row">The indexed row.</param>
        /// <returns>The display label.</returns>
        protected virtual string GetDisplayName(IIndexed<object> row)
        {
            if (row == null || row.Value == null)
            {
                return "null";
            }

            if (TryGetPreferredDisplayValue(row, out var preferredDisplayValue))
            {
                return preferredDisplayValue;
            }

            var value = row.Value;
            if (value is Def def)
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

            if (TryGetStringValue(row, "LabelCap", out var labelCap))
            {
                return labelCap;
            }

            if (TryGetStringValue(row, "Label", out var label))
            {
                return label;
            }

            if (TryGetStringValue(row, "label", out var lowerLabel))
            {
                return lowerLabel;
            }

            if (TryGetStringValue(row, "defName", out var defName))
            {
                return defName;
            }

            if (TryGetStringValue(row, "name", out var name))
            {
                return name;
            }

            return value.GetType().Name;
        }

        /// <summary>
        /// Gets the detail tooltip text for a row in the contents grid.
        /// </summary>
        /// <param name="row">The indexed row.</param>
        /// <returns>The tooltip content.</returns>
        protected virtual string GetDetails(IIndexed<object> row)
        {
            if (row == null)
            {
                return "Row: null";
            }

            var lines = new List<string>
            {
                $"Display: {GetDisplayName(row)}",
                $"Type: {row.Value?.GetType().FullName ?? "null"}",
                $"Value: {BuildRowPreview(row)}"
            };

            if (row.Metadata == null || row.Metadata.Count == 0)
            {
                lines.Add("Metadata: (none)");
                return string.Join("\n", lines);
            }

            lines.Add("Metadata:");
            foreach (var kvp in row.Metadata.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add($"- {kvp.Key}: {kvp.Value ?? "<null>"}");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Gets the icon for a row in the contents grid.
        /// </summary>
        /// <param name="row">The indexed row.</param>
        /// <returns>The icon texture to draw.</returns>
        protected virtual Texture2D GetIcon(IIndexed<object> row)
        {
            if (row == null)
            {
                return BaseContent.BadTex;
            }

            if (TryGetPreferredIconValue(row, out var preferredIconValue) && preferredIconValue is Texture2D preferredIcon)
            {
                return preferredIcon;
            }

            var value = row.Value;
            if (value is ThingDef thingDef && thingDef.uiIcon != null)
            {
                return thingDef.uiIcon;
            }

            if (value is Thing thing && thing.def != null && thing.def.uiIcon != null)
            {
                return thing.def.uiIcon;
            }

            if (value is BuildableDef buildableDef && buildableDef.uiIcon != null)
            {
                return buildableDef.uiIcon;
            }

            return TryGetFirstTextureMember(value, "uiIcon", "Icon", "icon", "Texture", "texture") ?? BaseContent.BadTex;
        }

        private static string BuildRowPreview(IIndexed<object> row)
        {
            if (row == null || row.Value == null)
            {
                return "<null>";
            }

            var text = row.Value.ToString();
            if (string.IsNullOrEmpty(text))
            {
                return "<empty>";
            }

            return text.Length <= 220 ? text : text.Substring(0, 217) + "...";
        }

        private static bool TryGetPreferredDisplayValue(IIndexed<object> row, out string value)
        {
            if (TryGetStringValue(row, "Name", out value))
            {
                return true;
            }

            if (TryGetStringValue(row, "Title", out value))
            {
                return true;
            }

            if (TryGetStringValue(row, "DisplayName", out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryGetPreferredIconValue(IIndexed<object> row, out object value)
        {
            if (TryGetObjectValue(row, "Icon", out value))
            {
                return true;
            }

            if (TryGetObjectValue(row, "uiIcon", out value))
            {
                return true;
            }

            if (TryGetObjectValue(row, "Texture", out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryGetStringValue(IIndexed<object> row, string propertyName, out string value)
        {
            value = null;
            try
            {
                var resolvedValue = row.GetValue<object>(propertyName);
                if (resolvedValue == null)
                {
                    return false;
                }

                value = resolvedValue.ToString();
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetObjectValue(IIndexed<object> row, string propertyName, out object value)
        {
            value = null;
            try
            {
                value = row.GetValue<object>(propertyName);
                return value != null;
            }
            catch
            {
                return false;
            }
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

        private void NavigateToSubTable(IReadOnlyTable subTable)
        {
            if (subTable == null)
            {
                return;
            }

            _history.Push(_currentTable);
            _currentTable = subTable;
            _subTableScroll = Vector2.zero;
            RefreshRows();
        }

        private void RefreshRows()
        {
            _rows = EnumerateRows(_currentTable, MaxDisplayedRows, out _rowsTruncated);
            _rowGridScroll = Vector2.zero;
        }

        private static List<IIndexed<object>> EnumerateRows(IReadOnlyTable table, int maxRows, out bool truncated)
        {
            var rows = new List<IIndexed<object>>();
            truncated = false;

            if (!(table is IEnumerable enumerable))
            {
                return rows;
            }

            foreach (var row in enumerable)
            {
                rows.Add(AdaptRow(row));
                if (rows.Count >= maxRows)
                {
                    truncated = true;
                    break;
                }
            }

            return rows;
        }

        private static IIndexed<object> AdaptRow(object row)
        {
            if (row == null)
            {
                return new ReflectiveIndexedRow(null, EmptyMetadata);
            }

            var type = row.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                if (valueProperty != null)
                {
                    var indexedValue = valueProperty.GetValue(row);
                    if (indexedValue != null)
                    {
                        row = indexedValue;
                    }
                }
            }

            if (row is IIndexed<object> objectIndexed)
            {
                return objectIndexed;
            }

            var indexedInterface = row.GetType().GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIndexed<>));

            if (indexedInterface != null)
            {
                return new IndexedRowAdapter(row, indexedInterface);
            }

            return new ReflectiveIndexedRow(row, EmptyMetadata);
        }

        private sealed class IndexedRowAdapter : IIndexed<object>
        {
            private readonly object _inner;
            private readonly Type _indexedInterface;
            private readonly MethodInfo _genericGetValueMethod;
            private readonly PropertyInfo _valueProperty;
            private readonly PropertyInfo _metadataProperty;

            public IndexedRowAdapter(object inner, Type indexedInterface)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _indexedInterface = indexedInterface ?? throw new ArgumentNullException(nameof(indexedInterface));

                _genericGetValueMethod = _indexedInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .First(x => x.Name == nameof(GetValue) && x.IsGenericMethod && x.GetParameters().Length == 1);
                _valueProperty = _indexedInterface.GetProperty(nameof(Value), BindingFlags.Public | BindingFlags.Instance);
                _metadataProperty = _indexedInterface.GetProperty(nameof(Metadata), BindingFlags.Public | BindingFlags.Instance);
            }

            public object Value => _valueProperty?.GetValue(_inner);

            public IReadOnlyDictionary<string, object> Metadata => _metadataProperty?.GetValue(_inner) as IReadOnlyDictionary<string, object> ?? EmptyMetadata;

            public TValue GetValue<TValue>(string propertyName)
            {
                try
                {
                    var result = _genericGetValueMethod.MakeGenericMethod(typeof(TValue)).Invoke(_inner, new object[] { propertyName });
                    return result is TValue value ? value : default;
                }
                catch
                {
                    return default;
                }
            }
        }

        private sealed class ReflectiveIndexedRow : IIndexed<object>
        {
            public ReflectiveIndexedRow(object value, IReadOnlyDictionary<string, object> metadata)
            {
                Value = value;
                Metadata = metadata ?? EmptyMetadata;
            }

            public object Value { get; }

            public IReadOnlyDictionary<string, object> Metadata { get; }

            public TValue GetValue<TValue>(string propertyName)
            {
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    return default;
                }

                if (Metadata != null)
                {
                    if (Metadata.TryGetValue(propertyName, out var metadataValue))
                    {
                        if (TryConvert(metadataValue, out TValue convertedMetadataValue))
                        {
                            return convertedMetadataValue;
                        }
                    }

                    var metadataMatch = Metadata.FirstOrDefault(x => string.Equals(x.Key, propertyName, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(metadataMatch.Key) && TryConvert(metadataMatch.Value, out TValue convertedInsensitiveMetadataValue))
                    {
                        return convertedInsensitiveMetadataValue;
                    }
                }

                if (Value == null)
                {
                    return default;
                }

                var type = Value.GetType();
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null && property.CanRead && TryConvert(property.GetValue(Value), out TValue convertedPropertyValue))
                {
                    return convertedPropertyValue;
                }

                var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null && TryConvert(field.GetValue(Value), out TValue convertedFieldValue))
                {
                    return convertedFieldValue;
                }

                return default;
            }

            private static bool TryConvert<TValue>(object input, out TValue value)
            {
                if (input == null)
                {
                    value = default;
                    return false;
                }

                if (input is TValue typedValue)
                {
                    value = typedValue;
                    return true;
                }

                try
                {
                    value = (TValue)Convert.ChangeType(input, typeof(TValue));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }
        }

        private static int? TryCount(IReadOnlyTable table)
        {
            if (!(table is IEnumerable enumerable))
            {
                return null;
            }

            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
            }

            return count;
        }
    }
}
