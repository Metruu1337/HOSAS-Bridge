$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$catalogs = @{}
foreach ($language in @('en','pl')) {
    $path = Join-Path $root "src/HOSASBridge.App/Localization/$language.json"
    $doc = [System.Text.Json.JsonDocument]::Parse([IO.File]::ReadAllText($path))
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($property in $doc.RootElement.EnumerateObject()) {
        if (-not $seen.Add($property.Name)) { throw "Duplicate key: $language / $($property.Name)" }
        if ([string]::IsNullOrWhiteSpace($property.Value.GetString())) { throw "Empty translation: $($property.Name)" }
        $original = [regex]::Matches($property.Name, '\{\d+(?:[^}]*)\}') | ForEach-Object Value | Sort-Object
        $translated = [regex]::Matches($property.Value.GetString(), '\{\d+(?:[^}]*)\}') | ForEach-Object Value | Sort-Object
        if (($original -join '|') -ne ($translated -join '|')) { throw "Placeholder mismatch: $($property.Name)" }
    }
    $catalogs[$language] = $seen
    $doc.Dispose()
}
if (-not $catalogs.en.SetEquals($catalogs.pl)) { throw 'English/Polish keys differ' }
foreach ($file in Get-ChildItem (Join-Path $root 'src/HOSASBridge.App') -Filter '*.xaml' -Recurse) {
    foreach ($match in [regex]::Matches([IO.File]::ReadAllText($file.FullName), '\{loc:Text (?:''([^'']+)''|([^}]+))\}')) {
        $key = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { $match.Groups[2].Value }
        if (-not $catalogs.en.Contains($key)) { throw "Missing UI translation: $key" }
    }
}
"Validated $($catalogs.en.Count) bilingual keys, placeholders and XAML labels."
