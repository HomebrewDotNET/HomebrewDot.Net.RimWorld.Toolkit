using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Collecting.Models;
using HomebrewDot.Net.RimWorld.Comparing;
using HomebrewDot.Net.RimWorld.Generic.Models;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;

namespace HomebrewDot.Net.RimWorld.Collecting.Components
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
        /// <summary>
        /// The key used in the context dictionary to retrieve the current item being compared. 
        /// Used to pass the object being compared to the underlying conditions.
        /// </summary>
        public const string ObjectKey = "Item";

        // Fields
        private readonly IComparator _comparator;

        /// <inheritdoc cref="CollectionComparator"/>
        /// <param name="comparator">Used to compare conditions within the collection.</param>
        public CollectionComparator(IComparator comparator)
        {
            _comparator = Guard.NotNull(comparator, nameof(comparator));
        }

        /// <inheritdoc/>
        public bool Matches(ICollectionDef collection, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, Dictionary<string, object> context)
        {
            collection = Guard.NotNull(collection, nameof(collection));
            obj = Guard.NotNull(obj, nameof(obj));
            collections ??= NullDictionary<string, ICollectionDef>.Instance;
            context ??= new Dictionary<string, object>();
            context[ObjectKey] = obj;

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
                conditionsMet = comparator.Compare(collection.Conditions, context);
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

        private IComparator GetComparator(Dictionary<string, object> context)
        {
            if (context.TryGetValue(ContextComparatorKey, out var comparatorObj) && comparatorObj is IComparator comparator)
            {
                return comparator;
            }
            return _comparator;
        }

        private bool MatchesCollections(IReadOnlyList<ICollectionConditionDef> collectionConditions, object obj, IReadOnlyDictionary<string, ICollectionDef> collections, Dictionary<string, object> context)
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
