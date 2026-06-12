using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Indexing;
using RimWorld;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld
{
    internal static class DebugExportUtility
    {
        public static string ExportSnapshotTable(IReadOnlyTable table)
        {
            if (table == null)
            {
                return null;
            }

            var rows = new List<object>();

            if (table is IEnumerable enumerable)
            {
                var exported = 0;
                foreach (var row in enumerable)
                {
                    rows.Add(BuildTableRowPayload(row));
                    exported++;
                }
            }

            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["exportedAtUtc"] = DateTime.UtcNow.ToString("o"),
                ["table"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = table.Name,
                    ["fullName"] = table.FullName,
                    ["isFiltered"] = table.IsFiltered,
                    ["rowCount"] = TryCount(table),
                    ["rowsExported"] = rows.Count,
                },
                ["rows"] = rows,
            };

            return ExportPayload("tables", table.FullName ?? table.Name ?? "table", payload);
        }

        public static string ExportSnapshotTableSet(IReadOnlyDatabase snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            var exportedTables = new List<object>();
            foreach (var table in snapshot.GetTables().OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase))
            {
                var tableRows = new List<object>();
                if (table is IEnumerable enumerable)
                {
                    var exported = 0;
                    foreach (var row in enumerable)
                    {
                        tableRows.Add(BuildTableRowPayload(row));
                        exported++;
                    }
                }

                exportedTables.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = table.Name,
                    ["fullName"] = table.FullName,
                    ["isFiltered"] = table.IsFiltered,
                    ["rowCount"] = TryCount(table),
                    ["rowsExported"] = tableRows.Count,
                    ["rows"] = tableRows,
                });
            }

            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["exportedAtUtc"] = DateTime.UtcNow.ToString("o"),
                ["version"] = snapshot.Version,
                ["tableCount"] = exportedTables.Count,
                ["tables"] = exportedTables,
            };

            return ExportPayload("tables", "all-tables", payload);
        }

        public static string ExportCollections(IReadOnlyDictionary<string, ICollectionDef> definitions, IReadOnlyDictionary<string, ICollector> collectors)
        {
            definitions ??= new Dictionary<string, ICollectionDef>(StringComparer.OrdinalIgnoreCase);
            collectors ??= new Dictionary<string, ICollector>(StringComparer.OrdinalIgnoreCase);

            var exports = new List<object>();
            foreach (var definition in definitions.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                collectors.TryGetValue(definition.Key, out var collector);
                exports.Add(BuildCollectionPayload(definition.Key, definition.Value, collector));
            }

            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["exportedAtUtc"] = DateTime.UtcNow.ToString("o"),
                ["definitionCount"] = definitions.Count,
                ["collectorCount"] = collectors.Count,
                ["collections"] = exports,
            };

            return ExportPayload("collections", "all-collections", payload);
        }

        public static string ExportCollection(string collectionName, ICollectionDef definition, ICollector collector)
        {
            var payload = BuildCollectionPayload(collectionName, definition, collector);
            return ExportPayload("collections", collectionName ?? "collection", payload);
        }

        private static object BuildCollectionPayload(string collectionName, ICollectionDef definition, ICollector collector)
        {
            var items = new List<object>();
            if (collector != null)
            {
                var index = 0;
                foreach (var item in collector.GetAll())
                {
                    items.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["display"] = GetDisplayName(item),
                        ["type"] = item?.GetType().FullName,
                        ["value"] = BuildPreview(item),
                        ["index"] = index,
                        ["intance"] = item,
                    });

                    index++;
                }
            }

            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["exportedAtUtc"] = DateTime.UtcNow.ToString("o"),
                ["name"] = collectionName,
                ["hasDefinition"] = definition != null,
                ["hasCollector"] = collector != null,
                ["collectorCount"] = collector?.Count ?? 0,
                ["conditions"] = definition?.Conditions?.Select(x => x?.ToString() ?? "<null>").ToArray() ?? Array.Empty<string>(),
                ["inclusions"] = definition?.Inclusions?.Select(x => x?.Name ?? "<null>").ToArray() ?? Array.Empty<string>(),
                ["exclusions"] = definition?.Exclusions?.Select(x => x?.Name ?? "<null>").ToArray() ?? Array.Empty<string>(),
                ["itemsExported"] = items.Count,
                ["items"] = items,
            };
        }

        private static object BuildTableRowPayload(object row)
        {
            var adapted = AdaptRow(row);
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (adapted.Metadata != null)
            {
                foreach (var kvp in adapted.Metadata.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    metadata[kvp.Key] = NormalizeScalarValue(kvp.Value);
                }
            }

            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["display"] = GetDisplayName(adapted.Value),
                ["type"] = adapted.Value?.GetType().FullName,
                ["value"] = BuildPreview(adapted.Value),
                ["metadata"] = metadata,
                ["instance"] = adapted.Value,
            };
        }

        private static string ExportPayload(string category, string name, object payload)
        {
            try
            {
                var folder = Path.Combine(GenFilePaths.SaveDataFolderPath, "ToolkitExports", category ?? "debug");
                Directory.CreateDirectory(folder);

                var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{SanitizeFileName(name)}.json";
                var path = Path.Combine(folder, fileName);

                var json = SerializeToJson(payload);
                File.WriteAllText(path, json, Encoding.UTF8);

                Messages.Message($"Exported to: {path}", MessageTypeDefOf.TaskCompletion, historical: false);
                Log($"Debug export created at {path}");

                return path;
            }
            catch (Exception ex)
            {
                LogError($"Failed to export debug data: {ex}");
                Messages.Message("Failed to export debug data. Check logs.", MessageTypeDefOf.RejectInput, historical: false);
                return null;
            }
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "export";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return sanitized.Replace(' ', '_');
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

        private static AdaptedIndexedRow AdaptRow(object row)
        {
            if (row == null)
            {
                return new AdaptedIndexedRow(null, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            }

            var rowType = row.GetType();
            if (rowType.IsGenericType && rowType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var valueProperty = rowType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                if (valueProperty != null)
                {
                    row = valueProperty.GetValue(row);
                }
            }

            if (row is IIndexed<object> indexedObject)
            {
                return new AdaptedIndexedRow(indexedObject.Value, indexedObject.Metadata ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            }

            var indexedInterface = row.GetType().GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIndexed<>));

            if (indexedInterface != null)
            {
                var value = indexedInterface.GetProperty(nameof(IIndexed<object>.Value), BindingFlags.Public | BindingFlags.Instance)?.GetValue(row);
                var metadata = indexedInterface.GetProperty(nameof(IIndexed<object>.Metadata), BindingFlags.Public | BindingFlags.Instance)?.GetValue(row) as IReadOnlyDictionary<string, object>;
                return new AdaptedIndexedRow(value, metadata ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            }

            return new AdaptedIndexedRow(row, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        }

        private static object NormalizeScalarValue(object input)
        {
            if (input == null)
            {
                return null;
            }

            var type = input.GetType();
            if (type.IsEnum)
            {
                return input.ToString();
            }

            if (input is string || input is bool)
            {
                return input;
            }

            if (input is byte || input is sbyte || input is short || input is ushort || input is int || input is uint || input is long || input is ulong || input is float || input is double || input is decimal)
            {
                return input;
            }

            return input.ToString();
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

            if (TryGetStringProperty(item, "Label", out var label))
            {
                return label;
            }

            if (TryGetStringProperty(item, "defName", out var defName))
            {
                return defName;
            }

            return item.GetType().Name;
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

        private static string SerializeToJson(object value)
        {
            var builder = new StringBuilder(4096);
            WriteJson(builder, value, depth: 0);
            return builder.ToString();
        }

        private static void WriteJson(StringBuilder builder, object value, int depth)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (value is string s)
            {
                WriteString(builder, s);
                return;
            }

            if (value is bool b)
            {
                builder.Append(b ? "true" : "false");
                return;
            }

            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            {
                builder.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                return;
            }

            if (value is IDictionary<string, object> dict)
            {
                builder.Append('{');
                var first = true;
                foreach (var kvp in dict)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    WriteString(builder, kvp.Key ?? string.Empty);
                    builder.Append(':');
                    WriteJson(builder, kvp.Value, depth + 1);
                }
                builder.Append('}');
                return;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                builder.Append('[');
                var first = true;
                foreach (var item in enumerable)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    WriteJson(builder, item, depth + 1);
                }
                builder.Append(']');
                return;
            }

            WriteString(builder, value.ToString());
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            if (!string.IsNullOrEmpty(value))
            {
                for (var i = 0; i < value.Length; i++)
                {
                    var ch = value[i];
                    switch (ch)
                    {
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            if (ch < 32)
                            {
                                builder.Append("\\u");
                                builder.Append(((int)ch).ToString("x4"));
                            }
                            else
                            {
                                builder.Append(ch);
                            }
                            break;
                    }
                }
            }
            builder.Append('"');
        }

        private readonly struct AdaptedIndexedRow
        {
            public AdaptedIndexedRow(object value, IReadOnlyDictionary<string, object> metadata)
            {
                Value = value;
                Metadata = metadata;
            }

            public object Value { get; }

            public IReadOnlyDictionary<string, object> Metadata { get; }
        }
    }
}
