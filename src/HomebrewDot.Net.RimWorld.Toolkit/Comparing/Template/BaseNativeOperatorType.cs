using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Comparing.Template
{
    /// <summary>
    /// Base class for creating operator types that use native C# operators (e.g. ==, !=, >, <, etc.).
    /// </summary>
    public abstract class BaseNativeOperatorType
    {
        // Statics
        private const BindingFlags MethodFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        private readonly static Dictionary<Type, Dictionary<Type, Dictionary<string, Func<object, object, bool>>>> _methodCache = new Dictionary<Type, Dictionary<Type, Dictionary<string, Func<object, object, bool>>>>();
        private static bool TryGetCompareDelegate(Type left, Type right, NativeOperatorType type, out Func<object, object, bool> compare)
        {
            compare = null;
            var methodName = type.GetMethodName();
            var method = left.GetMethod(methodName, MethodFlags, null, new[] { left, right }, null) ?? right.GetMethod(methodName, MethodFlags, null, new[] { left, right }, null);

            
            if (method == null)
            {
                // Fall back to object.Equals for Equal/NotEqual when the operand types do not define a
                // static operator method (e.g. == on reference types like Verse defs). object.Equals
                // resolves to reference equality for types that do not override Equals.
                if (type == NativeOperatorType.Equal || type == NativeOperatorType.NotEqual)
                {
                    if (type == NativeOperatorType.Equal)
                    {
                        compare = object.Equals;
                    }
                    else
                    {
                        compare = (leftValue, rightValue) => !object.Equals(leftValue, rightValue);
                    }
                    return true;
                }
                return false;
            }
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
            var method = left.GetMethod(methodName, MethodFlags, null, new[] { left, right }, null) ?? right.GetMethod(methodName, MethodFlags, null, new[] { left, right }, null);

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
        /// Coerces enum operands to a comparable numeric type so native operators can compare them.
        /// Enums do not define operator methods, expression trees reject enum operands for relational
        /// operators (e.g. <see cref="Expression.LessThan"/> throws for enums), and
        /// <see cref="Enum.CompareTo(object)"/> throws <see cref="ArgumentException"/> when the other
        /// operand is a numeric type instead of the same enum type (e.g. <c>Byte.CompareTo(Int32)</c>).
        /// Converting to the other operand's numeric type (or the wider underlying type when both sides
        /// are enums) makes enum-vs-numeric and enum-vs-enum comparisons work through the normal numeric
        /// paths, e.g. a <c>QualityCategory</c> comp property compared with <c>LessThan 2</c>.
        /// </summary>
        /// <param name="left">The left operand, coerced in place when it is an enum.</param>
        /// <param name="right">The right operand, coerced in place when it is an enum.</param>
        protected static void NormalizeEnumOperands(ref object left, ref object right)
        {
            var leftType = left?.GetType();
            var rightType = right?.GetType();
            var leftIsEnum = leftType != null && leftType.IsEnum;
            var rightIsEnum = rightType != null && rightType.IsEnum;
            if (!leftIsEnum && !rightIsEnum)
            {
                return;
            }

            var targetType = GetEnumComparisonTarget(leftIsEnum, leftType, rightIsEnum, rightType);
            if (leftIsEnum)
            {
                left = Convert.ChangeType(left, targetType, CultureInfo.InvariantCulture);
            }
            if (rightIsEnum)
            {
                right = Convert.ChangeType(right, targetType, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Coerces enum operand types to a comparable numeric type, wrapping the operand expressions in a
        /// <see cref="Expression.Convert"/> when provided. See <see cref="NormalizeEnumOperands(ref object, ref object)"/>
        /// for why this is needed; the expression-tree operator factories only accept the underlying type.
        /// </summary>
        /// <param name="left">The left operand expression, converted in place when its type is an enum.</param>
        /// <param name="right">The right operand expression, converted in place when its type is an enum.</param>
        /// <param name="leftType">The left operand type, coerced in place when it is an enum.</param>
        /// <param name="rightType">The right operand type, coerced in place when it is an enum.</param>
        private static void NormalizeEnumTypes(ref Expression left, ref Expression right, ref Type leftType, ref Type rightType)
        {
            var leftIsEnum = leftType != null && leftType.IsEnum;
            var rightIsEnum = rightType != null && rightType.IsEnum;
            if (!leftIsEnum && !rightIsEnum)
            {
                return;
            }

            var targetType = GetEnumComparisonTarget(leftIsEnum, leftType, rightIsEnum, rightType);
            if (leftIsEnum)
            {
                leftType = targetType;
                if (left != null)
                {
                    left = Expression.Convert(left, targetType);
                }
            }
            if (rightIsEnum)
            {
                rightType = targetType;
                if (right != null)
                {
                    right = Expression.Convert(right, targetType);
                }
            }
        }

        /// <summary>
        /// Picks a common numeric type that both operands can be coerced to for comparison. When exactly one
        /// side is an enum, the other side's numeric type is preferred (e.g. a byte-backed enum compared with
        /// an <see cref="int"/> becomes an <see cref="int"/> comparison). When both sides are enums, the wider
        /// of the two underlying types is used so the comparison stays valid.
        /// </summary>
        private static Type GetEnumComparisonTarget(bool leftIsEnum, Type leftType, bool rightIsEnum, Type rightType)
        {
            if (leftIsEnum && rightIsEnum)
            {
                var leftUnderlying = Enum.GetUnderlyingType(leftType);
                var rightUnderlying = Enum.GetUnderlyingType(rightType);
                if (leftUnderlying == rightUnderlying)
                {
                    return leftUnderlying;
                }
                return IsWiderNumericType(leftUnderlying, rightUnderlying) ? leftUnderlying : rightUnderlying;
            }
            if (leftIsEnum)
            {
                return IsNumericType(rightType) ? rightType : Enum.GetUnderlyingType(leftType);
            }
            return IsNumericType(leftType) ? leftType : Enum.GetUnderlyingType(rightType);
        }

        private static bool IsWiderNumericType(Type a, Type b)
        {
            return GetNumericTypeSize(a) >= GetNumericTypeSize(b);
        }

        private static int GetNumericTypeSize(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return 1;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return 2;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Single:
                    return 4;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Double:
                    return 8;
                case TypeCode.Decimal:
                    return 16;
                default:
                    return 0;
            }
        }

        private static bool IsNumericType(Type type)
        {
            if (type == null)
            {
                return false;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
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
            NormalizeEnumOperands(ref left, ref right);
            var leftType = left.GetType();
            var rightType = right.GetType();
            if (!_methodCache.TryGetValue(leftType, out var rightDict))
            {
                if (!_methodCache.TryGetValue(leftType, out rightDict))
                {
                    rightDict = new Dictionary<Type, Dictionary<string, Func<object, object, bool>>>();
                    _methodCache[leftType] = rightDict;
                }
            }
            if (!rightDict.TryGetValue(rightType, out var operatorDict))
            {
                if (!rightDict.TryGetValue(rightType, out operatorDict))
                {
                    operatorDict = new Dictionary<string, Func<object, object, bool>>();
                    rightDict[rightType] = operatorDict;
                }
            }
            var methodName = type.GetMethodName();
            if (!operatorDict.TryGetValue(methodName, out var compare))
            {
                if (!(type == NativeOperatorType.True || type == NativeOperatorType.False ? TryGetCompareDelegateSingle(leftType, rightType, type, out compare) : TryGetCompareDelegate(leftType, rightType, type, out compare)))
                {
                    operatorDict[methodName] = null;
                    return null;
                }
                operatorDict[methodName] = compare;
            }
            if (compare == null) return null;
            return compare(left, right);
        }

        /// <summary>
        /// Guards a compiled comparison against null operands, mirroring <see cref="Compare(object, object, NativeOperatorType)"/>
        /// which treats a null operand as no-match. Reference-type operands can be null (e.g. a collection that contains null
        /// elements, where the reference-equality fallback would otherwise treat null == null as a match); value-type operands
        /// are never null so no guard is emitted for them.
        /// </summary>
        /// <param name="leftOperand">The left operand expression.</param>
        /// <param name="leftType">The static type of the left operand.</param>
        /// <param name="rightOperand">The right operand expression.</param>
        /// <param name="rightType">The static type of the right operand.</param>
        /// <param name="comparison">The comparison expression to guard.</param>
        /// <returns>The comparison expression short-circuited to false when either operand is null.</returns>
        private static Expression GuardNulls(Expression leftOperand, Type leftType, Expression rightOperand, Type rightType, Expression comparison)
        {
            var leftIsNotNull = leftType.IsValueType
                ? (Expression)Expression.Constant(true)
                : Expression.NotEqual(leftOperand, Expression.Constant(null, leftType));
            var rightIsNotNull = rightType.IsValueType
                ? (Expression)Expression.Constant(true)
                : Expression.NotEqual(rightOperand, Expression.Constant(null, rightType));
            return Expression.AndAlso(leftIsNotNull, Expression.AndAlso(rightIsNotNull, comparison));
        }

        /// <summary>
        /// Tries to compile an expression that compares the left and right parameters using the specified native operator type. If the operator is not defined for the given types, returns false. Otherwise, returns true and outputs the compiled expression.
        /// </summary>
        /// <param name="left">The left parameter expression.</param>
        /// <param name="right">The right parameter expression.</param>
        /// <param name="leftType">The type of the left parameter.</param>
        /// <param name="rightType">The type of the right parameter.</param>
        /// <param name="type">The native operator type to use for comparison.</param>
        /// <param name="checkOnly">If true, only checks if the operator can be compiled for the given types without actually compiling the expression. This can be used for performance optimization when only checking for compatibility.</param>
        /// <param name="compareExpression">The compiled comparison expression.</param>
        /// <returns>True if the expression was successfully compiled; otherwise, false.</returns>
        protected bool TryCompile(Expression left, Expression right, Type leftType, Type rightType, NativeOperatorType type, bool checkOnly, out Expression compareExpression)
        {
            compareExpression = null;

            NormalizeEnumTypes(ref left, ref right, ref leftType, ref rightType);

            var methodName = type.GetMethodName();
            var method = leftType.GetMethod(methodName, MethodFlags, null, new[] { leftType, rightType }, null) ?? rightType.GetMethod(methodName, MethodFlags, null, new[] { leftType, rightType }, null);
            bool isSingle = type == NativeOperatorType.True || type == NativeOperatorType.False;

            if(method == null)
            {
                try
                {
                    var leftOperand = left ?? Expression.Default(leftType);
                    var rightOperand = right ?? Expression.Default(rightType);
                    var comparison = type.GetMethodCall(leftOperand, rightOperand);
                    compareExpression = type == NativeOperatorType.Equal || type == NativeOperatorType.NotEqual
                        ? GuardNulls(leftOperand, leftType, rightOperand, rightType, comparison)
                        : comparison;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    // Fall back to object.Equals for Equal/NotEqual when the operand types do not define a
                    // static operator method (e.g. == on reference types like Verse defs). object.Equals
                    // resolves to reference equality for types that do not override Equals. The static
                    // object.Equals(object, object) overload is null-safe (does not throw on null) but
                    // returns true for null == null, so it is wrapped in the same null guard as above to
                    // mirror Compare(): a null operand never matches.
                    if (type == NativeOperatorType.Equal || type == NativeOperatorType.NotEqual)
                    {
                        var leftOperand = left ?? Expression.Default(leftType);
                        var rightOperand = right ?? Expression.Default(rightType);
                        var equalsMethod = typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object), typeof(object) });
                        var equalCall = Expression.Call(
                            equalsMethod,
                            Expression.Convert(leftOperand, typeof(object)),
                            Expression.Convert(rightOperand, typeof(object)));
                        Expression comparison = type == NativeOperatorType.Equal ? (Expression)equalCall : Expression.Not(equalCall);
                        compareExpression = GuardNulls(leftOperand, leftType, rightOperand, rightType, comparison);
                        return true;
                    }
                    return false;
                }
            }

            if (method == null) return false;
            var methodParameters = method.GetParameters();
            if (!method.IsPublic) return false;
            if (method.ReturnType != typeof(bool)) return false;
            if (methodParameters.Length != (isSingle ? 1 : 2)) return false;
            if (!method.IsStatic) return false;

            if(checkOnly) return true;

            if (isSingle)
            {
                var compareLeft = Expression.Call(method, methodParameters[0].ParameterType.IsAssignableFrom(leftType) ? left : Expression.Convert(left, methodParameters[0].ParameterType));
                var compareRight = Expression.Call(method, methodParameters[0].ParameterType.IsAssignableFrom(rightType) ? right : Expression.Convert(right, methodParameters[0].ParameterType));
                compareExpression = Expression.Or(compareLeft, compareRight);
            }
            else
            {
                compareExpression = Expression.Call(
                    method, 
                    methodParameters[0].ParameterType.IsAssignableFrom(leftType) ? left : Expression.Convert(left, methodParameters[0].ParameterType),
                    methodParameters[1].ParameterType.IsAssignableFrom(rightType) ? right : Expression.Convert(right, methodParameters[1].ParameterType));
            }

            return true;
        }
    }

    /// <summary>
    /// Enumeration of native operator types that can be used in the <see cref="BaseNativeOperatorType"/> class to specify which operator to use for comparison.
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
        /// <summary>
        /// Gets the method name corresponding to the given <see cref="NativeOperatorType"/> (e.g. "op_Equality", "op_Inequality", "op_GreaterThan", "op_LessThan", "op_GreaterThanOrEqual", "op_LessThanOrEqual", "op_True", "op_False").
        /// </summary>
        /// <param name="type">The <see cref="NativeOperatorType"/> instance.</param>
        /// <returns>The corresponding method name.</returns>
        /// <exception cref="ArgumentException">Thrown when the operator type is invalid.</exception>
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
        /// Gets the expression that compares the left and right parameters using the specified native operator type. If the operator is not defined for the given types, throws an exception.
        /// </summary>
        /// <param name="type">The <see cref="NativeOperatorType"/> instance.</param>
        /// <param name="left">The left <see cref="Expression"/>.</param>
        /// <param name="right">The right <see cref="Expression"/>.</param>
        /// <returns>The corresponding <see cref="Expression"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the operator type is invalid.</exception>
        public static Expression GetMethodCall(this NativeOperatorType type, Expression left, Expression right)
        {
            return type switch
            {
                NativeOperatorType.Equal => Expression.Equal(left, right),
                NativeOperatorType.NotEqual => Expression.NotEqual(left, right),
                NativeOperatorType.GreaterThan => Expression.GreaterThan(left, right),
                NativeOperatorType.LessThan => Expression.LessThan(left, right),
                NativeOperatorType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
                NativeOperatorType.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
                NativeOperatorType.True => Expression.Or(Expression.Equal(left, Expression.Constant(true)), Expression.Equal(right, Expression.Constant(true))),
                NativeOperatorType.False => Expression.Or(Expression.Equal(left, Expression.Constant(false)), Expression.Equal(right, Expression.Constant(false))),
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
