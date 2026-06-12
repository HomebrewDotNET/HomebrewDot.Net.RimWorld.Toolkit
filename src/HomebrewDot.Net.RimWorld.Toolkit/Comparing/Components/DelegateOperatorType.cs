using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Comparing.Components
{
    /// <summary>
    /// An operator type that uses a delegate to perform comparisons. This allows for custom comparison logic to be provided at runtime.
    /// </summary>
    public class DelegateOperatorType : IOperatorType
    {
        private readonly Func<object, object, IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool> _comparer;

        /// <inheritdoc cref="DelegateOperatorType"/>
        /// <param name="comparer">The delegate used to perform the comparison.</param>
        public DelegateOperatorType(Func<object, object, IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>, bool> comparer)
        {
            _comparer = Guard.NotNull(comparer, nameof(comparer));
        }

        /// <inheritdoc/>
        public bool Compare(object left, object right, IReadOnlyDictionary<string, object> arguments, IReadOnlyDictionary<string, object> context)
        {
            return _comparer(left, right, arguments, context);
        }
    }
}
