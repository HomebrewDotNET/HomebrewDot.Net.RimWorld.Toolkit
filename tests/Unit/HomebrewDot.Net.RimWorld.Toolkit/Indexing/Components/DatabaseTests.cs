using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Components
{
    public class DatabaseTests
    {
        // ── Constructor / initial state ──────────────────────────────────────

        [Fact]
        public void NewDatabase_HasChangesIsFalse()
        {
            var db = new Database();
            Assert.False(db.HasChanges);
        }

        [Fact]
        public void NewDatabase_IsDeployingIsFalse()
        {
            var db = new Database();
            Assert.False(db.IsDeploying);
        }

        [Fact]
        public void NewDatabase_GetTables_ReturnsEmpty()
        {
            var db = new Database();
            Assert.Empty(db.GetTables());
        }

        // ── Deploy ────────────────────────────────────────────────────────────

        [Fact]
        public void Deploy_WithNullSchemaBuilder_ThrowsArgumentNullException()
        {
            var db = new Database();
            Assert.Throws<ArgumentNullException>(() => db.Deploy(null));
        }

        [Fact]
        public void Deploy_InvokesSchemaBuilderWithSelf()
        {
            var db = new Database();
            IDatabaseSchemaBuilder captured = null;
            db.Deploy(schema => captured = schema);

            Assert.NotNull(captured);
            Assert.Same(db, captured);
        }

        [Fact]
        public void Deploy_SetsIsDeployingTrueDuringSchemaBuilderCallback()
        {
            var db = new Database();
            bool wasDeploying = false;
            db.Deploy(_ => wasDeploying = db.IsDeploying);

            Assert.True(wasDeploying);
            Assert.False(db.IsDeploying); // cleared after callback
        }

        // ── WithTable / GetTable / GetTables ─────────────────────────────────

        [Fact]
        public void WithTable_NullName_ThrowsArgumentNullException()
        {
            var db = new Database();
            Assert.Throws<ArgumentNullException>(() =>
                db.Deploy(schema => schema.WithTable<string>(null, _ => { })));
        }

        [Fact]
        public void WithTable_WithoutBuilder_StillRegistersTable()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", null));

            var table = db.GetTable<string>("Items");

            Assert.NotNull(table);
            Assert.Equal("Items", table.Name);
        }

        [Fact]
        public void WithTable_RegistersTableRetrievableByName()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var table = db.GetTable<string>("Items");
            Assert.NotNull(table);
            Assert.Equal("Items", table.Name);
        }

        [Fact]
        public void WithTable_TwoTables_BothReturnedByGetTables()
        {
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Names", _ => { });
                schema.WithTable<SampleEntity>("Entities", _ => { });
            });

            var tables = db.GetTables().ToList();
            Assert.Equal(2, tables.Count);
        }

        [Fact]
        public void GetTable_WithNullName_ThrowsArgumentNullException()
        {
            var db = new Database();
            Assert.Throws<ArgumentNullException>(() => db.GetTable<string>(null));
        }

        [Fact]
        public void GetTable_WithUnknownName_ReturnsNull()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            Assert.Null(db.GetTable<string>("DoesNotExist"));
        }

        [Fact]
        public void GetTables_ByType_ReturnsOnlyMatchingTables()
        {
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Names", _ => { });
                schema.WithTable<SampleEntity>("Entities", _ => { });
            });

            var stringTables = db.GetTables<string>().ToList();
            Assert.Single(stringTables);
            Assert.Equal("Names", stringTables[0].Name);
        }

        // ── Upsert ────────────────────────────────────────────────────────────

        [Fact]
        public void Upsert_WithNullItem_ThrowsArgumentNullException()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var _md = default(IndexMetadata);
            Assert.Throws<ArgumentNullException>(() => db.Upsert<string>(null, ref _md));
        }

        [Fact]
        public void Upsert_WhenNoTableRegistered_ReturnsFalse()
        {
            var db = new Database();
            // No tables deployed
            var _md0 = default(IndexMetadata);
            var result = db.Upsert("hello", ref _md0);
            Assert.False(result);
        }

        [Fact]
        public void Upsert_WhenTableRegistered_ReturnsTrue()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var _md = default(IndexMetadata);
            var result = db.Upsert("hello", ref _md);

            Assert.True(result);
        }

        [Fact]
        public void Upsert_WhenTableRegistered_SetsHasChangesTrue()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            Assert.True(db.HasChanges);
        }

        // ── Find ─────────────────────────────────────────────────────────────

        [Fact]
        public void Find_WhenItemNotUpserted_ReturnsNull()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var result = db.Find("missing");

            Assert.Null(result);
        }

        [Fact]
        public void Find_AfterUpsert_ReturnsIndexedItemWithCorrectValue()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var result = db.Find("hello");

            Assert.NotNull(result);
            Assert.Equal("hello", result.Value);
        }

        [Fact]
        public void Find_AfterUpsertWithMetadata_ReturnsIndexedItemWithMetadata()
        {
            var db = new Database();
            var tagKey = IndexMetadataKey.Get("tag");
            db.Deploy(schema => schema.WithTable<string>("Items", tb =>
                tb.OnInserting((IWriteableIndexed<string> i, ref IndexMetadata m, IReadOnlyTable<string> t) =>
                {
                    if (m.TryGetValue<string>(tagKey, out var tag))
                        i.Set("tag", tag);
                })));
            var metadata = new IndexMetadata();
            metadata.Set(tagKey, "test");
            db.Upsert("hello", ref metadata);

            var result = db.Find("hello");

            Assert.NotNull(result);
            Assert.Equal("test", result.Metadata["tag"]);
        }

        // ── Delete ────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_WithNullItem_ThrowsArgumentNullException()
        {
            var db = new Database();
            var _md = default(IndexMetadata);
            Assert.Throws<ArgumentNullException>(() =>
                db.Delete<string>((string)null, ref _md));
        }

        [Fact]
        public void Delete_WithExternalIndexed_ReturnsFalse()
        {
            var db = new Database();
            var _md = default(IndexMetadata);
            var result = db.Delete("hello", ref _md);

            Assert.False(result);
        }

        [Fact]
        public void Delete_WithUpsertedItem_ReturnsTrueAndRemovesFromDatabase()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var deleted = db.Delete("hello", ref _md);

            Assert.True(deleted);
            Assert.Null(db.Find("hello"));
        }

        // ── Query ─────────────────────────────────────────────────────────────

        [Fact]
        public void Query_WithNullProperty_ThrowsArgumentNullException()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            Assert.Throws<ArgumentNullException>(() =>
                db.Query<string, string>(null, "search"));
        }

        [Fact]
        public void Query_WithIndex_ReturnsMatchingItems()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));

            var a = new SampleEntity { Name = "Alice" };
            var b = new SampleEntity { Name = "Bob" };
            var _md = default(IndexMetadata);
            db.Upsert(a, ref _md);
            db.Upsert(b, ref _md);

            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");

            Assert.Single(results);
            Assert.Equal("Alice", results.First().Name);
        }

        [Fact]
        public void Query_WithIndex_ReturnsEmptyWhenNoMatch()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name, null, "idx")));

            var _md = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md);

            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Charlie");

            Assert.Empty(results);
        }

        // ── OnInserting / OnInserted callbacks ───────────────────────────────

        [Fact]
        public void OnInserting_CallbackInvokedBeforeUpsert()
        {
            var db = new Database();
            object capturedItem = null;
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Items", _ => { });
                schema.OnInserting((IWriteableIndexed<object> item, ref IndexMetadata meta, IDatabase database) => capturedItem = item.Value);
            });

            var _md1 = default(IndexMetadata);
            db.Upsert("hello", ref _md1);

            Assert.Equal("hello", capturedItem);
        }

        [Fact]
        public void OnInserted_CallbackInvokedAfterUpsert()
        {
            var db = new Database();
            IIndexed<string> capturedIndexed = null;
            db.Deploy(schema =>
                schema.WithTable<string>("Items", tb =>
                    tb.OnInserted((IIndexed<string> indexed, ref IndexMetadata meta, IReadOnlyTable<string> table) => capturedIndexed = indexed)));

            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            Assert.NotNull(capturedIndexed);
            Assert.Equal("hello", capturedIndexed.Value);
        }

        // ── WithTable predicate filter ────────────────────────────────────────

        [Fact]
        public void WithTable_WithFilter_OnlyAcceptsMatchingItems()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("LongNames",
                    _ => { },
                    predicate: entity => entity.Name.Length > 4));

            var _md = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Bob" }, ref _md);    // filtered out (len=3)
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md);  // accepted (len=5)

            var longTable = db.GetTable<SampleEntity>("LongNames");
            Assert.NotNull(longTable);
            Assert.Single(longTable);
        }

        // Helper type used across several tests
        public class SampleEntity
        {
            public string Name { get; set; }
        }

        public class RootEntity
        {
            public string Name { get; set; }
        }

        public class DerivedRootEntity : RootEntity
        {
            public bool IsSpecial { get; set; }
        }

        // ── WithTable – name validation ───────────────────────────────────────

        [Fact]
        public void WithTable_NameContainsSeparatorChar_ThrowsArgumentException()
        {
            var db = new Database();
            Assert.Throws<ArgumentException>(() =>
                db.Deploy(schema => schema.WithTable<string>($"Parent{Database.TableNameSeparator}Child", _ => { })));
        }

        // ── Deploy – re-deploy clears previous tables ──────────────────────

        [Fact]
        public void Deploy_SecondDeploy_ClearsPreviousTables()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("First", _ => { }));
            db.Deploy(schema => schema.WithTable<SampleEntity>("Second", _ => { }));

            var tables = db.GetTables().ToList();
            Assert.Single(tables);
            Assert.Equal("Second", tables[0].Name);
        }

        // ── Snapshot ───────────────────────────────────────────────────────────

        [Fact]
        public void StartSnapshot_ResetsHasChangesToFalse()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);
            Assert.True(db.HasChanges);

            db.StartSnapshot().Build();

            Assert.False(db.HasChanges);
        }

        [Fact]
        public void SnapshotContainsUpsertedItem()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var snapshot = db.StartSnapshot().Build();
            var result = snapshot.Find("hello");

            Assert.NotNull(result);
            Assert.Equal("hello", result.Value);
        }

        // ── Upsert update semantics ───────────────────────────────────────────

        [Fact]
        public void Upsert_SameItemTwice_TableContainsOneItem()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);
            db.Upsert("hello", ref _md);

            var table = db.GetTable<string>("Items");
            Assert.Single(table);
        }

        // ── Delete additional ────────────────────────────────────────────────

        [Fact]
        public void Delete_SetsHasChangesTrue()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);
            db.StartSnapshot().Build(); // reset HasChanges

            db.Delete("hello", ref _md);

            Assert.True(db.HasChanges);
        }

        // ── Table.IsFiltered ──────────────────────────────────────────────────

        [Fact]
        public void Table_IsFiltered_IsFalse_WhenNoPredicateProvided()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var table = db.GetTable<string>("Items");
            Assert.False(table.IsFiltered);
        }

        [Fact]
        public void Table_IsFiltered_IsTrue_WhenPredicateProvided()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }, s => s.Length > 3));

            var table = db.GetTable<string>("Items");
            Assert.True(table.IsFiltered);
        }

        // ── Table enumeration ─────────────────────────────────────────────────

        [Fact]
        public void Table_Enumerable_YieldsAllUpsertedItems()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("alpha", ref _md);
            db.Upsert("beta", ref _md);

            var table = db.GetTable<string>("Items");
            var values = AsIndexed(table).Select(i => i.Value).OrderBy(v => v).ToList();

            Assert.Equal(new[] { "alpha", "beta" }, values);
        }

        // ── GetTable – wrong type conflict ───────────────────────────────────

        [Fact]
        public void GetTable_WrongType_ThrowsInvalidOperationException()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            Assert.Throws<InvalidOperationException>(() => db.GetTable<SampleEntity>("Items"));
        }

        // ── Callbacks – database-level ────────────────────────────────────────

        [Fact]
        public void OnInserted_DatabaseLevel_CalledAfterUpsert()
        {
            var db = new Database();
            IIndexed<object> captured = null;
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Items", _ => { });
                schema.OnInserted((IIndexed<object> i, ref IndexMetadata m, IDatabase d) => captured = i);
            });

            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            Assert.NotNull(captured);
            Assert.Equal("hello", (string)captured.Value);
        }

        [Fact]
        public void OnDeleting_DatabaseLevel_CalledBeforeDelete()
        {
            IIndexed<object> capturedBefore = null;
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Items", _ => { });
                schema.OnDeleting((IIndexed<object> indexed, ref IndexMetadata meta, IDatabase database) => capturedBefore = indexed);
            });
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            db.Delete("hello", ref _md);

            Assert.NotNull(capturedBefore);
        }

        [Fact]
        public void OnDeleted_DatabaseLevel_CalledAfterDelete()
        {
            IIndexed<object> capturedAfter = null;
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Items", _ => { });
                schema.OnDeleted((IIndexed<object> indexed, ref IndexMetadata meta, IDatabase database) => capturedAfter = indexed);
            });
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            db.Delete("hello", ref _md);

            Assert.NotNull(capturedAfter);
        }

        // ── Callbacks – table-level ───────────────────────────────────────────

        [Fact]
        public void OnInserting_TableLevel_CalledOnUpsert()
        {
            string capturedItem = null;
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<string>("Items", tb =>
                    tb.OnInserting((IWriteableIndexed<string> item, ref IndexMetadata m, IReadOnlyTable<string> t) => capturedItem = item.Value)));

            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            Assert.Equal("hello", capturedItem);
        }

        [Fact]
        public void OnDeleting_TableLevel_CalledBeforeDelete()
        {
            IIndexed<string> capturedBefore = null;
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<string>("Items", tb =>
                    tb.OnDeleting((IIndexed<string> indexed, ref IndexMetadata m, IReadOnlyTable<string> t) => capturedBefore = indexed)));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            db.Delete("hello", ref _md);

            Assert.NotNull(capturedBefore);
            Assert.Equal("hello", capturedBefore.Value);
        }

        [Fact]
        public void OnDeleted_TableLevel_CalledAfterDelete()
        {
            IIndexed<string> capturedAfter = null;
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<string>("Items", tb =>
                    tb.OnDeleted((IIndexed<string> indexed, ref IndexMetadata m, IReadOnlyTable<string> t) => capturedAfter = indexed)));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            db.Delete("hello", ref _md);

            Assert.NotNull(capturedAfter);
            Assert.Equal("hello", capturedAfter.Value);
        }

        // ── WithIndex (bool variant) ──────────────────────────────────────────

        [Fact]
        public void WithIndex_BoolVariant_QueryReturnsMatchingItems()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex("HasLongName", e => e.Value.Name.Length > 4, "idx")));

            var alice = new SampleEntity { Name = "Alice" }; // len=5 → true
            var bob = new SampleEntity { Name = "Bob" };     // len=3 → false
            var _md = default(IndexMetadata);
            db.Upsert(alice, ref _md);
            db.Upsert(bob, ref _md);

            var results = db.Query<SampleEntity, bool>("HasLongName", true, indexName: "idx");

            Assert.Single(results);
            Assert.Equal("Alice", results.First().Name);
        }

        // ── WithIndex – cleanup on delete ─────────────────────────────────────

        [Fact]
        public void WithIndex_OnDelete_ItemRemovedFromIndex()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));

            var alice = new SampleEntity { Name = "Alice" };
            var _md1 = default(IndexMetadata);
            db.Upsert(alice, ref _md1);

            db.Delete(alice, ref _md1);

            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            Assert.Empty(results);
        }

        // ── WithIndex – incremental snapshot index keys ──────────────────────

        [Fact]
        public void WithIndex_AfterSnapshot_IncrementalInsert_QueriesByActualIndexKey()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));

            db.StartSnapshot().Build(); // cached snapshot exists so later changes are intent-logged

            var _md = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md);

            var snapshot = db.StartSnapshot().Build();

            // The incremental snapshot index must be keyed by the property value, not the entity
            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            Assert.Single(results);
            Assert.Equal("Alice", results.First().Name);
        }

        [Fact]
        public void WithIndex_AfterSnapshot_IncrementalDelete_RemovesFromActualIndexKey()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));

            db.StartSnapshot().Build();

            var alice = new SampleEntity { Name = "Alice" };
            var bob = new SampleEntity { Name = "Bob" };
            var _md = default(IndexMetadata);
            db.Upsert(alice, ref _md);
            db.Upsert(bob, ref _md);
            db.StartSnapshot().Build(); // incremental insert into the snapshot index

            db.Delete(bob, ref _md);
            var snapshot = db.StartSnapshot().Build();

            // Bob's index entry is removed from the snapshot index...
            Assert.Empty(snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Bob"));
            // ...and Alice's keyed bucket is still intact
            Assert.Single(snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice"));
        }

        [Fact]
        public void WithIndex_ValueChange_MovesItemBetweenLiveBuckets()
        {
            var db = new Database();
            var tagKey = IndexMetadataKey.Get("IndexValueChangeTag");
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
            {
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name);
                tb.OnInserting((IWriteableIndexed<SampleEntity> i, ref IndexMetadata m, IReadOnlyTable<SampleEntity> t) =>
                {
                    if (m.TryGetValue<string>(tagKey, out var tag))
                    {
                        i.Set("tag", tag);
                    }
                });
            }));

            var item = new SampleEntity { Name = "Alice" };
            var md1 = new IndexMetadata();
            md1.Set(tagKey, "a");
            db.Upsert(item, ref md1);

            item.Name = "Alicia"; // mutate and re-upsert (value change)
            var md2 = new IndexMetadata();
            md2.Set(tagKey, "b");
            db.Upsert(item, ref md2);

            // The item moved buckets: it no longer matches the old key, only the new one
            Assert.Empty(db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice"));
            Assert.Single(db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alicia"));
        }

        [Fact]
        public void WithIndex_AfterSnapshot_ValueChange_MovesItemBetweenSnapshotBuckets()
        {
            var db = new Database();
            var tagKey = IndexMetadataKey.Get("IndexSnapshotValueChangeTag");
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
            {
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name);
                tb.OnInserting((IWriteableIndexed<SampleEntity> i, ref IndexMetadata m, IReadOnlyTable<SampleEntity> t) =>
                {
                    if (m.TryGetValue<string>(tagKey, out var tag))
                    {
                        i.Set("tag", tag);
                    }
                });
            }));

            var item = new SampleEntity { Name = "Alice" };
            var md1 = new IndexMetadata();
            md1.Set(tagKey, "a");
            db.Upsert(item, ref md1);
            db.StartSnapshot().Build(); // snapshot has Alice under "Alice"

            item.Name = "Alicia"; // mutate and re-upsert (value change)
            var md2 = new IndexMetadata();
            md2.Set(tagKey, "b");
            db.Upsert(item, ref md2);

            var snapshot = db.StartSnapshot().Build();

            // The incremental snapshot dropped the old keyed bucket and added the new one
            Assert.Empty(snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice"));
            Assert.Single(snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alicia"));
        }

        // ── Query – by table name ─────────────────────────────────────────────

        [Fact]
        public void Query_WithTableName_OnlyQueriesSpecifiedTable()
        {
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<SampleEntity>("Alpha", tb =>
                    tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name));
                schema.WithTable<SampleEntity>("Beta", tb =>
                    tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name));
            });

            var _md = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md);

            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice", tableName: "Alpha");

            Assert.Equal(1, results.Count);
        }

        // ── WithSubTable ──────────────────────────────────────────────────────

        [Fact]
        public void WithSubTable_UpsertPopulatesSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("SubItems", tableBuilder: _ => { })));

            var _md1 = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md1);

            var subTable = db.GetTable<SampleEntity>("Items.SubItems");
            Assert.NotNull(subTable);
            Assert.Single(subTable);
        }

        [Fact]
        public void GetTable_WithDotNotationPath_ReturnsSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Things", tb =>
                    tb.WithSubTable<SampleEntity>("Labels", tableBuilder: _ => { })));

            var subTable = db.GetTable<SampleEntity>("Things.Labels");
            Assert.NotNull(subTable);
        }

        [Fact]
        public void WithSubTable_WithoutBuilder_StillCreatesSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Things", tb =>
                    tb.WithSubTable<SampleEntity>("Labels")));

            var subTable = db.GetTable<SampleEntity>("Things.Labels");

            Assert.NotNull(subTable);
            Assert.Equal("Labels", subTable.Name);
        }

        [Fact]
        public void WithSubTable_WithDerivedSubType_UpsertAddsToRootAndSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<RootEntity>("Root", tb =>
                    tb.WithSubTable<DerivedRootEntity>("Derived")));

            RootEntity value = new DerivedRootEntity { Name = "derived", IsSpecial = true };
            var _md = default(IndexMetadata);
            db.Upsert(value, ref _md);

            var rootTable = db.GetTable<RootEntity>("Root");
            var derivedTable = db.GetTable<DerivedRootEntity>("Root.Derived");

            Assert.NotNull(rootTable);
            Assert.NotNull(derivedTable);
            Assert.Single(rootTable);
            Assert.Single(derivedTable);
            Assert.Equal("derived", AsIndexed(derivedTable).First().Value.Name);
        }

        [Fact]
        public void WithSubTable_WithNestedFilterOnDerivedType_OnlyMatchingDerivedItemsAreIncluded()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<RootEntity>("Root", tb =>
                    tb.WithSubTable<DerivedRootEntity>("Derived", null, st =>
                        st.WithSubTable("Special", x => x.IsSpecial))));

            var _md = default(IndexMetadata);
            db.Upsert<RootEntity>(new DerivedRootEntity { Name = "special", IsSpecial = true }, ref _md);
            db.Upsert<RootEntity>(new DerivedRootEntity { Name = "normal", IsSpecial = false }, ref _md);
            db.Upsert<RootEntity>(new RootEntity { Name = "base" }, ref _md);

            var derivedTable = db.GetTable<DerivedRootEntity>("Root.Derived");
            var specialTable = db.GetTable<DerivedRootEntity>("Root.Derived.Special");

            Assert.NotNull(derivedTable);
            Assert.NotNull(specialTable);
            Assert.Equal(2, AsIndexed(derivedTable).Count());
            Assert.Single(specialTable);
            Assert.Equal("special", AsIndexed(specialTable).First().Value.Name);
        }

        // ── Snapshot – isolation ─────────────────────────────────────────────

        [Fact]
        public void Snapshot_DoesNotReflectItemsAddedAfterSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("before", ref _md);

            var snapshot = db.StartSnapshot().Build();

            db.Upsert("after", ref _md);

            Assert.NotNull(snapshot.Find("before"));
            Assert.Null(snapshot.Find("after"));
        }

        [Fact]
        public void Snapshot_StillContainsItemDeletedAfterSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var snapshot = db.StartSnapshot().Build();

            db.Delete("hello", ref _md);

            // live DB no longer has it
            Assert.Null(db.Find("hello"));
            // snapshot is frozen — still has it
            Assert.NotNull(snapshot.Find("hello"));
        }

        [Fact]
        public void Snapshot_FindReturnsNullForMissingItem()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var snapshot = db.StartSnapshot().Build();

            Assert.Null(snapshot.Find("missing"));
        }

        // ── Snapshot – table access ──────────────────────────────────────────

        [Fact]
        public void Snapshot_GetTableReturnsFrozenTable()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var snapshot = db.StartSnapshot().Build();
            var table = snapshot.GetTable<string>("Items");

            Assert.NotNull(table);
            Assert.Equal("Items", table.Name);
        }

        [Fact]
        public void Snapshot_GetTableReturnsNullForUnknownName()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var snapshot = db.StartSnapshot().Build();

            Assert.Null(snapshot.GetTable<string>("DoesNotExist"));
        }

        [Fact]
        public void Snapshot_GetTablesReturnsAllTables()
        {
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Names", _ => { });
                schema.WithTable<SampleEntity>("Entities", _ => { });
            });

            var snapshot = db.StartSnapshot().Build();

            Assert.Equal(2, snapshot.GetTables().Count());
        }

        [Fact]
        public void Snapshot_GetTablesTypedReturnsOnlyMatchingType()
        {
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Names", _ => { });
                schema.WithTable<SampleEntity>("Entities", _ => { });
            });

            var snapshot = db.StartSnapshot().Build();

            var stringTables = snapshot.GetTables<string>().ToList();
            Assert.Single(stringTables);
            Assert.Equal("Names", stringTables[0].Name);
        }

        [Fact]
        public void Snapshot_TableEnumeratesCorrectItems()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("alpha", ref _md);
            db.Upsert("beta", ref _md);

            var snapshot = db.StartSnapshot().Build();
            var table = snapshot.GetTable<string>("Items");
            var values = AsIndexed(table).Select(i => i.Value).OrderBy(v => v).ToList();

            Assert.Equal(new[] { "alpha", "beta" }, values);
        }

        [Fact]
        public void Snapshot_TablePreservesIsFiltered()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }, s => s.Length > 3));

            var snapshot = db.StartSnapshot().Build();
            var table = snapshot.GetTable<string>("Items");

            Assert.True(table.IsFiltered);
        }

        // ── Snapshot – index data ────────────────────────────────────────────

        [Fact]
        public void Snapshot_QueryViaIndexReturnsMatchingItems()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));
            var _md = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md);
            db.Upsert(new SampleEntity { Name = "Bob" }, ref _md);

            var snapshot = db.StartSnapshot().Build();

            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            Assert.Single(results);
            Assert.Equal("Alice", results.First().Name);
        }

        [Fact]
        public void Snapshot_QueryReturnsEmptyForMissingKey()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));
            var _md = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md);

            var snapshot = db.StartSnapshot().Build();

            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Charlie");
            Assert.Empty(results);
        }

        [Fact]
        public void Snapshot_IndexDoesNotReflectDeletesAfterSnapshot()
        {
            // Arrange
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));
            var alice = new SampleEntity { Name = "Alice" };
            var _md = default(IndexMetadata);
            db.Upsert(alice, ref _md);

            // Act
            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            var snapshot = db.StartSnapshot().Build();
            db.Delete(alice, ref _md);
            var dbResults = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");

            // Assert
            Assert.Single(results);
            Assert.Empty(dbResults);
            results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            Assert.Single(results);
        }

        // ── Snapshot – caching ────────────────────────────────────────────────

        [Fact]
        public void StartSnapshot_WhenNoChanges_ReturnsCachedSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var builder1 = db.StartSnapshot().Build();
            var builder2 = db.StartSnapshot().Build(); // no changes since first

            Assert.Same(builder1, builder2);
        }

        [Fact]
        public void StartSnapshot_AfterChange_ReturnsNewSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("first", ref _md);

            var snap1 = db.StartSnapshot().Build().GetTable<string>("Items").GetSnapshot();
            db.Upsert("second", ref _md);
            var snap2 = db.StartSnapshot().Build().GetTable<string>("Items").GetSnapshot();

            Assert.DoesNotContain(snap1, x => x.Value == "second");
            Assert.Contains(snap2, x => x.Value == "second");
        }

        // ── Snapshot – version tracking ─────────────────────────────────────

        [Fact]
        public void Snapshot_VersionMatchesDatabaseVersionAtCallTime()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var snapshot = db.StartSnapshot().Build();

            Assert.Equal(db.Version, snapshot.Version);
        }

        // ── Snapshot – sub-tables ──────────────────────────────────────────────

        [Fact]
        public void Snapshot_SubTablePresentInFrozenTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("Sub", tableBuilder: _ => { })));
            var _md = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md);

            var snapshot = db.StartSnapshot().Build();
            var table = snapshot.GetTable<SampleEntity>("Items");

            Assert.Single(table.SubTables);
            Assert.Equal("Sub", table.SubTables[0].Name);
        }

        [Fact]
        public void Snapshot_GetTable_DotNotation_ReturnsSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("Sub", tableBuilder: _ => { })));

            var snapshot = db.StartSnapshot().Build();
            var subTable = snapshot.GetTable<SampleEntity>("Items.Sub");

            Assert.NotNull(subTable);
            Assert.Equal("Sub", subTable.Name);
        }

        [Fact]
        public void Snapshot_GetTable_DotNotation_ReturnsNullForMissingSegment()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", _ => { }));

            var snapshot = db.StartSnapshot().Build();

            Assert.Null(snapshot.GetTable<SampleEntity>("Items.NoSuch"));
        }

        [Fact]
        public void Snapshot_GetTable_WrongType_ThrowsInvalidOperationException()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var snapshot = db.StartSnapshot().Build();

            Assert.Throws<InvalidOperationException>(() => snapshot.GetTable<SampleEntity>("Items"));
        }

        [Fact]
        public void Snapshot_Query_DotNotationTableName_QueriesSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("Sub",
                        null,
                        sub => sub.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name))));
            var _md0 = default(IndexMetadata);
            db.Upsert(new SampleEntity { Name = "Alice" }, ref _md0);

            var snapshot = db.StartSnapshot().Build();
            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice", tableName: "Items.Sub");

            Assert.Single(results);
            Assert.Equal("Alice", results.First().Name);
        }

        // ── Snapshot – incremental building ──────────────────────────────────

        [Fact]
        public void StartSnapshot_AfterUpsert_ReturnsNewSnapshotWithChange()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("first", ref _md);

            // Take initial snapshot
            var snap1 = db.StartSnapshot().Build();
            Assert.NotNull(snap1.Find("first"));

            // Add more data
            db.Upsert("second", ref _md);

            // Take new snapshot — should contain both
            var snap2 = db.StartSnapshot().Build();
            Assert.NotNull(snap2.Find("first"));
            Assert.NotNull(snap2.Find("second"));
        }

        [Fact]
        public void StartSnapshot_AfterDelete_ReturnsNewSnapshotWithoutDeleted()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);

            var snap1 = db.StartSnapshot().Build();
            Assert.NotNull(snap1.Find("hello"));

            db.Delete("hello", ref _md);

            var snap2 = db.StartSnapshot().Build();
            Assert.Null(snap2.Find("hello"));
        }

        [Fact]
        public void StartSnapshot_MultipleIncrementalUpdates_PreservesAllChanges()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            // Round 1
            var _md = default(IndexMetadata);
            db.Upsert("alpha", ref _md);
            var snap1 = db.StartSnapshot().Build();
            Assert.NotNull(snap1.Find("alpha"));

            // Round 2
            db.Upsert("beta", ref _md);
            var snap2 = db.StartSnapshot().Build();
            Assert.NotNull(snap2.Find("alpha"));
            Assert.NotNull(snap2.Find("beta"));

            // Round 3 — also delete
            db.Upsert("gamma", ref _md);
            db.Delete("alpha", ref _md);
            var snap3 = db.StartSnapshot().Build();
            Assert.Null(snap3.Find("alpha"));
            Assert.NotNull(snap3.Find("beta"));
            Assert.NotNull(snap3.Find("gamma"));
        }

        [Fact]
        public void StartSnapshot_WhenNoChangesSinceLastSnapshot_CachesAndReturnsSame()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            var _md1 = default(IndexMetadata);
            db.Upsert("hello", ref _md1);

            var builder1 = db.StartSnapshot().Build();
            var builder2 = db.StartSnapshot().Build();

            // No changes between — should be the same snapshot object
            Assert.Same(builder1, builder2);
        }
        [Fact]
        public void StartSnapshot_WithTrackChanges_IncrementalSnapshotCarriesChangedSet()
        {
            var db = new Database();
            db.Deploy(schema => schema
                .WithTable<string>("Items", tb => tb.TrackChanges())
                .TrackChanges());
            Assert.True(db.TrackingChanges);

            var _md = default(IndexMetadata);
            db.Upsert("first", ref _md);
            var snap1 = db.StartSnapshot().Build();
            Assert.Contains(snap1.Changed, c => "first".Equals(c.Value));

            // Second incremental change
            db.Upsert("second", ref _md);
            var snap2 = db.StartSnapshot().Build();
            Assert.Contains(snap2.Changed, c => "second".Equals(c.Value));
        }

        [Fact]
        public void StartSnapshot_WithTrackChanges_IncrementalSnapshotCarriesDeletedSet()
        {
            var db = new Database();
            db.Deploy(schema => schema
                .WithTable<string>("Items", tb => tb.TrackChanges())
                .TrackChanges());
            Assert.True(db.TrackingChanges);

            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md);
            db.StartSnapshot().Build(); // clear change tracking

            db.Delete("hello", ref _md);
            var snap2 = db.StartSnapshot().Build();
            Assert.Contains(snap2.Deleted, d => "hello".Equals(d.Value));
        }

        // ── Pending intent log dedup ──────────────────────────────────────────

        [Fact]
        public void Upsert_ThenDelete_SameItemAfterSnapshot_UpdatesPendingActionToDeleteInPlace()
        {
            var db = new Database();
            db.Deploy(schema => schema
                .WithTable<string>("Items", _ => { })
                .TrackChanges());

            db.StartSnapshot().Build(); // cached snapshot exists so later changes are intent-logged

            var _md = default(IndexMetadata);
            db.Upsert("hello", ref _md); // registers a pending Upsert action
            db.Delete("hello", ref _md); // pending action is updated in place to Delete

            var snapshot = db.StartSnapshot().Build();

            // The intent log held a single Delete action, so the item is only deleted
            Assert.Empty(snapshot.Changed);
            Assert.Contains(snapshot.Deleted, d => "hello".Equals(d.Value));
        }

        [Fact]
        public void Upsert_SameItemTwiceAfterSnapshot_KeepsSinglePendingAction()
        {
            var db = new Database();
            var tagKey = IndexMetadataKey.Get("PendingDedupTag");
            db.Deploy(schema => schema
                .WithTable<string>("Items", tb => tb.OnInserting((IWriteableIndexed<string> i, ref IndexMetadata m, IReadOnlyTable<string> t) =>
                {
                    if (m.TryGetValue<string>(tagKey, out var tag))
                    {
                        i.Set("tag", tag);
                    }
                }))
                .TrackChanges());

            var md1 = new IndexMetadata();
            md1.Set(tagKey, "a");
            db.Upsert("hello", ref md1);
            db.StartSnapshot().Build(); // commit and cache the snapshot

            var md2 = new IndexMetadata();
            md2.Set(tagKey, "b");
            db.Upsert("hello", ref md2); // registers a pending Upsert action

            var md3 = new IndexMetadata();
            md3.Set(tagKey, "c");
            db.Upsert("hello", ref md3); // pending action is updated in place (still one action)

            var snapshot = db.StartSnapshot().Build();

            Assert.Single(snapshot.Changed);
            Assert.Contains(snapshot.Changed, c => "hello".Equals(c.Value));
            Assert.Empty(snapshot.Deleted);
        }

        private static IEnumerable<IIndexed<T>> AsIndexed<T>(IReadOnlyTable<T> table) where T : class
            => (IEnumerable<IIndexed<T>>)table;
    }
}
