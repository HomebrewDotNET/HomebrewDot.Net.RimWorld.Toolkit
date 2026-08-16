using HomebrewDot.Net.Rimworld.Indexing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Indexing.Models.Delegates;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// A <see cref="ITableListener{T}"/> that uses delegates.
    /// </summary>
    public class DelegateTableListener<T> : ITableListener<T> where T : class
    {
        // State
        public OnTableDeleting<T> onDeleting;
        public OnTableDeleted<T> onDeleted;
        public OnTableInserted<T> onUpserted;
        public OnTableInserting<T> onUpserting;

        /// <inheritdoc/>
        public void OnDeleting(IIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onDeleting?.Invoke(indexed, ref metadata, table);
        }
        /// <inheritdoc/>
        public void OnDeleted(IIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onDeleted?.Invoke(indexed, ref metadata, table);
        }
        /// <inheritdoc/>
        public void OnUpserted(IIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onUpserted?.Invoke(indexed, ref metadata, table);
        }
        /// <inheritdoc/>
        public void OnUpserting(IWriteableIndexed<T> indexed, ref IndexMetadata metadata, IReadOnlyTable<T> table)
        {
            onUpserting?.Invoke(indexed, ref metadata, table);
        }
    }
}
