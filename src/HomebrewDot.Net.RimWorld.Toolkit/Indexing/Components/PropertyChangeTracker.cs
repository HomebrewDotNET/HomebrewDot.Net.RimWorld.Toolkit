using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// Keeps track of changes to properties of objects of type T by comparing the current value of the property with the previous value stored in an <see cref="IIndexed{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyChangeTracker<T, TProperty> : IChangeTracker<T>, IDisposable where T : class
    {
        // Fields
        private readonly Func<T, TProperty> _getProperty;
        private readonly IndexMetadataKey<TProperty> _metadataKey;
        private readonly IComparer<TProperty> _comparer;

        /// <inheritdoc cref="PropertyChangeTracker{T, TProperty}"/>
        /// <param name="getProperty">Delegate used to get the property value.</param>
        /// <param name="metadataKey">Key used to store the previous value in the metadata.</param>
        /// <param name="comparer">Optional comparer used to compare property values. If not provided, the default equality comparer will be used.</param>
        public PropertyChangeTracker(Func<T, TProperty> getProperty, IndexMetadataKey<TProperty> metadataKey, IComparer<TProperty> comparer = null)
        {
            _getProperty = Guard.NotNull(getProperty, nameof(getProperty));
            _metadataKey = Guard.NotNull(metadataKey, nameof(metadataKey));
            _comparer = comparer;
        }

        /// <inheritdoc/>
        public bool HasChanged(T current, IIndexed<T> previous, ref IndexMetadata metadata)
        {
            current = Guard.NotNull(current, nameof(current));

            metadata.PersistKey(_metadataKey);
            bool changed = false;
            object previousValue = null;
            if (previous is null)
            {
                changed = true;
            }
            else if (!previous.Metadata.TryGetValue(_metadataKey.Name, out previousValue))
            {
                changed = true;
            }

            var newValue = _getProperty(current);
            metadata.Set(_metadataKey, newValue);
            if(changed)
            {
                return true;
            }
            if (previousValue is null && newValue is null)
            {
                return false;
            }
            if (_comparer != null)
            {
                return _comparer.Compare((TProperty)previousValue, newValue) != 0;
            }
            return !EqualityComparer<TProperty>.Default.Equals((TProperty)previousValue, newValue);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
