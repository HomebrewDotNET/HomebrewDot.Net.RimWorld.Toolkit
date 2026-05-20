using System;
using HomebrewDot.Net.RimWorld.Indexing;
using HomebrewDot.Net.RimWorld.Indexing.Triggers;
using Moq;
using Xunit;

namespace HomebrewDot.Net.RimWorld.Tests.Indexing.Triggers
{
    public class OnSnapshotTakenTriggerTests
    {
        [Fact]
        public void Constructor_WithNullSnapshot_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OnSnapshotTakenTrigger(null));
        }

        [Fact]
        public void Constructor_WithValidSnapshot_SetsSnapshotProperty()
        {
            var snapshot = new Mock<IReadOnlyDatabase>().Object;

            var sut = new OnSnapshotTakenTrigger(snapshot);

            Assert.Same(snapshot, sut.Snapshot);
        }
    }

    public class PreparingSnapshotTriggerTests
    {
        [Fact]
        public void Constructor_WithNullSnapshotManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PreparingSnapshotTrigger(null));
        }

        [Fact]
        public void Constructor_WithValidManager_SetsSnapshotManagerProperty()
        {
            var manager = new Mock<ISnapshotManager>().Object;

            var sut = new PreparingSnapshotTrigger(manager);

            Assert.Same(manager, sut.SnapshotManager);
        }
    }
}
