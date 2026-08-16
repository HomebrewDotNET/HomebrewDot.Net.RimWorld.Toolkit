---
type: subsystem
title: Testing and Build Overview
description: The build, test, and benchmark infrastructure — product csproj env-var reference resolution, solution structure, central package management, unit and integration test projects, test models, benchmarks, About metadata, CI workflow, and validation commands.
tags: [build, testing, benchmarks, csproj, ci, solution, toolkit-testing]
---

# Testing and Build Overview

The repository builds a single .NET Framework 4.7.2 library mod and ships unit tests, integration tests, and benchmarks in a four-project solution. All RimWorld/Harmony assembly references are resolved from the local game install via overridable environment variables. There is no build/test CI — the only GitHub workflow regenerates this wiki.

## Solution

`HomebrewDot.Net.RimWorld.Toolkit.sln` — Visual Studio 2017+ format, 4 build projects + virtual solution folders (`src`, `tests` → `Unit`/`Integration`, `bench`). All platform variants (`Any CPU`, `x64`, `x86`) alias to `Any CPU`; no separate platform builds.

| Project | Path | Target | Role |
|---------|------|--------|------|
| `HomebrewDot.Net.Rimworld.Toolkit` | `src/HomebrewDot.Net.RimWorld.Toolkit/` | `net472` | Product assembly |
| `HomebrewDot.Net.RimWorld.Toolkit.Tests` | `tests/Unit/.../` | `net472` | xUnit unit tests |
| `HomebrewDot.Net.RimWorld.Toolkit.IntegrationTests` | `tests/Integration/.../` | `net472` | xUnit integration tests |
| `HomebrewDot.Net.RimWorld.Toolkit.Benchmarks` | `bench/.../` | `net472` | BenchmarkDotNet |

## Central package management

`Directory.Packages.props` (`ManagePackageVersionsCentrally=true`): BenchmarkDotNet `0.12.1`, Microsoft.NET.Test.Sdk `16.11.0`, Moq `4.20.72`, xunit `2.4.2`, xunit.runner.visualstudio `2.4.5`. `Directory.Build.props` suppresses `NU1507`.

## Product csproj — reference resolution

`src/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.Rimworld.Toolkit.csproj`:

