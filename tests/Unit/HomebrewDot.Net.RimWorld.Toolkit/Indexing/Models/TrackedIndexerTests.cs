using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing.Models
{
    public class TrackedIndexerTests
    {
        private static IIndexed<string> CreateIndexed(string value, Dictionary<string, object> metadata = null)
        {
            var mock = new Mock<IIndexed<string>>();
            mock.Setup(i => i.Value).Returns(value);
            mock.Setup(i => i.Metadata).Returns(metadata ?? new Dictionary<string, object>());
            return mock.Object;
        }

        private static (Mock<IWriteableIndexed<object>> mock, Dictionary<string, object> metadata)
            CreateWriteableIndexed(string value)
        {
            var metadata = new Dictionary<string, object>();
            var mock = new Mock<IWriteableIndexed<object>>();
            var indexedMock = mock.As<IIndexed<string>>();
            indexedMock.Setup(i => i.Value).Returns(value);
            indexedMock.Setup(i => i.Metadata).Returns(metadata);
            mock.Setup(i => i.Set(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((key, val) => metadata[key] = val)
                .Returns(true);
            mock.Setup(i => i.Unset(It.IsAny<string>()))
                .Callback<string>(key => metadata.Remove(key))
                .Returns(true);
            return (mock, metadata);
        }

        // ── Constructor / Initial State ──────────────────────────────────────

        [Fact]
        public void WatchesChanges_WhenNoWatchersRegistered_ReturnsFalse()
        {
            var sut = new TrackedIndexer<string>();
            Assert.False(sut.WatchesChanges);
        }

        // ── IIndexerBuilder.Set ──────────────────────────────────────────────

        [Fact]
        public void Set_WithWatchForChangesFalse_DoesNotEnableWatchesChanges()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            builder.Set("Key", s => s.Length, watchForChanges: false);

            Assert.False(sut.WatchesChanges);
        }

        [Fact]
        public void Set_WithWatchForChangesTrue_EnablesWatchesChanges()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            builder.Set("Key", s => s.Length, watchForChanges: true);

            Assert.True(sut.WatchesChanges);
        }

        [Fact]
        public void Set_WithNullMetadataKey_ThrowsArgumentNullException()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            Assert.Throws<ArgumentNullException>(() => builder.Set(null, s => s, false));
        }

        [Fact]
        public void Set_WithNullValueFunc_ThrowsArgumentNullException()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            Assert.Throws<ArgumentNullException>(() => builder.Set("Key", null, false));
        }

        [Fact]
        public void Set_ReturnsSameBuilderForChaining()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            var result = builder.Set("A", s => s, false);

            Assert.Same(builder, result);
        }

        // ── IIndexerBuilder.Requires ─────────────────────────────────────────

        [Fact]
        public void Requires_EnablesWatchesChanges()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            builder.Requires("Key", s => s.Length);

            Assert.True(sut.WatchesChanges);
        }

        [Fact]
        public void Requires_WithNullMetadataKey_ThrowsArgumentNullException()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            Assert.Throws<ArgumentNullException>(() => builder.Requires(null, s => s));
        }

        [Fact]
        public void Requires_WithNullValueFunc_ThrowsArgumentNullException()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            Assert.Throws<ArgumentNullException>(() => builder.Requires("Key", null));
        }

        [Fact]
        public void Requires_ReturnsSameBuilderForChaining()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            var result = builder.Requires("A", s => s);

            Assert.Same(builder, result);
        }

        // ── IChangeTracker.HasChanged ────────────────────────────────────────

        [Fact]
        public void HasChanged_WhenNoWatchers_ReturnsFalse()
        {
            var sut = new TrackedIndexer<string>();
            var current = "test";
            var previous = CreateIndexed("test");

            var result = sut.HasChanged(current, previous, null);

            Assert.False(result);
        }

        [Fact]
        public void HasChanged_WhenCurrentValueEqualsPrevious_ReturnsFalse()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Tag", s => s, watchForChanges: true);
            var current = "same";
            var previous = CreateIndexed("same", new Dictionary<string, object> { ["Tag"] = "same" });

            var result = sut.HasChanged(current, previous, null);

            Assert.False(result);
        }

        [Fact]
        public void HasChanged_WhenCurrentValueDiffersFromPrevious_ReturnsTrue()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Tag", s => s, watchForChanges: true);
            var current = "new";
            var previous = CreateIndexed("old", new Dictionary<string, object> { ["Tag"] = "old" });

            var result = sut.HasChanged(current, previous, null);

            Assert.True(result);
        }

        [Fact]
        public void HasChanged_WhenMetadataKeyMissingAndCurrentValueNotNull_ReturnsTrue()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Tag", s => s, watchForChanges: true);
            var current = "hello";
            var previous = CreateIndexed("other"); // No "Tag" key in metadata

            var result = sut.HasChanged(current, previous, null);

            Assert.True(result);
        }

        [Fact]
        public void HasChanged_WhenMetadataKeyMissingAndCurrentValueIsNull_ReturnsFalse()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Tag", s => s, watchForChanges: true);
            string current = null;
            var previous = CreateIndexed("other"); // No "Tag" key in metadata; current value is null

            var result = sut.HasChanged(current, previous, null);

            Assert.False(result);
        }

        // ── Index ────────────────────────────────────────────────────────────

        [Fact]
        public void Index_WithTypedIndexed_SetsMetadataFromGetters()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Length", s => s.Length, watchForChanges: false);

            var (writeableMock, metadata) = CreateWriteableIndexed("Hello");
            var db = new Mock<IDatabase>().Object;
            var insertMeta = new Dictionary<string, object>();

            sut.Index(db, insertMeta, writeableMock.Object);

            Assert.True(metadata.ContainsKey("Length"));
            Assert.Equal(5, metadata["Length"]);
        }

        [Fact]
        public void Index_WithTypedIndexed_SetsMetadataFromWatchers()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Value", s => s, watchForChanges: true);

            var (writeableMock, metadata) = CreateWriteableIndexed("Hello");
            var db = new Mock<IDatabase>().Object;
            var insertMeta = new Dictionary<string, object>();

            sut.Index(db, insertMeta, writeableMock.Object);

            Assert.True(metadata.ContainsKey("Value"));
            Assert.Equal("Hello", metadata["Value"]);
        }

        [Fact]
        public void Index_WhenGetterReturnsNull_CallsUnset()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Tag", s => null, watchForChanges: false);

            var (writeableMock, _) = CreateWriteableIndexed("Hello");
            var db = new Mock<IDatabase>().Object;
            var insertMeta = new Dictionary<string, object>();

            sut.Index(db, insertMeta, writeableMock.Object);

            writeableMock.Verify(i => i.Unset("Tag"), Times.Once);
        }

        [Fact]
        public void Index_WhenWatcherReturnsNull_CallsUnset()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            // Set with watchForChanges adds to both _watchers and _getters
            builder.Set("Tag", s => null, watchForChanges: true);

            var (writeableMock, _) = CreateWriteableIndexed("Hello");
            var db = new Mock<IDatabase>().Object;
            var insertMeta = new Dictionary<string, object>();

            sut.Index(db, insertMeta, writeableMock.Object);

            // watchForChanges adds to both _watchers and _getters, so Unset called twice
            writeableMock.Verify(i => i.Unset("Tag"), Times.Exactly(2));
        }

        [Fact]
        public void Index_WhenIndexedIsNotTypedIndexed_DoesNothing()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;
            builder.Set("Tag", s => s, watchForChanges: true);

            var mockIndexed = new Mock<IWriteableIndexed<object>>();
            // Does NOT implement IIndexed<string>
            mockIndexed.Setup(i => i.Set(It.IsAny<string>(), It.IsAny<object>())).Returns(true);

            var db = new Mock<IDatabase>().Object;
            var insertMeta = new Dictionary<string, object>();

            sut.Index(db, insertMeta, mockIndexed.Object);

            // Set should never be called since the indexed is not IIndexed<string>
            mockIndexed.Verify(i => i.Set(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void Index_WithNullDatabase_ThrowsArgumentNullException()
        {
            var sut = new TrackedIndexer<string>();
            var indexed = new Mock<IWriteableIndexed<object>>().Object;

            Assert.Throws<ArgumentNullException>(() =>
                sut.Index(null, new Dictionary<string, object>(), indexed));
        }

        [Fact]
        public void Index_WithNullMetadata_ThrowsArgumentNullException()
        {
            var sut = new TrackedIndexer<string>();
            var indexed = new Mock<IWriteableIndexed<object>>().Object;

            Assert.Throws<ArgumentNullException>(() =>
                sut.Index(new Mock<IDatabase>().Object, null, indexed));
        }

        [Fact]
        public void Index_WithNullIndexed_ThrowsArgumentNullException()
        {
            var sut = new TrackedIndexer<string>();

            Assert.Throws<ArgumentNullException>(() =>
                sut.Index(new Mock<IDatabase>().Object, new Dictionary<string, object>(), null));
        }

        // ── Fluent chaining ──────────────────────────────────────────────────

        [Fact]
        public void Set_CanBeChainedWithRequires()
        {
            var sut = new TrackedIndexer<string>();
            var builder = (IIndexerBuilder<string>)sut;

            builder.Set("A", s => s, false)
                   .Requires("B", s => s.Length);

            Assert.True(sut.WatchesChanges); // Requires enables watching

            // Verify both getters work via Index
            var (writeableMock, metadata) = CreateWriteableIndexed("Hi");
            sut.Index(new Mock<IDatabase>().Object, new Dictionary<string, object>(), writeableMock.Object);

            Assert.True(metadata.ContainsKey("A"));
            Assert.Equal("Hi", metadata["A"]);
            Assert.True(metadata.ContainsKey("B"));
            Assert.Equal(2, metadata["B"]);
        }
    }
}
