using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Indexing.Models
{
    /// <summary>
    /// Contains delegate definitions used in the indexing system. These delegates are used to define callback methods that can be invoked during various stages of the indexing process, such as when inserting data into the database.
    /// </summary>
    public static class Delegates
    {
        /// <summary>
        /// Delegate invoked when an item is being inserted into the database.
        /// </summary>
        /// <param name="value">The item being inserted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="database">The database into which the item is being inserted.</param>
        public delegate void OnDatabaseInserting(IWriteableIndexed<object> value, ref IndexMetadata metadata, IDatabase database);

        /// <summary>
        /// Delegate invoked after an item has been inserted into the database.
        /// </summary>
        /// <param name="value">The item that was inserted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="database">The database into which the item was inserted.</param>
        public delegate void OnDatabaseInserted(IIndexed<object> value, ref IndexMetadata metadata, IDatabase database);

        /// <summary>
        /// Delegate invoked when an item is being deleted from the database.
        /// </summary>
        /// <param name="value">The item being deleted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="database">The database from which the item is being deleted.</param>
        public delegate void OnDatabaseDeleting(IIndexed<object> value, ref IndexMetadata metadata, IDatabase database);
        /// <summary>
        /// Delegate invoked after an item has been deleted from the database.
        /// </summary>
        /// <param name="value">The item that was deleted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="database">The database from which the item was deleted.</param>
        public delegate void OnDatabaseDeleted(IIndexed<object> value, ref IndexMetadata metadata, IDatabase database);
        /// <summary>
        /// Delegate invoked when an item is being inserted into a table.
        /// </summary>
        /// <typeparam name="T">The type of the item being inserted.</typeparam>
        /// <param name="value">The item being inserted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="table">The table into which the item is being inserted.</param>
        public delegate void OnTableInserting<T>(IWriteableIndexed<T> value, ref IndexMetadata metadata, IReadOnlyTable<T> table) where T : class;
        /// <summary>
        /// Delegate invoked after an item has been inserted into a table.
        /// </summary>
        /// <typeparam name="T">The type of the item that was inserted.</typeparam>
        /// <param name="value">The item that was inserted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="table">The table into which the item was inserted.</param>
        public delegate void OnTableInserted<T>(IIndexed<T> value, ref IndexMetadata metadata, IReadOnlyTable<T> table) where T : class;
        /// <summary>
        /// Delegate invoked when an item is being deleted from a table.
        /// </summary>
        /// <typeparam name="T">The type of the item being deleted.</typeparam>
        /// <param name="value">The item being deleted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="table">The table from which the item is being deleted.</param>
        public delegate void OnTableDeleting<T>(IIndexed<T> value, ref IndexMetadata metadata, IReadOnlyTable<T> table) where T : class;

        /// <summary>
        /// Delegate invoked after an item has been deleted from a table.
        /// </summary>
        /// <typeparam name="T">The type of the item that was deleted.</typeparam>
        /// <param name="value">The item that was deleted.</param>
        /// <param name="metadata">Metadata associated with the item.</param>
        /// <param name="table">The table from which the item was deleted.</param>
        public delegate void OnTableDeleted<T>(IIndexed<T> value, ref IndexMetadata metadata, IReadOnlyTable<T> table) where T : class;
    }
}
