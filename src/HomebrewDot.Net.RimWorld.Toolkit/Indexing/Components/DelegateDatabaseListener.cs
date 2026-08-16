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
    /// A <see cref="IDatabaseListener{T}"/> that uses delegates.
    /// </summary>
    public class DelegateDatabaseListener<T> : IDatabaseListener<T> where T : class
    {
        // State
        public OnDatabaseDeleting onDeleting;
        public OnDatabaseDeleted onDeleted;
        public OnDatabaseInserted onUpserted;
        public OnDatabaseInserting onUpserting;


        /// <inheritdoc/>
        public void OnDeleted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onDeleted?.Invoke(indexed, ref metadata, database);
        }

        public void OnDeleting(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onDeleting?.Invoke(indexed, ref metadata, database);
        }

        /// <inheritdoc/>
        public void OnUpserted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onUpserted?.Invoke(indexed, ref metadata, database);
        }
        /// <inheritdoc/>
        public void OnUpserting(IWriteableIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database)
        {
            onUpserting?.Invoke(indexed, ref metadata, database);
        }
    }
}
