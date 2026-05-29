using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.RimWorld.Comparing.Template
{
    /// <summary>
    /// Base class for creating operator types that use native C# operators (e.g. ==, !=, >, <, etc.).
    /// </summary>
    public abstract class BaseNativeOperator
    {
        // Statics
        private readonly static Dictionary<Type, Dictionary<Type, Dictionary<string, Func<object, object, bool>>>> _methodCache = new Dictionary<Type, Dictionary<Type, Dictionary<string, Func<object, object, bool>>>>();
        private static bool TryGetCompareDelegate(Type left, Type right, NativeOperatorType type, out Func<object, object, bool> compare)
        {
            compare = null;
            var methodName = type.GetMethodName();
            var method = left.GetMethod(methodName, [left, right]) ?? right.GetMethod(methodName, [left, right]);

            
            if (method == null) return false;
            var methodParameters = method.GetParameters();
            if (!method.IsPublic) return false;
            if (method.ReturnType != typeof(bool)) return false;
            if (methodParameters.Length != 2) return false;
            if (!method.IsStatic) return false;
            var leftParameter = Expression.Parameter(typeof(object), "left");
            var rightParameter = Expression.Parameter(typeof(object), "right");
            var castLeft = Expression.Convert(leftParameter, left);
            var castRight = Expression.Convert(rightParameter, right);
            var leftVariable = Expression.Variable(left, "leftVar");
            var rightVariable = Expression.Variable(right, "rightVar");
            var assignLeft = Expression.Assign(leftVariable, castLeft);
            var assignRight = Expression.Assign(rightVariable, castRight);
            var leftType = methodParameters[0].ParameterType;
            var rightType = methodParameters[1].ParameterType;
            Expression argumentLeft = leftVariable;
            Expression argumentRight = rightVariable;
            if (leftType != left)
            {
                // Cast just to be sure
                argumentLeft = Expression.Convert(leftVariable, leftType);
            }
            if(rightType != right) {
                // Cast just to be sure
                argumentRight = Expression.Convert(rightVariable, rightType);
            }
            var compareCall = Expression.Call(method, argumentLeft, argumentRight);
            var body = Expression.Block(new[] { leftVariable, rightVariable }, assignLeft, assignRight, compareCall);

            compare = Expression.Lambda<Func<object, object, bool>>(body, leftParameter, rightParameter).Compile();
            return true;
        }

        private static bool TryGetCompareDelegateSingle(Type left, Type right, NativeOperatorType type, out Func<object, object, bool> compare)
        {
            compare = null;
            var methodName = type.GetMethodName();
            var method = left.GetMethod(methodName, [left, right]) ?? right.GetMethod(methodName, [left, right]);

            if (method == null) return false;
            if (!method.IsPublic) return false;
            if (method.ReturnType != typeof(bool)) return false;
            if (method.GetParameters().Length != 1) return false;
            if (!method.IsStatic) return false;
            var leftParameter = Expression.Parameter(typeof(object), "left");
            var rightParameter = Expression.Parameter(typeof(object), "right");
            var leftVariable = Expression.Variable(left, "leftVar");
            var rightVariable = Expression.Variable(right, "rightVar");
            var castLeft = Expression.Assign(leftVariable, Expression.Convert(leftParameter, left));
            var castRight = Expression.Assign(rightVariable, Expression.Convert(rightParameter, right));
            var compareLeft = Expression.Call(method, leftVariable);
            var compareRight = Expression.Call(method, rightVariable);
            var leftOrRight = Expression.Or(compareLeft, compareRight);

            var body = Expression.Block(new[] { leftVariable, rightVariable }, castLeft, castRight, leftOrRight);
            compare = Expression.Lambda<Func<object, object, bool>>(body, leftParameter, rightParameter).Compile();

            return true;
        }

        /// <summary>
        /// Tries to compare the left and right operands using the specified native operator type. If the operator is not defined for the given types, returns null. Otherwise, returns the result of the comparison.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <param name="type">The native operator type to use for comparison.</param>
        /// <returns>The result of the comparison, or null if the operator is not defined for the given types.</returns>
        protected bool? Compare(object left, object right, NativeOperatorType type)
        {
            if (left == null || right == null) return null;
            var leftType = left.GetType();
            var rightType = right.GetType();
            if (!_methodCache.TryGetValue(leftType, out var rightDict))
            {
                lock (_methodCache)
                {
                    if (!_methodCache.TryGetValue(leftType, out rightDict))
                    {
                        rightDict = new Dictionary<Type, Dictionary<string, Func<object, object, bool>>>();
                        _methodCache[leftType] = rightDict;
                    }
                }
            }
            if (!rightDict.TryGetValue(rightType, out var operatorDict))
            {
                lock (rightDict)
                {
                    if (!rightDict.TryGetValue(rightType, out operatorDict))
                    {
                        operatorDict = new Dictionary<string, Func<object, object, bool>>();
                        rightDict[rightType] = operatorDict;
                    }
                }
            }
            var methodName = type.GetMethodName();
            if (!operatorDict.TryGetValue(methodName, out var compare))
            {
                lock (operatorDict)
                {
                    if (!operatorDict.TryGetValue(methodName, out compare))
                    {

                        if (!(type == NativeOperatorType.True || type == NativeOperatorType.False ? TryGetCompareDelegateSingle(leftType, rightType, type, out compare) : TryGetCompareDelegate(leftType, rightType, type, out compare)))
                        {
                            operatorDict[methodName] = null;
                            return null;
                        }
                        operatorDict[methodName] = compare;
                    }
                }
            }
            if (compare == null) return null;
            return compare(left, right);
        }
    }

    /// <summary>
    /// Enumeration of native operator types that can be used in the <see cref="BaseNativeOperator"/> class to specify which operator to use for comparison.
    /// </summary>
    public enum NativeOperatorType
    {
        /// <summary>
        /// Checks if the left and right operands are equal using the == operator.
        /// </summary>
        Equal,
        /// <summary>
        /// Checks if the left and right operands are not equal using the != operator.
        /// </summary>
        NotEqual,
        /// <summary>
        /// Checks if the left operand is greater than the right operand using the > operator.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Checks if the left operand is less than the right operand using the < operator.
        /// </summary>
        LessThan,
        /// <summary>
        /// Checks if the left operand is greater than or equal to the right operand using the >= operator.
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// Checks if the left operand is less than or equal to the right operand using the <= operator.
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// Checks if the left is equal to true.
        /// </summary>
        True,
        /// <summary>
        /// Checks if the left is equal to false.
        /// </summary>
        False
    }

    /// <summary>
    /// Contains extension methods for the <see cref="NativeOperatorType"/> enumeration.
    /// </summary>
    public static class NativeOperatorTypeExtensions
    {
        /// <summary>
        /// Gets the <see cref="NativeOperatorType"/> corresponding to the given operator string (e.g. "==", "!=", ">", "<", ">=", "<=", "true", "false").
        /// </summary>
        /// <param name="type">The <see cref="NativeOperatorType"/> instance.</param>
        /// <param name="operatorString">The operator string to convert.</param>
        /// <returns>The corresponding <see cref="NativeOperatorType"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the operator string is invalid.</exception>
        public static NativeOperatorType GetByOperator(this NativeOperatorType type, string operatorString)
        {
            return operatorString switch
            {
                "==" => NativeOperatorType.Equal,
                "!=" => NativeOperatorType.NotEqual,
                ">" => NativeOperatorType.GreaterThan,
                "<" => NativeOperatorType.LessThan,
                ">=" => NativeOperatorType.GreaterThanOrEqual,
                "<=" => NativeOperatorType.LessThanOrEqual,
                "true" => NativeOperatorType.True,
                "false" => NativeOperatorType.False,
                _ => throw new ArgumentException($"Invalid operator string: {operatorString}")
            };
        }
        public static string GetMethodName(this NativeOperatorType type)
        {
            return type switch
            {
                NativeOperatorType.Equal => "op_Equality",
                NativeOperatorType.NotEqual => "op_Inequality",
                NativeOperatorType.GreaterThan => "op_GreaterThan",
                NativeOperatorType.LessThan => "op_LessThan",
                NativeOperatorType.GreaterThanOrEqual => "op_GreaterThanOrEqual",
                NativeOperatorType.LessThanOrEqual => "op_LessThanOrEqual",
                NativeOperatorType.True => "op_True",
                NativeOperatorType.False => "op_False",
                _ => throw new ArgumentException($"Invalid operator type: {type}")
            };
        }
        /// <summary>
        /// Gets the operator string corresponding to the given <see cref="NativeOperatorType"/> (e.g. "==", "!=", ">", "<", ">=", "<=", "true", "false").
        /// </summary>
        /// <param name="type">The <see cref="NativeOperatorType"/> instance.</param>
        /// <returns>The corresponding operator string.</returns>
        /// <exception cref="ArgumentException">Thrown when the operator type is invalid.</exception>
        public static string ToOperatorString(this NativeOperatorType type)
        {
            return type switch
            {
                NativeOperatorType.Equal => "==",
                NativeOperatorType.NotEqual => "!=",
                NativeOperatorType.GreaterThan => ">",
                NativeOperatorType.LessThan => "<",
                NativeOperatorType.GreaterThanOrEqual => ">=",
                NativeOperatorType.LessThanOrEqual => "<=",
                NativeOperatorType.True => "true",
                NativeOperatorType.False => "false",
                _ => throw new ArgumentException($"Invalid operator type: {type}")
            };
        }
    }
}
