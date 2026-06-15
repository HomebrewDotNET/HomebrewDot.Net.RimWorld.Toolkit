using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Generic.Models;
using static HomebrewDot.Net.Rimworld.Toolkit.Helpers;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    /// <summary>
    /// Default implementation of the <see cref="IReferenceResolver"/> interface, which uses a provided dictionary of reference types to resolve references. The resolver can also use reference types provided in the context dictionary, which will take precedence over the reference types provided in the constructor if both are present. 
    /// This allows for dynamic overriding of reference types on a per-resolution basis, providing flexibility in how references are resolved based on the specific context of each resolution operation.
    /// </summary>
    public class ReferenceResolver : IReferenceTypeResolver
    {
        // Statics
        private static readonly Dictionary<string, Func<object, object, IReadOnlyDictionary<string, object>, object>> _compiledResolversCache = new Dictionary<string, Func<object, object, IReadOnlyDictionary<string, object>, object>>();

        // Constants
        /// <summary>
        /// The key used in the context dictionary to store the reference types that the resolver can use to resolve references. 
        /// The value associated with this key should be an <see cref="IReadOnlyDictionary{string, IReferenceType}"/> where the keys are the reference type names and the values are the corresponding <see cref="IReferenceTypeCompileable"/> instances that can resolve references of that type.
        /// Will take precedence over the reference types provided in the constructor if both are present, allowing for dynamic overriding of reference types on a per-resolution basis. If this key is not present in the context, the resolver will fall back to using the reference types provided in the constructor.
        /// </summary>
        public const string ContextReferenceTypesKey = "ReferenceTypes";

        // Fields
        private readonly IReadOnlyDictionary<string, IReferenceType> _referenceTypes;

        /// <inheritdoc cref="ReferenceResolver"/>
        /// <param name="referenceTypes">A dictionary of reference types to be used by the resolver. If null, an empty dictionary will be used.</param>
        public ReferenceResolver(IReadOnlyDictionary<string, IReferenceType> referenceTypes)
        {
            _referenceTypes = referenceTypes ?? NullDictionary<string, IReferenceType>.Instance;
        }

        /// <inheritdoc/>
        public bool TryResolve(object input, IReference reference, IReadOnlyDictionary<string, object> context, out object result)
        {
            reference = Guard.NotNull(reference, nameof(reference));
            result = null;
            
            var referenceType = GetReferenceType(input, reference, context);

            if (referenceType == null)
            {
                return false;
            }
            else if(referenceType is IReferenceTypeCompileable compileableReferenceType)
            {
                var cacheKey = compileableReferenceType.GetCacheKey(input, reference.Value, context, out var returnType);
                if(cacheKey != null)
                {
                    if(!_compiledResolversCache.TryGetValue(cacheKey, out var cachedResolver))
                    {
                        cachedResolver = GetCompiledResolver(compileableReferenceType, reference.Value, context);
                        _compiledResolversCache[cacheKey] = cachedResolver;
                    }
                    result = cachedResolver(input, reference.Value, context);
                    return true;
                }
            }

                result = referenceType.Resolve(input, reference.Value, context);
            return true;
        }


        /// <inheritdoc/>
        public IReferenceType GetReferenceType(object input, IReference reference, IReadOnlyDictionary<string, object> context)
        {
            reference = Guard.NotNull(reference, nameof(reference));
            var priorityReferenceTypes = context != null && context.TryGetValue(ContextReferenceTypesKey, out var referenceTypesObj) && referenceTypesObj is IReadOnlyDictionary<string, IReferenceTypeCompileable> referenceTypesFromContext
                ? referenceTypesFromContext
                : null;

            var referenceType = Guard.NotNull(reference.Type, nameof(reference.Type)).Trim() switch
            {
                var type when priorityReferenceTypes != null && priorityReferenceTypes.TryGetValue(type, out var referenceTypeFromContext) => referenceTypeFromContext,
                var type when _referenceTypes.TryGetValue(type, out var referenceTypeFromConstructor) => referenceTypeFromConstructor,
                _ => null
            };

            return referenceType;
        }

        private Func<object, object, IReadOnlyDictionary<string, object>, object> GetCompiledResolver(IReferenceTypeCompileable referenceType, object value, IReadOnlyDictionary<string, object> context)
        {
            var inputParameter = System.Linq.Expressions.Expression.Parameter(typeof(object), "input");
            var contextParameter = System.Linq.Expressions.Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), "context");
            var resolutionExpression = referenceType.Compile(inputParameter, contextParameter, value, context);
            return System.Linq.Expressions.Expression.Lambda<Func<object, object, IReadOnlyDictionary<string, object>, object>>(resolutionExpression, inputParameter, contextParameter).Compile();
        }
    }
}
