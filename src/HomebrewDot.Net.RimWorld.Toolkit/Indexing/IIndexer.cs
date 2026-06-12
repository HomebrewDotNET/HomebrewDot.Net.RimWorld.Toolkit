using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Components;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Used to manage <see cref="IIndexed{T}.Metadata"/> during insertion.
    /// </summary>
    public interface IIndexer
    {
        /// <summary>
        /// Used to setup the indexer.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Fill metadata on <paramref name="indexed"/>.
        /// </summary>
        /// <param name="database">The database to index.</param>
        /// <param name="insertMetadata">The metadata associated with the data being inserted.</param>
        /// <param name="indexed">The indexed data.</param>
        void Index(IDatabase database, IReadOnlyDictionary<string, object> insertMetadata, IWriteableIndexed<object> indexed);
    }
}
