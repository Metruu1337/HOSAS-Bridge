param([string]$Executable = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/portable/HOSASBridge.exe'), [ValidateSet('pl','en')][string]$Language = 'pl')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$env:HOSASBRIDGE_DATA = Join-Path $root "artifacts/smoke-data-$Language"
New-Item -ItemType Directory -Force $env:HOSASBRIDGE_DATA | Out-Null
@{ Language = $Language } | ConvertTo-Json | Set-Content (Join-Path $env:HOSASBRIDGE_DATA 'settings.json')
$out = Join-Path $root "artifacts/ui-smoke-$Language"
$process = Start-Process -FilePath $Executable -ArgumentList '--smoke-test',('"' + $out + '"') -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(30000)) { throw 'WPF smoke test exceeded 30 seconds; inspect the application.' }
if ($process.ExitCode -ne 0) { throw "WPF smoke failed: $($process.ExitCode)" }
if ((Get-ChildItem $out -Filter 'tab-*.png').Count -ne 8) { throw 'Not all tabs rendered' }
$result = Get-Content (Join-Path $out 'result.txt') -Raw
$expectedCulture = if ($Language -eq 'pl') { 'pl-PL' } else { 'en-US' }
if (-not $result.Contains("Culture: $expectedCulture.")) { throw "Unexpected UI culture: $result" }
$result
