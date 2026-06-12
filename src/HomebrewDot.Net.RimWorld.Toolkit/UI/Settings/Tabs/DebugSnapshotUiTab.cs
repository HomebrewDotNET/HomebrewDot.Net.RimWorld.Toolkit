using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.UI.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld
{
    /// <summary>
    /// Developer tab that visualizes current snapshot tables.
    /// </summary>
    internal sealed class DebugSnapshotUiTab : IToolkitSettingsTab
    {
        private Vector2 _debugOverviewScroll = Vector2.zero;

        private readonly struct TableDisplayEntry
        {
            public TableDisplayEntry(IReadOnlyTable table, int depth)
            {
                Table = table;
                Depth = depth;
            }

            public IReadOnlyTable Table { get; }

            public int Depth { get; }
        }

        /// <inheritdoc/>
        public string Title => "Debug Snapshot";

        /// <inheritdoc/>
        public void Draw(Rect rect)
        {
            var snapshot = Toolkit.Indexing.Manager?.DatabaseSnapshot;
            if (snapshot == null)
            {
                Widgets.Label(rect, "No snapshot available.");
                return;
            }

            var headerRect = new Rect(rect.x, rect.y, rect.width, 42f);
            Widgets.Label(headerRect, $"Snapshot Version: {snapshot.Version}\nTables (name + best-effort count):");

            const float actionGap = 8f;
            var actionRowRect = new Rect(rect.x, headerRect.yMax + 2f, rect.width, 32f);
            var actionButtonWidth = Mathf.Min(200f, (actionRowRect.width - actionGap) / 2f);

            var loadRect = new Rect(actionRowRect.x, actionRowRect.y, actionButtonWidth, actionRowRect.height);
            Widgets.DrawMenuSection(loadRect);
            if (Widgets.ButtonInvisible(loadRect))
            {
                Toolkit.Indexing.ConfigureSchema += Index_ConfigureSchema;
                Toolkit.Indexing.StartIndexing(Current.Game, true);
                snapshot = Toolkit.Indexing.Manager?.DatabaseSnapshot;
            }
            Widgets.Label(loadRect.ContractedBy(4f), "Load Debug Tables");

            var exportRect = new Rect(loadRect.xMax + actionGap, actionRowRect.y, actionButtonWidth, actionRowRect.height);
            Widgets.DrawMenuSection(exportRect);
            if (Widgets.ButtonInvisible(exportRect))
            {
                DebugExportUtility.ExportSnapshotTableSet(snapshot);
            }
            Widgets.Label(exportRect.ContractedBy(4f), "Export Tables");

            var tableEntries = BuildTableEntries(snapshot);
            var hasTables = tableEntries.Count != 0;

            const float emptyStateButtonHeight = 32f;
            const float emptyStateListGap = 8f;
            if (!hasTables)
            {
                var emptyStateRect = new Rect(rect.x, actionRowRect.yMax + 6f, rect.width, emptyStateButtonHeight);
                Widgets.Label(emptyStateRect, "No tables loaded yet. Use 'Load Debug Tables'.");
                tableEntries = BuildTableEntries(snapshot);
                hasTables = tableEntries.Count != 0;
            }

            var topOffset = hasTables ? 0f : emptyStateButtonHeight + emptyStateListGap;
            var outRect = new Rect(rect.x, actionRowRect.yMax + 6f + topOffset, rect.width, Mathf.Max(0f, rect.height - (actionRowRect.yMax - rect.y) - 6f - topOffset));
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, tableEntries.Count == 0 ? 28f : tableEntries.Count * 24f + 6f));

            Widgets.BeginScrollView(outRect, ref _debugOverviewScroll, viewRect);
            if (tableEntries.Count == 0)
            {
                Widgets.Label(new Rect(0f, 0f, viewRect.width, 22f), "- (no tables)");
            }
            else
            {
                var y = 0f;
                for (var i = 0; i < tableEntries.Count; i++)
                {
                    var entry = tableEntries[i];
                    var lineRect = new Rect(0f, y, viewRect.width, 22f);
                    if (Mouse.IsOver(lineRect))
                    {
                        Widgets.DrawHighlight(lineRect);
                    }

                    var indent = new string(' ', entry.Depth * 2);
                    var count = TryCount(entry.Table);
                    var text = count.HasValue
                        ? $"{indent}- {entry.Table.Name} ({count.Value})"
                        : $"{indent}- {entry.Table.Name}";
                    Widgets.Label(lineRect, text);

                    if (Widgets.ButtonInvisible(lineRect))
                    {
                        Find.WindowStack.Add(new SnapshotTableDetailsWindow(entry.Table));
                    }

                    y += 24f;
                }
            }
            Widgets.EndScrollView();
        }

        private void Index_ConfigureSchema(IDatabaseSchemaBuilder obj)
        {
            throw new NotImplementedException();
        }

        private static void LoadDebugTables(IDatabaseSchemaBuilder builder)
        {
            Toolkit.Indexing.Def.EnsureGatherer();
            Toolkit.Indexing.Def.Thing.EnsureTable();
            Toolkit.Indexing.Def.Thing.Weapon.Melee.EnsureTable();
            Toolkit.Indexing.Def.Thing.Weapon.Ranged.EnsureTable();
            Toolkit.Indexing.Def.Thing.Apparel.EnsureTable();
            Toolkit.Indexing.Def.ConfigureTable(b => b.WithSubTable<PawnKindDef>(nameof(PawnKindDef))
                                                   .WithSubTable<RecipeDef>(nameof(RecipeDef))
                                                   .WithSubTable<ResearchProjectDef>(nameof(ResearchProjectDef))
                                                   .WithSubTable<IncidentDef>(nameof(IncidentDef))
                                                   .WithSubTable<WorldObjectDef>(nameof(WorldObjectDef))
                                                   .WithSubTable<BiomeDef>(nameof(BiomeDef))
                                                   .WithSubTable<BodyDef>(nameof(BodyDef)));
            Toolkit.Indexing.Thing.EnsureGatherer();
            Toolkit.Indexing.Thing.Resources.EnsureTable();
        }

        private static List<TableDisplayEntry> BuildTableEntries(IReadOnlyDatabase snapshot)
        {
            var entries = new List<TableDisplayEntry>();

            foreach (var table in snapshot.GetTables().OrderBy(t => t.FullName, StringComparer.OrdinalIgnoreCase))
            {
                AppendTableEntry(entries, table, depth: 0);
            }

            return entries;
        }

        private static void AppendTableEntry(List<TableDisplayEntry> entries, IReadOnlyTable table, int depth)
        {
            entries.Add(new TableDisplayEntry(table, depth));

            var subTables = table.SubTables;
            if (subTables == null || subTables.Count == 0)
            {
                return;
            }

            foreach (var subTable in subTables.OrderBy(t => t.FullName, StringComparer.OrdinalIgnoreCase))
            {
                AppendTableEntry(entries, subTable, depth + 1);
            }
        }

        private static int? TryCount(IReadOnlyTable table)
        {
            if (!(table is System.Collections.IEnumerable enumerable))
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
