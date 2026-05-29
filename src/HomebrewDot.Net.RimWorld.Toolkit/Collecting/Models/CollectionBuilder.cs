using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.RimWorld.Comparing.Models;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;

namespace HomebrewDot.Net.RimWorld.Collecting.Models
{
    /// <summary>
    /// Model for fluently building a <see cref="ICollectionDef"/> and optionally a <see cref="ICollector{T}"/> to maintain the collection. This is the main implementation of <see cref="ICollectionBuilder{TReturn}"/>.
    /// </summary>
    /// <typeparam name="TReturn">The type to return for the fluent syntax.</typeparam>
    public abstract class CollectionBuilder<TReturn> : ConditionBuilder<TReturn>, ICollectionBuilder<TReturn> where TReturn : ICollectionBuilder<TReturn>
    {
        // State
        private Func<ICollectionDef, ICollector> _collectorFactory;

        // Properties
        public ICollectionDef Collection { 
            get
            {
                return new CollectionDef() { Conditions = Conditions?.ToArray() };
            } 
        }

        public bool TryBuildCollector(ICollectionDef collectionDef, out ICollector collector)
        {
            collectionDef = Guard.NotNull(collectionDef, nameof(collectionDef));
            if (_collectorFactory is null)
            {
                collector = null;
                return false;
            }
            collector = _collectorFactory(collectionDef);
            return true;
        }

        /// <inheritdoc/>
        TReturn ICollectionBuilder<TReturn>.CollectWith(Func<ICollectionDef, ICollector> collectorFactory)
        {
            _collectorFactory = Guard.NotNull(collectorFactory, nameof(collectorFactory));
            return Return;
        }
        /// <inheritdoc/>
        TReturn ICollectionBuilder<TReturn>.CollectWith<T>(Func<ICollectionDef, ICollector<T>> collectorFactory)
        {
            _collectorFactory = Guard.NotNull(collectorFactory, nameof(collectorFactory));
            return Return;
        }
    }
    /// <summary>
    /// Model for fluently building a <see cref="ICollectionDef"/> and optionally a <see cref="ICollector{T}"/> to maintain the collection. This is the main implementation of <see cref="ICollectionBuilder"/>.
    /// </summary>
    public class CollectionBuilder : CollectionBuilder<ICollectionBuilder>, ICollectionBuilder
    {
        public override ICollectionBuilder Return => this;
    }
}
