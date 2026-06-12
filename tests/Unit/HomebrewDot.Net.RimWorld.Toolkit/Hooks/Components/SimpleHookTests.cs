using System;
using Xunit;
using HomebrewDot.Net.Rimworld.Hooks;

namespace HomebrewDot.Net.Rimworld.Tests.Hooks.Components
{
    public class SimpleHookTests
    {
        private static readonly object DefaultOwner = new object();

        #region Constructor (Action overload)

        [Fact]
        public void Constructor_ActionOverload_WithNullOwner_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SimpleHook<string>(null, arg => { }));
        }

        [Fact]
        public void Constructor_ActionOverload_WithNullAction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SimpleHook<string>(DefaultOwner, (Action<string>)null));
        }

        [Fact]
        public void Constructor_ActionOverload_SetsOwner()
        {
            // Arrange
            var owner = new object();

            // Act
            var hook = new SimpleHook<string>(owner, arg => { });

            // Assert
            Assert.Same(owner, hook.Owner);
        }

        [Fact]
        public void Constructor_ActionOverload_DefaultOnce_IsFalse()
        {
            // Act
            var hook = new SimpleHook<string>(DefaultOwner, arg => { });

            // Assert
            Assert.False(hook.Once);
        }

        [Fact]
        public void Constructor_ActionOverload_WithOnceTrue_SetsOnce()
        {
            // Act
            var hook = new SimpleHook<string>(DefaultOwner, arg => { }, once: true);

            // Assert
            Assert.True(hook.Once);
        }

        [Fact]
        public void Constructor_ActionOverload_DefaultPriority_Is128()
        {
            // Act
            var hook = new SimpleHook<string>(DefaultOwner, arg => { });

            // Assert
            Assert.Equal(128, hook.Priority);
        }

        [Fact]
        public void Constructor_ActionOverload_WithCustomPriority_SetsPriority()
        {
            // Act
            var hook = new SimpleHook<string>(DefaultOwner, arg => { }, priority: 10);

            // Assert
            Assert.Equal(10, hook.Priority);
        }

        #endregion

        #region Constructor (Func overload)

        [Fact]
        public void Constructor_FuncOverload_WithNullOwner_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SimpleHook<string>(null, arg => true));
        }

        [Fact]
        public void Constructor_FuncOverload_WithNullAction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SimpleHook<string>(DefaultOwner, (Func<string, bool>)null));
        }

        [Fact]
        public void Constructor_FuncOverload_SetsOwner()
        {
            // Arrange
            var owner = new object();

            // Act
            var hook = new SimpleHook<string>(owner, arg => true);

            // Assert
            Assert.Same(owner, hook.Owner);
        }

        #endregion

        #region OnTrigger

        [Fact]
        public void OnTrigger_ActionOverload_WhenActionExecutes_ReturnsTrue()
        {
            // Arrange
            bool actionCalled = false;
            var hook = new SimpleHook<string>(DefaultOwner, arg => { actionCalled = true; });

            // Act
            bool result = hook.OnTrigger("test");

            // Assert
            Assert.True(result);
            Assert.True(actionCalled);
        }

        [Fact]
        public void OnTrigger_FuncOverload_WhenFuncReturnsTrue_ReturnsTrue()
        {
            // Arrange
            var hook = new SimpleHook<string>(DefaultOwner, arg => true);

            // Act
            bool result = hook.OnTrigger("test");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void OnTrigger_FuncOverload_WhenFuncReturnsFalse_ReturnsFalse()
        {
            // Arrange
            var hook = new SimpleHook<string>(DefaultOwner, arg => false);

            // Act
            bool result = hook.OnTrigger("test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void OnTrigger_PassesArgumentToAction()
        {
            // Arrange
            string received = null;
            var hook = new SimpleHook<string>(DefaultOwner, arg => { received = arg; });

            // Act
            hook.OnTrigger("hello");

            // Assert
            Assert.Equal("hello", received);
        }

        [Fact]
        public void OnTrigger_WhenActionThrows_WithErrorHandler_CallsErrorHandlerAndReturnsItsResult()
        {
            // Arrange
            var thrownEx = new InvalidOperationException("boom");
            bool errorHandlerCalled = false;
            var hook = new SimpleHook<string>(
                DefaultOwner,
                arg => throw thrownEx,
                errorHandler: (ex, arg) => { errorHandlerCalled = true; return false; });

            // Act
            bool result = hook.OnTrigger("test");

            // Assert
            Assert.False(result);
            Assert.True(errorHandlerCalled);
        }

        [Fact]
        public void OnTrigger_WhenActionThrows_WithErrorHandlerReturningTrue_ReturnsTrue()
        {
            // Arrange
            var hook = new SimpleHook<string>(
                DefaultOwner,
                arg => throw new InvalidOperationException(),
                errorHandler: (ex, arg) => true);

            // Act
            bool result = hook.OnTrigger("test");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void OnTrigger_WhenActionThrows_WithoutErrorHandler_Rethrows()
        {
            // Arrange
            var thrownEx = new InvalidOperationException("boom");
            var hook = new SimpleHook<string>(DefaultOwner, arg => throw thrownEx);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => hook.OnTrigger("test"));
            Assert.Same(thrownEx, ex);
        }

        [Fact]
        public void OnTrigger_ErrorHandler_ReceivesCorrectExceptionAndArgument()
        {
            // Arrange
            var thrownEx = new InvalidOperationException("boom");
            Exception receivedEx = null;
            string receivedArg = null;
            var hook = new SimpleHook<string>(
                DefaultOwner,
                arg => throw thrownEx,
                errorHandler: (ex, arg) => { receivedEx = ex; receivedArg = arg; return false; });

            // Act
            hook.OnTrigger("hello");

            // Assert
            Assert.Same(thrownEx, receivedEx);
            Assert.Equal("hello", receivedArg);
        }

        #endregion
    }
}
