using System;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests.Hooks.Triggers
{
    public class CooperativeWorkManagerTests
    {
        [Fact]
        public void ShouldIncreaseBudget_WithStableLongRunningWork_ReturnsFalse()
        {
            // Act
            bool result = CooperativeWorkManager.ShouldIncreaseBudget(4, 4, 0, TimeSpan.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldIncreaseBudget_WithGrowingWorkQueue_ReturnsTrue()
        {
            // Act
            bool result = CooperativeWorkManager.ShouldIncreaseBudget(4, 5, 0, TimeSpan.Zero);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldIncreaseBudget_WithRepeatedCancellations_ReturnsTrue()
        {
            // Act
            bool result = CooperativeWorkManager.ShouldIncreaseBudget(4, 4, 5, TimeSpan.Zero);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldIncreaseBudget_WithTickSpikeAboveMax_ReturnsTrue()
        {
            // Act
            bool result = CooperativeWorkManager.ShouldIncreaseBudget(4, 4, 0, TimeSpan.FromMilliseconds(2));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ToString_WithStats_ReturnsQueueBudgetCycleAndLifetimeValues()
        {
            // Arrange
            var stats = new CooperativeWorkManagerStats
            {
                PendingCurrentCycle = 1,
                PendingNextCycle = 2,
                PendingFinalize = 3,
                AcceptedThisBudgetCycle = 4,
                CompletedThisBudgetCycle = 5,
                CanceledThisBudgetCycle = 6,
                Ticks = 7,
                AcceptedWork = 8,
                CompletedWork = 9,
                CanceledWork = 10
            };

            // Act
            string result = stats.ToString();

            // Assert
            Assert.Equal("PendingCurrentCycle=1, PendingNextCycle=2, PendingFinalize=3, AcceptedThisBudgetCycle=4, CompletedThisBudgetCycle=5, CanceledThisBudgetCycle=6, Ticks=7, AcceptedWork=8, CompletedWork=9, CanceledWork=10", result);
        }
    }
}