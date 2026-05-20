using System;
using System.Collections.Generic;
using System.Linq;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Components;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing.Components
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
        public void WithTable_NullTableBuilder_ThrowsArgumentNullException()
        {
            var db = new Database();
            Assert.Throws<ArgumentNullException>(() =>
                db.Deploy(schema => schema.WithTable<string>("Items", null)));
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

            Assert.Throws<ArgumentNullException>(() => db.Upsert<string>(null, null));
        }

        [Fact]
        public void Upsert_WhenNoTableRegistered_ReturnsFalse()
        {
            var db = new Database();
            // No tables deployed
            var result = db.Upsert("hello", null);
            Assert.False(result);
        }

        [Fact]
        public void Upsert_WhenTableRegistered_ReturnsTrue()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var result = db.Upsert("hello", null);

            Assert.True(result);
        }

        [Fact]
        public void Upsert_WhenTableRegistered_SetsHasChangesTrue()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            db.Upsert("hello", null);

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
            db.Upsert("hello", null);

            var result = db.Find("hello");

            Assert.NotNull(result);
            Assert.Equal("hello", result.Value);
        }

        [Fact]
        public void Find_AfterUpsertWithMetadata_ReturnsIndexedItemWithMetadata()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", new Dictionary<string, object> { ["tag"] = "test" });

            var result = db.Find("hello");

            Assert.NotNull(result);
            Assert.Equal("test", result.Metadata["tag"]);
        }

        // ── Delete ────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_WithNullItem_ThrowsArgumentNullException()
        {
            var db = new Database();
            Assert.Throws<ArgumentNullException>(() =>
                db.Delete<string>(null, null));
        }

        [Fact]
        public void Delete_WithExternalIndexed_ReturnsFalse()
        {
            var db = new Database();
            var result = db.Delete("hello", null);

            Assert.False(result);
        }

        [Fact]
        public void Delete_WithUpsertedItem_ReturnsTrueAndRemovesFromDatabase()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);

            var deleted = db.Delete("hello", null);

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
            db.Upsert(a, null);
            db.Upsert(b, null);

            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");

            Assert.Single(results);
            Assert.Equal("Alice", results.First().Value.Name);
        }

        [Fact]
        public void Query_WithIndex_ReturnsEmptyWhenNoMatch()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name, null, "idx")));

            db.Upsert(new SampleEntity { Name = "Alice" }, null);

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
                schema.OnInserting((database, item) => capturedItem = item.Value);
            });

            db.Upsert("hello", null);

            Assert.Equal("hello", capturedItem);
        }

        [Fact]
        public void OnInserted_CallbackInvokedAfterUpsert()
        {
            var db = new Database();
            IIndexed<string> capturedIndexed = null;
            db.Deploy(schema =>
                schema.WithTable<string>("Items", tb =>
                    tb.OnInserted((database, table, indexed) => capturedIndexed = indexed)));

            db.Upsert("hello", null);

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

            db.Upsert(new SampleEntity { Name = "Bob" }, null);    // filtered out (len=3)
            db.Upsert(new SampleEntity { Name = "Alice" }, null);  // accepted (len=5)

            var longTable = db.GetTable<SampleEntity>("LongNames");
            Assert.NotNull(longTable);
            Assert.Single(longTable);
        }

        // Helper type used across several tests
        public class SampleEntity
        {
            public string Name { get; set; }
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

        // ── AsReadOnly ────────────────────────────────────────────────────────

        [Fact]
        public void AsReadOnly_ResetsHasChangesToFalse()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);
            Assert.True(db.HasChanges);

            db.AsReadOnly();

            Assert.False(db.HasChanges);
        }

        [Fact]
        public void AsReadOnly_SnapshotContainsUpsertedItem()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);

            var snapshot = db.AsReadOnly();
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
            db.Upsert("hello", null);
            db.Upsert("hello", null);

            var table = db.GetTable<string>("Items");
            Assert.Single(table);
        }

        // ── Delete additional ────────────────────────────────────────────────

        [Fact]
        public void Delete_SetsHasChangesTrue()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);
            db.AsReadOnly(); // reset HasChanges

            db.Delete("hello", null);

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
            db.Upsert("alpha", null);
            db.Upsert("beta", null);

            var table = db.GetTable<string>("Items");
            var values = table.Select(i => i.Value).OrderBy(v => v).ToList();

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
                schema.OnInserted((database, indexed) => captured = indexed);
            });

            db.Upsert("hello", null);

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
                schema.OnDeleting((database, indexed, meta) => capturedBefore = indexed);
            });
            db.Upsert("hello", null);

            db.Delete("hello", null);

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
                schema.OnDeleted((database, indexed, meta) => capturedAfter = indexed);
            });
            db.Upsert("hello", null);

            db.Delete("hello", null);

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
                    tb.OnInserting((database, table, item) => capturedItem = item.Value)));

            db.Upsert("hello", null);

            Assert.Equal("hello", capturedItem);
        }

        [Fact]
        public void OnDeleting_TableLevel_CalledBeforeDelete()
        {
            IIndexed<string> capturedBefore = null;
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<string>("Items", tb =>
                    tb.OnDeleting((database, table, indexed, meta) => capturedBefore = indexed)));
            db.Upsert("hello", null);

            db.Delete("hello", null);

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
                    tb.OnDeleted((database, table, indexed, meta) => capturedAfter = indexed)));
            db.Upsert("hello", null);

            db.Delete("hello", null);

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
            db.Upsert(alice, null);
            db.Upsert(bob, null);

            var results = db.Query<SampleEntity, bool>("HasLongName", true, indexName: "idx");

            Assert.Single(results);
            Assert.Equal("Alice", results.First().Value.Name);
        }

        // ── WithIndex – cleanup on delete ─────────────────────────────────────

        [Fact]
        public void WithIndex_OnDelete_ItemRemovedFromIndex()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));

            var alice = new SampleEntity { Name = "Alice" };
            db.Upsert(alice, null);

            db.Delete(alice, null);

            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            Assert.Empty(results);
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

            db.Upsert(new SampleEntity { Name = "Alice" }, null);

            // Upsert routes to the last-registered table (Beta); querying Alpha should return nothing.
            var results = db.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice", tableName: "Alpha");

            Assert.Empty(results);
        }

        // ── WithSubTable ──────────────────────────────────────────────────────

        [Fact]
        public void WithSubTable_UpsertPopulatesSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("SubItems", _ => { }, null)));

            db.Upsert(new SampleEntity { Name = "Alice" }, null);

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
                    tb.WithSubTable<SampleEntity>("Labels", _ => { }, null)));

            var subTable = db.GetTable<SampleEntity>("Things.Labels");
            Assert.NotNull(subTable);
        }

        // ── AsReadOnly – snapshot isolation ──────────────────────────────────

        [Fact]
        public void AsReadOnly_Snapshot_DoesNotReflectItemsAddedAfterSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("before", null);

            var snapshot = db.AsReadOnly();

            db.Upsert("after", null);

            Assert.NotNull(snapshot.Find("before"));
            Assert.Null(snapshot.Find("after"));
        }

        [Fact]
        public void AsReadOnly_Snapshot_StillContainsItemDeletedAfterSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);

            var snapshot = db.AsReadOnly();

            db.Delete("hello", null);

            // live DB no longer has it
            Assert.Null(db.Find("hello"));
            // snapshot is frozen — still has it
            Assert.NotNull(snapshot.Find("hello"));
        }

        [Fact]
        public void AsReadOnly_Snapshot_FindReturnsNullForMissingItem()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var snapshot = db.AsReadOnly();

            Assert.Null(snapshot.Find("missing"));
        }

        // ── AsReadOnly – snapshot table access ────────────────────────────────

        [Fact]
        public void AsReadOnly_Snapshot_GetTableReturnsFrozenTable()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);

            var snapshot = db.AsReadOnly();
            var table = snapshot.GetTable<string>("Items");

            Assert.NotNull(table);
            Assert.Equal("Items", table.Name);
        }

        [Fact]
        public void AsReadOnly_Snapshot_GetTableReturnsNullForUnknownName()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var snapshot = db.AsReadOnly();

            Assert.Null(snapshot.GetTable<string>("DoesNotExist"));
        }

        [Fact]
        public void AsReadOnly_Snapshot_GetTablesReturnsAllTables()
        {
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Names", _ => { });
                schema.WithTable<SampleEntity>("Entities", _ => { });
            });

            var snapshot = db.AsReadOnly();

            Assert.Equal(2, snapshot.GetTables().Count());
        }

        [Fact]
        public void AsReadOnly_Snapshot_GetTablesTypedReturnsOnlyMatchingType()
        {
            var db = new Database();
            db.Deploy(schema =>
            {
                schema.WithTable<string>("Names", _ => { });
                schema.WithTable<SampleEntity>("Entities", _ => { });
            });

            var snapshot = db.AsReadOnly();

            var stringTables = snapshot.GetTables<string>().ToList();
            Assert.Single(stringTables);
            Assert.Equal("Names", stringTables[0].Name);
        }

        [Fact]
        public void AsReadOnly_Snapshot_TableEnumeratesCorrectItems()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("alpha", null);
            db.Upsert("beta", null);

            var snapshot = db.AsReadOnly();
            var table = snapshot.GetTable<string>("Items");
            var values = table.Select(i => i.Value).OrderBy(v => v).ToList();

            Assert.Equal(new[] { "alpha", "beta" }, values);
        }

        [Fact]
        public void AsReadOnly_Snapshot_TablePreservesIsFiltered()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }, s => s.Length > 3));

            var snapshot = db.AsReadOnly();
            var table = snapshot.GetTable<string>("Items");

            Assert.True(table.IsFiltered);
        }

        // ── AsReadOnly – index data in snapshot ───────────────────────────────

        [Fact]
        public void AsReadOnly_Snapshot_QueryViaIndexReturnsMatchingItems()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));
            db.Upsert(new SampleEntity { Name = "Alice" }, null);
            db.Upsert(new SampleEntity { Name = "Bob" }, null);

            var snapshot = db.AsReadOnly();

            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            Assert.Single(results);
            Assert.Equal("Alice", results.First().Value.Name);
        }

        [Fact]
        public void AsReadOnly_Snapshot_QueryReturnsEmptyForMissingKey()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));
            db.Upsert(new SampleEntity { Name = "Alice" }, null);

            var snapshot = db.AsReadOnly();

            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Charlie");
            Assert.Empty(results);
        }

        [Fact]
        public void AsReadOnly_Snapshot_IndexDoesNotReflectDeletesAfterSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", tb =>
                tb.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name)));
            var alice = new SampleEntity { Name = "Alice" };
            db.Upsert(alice, null);

            var snapshot = db.AsReadOnly();

            db.Delete(alice, null);

            // Index in snapshot is still populated
            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice");
            Assert.Single(results);
        }

        // ── AsReadOnly – snapshot caching ─────────────────────────────────────

        [Fact]
        public void AsReadOnly_CalledTwiceWithNoChanges_ReturnsSameSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);

            var first = db.AsReadOnly();
            var second = db.AsReadOnly(); // no changes since first

            Assert.Same(first, second);
        }

        [Fact]
        public void AsReadOnly_AfterChange_ReturnsNewSnapshot()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("first", null);

            var snap1 = db.AsReadOnly();
            db.Upsert("second", null);
            var snap2 = db.AsReadOnly();

            Assert.NotSame(snap1, snap2);
            Assert.Null(snap1.Find("second"));
            Assert.NotNull(snap2.Find("second"));
        }

        // ── AsReadOnly – version tracking ─────────────────────────────────────

        [Fact]
        public void AsReadOnly_Snapshot_VersionMatchesDatabaseVersionAtCallTime()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));
            db.Upsert("hello", null);

            var snapshot = db.AsReadOnly();

            Assert.Equal(db.Version, snapshot.Version);
        }

        // ── AsReadOnly – sub-tables in snapshot ───────────────────────────────

        [Fact]
        public void AsReadOnly_Snapshot_SubTablePresentInFrozenTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("Sub", _ => { }, null)));
            db.Upsert(new SampleEntity { Name = "Alice" }, null);

            var snapshot = db.AsReadOnly();
            var table = snapshot.GetTable<SampleEntity>("Items");

            Assert.Single(table.SubTables);
            Assert.Equal("Sub", table.SubTables[0].Name);
        }

        [Fact]
        public void AsReadOnly_Snapshot_GetTable_DotNotation_ReturnsSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("Sub", _ => { }, null)));

            var snapshot = db.AsReadOnly();
            var subTable = snapshot.GetTable<SampleEntity>("Items.Sub");

            Assert.NotNull(subTable);
            Assert.Equal("Sub", subTable.Name);
        }

        [Fact]
        public void AsReadOnly_Snapshot_GetTable_DotNotation_ReturnsNullForMissingSegment()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<SampleEntity>("Items", _ => { }));

            var snapshot = db.AsReadOnly();

            Assert.Null(snapshot.GetTable<SampleEntity>("Items.NoSuch"));
        }

        [Fact]
        public void AsReadOnly_Snapshot_GetTable_WrongType_ThrowsInvalidOperationException()
        {
            var db = new Database();
            db.Deploy(schema => schema.WithTable<string>("Items", _ => { }));

            var snapshot = db.AsReadOnly();

            Assert.Throws<InvalidOperationException>(() => snapshot.GetTable<SampleEntity>("Items"));
        }

        [Fact]
        public void AsReadOnly_Snapshot_Query_DotNotationTableName_QueriesSubTable()
        {
            var db = new Database();
            db.Deploy(schema =>
                schema.WithTable<SampleEntity>("Items", tb =>
                    tb.WithSubTable<SampleEntity>("Sub",
                        sub => sub.WithIndex<string>(nameof(SampleEntity.Name), e => e.Value.Name),
                        null)));
            db.Upsert(new SampleEntity { Name = "Alice" }, null);

            var snapshot = db.AsReadOnly();
            var results = snapshot.Query<SampleEntity, string>(nameof(SampleEntity.Name), "Alice", tableName: "Items.Sub");

            Assert.Single(results);
            Assert.Equal("Alice", results.First().Value.Name);
        }
    }
}
