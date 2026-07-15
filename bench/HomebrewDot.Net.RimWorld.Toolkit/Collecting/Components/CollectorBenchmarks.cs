using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Comparing.Components;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Referencing.Models;

namespace HomebrewDot.Net.Rimworld.Benchmarks.Collecting.Components
{
    /// <summary>
    /// Benchmarks for <see cref="Collector{T}"/> covering the cost of the collector's own
    /// bookkeeping (hash add/remove, event invocation, lock, snapshot copy) and the cost
    /// of evaluating <see cref="ICollectionDef"/> configurations of increasing complexity.
    ///
    /// Configurations exercised:
    ///  - Empty (no conditions, inclusions or exclusions)
    ///  - Conditions only (using <see cref="PropertyReferenceType"/> to read an entity field)
    ///  - Inclusions only (single sub-collection)
    ///  - Conditions + Inclusions (AND)
    ///  - Conditions + Inclusions (OR)
    ///  - Exclusions only
    ///  - Conditions + Inclusions + Exclusions (combined)
    ///  - Inverted inclusion (NOT in collection)
    ///  - Deep nested inclusions (recursive resolution)
    ///
    /// Each configuration also exposes CanCollect / Contains / GetAll variants where useful
    /// so the collector's own overhead can be isolated from the comparator's work.
    ///
    /// Operator types and reference types are pulled from <see cref="Toolkit.Services"/> —
    /// the same way the production comparator is wired up by <see cref="Toolkit.Collecting.Comparator"/>.
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class CollectorBenchmarks
    {
        // ── Parameters ────────────────────────────────────────────────────────

        [Params(100, 1000)]
        public int ItemCount { get; set; }

        [Params(50)]
        public int MatchPercent { get; set; }

        // ── Shared state ──────────────────────────────────────────────────────

        private CollectorBenchmarkEntity[] _matchingItems;
        private CollectorBenchmarkEntity[] _nonMatchingItems;
        private CollectorBenchmarkEntity[] _mixedItems;

        // Comparator stubs / fast-path
        private AlwaysTrueComparator _alwaysTrue;
        private AlwaysFalseComparator _alwaysFalse;

        // Real comparator wiring — built from the Toolkit services registry
        // (same source as production code in Toolkit.Collecting.Comparator)
        private IComparator _conditionComparator;
        private CollectionComparator _collectionComparator;

        // Pre-built collections + per-config collectors
        private IReadOnlyDictionary<string, ICollectionDef> _collectionsAnd;
        private IReadOnlyDictionary<string, ICollectionDef> _collectionsOr;
        private IReadOnlyDictionary<string, ICollectionDef> _collectionsInverted;
        private IReadOnlyDictionary<string, ICollectionDef> _collectionsDeep;

        private Collector<CollectorBenchmarkEntity> _emptyCollector;
        private Collector<CollectorBenchmarkEntity> _conditionsOnlyCollector;
        private Collector<CollectorBenchmarkEntity> _inclusionsOnlyCollector;
        private Collector<CollectorBenchmarkEntity> _conditionsAndInclusionsCollector;
        private Collector<CollectorBenchmarkEntity> _conditionsOrInclusionsCollector;
        private Collector<CollectorBenchmarkEntity> _exclusionsOnlyCollector;
        private Collector<CollectorBenchmarkEntity> _combinedCollector;
        private Collector<CollectorBenchmarkEntity> _invertedInclusionCollector;
        private Collector<CollectorBenchmarkEntity> _deepNestedCollector;
        private Collector<CollectorBenchmarkEntity> _alwaysTrueCollector;

        // Pre-started collectors that match everything (for Collect()/Contains()/GetAll paths)
        private Collector<CollectorBenchmarkEntity> _matchAllCollectCollector;
        private Collector<CollectorBenchmarkEntity> _matchAllContainsCollector;
        private Collector<CollectorBenchmarkEntity> _matchAllGetAllCollector;

        // ── Setup ─────────────────────────────────────────────────────────────

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Register all built-in reference and operator types — same call
            // the production Mod constructor makes, so the Comparator below
            // resolves "Property" references and the "Equals" operator alias
            // just like at runtime.
            Toolkit.ConfigureServices();

            var referenceTypes = Toolkit.Services.GetAllNamed<IReferenceType>();
            var referenceResolver = new ReferenceResolver(referenceTypes);
            var operatorTypes = Toolkit.Services.GetAllNamed<IOperatorType>();
            _conditionComparator = new Comparator(referenceResolver, operatorTypes);
            _collectionComparator = new CollectionComparator(_conditionComparator);

            _matchingItems = Enumerable.Range(0, ItemCount)
                .Select(i => new CollectorBenchmarkEntity($"item-{i}", targetGroup: "A"))
                .ToArray();

            _nonMatchingItems = Enumerable.Range(0, ItemCount)
                .Select(i => new CollectorBenchmarkEntity($"item-{i}", targetGroup: "Z"))
                .ToArray();

            _mixedItems = InterleaveByPercent(_matchingItems, _nonMatchingItems, MatchPercent);

            _alwaysTrue = new AlwaysTrueComparator();
            _alwaysFalse = new AlwaysFalseComparator();

            BuildCollections();

            // Per-config collectors. We do NOT call StartCollecting in the
            // benchmarks that measure "start + collect" so the cost of starting
            // is part of the measurement when relevant.
            _emptyCollector = new Collector<CollectorBenchmarkEntity>(new CollectionDef());
            _conditionsOnlyCollector = new Collector<CollectorBenchmarkEntity>(BuildConditionsOnlyDef());
            _inclusionsOnlyCollector = new Collector<CollectorBenchmarkEntity>(BuildInclusionsOnlyDef());
            _conditionsAndInclusionsCollector = new Collector<CollectorBenchmarkEntity>(BuildConditionsAndInclusionsDef(andMode: true));
            _conditionsOrInclusionsCollector = new Collector<CollectorBenchmarkEntity>(BuildConditionsAndInclusionsDef(andMode: false));
            _exclusionsOnlyCollector = new Collector<CollectorBenchmarkEntity>(BuildExclusionsOnlyDef());
            _combinedCollector = new Collector<CollectorBenchmarkEntity>(BuildCombinedDef());
            _invertedInclusionCollector = new Collector<CollectorBenchmarkEntity>(BuildInvertedInclusionDef());
            _deepNestedCollector = new Collector<CollectorBenchmarkEntity>(BuildDeepNestedDef());
            _alwaysTrueCollector = new Collector<CollectorBenchmarkEntity>(new CollectionDef());

            _matchAllCollectCollector = new Collector<CollectorBenchmarkEntity>(new CollectionDef());
            _matchAllContainsCollector = new Collector<CollectorBenchmarkEntity>(new CollectionDef());
            _matchAllGetAllCollector = new Collector<CollectorBenchmarkEntity>(new CollectionDef());

            _matchAllCollectCollector.StartCollecting(_alwaysTrue, new Dictionary<string, ICollectionDef>());
            _matchAllContainsCollector.StartCollecting(_alwaysTrue, new Dictionary<string, ICollectionDef>());
            _matchAllGetAllCollector.StartCollecting(_alwaysTrue, new Dictionary<string, ICollectionDef>());
        }

