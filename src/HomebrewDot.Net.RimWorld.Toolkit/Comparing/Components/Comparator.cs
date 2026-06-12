using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using HomebrewDot.Net.Rimworld.Generic.Models;
using HomebrewDot.Net.Rimworld.Referencing;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// Default implementation of the <see cref="IComparator"/> interface, which evaluates conditions defined in <see cref="ConditionDef"/> instances against a given context.
    /// </summary>
    public class Comparator : IComparator
    {
        // Constants
        /// <summary>
        /// The key used in the context dictionary to store the operator types that the resolver can use to resolve references. 
        /// The value associated with this key should be an <see cref="IReadOnlyDictionary{string, IOperatorType}"/> where the keys are the operator type names and the values are the corresponding <see cref="IOperator"/> instances that can resolve references of that type.
        /// Will take precedence over the operator types provided in the constructor if both are present, allowing for dynamic overriding of operator types on a per-resolution basis. If this key is not present in the context, the resolver will fall back to using the operator types provided in the constructor.
        /// </summary>
        public const string ContextOperatorTypesKey = "OperatorTypes";
        /// <summary>
        /// The key used in the context dictionary to store the reference resolver that the comparator can use to resolve references.
        /// The value associated with this key should be an instance of <see cref="IReferenceResolver"/> that can resolve references encountered during the comparison process.
        /// </summary>
        public const string ContextReferenceResolverKey = "ReferenceResolver";
        /// <summary>
        /// The key used in the context dictionary to store the delegate that can convert <see cref="IConditionDef.Compare"/> strings to <see cref="IReference"/> instances.
        /// The value associated with this key should be a delegate of type <see cref="Func{IConditionDef, IReadOnlyDictionary{string, object}, string, IReferenceType}"/> that can convert <see cref="IConditionDef.Compare"/> strings to <see cref="IReference"/> instances.
        /// </summary>
        public const string CompareStringToReferenceKey = "CompareStringToReference";
        /// <summary>
        /// The key used in the context dictionary to store the delegate that can convert <see cref="IConditionDef.With"/> strings to <see cref="IOperator"/> instances or operator type keys.
        /// The value associated with this key should be a delegate of type <see cref="Func{IConditionDef, IReadOnlyDictionary{string, object}, string, IOperator}"/> that can convert <see cref="IConditionDef.With"/> strings to <see cref="IOperator"/> instances or operator type keys.
        /// </summary>
        public const string OperatorStringToOperatorKey = "OperatorStringToOperator";
        /// <summary>
        /// The key used in the context dictionary to store the delegate that can convert <see cref="IConditionDef.ToString"/> strings to <see cref="IReference"/> instances.
        /// The value associated with this key should be a delegate of type <see cref="Func{IConditionDef, IReadOnlyDictionary{string, object}, string, IReference}"/> that can convert <see cref="IConditionDef.ToString"/> strings to <see cref="IReference"/> instances.
        /// </summary>
        public const string ToStringToReferenceKey = "ToStringToReference";

        // Fields
        private readonly IReferenceResolver _referenceResolver;
        private readonly IReadOnlyDictionary<string, IOperatorType> _operatorTypes;
        private readonly Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IOperator> _operatorStringToOperator;
        private readonly Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference> _compareStringToReference;
        private readonly Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference> _toStringToReference;

        /// <inheritdoc cref="Comparator"/>
        /// <param name="referenceResolver">Used to resolve <see cref="IReference"/>(s) to their values to compare. Optional but will throw when <see cref="IReference"/> is encountered and no resolver is provided.</param>
        /// <param name="operatorTypes">A dictionary of operator types used for comparison.</param>
        /// <param name="compareStringToReference">Optional delegate used to convert <see cref="IConditionDef.Compare"/> strings to <see cref="IReference"/> instances.</param>
        /// <param name="operatorStringToOperator">Optional delegate used to convert <see cref="IConditionDef.With"/> strings to <see cref="IOperator"/> instances or operator type keys.</param>
        /// <param name="toStringToReference">Optional delegate used to convert <see cref="IConditionDef.ToString"/> strings to <see cref="IReference"/> instances.</param>
        public Comparator(IReferenceResolver referenceResolver, 
            IReadOnlyDictionary<string, IOperatorType> operatorTypes, 
            Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference> compareStringToReference = null, 
            Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IOperator> operatorStringToOperator = null,
            Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference> toStringToReference = null)
        {
            _referenceResolver = referenceResolver;
            _operatorTypes = operatorTypes;
            _compareStringToReference = compareStringToReference;
            _operatorStringToOperator = operatorStringToOperator;
            _toStringToReference = toStringToReference;
        }

        /// <inheritdoc/>
        public bool Compare(object input, IConditionDef condition, IReadOnlyDictionary<string, object> context)
        {
            condition = Guard.NotNull(condition, nameof(condition));
            context ??= NullDictionary<string, object>.Instance;

            var isGroupCondition = condition.Conditions != null && condition.Conditions.Count > 0;
            var isCondition = condition.With != null;
            bool groupResult = true;
            if (isGroupCondition)
            {
                groupResult = Compare(input, condition.Conditions, context);
            }
            if (!isCondition)
            {
                if (!isGroupCondition)
                {
                    throw new InvalidOperationException("Condition must have either 'With' or 'Conditions' defined.");
                }

                return groupResult;
            }

            if(isGroupCondition && condition.ConditionGroupIsOr && groupResult)
            {
                // Group is an OR and it already evaluated to true, so we can skip the current condition as it will be true anyway.
                return true;
            }
            else if(isGroupCondition && !condition.ConditionGroupIsOr && !groupResult)
            {
                // Group is an AND and it already evaluated to false, so we can skip the current condition as it will be false anyway.
                return false;
            }

            var compareValue = ResolveValue(input, condition, condition.Compare, _compareStringToReference, CompareStringToReferenceKey, context);
            var withValue = GetOperatorType(condition, condition.With, context, out var operatorArguments);
            var toValue = ResolveValue(input, condition, condition.To, _toStringToReference, ToStringToReferenceKey, context);

            var conditionResult = withValue.Compare(compareValue, toValue, operatorArguments, context);

            return isGroupCondition ? (condition.ConditionGroupIsOr ? groupResult || conditionResult : groupResult && conditionResult) : conditionResult;
        }
        /// <inheritdoc/>
        public bool Compare(object input, IReadOnlyList<IConditionDef> conditions, IReadOnlyDictionary<string, object> context)
        {
            conditions = Guard.NotNull(conditions, nameof(conditions));
            context ??= NullDictionary<string, object>.Instance;

            // Evaluate a linear chain where each condition's IsOr indicates
            // whether the current AND-group should end after this condition.
            // This gives standard expression semantics:
            // (A && B) || (C && D) || E
            bool currentAndGroup = true;
            bool hasAnyTerm = false;
            bool anyAndGroupPassed = false;
            for (int i = 0; i < conditions.Count; i++)
            {
                var subCondition = conditions[i];
                var subResult = Compare(input, subCondition, context);
                hasAnyTerm = true;
                currentAndGroup = currentAndGroup && subResult;

                var endsCurrentGroup = i == conditions.Count - 1 || subCondition.IsOr;
                if (endsCurrentGroup)
                {
                    if (currentAndGroup)
                    {
                        anyAndGroupPassed = true;
                        break;
                    }

                    currentAndGroup = true;
                }
            }

            return hasAnyTerm && anyAndGroupPassed;
        }
        private object ResolveValue(object input, IConditionDef condition, object obj, Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference> stringResolver, string stringResolverContextKey, IReadOnlyDictionary<string, object> context)
        {
            if(context.TryGetValue(stringResolverContextKey, out var resolverObj) && resolverObj is Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IReference> resolverFromContext)
            {
                stringResolver = resolverFromContext;
            }
            if (obj == null)
            {
                return null;
            }
            else if (obj is string stringObj && stringResolver != null)
            {
                obj = (object)stringResolver(condition, context, stringObj) ?? stringObj;
            }

            if (obj is IReference reference)
            {
                var resolver = GetReferenceResolver(context);
                if (resolver == null)
                {
                    throw new InvalidOperationException($"Cannot resolve reference of type '{reference.Type}' because no reference resolver was provided.");
                }
                if (!resolver.TryResolve(input, reference, context, out var resolved))
                {
                    throw new InvalidOperationException($"Failed to resolve reference of type '{reference.Type}' with value '{reference.Value}'.");
                }
                return resolved;
            }

            return obj;
        }

        private IReferenceResolver GetReferenceResolver(IReadOnlyDictionary<string, object> context)
        {
            if (context.TryGetValue(ContextReferenceResolverKey, out var resolverObj) && resolverObj is IReferenceResolver resolverFromContext)
            {
                return resolverFromContext;
            }
            return _referenceResolver;
        }

        private IOperatorType GetOperatorType(IConditionDef condition, object @operator, IReadOnlyDictionary<string, object> context, out IReadOnlyDictionary<string, object> operatorArguments)
        {
            operatorArguments = NullDictionary<string, object>.Instance;
            string operatorType;
            if(@operator is string operatorString)
            {
                operatorType = operatorString;
                if (context.TryGetValue(OperatorStringToOperatorKey, out var operatorStringToOperatorObj) && operatorStringToOperatorObj is Func<IConditionDef, IReadOnlyDictionary<string, object>, string, IOperator> operatorStringToOperatorFromContext)
                {
                    var operatorObj = operatorStringToOperatorFromContext(condition, context, operatorString);
                    if(operatorObj != null)
                    {
                        operatorType = operatorObj.Type;
                        operatorArguments = operatorObj.Arguments ?? NullDictionary<string, object>.Instance;
                    }
                }
                else if (_operatorStringToOperator != null)
                {
                    var operatorObj = _operatorStringToOperator(condition, context, operatorString);
                    if (operatorObj != null)
                    {
                        operatorType = operatorObj.Type;
                        operatorArguments = operatorObj.Arguments ?? NullDictionary<string, object>.Instance;
                    }
                }
            }
            else if (@operator is IOperator operatorObj)
            {
                operatorType = operatorObj.Type;
                operatorArguments = operatorObj.Arguments ?? NullDictionary<string, object>.Instance;
            }
            else
            {
                throw new InvalidOperationException($"Invalid operator: {@operator}. Must be either a string key or an IOperator instance.");
            }

            if (context.TryGetValue(ContextOperatorTypesKey, out var operatorTypesObj) && operatorTypesObj is IReadOnlyDictionary<string, IOperatorType> operatorTypesFromContext)
            {
                if (operatorTypesFromContext.TryGetValue(operatorType, out var operatorTypeFromContext))
                {
                    return operatorTypeFromContext;
                }
            }
            if (_operatorTypes != null && _operatorTypes.TryGetValue(operatorType, out var operatorTypeFromConstructor))
            {
                return operatorTypeFromConstructor;
            }
            throw new InvalidOperationException($"Operator type '{operatorType}' not found in context or constructor operator types.");
        }
    }
}
