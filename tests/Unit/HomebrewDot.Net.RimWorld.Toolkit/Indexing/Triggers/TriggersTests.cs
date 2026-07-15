using System;
using HomebrewDot.Net.Rimworld.Indexing;
using HomebrewDot.Net.Rimworld.Indexing.Triggers;
using Moq;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Indexing.Triggers
{
    public class OnSnapshotTakenTriggerTests
    {
        [Fact]
        public void Constructor_WithNullSnapshot_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OnSnapshotTakenTrigger(null, true));
        }

        [Fact]
        public void Constructor_WithValidSnapshot_SetsSnapshotProperty()
        {
            var snapshot = new Mock<IReadOnlyDatabase>().Object;

            var sut = new OnSnapshotTakenTrigger(snapshot, true);

            Assert.Same(snapshot, sut.Snapshot);
        }
    }
}
