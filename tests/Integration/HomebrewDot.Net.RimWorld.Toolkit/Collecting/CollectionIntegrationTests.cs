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
using Verse;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Collecting
{
    [Trait("Category", "Integration")]
    public class CollectionIntegrationTests
    {
        public CollectionIntegrationTests()
        {
            Toolkit.ConfigureServices();
        }

        [Fact]
        public void WhenFilterIsCreatedOnDefName_CorrectDefsAreFiltered()
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
                                Toolkit.Indexing.Manager.Push(pureDef);
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
                                                                var table = x.GetTable<ThingDef>(Toolkit.Indexing.Def.Thing.FullTableName);
                                                               return table.Version;
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
    }
}
