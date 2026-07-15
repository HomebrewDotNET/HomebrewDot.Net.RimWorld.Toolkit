using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using Verse;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers.Logging;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Reads from <see cref="DefDatabase{T}"/> and pushes it to the snapshot manager for indexing.
    /// </summary>
    public class DefGatherer : IDataGatherer
    {
        // Statics
        /// <summary>
        /// Singleton instance of the DefGatherer. This class is stateless and can be shared across the application, so we provide a single instance for convenience.
        /// </summary>
        public static DefGatherer Instance { get; } = new DefGatherer();
        private DefGatherer() { }

        /// <inheritdoc/>
        public void GatherData(Game game, ISnapshotManager snapshotManager)
        {
            snapshotManager = Guard.NotNull(snapshotManager, nameof(snapshotManager));

            Log($"Loading all defs...");
            var stopWatch = Stopwatch.StartNew();
            var defs = CollectAllConcreteDefs();
            var elapsed = stopWatch.Elapsed;
            Log($"Loaded {defs.Count} defs in {elapsed.TotalMilliseconds} ms. Pushing to snapshot manager...");
            stopWatch.Reset();
            int accepted = 0;
            foreach (var def in defs)
            {
                var metadata = new IndexMetadata();
                if(snapshotManager.Push(def, ref metadata))
                {
                    accepted++;
                }
            }
            elapsed = stopWatch.Elapsed;
            Log($"Pushed {accepted}/{defs.Count} defs to snapshot manager in {elapsed.TotalMilliseconds}ms.");
        }
        /// <inheritdoc/>
        public void Initialize(Game game)
        {}
        /// <inheritdoc/>
        public void Reset()
        {}

        private static List<Def> CollectAllConcreteDefs()
        {
            var results = new List<Def>();
            var seen = new HashSet<Def>();
            var baseDefType = typeof(Def);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types?.Where(x => x != null).ToArray() ?? Array.Empty<Type>();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || !baseDefType.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    IEnumerable<Def> typedDefs;
                    try
                    {
                        var dbType = typeof(DefDatabase<>).MakeGenericType(type);
                        var defsProperty = dbType.GetProperty(nameof(DefDatabase<Def>.AllDefsListForReading), BindingFlags.Public | BindingFlags.Static);
                        var values = defsProperty?.GetValue(null) as System.Collections.IEnumerable;
                        if (values == null)
                        {
                            continue;
                        }

                        typedDefs = values.OfType<Def>();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var def in typedDefs)
                    {
                        if (def != null && seen.Add(def))
                        {
                            results.Add(def);
                        }
                    }
                }
            }

            return results;
        }
    }
}
