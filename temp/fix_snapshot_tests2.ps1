$path = "j:\Cloud\Development\Mods\Rimworld\HomebrewDot.Net.RimWorld.Toolkit\tests\Unit\HomebrewDot.Net.RimWorld.Toolkit\Indexing\Components\SnapshotManagerTests.cs"
$content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

# Remove bool changed parameter from Push calls (the 2nd arg)
# Pattern: sut.Push("hello", false, default) -> sut.Push("hello", default)
# Pattern: sut.Push("hello", true, default) -> sut.Push("hello", default)
# Pattern: sut.Push<string>(null, false, default) -> sut.Push<string>(null, default)
$count = 0

# Replace specific patterns
$old1 = 'sut.Push<string>(null, false, default)'
$new1 = 'sut.Push<string>(null, default)'
if ($content.Contains($old1)) { $count++; $content = $content -replace [regex]::Escape($old1), $new1 }

$old2 = 'sut.Push<string>(null, false, (IReadOnlyDictionary<string, object>)null'
$new2 = 'sut.Push<string>(null, default'
if ($content.Contains($old2)) { $count++; $content = $content -replace [regex]::Escape($old2), $new2 }

# Pattern: Push("hello", false, default)
$content = $content -replace 'sut\.Push\("hello", false, default\)', 'sut.Push("hello", default)'

# Pattern: Push("hello", true, default)
$content = $content -replace 'sut\.Push\("hello", true, default\)', 'sut.Push("hello", default)'

# Pattern: Push("hello", false, (IReadOnlyDictionary<string, object>)null)
$content = $content -replace 'sut\.Push\("hello", false, \(IReadOnlyDictionary<string, object>\)null\)', 'sut.Push("hello", default)'

# Pattern: Push("hello", true, (IReadOnlyDictionary<string, object>)null)
$content = $content -replace 'sut\.Push\("hello", true, \(IReadOnlyDictionary<string, object>\)null\)', 'sut.Push("hello", default)'

# Pattern: Push("hello", false, (IReadOnlyDictionary<string, object>)metadata)
$content = $content -replace 'sut\.Push\("hello", false, \(IReadOnlyDictionary<string, object>\)metadata\)', 'sut.Push("hello", metadata)'

# Pattern: sut.Push("hello", false,
$content = $content -replace 'sut\.Push\("hello", false,`r?`n', 'sut.Push("hello", '

# Pattern: sut.Push<string>(null, false, new KeyValuePair...
$content = $content -replace 'sut\.Push<string>\(null, false,', 'sut.Push<string>(null, '

# Pattern: sut.Push("hello", false, new KeyValuePair...
$content = $content -replace 'sut\.Push\("hello", false,', 'sut.Push("hello", '

# Pattern: sut.Push("hello", false, ("key"...
$content = $content -replace 'sut\.Push\("hello", false, \(', 'sut.Push("hello", ('

Write-Host "Cleaned up $count Push calls"
[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
Write-Host "Done"
