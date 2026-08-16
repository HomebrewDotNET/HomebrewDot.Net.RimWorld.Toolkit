---
type: subsystem
title: UI Subsystem Overview
description: The in-game debug UI — the dev-mode-gated settings window with snapshot/collection debug tabs, the condition and collection editor windows, the IReferenceTypeInputHelper picker pattern, reusable IMGUI primitives, and the self-contained JSON export utility.
tags: [ui, settings, debug, editor-windows, imgui, toolkit-ui]
---

# UI Subsystem Overview

The UI subsystem (namespaces `HomebrewDot.Net.Rimworld.UI` and sub-namespaces `.UI.Settings`/`.UI.Components`; some debug types in the parent `HomebrewDot.Net.Rimworld` namespace) provides the mod's settings window, in-game debug viewers for [Indexing](../indexing/overview.md) snapshots and [Collecting](../collecting/overview.md) collections, editor windows for building condition/collection definitions, and reusable IMGUI primitives. The settings window is created by the [`Toolkit`](../facades/toolkit.md) constructor and exposed via `GetSettings`.

## Settings window

### `IToolkitSettingsTab` (`UI/Settings/IToolkitSettingsTab.cs`)
Internal interface: `string Title { get; }` and `void Draw(Rect rect)`. The single contract all tabs implement.

### `ToolkitSettingsUi` (`UI/Settings/ToolkitSettingsUi.cs`)
Internal sealed class created by the `Toolkit` constructor. Tabs are **hardcoded** (not DI-discovered):
- `_allTabs = { new SettingsUiTab(), new DebugSnapshotUiTab(), new DebugCollectionsUiTab() }`
- `_nonDevTabs = { _allTabs[0] }` (only the user-facing `SettingsUiTab`)

`Draw(Rect)` picks `Prefs.DevMode ? _allTabs : _nonDevTabs` — the two debug tabs are **dev-mode-gated**. When `tabs.Length > 1`, a 30px tab-header band is drawn at top; content fills below. Index clamping resets to 0 if dev mode toggled off while a debug tab was selected.

### `SettingsUiTab` (`UI/Settings/Tabs/SettingsUiTab.cs`)
Title `"Settings"`. The only user-facing tab. Three checkboxes bound to [`Toolkit.Settings`](../facades/toolkit.md):
- "Slow Gathering" → `SlowGatheringEnabled` (use TickLong instead of TickRare).
- "Verbose Logging" → `Verbose`.
- "Performance Logging" → `PerformanceLogging`.

This tab is the sole trigger for `ToolkitSettings.Changed` (fires on save when a value changes).

## Debug tabs

### `DebugSnapshotUiTab` (`UI/Settings/Tabs/DebugSnapshotUiTab.cs`)
Title `"Debug Snapshot"`. The debug viewer for `Toolkit.Indexing`:
- Reads `Toolkit.Indexing.Manager?.DatabaseSnapshot` (`IReadOnlyDatabase`). If null → "No snapshot available."
- Header: snapshot version + table count.
- Buttons: "Load Debug Tables" (calls `StartIndexing(Current.Game, true)`), "Export Tables" (`DebugExportUtility.ExportSnapshotTableSet`).
- Lists all tables (ordered by `FullName`, sub-tables nested with depth indentation). Click opens `SnapshotTableDetailsWindow`.
- `TryCount` best-effort counts by enumerating.

### `DebugCollectionsUiTab` (`UI/Settings/Tabs/DebugCollectionsUiTab.cs`)
Title `"Debug Collections"`. The debug entry point for `Toolkit.Collecting`:
- Pulls `GetAllDefinitions()` and `GetAllCollectors()`.
- Buttons: "Load Debug Collections" (builds "Snipers" range ≥ 30 and "ShortRange" range ≤ 15 demo collections via `CollectFromSnapshot` from the ranged-weapon table, then `StartCollection`), "Restart Collection" (`StartCollection`), "Export Collections" (`DebugExportUtility.ExportCollections`).
- Lists collections with condition/inclusion/exclusion counts and collected count. Click opens `CollectionDetailsWindow`.

## Detail windows

### `SnapshotTableDetailsWindow` (`UI/Settings/Tabs/SnapshotTableDetailsWindow.cs`)
`internal class` (non-sealed, designed for subclassing; `GetDisplayName`/`GetDetails`/`GetIcon` are `protected virtual`). Displays an `IReadOnlyTable`:
- Back-navigation (`Stack<IReadOnlyTable>`), search field, order-by-path field.
- Caps at `MaxDisplayedRows = 500`.
- Filters by case-insensitive substring on display name/preview/type; sorts by `GetSortValue` traversing `row.Value` along the order-by path (via `Toolkit.Helpers.Traversing`).
- Row adaptation (`AdaptRow`): unwraps `KeyValuePair<,>.Value`, coerces `IIndexed<object>` or generic `IIndexed<>` into a uniform adapter (`IndexedRowAdapter`/`ReflectiveIndexedRow`) exposing `Value`, `Metadata`, `GetValue<T>`.
- Sub-tables shown in an `IconGrid<IReadOnlyTable>`; rows in an `IconGrid<IIndexed<object>>`.

