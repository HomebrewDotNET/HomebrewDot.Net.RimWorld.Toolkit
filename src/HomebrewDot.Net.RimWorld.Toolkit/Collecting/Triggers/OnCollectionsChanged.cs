using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Collecting.Triggers
{
    /// <summary>
    /// Raised when a collection is added or removed.
    /// </summary>
    public class OnCollectionsChanged
    {
        /// <summary>
        /// The name of the collection that was added or removed.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The definition of the collection that was added or removed.
        /// </summary>
        public ICollectionDef Collection { get; }
        /// <summary>
        /// The collector for <see cref="Collection"/> when defined. Can be null if no collector was created for the collection.
        /// </summary>
        public ICollector Collector { get; }
        /// <summary>
        /// Whether the collection was added or removed. True if the collection was added, false if it was removed.
        /// </summary>
        public bool Added { get; }

        /// <inheritdoc cref="OnCollectionsChanged"/>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="collection"><inheritdoc cref="Collection"/></param>
        /// <param name="collector"><inheritdoc cref="Collector"/></param>
        /// <param name="added"><inheritdoc cref="Added"/></param>
        public OnCollectionsChanged(string name, ICollectionDef collection, ICollector collector, bool added)
        {
            Name = Guard.NotNullOrWhitespace(name, nameof(name));
            Collection = Guard.NotNull(collection, nameof(collection));
            Collector = collector;
            Added = added;
        }
    }
}
