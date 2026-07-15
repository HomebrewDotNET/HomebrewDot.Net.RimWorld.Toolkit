using HomebrewDot.Net.Rimworld.Indexing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// A <see cref="ITableListener{T}"/> that uses delegates.
    /// </summary>
    public class DelegateTableListener<T> : ITableListener<T> where T : class
    {
        // State
        public Action<IIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onDeleting;
        public Action<IIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onDeleted;
        public Action<IIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onUpserted;
        public Action<IWriteableIndexed<T>, IndexMetadata, IReadOnlyTable<T>> onUpserting;

        /// <inheritdoc/>
        public void OnDeleting(IIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onDeleting?.Invoke(indexed, metadata, table);
        }
        /// <inheritdoc/>
        public void OnDeleted(IIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onDeleted?.Invoke(indexed, metadata, table);
        }
        /// <inheritdoc/>
        public void OnUpserted(IIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onUpserted?.Invoke(indexed, metadata, table);
        }
        /// <inheritdoc/>
        public void OnUpserting(IWriteableIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onUpserting?.Invoke(indexed, metadata, table);
        }
    }
}
