using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Comparing.Models;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Collecting.Models
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

        public TReturn ExcludeFrom(Func<ICollectionConditionBuilder, ICollectionConditionBuilderChain<ICollectionConditionBuilder>> builder)
        {
            throw new NotImplementedException();
        }

        public TReturn IncludeFrom(Func<ICollectionConditionBuilder, ICollectionConditionBuilderChain<ICollectionConditionBuilder>> builder)
        {
            throw new NotImplementedException();
        }

        public TReturn OrIncludeFrom(Func<ICollectionConditionBuilder, ICollectionConditionBuilderChain<ICollectionConditionBuilder>> builder)
        {
            throw new NotImplementedException();
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
        /// <inheritdoc/>
        public override ICollectionBuilder Return => this;
    }

    /// <summary>
    /// Model for fluently building a collection condition as part of a <see cref="ICollectionDef"/>. This is the main implementation of <see cref="ICollectionConditionBuilder"/>, <see cref="ICollectionConditionBuilderAdditionalBuilder"/>, and <see cref="ICollectionConditionBuilderChain{T}"/>.
    /// </summary>
    public class CollectionConditionBuilder : ICollectionConditionBuilder, ICollectionConditionBuilderAdditionalBuilder, ICollectionConditionBuilderChain<ICollectionConditionBuilder>
    {
        // Fields
        private readonly List<CollectionConditionDef> _conditions = new List<CollectionConditionDef>();

        // State
        private string _currentPropertyPath;
        private string _currentCollectionName;
        private bool _currentIsInverted;

		/// <inheritdoc/>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ICollectionConditionBuilder And
        {
            get
            {
                if(_currentCollectionName is null)
                {
                    throw new InvalidOperationException("Cannot add condition when a collection name is missing");
                }
                else
                {
                    _conditions.Add(new CollectionConditionDef()
                    {
                        Name = _currentCollectionName,
                        By = _currentPropertyPath,
                        IsOr = false
                    });
                    _currentCollectionName = null;
                    _currentPropertyPath = null;
                    return this;
                }
            }
        }
        /// <inheritdoc/>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ICollectionConditionBuilder Or
        {
            get
            {
                if (_currentCollectionName is null)
                {
                    throw new InvalidOperationException("Cannot add condition when a collection name is missing");
                }
                else
                {
                    _conditions.Add(new CollectionConditionDef()
                    {
                        Name = _currentCollectionName,
                        By = _currentPropertyPath,
                        IsOr = true,
                        Inverted = _currentIsInverted
                    });
                    _currentCollectionName = null;
                    _currentPropertyPath = null;
                    return this;
                }
            }
        }
        /// <inheritdoc/>
        public ICollectionConditionBuilderChain<ICollectionConditionBuilder> By(string propertyPath)
        {
            propertyPath = Guard.NotNullOrEmpty(propertyPath, nameof(propertyPath));
            if(_currentCollectionName is null)
            {
                throw new InvalidOperationException("Cannot set property path when a collection name is missing");
            }
            else if(_currentPropertyPath is not null)
            {
                throw new InvalidOperationException("Cannot set property path when a property path is already set");
            }
            else
            {
                _currentPropertyPath = propertyPath;
            }
            return this;
        }
        /// <inheritdoc/>
        public ICollectionConditionBuilderAdditionalBuilder Collection(string name)
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            if(_currentCollectionName is not null)
            {
                throw new InvalidOperationException("Cannot set collection name when a collection name is already set");
            }
            return this;
        }
		/// <inheritdoc/>
		ICollectionConditionBuilderAdditionalBuilder ICollectionConditionBuilder.NotCollection(string name)
        {
            name = Guard.NotNullOrEmpty(name, nameof(name));
            _currentIsInverted = true;
            return Collection(name);
		}
    }
}
