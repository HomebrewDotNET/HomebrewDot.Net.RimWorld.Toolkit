using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Operator that compares the string version of the left value against the string version of the right value, where he right value is a regex.
    /// </summary>
    public class MatchOperatorType : IOperatorTypeCompileable
    {
        // Constants
        /// <summary>
        /// The default name of the operator, which is used when no name is specified in the definition. This is also the name that should be used when referencing this operator type in definitions or code.
        /// </summary>
        public const string DefaultTypeName = "Match";
        // Statics
        /// <summary>
        /// The singleton instance of the <see cref="MatchOperatorType"/>, which can be used to reference this operator type without needing to create multiple instances.
        /// </summary>
        public static readonly MatchOperatorType Instance = new MatchOperatorType();
        /// <summary>
        /// The aliases for this operator type, which can be used to reference this operator type in definitions or code.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Aliases = new[] { DefaultTypeName, "Matches", "Regex" };

        private MatchOperatorType()
        {
            
        }

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if(left == null || right == null)
                return false;

            var leftStr = left.ToString();
            var rightStr = right.ToString();

            if(Regex.IsMatch(leftStr, rightStr))
                return true;

            return false;
        }
        /// <inheritdoc/>
        public string GetCacheKey(Type left, Type right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            if(right != null && (right.Equals(typeof(string)) || right.Equals(typeof(Regex))))
            {
                return $"{GetType().FullName}_{left?.FullName ?? "NULL"}_{right.FullName}";
            }
            return null;
        }
        /// <inheritdoc/>
        public Expression Compile(Expression leftValue, Type leftExpressionType, Expression rightValue, Type rightExpressionType, ParameterExpression argumentsParameter, ParameterExpression contextParameter, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            // Setup left
            Expression getLeft;
            Expression getLeftString;
            List<ParameterExpression> variables = new List<ParameterExpression>();
            List<Expression> setup = new List<Expression>();
            if(leftValue is not ConstantExpression)
            {
                var leftVariable = Expression.Variable(leftExpressionType, "leftValue");
                variables.Add(leftVariable);
                setup.Add(Expression.Assign(leftVariable, leftValue));
                getLeft = leftVariable;
            }
            else
            {
                getLeft = leftValue;
            }
            if (leftExpressionType != typeof(string))
            {
                var leftStringVariable = Expression.Variable(typeof(string), "leftStringValue");
                if(leftExpressionType.IsValueType)
                {
                    setup.Add(Expression.Assign(leftStringVariable, Expression.Call(getLeft, ToolkitConstants.ObjectCache<object>.ToStringMethod)));
                }
                else
                {
                    var ifLeftNotNullAssign = Expression.IfThen(
                        Expression.NotEqual(getLeft, Expression.Default(leftExpressionType)),
                        Expression.Assign(leftStringVariable, Expression.Call(getLeft, ToolkitConstants.ObjectCache<object>.ToStringMethod))
                    );
                    variables.Add(leftStringVariable);
                    setup.Add(ifLeftNotNullAssign);
                }
                getLeftString = leftStringVariable;
            }
            else
            {
                getLeftString = getLeft;
            }
            Expression getRegex = null;
            // Setup regex
            if(rightExpressionType == typeof(Regex))
            {
                getRegex = rightValue;
            }
            else 
            {
                Expression getRightString = null;
                // Setup right
                if (rightExpressionType != typeof(string))
                {
                    var rightVariable = Expression.Variable(rightExpressionType, "rightValue");
                    variables.Add(rightVariable);
                    var assignRightVariable = Expression.Assign(rightVariable, rightValue);
                    setup.Add(assignRightVariable);
                    var rightStringVariable = Expression.Variable(typeof(string), "rightStringValue");
                    variables.Add(rightStringVariable);

                    if(rightExpressionType.IsValueType)
                    {
                        setup.Add(Expression.Assign(rightStringVariable, Expression.Call(rightVariable, ToolkitConstants.ObjectCache<object>.ToStringMethod)));
                    }
                    else
                    {
                        var ifRightNotNullAssign = Expression.IfThen(
                            Expression.NotEqual(rightVariable, Expression.Default(rightExpressionType)),
                            Expression.Assign(rightStringVariable, Expression.Call(rightVariable, ToolkitConstants.ObjectCache<object>.ToStringMethod))
                        );
                    }
                }
                else
                {
                    getRightString = rightValue;
                }

                var regexVariable = Expression.Variable(typeof(Regex), "regexValue");
                var regexConstructor = Toolkit.Helpers.Expression.GetConstructor<Regex>(() => new Regex(default(string), default(RegexOptions)));
                var regexOptions = Expression.Constant(RegexOptions.Compiled);
                var getOrSetRegexCache = Toolkit.Helpers.Expression.GetMethod(() => Toolkit.Cache<string, Regex>.GetOrSet(default, default, default));
                var createRegex = Expression.New(regexConstructor, getRightString, regexOptions);
                var valueFactory = Expression.Lambda<Func<Regex>>(createRegex);
                var getOrSetRegex = Expression.Call(getOrSetRegexCache, getRightString, valueFactory, Expression.Constant(true));

                var ifStringNotNullAssign = Expression.IfThen(
                    Expression.NotEqual(getRightString, Expression.Default(typeof(string))),
                    Expression.Assign(regexVariable, getOrSetRegex)
                );
                variables.Add(regexVariable);
                setup.Add(ifStringNotNullAssign);
                getRegex = regexVariable;
            }

            var regexResultVariable = Expression.Variable(typeof(bool), "regexResult");
            var isMatchMethod = Toolkit.Helpers.Expression.GetMethod<Regex>(x => x.IsMatch(default));

            var ifLeftNotNullAndRegexNotNullAssign = Expression.IfThen(
                Expression.AndAlso(
                    Expression.NotEqual(getLeftString, Expression.Default(typeof(string))),
                    Expression.NotEqual(getRegex, Expression.Default(typeof(Regex)))
                ),
                Expression.Assign(regexResultVariable, Expression.Call(getRegex, isMatchMethod, getLeftString))
            );
            variables.Add(regexResultVariable);
            setup.Add(ifLeftNotNullAndRegexNotNullAssign);

            return Expression.Block(variables, setup.Concat(new[] { regexResultVariable }));
        }
    }
}
