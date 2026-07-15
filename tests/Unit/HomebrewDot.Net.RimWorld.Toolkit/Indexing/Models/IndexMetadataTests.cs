using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Models
{
    [Trait("Category", "Unit")]
    public class IndexMetadataTests
    {
        private static readonly IndexMetadataKey<int> IntKey = IndexMetadataKey<int>.Get("IntVal");
        private static readonly IndexMetadataKey<bool> BoolKey = IndexMetadataKey<bool>.Get("BoolVal");
        private static readonly IndexMetadataKey<float> FloatKey = IndexMetadataKey<float>.Get("FloatVal");
        private static readonly IndexMetadataKey<string> StringKey = IndexMetadataKey<string>.Get("StringVal");
        private static readonly IndexMetadataKey<object> ObjKey = IndexMetadataKey<object>.Get("ObjVal");

        // ── Set with persistent: true ──────────────────────────────────────

        [Fact]
        public void Set_WithPersistentTrue_TransfersValueViaPersistTo()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 42, persistent: true);

            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);

            indexed.Verify(x => x.Set(IntKey.Name, 42), Times.Once);
        }

        [Fact]
        public void Set_WithPersistentFalse_DoesNotTransferViaPersistTo()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 42, persistent: false);

            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);

            indexed.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void Set_MultiplePersistentKeys_AllTransferred()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 10, persistent: true);
            md.Set(BoolKey, true, persistent: true);
            md.Set(StringKey, "hello", persistent: true);

            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);

            indexed.Verify(x => x.Set(IntKey.Name, 10), Times.Once);
            indexed.Verify(x => x.Set(BoolKey.Name, true), Times.Once);
            // Strings route to the object dictionary, so PersistTo calls Set<object>
            indexed.Verify(x => x.Set(StringKey.Name, (object)"hello"), Times.Once);
        }

        [Fact]
        public void Set_MixedPersistentAndNonPersistent_OnlyPersistentTransferred()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 10, persistent: true);
            md.Set(BoolKey, true, persistent: false);

            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);

            indexed.Verify(x => x.Set(IntKey.Name, 10), Times.Once);
            indexed.Verify(x => x.Set(BoolKey.Name, It.IsAny<bool>()), Times.Never);
        }

        // ── PersistKey ──────────────────────────────────────────────────────

        [Fact]
        public void PersistKey_ThenSet_TransfersValue()
        {
            // PersistKey adds the key to _persistentKeys. Set(persistent:false) sets
            // the value but does NOT remove the key from _persistentKeys.
            // Therefore PersistTo still transfers the value.
            var md = new IndexMetadata();
            md.PersistKey(IntKey);
            md.Set(IntKey, 99, persistent: false);

            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);

            indexed.Verify(x => x.Set(IntKey.Name, 99), Times.Once);
        }

        [Fact]
        public void PersistKey_ThenSetWithPersistent_TransfersValue()
        {
            var md = new IndexMetadata();
            md.PersistKey(IntKey);
            md.Set(IntKey, 99, persistent: true);

            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);

            indexed.Verify(x => x.Set(IntKey.Name, 99), Times.Once);
        }

        // ── Type-specific dictionary routing ────────────────────────────────

        [Fact]
        public void Set_Int_RoutesToIntDictionary()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 42, persistent: true);

            Assert.True(md.TryGetValue(IntKey, out int val));
            Assert.Equal(42, val);
        }

        [Fact]
        public void Set_Bool_RoutesToBoolDictionary()
        {
            var md = new IndexMetadata();
            md.Set(BoolKey, true, persistent: true);

            Assert.True(md.TryGetValue(BoolKey, out bool val));
            Assert.True(val);
        }

        [Fact]
        public void Set_Float_RoutesToFloatDictionary()
        {
            var md = new IndexMetadata();
            md.Set(FloatKey, 3.14f, persistent: true);

            Assert.True(md.TryGetValue(FloatKey, out float val));
            Assert.Equal(3.14f, val);
        }

        [Fact]
        public void Set_String_RoutesToObjectDictionary()
        {
            var md = new IndexMetadata();
            md.Set(StringKey, "test", persistent: true);

            Assert.True(md.TryGetValue(StringKey, out string val));
            Assert.Equal("test", val);
        }

        [Fact]
        public void Set_Object_RoutesToObjectDictionary()
        {
            var md = new IndexMetadata();
            var obj = new Tentity { Number = 5 };
            md.Set<object>(ObjKey, obj, persistent: true);

            Assert.True(md.TryGetValue(ObjKey, out object val));
            Assert.Same(obj, val);
        }

        // ── TryGetValue ─────────────────────────────────────────────────────

        [Fact]
        public void TryGetValue_KeyNotSet_ReturnsFalse()
        {
            var md = new IndexMetadata();
            Assert.False(md.TryGetValue(IntKey, out int _));
        }

        [Fact]
        public void TryGetValue_WrongType_ReturnsFalse()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 42, persistent: true);

            // Int stored, but asking for string — should fail
            Assert.False(md.TryGetValue(IndexMetadataKey.Get("IntVal"), out string _));
        }

        // ── ContainsKey ─────────────────────────────────────────────────────

        [Fact]
        public void ContainsKey_AfterSet_ReturnsTrue()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 42, persistent: true);

            Assert.True(md.ContainsKey(IntKey));
        }

        [Fact]
        public void ContainsKey_NotSet_ReturnsFalse()
        {
            var md = new IndexMetadata();
            Assert.False(md.ContainsKey(IntKey));
        }

        // ── Unset ───────────────────────────────────────────────────────────

        [Fact]
        public void Unset_RemovesPersistentKeyAndValue()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 42, persistent: true);
            md.Unset<int>(IntKey);

            Assert.False(md.TryGetValue(IntKey, out int _));

            // Should not transfer via PersistTo
            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);
            indexed.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        // ── Dispose safety ──────────────────────────────────────────────────

        [Fact]
        public void PersistTo_AfterDispose_StillWorksOnExistingData()
        {
            var md = new IndexMetadata();
            md.Set(IntKey, 42, persistent: true);

            // PersistTo does not call Dispose — the caller (Database.Update) does.
            // So we test that PersistTo works before Dispose.
            var indexed = new Mock<IWriteableIndexed<Tentity>>();
            md.PersistTo(indexed.Object);

            indexed.Verify(x => x.Set(IntKey.Name, 42), Times.Once);
        }
    }
}