| MSBuild Property | Env Var | Default (when unset) |
|------------------|---------|----------------------|
| `RimworldVersion` | `RIMWORLD_VERSION` | `1.6` |
| `HarmonyVersion` | `HARMONY_VERSION` | `1.5` |
| `RimworldRoot` | `RIMWORLD_ROOT` | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld` |
| `RimworldWorkshopRoot` | `RIMWORLD_WORKSHOP_ROOT` | `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100` |
| `RimWorldManaged` | `RIMWORLD_MANAGED` | `$(RimworldRoot)\RimWorldWin64_Data\Managed` |
| `HarmonyRoot` | `RIMWORLD_HARMONY_ROOT` | `$(RimworldWorkshopRoot)\2009463077\$(HarmonyVersion)\Assemblies` |
| `RimworldModsRoot` | `RIMWORLD_MODS_ROOT` | `$(RimworldRoot)\Mods` |
| `ModTestingFolderName` | `MOD_TESTING_FOLDER_NAME` | `Homebrewed Toolkit - DEV` |

Assembly references (all `Private=false` — not copied to output): `Assembly-CSharp` (`$(RimWorldManaged)\Assembly-CSharp.dll`), `UnityEngine.CoreModule`, `UnityEngine.IMGUIModule`, `0Harmony` (`$(HarmonyRoot)\0Harmony.dll`).

`OutputPath = ..\..\$(RimworldVersion)\Assemblies` → built `HomebrewDot.Net.Rimworld.Toolkit.dll` lands in `1.6/Assemblies/` at the repo root (the RimWorld mod layout expected by `About/About.xml`).

### SyncModContentToModsFolderForTesting

Runs `AfterTargets="Build"` when `$(RimworldModsRoot)` exists. Detects `rsync` via `where rsync`; skips entirely if not found. Skips UNC/network paths. Converts Windows paths to cygwin-style for rsync. Syncs (with `-a -q --no-perms --delete --mkpath`):
- `About/About.xml` → `<target>/About/`
- `<RimworldVersion>/` → `<target>/`
- `About/$(PublishedFileIdFileName)` → `<target>/About/PublishedFileId.txt` (if exists)
- Optional `Defs/`, `Patches/`, `Languages/`, `Textures/`, `Sounds/` (if source dir exists)

`ModTestingTargetDir = $(RimworldModsRoot)\$(ModTestingFolderName)` (default `Homebrewed Toolkit - DEV`).

### Internal visibility

`Properties/AssemblyInfo.cs` exposes `internal` members to the unit test, integration test, benchmarks projects, and an external `HomebrewDot.Net.RimWorld.DynamicFilters.IntegrationTests` assembly (a downstream consumer not in this solution).

## Unit test project

`tests/Unit/.../HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj`:
- `OutputPath = $(TEMP)\...\` — outputs to the local `%TEMP%` drive to avoid the .NET Framework mapped-drive (J:\) security restriction.
- `app.config`: `<loadFromRemoteSources enabled="true" />`.
- `Properties/AssemblyInfo.cs`: `[assembly: CollectionBehavior(DisableTestParallelization = true)]` — all unit tests run serially.
- Assembly references use **hardcoded** Windows paths for Assembly-CSharp/UnityEngine (an inconsistency vs the integration project's env-var-parameterized paths).
- Package references: Microsoft.NET.Test.Sdk, Moq, xunit, xunit.runner.visualstudio.

Test layout mirrors source namespace structure (`Collecting`, `Comparing`, `Hooks`, `Indexing`, `Referencing`, `Extensions`, `Helpers`, `Collections`, `UI`). Tests use xUnit `[Fact]`/`[Theory]` and Moq for interfaces.

## Integration test project

`tests/Integration/.../HomebrewDot.Net.RimWorld.Toolkit.IntegrationTests.csproj`:
- `OutputPath = $(TEMP)\...\` (same mapped-drive workaround).
- Assembly references use env-parameterized `$(RimWorldManaged)` (consistent with the product project).
- Embeds `AssemblyMetadataAttribute("RimworldLocation", "$(RimworldRoot)")` — bakes the RimWorld install path into the assembly at build time. `CollectionIntegrationTests` reads this at runtime to locate `Data\Core\Defs` for loading real game `ThingDef` XML.
- `Properties/AssemblyInfo.cs`: `DisableTestParallelization = true`.

### Bootstrap pattern

All integration test classes call `Toolkit.ConfigureServices()` (or `ToolkitImpl.ConfigureServices()` via alias `using ToolkitImpl = HomebrewDot.Net.Rimworld.Toolkit;`) in their constructor to populate the service registry.

### xUnit collection

`IndexingIntegrationCollection.cs` — `[CollectionDefinition("IndexingIntegration", DisableParallelization = true)]`. Classes annotated `[Collection("IndexingIntegration")]`: `CollectingBuildIntegrationTests`, `IndexingConfigurationIntegrationTests`, `CollectionIntegrationTests`. This serializes them against each other (they mutate shared `Toolkit.Indexing` static state).

All integration test classes are tagged `[Trait("Category", "Integration")]`.

### Cleanup patterns

| Class | Cleanup |
|-------|---------|
| `HooksIntegrationTests` | `Dispose()` → `ToolkitImpl.Hooks.ReloadManager()` |
| `IndexingConfigurationIntegrationTests` | `Dispose()` → `Toolkit.Indexing.Orchestrator = null`, `Manager = null` |
| `CollectingBuildIntegrationTests` | `Dispose()` → remove all collectors/defs, reset orchestrator/manager, reload comparator |
| `CollectionIntegrationTests` | `Dispose()` → reset orchestrator + manager |
| `ServicesIntegrationTests` | per-test `UnregisterByName<T>()` in finally blocks |
| `ComparingIntegrationTests`, `ReferenceResolverIntegrationTests` | fresh comparator/resolver per test (no shared cleanup) |

## Test models

`src/HomebrewDot.Net.RimWorld.Toolkit/Testing/Models/`:
- `Null` — empty class for filling generic type parameters.
- `Tentity<T>` — generic test entity with every primitive C# field/property type (`int`, `long`, `float`, `double`, `string`, `bool`, `T`) plus array + `List<T>` pairs for each. Every field has a backing property.
- `Tentity : Tentity<Null>` — non-generic convenience alias.

These are shared by unit tests (for reflection/metadata exercises) and integration tests (as concrete entities for the indexing/collecting pipeline).

## Benchmarks

`bench/HomebrewDot.Net.RimWorld.Toolkit/` — `OutputType=Exe`, `BenchmarkDotNet` 0.12.1. `Program.Main` uses `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)`. Post-build `CopyFrameworkNetStandardFacade` copies `netstandard.dll` for BenchmarkDotNet's netstandard dependency. `app.config` has binding redirects for `System.Buffers`, `System.Memory`, etc. and `<loadFromRemoteSources enabled="true" />`.

### CollectorBenchmarks (`bench/.../Collecting/Components/CollectorBenchmarks.cs`)
`[MemoryDiagnoser]`, `Orderer(FastestToSlowest)`, `GroupBenchmarksBy(ByCategory)`. Params: `ItemCount` (100, 1000), `MatchPercent` (50). Setup: `Toolkit.ConfigureServices()`, builds a `ReferenceResolver` + `Comparator` from services, creates mixed matching/non-matching entities, pre-builds 10 `Collector` configs (empty, conditions-only, inclusions-only, conditions+inclusions AND/OR, exclusions-only, combined, inverted inclusion, deep nested 5-level). Categories: `Collect_Config`, `CanCollect_Config`, `Collect_Repeat` (dedup), `Collect_Remove` (flip-flop), `Contains`, `GetAll`, `Lifecycle`. Uses `AlwaysTrueComparator`/`AlwaysFalseComparator` stubs to isolate collector overhead.

### DatabaseBenchmarks (`bench/.../Indexing/Components/DatabaseBenchmarks.cs`)
`[MemoryDiagnoser]`, `Orderer(FastestToSlowest)`. Params: `ItemCount` (100). Setup: creates 5 databases (find/query/update/snapshot/cached) with Name + Group indexes, pre-populated. Methods: `BulkUpsert_WithoutIndexes`, `BulkUpsert_WithIndexes`, `Find_ExistingItem`, `Query_ByIndexedName`, `Upsert_ExistingIndexedItem`, `CreateSnapshot_AfterUpdate`, `ReuseCachedSnapshot_WithoutChanges`.

## About / mod metadata

`About/About.xml`:
- `name`: Homebrewed Toolkit
- `packageId`: `homebrewdot.net.rimworld.toolkit`
- `supportedVersions`: 1.6
- Dependencies: RimWorld Core (`ludeon.rimworld`) + Harmony (`brrainz.harmony`, Workshop ID `2009463077`)
- No load order constraints or incompatibilities.

`About/DevPublishedFileId.txt`: `3766400325` (Steam Workshop DEV published file id; synced as `PublishedFileId.txt` for dev uploads).

## CI

`.github/workflows/openwiki-update.yml` is the **only** workflow. Triggers: `workflow_dispatch` + `schedule: cron "0 8 * * *"` (daily 08:00 UTC). Runner `ubuntu-latest`. Installs `openwiki@0.3.1`, `mermaid@11.16.0`, `jsdom@29.1.1`; runs `openwiki code --update --print` with `OPENWIKI_PROVIDER=openrouter`, `OPENWIKI_MODEL_ID=z-ai/glm-5.2`, LangSmith tracing. Creates a PR on branch `openwiki/update`. All actions pinned by SHA. **No build/test/benchmark CI pipeline exists.**

There is also `.github/instructions/` with three testing-convention files (unit: xUnit + Moq, AAA, `<Method>_<When>_<Expected>` naming; integration: `[Trait("Category","Integration")]`, real dependencies; system: defined but no `tests/System/` exists).

## Validation commands

```bash
# Build the product (outputs 1.6/Assemblies/HomebrewDot.Net.Rimworld.Toolkit.dll)
dotnet build src/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.Rimworld.Toolkit.csproj -c Release

# Run all unit tests
dotnet test tests/Unit/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.Tests.csproj

# Run integration tests (requires RimWorld at RIMWORLD_ROOT)
dotnet test tests/Integration/HomebrewDot.Net.RimWorld.Toolkit/HomebrewDot.Net.RimWorld.Toolkit.IntegrationTests.csproj

# Focused filter by namespace/class
dotnet test tests/Unit/.../Tests.csproj --filter "FullyQualifiedName~Indexing.Components.DatabaseTests"

# Benchmarks
dotnet run -c Release --project bench/HomebrewDot.Net.RimWorld.Toolkit
```

All build/test commands require a RimWorld installation (Steam default path or env var overrides) because `Assembly-CSharp.dll`, `UnityEngine.*.dll`, and `0Harmony.dll` are referenced from the game install directory.
