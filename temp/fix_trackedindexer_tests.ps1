$path = "j:\Cloud\Development\Mods\Rimworld\HomebrewDot.Net.RimWorld.Toolkit\tests\Unit\HomebrewDot.Net.RimWorld.Toolkit\Indexing\Models\TrackedIndexerTests.cs"
$content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

# Replace NullDictionary references with default
$content = $content -replace [regex]::Escape("NullDictionary<string, object>.Instance"), "default"

# Replace builder.Set("Key" with builder.Set(IndexMetadataKey.Get("Key"
# Need to be careful with pattern: builder.Set("X", ...) -> builder.Set(IndexMetadataKey.Get("X"), ...)
$content = $content -replace 'builder\.Set\("([^"]+)",', 'builder.Set(IndexMetadataKey.Get("$1"),'

# Replace builder.Requires("Key" with builder.Requires(IndexMetadataKey.Get("Key"
$content = $content -replace 'builder\.Requires\("([^"]+)",', 'builder.Requires(IndexMetadataKey.Get("$1"),'

# Replace new Dictionary<string, object>() in Index calls with default(IndexMetadata)
# But only where used for insertMeta parameter
$content = $content -replace [regex]::Escape('sut.Index(new Mock<IDatabase>().Object, new Dictionary<string, object>(), writeableMock.Object)'), 'sut.Index(new Mock<IDatabase>().Object, default(IndexMetadata), writeableMock.Object)'

# Replace the metadata-aware watcher test
$oldMetaAware = @'
builder.Set("MetaValue", (s, m) => m.TryGetValue("source", out var src) ? $"{src}:{s}" : s, watchForChanges: true);

            var current = "test";
            var previous = CreateIndexed("test", new Dictionary<string, object> { ["MetaValue"] = "oldSource:test" });
            var insertMeta = new Dictionary<string, object> { ["source"] = "newSource" };
'@
$newMetaAware = @'
builder.Set("MetaValue", (s, m) => m.TryGetValue<string>(IndexMetadataKey.Get("source"), out var src) ? $"{src}:{s}" : s, watchForChanges: true);

            var current = "test";
            var previous = CreateIndexed("test", new Dictionary<string, object> { ["MetaValue"] = "oldSource:test" });
            var insertMeta = default(IndexMetadata);
'@
$content = $content -replace [regex]::Escape($oldMetaAware), $newMetaAware

Write-Host "Done"
[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
