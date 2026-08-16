# Homebrewed Toolkit for RimWorld

## Description

Homebrewed Toolkit is a shared code library for RimWorld 1.6 mods (targets .NET Framework 4.7.2). It is not a content mod. Other mods reference its assembly and use the static `Toolkit` facade for:

- **Hooks** — run code when game events fire
- **Indexing** — build read-only snapshots of game data that can be queried outside the main game loop
- **Collecting** — define named collections of objects that match conditions
- **Services** — register and resolve shared objects by type or name

The examples below follow patterns from Homebrewed Dynamic Filters, a mod built on this toolkit.

## Installation

### Players

Install like any other mod and enable "Homebrewed Toolkit" in the mod list. Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).

### Mod developers

Reference the built assembly from `1.6/Assemblies/` and ship it with your mod:

```xml
<ItemGroup>
  <Reference Include="HomebrewDot.Net.Rimworld.Toolkit">
    <HintPath>path\to\HomebrewDot.Net.Rimworld.Toolkit.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

## Usage

All public APIs live under the `HomebrewDot.Net.Rimworld` namespace.

### Hooks

Use hooks to run code when game events fire.

```csharp
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Hooks.Triggers;

Toolkit.Hooks.Manager.RegisterHook<OnGameLoadedTrigger>(Toolkit.Instance, e =>
{
    Toolkit.Indexing.StartIndexing(e.Game, takeSnapshot: true);
});
```

### Indexing

Index game data into read-only snapshots that are safe to read from background threads.

```csharp
using HomebrewDot.Net.Rimworld;
using Verse;

Toolkit.Indexing.Def.EnsureGatherer();
Toolkit.Indexing.Def.Thing.EnsureTable();
Toolkit.Indexing.Thing.TrackMap();
Toolkit.Indexing.ReloadOrchestration();

var thingDefs = Toolkit.Indexing.Manager.DatabaseSnapshot?.GetTable<ThingDef>(
    Toolkit.Indexing.Def.Thing.FullTableName);
```

Define your own tables and metadata indexers:

```csharp
using HomebrewDot.Net.Rimworld;
using Verse;

Toolkit.Indexing.ConfigureSchema += b => b.WithTable<ThingDef>(nameof(ThingDef));

Toolkit.Indexing.Indexers.BuildIndexer<ThingDef>("Map", x =>
    x.Include<Map>(ToolkitConstants.Thing.Map, true));

Toolkit.Indexing.ReloadOrchestration();
```

### Collecting

Define a named collection, start it, then read the matches:

```csharp
using System;
using System.Collections.Generic;
using HomebrewDot.Net.Rimworld;
using HomebrewDot.Net.Rimworld.Indexing;
using Verse;

var table = $"{nameof(Def)}.{nameof(ThingDef)}.Weapon.Ranged";
var getThings = new Func<IReadOnlyDatabase, IEnumerable<IIndexed<ThingDef>>>(
    s => s.GetTable<ThingDef>(table));

Toolkit.Collecting.Build("Snipers", b => b
    .Compare.Indexed(ToolkitConstants.Stats.Weapon.Def.Range).With.GreaterThanOrEqual().To.Value(30)
    .CollectFromSnapshot(getThings));

Toolkit.Collecting.StartCollection();

if (Toolkit.Collecting.GetAllCollectors().TryGetValue("Snipers", out var collector))
{
    foreach (IIndexed<ThingDef> item in collector.GetAll())
    {
        Log.Message(item.Value.defName);
    }
}
```

Remove a collection when it is no longer needed:

```csharp
Toolkit.Collecting.Remove("Snipers");
```

### Services

Register and resolve shared objects by type or by name:

```csharp
using HomebrewDot.Net.Rimworld;

Toolkit.Services.Register<IMyService>(new MyService());
var service = Toolkit.Services.GetRequired<IMyService>();
Toolkit.Services.Unregister<IMyService>(service);

Toolkit.Services.Register<IMyService>(new MyService("ui"), "ui");
var uiService = Toolkit.Services.Get<IMyService>("ui");
var allNamed = Toolkit.Services.GetAllNamed<IMyService>();
Toolkit.Services.UnregisterByName<IMyService>("ui");
```

## Contributing

Not accepting direct contributions right now. Feel free to fork.

## License

Licensed under Apache License 2.0. See [LICENSE.md](LICENSE.md).
