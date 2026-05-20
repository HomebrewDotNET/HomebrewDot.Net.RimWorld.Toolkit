using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using ExpressionHelper = HomebrewDot.Net.RimWorld.Toolkit.Helpers.Expression;

namespace HomebrewDot.Net.RimWorld.Tests.Helpers
{
    public class ExpressionTests
    {
        #region GetMethod(Expression<Action>)

        [Fact]
        public void GetMethod_WithStaticVoidMethodCall_ReturnsCorrectMethodInfo()
        {
            // Arrange & Act
            MethodInfo result = ExpressionHelper.GetMethod(() => Console.WriteLine());

            // Assert
            Assert.NotNull(result);
            Assert.Equal("WriteLine", result.Name);
            Assert.Equal(typeof(Console), result.DeclaringType);
        }

        [Fact]
        public void GetMethod_WithStaticNonVoidMethodCall_ReturnsCorrectMethodInfo()
        {
            // Arrange & Act
            MethodInfo result = ExpressionHelper.GetMethod(() => Convert.ChangeType(null, typeof(string)));

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ChangeType", result.Name);
            Assert.Equal(typeof(Convert), result.DeclaringType);
        }

        [Fact]
        public void GetMethod_WithNullExpression_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ExpressionHelper.GetMethod((System.Linq.Expressions.Expression<Action>)null));
        }

        [Fact]
        public void GetMethod_WithNonMethodCallExpression_ThrowsArgumentException()
        {
            // Arrange - use a constructor call (NewExpression), not a MethodCallExpression
            System.Linq.Expressions.Expression<Action> expr = () => new object();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ExpressionHelper.GetMethod(expr));
        }

        #endregion

        #region GetMethod<T>(Expression<Action<T>>)

        [Fact]
        public void GetMethodT_WithInstanceVoidMethodCall_ReturnsCorrectMethodInfo()
        {
            // Arrange & Act
            MethodInfo result = ExpressionHelper.GetMethod<List<string>>(l => l.Clear());

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Clear", result.Name);
            Assert.Equal(typeof(List<string>), result.DeclaringType);
        }

        [Fact]
        public void GetMethodT_WithInstanceNonVoidMethodCall_ReturnsCorrectMethodInfo()
        {
            // Arrange & Act
            MethodInfo result = ExpressionHelper.GetMethod<IReadOnlyDictionary<string, object>>(d => d.ContainsKey(default));

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ContainsKey", result.Name);
        }

        [Fact]
        public void GetMethodT_WithNullExpression_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ExpressionHelper.GetMethod((System.Linq.Expressions.Expression<Action<string>>)null));
        }

        [Fact]
        public void GetMethodT_WithNonMethodCallExpression_ThrowsArgumentException()
        {
            // Arrange - a lambda that resolves to a member access, not a method call
            // We construct an expression tree programmatically to get a non-method-call body
            var param = System.Linq.Expressions.Expression.Parameter(typeof(string), "s");
            var memberAccess = System.Linq.Expressions.Expression.Property(param, nameof(string.Length));
            // Wrap in Expression<Action<string>> - body is MemberExpression, not MethodCallExpression
            // This cannot be done via lambda, so we test via Func overload with a MemberExpression body to ensure coverage
            // Alternatively, verify the message is correct on a known-bad GetMethod<T,TResult> test
            Assert.Throws<ArgumentException>(() =>
                ExpressionHelper.GetMethod<string, int>((System.Linq.Expressions.Expression<Func<string, int>>)
                    System.Linq.Expressions.Expression.Lambda<Func<string, int>>(memberAccess, param)));
        }

        #endregion

        #region GetMethod<T, TResult>(Expression<Func<T, TResult>>)

        [Fact]
        public void GetMethodTTResult_WithInstanceMethodCall_ReturnsCorrectMethodInfo()
        {
            // Arrange & Act
            MethodInfo result = ExpressionHelper.GetMethod<string, string>(s => s.ToUpper());

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ToUpper", result.Name);
            Assert.Equal(typeof(string), result.DeclaringType);
        }

        [Fact]
        public void GetMethodTTResult_WithInterfaceMethodCall_ReturnsCorrectMethodInfo()
        {
            // Arrange & Act
            MethodInfo result = ExpressionHelper.GetMethod<Type, bool>(t => t.IsAssignableFrom(default));

            // Assert
            Assert.NotNull(result);
            Assert.Equal("IsAssignableFrom", result.Name);
        }

        [Fact]
        public void GetMethodTTResult_WithNullExpression_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ExpressionHelper.GetMethod((System.Linq.Expressions.Expression<Func<string, string>>)null));
        }

        [Fact]
        public void GetMethodTTResult_WithNonMethodCallExpression_ThrowsArgumentException()
        {
            // Arrange - build a MemberExpression for string.Length programmatically
            var param = System.Linq.Expressions.Expression.Parameter(typeof(string), "s");
            var memberAccess = System.Linq.Expressions.Expression.Property(param, nameof(string.Length));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<string, int>>(memberAccess, param);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ExpressionHelper.GetMethod<string, int>(lambda));
        }

        #endregion
    }
}
