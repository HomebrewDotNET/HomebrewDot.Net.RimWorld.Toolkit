using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Generic.Models;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;
using LinqExpression = System.Linq.Expressions.Expression;

namespace HomebrewDot.Net.Rimworld.Collecting.Components
{
    /// <summary>
    /// Component responsible for comparing an object to a collection definition, including its conditions and referenced sub-collections.
    /// </summary>
    public class CollectionComparator : ICollectionComparator
    {
        // Constants
        /// <summary>
        /// The key used in the context dictionary to retrieve the comparator instance for this collection.
        /// Can be used to overwrite the global one for the current collection.
        /// </summary>
        public const string ContextComparatorKey = "Comparator";

        // Fields
        private readonly IComparator _comparator;
        private readonly Dictionary<string, Func<object, IReadOnlyDictionary<string, object>, bool>> _compiledCollectionExpressionsCache = new Dictionary<string, Func<object, IReadOnlyDictionary<string, object>, bool>>();

        /// <inheritdoc cref="CollectionComparator"/>
        /// <param name="comparator">Used to compare conditions within the collection.</param>
        public CollectionComparator(IComparator comparator)
        {
            _comparator = Guard.NotNull(comparator, nameof(comparator));
        }

        /// <inheritdoc/>
        public bool Matches(ICollectionDef collection, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            collection = Guard.NotNull(collection, nameof(collection));
            obj = Guard.NotNull(obj, nameof(obj));
            collections ??= NullDictionary<string, ICollectionDef>.Instance;
            context ??= new Dictionary<string, object>();

            if(collection is ICacheable cacheable)
            {
                var cacheKey = cacheable.GetCacheKey();
                if(cacheKey is not null)
                {
                    var fullCacheKey = $"{obj?.GetType()?.FullName ?? "NULL"}:{cacheKey}";
                    if (_compiledCollectionExpressionsCache.TryGetValue(fullCacheKey, out var cachedExpression))
                    {
                        return cachedExpression(obj, context);
                    }
                    var inputParameter = LinqExpression.Parameter(typeof(object), "input");
                    var contextParameter = LinqExpression.Parameter(typeof(IReadOnlyDictionary<string, object>), "context");
                    var comparator = GetComparator(context);
                    var expression = Compile(inputParameter, contextParameter, comparator, collection, obj, collections, context);
                    var lambda = LinqExpression.Lambda<Func<object, IReadOnlyDictionary<string, object>, bool>>(expression, inputParameter, contextParameter);
                    var compiled = lambda.Compile();
                    _compiledCollectionExpressionsCache[fullCacheKey] = compiled;
                    return compiled(obj, context);
                }
            }

            bool hasExclusionCollections = collection.Exclusions != null && collection.Exclusions.Count > 0;
            if (hasExclusionCollections)
            {
                if (MatchesCollections(collection.Exclusions, obj, collections, context))
                {
                    return false;
                }
            }

            bool hasConditions = collection.Conditions != null && collection.Conditions.Count > 0;
            bool conditionsMet = true;
            if (hasConditions)
            {
                var comparator = GetComparator(context);
                conditionsMet = comparator.Compare(obj, collection.Conditions, context);
            }
            bool hasInclusionCollections = collection.Inclusions != null && collection.Inclusions.Count > 0;
            if(hasInclusionCollections)
            {
                if(!collection.InclusionsAreOr && !conditionsMet)
                {
                    // If there are inclusion collections and the conditions are not met, we can skip checking the inclusion collections as they will be false anyway.
                    return false;
                }
                var inclusionsMet = MatchesCollections(collection.Inclusions, obj, collections, context);

                conditionsMet = hasConditions ? (collection.InclusionsAreOr ? conditionsMet || inclusionsMet : conditionsMet && inclusionsMet) : inclusionsMet;
            }

            return conditionsMet;
        }
        /// <inheritdoc/>
        public IEnumerable<(object Object, bool Matches)> Matches(ICollectionDef collection, IEnumerable<object> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            collection = Guard.NotNull(collection, nameof(collection));
            collections ??= NullDictionary<string, ICollectionDef>.Instance;
            context ??= new Dictionary<string, object>();

            if (collection is ICacheable cacheable)
            {
                var cacheKey = cacheable.GetCacheKey();
                if (cacheKey is not null)
                {
                    var tempCache = new Dictionary<string, Func<object, IReadOnlyDictionary<string, object>, bool>>();

                    foreach (var obj in objects)
                    {
                        var fullCacheKey = $"{obj?.GetType()?.FullName ?? "NULL"}:{cacheKey}";
                        if(!tempCache.TryGetValue(fullCacheKey, out var cachedExpression))
                        {
                            var inputParameter = LinqExpression.Parameter(typeof(object), "input");
                            var contextParameter = LinqExpression.Parameter(typeof(IReadOnlyDictionary<string, object>), "context");
                            var comparator = GetComparator(context);
                            var expression = Compile(inputParameter, contextParameter, comparator, collection, obj, collections, context);
                            var lambda = LinqExpression.Lambda<Func<object, IReadOnlyDictionary<string, object>, bool>>(expression, inputParameter, contextParameter);
                            cachedExpression = lambda.Compile();
                            tempCache[fullCacheKey] = cachedExpression;
                        }

                        yield return (obj, cachedExpression(obj, context));
                    }
                }
            }

            foreach(var obj in objects)
            {
                yield return (obj, Matches(collection, obj, collections, context));
            }
        }

