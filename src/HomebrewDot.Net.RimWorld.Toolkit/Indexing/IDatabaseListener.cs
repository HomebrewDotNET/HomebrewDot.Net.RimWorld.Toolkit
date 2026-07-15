using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Models;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Listens to changes on entities of type <typeparamref name="T"/>.
    /// </summary>
    public interface IDatabaseListener<in T> where T : class
    {
        /// <summary>
        /// Raised when an entity is being inserted or updated.
        /// </summary>
        /// <param name="indexed">The instance being added or updated</param>
        /// <param name="metadata">The current metadata that can be enriched</param>
        /// <param name="database">The database <paramref name="indexed"/> is being raised in</param>
        void OnUpserting(IWriteableIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database);
        /// <summary>
        /// Raised when <paramref name="indexed"/> was either inserted or updated.
        /// </summary>
        /// <param name="indexed">The instance thas added or updated</param>
        /// <param name="metadata">The enriched metadata that was used during upsertion for <paramref name="indexed"/></param>
        /// <param name="database">The database <paramref name="indexed"/> was inserted to</param>
        void OnUpserted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database);
        /// <summary>
        /// Raised when <paramref name="indexed"/> is being deleted for <paramref name="database"/>.
        /// </summary>
        /// <param name="indexed">The instance that was deleted</param>
        /// <param name="metadata">The enriched metadata that was used during deletion for <paramref name="indexed"/></param>
        /// <param name="database">The database <paramref name="indexed"/> was inserted to</param>
        void OnDeleting(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database);
        /// <summary>
        /// Raised when <paramref name="indexed"/> was deleted for <paramref name="database"/>.
        /// </summary>
        /// <param name="indexed">The instance that was deleted</param>
        /// <param name="metadata">The enriched metadata that was used during deletion for <paramref name="indexed"/></param>
        /// <param name="database">The database <paramref name="indexed"/> was inserted to</param>
        void OnDeleted(IIndexed<T> indexed, ref IndexMetadata metadata, IDatabase database);
    }
}
