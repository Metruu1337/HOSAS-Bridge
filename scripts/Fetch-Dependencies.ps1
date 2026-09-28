param([string]$Root = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$destination = Join-Path $Root 'dependencies'
New-Item -ItemType Directory -Force $destination | Out-Null
$packages = @(
    @{ Name='vJoySetup.exe'; Url='https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/vJoySetup_v2.2.2.0_Win10_Win11.exe'; Hash='EF569A3105CD301B89580F18F60C66B339E95296ACF2C0DFCAF4B4BBF8AB68FE' },
    @{ Name='HidHideSetup.exe'; Url='https://github.com/nefarius/HidHide/releases/download/v1.5.230.0/HidHide_1.5.230_x64.exe'; Hash='F4BBBCB82E6258641B887C74BC81C4C5F66E4AA811808DFC304347687B7605F6' }
)
foreach ($package in $packages) {
    $target = Join-Path $destination $package.Name
    if (-not (Test-Path $target)) { Invoke-WebRequest $package.Url -OutFile $target }
    if ((Get-FileHash $target -Algorithm SHA256).Hash -ne $package.Hash) { throw "Checksum mismatch: $target" }
    if ((Get-AuthenticodeSignature $target).Status -ne 'Valid') { throw "Authenticode validation failed: $target" }
}
$sdkZip = Join-Path $Root '.tools/vJoySDK.zip'
$native = Join-Path $destination 'vJoyInterface.dll'
if (-not (Test-Path $native)) {
    New-Item -ItemType Directory -Force (Split-Path $sdkZip) | Out-Null
    Invoke-WebRequest 'https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/SDK.zip' -OutFile $sdkZip
    if ((Get-FileHash $sdkZip).Hash -ne '0E796B185B66819D5FBEAE645F3F038ECBFBBDE837D3D3F06CBA82AE1DB07C67') { throw 'vJoy SDK checksum mismatch' }
    Expand-Archive $sdkZip (Join-Path $Root '.tools/vJoySDK') -Force
    Copy-Item (Join-Path $Root '.tools/vJoySDK/SDK/lib/x64/vJoyInterface.dll') $native
}
if ((Get-FileHash $native).Hash -ne '199316AAEFAC95118E869C07E7F332CE492DD9A8B6DAC5A3CA20B1D5BB5058A1') { throw 'Native SDK DLL checksum mismatch' }
