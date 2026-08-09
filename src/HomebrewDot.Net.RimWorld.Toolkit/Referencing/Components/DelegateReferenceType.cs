using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// A reference type that uses a delegate to resolve references. This allows for custom resolution logic to be provided at runtime.
    /// </summary>
    public class DelegateReferenceType : IReferenceType
    {
        private readonly Func<object, object, IReadOnlyDictionary<string, object>, object> _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateReferenceType"/> class with the specified resolver delegate.
        /// </summary>
        /// <param name="resolver">The delegate used to resolve references. Matches signiture of <see cref="Resolve(object, object, IReadOnlyDictionary{string, object})"/></param>
        public DelegateReferenceType(Func<object, object, IReadOnlyDictionary<string, object>, object> resolver)
        {
            _resolver = Guard.NotNull(resolver, nameof(resolver));
        }

        /// <inheritdoc/>
        public bool RequiresValue => true;

        /// <inheritdoc/>
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            return _resolver(input, value, context);
        }
    }
}
