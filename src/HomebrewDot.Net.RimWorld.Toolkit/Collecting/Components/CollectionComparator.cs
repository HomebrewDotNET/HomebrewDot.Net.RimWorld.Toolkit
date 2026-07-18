using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Collecting.Models;
using HomebrewDot.Net.Rimworld.Comparing;
using HomebrewDot.Net.Rimworld.Generic;
using HomebrewDot.Net.Rimworld.Generic.Models;
using static HomebrewDot.Net.Rimworld.Toolkit;
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
        private readonly static Dictionary<Type, Func<ICollectionDef, object, IReadOnlyDictionary<string, ICollectionDef>, IReadOnlyDictionary<string, object>, bool>> _cacheWarmers = new Dictionary<Type, Func<ICollectionDef, object, IReadOnlyDictionary<string, ICollectionDef>, IReadOnlyDictionary<string, object>, bool>>();
        private readonly static MethodInfo _matchMethod = Helpers.Expression.GetMethod<CollectionComparator>(x => x.Matches(default, default(object), default, default)).GetGenericMethodDefinition();
        private readonly static Dictionary<string, Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, bool>>> _compiledCollectionExpressionsCache = new Dictionary<string, Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, bool>>>();
        private readonly static Dictionary<string, Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, bool>>> _compiledCollectionWithSubExpressionsCache = new Dictionary<string, Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, bool>>>();


        // Fields
        private readonly IComparator _comparator;
        

        /// <inheritdoc cref="CollectionComparator"/>
        /// <param name="comparator">Used to compare conditions within the collection.</param>
        public CollectionComparator(IComparator comparator)
        {
            _comparator = Guard.NotNull(comparator, nameof(comparator));
        }

        /// <summary>
        /// Compiles expressions trees for all <paramref name="collections"/> and <paramref name="types"/> combinations.
        /// </summary>
        /// <param name="collections">The collections to warmup the expression tries for</param>
        /// <param name="types">All the types to warmup the expression tries for</param>
        public void WarmupCache(IReadOnlyDictionary<string, ICollectionDef> collections, IEnumerable<Type> types)
        {
            var collectors = Toolkit.Collecting.GetAllCollectors();
            var typeCache = new Dictionary<Type, Type[]>();
            foreach (var collection in collections)
            {
                var collectionDef = collection.Value;
                var collectionName = collection.Key;
                IEnumerable<Type> collectionTypes;
                try
                {
                    if(collectors.TryGetValue(collectionName, out var collector))
                    {
                        var acceptedTypes = Toolkit.Helpers.GetGenericTypes(collector.GetType(), typeof(ICollector<Verse.Def>));
                        foreach(var acceptedType in acceptedTypes)
                        {
                            if (!typeCache.ContainsKey(acceptedType))
                            {
                                var scannedTypes = Helpers.ScanForTypes(x =>
                                {
                                    return x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition && acceptedType.IsAssignableFrom(x);
                                });
                                typeCache[acceptedType] = scannedTypes.ToArray();
                            }
                        }
                        collectionTypes = acceptedTypes.SelectMany(x => typeCache[x]);
                    }
                    else
                    {
                        collectionTypes = types;
                    }

                        
                }
                catch
                {
                    continue;
                }

                foreach (var type in collectionTypes)
                {
                    try
                    {
                        if (!_cacheWarmers.TryGetValue(type, out var cacheWarmer))
                        {
                            var targetMethod = _matchMethod.MakeGenericMethod(type);
                            var inputObject = LinqExpression.Parameter(typeof(object), "obj");
                            var inputCollection = LinqExpression.Parameter(typeof(ICollectionDef), "collection");
                            var inputCollections = LinqExpression.Parameter(typeof(IReadOnlyDictionary<string, ICollectionDef>), "collections");
                            var inputContext = LinqExpression.Parameter(typeof(IReadOnlyDictionary<string, object>), "context");
                            var typedObject = LinqExpression.Parameter(type, "input");
                            var assignTypedObject = LinqExpression.Assign(typedObject, LinqExpression.Convert(inputObject, type));
                            var callMethod = LinqExpression.Call(LinqExpression.Constant(this), targetMethod, inputCollection, typedObject, inputCollections, inputContext);
                            var block = LinqExpression.Block([typedObject], assignTypedObject, callMethod);
                            var lambda = LinqExpression.Lambda<Func<ICollectionDef, object, IReadOnlyDictionary<string, ICollectionDef>, IReadOnlyDictionary<string, object>, bool>>(block, inputCollection, inputObject, inputCollections, inputContext);
                            cacheWarmer = lambda.Compile();
                            _cacheWarmers[type] = cacheWarmer;
                        }
                        var instance = FormatterServices.GetUninitializedObject(type); 
                        cacheWarmer(collectionDef, instance, collections, NullDictionary<string, object>.Instance);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            
        }

        /// <summary>
        /// Clear internal expression tree caches.
        /// </summary>
        public void ClearCache()
        {
            _compiledCollectionWithSubExpressionsCache.Clear();
        }

        /// <inheritdoc/>
        public bool Matches<T>(ICollectionDef collection, T obj, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            collection = Guard.NotNull(collection, nameof(collection));
            obj = Guard.NotNull(obj, nameof(obj));
            collections ??= NullDictionary<string, ICollectionDef>.Instance;
            context ??= NullDictionary<string, object>.Instance;

            if (collection is ICacheable cacheable)
            {
                var cacheKey = cacheable.GetCacheKey();
                if (cacheKey is not null)
                {
                    var cache = _compiledCollectionExpressionsCache;
                    if (collection.HasSubCollections())
                    {
                        cache = _compiledCollectionWithSubExpressionsCache;
                    }
                    if (!cache.TryGetValue(cacheKey, out var typeCache))
                    {
                        typeCache = new Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, bool>>();
                        cache[cacheKey] = typeCache;
                    }
                    var fullCacheKey = obj?.GetType() ?? typeof(object);
                    if (typeCache.TryGetValue(fullCacheKey, out var cachedExpression))
                    {
                        return cachedExpression(obj, context);
                    }
                    var stopwatch = Stopwatch.StartNew();
                    var inputParameter = LinqExpression.Parameter(typeof(object), "input");
                    var contextParameter = LinqExpression.Parameter(typeof(IReadOnlyDictionary<string, object>), "context");
                    var comparator = GetComparator(context);
                    var expression = Compile(inputParameter, obj, contextParameter, comparator, collection, collections, context);
                    var lambda = LinqExpression.Lambda<Func<object, IReadOnlyDictionary<string, object>, bool>>(expression, inputParameter, contextParameter);
                    var compiled = lambda.Compile();
                    stopwatch.Stop();
                    if (Logging.IsPerformanceEnabled && !_cacheWarmers.ContainsKey(fullCacheKey)) Logging.LogPerformance($"Compiled collection '{cacheKey}' for type '{fullCacheKey}' in {stopwatch.ElapsedMilliseconds}ms.");
                    typeCache[fullCacheKey] = compiled;
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
            if (hasInclusionCollections)
            {
                if (!collection.InclusionsAreOr && !conditionsMet)
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
        public IEnumerable<(T Object, bool Matches)> Matches<T>(ICollectionDef collection, IEnumerable<T> objects, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            collection = Guard.NotNull(collection, nameof(collection));
            collections ??= NullDictionary<string, ICollectionDef>.Instance;
            context ??= new Dictionary<string, object>();

            if (collection is ICacheable cacheable)
            {
                var cacheKey = cacheable.GetCacheKey();
                if (cacheKey is not null)
                {
                    var cache = _compiledCollectionExpressionsCache;
                    if (collection.HasSubCollections())
                    {
                        cache = _compiledCollectionWithSubExpressionsCache;
                    }
                    if (!cache.TryGetValue(cacheKey, out var typeCache))
                    {
                        typeCache = new Dictionary<Type, Func<object, IReadOnlyDictionary<string, object>, bool>>();
                        cache[cacheKey] = typeCache;
                    }

                    foreach (var obj in objects)
                    {
                        var fullCacheKey = obj?.GetType() ?? typeof(object);
                        if (typeCache.TryGetValue(fullCacheKey, out var cachedExpression))
                        {
                            yield return (obj, cachedExpression(obj, context));
                            continue;
                        }
                        var stopwatch = Stopwatch.StartNew();
                        var inputParameter = LinqExpression.Parameter(typeof(object), "input");
                        var contextParameter = LinqExpression.Parameter(typeof(IReadOnlyDictionary<string, object>), "context");
                        var comparator = GetComparator(context);
                        var expression = Compile(inputParameter, obj, contextParameter, comparator, collection, collections, context);
                        var lambda = LinqExpression.Lambda<Func<object, IReadOnlyDictionary<string, object>, bool>>(expression, inputParameter, contextParameter);
                        cachedExpression = lambda.Compile();
                        if (Logging.IsPerformanceEnabled && !_cacheWarmers.ContainsKey(fullCacheKey)) Logging.LogPerformance($"Compiled collection '{cacheKey}' for type '{fullCacheKey}' in {stopwatch.ElapsedMilliseconds}ms.");
                        typeCache[fullCacheKey] = cachedExpression;

                        yield return (obj, cachedExpression(obj, context));
                    }
                }
                yield break;
            }

            foreach (var obj in objects)
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

        private LinqExpression Compile(ParameterExpression inputParameter, object input, ParameterExpression contextParameter, IComparator comparator, ICollectionDef collection, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            bool hasExclusionCollections = collection.Exclusions != null && collection.Exclusions.Count > 0;
            LinqExpression isExcluded = null;
            if (hasExclusionCollections)
            {
                var exclusionExpressions = collection.Exclusions.Select(exclusion => (Expression: CompileCollectionRef(inputParameter, exclusion.By, input, contextParameter, comparator, collections.TryGetValue(exclusion.Name, out var collection) ? collection : throw new InvalidOperationException($"Collection '{exclusion.Name}' not found."), collections, context), IsOr: exclusion.IsOr)).ToArray();
                isExcluded = exclusionExpressions[0].Expression;
                for (int i = 1; i < exclusionExpressions.Length; i++)
                {
                    var exclusionExpression = exclusionExpressions[i];
                    isExcluded = exclusionExpressions[i - 1].IsOr ? LinqExpression.OrElse(isExcluded, exclusionExpression.Expression) : LinqExpression.AndAlso(isExcluded, exclusionExpression.Expression);
                }
            }

            bool hasInclusionCollections = collection.Inclusions != null && collection.Inclusions.Count > 0;
            LinqExpression isIncluded = null;
            if (hasInclusionCollections)
            {
                var inclusionExpressions = collection.Inclusions.Select(inclusion => (Expression: CompileCollectionRef(inputParameter, inclusion.By, input, contextParameter, comparator, collections.TryGetValue(inclusion.Name, out var collection) ? collection : throw new InvalidOperationException($"Collection '{inclusion.Name}' not found."), collections, context), IsOr: inclusion.IsOr)).ToArray();
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
                if (comparator is IComparatorCompiler expressionCompiler)
                {
                    conditionsMet = expressionCompiler.Compile(inputParameter, input, collection.CombinedConditions, contextParameter, context);
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
            else if (isIncluded is not null)
            {
                itemIsMatch = collection.InclusionsAreOr ? LinqExpression.OrElse(itemIsMatch, isIncluded) : LinqExpression.AndAlso(itemIsMatch, isIncluded);
            }

            if (itemIsMatch is null)
            {
                return isExcluded is null ? LinqExpression.Constant(false) : LinqExpression.Not(isExcluded);
            }
            return isExcluded is null ? itemIsMatch : LinqExpression.AndAlso(itemIsMatch, LinqExpression.Not(isExcluded));
        }

        LinqExpression CompileCollectionRef(ParameterExpression inputParameter, string by, object input, ParameterExpression contextParameter, IComparator comparator, ICollectionDef collection, IReadOnlyDictionary<string, ICollectionDef> collections, IReadOnlyDictionary<string, object> context)
        {
            if(string.IsNullOrWhiteSpace(by))
            {
                return Compile(inputParameter, input, contextParameter, comparator, collection, collections, context);
            }

            var expectedType = Toolkit.Helpers.Traversing.TryWalkIndexedPath(input.GetType(), by);
            var byVariable = LinqExpression.Variable(expectedType, "byValue");
            var getter = Toolkit.Helpers.Traversing.GenerateFullGetter(inputParameter, input.GetType(), by);
            var assignBy = LinqExpression.Assign(byVariable, getter);
            var value = Toolkit.Helpers.Traversing.Traverse(input, by) ?? FormatterServices.GetUninitializedObject(expectedType);
            return LinqExpression.Block(new[] { byVariable }, assignBy, Compile(byVariable, value, contextParameter, comparator, collection, collections, context));
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
                if (collectionCondition.Inverted)
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
