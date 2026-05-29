using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.RimWorld.Collecting;
using HomebrewDot.Net.RimWorld.Collecting.Components;
using HomebrewDot.Net.RimWorld.Comparing;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Referencing.Components;
using HomebrewDot.Net.RimWorld.UI.Settings;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.RimWorld
{
    /// <summary>
    /// Developer tab that visualizes configured collections and collectors.
    /// </summary>
    internal sealed class DebugCollectionsUiTab : IToolkitSettingsTab
    {
        private Vector2 _scroll = Vector2.zero;

        private readonly struct CollectionDisplayEntry
        {
            public CollectionDisplayEntry(string name, ICollectionDef definition, ICollector collector)
            {
                Name = name;
                Definition = definition;
                Collector = collector;
            }

            public string Name { get; }

            public ICollectionDef Definition { get; }

            public ICollector Collector { get; }
        }

        /// <inheritdoc/>
        public string Title => "Debug Collections";

        /// <inheritdoc/>
        public void Draw(Rect rect)
        {
            var definitions = Toolkit.Collecting.GetAllDefinitions();
            var collectors = Toolkit.Collecting.GetAllCollectors();

            var headerRect = new Rect(rect.x, rect.y, rect.width, 42f);
            Widgets.Label(headerRect, $"Collections: {definitions.Count} | Collectors: {collectors.Count}\nInspect and bootstrap debug collections.");

            var actionsRect = new Rect(rect.x, headerRect.yMax + 6f, rect.width, 32f);
            DrawActions(actionsRect);

            definitions = Toolkit.Collecting.GetAllDefinitions();
            collectors = Toolkit.Collecting.GetAllCollectors();
            var entries = BuildCollectionEntries(definitions, collectors);

            var outRect = new Rect(rect.x, actionsRect.yMax + 8f, rect.width, Mathf.Max(0f, rect.height - (actionsRect.yMax - rect.y) - 8f));
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, entries.Count == 0 ? 28f : entries.Count * 24f + 6f));

            Widgets.BeginScrollView(outRect, ref _scroll, viewRect);
            if (entries.Count == 0)
            {
                Widgets.Label(new Rect(0f, 0f, viewRect.width, 22f), "- (no collections)");
            }
            else
            {
                var y = 0f;
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var lineRect = new Rect(0f, y, viewRect.width, 22f);
                    if (Mouse.IsOver(lineRect))
                    {
                        Widgets.DrawHighlight(lineRect);
                    }

                    var conditionCount = entry.Definition?.Conditions?.Count ?? 0;
                    var inclusionCount = entry.Definition?.Inclusions?.Count ?? 0;
                    var exclusionCount = entry.Definition?.Exclusions?.Count ?? 0;
                    var collectedCount = entry.Collector?.Count ?? 0;

                    Widgets.Label(lineRect, $"- {entry.Name} | conditions: {conditionCount}, inclusions: {inclusionCount}, exclusions: {exclusionCount}, collector: {(entry.Collector != null ? "yes" : "no")}, collected: {collectedCount}");
                    if (Widgets.ButtonInvisible(lineRect))
                    {
                        Find.WindowStack.Add(new CollectionDetailsWindow(entry.Name));
                    }

                    y += 24f;
                }
            }
            Widgets.EndScrollView();
        }

        private static void DrawActions(Rect rect)
        {
            const float buttonGap = 8f;
            var buttonWidth = (rect.width - buttonGap) / 2f;

            var loadRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            Widgets.DrawMenuSection(loadRect);
            if (Widgets.ButtonInvisible(loadRect))
            {
                LoadDebugCollections();
            }
            Widgets.Label(loadRect.ContractedBy(4f), "Load Debug Collections");

            var refreshRect = new Rect(loadRect.xMax + buttonGap, rect.y, buttonWidth, rect.height);
            Widgets.DrawMenuSection(refreshRect);
            if (Widgets.ButtonInvisible(refreshRect))
            {
                Toolkit.Collecting.StartCollection();
            }
            Widgets.Label(refreshRect.ContractedBy(4f), "Restart Collection");
        }

        private static void LoadDebugCollections()
        {
            var collectionTable = $"{nameof(Def)}.{nameof(ThingDef)}.Weapon.Ranged";
            var getThings = new Func<IReadOnlyDatabase, IEnumerable<IIndexed<ThingDef>>>(s => s.GetTable<ThingDef>(collectionTable));
            Toolkit.Collecting.Build("Snipers", b => b.Compare.Indexed(ToolkitConstants.Stats.Weapon.Def.Range).With.GreaterThanOrEqual().To.Value(30)
                                                      .CollectFromSnapshot(getThings)
                                    );
            Toolkit.Collecting.Build("ShortRange", b => b.Compare.Indexed(ToolkitConstants.Stats.Weapon.Def.Range).With.LessThanOrEqual(15)
                                                         .CollectFromSnapshot(getThings)
                                    );
            Toolkit.Collecting.StartCollection();
        }

        private static List<CollectionDisplayEntry> BuildCollectionEntries(IReadOnlyDictionary<string, ICollectionDef> definitions, IReadOnlyDictionary<string, ICollector> collectors)
        {
            var lines = new List<CollectionDisplayEntry>();
            if (definitions.Count == 0)
            {
                return lines;
            }

            foreach (var definitionPair in definitions.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var name = definitionPair.Key;
                var definition = definitionPair.Value;
                collectors.TryGetValue(name, out var collector);

                lines.Add(new CollectionDisplayEntry(name, definition, collector));
            }

            return lines;
        }
    }
}