        private IComparator GetComparator(IReadOnlyDictionary<string, object> context)
        {
            if (context.TryGetValue(ContextComparatorKey, out var comparatorObj) && comparatorObj is IComparator comparator)
            {
                return comparator;
            }
            return _comparator;
        }

        private LinqExpression Compile(ParameterExpression inputParameter, ParameterExpression contextParameter, IComparator comparator, ICollectionDef collection, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            bool hasExclusionCollections = collection.Exclusions != null && collection.Exclusions.Count > 0;
            LinqExpression isExcluded = null;
            if (hasExclusionCollections)
            {
                var exclusionExpressions = collection.Exclusions.Select(exclusion => (Expression: Compile(inputParameter, contextParameter, comparator, collections.TryGetValue(exclusion.Name, out var collection) ? collection : throw new InvalidOperationException($"Collection '{exclusion.Name}' not found."), obj, collections, context), IsOr: exclusion.IsOr)).ToArray();
                isExcluded = exclusionExpressions[0].Expression;
                for(int i = 1; i < exclusionExpressions.Length; i++)
                {
                    var exclusionExpression = exclusionExpressions[i];
                    isExcluded = exclusionExpressions[i-1].IsOr ? LinqExpression.OrElse(isExcluded, exclusionExpression.Expression) : LinqExpression.AndAlso(isExcluded, exclusionExpression.Expression);
                }
            }

            bool hasInclusionCollections = collection.Inclusions != null && collection.Inclusions.Count > 0;
            LinqExpression isIncluded = null;
            if (hasInclusionCollections) {
                var inclusionExpressions = collection.Inclusions.Select(inclusion => (Expression: Compile(inputParameter, contextParameter, comparator, collections.TryGetValue(inclusion.Name, out var collection) ? collection : throw new InvalidOperationException($"Collection '{inclusion.Name}' not found."), obj, collections, context), IsOr: inclusion.IsOr)).ToArray();
                isIncluded = inclusionExpressions[0].Expression;
                for (int i = 1; i < inclusionExpressions.Length; i++)
                {
                    var inclusionExpression = inclusionExpressions[i];
                    isIncluded = inclusionExpressions[i - 1].IsOr ? LinqExpression.OrElse(isIncluded, inclusionExpression.Expression) : LinqExpression.AndAlso(isIncluded, inclusionExpression.Expression);
                }
            }

            bool hasConditions = collection.Conditions != null && collection.Conditions.Count > 0;
            LinqExpression conditionsMet = null;
            if (hasConditions)
            {
                if(comparator is IComparatorCompiler expressionCompiler)
                {
                    conditionsMet = expressionCompiler.Compile(inputParameter, collection.CombinedConditions, contextParameter, context);
                }
                else
                {
                    var method = Toolkit.Helpers.Expression.GetMethod<IComparator>(x => x.Compare(null, (IConditionDef)null, null));
                    conditionsMet = LinqExpression.Call(
                        LinqExpression.Constant(comparator),
                        method,
                        inputParameter,
                        LinqExpression.Constant(collection.CombinedConditions),
                        contextParameter
                    );
                }
            }

            LinqExpression itemIsMatch = conditionsMet;

            if (itemIsMatch is null)
            {
                itemIsMatch = isIncluded;
            }
            else if(isIncluded is not null)
            {
                itemIsMatch = collection.InclusionsAreOr ? LinqExpression.OrElse(itemIsMatch, isIncluded) : LinqExpression.AndAlso(itemIsMatch, isIncluded);
            }

            if (itemIsMatch is null)
            {
                return isExcluded is null ? LinqExpression.Constant(false) : LinqExpression.Not(isExcluded);
            }
            return isExcluded is null ? itemIsMatch : LinqExpression.AndAlso(itemIsMatch, LinqExpression.Not(isExcluded));
        }

        private bool MatchesCollections(IReadOnlyList<ICollectionConditionDef> collectionConditions, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            // Evaluate referenced collections with the same semantics as condition chains:
            // contiguous terms form an AND-group, and IsOr ends the current group.
            // Final result is true when any AND-group evaluates to true.
            bool currentAndGroup = true;
            bool hasAnyTerm = false;
            bool anyAndGroupPassed = false;
            for (int i = 0; i < collectionConditions.Count; i++)
            {
                var collectionCondition = collectionConditions[i];
                if (!collections.TryGetValue(collectionCondition.Name, out var subCollection))
                {
                    throw new InvalidOperationException($"Collection '{collectionCondition.Name}' not found in collections dictionary.");
                }

                var matches = Matches(subCollection, obj, collections, context);
                if(collectionCondition.Inverted)
                {
                    matches = !matches;
				}
				hasAnyTerm = true;
                currentAndGroup = currentAndGroup && matches;

                var endsCurrentGroup = i == collectionConditions.Count - 1 || collectionCondition.IsOr;
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
    }
}
