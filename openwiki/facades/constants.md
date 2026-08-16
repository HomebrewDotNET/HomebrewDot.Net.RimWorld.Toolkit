---
type: facade
title: ToolkitConstants
description: The ToolkitConstants hub — tick intervals, expansion/mod interop constants, the per-type ObjectCache reflection cache, cached MethodInfos, expression defaults, and metadata keys for built-in indexers.
tags: [constants, metadata-keys, mod-interop, reflection-cache]
---

# ToolkitConstants

`ToolkitConstants` ([`src/HomebrewDot.Net.RimWorld.Toolkit/ToolkitConstants.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/ToolkitConstants.cs)) is a `public static class` that centralizes constants and cached reflection metadata used across the toolkit. It is the source of metadata-key identity for [built-in indexers](../indexing/overview.md) and the [reference-type compile path](../referencing/overview.md).

## Tick intervals

- `TickRareInterval = 250` — RimWorld rare-tick spread.
- `TickLongInterval = 2000` — RimWorld long-tick spread.

These drive the [snapshot orchestration](../indexing/gatherers-and-indexers.md) windows and the `OnGameTickTrigger.TickerType` classification.

## Expansion and mod interop

`ToolkitConstants.Odyssey`: `PackageId = "ludeon.rimworld.odyssey"`, `IsLoaded` (via `ModLister.GetActiveModWithIdentifier`), `UniqueWeaponCompName = "CompUniqueWeapon"`, `DroneCorpseCategoryDefName = "CorpsesDrone"`.

`ToolkitConstants.Mods.*` — nested mod-specific constants used by built-in indexers and cross-mod compatibility checks:

| Mod | PackageId | Notable constants |
|-----|-----------|-------------------|
| `MakeItUnique` | `natangry.makeitunique` | `ApparelPackageId`, `UniqueDefSuffix = "_Unique"`, `IsLoaded`/`IsApparelLoaded` |
| `Alpha.Bees` | `sarg.rimbees` | `IsLoaded` |
| `BadMeatCategory` | `Mlie.BadMeatCategory` | `MeatBadCategoryDefName = "MeatBad"`, `IsLoaded` |
| `BadLeatherCategory` | `Mlie.BadLeatherCategory` | `LeatherBadCategoryDefName = "LeatherBad"`, `IsLoaded` |
| `VqeDroneFactory` | `vanillaquestsexpanded.dronefactory` | `DroneCorpseCategoryDefName = "VQE_CorpsesDrone"`, `IsLoaded` |
| `BigAndSmall` | `RedMattis.BetterPrerequisites` | `RobotCorpseCategoryDefName = "BS_RobotCorpses"`, `IsLoaded` |
| `BetterWorkbenchManagement` | `falconne.BWM` | `IsLoaded` + reflection type names (`MainTypeName`, `ExtendedBillDataStorageTypeName`, `DialogThingFilterTypeName`, `CountProductsDetourTypeName`) for interop |

These let built-in indexers (e.g. `TrackIsFoul`) classify items conditionally based on which mods are active without hard dependencies.

## ObjectCache<T> — per-type reflection cache

`ToolkitConstants.ObjectCache<T>` is built eagerly per type via `ToDictionarySafe` (drops duplicate keys):

- `IndexedProperties`: `IReadOnlyDictionary<string, PropertyInfo>` = `typeof(T).GetProperties(Public|Instance)` keyed by name.
- `IndexedFields`: `IReadOnlyDictionary<string, FieldInfo>` = `typeof(T).GetFields(Public|Instance)`.
- `IndexedMembers`: `IReadOnlyDictionary<string, MemberInfo>` merged case-insensitively (properties win on name collision).

This is the backing store for [`Helpers.Traversing<T>`](toolkit.md) getter compilation and the [`IndexedReferenceType`](../referencing/overview.md) metadata lookup.

## Reflections — cached MethodInfos

`ToolkitConstants.Reflections` caches `MethodInfo`s resolved via `Helpers.Expression.GetMethod(...)`:

- `ConvertChangeType`, `DictionaryStringObjectContainsKey`, `DictionaryStringObjectGetItem` (the `string`-keyed indexer getter), `TypeIsAssignableFrom`.
- `GetCompProperties` (`ThingDef.GetCompProperties<CompProperties>`) — used by [`CompReferenceType`](../referencing/overview.md) to build cached comp-property getters.
- `TryGetComp` (`Thing.TryGetComp<ThingComp>`) — used by `CompReferenceType` and `Helpers.Comp.HasComp`.

## Expressions<T>

`ToolkitConstants.Expressions<T>` provides `Expression Default = Expression.Constant(default(T), typeof(T))` — a typed null/default constant used by reference-type `Compile` paths as a fallback when a value cannot be resolved.

## Thing and Def.Thing metadata keys

`ToolkitConstants.Thing` holds method-name constants (`TickMethod`, `NotifyAddedmethod`, `NotifyRemovedMethod`) and `IndexMetadataKey<T>` instances used by [Thing-level indexers](../indexing/overview.md):

- `ContainerMetadata`, `HolderMetadata` (string), `Map` (`Map`), `DestroyMode` (`DestroyMode`), `IsUnique` (bool), `ModId` (string), `HitPointPercentage` (float).

`ToolkitConstants.Def.Thing` holds metadata keys for `ThingDef` enrichment used by the built-in `TrackIs*` indexers:

- `IsConstructionMaterial` (bool), `IsFoul` (bool), `IsDrink` (bool), `IsAlcoholic` (bool), `IsMedical` (bool), `IsSurgical` (bool).

These keys are also the property names queried via `IIndexed<T>.GetValue<TValue>(propertyName)` by [collection conditions](../collecting/overview.md) — e.g. a collection comparing `Compare.Indexed("IsFoul").With.True()` reads the metadata set by `TrackIsFoul`.

## Stats

`ToolkitConstants.Stats.Weapon.Def.Range = "Range"` is the RimWorld stat def name for weapon range, used by the debug collections ("Snipers" with `>= 30`, "ShortRange" with `<= 15`) and by reference comparisons.
