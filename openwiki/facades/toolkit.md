---
type: facade
title: Toolkit Facade and Shared Services
description: The Toolkit Mod singleton, ConfigureServices bootstrap, and the nested static facades Services, Helpers, Pool, Cache, and ToolkitSettings that the whole library is built on.
tags: [facade, services, helpers, settings, toolkit]
---

# Toolkit Facade and Shared Services

`Toolkit` ([`src/HomebrewDot.Net.RimWorld.Toolkit/Toolkit.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Toolkit.cs)) is a `Verse.Mod` subclass and the single composition root for the library. RimWorld instantiates it once per content pack; the constructor sets the `Instance` singleton, builds the settings UI, and calls `ConfigureServices()`.

## Singleton and construction

```csharp
public class Toolkit : Mod
{
    public static string ModId { get; } = typeof(Toolkit).FullName.ToLower();
    internal static Harmony Harmony => _harmony.Value; // Lazy<Harmony>, new Harmony(ModId)
    public static Toolkit Instance { get; } // throws if accessed before construction
    public static ToolkitSettings Settings { get; } // lazily GetSettings<ToolkitSettings>()
    public Toolkit(ModContentPack content) : base(content) { Instance = this; _settingsUi = new ToolkitSettingsUi(); ConfigureServices(); }
}
```

`ModId` is `homebrewdot.net.rimworld.toolkit.toolkit` (the type full name lower-cased) and is used as the Harmony instance id. `Settings` is lazily initialized via `Instance.GetSettings<ToolkitSettings>()` and cached.

## ConfigureServices

`internal static void ConfigureServices()` registers all strategy implementations into [`Toolkit.Services`](#services) by name:

- **Reference types** (`IReferenceType`): `IndexedReferenceType` (`"Indexed"`), `PropertyReferenceType` (`"Property"`), `ValueReferenceType` (`"Value"`), `StatReferenceType` (`"Stat"`), `CompReferenceType` (`"Comp"`), `DefReferenceType<ThingCategoryDef>` (`"ThingCategoryDef"`), `DefReferenceType<StuffCategoryDef>` (`"StuffCategoryDef"`), `SelfReferenceType` (`"Self"`). See [Referencing](../referencing/overview.md).
- **UI input helpers** (`IReferenceTypeInputHelper`): `StateReferenceTypeInputHelper` (under `"Stat"`), `DefReferenceTypeInputHelper<ThingCategoryDef>`/`<StuffCategoryDef>`, `CompReferenceTypeInputHelper`. See [UI](../ui/overview.md).
- **Operator types** (`IOperatorType`): each operator registered once per alias — `EqualsOperatorType` (`Equal`, `eq`, `==`, ...), `NotEquals`, `Greater`, `GreaterOrEqual`, `Lesser`, `LesserOrEqual`, `True`, `False`, `Null`, `NotNull`, `Match` — plus `InOperatorType`, `ContainsOperatorType`, `InThingCategoryOperatorType` under their `DefaultTypeName` only. See [Comparing](../comparing/overview.md).

`ConfigureServices` is idempotent in effect because `Services.Register` overwrites named entries and appends to the unnamed list. Integration tests call `Toolkit.ConfigureServices()` (or `ToolkitImpl.ConfigureServices()`) in their constructors to ensure the registry is populated.

## Services

`public static class Services` is a non-thread-safe service registry backed by three static dictionaries:

- `_services` (`Dictionary<Type, object>`) → `List<T>` of all instances for a service type.
- `_serviceCache` (`Dictionary<Type, object>`) → cached `T[]` snapshot, invalidated on every mutation.
- `_namedServices` (`Dictionary<Type, object>`) → `Dictionary<string, T>` using `StringComparer.OrdinalIgnoreCase` (case-insensitive names).

| Method | Behavior |
|--------|----------|
| `Register<T>(T service, string name = null)` | Appends to `_services`; invalidates `_serviceCache`; if `name` non-whitespace, sets `dict[name] = service` (overwrites). |
| `Unregister<T>(T service)` | Removes from `_services`; removes matching named entries; disposes if `IDisposable`. Returns whether removed. |
| `UnregisterByName<T>(string name)` | Resolves named instance and unregisters it. |
| `Get<T>(string name = null)` | Named: returns found or `default`. Unnamed: `GetAll<T>().LastOrDefault()` (LIFO — last registered wins). |
| `GetRequired<T>(string name = null)` | `Get<T>` then throws `InvalidOperationException` if null. |
| `GetAll<T>(bool includeNamed = false)` | Default: returns cached `T[]` (or `Array.Empty<T>()`). `includeNamed`: fresh list merging unnamed + named. |
| `GetAllNamed<T>()` | Returns the named `IReadOnlyDictionary<string,T>`, or `NullDictionary<string,T>.Instance` if none (no allocation). |

Invariants: case-insensitive named lookup; LIFO for unnamed `Get`; `GetAllNamed` returns the [`NullDictionary`](../generic/overview.md) singleton when empty; cache invalidation on mutation; disposed services disposed via `Invoking.Safe`. Focused tests: `ToolkitServicesTests` (named register/get, LIFO last-wins, `GetRequired` throws, unregister cleans named mapping, `GetAll`/`GetAllNamed`); `ServicesIntegrationTests` (case-insensitivity, multiple named registrations, cleanup round-trip).

## Helpers

`public static class Helpers` contains non-RimWorld-specific utilities as nested static classes:

- **`Logging`**: routes through `Verse.Log.Message/Warning/Error` with a `LogLevel` enum (`DBG, INF, WRN, ERR, PRF`). Has an `IsBroken` fallback to `Console.WriteLine` after a Verse logging failure. `Verbose`/`Performance` flags are seeded from `Toolkit.Settings` and updated by a `ToolkitSettings.Changed` hook. `IsVerboseEnabled` gates `LogVerbose`; `IsPerformanceEnabled` (`Verbose || Performance`) gates `LogPerformance`.
- **`Invoking`**: `Safe(Action)` (try/catch + `LogError`) and `Safe<T>(Func<T>, T defaultValue)` — used pervasively to isolate failures (hook dispatch, listener invocation, settings copy, orchestrator dispose).
- **`Guard`** (`[AggressiveInlining]`): `NotNull` (`ArgumentNullException`), `NotNullOrEmpty` (allows whitespace), `NotNullOrWhitespace`, `Is<T>(value, Expression<Predicate<T>>, exceptionBuilder, parameterName)` (compiles the predicate; throws `ArgumentException` with the expression text or a custom exception). Focused tests: `GuardTests`.
- **`Expression`**: reflection over `System.Linq.Expressions.Expression` trees — `GetMethod`, `GetConstructor`, `GetConstructorForGeneric(targetTypeArg, expr)` (finds the equivalent ctor on a closed generic by index), `GetProperty`, `GetNestedProperties`/`GetNestedMembers`, `GetMember`, `CompileLoop(input, inputType, loopBody)` (emits a typed iteration loop: index-based for arrays/`IReadOnlyList<T>`, `IEnumerator<T>` for other `IEnumerable<T>`, single-call for non-collections). Focused tests: `ExpressionTests`.
- **`Traversing`** / **`Traversing<T>`**: property/field path traversal with compiled getter caches (`ConcurrentDictionary`). `TraversePath(obj, "a.b.c")`, `SplitPath` (splits on `.`), `TryWalkPath`/`TryWalkIndexedPath` (walk types), `GenerateFullGetter(Expression, type, path)` (builds a null-safe block expression for [indexer `ByPath`](../indexing/overview.md) and [reference compile](../referencing/overview.md)), `GetMembers(Type)`. Focused tests: `TraversingTests`.
- **`Comp`**: `HasComp(Thing, compName)` — caches type resolution and a compiled `TryGetComp<T>` lambda.
- Top-level: `TryGetType(string)` (tries `Type.GetType`, `Verse.`/`RimWorld.` prefixes, then brute-force assembly scan tolerating `ReflectionTypeLoadException`), `ScanForTypes(Predicate<Type>)`, `GetGenericTypes(Type, genericType)` (extracts the first generic argument from matching generic interfaces). Focused tests: `TypeUnitTests` (Theory over 5 type names).

## Pool and Cache

`Toolkit.Pool<T> where T : IPoolable, new()` — a `Queue<T>` (initial capacity 1024, not thread-safe). `Rent()` returns a dequeued item or `new T()`; `Return(value)` calls `Guard.NotNull`, `value.Reset()` (clears state via [`IPoolable`](../generic/overview.md)), and enqueues. Used by `DatabaseAction`/`TableAction`, `PendingUpsert`, `TrackingIndexed<T>` snapshot key lists, `SyncPendingWork<T>`, `RaiseCooperativeWork<T>`.

`Toolkit.Cache<TKey, TValue>` — a `ConcurrentDictionary`. `GetOrSet(key, factory, expensive=false)`: on miss, `GetOrAdd`; when `expensive`, the factory performs a double-check `TryGetValue` to avoid duplicate construction under contention. `Invalidate(key)` removes. Notable uses: comp/def type resolution, `TrackIsConstructionMaterial` research-keyed buildable-def cache, `Indexed<T>`/`TrackingIndexed<T>` compiled accessor/creator caches.

## ToolkitSettings

`public class ToolkitSettings : ModSettings` ([`Toolkit.cs`](../../src/HomebrewDot.Net.RimWorld.Toolkit/Toolkit.cs)) holds four `bool` fields (default `false`):

| Field | Effect |
|-------|--------|
| `DynamicGatheringEnabled` | (experimental) spread snapshot pushing across ticks. |
| `SlowGatheringEnabled` | Use `TickLong` (2000-tick) instead of `TickRare` (250-tick) snapshot windows. Read by the `Indexing.Orchestrator` getter when constructing `SnapshotOrchestrator`. |
| `Verbose` | Enable verbose debug logging. |
| `PerformanceLogging` | Enable performance/timing logs. |

`ExposeData()` scribes all four with `defaultValue: false`. The **`Changed` trigger fires only during `Scribe.mode == LoadSaveMode.Saving`** and only when at least one old value differs from the new. It triggers `new Changed(defensiveCopy)` through [`Toolkit.Hooks.Manager`](../hooks/overview.md). The `Changed` nested class carries a defensive copy of the settings (constructed via the internal copy ctor).

Subscribers to `ToolkitSettings.Changed`:
- `Helpers.Logging` static ctor — updates `Verbose`/`Performance`.
- `Toolkit.Indexing` static ctor — calls `StartIndexing(Current.Game)` (so toggling `SlowGatheringEnabled` reloads orchestration, as shown in the README advanced hooks example).

The settings are edited through [`SettingsUiTab`](../ui/overview.md).
