using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using HomebrewDot.Net.Rimworld.Hooks;

namespace HomebrewDot.Net.Rimworld.Tests.Hooks.Components
{
    public class HookManagerTests
    {
        private readonly HookManager _sut;
        private readonly object _defaultOwner;

        public HookManagerTests()
        {
            _sut = new HookManager();
            _defaultOwner = new object();
        }

        #region RegisterHook

        [Fact]
        public void RegisterHook_WithNullHook_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.RegisterHook<string>(null));
        }

        [Fact]
        public void RegisterHook_WithValidHook_DoesNotThrow()
        {
            // Arrange
            var hook = new SimpleHook<string>(_defaultOwner, _ => true);

            // Act & Assert (no exception)
            _sut.RegisterHook(hook);
        }

        [Fact]
        public void RegisterHook_WithValidHook_CanBeRetrievedByOwner()
        {
            // Arrange
            var hook = new SimpleHook<string>(_defaultOwner, _ => true);

            // Act
            _sut.RegisterHook(hook);
            IHook<string>[] result = _sut.GetOwnerBy<string>(_defaultOwner);

            // Assert
            Assert.Single(result);
            Assert.Same(hook, result[0]);
        }

        [Fact]
        public void RegisterHook_RegisteringSameHookTwice_DoesNotDuplicate()
        {
            // Arrange
            var hook = new SimpleHook<string>(_defaultOwner, _ => true);

            // Act
            _sut.RegisterHook(hook);
            _sut.RegisterHook(hook);
            IHook<string>[] result = _sut.GetOwnerBy<string>(_defaultOwner);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void RegisterHook_MultipleOwners_TracksEachSeparately()
        {
            // Arrange
            var owner1 = new object();
            var owner2 = new object();
            var hook1 = new SimpleHook<string>(owner1, _ => true);
            var hook2 = new SimpleHook<string>(owner2, _ => true);

            // Act
            _sut.RegisterHook(hook1);
            _sut.RegisterHook(hook2);

            // Assert
            Assert.Single(_sut.GetOwnerBy<string>(owner1));
            Assert.Single(_sut.GetOwnerBy<string>(owner2));
        }

        #endregion

        #region GetOwnerBy

        [Fact]
        public void GetOwnerBy_WithNullOwner_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.GetOwnerBy<string>(null));
        }

        [Fact]
        public void GetOwnerBy_WithNoHooksRegistered_ReturnsEmptyArray()
        {
            // Act
            IHook<string>[] result = _sut.GetOwnerBy<string>(_defaultOwner);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetOwnerBy_WithHooksOfDifferentEventType_ReturnsOnlyMatchingType()
        {
            // Arrange
            var stringHook = new SimpleHook<string>(_defaultOwner, _ => true);
            var exceptionHook = new SimpleHook<Exception>(_defaultOwner, _ => true);
            _sut.RegisterHook(stringHook);
            _sut.RegisterHook(exceptionHook);

            // Act
            IHook<string>[] result = _sut.GetOwnerBy<string>(_defaultOwner);

            // Assert
            Assert.Single(result);
            Assert.Same(stringHook, result[0]);
        }

        #endregion

        #region UnregisterHook

        [Fact]
        public void UnregisterHook_WithNullHook_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.UnregisterHook<string>(null));
        }

        [Fact]
        public void UnregisterHook_WithRegisteredHook_RemovesItFromOwner()
        {
            // Arrange
            var hook = new SimpleHook<string>(_defaultOwner, _ => true);
            _sut.RegisterHook(hook);

            // Act
            _sut.UnregisterHook(hook);
            IHook<string>[] result = _sut.GetOwnerBy<string>(_defaultOwner);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void UnregisterHook_WithRegisteredHook_StopsItFromBeingTriggered()
        {
            // Arrange
            bool called = false;
            var hook = new SimpleHook<string>(_defaultOwner, arg => { called = true; return true; });
            _sut.RegisterHook(hook);
            _sut.UnregisterHook(hook);

            // Act
            _sut.Trigger("test");

            // Assert
            Assert.False(called);
        }

        [Fact]
        public void UnregisterHook_WithUnregisteredHook_DoesNotThrow()
        {
            // Arrange
            var hook = new SimpleHook<string>(_defaultOwner, _ => true);

            // Act & Assert (no exception)
            _sut.UnregisterHook(hook);
        }

        #endregion

        #region UnregisterAllBy

        [Fact]
        public void UnregisterAllBy_WithNullOwner_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.UnregisterAllBy<string>(null));
        }

        [Fact]
        public void UnregisterAllBy_WithNoHooksForOwner_ReturnsEmptyArray()
        {
            // Act
            IHook<string>[] result = _sut.UnregisterAllBy<string>(_defaultOwner);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void UnregisterAllBy_WithRegisteredHooks_ReturnsUnregisteredHooks()
        {
            // Arrange
            var hook1 = new SimpleHook<string>(_defaultOwner, _ => true);
            var hook2 = new SimpleHook<string>(_defaultOwner, _ => true);
            _sut.RegisterHook(hook1);
            _sut.RegisterHook(hook2);

            // Act
            IHook<string>[] result = _sut.UnregisterAllBy<string>(_defaultOwner);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(hook1, result);
            Assert.Contains(hook2, result);
        }

        [Fact]
        public void UnregisterAllBy_WithRegisteredHooks_RemovesThemFromManager()
        {
            // Arrange
            var hook1 = new SimpleHook<string>(_defaultOwner, _ => true);
            var hook2 = new SimpleHook<string>(_defaultOwner, _ => true);
            _sut.RegisterHook(hook1);
            _sut.RegisterHook(hook2);

            // Act
            _sut.UnregisterAllBy<string>(_defaultOwner);
            IHook<string>[] remaining = _sut.GetOwnerBy<string>(_defaultOwner);

            // Assert
            Assert.Empty(remaining);
        }

        [Fact]
        public void UnregisterAllBy_OnlyUnregistersMatchingEventType()
        {
            // Arrange
            var stringHook = new SimpleHook<string>(_defaultOwner, _ => true);
            var exceptionHook = new SimpleHook<Exception>(_defaultOwner, _ => true);
            _sut.RegisterHook(stringHook);
            _sut.RegisterHook(exceptionHook);

            // Act
            _sut.UnregisterAllBy<string>(_defaultOwner);

            // Assert - exception hook still registered
            IHook<Exception>[] remaining = _sut.GetOwnerBy<Exception>(_defaultOwner);
            Assert.Single(remaining);
        }

        #endregion

        #region Trigger

        [Fact]
        public void Trigger_WithNullArg_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.Trigger<string>(null));
        }

        [Fact]
        public void Trigger_WithNoHooksRegistered_DoesNotThrow()
        {
            // Act & Assert (no exception)
            _sut.Trigger("test");
        }

        [Fact]
        public void Trigger_WithRegisteredHook_InvokesHook()
        {
            // Arrange
            bool called = false;
            var hook = new SimpleHook<string>(_defaultOwner, arg => { called = true; return true; });
            _sut.RegisterHook(hook);

            // Act
            _sut.Trigger("test");

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void Trigger_PassesArgumentToHook()
        {
            // Arrange
            string received = null;
            var hook = new SimpleHook<string>(_defaultOwner, arg => { received = arg; return true; });
            _sut.RegisterHook(hook);

            // Act
            _sut.Trigger("hello");

            // Assert
            Assert.Equal("hello", received);
        }

        [Fact]
        public void Trigger_WithOnceHook_UnregistersAfterSuccessfulTrigger()
        {
            // Arrange
            int callCount = 0;
            var hook = new SimpleHook<string>(_defaultOwner, arg => { callCount++; return true; }, once: true);
            _sut.RegisterHook(hook);

            // Act
            _sut.Trigger("first");
            _sut.Trigger("second");

            // Assert
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Trigger_WithOnceHook_WhenFuncReturnsFalse_DoesNotUnregister()
        {
            // Arrange
            int callCount = 0;
            var hook = new SimpleHook<string>(_defaultOwner, arg => { callCount++; return false; }, once: true);
            _sut.RegisterHook(hook);

            // Act
            _sut.Trigger("first");
            _sut.Trigger("second");

            // Assert
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void Trigger_WithMultipleHooks_InvokesAllHooks()
        {
            // Arrange
            int callCount = 0;
            var hook1 = new SimpleHook<string>(_defaultOwner, arg => { callCount++; return true; });
            var hook2 = new SimpleHook<string>(_defaultOwner, arg => { callCount++; return true; });
            _sut.RegisterHook(hook1);
            _sut.RegisterHook(hook2);

            // Act
            _sut.Trigger("test");

            // Assert
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void Trigger_WithMultipleHooks_InvokesInAscendingPriorityOrder()
        {
            // Arrange
            var invocationOrder = new List<byte>();
            var lowPriority = new SimpleHook<string>(_defaultOwner, arg => { invocationOrder.Add(200); return true; }, priority: 200);
            var highPriority = new SimpleHook<string>(_defaultOwner, arg => { invocationOrder.Add(10); return true; }, priority: 10);
            var midPriority = new SimpleHook<string>(_defaultOwner, arg => { invocationOrder.Add(100); return true; }, priority: 100);
            _sut.RegisterHook(lowPriority);
            _sut.RegisterHook(midPriority);
            _sut.RegisterHook(highPriority);

            // Act
            _sut.Trigger("test");

            // Assert - lower priority value means higher priority (invoked first)
            Assert.Equal(new byte[] { 10, 100, 200 }, invocationOrder);
        }

        [Fact]
        public void Trigger_WithHookForDifferentEventType_DoesNotInvokeIt()
        {
            // Arrange
            bool called = false;
            var exceptionHook = new SimpleHook<Exception>(_defaultOwner, arg => { called = true; return true; });
            _sut.RegisterHook(exceptionHook);

            // Act
            _sut.Trigger("test");

            // Assert
            Assert.False(called);
        }

        #endregion
    }
}
