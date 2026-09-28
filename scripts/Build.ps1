param(
    [string]$Dotnet = (Join-Path (Split-Path $PSScriptRoot -Parent) '.tools/dotnet/dotnet.exe'),
    [string]$InnoCompiler = (Join-Path (Split-Path $PSScriptRoot -Parent) '.tools/inno/ISCC.exe'),
    [switch]$SkipInstaller,
    [switch]$SkipSmoke
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
if (-not (Test-Path $Dotnet)) { $Dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
function Invoke-Dotnet([string[]]$Arguments) { & $Dotnet @Arguments; if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $Arguments" } }
$version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version
$commit = git rev-parse --verify HEAD 2>$null
if ($LASTEXITCODE -ne 0) { $commit = 'uncommitted-local' }
elseif (git status --porcelain) { $commit = "$commit-dirty" }
$tag = if ($env:BUILD_TAG) { $env:BUILD_TAG } else { 'untagged' }
$metadata = @("-p:BuildCommit=$commit", "-p:BuildTag=$tag")
if ($env:BUILD_SOURCE_URL) { $metadata += "-p:RepositoryUrl=$env:BUILD_SOURCE_URL" }
& "$PSScriptRoot/Validate-Localization.ps1"
& "$PSScriptRoot/Fetch-Dependencies.ps1" -Root $root
Invoke-Dotnet -Arguments @('restore','HOSASBridge.slnx','--locked-mode')
Invoke-Dotnet -Arguments (@('build','HOSASBridge.slnx','--no-restore','-c','Release') + $metadata)
Invoke-Dotnet -Arguments @('test','HOSASBridge.slnx','--no-build','-c','Release','--logger','trx','--results-directory','artifacts/test-results')
Invoke-Dotnet -Arguments (@('publish','src/HOSASBridge.App/HOSASBridge.App.csproj','-c','Release','-r','win-x64','--self-contained','true','-o','artifacts/portable','-p:DebugType=None','-p:RestoreLockedMode=true') + $metadata)
Copy-Item README.pl.md,THIRD-PARTY-NOTICES.md,SECURITY.md,CHANGELOG.md,README.md,ARCHITECTURE.md,DEPENDENCIES.md,BUILDING.md,TROUBLESHOOTING.md,TESTING.md,RELEASE_REPORT.md,LICENSE artifacts/portable
if (-not $SkipSmoke) { & "$PSScriptRoot/Smoke-Test.ps1" -Executable (Join-Path $root 'artifacts/portable/HOSASBridge.exe') -Language pl }
if (-not $SkipSmoke) { & "$PSScriptRoot/Smoke-Test.ps1" -Executable (Join-Path $root 'artifacts/portable/HOSASBridge.exe') -Language en }
Copy-Item docs artifacts/portable -Recurse -Force
Compress-Archive -Path 'artifacts/portable/*' -DestinationPath "artifacts/HOSASBridge-$version-win-x64.zip" -Force
if (-not $SkipInstaller) {
    if (-not (Test-Path $InnoCompiler)) { throw 'Pass -InnoCompiler with the path to ISCC.exe (Inno Setup 6.7.3 or compatible).' }
    & $InnoCompiler installer/HOSASBridge.iss
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
    & "$PSScriptRoot/Verify-Release.ps1" -Root $root
}
Get-FileHash "artifacts/HOSASBridge-$version-win-x64.zip",'artifacts/portable/HOSASBridge.exe' | Format-Table -AutoSize

& "$PSScriptRoot/Release-Metadata.ps1" -Root $root -Commit $commit -Tag $tag
