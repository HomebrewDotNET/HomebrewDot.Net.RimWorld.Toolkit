using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HomebrewDot.Net.Rimworld
{
    /// <summary>
    /// Contains constant/readonly values used across the toolkit.
    /// </summary>
    public static class ToolkitConstants
    {
        /// <summary>
        /// How many ticks the Rimworld tick manager spreads out the rare ticking of things.
        /// </summary>
        public const int TickRareInterval = 250;
        /// <summary>
        /// How many ticks the Rimworld tick manager spreads out the long ticking of things.
        /// </summary>
        public const int TickLongInterval = 2000;

        /// <summary>
        /// Contains cached reflection info for the indexed type, to avoid repeated reflection calls when building indexes and enrichers.
        /// </summary>
        public static class ObjectCache<T>
        {
            /// <summary>
            /// Cache of property info for the indexed type, keyed by property name.
            /// </summary>
            public static IReadOnlyDictionary<string, PropertyInfo> IndexedProperties { get; } = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
			/// <summary>
			/// Cache of field info for the indexed type, keyed by field name.
			/// </summary>
			public static IReadOnlyDictionary<string, FieldInfo> IndexedFields { get; } = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
			/// <summary>
			/// MethodInfo for object.GetType().
			/// </summary>
			public static readonly MethodInfo GetTypeMethod = Toolkit.Helpers.Expression.GetMethod(() => default(T)!.GetType());
            /// <summary>
            /// MethodInfo for object.ToString().
            /// </summary>
            public static readonly MethodInfo ToStringMethod = Toolkit.Helpers.Expression.GetMethod(() => default(T)!.ToString());
        }

        /// <summary>
        /// Contains cacheds reflection for commonly used methods in the toolkit, to avoid repeated reflection calls when building indexes and enrichers.
        /// </summary>
        public static class Reflections
        {
            /// <summary>
            /// MethodInfo for Convert.ChangeType(object value, Type conversionType).
            /// </summary>
            public static readonly MethodInfo ConvertChangeType = Toolkit.Helpers.Expression.GetMethod(() => Convert.ChangeType(default!, (Type)default!));
            /// <summary>
            /// MethodInfo for IReadOnlyDictionary<string, object>.ContainsKey(string key).
            /// </summary>
            public static readonly MethodInfo DictionaryStringObjectContainsKey = Toolkit.Helpers.Expression.GetMethod<IReadOnlyDictionary<string, object>>(d => d.ContainsKey(default!));
            /// <summary>
            /// MethodInfo for IReadOnlyDictionary<string, object>.get_Item(string key).
            /// </summary>
            public static readonly MethodInfo DictionaryStringObjectGetItem = ObjectCache<IReadOnlyDictionary<string, object>>.IndexedProperties.Values.First(p => p.Name.Equals("Item", StringComparison.OrdinalIgnoreCase) && p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(string)).GetGetMethod()!;
            /// <summary>
            /// MethodInfo for Type.IsAssignableFrom(Type c).
            /// </summary>
            public static readonly MethodInfo TypeIsAssignableFrom = Toolkit.Helpers.Expression.GetMethod<Type>(t => t.IsAssignableFrom(default!));
        }
        /// <summary>
        /// Contains cached expressions for commonly used expressions in the toolkit, to avoid repeated expression compilation when building indexes and enrichers.
        /// </summary>
        /// <typeparam name="T">The type for which the expressions are cached.</typeparam>
        public static class Expressions<T>
        {
            public static readonly Expression Default = Expression.Constant(default(T), typeof(T));
        }

        /// <summary>
        /// Constants related to the Thing type.
        /// </summary>
        public static class Thing
        {
            /// <summary>
            /// Name of the internal tick methods of things.
            /// </summary>
            public const string TickMethod = "Tick";
            /// <summary>
            /// Method on <see cref="ThingOwner"/> that is called when a thing is added to a container, which can be used to track the current container of a thing.
            /// </summary>
            public const string NotifyAddedmethod = "NotifyAdded";
            /// <summary>
            /// Method on <see cref="ThingOwner"/> that is called when a thing is removed from a container, which can be used to track the last container of a thing before it was removed.
            /// </summary>
            public const string NotifyRemovedMethod = "NotifyRemoved";

            /// <summary>
            /// Key of the metadata that contains the current container of a thing.
            /// </summary>
            public const string ContainerMetadata = "Thing__Container";
            /// <summary>
            /// Key of the metadata that contains the current holder of a thing.
            /// </summary>
            public const string HolderMetadata = "Thing__Holder";
        }

        /// <summary>
        /// Constants related to stats and stat defs.
        /// </summary>
        public static class  Stats
        {
            /// <summary>
            /// Constants related weapon stats
            /// </summary>
            public static class Weapon
            {
                /// <summary>
                /// Constants related to weapon stats on the def level.
                /// </summary>
                public static class Def
                {
                    /// <summary>
                    /// Name of the stat that defines the range of a weapon.
                    /// </summary>
                    public const string Range = "Range";
                }
            }
        }
    }
}
