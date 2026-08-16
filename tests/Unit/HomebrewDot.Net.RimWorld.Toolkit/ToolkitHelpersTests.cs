using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld.Generic;
using Verse;
using Xunit;

namespace HomebrewDot.Net.Rimworld.Tests
{
    /// <summary>
    /// Unit tests for the pure helper members of <see cref="Toolkit"/>: <see cref="Toolkit.Helpers.GetTickerType"/>,
    /// <see cref="Toolkit.Pool{T}"/> and <see cref="Toolkit.Cache{TKey,TValue}"/>. No RimWorld state is required.
    /// </summary>
    [Trait("Category", "Unit")]
    public class ToolkitHelpersTests
    {
        /// <summary>
        /// Test poolable that counts how often <see cref="IPoolable.Reset"/> was called.
        /// </summary>
        private sealed class TestPoolable : IPoolable
        {
            /// <summary>
            /// Number of times <see cref="Reset"/> has been called.
            /// </summary>
            public int ResetCount { get; private set; }

            /// <inheritdoc/>
            public void Reset()
            {
                ResetCount++;
            }
        }

        #region GetTickerType

        [Fact]
        public void GetTickerType_AtLongInterval_ReturnsLong()
        {
            // Act
            var result = Toolkit.Helpers.GetTickerType(2000);

            // Assert
            Assert.Equal(TickerType.Long, result);
        }

        [Fact]
        public void GetTickerType_AtRareInterval_ReturnsRare()
        {
            // Act
            var result = Toolkit.Helpers.GetTickerType(250);

            // Assert
            Assert.Equal(TickerType.Rare, result);
        }

        [Fact]
        public void GetTickerType_AtNeitherInterval_ReturnsNormal()
        {
            // Act
            var result = Toolkit.Helpers.GetTickerType(1);

            // Assert
            Assert.Equal(TickerType.Normal, result);
        }

        [Fact]
        public void GetTickerType_AtCommonMultiple_ReturnsLong()
        {
            // Arrange - 4000 is a multiple of both the long (2000) and the rare (250) interval.
            // Long is checked first, so it must win.
            const long tick = 4000;

            // Act
            var result = Toolkit.Helpers.GetTickerType(tick);

            // Assert
            Assert.Equal(TickerType.Long, result);
        }

        #endregion

        #region Pool

        [Fact]
        public void Pool_Rent_AfterReturningTwoInstances_RentsMostRecentlyReturned()
        {
            // Arrange - return two instances so the pool holds both (LIFO stack)
            var first = Toolkit.Pool<TestPoolable>.Rent();
            var second = Toolkit.Pool<TestPoolable>.Rent();
            Toolkit.Pool<TestPoolable>.Return(first);
            Toolkit.Pool<TestPoolable>.Return(second);

            // Act
            var rented = Toolkit.Pool<TestPoolable>.Rent();

            // Assert - the most recently returned instance is rented first
            Assert.Same(second, rented);
        }

        [Fact]
        public void Pool_Rent_WhenEmpty_CreatesNewInstance()
        {
            // Arrange - drain the shared static pool so it is guaranteed empty (capacity is 1024)
            var seen = new List<TestPoolable>();
            while (seen.Count < 1024)
            {
                var item = Toolkit.Pool<TestPoolable>.Rent();
                seen.Add(item);
            }

            // Act
            var fresh = Toolkit.Pool<TestPoolable>.Rent();

            // Assert - a brand-new instance is created when the pool is empty
            Assert.NotNull(fresh);
            Assert.DoesNotContain(fresh, seen);
        }

        [Fact]
        public void Pool_Return_WithNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Toolkit.Pool<TestPoolable>.Return(null));
        }

        #endregion

        #region Cache

        [Fact]
        public void Cache_GetOrSet_FirstCall_ReturnsFactoryValue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();

            // Act
            var result = Toolkit.Cache<string, string>.GetOrSet(key, () => "cached-value");

            // Assert
            Assert.Equal("cached-value", result);
        }

        [Fact]
        public void Cache_GetOrSet_SecondCall_ReusesCachedValue()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            int factoryCalls = 0;

            // Act
            var first = Toolkit.Cache<string, string>.GetOrSet(key, () => { factoryCalls++; return "value"; });
            var second = Toolkit.Cache<string, string>.GetOrSet(key, () => { factoryCalls++; return "value"; });

            // Assert - the factory only ran once and the same value is returned
            Assert.Equal(1, factoryCalls);
            Assert.Equal(first, second);
        }

        [Fact]
        public void Cache_Invalidate_ExistingKey_ReturnsTrueThenRefreshes()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var first = Toolkit.Cache<string, string>.GetOrSet(key, () => "old-value");

            // Act
            var removed = Toolkit.Cache<string, string>.Invalidate(key);
            var refreshed = Toolkit.Cache<string, string>.GetOrSet(key, () => "new-value");

            // Assert - the entry was removed and a fresh value is produced
            Assert.True(removed);
            Assert.Equal("new-value", refreshed);
            Assert.NotEqual(first, refreshed);
        }

        #endregion
    }
}
