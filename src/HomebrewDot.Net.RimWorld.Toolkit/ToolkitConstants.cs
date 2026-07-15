using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Indexing.Models;
using Verse;

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
        /// Constants related to the Rimworld Odyssey expansion, which can be used to check if the expansion is installed and active, and to conditionally enable or disable features that depend on the presence of this expansion.
        /// </summary>
        public static class Odyssey
        {
            /// <summary>
            /// The package ID of the Rimworld Odyssey expansion, which can be used to check if the expansion is installed and active.
            /// </summary>
            public const string PackageId = "ludeon.rimworld.odyssey";
            /// <summary>
            /// Checks if the Rimworld Odyssey expansion is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this expansion.
            /// </summary>
            public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
            /// <summary>
            /// The name of the comp that is added to unique weapons by the Rimworld Odyssey expansion, which can be used to check if a weapon is unique in code or definitions.
            /// </summary>
            public const string UniqueWeaponCompName = "CompUniqueWeapon";
        }

        /// <summary>
        /// Contains constants related to mods, such as mod IDs and other mod-specific information that may be used across the toolkit.
        /// </summary>
        public static class Mods
        {
            /// <summary>
            /// Constants related to the "Make It Unique" mod.
            /// </summary>
            public static class MakeItUnique
            {
                /// <summary>
                /// Checks if the "Make It Unique" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
                /// <summary>
                /// Checks if the "Make It Unique - Apparel" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsApparelLoaded => ModLister.GetActiveModWithIdentifier(ApparelPackageId) != null;

                /// <summary>
                /// Id of the "Make It Unique" mod, which can be used to check if the mod is installed and active.
                /// </summary>
                public const string PackageId = "natangry.makeitunique";
                /// <summary>
                /// Id of the "Make It Unique - Apparel" mod, which can be used to check if the mod is installed and active.
                /// </summary>
                public const string ApparelPackageId = "natangry.makeitunique.apparel";
                /// <summary>
                /// Suffix used in the names of defs that have been made unique by the "Make It Unique" mod, which can be used to identify such defs in code or definitions.
                /// </summary>
                public const string UniqueDefSuffix = "_Unique";
            }

            /// <summary>
            /// Contains constants related to the "Alpha" series mods by "Sarg Bjornson"
            /// </summary>
            public static class Alpha
            {
                /// <summary>
                /// Contains constants related to the "Alpha Bees" mod by "Sarg Bjornson"
                /// </summary>
                public static class Bees
                {
                    /// <summary>
                    /// Id of the "Alpha Bees" mod, which can be used to check if the mod is installed and active.
                    /// </summary>
                    public const string PackageId = "sarg.rimbees";
                    /// <summary>
                    /// Checks if the "Alpha Bees" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                    /// </summary>
                    public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
                }
            }
        }

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
            /// <summary>
            /// MethodInfo for ThingDef.GetCompProperties<CompProperties>().
            /// </summary>
            public static readonly MethodInfo GetCompProperties = Toolkit.Helpers.Expression.GetMethod<ThingDef>(t => t.GetCompProperties<CompProperties>());
            /// <summary>
            /// MethodInfo for Thing.TryGetComp<ThingComp>().
            /// </summary>
            public static readonly MethodInfo TryGetComp = Toolkit.Helpers.Expression.GetMethod<Verse.Thing>(t => t.TryGetComp<ThingComp>());
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
            public static readonly IndexMetadataKey<string> ContainerMetadata = IndexMetadataKey<string>.Get("Thing__Container");
            /// <summary>
            /// Key of the metadata that contains the current holder of a thing.
            /// </summary>
            public static readonly IndexMetadataKey<string> HolderMetadata = IndexMetadataKey<string>.Get("Thing__Holder");

            /// <summary>
            /// Key of the metadata that contains the current map of a thing.
            /// </summary>
            public static readonly IndexMetadataKey<Map> Map = IndexMetadataKey<Map>.Get(nameof(Thing.Map));
            /// <summary>
            /// Key of the metadata that contains the destroy mode of a thing.
            /// </summary>
            public static readonly IndexMetadataKey<DestroyMode> DestroyMode = IndexMetadataKey<DestroyMode>.Get("DestroyMode");
            /// <summary>
            /// Key of the metadata that indicates whether a thing def is a Odyssey unique item, which can be used to check if the def is unique in code or definitions.
            /// </summary>
            public static readonly IndexMetadataKey<bool> IsUnique = IndexMetadataKey<bool>.Get("IsUnique");
            /// <summary>
            /// Key of the metadata that contains the mod ID of a def, which can be used to check which mod a def belongs to in code or definitions.
            /// </summary>
            public static readonly IndexMetadataKey<string> ModId = IndexMetadataKey<string>.Get("ModId");
        }

        /// <summary>
        /// Constants related to <see cref="Verse.Def"/>s.
        /// </summary>
        public static class Def
        {
            /// <summary>
            /// Constants related to <see cref="Verse.ThingDef"/>s.
            /// </summary>
            public static class Thing
            {
                /// <summary>
                /// Key of the metadata that indicates whether a thing is considered a construction material for any def buildable by the player.
                /// </summary>
                public readonly static IndexMetadataKey<bool> IsConstructionMaterial = IndexMetadataKey<bool>.Get("IsConstructionMaterial");                
            }
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
