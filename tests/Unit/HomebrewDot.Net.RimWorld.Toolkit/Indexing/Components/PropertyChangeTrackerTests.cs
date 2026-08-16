using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using HomebrewDot.Net.Rimworld.Testing.Models;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing.Components
{
    [Trait("Category", "Unit")]
    public class PropertyChangeTrackerTests
    {
        private static readonly IndexMetadataKey<int> NumberKey = IndexMetadataKey<int>.Get("Number");

        [Fact]
        public void Constructor_WithNullGetProperty_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PropertyChangeTracker<Tentity, int>(null, NumberKey));
        }

        [Fact]
        public void Constructor_WithNullMetadataKey_Throws()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PropertyChangeTracker<Tentity, int>(t => t.Number, null));
        }

        [Fact]
        public void HasChanged_WhenNoPriorValue_ReturnsTrue()
        {
            // Arrange
            var sut = new PropertyChangeTracker<Tentity, int>(t => t.Number, NumberKey);
            var entity = new Tentity { Number = 5 };
            var metadata = new IndexMetadata();

            // Act
            var changed = sut.HasChanged(entity, new IndexedStub<Tentity>(entity, new Dictionary<string, object>()), ref metadata);

            // Assert
            Assert.True(changed);
        }

        [Fact]
        public void HasChanged_WhenValueUnchanged_ReturnsFalse()
        {
            // Arrange
            var sut = new PropertyChangeTracker<Tentity, int>(t => t.Number, NumberKey);
            var entity = new Tentity { Number = 5 };
            var metadata = new IndexMetadata();
            var indexed = new IndexedStub<Tentity>(entity, new Dictionary<string, object>
            {
                [NumberKey.Name] = 5
            });

            // Act
            var changed = sut.HasChanged(entity, indexed, ref metadata);

            // Assert
            Assert.False(changed);
        }

        [Fact]
        public void HasChanged_WhenValueChanged_ReturnsTrue()
        {
            // Arrange
            var sut = new PropertyChangeTracker<Tentity, int>(t => t.Number, NumberKey);
            var entity = new Tentity { Number = 10 };
            var metadata = new IndexMetadata();
            var indexed = new IndexedStub<Tentity>(entity, new Dictionary<string, object>
            {
                [NumberKey.Name] = 5
            });

            // Act
            var changed = sut.HasChanged(entity, indexed, ref metadata);

            // Assert
            Assert.True(changed);
        }

        [Fact]
        public void HasChanged_WhenCurrentValueNull_Throws()
        {
            // Arrange
            var sut = new PropertyChangeTracker<Tentity, int>(t => t.Number, NumberKey);
            var metadata = new IndexMetadata();
            var indexed = new IndexedStub<Tentity>(new Tentity(), new Dictionary<string, object>
            {
                [NumberKey.Name] = 5
            });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => sut.HasChanged(null, indexed, ref metadata));
        }

        [Fact]
        public void HasChanged_WithCustomComparer_UsesComparer()
        {
            // Arrange
            var comparer = new ThresholdComparer(threshold: 5);
            var sut = new PropertyChangeTracker<Tentity, int>(t => t.Number, NumberKey, comparer);
            var entity = new Tentity { Number = 6 };
            var metadata = new IndexMetadata();
            var indexed = new IndexedStub<Tentity>(entity, new Dictionary<string, object>
            {
                [NumberKey.Name] = 2
            });

            // Act
            var changed = sut.HasChanged(entity, indexed, ref metadata);

            // Assert - within threshold of 5, so not changed
            Assert.False(changed);
        }

        private sealed class IndexedStub<T> : IIndexed<T> where T : class
        {
            public IndexedStub(T value, IReadOnlyDictionary<string, object> metadata)
            {
                Value = value;
                Metadata = metadata;
            }

            public T Value { get; }
            public IReadOnlyDictionary<string, object> Metadata { get; }
            public bool HasSnapshot => false;
            public bool IsSnapshot => false;
            public IIndexed<T> Snapshot => null;

            public bool HasPendingChanges => throw new NotImplementedException();

            public bool IsRemoved => throw new NotImplementedException();

            public bool IsInsert => throw new NotImplementedException();

            public TValue GetValue<TValue>(string propertyName) => default;
        }

        private sealed class ThresholdComparer : IComparer<int>
        {
            private readonly int _threshold;
            public ThresholdComparer(int threshold) { _threshold = threshold; }
            public int Compare(int x, int y) => Math.Abs(x - y) < _threshold ? 0 : (x.CompareTo(y));
        }
    }
}
