using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly string _metadataKey;
        private readonly IComparer<TProperty> _comparer;

        /// <inheritdoc cref="PropertyChangeTracker{T}"/>
        /// <param name="getProperty">Delegate used to get the property value.</param>
        /// <param name="metadataKey">Key used to store the previous value in the metadata.</param>
        /// <param name="comparer">Optional comparer used to compare property values. If not provided, the default equality comparer will be used.</param>
        public PropertyChangeTracker(Func<T, TProperty> getProperty, string metadataKey, IComparer<TProperty> comparer = null)
        {
            _getProperty = Guard.NotNull(getProperty, nameof(getProperty));
            _metadataKey = Guard.NotNullOrWhitespace(metadataKey, nameof(metadataKey));
            _comparer = comparer;
            Toolkit.Indexing.ConfigureSchema += ConfigureSchema;
        }

        /// <inheritdoc/>
        public bool HasChanged(T current, IIndexed<T> previous, IIndexed<T> snapshot)
        {
            current = Guard.NotNull(current, nameof(current));
            previous = Guard.NotNull(previous, nameof(previous));

            if(!previous.Metadata.TryGetValue(_metadataKey, out var previousValue))
            {
                return true;
            }

            var newValue = _getProperty(current);
            if(previousValue is null && newValue is null)
            {
                return false;
            }
            if (_comparer != null)
            {
                return _comparer.Compare((TProperty)previousValue, newValue) != 0;
            }
            return !EqualityComparer<TProperty>.Default.Equals((TProperty)previousValue, newValue);
        }

        private void ConfigureSchema(IDatabaseSchemaBuilder builder)
        {
            builder.OnInserting((d, m, i) =>
            {
                if (i.Value is T obj)
                {
                    var value = _getProperty(obj);
                    i.Set(_metadataKey, value);
                }
            });
        }


        /// <inheritdoc/>
        public void Dispose()
        {
            Toolkit.Indexing.ConfigureSchema -= ConfigureSchema;
        }
    }
}
