---
type: subsystem
title: Generic Infrastructure Overview
description: Cross-cutting contracts and utilities — ICacheable/IPoolable/IHandler, Toolkit.Pool and Toolkit.Cache, NullDictionary singleton, pooled collections, the pending-work coroutine system (PendingWork and SyncPendingWork), Callback eventing, and extension methods.
tags: [generic, pooling, caching, null-dictionary, pending-work, extensions, toolkit-generic]
---

# Generic Infrastructure Overview

The Generic subsystem (namespace `HomebrewDot.Net.Rimworld.Generic`, components in `.Generic.Components`, models in `.Generic.Models`; extensions in `HomebrewDot.Net.Rimworld.Extensions`; eventing in `HomebrewDot.Net.Rimworld.Eventing.Models`) provides the cross-cutting contracts and utilities every other subsystem builds on. It defines the pooling/caching interfaces consumed by [`Toolkit.Pool<T>`/`Toolkit.Cache<TKey,TValue>`](../facades/toolkit.md), the null-object dictionary used as a zero-allocation fallback, the coroutine-based pending-work engine that powers [cooperative work](../hooks/overview.md), and the cache-key extension that feeds the [Comparator's](../comparing/overview.md) compiled cache.

## Contracts

### `ICacheable` (`Generic/ICacheable.cs`)
```csharp
public interface ICacheable { string GetCacheKey(); }
```
<!-- openwiki: broken internal link [#objectextensions] heading anchor "objectextensions" does not exist in /openwiki/generic/overview.md. Fix the href or restore the target, then delete this comment. -->
Implemented by `IReference`, `IOperator`, `ConditionDef`, `CollectionDef`, `StaticCollectionDef`. Consumed by [`ObjectExtensions.ToCacheKey`](#objectextensions) and by the `Comparator`/`CollectionComparator` compiled-delegate caches keyed by the returned string.

### `IPoolable` (`Generic/IPoolable.cs`)
```csharp
public interface IPoolable { void Reset(); }
```
Called by `Toolkit.Pool<T>.Return` before re-enqueuing. Implementors: `SyncPendingWork<T>` (`Reset` → `Clear`), `PooledCollection<TCollection,TElement>` (`Reset` → `Collection.Clear`), `RaiseCooperativeWork<T>` (clears four internal fields).

### `IHandler` (`Generic/IHandler.cs`)
```csharp
public interface IHandler { byte Priority { get; } }
```
Base interface for hook ordering. `IHook<T> : IHandler`. [`HookManager`](../hooks/overview.md) sorts by `Priority` ascending (lower byte = earlier execution). `CooperativeWorkManager` uses `byte.MinValue`.

### Pending-work contracts (`Generic/IPendingWork.cs`, `Generic/ISyncPendingWork.cs`)

Two designs coexist:

**`IPendingWork<T>`** (older, Task/TCS-based):
```csharp
Task Work { get; }
T Context { get; }
Task Start(Func<Task> startWork, T context);
Task Continue();
Task Yield();
```
Sole implementor: `PendingWork<T>`. The work coroutine awaits `Yield()`; the tick loop calls `Continue()` to release the awaiter.

**`ISyncWork` / `ISyncPendingWork<in T>` / `ISyncRunningWork<out T>`** (current, IEnumerator-based):
```csharp
public interface ISyncWork { bool IsFinished { get; } bool Continue(); }
public interface ISyncPendingWork<in T> : ISyncWork {
    bool IsStarted { get; }
    bool Start(IEnumerator work, T context);
    void Clear();
}
public interface ISyncRunningWork<out T> : ISyncWork { T Context { get; } }
```
`SyncPendingWork<T>` implements both plus `IPoolable`. This is the design used by [`CooperativeWorkManager`](../hooks/overview.md).

## Pending-work implementations

### `PendingWork<T>` (`Generic/Components/PendingWork.cs`)
Task-based. Uses a single `TaskCompletionSource<bool>` swapped by `Yield()`/`Continue()`. `Start` throws `InvalidProgramException` if already started. `Continue` completes the current handle to unblock the awaiter. This is the legacy design; the production cooperative-work engine uses `SyncPendingWork<T>`.

### `SyncPendingWork<T>` (`Generic/Components/SyncPendingWork.cs`)
The IEnumerator coroutine stepper. Implements `ISyncPendingWork<T>`, `ISyncRunningWork<T>`, `IPoolable`.

**Configurable delegates** (set by `RaiseCooperativeWork.From<T>`):
- `Func<T, TimeSpan?> timeoutSelector` — per-tick time budget.
- `Func<T, Stopwatch> trackerSelector` — elapsed-time tracker.

**State**: `Stack<IEnumerator> _pendingWork` (capacity 8); `_currentFinished = true` (init finished so `Start` won't throw on first use).

**`Start(IEnumerator work, T context)`**: throws `InvalidOperationException` if `!_currentFinished`. Sets `Context`, pushes `work`, calls `Continue()` immediately.

**`Continue()`** — the core stepper:
1. If finished, return `true`.
2. Read timeout + tracker from selectors.
3. While stack non-empty: if `tracker.Elapsed >= timeout` → return `false` (budget exhausted). Peek; if `MoveNext()`: if `Current is IEnumerable` → push its enumerator (auto-recurse); if `IEnumerator` → push; else → `return false` (yielded a non-enumerator, e.g. `yield return null`). Else → `Pop()`.
4. Stack empty → `_currentFinished = true`, return `true`.

**`Clear()` / `IPoolable.Reset()`**: resets `_currentFinished = true`, clears stack, `Context = default` (makes `IsStarted` false), `IsFinished = false`.

Invariants: nested `IEnumerable`/`IEnumerator` yields auto-expand; `IsStarted` is `Context != null`; `IsFinished` only flips true inside `Continue`.

## NullDictionary

`NullDictionary<TKey, TValue>` (`Generic/Models/NullDictionary.cs`) is a null-object singleton implementing both `IDictionary` and `IReadOnlyDictionary`. `Instance` (private constructor).

| Behavior | Result |
|----------|--------|
| `this[key].get` | `default(TValue)` |
| `Count` | `0`; `IsReadOnly` → `true` |
| `Keys`/`Values` | `Array.Empty<...>()` |
| `ContainsKey`/`TryGetValue`/`Contains` | `false` + `default` |
| `Add`/`set`/`Clear`/`Remove` | no-op / `false` |
| `GetEnumerator` | `Enumerable.Empty<...>().GetEnumerator()` |
| `CopyTo` | no-throw no-op |

Used by `Toolkit.Services.GetAllNamed<T>()` as a zero-allocation empty fallback so callers iterate without null-checks. See [Toolkit facade](../facades/toolkit.md).

## Pooled collections

`PooledCollection<TCollection, TElement>` (`Generic/Models/PooledCollection.cs`) — base wrapper `where TCollection : ICollection<TElement>`. Implements `IPoolable`. `Reset()` → `Collection.Clear()` (retains capacity). `Collection` is the wrapped instance.

| Subclass | Wraps | Ctor |
|----------|-------|------|
| `PooledList<T>` | `List<T>` | `: base(new List<T>())` |
| `PooledHashSet<T>` | `HashSet<T>` | `: base(new HashSet<T>())` |

Used via `Toolkit.Pool<PooledList<T>>.Rent()`/`Return(...)`. Production example: `Database` snapshot diffing rents a `PooledList<string>` for keys-to-remove, adds keys, returns it (the `List<T>` is reused across snapshot rounds, avoiding per-tick allocation). `Return` calls `Reset()` → `Clear()` before re-enqueue.

## Callback eventing

`Callback<TArgs>`, `Callback<TArgs1,TArgs2>`, `Callback<TArgs1,TArgs2,TArgs3>` (`Eventing/Models/Callback.cs`) — thin wrappers over `event Action<...>` with `HasSubscribers`, `SubscriberCount`, `Invoke(...)`. The 2-arg version has custom add/remove (first `+=` on null avoids multicast; remove of a reference-equal-to-entire-multicast clears the field).

> `Callback` is imported by `CooperativeWorkManager`'s namespace but not directly consumed there — the cooperative-work system uses plain `Action onCompleted` and `RaiseCooperativeWork.OnCompleted`/`Chain`.

## Host infrastructure (in Toolkit.cs)

### `Toolkit.Pool<T>` where `T : IPoolable, new()`
`Queue<T>` (initial capacity 1024, per-`T` static, not thread-safe). `Rent()` → dequeue or `new T()`. `Return(value)` → `Guard.NotNull` + `value.Reset()` + enqueue. Rented objects are never reset (caller gets a fresh or previously-reset instance); returned objects are always reset first.

### `Toolkit.Cache<TKey, TValue>`
`ConcurrentDictionary` (per key/value pair static). `GetOrSet(key, factory, expensive=false)`: non-expensive → `GetOrAdd`; expensive → double-check `TryGetValue` inside `GetOrAdd` to reduce duplicate construction. `Invalidate(key)` → `TryRemove`.

> This is **distinct** from `ICacheable.GetCacheKey()`: `ICacheable` feeds string-keyed compiled-comparison caches (`Dictionary<string, Func<...>>`); `Cache<TKey,TValue>` is a reflection/lambda compilation cache (e.g. `Helpers.Comp` type lookups).

## Extension methods

### `EnumerableExtensions` (`Extensions/EnumerableExtensions.cs`)
| Method | Behavior |
|--------|----------|
| `Enumerate<T>(IEnumerable<T>)` | Identity passthrough (disambiguates multiple `IEnumerable<T>`). |
| `Enumerate(IEnumerable)` → `IEnumerable<object>` | Non-generic → object sequence; null-safe. |
| `TryEnumerate<T>(object, out IEnumerable<T>)` | Type-tolerant try-cast; strings excluded; `IEnumerable<T>` → typed; `IEnumerable` → `OfType<T>`. |
| `IsCollection(object)` | null/string → false; else `is IEnumerable`. |
| `ToDictionarySafe<T,TKey,TValue>(...)` | Builds dict ignoring duplicate keys (first wins). |

### `ObjectExtensions` (`Extensions/ObjectExtensions.cs`)
`StringBuilder ToCacheKey(this object, StringBuilder, bool includeTypeNames)` — recursive deterministic cache-key builder. Branches: null → `"null"`; `ICacheable` → `GetCacheKey()`; `KeyValuePair<string,object>` → `{key: value}`; collection → `[elem0, elem1, ...]`; else → `ToString()`. If `includeTypeNames`, prepends `(Type.FullName)`. Consumers: `OperatorDef.GetCacheKey`, `ReferenceDef.GetCacheKey`, `ConditionDef.ToString`.

### `TypeExtensions` (`Extensions/TypeExtensions.cs`)
| Method | Behavior |
|--------|----------|
| `GetInheritanceDistance(Type, Type baseType)` | Steps up `BaseType` chain; direct subclass = 1, same = 0, unrelated or downward = -1. |
| `GetActualType(Type)` | Unwraps `Nullable<T>` to `T`. |
| `IsCollection(Type)` | `typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)`. |

## Focused tests

- **`NullDictionaryTests`** (18 tests): singleton identity (`Assert.Same`); `Count=0`, `IsReadOnly=true`; keys/values empty; indexer get returns default; indexer set no-op; `ContainsKey`/`TryGetValue` false; `Add`/`Remove`/`Clear` no-op; `Contains` false; generic/non-generic enumeration empty; `CopyTo` no-throw; `IReadOnlyDictionary` views empty.
- **`EnumerableExtensionsTests`**: `Enumerate` generic identity; non-generic null/array; `TryEnumerate` matching type, string exclusion, null, empty array; `IsCollection` array.
- **`TypeExtensionsTests`**: `GetInheritanceDistance` null guards; same type = 0; direct subclass = 1; two levels = 2; unrelated = -1; `object` root depth; system types; base→child = -1 (upward-only).

No unit tests exist for `ObjectExtensions`, `TypeExtensions.GetActualType`/`IsCollection`, `ToDictionarySafe`, `PooledCollection`/`PooledList`/`PooledHashSet`, `PendingWork`/`SyncPendingWork`, or `Callback` directly — these are exercised transitively via [Indexing](../indexing/overview.md) and [Hooks](../hooks/overview.md) tests.

## Validation

```bash
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj --filter "FullyQualifiedName~Collections.Models"
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj --filter "FullyQualifiedName~Extensions"
```
