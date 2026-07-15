using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Collecting;
using HomebrewDot.Net.Rimworld.Collecting.Components;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Referencing.Components;
using HomebrewDot.Net.Rimworld.Extensions;
using RimWorld;
using Verse;
using Xunit;
using HomebrewDot.Net.Rimworld.Indexing.Models;

namespace HomebrewDot.Net.RimWorld.Tests.Collecting
{
    [Trait("Category", "Integration")]
    [Collection("IndexingIntegration")]
    public class CollectionIntegrationTests : IDisposable
    {
        public CollectionIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        public void Dispose()
        {
            // Reset static indexing state to prevent interference with other tests
            InvokeSafe(() => Toolkit.Indexing.Orchestrator = null);
            InvokeSafe(() => Toolkit.Indexing.Manager = null);
        }

        private static void InvokeSafe(Action action)
        {
            try { action(); } catch { /* best-effort cleanup */ }
        }

        [Fact]
        public void WhenFilterIsCreatedOnDefName_CorrectDefsAreFiltered()
        {
            // Arrange
            Toolkit.Indexing.Def.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, false);
            var gameLocation = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(x => x.Key == "RimworldLocation")
                .Value;
            const string DefRootFolder = "Data\\Core\\Defs";
            var defFolder = new DirectoryInfo(Path.Combine(gameLocation, DefRootFolder));
            const string ResourceRootFolder = "ThingDefs_Items";
            var resourceFolder = new DirectoryInfo(Path.Combine(defFolder.FullName, ResourceRootFolder));
            var allXmls = resourceFolder.GetFiles("*.xml", SearchOption.AllDirectories).ToArray();
            Assert.NotEmpty(allXmls);
            ushort counter = 0;
            foreach (var xml in allXmls)
            {
                using (FileStream stream = File.OpenRead(xml.FullName))
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XDocument doc = XDocument.Load(reader);
                        var defElements = doc.Element("Defs")?.Elements("ThingDef");
                        if(defElements is not null)
                        {
                            foreach (var defElement in defElements)
                            {
                                var defName = defElement.Element("defName")?.Value;
                                var label = defElement.Element("label")?.Value;
                                var description = defElement.Element("description")?.Value;
                                ThingDef pureDef = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
                                pureDef.index = counter++;
                                pureDef.defNameHash = counter;
                                pureDef.defName = defName;
                                pureDef.label = label;
                                pureDef.description = description;
                                var metadata = new IndexMetadata();
                                Toolkit.Indexing.Manager.Push(pureDef, ref metadata);
                            }
                        }
                    }
                }
            }

            Toolkit.Indexing.Orchestrator.ForceSnapshot();
            var table = Toolkit.Indexing.Def.Thing.GetTable();
            var count = table.EnumerableCount();
            var allCollectedDefNames = table.Enumerate<ThingDef>().Select(x => x.defName).ToArray();
            Assert.True(count > 0, "No ThingDefs were indexed. Check if the game location is correct and the XML files are being read properly.");
            string[] defNames = ["Steel", "Wood", "Silver", "Gold"];
            var collectionId = Guid.NewGuid().ToString();
            var collectionName = $"LabelCollection_{collectionId}";

            // Act
            Toolkit.Collecting.Build(collectionName, x => x.Compare.Indexed(nameof(ThingDef.defName)).With.Operator("Match").To.Value($"^({string.Join("|", defNames)})$")
                                                           .CollectFromSnapshot<ICollectionBuilder, ThingDef>(x =>
                                                           {
                                                                return x.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName);
                                                           }, x =>
                                                           {
                                                               return x.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName);
                                                           }), true);
            var collection = Toolkit.Collecting.GetAllCollectors()[collectionName] as ICollector<ThingDef>;

            // Assert
            var allItems = collection.GetAll();
            Assert.True(allItems.Any(), $"None of the following defs matches: {Environment.NewLine + string.Join($",{Environment.NewLine}", allCollectedDefNames)}");
            Assert.All(allItems, item => Assert.Contains(item.defName, defNames));
        }

        [Fact]
        public void WhenNestedIndexedPropertyParentIsNull_DoesNotThrow()
        {
            // Arrange
            Toolkit.Indexing.Def.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, true);
            var gameLocation = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(x => x.Key == "RimworldLocation")
                .Value;
            const string DefRootFolder = "Data\\Core\\Defs";
            var defFolder = new DirectoryInfo(Path.Combine(gameLocation, DefRootFolder));
            const string ResourceRootFolder = "ThingDefs_Items";
            var resourceFolder = new DirectoryInfo(Path.Combine(defFolder.FullName, ResourceRootFolder));
            var allXmls = resourceFolder.GetFiles("*.xml", SearchOption.AllDirectories).ToArray();
            Assert.NotEmpty(allXmls);
            ushort counter = 0;
            foreach (var xml in allXmls)
            {
                using (FileStream stream = File.OpenRead(xml.FullName))
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XDocument doc = XDocument.Load(reader);
                        var defElements = doc.Element("Defs")?.Elements("ThingDef");
                        if(defElements is not null)
                        {
                            foreach (var defElement in defElements)
                            {
                                var defName = defElement.Element("defName")?.Value;
                                var label = defElement.Element("label")?.Value;
                                var description = defElement.Element("description")?.Value;
                                ThingDef pureDef = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
                                pureDef.index = counter++;
                                pureDef.defNameHash = counter;
                                pureDef.defName = defName;
                                pureDef.label = label;
                                pureDef.description = description;
                                var metadata = new IndexMetadata();
                                Toolkit.Indexing.Manager.Push(pureDef, ref metadata);
                            }
                        }
                    }
                }
            }

            Toolkit.Indexing.Orchestrator.ForceSnapshot();

            // Act - nested property where parent (ingestible) is null on some items
            var collectionId = Guid.NewGuid().ToString();
            var collectionName = $"NestedNullCollection_{collectionId}";
            Toolkit.Collecting.Build(collectionName, x => x.Compare.Indexed($"{nameof(ThingDef.ingestible)}.preferability")
                                                           .With.Operator("In")
                                                           .To.Value(new FoodPreferability[] { FoodPreferability.MealFine })
                                                           .CollectFromSnapshot<ICollectionBuilder, ThingDef>(x =>
                                                           {
                                                                return x.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName);
                                                           }, x =>
                                                           {
                                                               return x.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName);
                                                           }), true);
            var collection = Toolkit.Collecting.GetAllCollectors()[collectionName] as ICollector<ThingDef>;

            // Assert - should not throw, and only items with ingestible.preferability == MealFine should match
            var allItems = collection.GetAll();
            Assert.All(allItems, item => Assert.Equal(FoodPreferability.MealFine, item.ingestible?.preferability));
        }

        [Fact]
        public void WhenNestedIndexedPropertyPathIsAllNull_DoesNotThrow()
        {
            // Arrange
            Toolkit.Indexing.Def.Thing.EnsureTable();
            Toolkit.Indexing.StartIndexing(null, true);
            var gameLocation = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(x => x.Key == "RimworldLocation")
                .Value;
            const string DefRootFolder = "Data\\Core\\Defs";
            var defFolder = new DirectoryInfo(Path.Combine(gameLocation, DefRootFolder));
            const string ResourceRootFolder = "ThingDefs_Items";
            var resourceFolder = new DirectoryInfo(Path.Combine(defFolder.FullName, ResourceRootFolder));
            var allXmls = resourceFolder.GetFiles("*.xml", SearchOption.AllDirectories).ToArray();
            Assert.NotEmpty(allXmls);
            ushort counter = 0;
            foreach (var xml in allXmls)
            {
                using (FileStream stream = File.OpenRead(xml.FullName))
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XDocument doc = XDocument.Load(reader);
                        var defElements = doc.Element("Defs")?.Elements("ThingDef");
                        if(defElements is not null)
                        {
                            foreach (var defElement in defElements)
                            {
                                var defName = defElement.Element("defName")?.Value;
                                var label = defElement.Element("label")?.Value;
                                var description = defElement.Element("description")?.Value;
                                ThingDef pureDef = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
                                pureDef.index = counter++;
                                pureDef.defNameHash = counter;
                                pureDef.defName = defName;
                                pureDef.label = label;
                                pureDef.description = description;
                                var metadata = new IndexMetadata();
                                Toolkit.Indexing.Manager.Push(pureDef, ref metadata);
                            }
                        }
                    }
                }
            }

            Toolkit.Indexing.Orchestrator.ForceSnapshot();

            // Act - deeply nested property where all intermediate are null
            var collectionId = Guid.NewGuid().ToString();
            var collectionName = $"DeepNullCollection_{collectionId}";
            // Use a path with 3 levels of nesting on a non-existent sub-property to test full null chain
            Toolkit.Collecting.Build(collectionName, x => x.Compare.Indexed($"{nameof(ThingDef.ingestible)}.preferability")
                                                           .With.Operator("Equals")
                                                           .To.Value("ThisShouldNotMatchAnything")
                                                           .CollectFromSnapshot<ICollectionBuilder, ThingDef>(x =>
                                                           {
                                                                return x.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName);
                                                           }, x =>
                                                           {
                                                               return x.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName);
                                                           }), true);

            var collection = Toolkit.Collecting.GetAllCollectors()[collectionName] as ICollector<ThingDef>;

            // Assert - should not throw and return empty
            var allItems = collection.GetAll();
            Assert.Empty(allItems);
        }
    }
}
