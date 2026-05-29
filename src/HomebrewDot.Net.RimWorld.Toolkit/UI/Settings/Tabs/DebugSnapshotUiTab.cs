using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Components;
using HomebrewDot.Net.RimWorld.UI.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.RimWorld
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
            var snapshot = Toolkit.Index.Manager?.DatabaseSnapshot;
            if (snapshot == null)
            {
                Widgets.Label(rect, "No snapshot available.");
                return;
            }

            var headerRect = new Rect(rect.x, rect.y, rect.width, 42f);
            Widgets.Label(headerRect, $"Snapshot Version: {snapshot.Version}\nTables (name + best-effort count):");

            var tableEntries = BuildTableEntries(snapshot);
            var hasTables = tableEntries.Count != 0;

            const float emptyStateButtonHeight = 32f;
            const float emptyStateListGap = 8f;
            if (!hasTables)
            {
                var emptyStateRect = new Rect(rect.x, headerRect.yMax + 6f, rect.width, emptyStateButtonHeight);
                var buttonRect = new Rect(emptyStateRect.x, emptyStateRect.y, Mathf.Min(280f, emptyStateRect.width), emptyStateRect.height);
                Widgets.DrawMenuSection(buttonRect);
                if (Widgets.ButtonInvisible(buttonRect))
                {
                    Toolkit.Index.Configure(x => x.With(DefGatherer.Instance));
                    Toolkit.Index.ConfigureSchema(LoadDebugTables);
                    Toolkit.Index.StartIndexing(Current.Game, true);
                    snapshot = Toolkit.Index.Manager?.DatabaseSnapshot;
                }
                Widgets.Label(buttonRect.ContractedBy(4f), "Load Debug Tables");
                tableEntries = BuildTableEntries(snapshot);
                hasTables = tableEntries.Count != 0;
            }

            var topOffset = hasTables ? 0f : emptyStateButtonHeight + emptyStateListGap;
            var outRect = new Rect(rect.x, headerRect.yMax + 6f + topOffset, rect.width, Mathf.Max(0f, rect.height - headerRect.height - 6f - topOffset));
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

        private static void LoadDebugTables(IDatabaseSchemaBuilder builder)
            => builder.WithTable<Def>(nameof(Def), t => t.WithSubTable<ThingDef>(nameof(ThingDef), null, st => st.WithSubTable("Weapon", x => x.IsWeapon, wst => wst.WithSubTable("Melee", x => x.IsMeleeWeapon)
                                                                                                                                                                    .WithSubTable("Ranged", x => x.IsRangedWeapon, rw => rw.OnInserting((d, tb, x) =>
                                                                                                                                                                    {
                                                                                                                                                                        var def = x.Value;
                                                                                                                                                                        var range = def.Verbs?.Max(v => v.range) ?? 0f;
                                                                                                                                                                        x.Set(ToolkitConstants.Stats.Weapon.Def.Range, range);
                                                                                                                                                                    }))
                                                                                                                              )
                                                                                                                 .WithSubTable("Apparel", x => x.IsApparel)
                                                                                )
                                                         .WithSubTable<PawnKindDef>(nameof(PawnKindDef))
                                                         .WithSubTable<RecipeDef>(nameof(RecipeDef))
                                                         .WithSubTable<ResearchProjectDef>(nameof(ResearchProjectDef))
                                                         .WithSubTable<IncidentDef>(nameof(IncidentDef))
                                                         .WithSubTable<WorldObjectDef>(nameof(WorldObjectDef))
                                                         .WithSubTable<BiomeDef>(nameof(BiomeDef))
                                                         .WithSubTable<BodyDef>(nameof(BodyDef))
                                     );

        private static List<TableDisplayEntry> BuildTableEntries(IReadOnlyDatabase snapshot)
        {
            var entries = new List<TableDisplayEntry>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in snapshot.GetTables().OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!visited.Add(table.Name))
                {
                    continue;
                }

                AppendTableEntry(entries, table, visited, depth: 0);
            }

            return entries;
        }

        private static void AppendTableEntry(List<TableDisplayEntry> entries, IReadOnlyTable table, HashSet<string> visited, int depth)
        {
            entries.Add(new TableDisplayEntry(table, depth));

            var subTables = table.SubTables;
            if (subTables == null || subTables.Count == 0)
            {
                return;
            }

            foreach (var subTable in subTables.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!visited.Add(subTable.Name))
                {
                    continue;
                }

                AppendTableEntry(entries, subTable, visited, depth + 1);
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
