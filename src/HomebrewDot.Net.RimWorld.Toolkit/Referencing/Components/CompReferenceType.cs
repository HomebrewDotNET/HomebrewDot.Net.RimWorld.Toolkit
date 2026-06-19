using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld.Referencing.Components
{
    public class CompReferenceType : IReferenceType
    {
        public object Resolve(object input, object value, IReadOnlyDictionary<string, object> context)
        {
            if(value is null) return null;
            Type compType = value as Type;
            string properties = null;
            if (value is null)
            {
                var reference = value?.ToString();
                if (string.IsNullOrWhiteSpace(reference)) return null;

                if (reference.Contains('|'))
                {
                    var split = reference.Split('|');
                    var compTypeName = split[0];
                    compType = Toolkit.Cache<string, Type>.GetOrSet(compTypeName, () => Toolkit.Helpers.TryGetType(compTypeName), true);
                    properties = split[1];
                }
                else
                {
                    compType = Type.GetType(reference);
                }
            }

            if(compType is null) return null;

            return null;
        }
    }
}
