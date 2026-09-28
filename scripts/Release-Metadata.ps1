param([string]$Root = (Split-Path $PSScriptRoot -Parent), [string]$Commit='uncommitted-local', [string]$Tag='untagged')
$ErrorActionPreference = 'Stop'
$version = ([xml](Get-Content (Join-Path $Root 'Directory.Build.props'))).Project.PropertyGroup.Version
$assets = Get-Content (Join-Path $Root 'src/HOSASBridge.App/obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
$components = @()
foreach ($entry in $assets.libraries.GetEnumerator() | Sort-Object Key) {
    if ($entry.Value.type -ne 'package') { continue }
    $parts = $entry.Key.Split('/')
    $components += @{ type='library'; name=$parts[0]; version=$parts[1]; purl="pkg:nuget/$($parts[0])@$($parts[1])"; 'bom-ref'=$entry.Key }
}
foreach ($entry in @(@('vJoy','2.2.2.0','vJoySetup.exe'),@('HidHide','1.5.230.0','HidHideSetup.exe'),@('vJoyInterface','2.2.2.0','vJoyInterface.dll'))) {
    $components += @{ type='library'; name=$entry[0]; version=$entry[1]; 'bom-ref'=$entry[0]; licenses=@(@{license=@{id='MIT'}}); hashes=@(@{alg='SHA-256'; content=(Get-FileHash (Join-Path $Root "dependencies/$($entry[2])")).Hash.ToLowerInvariant()}) }
}
$runtime = Get-Content (Join-Path $Root 'artifacts/portable/HOSASBridge.runtimeconfig.json') -Raw | ConvertFrom-Json
foreach ($framework in $runtime.runtimeOptions.includedFrameworks) { $components += @{type='framework';name=$framework.name;version=$framework.version;'bom-ref'=$framework.name;licenses=@(@{license=@{id='MIT'}})} }
@{ bomFormat='CycloneDX'; specVersion='1.5'; version=1; serialNumber=('urn:uuid:'+[guid]::NewGuid()); metadata=@{timestamp=[DateTime]::UtcNow.ToString('O'); component=@{type='application';name='HOSAS Bridge';version=$version}}; components=$components } | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $Root 'artifacts/SBOM.cdx.json')
@{ version=$version; commit=$Commit; tag=$Tag; source=$env:BUILD_SOURCE_URL; configuration='Release'; platform='win-x64'; applicationSigning='unsigned'; builtAtUtc=[DateTime]::UtcNow.ToString('O'); hardwareAcceptance='Manual checklist required'; sbomScope='Application NuGet packages, bundled driver packages and self-contained .NET frameworks; driver internals covered by vendor notices.' } | ConvertTo-Json | Set-Content (Join-Path $Root 'artifacts/build-metadata.json')
$files = @((Join-Path $Root "artifacts/HOSASBridge-$version-win-x64.zip"),(Join-Path $Root "artifacts/installer/HOSASBridge-$version-win-x64-Setup.exe"),(Join-Path $Root 'artifacts/portable/HOSASBridge.exe'),(Join-Path $Root 'artifacts/portable/HOSASBridge.dll'),(Join-Path $Root 'artifacts/SBOM.cdx.json'),(Join-Path $Root 'artifacts/build-metadata.json'))
$files | Where-Object { Test-Path $_ } | ForEach-Object { "$((Get-FileHash $_).Hash.ToLowerInvariant())  $([IO.Path]::GetRelativePath((Join-Path $Root 'artifacts'),$_).Replace('\','/'))" } | Set-Content (Join-Path $Root 'artifacts/SHA256SUMS.txt')
