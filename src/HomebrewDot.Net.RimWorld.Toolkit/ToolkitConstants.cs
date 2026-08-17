using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HomebrewDot.Net.Rimworld.Extensions;
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
        /// How many ticks the Rimworld tick manager spreads out the normal ticking of things.
        /// </summary>
        public const int TickNormalInterval = 60;
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
            /// <summary>
            /// The defName of the ThingCategoryDef added by the Odyssey expansion for drone corpses. Mods that add drones (e.g. Vanilla Quests Expanded - Drone Factory) re-target their drone corpse flesh types to this category when Odyssey is active.
            /// </summary>
            public const string DroneCorpseCategoryDefName = "CorpsesDrone";
        }

        /// <summary>
        /// Constants related to the Rimworld Anomaly expansion, which can be used to check if the expansion is installed and active, and to conditionally enable or disable features that depend on the presence of this expansion.
        /// </summary>
        public static class Anomaly
        {
            /// <summary>
            /// The package ID of the Rimworld Anomaly expansion, which can be used to check if the expansion is installed and active.
            /// </summary>
            public const string PackageId = "ludeon.rimworld.anomaly";
            /// <summary>
            /// Checks if the Rimworld Anomaly expansion is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this expansion.
            /// </summary>
            public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
            /// <summary>
            /// The defName of the HediffDef added by the Rimworld Anomaly expansion that marks a pawn as a ghoul, used to identify ghoul corpses. Ghouls are transformed humans, so their corpses share the Human corpse def and can only be identified per-instance through this hediff.
            /// </summary>
            public const string GhoulHediffDefName = "Ghoul";
        }

        /// <summary>
        /// Constants related to the Rimworld Ideology expansion, which can be used to check if the expansion is installed and active, and to conditionally enable or disable features that depend on the presence of this expansion.
        /// </summary>
        public static class Ideology
        {
            /// <summary>
            /// The package ID of the Rimworld Ideology expansion, which can be used to check if the expansion is installed and active.
            /// </summary>
            public const string PackageId = "ludeon.rimworld.ideology";
            /// <summary>
            /// Checks if the Rimworld Ideology expansion is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this expansion.
            /// </summary>
            public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
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

            /// <summary>
            /// Contains constants related to the "Bad Meat Category" mod by "Mlie".
            /// </summary>
            public static class BadMeatCategory
            {
                /// <summary>
                /// Id of the "Bad Meat Category" mod, which can be used to check if the mod is installed and active.
                /// </summary>
                public const string PackageId = "Mlie.BadMeatCategory";
                /// <summary>
                /// The defName of the ThingCategoryDef added by Bad Meat Category.
                /// </summary>
                public const string MeatBadCategoryDefName = "MeatBad";
                /// <summary>
                /// Checks if the "Bad Meat Category" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
            }

            /// <summary>
            /// Contains constants related to the "Bad Leather Category" mod by "Mlie".
            /// </summary>
            public static class BadLeatherCategory
            {
                /// <summary>
                /// Id of the "Bad Leather Category" mod, which can be used to check if the mod is installed and active.
                /// </summary>
                public const string PackageId = "Mlie.BadLeatherCategory";
                /// <summary>
                /// The defName of the ThingCategoryDef added by Bad Leather Category.
                /// </summary>
                public const string LeatherBadCategoryDefName = "LeatherBad";
                /// <summary>
                /// Checks if the "Bad Leather Category" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
            }

            /// <summary>
            /// Contains constants related to the "Vanilla Quests Expanded - Drone Factory" mod.
            /// </summary>
            public static class VqeDroneFactory
            {
                /// <summary>
                /// Id of the "Vanilla Quests Expanded - Drone Factory" mod, which can be used to check if the mod is installed and active.
                /// </summary>
                public const string PackageId = "vanillaquestsexpanded.dronefactory";
                /// <summary>
                /// The defName of the ThingCategoryDef added by Vanilla Quests Expanded - Drone Factory for drone corpses.
                /// </summary>
                public const string DroneCorpseCategoryDefName = "VQE_CorpsesDrone";
                /// <summary>
                /// Checks if the "Vanilla Quests Expanded - Drone Factory" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
            }

            /// <summary>
            /// Contains constants related to the "Big and Small - Framework" mod by "RedMattis".
            /// </summary>
            public static class BigAndSmall
            {
                /// <summary>
                /// Id of the "Big and Small - Framework" mod, which can be used to check if the mod is installed and active. This is the packageId declared in the mod's About.xml.
                /// </summary>
                public const string PackageId = "RedMattis.BetterPrerequisites";
                /// <summary>
                /// The defName of the ThingCategoryDef added by Big and Small - Framework for robot corpses.
                /// </summary>
                public const string RobotCorpseCategoryDefName = "BS_RobotCorpses";
                /// <summary>
                /// Checks if the "Big and Small - Framework" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
            }

            /// <summary>
            /// Contains constants related to the "Better Workbench Management" mod by "Falconne".
            /// </summary>
            public static class BetterWorkbenchManagement
            {
                /// <summary>
                /// Id of the "Better Workbench Management" mod, which can be used to check if the mod is installed and active. This is the packageId declared in the mod's About.xml.
                /// </summary>
                public const string PackageId = "falconne.BWM";
                /// <summary>
                /// Checks if the "Better Workbench Management" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;

                /// <summary>
                /// The full name of the <c>Main</c> type declared by the Better Workbench Management mod, used to resolve the type through reflection.
                /// </summary>
                public const string MainTypeName = "ImprovedWorkbenches.Main";
                /// <summary>
                /// The full name of the <c>ExtendedBillDataStorage</c> type declared by the Better Workbench Management mod, used to resolve the type through reflection.
                /// </summary>
                public const string ExtendedBillDataStorageTypeName = "ImprovedWorkbenches.ExtendedBillDataStorage";
                /// <summary>
                /// The full name of the <c>ExtendedBillData</c> type declared by the Better Workbench Management mod, used to resolve the type through reflection.
                /// </summary>
                public const string ExtendedBillDataTypeName = "ImprovedWorkbenches.ExtendedBillData";
                /// <summary>
                /// The full name of the <c>Dialog_ThingFilter</c> type declared by the Better Workbench Management mod, used to resolve the type through reflection.
                /// </summary>
                public const string DialogThingFilterTypeName = "ImprovedWorkbenches.Dialog_ThingFilter";
                /// <summary>
                /// The full name of the <c>RecipeWorkerCounter_CountProducts_Detour</c> type declared by the Better Workbench Management mod, used to resolve the type through reflection.
                /// </summary>
                public const string CountProductsDetourTypeName = "ImprovedWorkbenches.RecipeWorkerCounter_CountProducts_Detour";
            }

            /// <summary>
            /// Contains constants related to the "Davai's Sorted Categories" mod by "Davai".
            /// </summary>
            public static class DavaiSortedCategories
            {
                /// <summary>
                /// Id of the "Davai's Sorted Categories" mod, which can be used to check if the mod is installed and active. This is the packageId declared in the mod's About.xml.
                /// </summary>
                public const string PackageId = "davai.sortedcategories";
                /// <summary>
                /// The defName of the ThingCategoryDef added by Davai's Sorted Categories for meat that causes mood debuffs when eaten (human meat, insect meat, twisted meat, etc.), into which the mod moves such meat.
                /// </summary>
                public const string NastyMeatCategoryDefName = "DavaiNastyMeat";
                /// <summary>
                /// Checks if the "Davai's Sorted Categories" mod is loaded and active in the current Rimworld session. This can be used to conditionally enable or disable features that depend on the presence of this mod.
                /// </summary>
                public static bool IsLoaded => ModLister.GetActiveModWithIdentifier(PackageId) != null;
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
            public static IReadOnlyDictionary<string, PropertyInfo> IndexedProperties { get; } = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionarySafe(p => p.Name, p => p);
			/// <summary>
			/// Cache of field info for the indexed type, keyed by field name.
			/// </summary>
			public static IReadOnlyDictionary<string, FieldInfo> IndexedFields { get; } = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionarySafe(f => f.Name, f => f);
            /// <summary>
            /// Cache of member info (properties and fields) for the indexed type, keyed by member name. This is a combination of the IndexedProperties and IndexedFields caches, and can be used to access both properties and fields by name.
            /// </summary>
            public static IReadOnlyDictionary<string, MemberInfo> IndexedMembers { get; } = BuildIndexedMembers();
            private static IReadOnlyDictionary<string, MemberInfo> BuildIndexedMembers()
            {
                var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase);
                foreach(var property in IndexedProperties)
                {
                    if(!members.ContainsKey(property.Key))
                    {
                        members.Add(property.Key, property.Value);
                    }
                }

                foreach(var field in IndexedFields)
                {
                    if(!members.ContainsKey(field.Key))
                    {
                        members.Add(field.Key, field.Value);
                    }
                } 
                return members;
            }
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
            /// Key of the metadata that indicates whether a thing is the corpse of a ghoul (Anomaly), which can be used to check if the thing is a ghoul corpse in code or definitions. Only set by <see cref="Toolkit.Indexing.Thing.TrackIsGhoulCorpse"/> for <see cref="Verse.Corpse"/>s whose <see cref="Verse.Corpse.InnerPawn"/> carries the Anomaly "Ghoul" hediff.
            /// </summary>
            public static readonly IndexMetadataKey<bool> IsGhoulCorpse = IndexMetadataKey<bool>.Get("IsGhoulCorpse");
            /// <summary>
            /// Key of the metadata that indicates whether a thing is the corpse of a colonist, which can be used to check if the thing is a colonist corpse in code or definitions. Only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for humanlike <see cref="Verse.Corpse"/>s whose <see cref="Verse.Corpse.InnerPawn"/> was a free colonist.
            /// </summary>
            public static readonly IndexMetadataKey<bool> IsColonistCorpse = IndexMetadataKey<bool>.Get("IsColonistCorpse");
            /// <summary>
            /// Key of the metadata that indicates whether a thing is the corpse of a stranger, which can be used to check if the thing is a stranger corpse in code or definitions. Only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for humanlike <see cref="Verse.Corpse"/>s whose <see cref="Verse.Corpse.InnerPawn"/> did not belong to the player faction.
            /// </summary>
            public static readonly IndexMetadataKey<bool> IsStrangerCorpse = IndexMetadataKey<bool>.Get("IsStrangerCorpse");
            /// <summary>
            /// Key of the metadata that indicates whether a thing is the corpse of a slave, which can be used to check if the thing is a slave corpse in code or definitions. Only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for humanlike <see cref="Verse.Corpse"/>s whose <see cref="Verse.Corpse.InnerPawn"/> was a player-faction slave (Ideology).
            /// </summary>
            public static readonly IndexMetadataKey<bool> IsSlaveCorpse = IndexMetadataKey<bool>.Get("IsSlaveCorpse");
            /// <summary>
            /// Key of the metadata that indicates whether a thing is an unnatural corpse (Anomaly), which can be used to check if the thing is an unnatural corpse in code or definitions. Only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for <see cref="Verse.UnnaturalCorpse"/>s.
            /// </summary>
            public static readonly IndexMetadataKey<bool> IsUnnaturalCorpse = IndexMetadataKey<bool>.Get("IsUnnaturalCorpse");
            /// <summary>
            /// Key of the metadata that indicates whether a thing is the corpse of a tame colony animal (a pet), which can be used to check if the thing is a pet corpse in code or definitions. Only set by <see cref="Toolkit.Indexing.Thing.TrackCorpseKind"/> for <see cref="Verse.Corpse"/>s whose <see cref="Verse.Corpse.InnerPawn"/> was a tame animal of the player faction.
            /// </summary>
            public static readonly IndexMetadataKey<bool> IsPetCorpse = IndexMetadataKey<bool>.Get("IsPetCorpse");
            /// <summary>
            /// Key of the metadata that contains the mod ID of a def, which can be used to check which mod a def belongs to in code or definitions.
            /// </summary>
            public static readonly IndexMetadataKey<string> ModId = IndexMetadataKey<string>.Get("ModId");
            /// <summary>
            /// Key of the metadata that contains the hit point percentage of a thing, which can be used to check the condition of a thing in code or definitions.
            /// </summary>
            public static readonly IndexMetadataKey<float> HitPointPercentage = IndexMetadataKey<float>.Get("HitPointPercentage");
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
                /// <summary>
                /// Key of the metadata that indicates whether a ThingDef is foul (meat or leather from humanlike,
                /// insectoid, twisted or other non-standard creatures, plus meat of pollution-adapted animals).
                /// </summary>
                public readonly static IndexMetadataKey<bool> IsFoul = IndexMetadataKey<bool>.Get("IsFoul");
                /// <summary>
                /// Key of the metadata that indicates whether a ThingDef is a drinkable beverage.
                /// </summary>
                public readonly static IndexMetadataKey<bool> IsDrink = IndexMetadataKey<bool>.Get("IsDrink");
                /// <summary>
                /// Key of the metadata that indicates whether a ThingDef is an alcoholic drink.
                /// </summary>
                public readonly static IndexMetadataKey<bool> IsAlcoholic = IndexMetadataKey<bool>.Get("IsAlcoholic");
                /// <summary>
                /// Key of the metadata that indicates whether a ThingDef is a medical item (medicine or medical drug).
                /// </summary>
                public readonly static IndexMetadataKey<bool> IsMedical = IndexMetadataKey<bool>.Get("IsMedical");
                /// <summary>
                /// Key of the metadata that indicates whether a ThingDef is a surgical part (body part, prosthetic, bionic, etc.).
                /// </summary>
                public readonly static IndexMetadataKey<bool> IsSurgical = IndexMetadataKey<bool>.Get("IsSurgical");
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
