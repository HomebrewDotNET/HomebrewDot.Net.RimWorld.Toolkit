# Homebrewed Toolkit for RimWorld

## Description
Homebrewed Toolkit is a shared code library for RimWorld mods. It exposes static APIs through Toolkit for hooks, snapshot indexing, data collections, service lookup, and helper utilities.

## Usage
Examples below are based on patterns used in this repository (debug UI and tests).

### Toolkit.Hooks
Use hooks to run code when game events fire.

Basic
```csharp
using HomebrewDot.Net.RimWorld;
using HomebrewDot.Net.RimWorld.Hooks.Triggers;

Toolkit.Hooks.Manager.RegisterHook<OnGameLoadedTrigger>(
	Toolkit.Instance,
	e => Toolkit.Index.StartIndexing(e.Game, takeSnapshot: true)
);
```

Advanced
```csharp
using HomebrewDot.Net.RimWorld;
using HomebrewDot.Net.RimWorld.Hooks.Triggers;

Toolkit.Hooks.Manager
	.RegisterHook<OnSaveLoadedTrigger>(Toolkit.Instance, e => Toolkit.Index.StartIndexing(e.Game, true))
	.RegisterHook<ToolkitSettings.Changed>(Toolkit.Instance, e =>
	{
		if (e.Settings.SlowGatheringEnabled)
		{
			Toolkit.Helpers.Logging.Log("Slow gathering enabled — reloading orchestration.");
			Toolkit.Index.ReloadOrchestration();
		}
	});

Toolkit.Hooks.Manager.UnregisterAllBy<ToolkitSettings.Changed>(Toolkit.Instance);
```

### Toolkit.Index
Use indexing to build read-only snapshots that can be queried outside the main game loop.

Basic
```csharp
using HomebrewDot.Net.RimWorld;
using HomebrewDot.Net.RimWorld.Indexing.Components;
using Verse;

Toolkit.Index.Configure(x => x.With(DefGatherer.Instance));
Toolkit.Index.StartIndexing(Current.Game, takeSnapshot: true);

var snapshot = Toolkit.Index.Manager.DatabaseSnapshot;
```

Advanced
```csharp
using HomebrewDot.Net.RimWorld;
using RimWorld;
using Verse;

Toolkit.Index.ConfigureSchema(builder =>
	builder.WithTable<Def>(
		nameof(Def),
		table => table.WithSubTable<ThingDef>(nameof(ThingDef))
	)
);

Toolkit.Index.ReloadOrchestration();

var thingTable = Toolkit.Index.Manager.DatabaseSnapshot?.GetTable<ThingDef>($"{nameof(Def)}.{nameof(ThingDef)}");
```

### Toolkit.Collecting
Use collecting to define named filters and keep collector sets in sync.

Basic
```csharp
using System;
using System.Collections.Generic;
using HomebrewDot.Net.RimWorld;
using HomebrewDot.Net.RimWorld.Indexing;
using RimWorld;
using Verse;

var table = $"{nameof(Def)}.{nameof(ThingDef)}.Weapon.Ranged";
var getThings = new Func<IReadOnlyDatabase, IEnumerable<IIndexed<ThingDef>>>(s => s.GetTable<ThingDef>(table));

Toolkit.Collecting.Build("Snipers", b => b
	.Compare.Indexed(ToolkitConstants.Stats.Weapon.Def.Range).With.GreaterThanOrEqual().To.Value(30)
	.CollectFromSnapshot(getThings));

Toolkit.Collecting.StartCollection();

var collectors = Toolkit.Collecting.GetAllCollectors();
if (collectors.TryGetValue("Snipers", out var collector))
{
	foreach (IIndexed<ThingDef> item in collector.GetAll())
	{
		Log.Message(item.Value.defName);
	}
}
```

Advanced
```csharp
using System;
using System.Collections.Generic;
using HomebrewDot.Net.RimWorld;
using HomebrewDot.Net.RimWorld.Indexing;
using RimWorld;
using Verse;

var table = $"{nameof(Def)}.{nameof(ThingDef)}.Weapon.Ranged";
var getThings = new Func<IReadOnlyDatabase, IEnumerable<IIndexed<ThingDef>>>(s => s.GetTable<ThingDef>(table));

Toolkit.Collecting.Build("Snipers", b => b
	.Compare.Indexed(ToolkitConstants.Stats.Weapon.Def.Range).With.GreaterThanOrEqual().To.Value(30)
	.CollectFromSnapshot(getThings));

Toolkit.Collecting.Build("ShortRange", b => b
	.Compare.Indexed(ToolkitConstants.Stats.Weapon.Def.Range).With.LessThanOrEqual(15)
	.CollectFromSnapshot(getThings));

Toolkit.Collecting.StartCollection();

var collectors = Toolkit.Collecting.GetAllCollectors();
foreach (var pair in collectors)
{
	Log.Message($"{pair.Key}: {pair.Value.Count} items");
}
```

### Toolkit.Services
Use services to register and resolve shared objects by type or by name.

Basic
```csharp
using HomebrewDot.Net.RimWorld;

Toolkit.Services.Register<IMyService>(new MyService());
var service = Toolkit.Services.GetRequired<IMyService>();
Toolkit.Services.Unregister<IMyService>(service);
```

Advanced
```csharp
using System.Linq;
using HomebrewDot.Net.RimWorld;

Toolkit.Services.Register<IMyService>(new MyService("default"));
Toolkit.Services.Register<IMyService>(new MyService("ui"), "ui");

var uiService = Toolkit.Services.Get<IMyService>("ui");
var allServices = Toolkit.Services.GetAll<IMyService>().ToList();
var namedServices = Toolkit.Services.GetAllNamed<IMyService>();

Toolkit.Services.UnregisterByName<IMyService>("ui");
```

## Contributing
Not accepting direct contributions right now. If you want to build on this project, feel free to fork it.

## License
Licensed under Apache License 2.0. See [LICENSE.md](LICENSE.md).