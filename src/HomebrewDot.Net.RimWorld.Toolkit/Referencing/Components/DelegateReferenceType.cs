using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.RimWorld.Toolkit.Helpers;

namespace HomebrewDot.Net.RimWorld.Referencing.Components
{
    /// <summary>
    /// A reference type that uses a delegate to resolve references. This allows for custom resolution logic to be provided at runtime.
    /// </summary>
    public class DelegateReferenceType : IReferenceType
    {
        private readonly Func<object, IReadOnlyDictionary<string, object>, object> _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateReferenceType"/> class with the specified resolver delegate.
        /// </summary>
        /// <param name="resolver">The delegate used to resolve references.</param>
        public DelegateReferenceType(Func<object, IReadOnlyDictionary<string, object>, object> resolver)
        {
            _resolver = Guard.NotNull(resolver, nameof(resolver));
        }

        /// <inheritdoc/>
        public object Resolve(object value, IReadOnlyDictionary<string, object> context)
        {
            return _resolver(value, context);
        }
    }
}
