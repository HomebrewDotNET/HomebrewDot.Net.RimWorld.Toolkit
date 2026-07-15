$path = "j:\Cloud\Development\Mods\Rimworld\HomebrewDot.Net.RimWorld.Toolkit\tests\Unit\HomebrewDot.Net.RimWorld.Toolkit\Indexing\Components\SnapshotManagerTests.cs"
$content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

# Replace It.IsAny<IReadOnlyDictionary<string, object>>() with It.IsAny<IndexMetadata>()
$count = [regex]::Matches($content, [regex]::Escape("It.IsAny<IReadOnlyDictionary<string, object>>()")).Count
Write-Host "Replacing $count It.IsAny<IReadOnlyDictionary> refs"
$content = $content -replace [regex]::Escape("It.IsAny<IReadOnlyDictionary<string, object>>()"), "It.IsAny<IndexMetadata>()"

# Replace captured variable declarations
$content = $content -replace [regex]::Escape("IReadOnlyDictionary<string, object> captured = null;"), "IndexMetadata captured = default;"

# Replace dictionary metadata construction in Destroyed_WithMetadata
$oldStr1 = 'var metadata = new Dictionary<string, object> { ["reason"] = "despawned" };'
$newStr1 = 'var metadata = new IndexMetadata();
            metadata.Set(IndexMetadataKey.Get("reason"), "despawned");'
$content = $content -replace [regex]::Escape($oldStr1), $newStr1

# Replace dictionary metadata construction in Push_Dict_WhenMetadataDiffers_Skips
$oldStr2 = 'var metadata = new Dictionary<string, object> { ["k1"] = "new" };'
$newStr2 = 'var metadata = default(IndexMetadata);'
$content = $content -replace [regex]::Escape($oldStr2), $newStr2

# Replace Callback captures
$content = $content -replace [regex]::Escape("Callback<string, IReadOnlyDictionary<string, object>>"), "Callback<string, IndexMetadata>"

Write-Host "Done"
[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