### `CollectionDetailsWindow` (`UI/Settings/Tabs/CollectionDetailsWindow.cs`)
`internal sealed`. Displays a named collection:
- Stats: conditions/inclusions/exclusions/collected counts.
- Buttons: Refresh, Restart Collection, Export Collection, Copy Rules (writes rule lines to clipboard).
- "Collection Rules" scrollable list + "Collected Items" `IconGrid<object>`.
- `GetIcon`: prefers `ThingDef.uiIcon`/`Thing.def.uiIcon`/`BuildableDef.uiIcon`, then reflective `"uiIcon"/"Icon"/"icon"/"Texture"/"texture"`, fallback `BaseContent.BadTex`.

## Editor windows

### `ConditionDefEditorWindow` (`UI/Components/ConditionDefEditorWindow.cs`)
`public sealed : Window`. Edits a [`ConditionDefConfig`](../comparing/overview.md) — the "Compare … With … To" triple. Pulls DI dictionaries: `Toolkit.Services.GetAllNamed<IReferenceType>()`, `GetAllNamed<IReferenceTypeInputHelper>()`, `GetAllNamed<IOperatorType>()`.

Rows: Compare (Default/Reference mode toggle), With (operator text + "..." picker), To (Default/Reference toggle; default mode uses `ConstantInputField`), IsOr checkbox, Inverted checkbox. Reference mode shows a "Pick" button → `SelectionWindow<string>` over reference-type names; if an `IReferenceTypeInputHelper` exists for the type, a `>` button opens the helper's picker window. Validation requires operator + operands; Save invokes `onSave`.

### `CollectionDefConfigEditorWindow` (`UI/Components/CollectionDefConfigEditorWindow.cs`)
`public sealed : Window`. Edits a [`CollectionDefConfig`](../collecting/overview.md) — conditions, inclusions, exclusions, `InclusionsAreOr`. Three scrollable sections each with Add/Edit/Delete per row. Condition rows open `ConditionDefEditorWindow`; inclusion/exclusion rows open `CollectionConditionDefConfigEditorWindow`. `excludeCollectionName` prevents self-referential inclusions/exclusions. No validation on save (unlike the condition editor).

### `CollectionConditionDefConfigEditorWindow` (`UI/Components/CollectionConditionDefConfigEditorWindow.cs`)
`public sealed : Window`. Edits a single `CollectionConditionDefConfig` (a reference to another collection by name, optionally with a "By" qualifier). Name field + "..." picker (reads `Toolkit.Collecting.GetAllDefinitions()`, filters out self and parent). By, IsOr, Inverted. Validation requires Name.

## Input helper pattern

### `IReferenceTypeInputHelper` (`UI/IReferenceTypeInputHelper.cs`)
```csharp
public interface IReferenceTypeInputHelper
{
    Window GetInputWindow(string name, IReferenceType referenceType, Action<string> onSelected);
}
```
Decouples editor windows from the specifics of picking a value for a given `IReferenceType`. Helpers are singletons registered in `Toolkit.Services` by the reference-type name. `ConditionDefEditorWindow` is the sole consumer.

### Implementations

| Helper | Registered under | Picks |
|--------|-------------------|-------|
| `CompReferenceTypeInputHelper` | `"Comp"` | Two-step: comp type (`SelectionWindow<CompTypeOption>` over concrete `ThingComp`/`CompProperties` subclasses), then property on that comp (`SelectionWindow<PropertyOption>` including "(whole comp)"). `BuildValue` produces `"TypeName"` or `"TypeName|MemberName"` using `CompReferenceType.PathSeparator`. |
| `DefReferenceTypeInputHelper<T>` | `"ThingCategoryDef"`, `"StuffCategoryDef"` | `SelectionWindow<T>` over `DefDatabase<T>.AllDefsListForReading` ordered by label; `onSelected(defName)`. |
| `StateReferenceTypeInputHelper` | `"Stat"` | `SelectionWindow<StatDef>` over `DefDatabase<StatDef>`; `onSelected(defName)`. (Named "State" but selects `StatDef`s — a naming inconsistency.) |

Registration in `Toolkit.ConfigureServices` also registers UI input helpers for `Stat`, `ThingCategoryDef`, `StuffCategoryDef`, `Comp`. See [Toolkit facade](../facades/toolkit.md).

