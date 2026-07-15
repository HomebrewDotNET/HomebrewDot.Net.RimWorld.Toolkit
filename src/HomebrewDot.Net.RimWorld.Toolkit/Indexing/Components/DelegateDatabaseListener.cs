using HomebrewDot.Net.Rimworld.Indexing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing.Components
{
    /// <summary>
    /// A <see cref="IDatabaseListener{T}"/> that uses delegates.
    /// </summary>
    public class DelegateDatabaseListener<T> : IDatabaseListener<T> where T : class
    {
        // State
        public Action<IIndexed<T>, IndexMetadata, IDatabase> onDeleting;
        public Action<IIndexed<T>, IndexMetadata, IDatabase> onDeleted;
        public Action<IIndexed<T>, IndexMetadata, IDatabase> onUpserted;
        public Action<IWriteableIndexed<T>, IndexMetadata, IDatabase> onUpserting;


        /// <inheritdoc/>
        public void OnDeleted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onDeleted?.Invoke(indexed, metadata, database);
        }

        public void OnDeleting(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onDeleting?.Invoke(indexed, metadata, database);
        }

        /// <inheritdoc/>
        public void OnUpserted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onUpserted?.Invoke(indexed, metadata, database);
        }
        /// <inheritdoc/>
        public void OnUpserting(IWriteableIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onUpserting?.Invoke(indexed, metadata, database);
        }
    }
}
