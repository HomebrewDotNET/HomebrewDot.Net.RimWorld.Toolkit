using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Components;
using HomebrewDot.Net.Rimworld.Indexing.Models;

namespace HomebrewDot.Net.Rimworld.Indexing
{
    /// <summary>
    /// Used to manage <see cref="IIndexed{T}.Metadata"/> during upsertion.
    /// </summary>
    public interface IIndexer<T> : IDatabaseListener<T> where T : class
    {
        /// <summary>
        /// Used to setup the indexer.
        /// </summary>
        void Initialize();
    }
}