## IMGUI primitives

### `Grid<T>` (`UI/Components/Grid.cs`)
Generic scrollable grid. Configurable `DrawContent`/`GetTooltip`/`CellWidth`/`CellHeight`/`CellGap`, events `OnClick`/`OnRightClick`. `Draw(Rect, ref scroll, items)` computes `cellsPerRow`, ScrollView, per-cell `DrawMenuSection` + hover highlight + tooltip + click. Foundational primitive used by `SelectionWindow`.

### `IconGrid<T>` (two distinct types)
- `HomebrewDot.Net.Rimworld.UI.Components.IconGrid<T>` — extends `Grid<T>`, square cells, `DrawIcon` delegate. `FromTexture` static factory. Used by editor windows.
- `HomebrewDot.Net.RimWorld.IconGrid<T>` (top-level, sealed, **not** a `Grid<T>` subclass, namespace capital-W) — standalone reimplementation with delegate properties. Used by the debug detail windows. A notable duplication.

### `SelectionWindow<T>` (`UI/Components/SelectionWindow.cs`)
`public sealed : Window`. The reusable picker. Single or multiple selection, optional filtering. Constructor wires `optionsGrid.OnClick`/`selectedGrid.OnClick`. `RebuildFilteredOptions` uses custom `filterPredicate` or `DefaultFilter` (case-insensitive `IndexOf` on `getFilterStrings` or `ToString`). `InitialSize => (980, 720)`.

### `ConfirmWindow` (`UI/Components/ConfirmWindow.cs`)
`public sealed : Window`. Title + scrollable message + Cancel/Confirm. `onConfirm` required. Height clamped to message.

### `ConstantInputField` (`UI/Components/ConstantInputField.cs`)
`public class`. Typed constant editor with three modes (Text/Number/Decimal) via T/N/D buttons. Buffers for `Widgets.TextFieldNumeric`. `SyncBuffers` re-seeds after external value changes. Used by `ConditionDefEditorWindow` for the "To" default-mode field.

### `ConstantType` (`UI/ConstantType.cs`)
`public enum`: `Text = 0`, `Number = 1`, `Decimal = 2`. Pinned by tests.

## DebugExportUtility

`DebugExportUtility` (`UI/Settings/Tabs/DebugExportUtility.cs`) — `internal static`. Exports [Indexing](../indexing/overview.md) tables and [Collecting](../collecting/overview.md) collections to JSON files under `SaveDataFolderPath/ToolkitExports/<category>/`. File names are timestamped + sanitized. Posts RimWorld `Messages.Message` on success/failure.

Includes a **hand-written JSON serializer** (no `System.Text.Json` dependency) handling null/string (with escaping)/bool/numerics/`IDictionary`/`IEnumerable`. Payloads: table (`{name, fullName, rowCount, rows[]}`), table set (`{version, tableCount, tables[]}`), collections (`{name, hasDefinition, hasCollector, conditions[], items[]}`). Row adaptation mirrors `SnapshotTableDetailsWindow.AdaptRow`.

## Focused tests

- **`ConstantTypeTests`**: pins `Text=0`/`Number=1`/`Decimal=2` and their `ToString()` values.
- **`ConditionDefEditorWindowTests`** / **`CollectionDefConfigEditorWindowTests`** / **`CollectionConditionDefConfigEditorWindowTests`**: constructor accepts `TypeInitializationException`/`SecurityException` (RimWorld `Window` requires a live game context via `SoundDefOf`) or succeeds; `onSave` null guard (`ArgumentNullException`); null lists tolerated by collection editor.
- **`CompReferenceTypeInputHelperTests`**: `ScanCompTypes` returns only concrete `ThingComp`/`CompProperties` subclasses; includes `CompGlower` and `CompProperties_Explosive`; `BuildValue` produces `"CompGlower"` or `"CompProperties|compClass"`; `Traversing.GetMembers(CompProperties)` includes `compClass`.
- **`DefReferenceTypeInputHelperTests`**: per-generic-type singleton (`Assert.Same`); `GetInputWindow` may throw `TypeInitializationException`/`SecurityException` (game-context dependency) or succeed.
- **`ConstantInputFieldTests`**: constructor (default + values) and `SyncBuffers` are side-effect-free (no GUI statics touched); numeric buffers seeded invariantly.

> **Testability**: all `Window`-derived tests accept `TypeInitializationException`/`SecurityException` because RimWorld `Window` construction requires a live game (`SoundDefOf`). Pure-logic tests exist only for `ConstantType`, `CompReferenceTypeInputHelper` (static methods), `DefReferenceTypeInputHelper` (singleton), and `ConstantInputField` (constructor/buffer sync).

## Validation

```bash
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj --filter "FullyQualifiedName~UI"
```