        // ── Collect: configuration matrix ────────────────────────────────────
        // Each call iterates over a pre-built mix of matching and non-matching
        // items. The matching ratio is controlled by MatchPercent.

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_Empty()
        {
            _emptyCollector.StartCollecting(_alwaysTrue, new Dictionary<string, ICollectionDef>());
            return RunCollect(_emptyCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_AlwaysTrue()
        {
            _alwaysTrueCollector.StartCollecting(_alwaysTrue, new Dictionary<string, ICollectionDef>());
            return RunCollect(_alwaysTrueCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_AlwaysFalse()
        {
            _emptyCollector.StartCollecting(_alwaysFalse, new Dictionary<string, ICollectionDef>());
            return RunCollect(_emptyCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_ConditionsOnly()
        {
            _conditionsOnlyCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCollect(_conditionsOnlyCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_InclusionsOnly()
        {
            _inclusionsOnlyCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCollect(_inclusionsOnlyCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_ConditionsAndInclusions_And()
        {
            _conditionsAndInclusionsCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCollect(_conditionsAndInclusionsCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_ConditionsAndInclusions_Or()
        {
            _conditionsOrInclusionsCollector.StartCollecting(_collectionComparator, _collectionsOr);
            return RunCollect(_conditionsOrInclusionsCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_ExclusionsOnly()
        {
            _exclusionsOnlyCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCollect(_exclusionsOnlyCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_Combined()
        {
            _combinedCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCollect(_combinedCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_InvertedInclusion()
        {
            _invertedInclusionCollector.StartCollecting(_collectionComparator, _collectionsInverted);
            return RunCollect(_invertedInclusionCollector);
        }

        [Benchmark, BenchmarkCategory("Collect_Config")]
        public int Collect_DeepNestedInclusions()
        {
            _deepNestedCollector.StartCollecting(_collectionComparator, _collectionsDeep);
            return RunCollect(_deepNestedCollector);
        }

        // ── CanCollect: configuration matrix ────────────────────────────────
        // Isolate the match evaluation from the add/remove + event paths.

        [Benchmark, BenchmarkCategory("CanCollect_Config")]
        public int CanCollect_AlwaysTrue()
        {
            _alwaysTrueCollector.StartCollecting(_alwaysTrue, new Dictionary<string, ICollectionDef>());
            return RunCanCollect(_alwaysTrueCollector);
        }

        [Benchmark, BenchmarkCategory("CanCollect_Config")]
        public int CanCollect_AlwaysFalse()
        {
            _emptyCollector.StartCollecting(_alwaysFalse, new Dictionary<string, ICollectionDef>());
            return RunCanCollect(_emptyCollector);
        }

        [Benchmark, BenchmarkCategory("CanCollect_Config")]
        public int CanCollect_ConditionsOnly()
        {
            _conditionsOnlyCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCanCollect(_conditionsOnlyCollector);
        }

        [Benchmark, BenchmarkCategory("CanCollect_Config")]
        public int CanCollect_ConditionsAndInclusions_And()
        {
            _conditionsAndInclusionsCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCanCollect(_conditionsAndInclusionsCollector);
        }

        [Benchmark, BenchmarkCategory("CanCollect_Config")]
        public int CanCollect_Combined()
        {
            _combinedCollector.StartCollecting(_collectionComparator, _collectionsAnd);
            return RunCanCollect(_combinedCollector);
        }

        [Benchmark, BenchmarkCategory("CanCollect_Config")]
        public int CanCollect_DeepNestedInclusions()
        {
            _deepNestedCollector.StartCollecting(_collectionComparator, _collectionsDeep);
            return RunCanCollect(_deepNestedCollector);
        }

        // ── Collect on items that don't change membership ─────────────────────
        // Re-collect the same matching items; the HashSet rejects duplicates so
        // the OnCollected event never fires after the first pass.

        [Benchmark, BenchmarkCategory("Collect_Repeat")]
        public int Collect_AllMatching_ReAddSame()
        {
            int count = 0;
            for (int i = 0; i < _matchingItems.Length; i++)
            {
                if (_matchAllCollectCollector.Collect(_matchingItems[i], null))
                {
                    count++;
                }
            }
            return count;
        }

        // ── Remove on items that are mostly present ───────────────────────────

        [Benchmark, BenchmarkCategory("Collect_Remove")]
        public int Collect_FlipFlop_MatchingNonMatching()
        {
            // Walk through the mixed stream so items constantly enter and leave
            // the collection, exercising both Add and Remove on the HashSet and
            // firing both OnCollected and OnRemoved events.
            int count = 0;
            for (int i = 0; i < _mixedItems.Length; i++)
            {
                if (_matchAllCollectCollector.Collect(_mixedItems[i], null))
                {
                    count++;
                }
            }
            // Then drive the reverse: re-collect with a comparator that now
            // rejects everything, forcing a Remove path.
            _emptyCollector.StartCollecting(_alwaysFalse, new Dictionary<string, ICollectionDef>());
            for (int i = 0; i < _mixedItems.Length; i++)
            {
                _emptyCollector.Collect(_mixedItems[i], null);
            }
            return count;
        }

        // ── Contains ─────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("Contains")]
        public int Contains_AllPresent()
        {
            int hits = 0;
            for (int i = 0; i < _matchingItems.Length; i++)
            {
                if (_matchAllContainsCollector.Contains(_matchingItems[i]))
                {
                    hits++;
                }
            }
            return hits;
        }

        [Benchmark, BenchmarkCategory("Contains")]
        public int Contains_NonePresent()
        {
            int hits = 0;
            for (int i = 0; i < _matchingItems.Length; i++)
            {
                if (_matchAllContainsCollector.Contains(_nonMatchingItems[i]))
                {
                    hits++;
                }
            }
            return hits;
        }

        // ── GetAll ───────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("GetAll")]
        public int GetAll_Snapshot()
        {
            int total = 0;
            for (int i = 0; i < 16; i++)
            {
                var snapshot = _matchAllGetAllCollector.GetAll();
                total += snapshot.Count;
            }
            return total;
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        [Benchmark, BenchmarkCategory("Lifecycle")]
        public int StartStop_Empty()
        {
            var c = new Collector<CollectorBenchmarkEntity>(new CollectionDef());
            c.StartCollecting(_alwaysTrue, new Dictionary<string, ICollectionDef>());
            c.Collect(_matchingItems[0], null);
            c.StopCollecting();
            return c.Count;
        }

        [Benchmark, BenchmarkCategory("Lifecycle")]
        public int Clear_PreservesConfig()
        {
            ((ICollector)_matchAllCollectCollector).Clear();
            return _matchAllCollectCollector.Count;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private int RunCollect(Collector<CollectorBenchmarkEntity> collector)
        {
            int count = 0;
            for (int i = 0; i < _mixedItems.Length; i++)
            {
                if (collector.Collect(_mixedItems[i], null))
                {
                    count++;
                }
            }
            return count;
        }

        private int RunCanCollect(Collector<CollectorBenchmarkEntity> collector)
        {
            int count = 0;
            for (int i = 0; i < _mixedItems.Length; i++)
            {
                if (collector.CanCollect(_mixedItems[i], null))
                {
                    count++;
                }
            }
            return count;
        }

        private static CollectorBenchmarkEntity[] InterleaveByPercent(
            CollectorBenchmarkEntity[] matching,
            CollectorBenchmarkEntity[] nonMatching,
            int matchPercent)
        {
            if (matchPercent < 0) matchPercent = 0;
            if (matchPercent > 100) matchPercent = 100;
            int total = matching.Length + nonMatching.Length;
            var result = new CollectorBenchmarkEntity[total];
            int matchSlots = (int)(total * (matchPercent / 100.0));
            for (int i = 0; i < total; i++)
            {
                if (i < matchSlots)
                {
                    result[i] = matching[i % matching.Length];
                }
                else
                {
                    result[i] = nonMatching[(i - matchSlots) % nonMatching.Length];
                }
            }
            return result;
        }

        private void BuildCollections()
        {
            // "GroupA" — items whose TargetGroup property equals "A"
            var groupA = new StaticCollectionDef(new CollectionDef
            {
                Conditions = new[]
                {
                    new ConditionDef
                    {
                        Compare = new ReferenceDef
                        {
                            Type = PropertyReferenceType.DefaultTypeName,
                            Value = nameof(CollectorBenchmarkEntity.TargetGroup),
                        },
                        With = EqualsOperatorType.DefaultTypeName,
                        To = "A",
                    },
                },
            });
            // "GroupZ" — items whose TargetGroup property equals "Z" (the "non-matching" set)
            var groupZ = new StaticCollectionDef(new CollectionDef
            {
                Conditions = new[]
                {
                    new ConditionDef
                    {
                        Compare = new ReferenceDef
                        {
                            Type = PropertyReferenceType.DefaultTypeName,
                            Value = nameof(CollectorBenchmarkEntity.TargetGroup),
                        },
                        With = EqualsOperatorType.DefaultTypeName,
                        To = "Z",
                    },
                },
            });

            var baseCollections = new Dictionary<string, ICollectionDef>
            {
                ["GroupA"] = groupA,
                ["GroupZ"] = groupZ,
            };
            _collectionsAnd = new Dictionary<string, ICollectionDef>(baseCollections);
            _collectionsOr = new Dictionary<string, ICollectionDef>(baseCollections);
            _collectionsInverted = new Dictionary<string, ICollectionDef>(baseCollections);

            // Deep nested collection: A -> B -> C -> D -> E -> F
            const int deepLevels = 5;
            var deep = new Dictionary<string, ICollectionDef>(baseCollections);
            for (int level = 1; level <= deepLevels; level++)
            {
                deep.Add($"Level{level}", BuildDeepLevel(depth: level, maxDepth: deepLevels));
            }
            deep.Add("DeepRoot", BuildDeepLevel(depth: 0, maxDepth: deepLevels));
            _collectionsDeep = deep;
        }

        private static CollectionDef BuildDeepLevel(int depth, int maxDepth)
        {
            if (depth >= maxDepth)
            {
                return new CollectionDef
                {
                    Conditions = new[]
                    {
                        new ConditionDef
                        {
                            Compare = new ReferenceDef
                            {
                                Type = PropertyReferenceType.DefaultTypeName,
                                Value = nameof(CollectorBenchmarkEntity.TargetGroup),
                            },
                            With = EqualsOperatorType.DefaultTypeName,
                            To = "A",
                        },
                    },
                };
            }

            return new CollectionDef
            {
                Inclusions = new[]
                {
                    new CollectionConditionDef { Name = $"Level{depth + 1}" },
                },
                InclusionsAreOr = false,
            };
        }

        private static StaticCollectionDef BuildConditionsOnlyDef()
        {
            return new StaticCollectionDef(new CollectionDef
            {
                Conditions = new[]
                {
                    new ConditionDef
                    {
                        Compare = new ReferenceDef
                        {
                            Type = PropertyReferenceType.DefaultTypeName,
                            Value = nameof(CollectorBenchmarkEntity.TargetGroup),
                        },
                        With = EqualsOperatorType.DefaultTypeName,
                        To = "A",
                    },
                },
            });
        }

        private static StaticCollectionDef BuildInclusionsOnlyDef()
        {
            return new StaticCollectionDef(new CollectionDef
            {
                Inclusions = new[] { new CollectionConditionDef { Name = "GroupA" } },
                InclusionsAreOr = false,
            });
        }

        private static StaticCollectionDef BuildConditionsAndInclusionsDef(bool andMode)
        {
            return new StaticCollectionDef(new CollectionDef
            {
                Conditions = new[]
                {
                    new ConditionDef
                    {
                        Compare = new ReferenceDef
                        {
                            Type = PropertyReferenceType.DefaultTypeName,
                            Value = nameof(CollectorBenchmarkEntity.TargetGroup),
                        },
                        With = EqualsOperatorType.DefaultTypeName,
                        To = "A",
                    },
                },
                Inclusions = new[] { new CollectionConditionDef { Name = "GroupA" } },
                InclusionsAreOr = !andMode,
            });
        }

        private static StaticCollectionDef BuildExclusionsOnlyDef()
        {
            return new StaticCollectionDef(new CollectionDef
            {
                Exclusions = new[] { new CollectionConditionDef { Name = "GroupZ" } },
            });
        }

        private static StaticCollectionDef BuildCombinedDef()
        {
            return new StaticCollectionDef(new CollectionDef
            {
                Conditions = new[]
                {
                    new ConditionDef
                    {
                        Compare = new ReferenceDef
                        {
                            Type = PropertyReferenceType.DefaultTypeName,
                            Value = nameof(CollectorBenchmarkEntity.TargetGroup),
                        },
                        With = EqualsOperatorType.DefaultTypeName,
                        To = "A",
                    },
                },
                Inclusions = new[] { new CollectionConditionDef { Name = "GroupA" } },
                InclusionsAreOr = false,
                Exclusions = new[] { new CollectionConditionDef { Name = "GroupZ" } },
            });
        }

        private static StaticCollectionDef BuildInvertedInclusionDef()
        {
            // Matches items NOT in "GroupZ" — exclusion-style via inverted inclusion.
            return new StaticCollectionDef(new CollectionDef
            {
                Inclusions = new[] { new CollectionConditionDef { Name = "GroupZ", Inverted = true } },
                InclusionsAreOr = false,
            });
        }

        private static StaticCollectionDef BuildDeepNestedDef()
        {
            return new StaticCollectionDef(new CollectionDef
            {
                Inclusions = new[] { new CollectionConditionDef { Name = "DeepRoot" } },
                InclusionsAreOr = false,
            });
        }

        // ── Comparator stubs ────────────────────────────────────────────────

        private sealed class AlwaysTrueComparator : ICollectionComparator
        {
            public bool Matches<T>(ICollectionDef collection, T obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
            {
                return true;
            }

            public IEnumerable<(T Object, bool Matches)> Matches<T>(ICollectionDef collection, IEnumerable<T> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class AlwaysFalseComparator : ICollectionComparator
        {
            public bool Matches<T>(ICollectionDef collection, T obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
            {
                return false;
            }

            public IEnumerable<(T Object, bool Matches)> Matches<T>(ICollectionDef collection, IEnumerable<T> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
            {
                throw new NotImplementedException();
            }
        }

        // ── Benchmark entity ────────────────────────────────────────────────

        public sealed class CollectorBenchmarkEntity
        {
            public CollectorBenchmarkEntity(string name, string targetGroup)
            {
                Name = name;
                TargetGroup = targetGroup;
            }

            public string Name { get; }
            public string TargetGroup { get; }
        }
    }
}
