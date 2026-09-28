param([string]$Root = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$toolsPath = Join-Path $Root '.tools'
New-Item -ItemType Directory -Force $toolsPath | Out-Null
$package = Join-Path $toolsPath 'innosetup.exe'
if (-not (Test-Path $package)) { Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' -OutFile $package }
if ((Get-FileHash $package).Hash -ne '9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732') { throw 'Inno Setup checksum mismatch' }
if ((Get-AuthenticodeSignature $package).Status -ne 'Valid') { throw 'Inno Setup signature invalid' }
$destination = Join-Path $toolsPath 'inno'
$process = Start-Process -FilePath $package -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/DIR="' + $destination + '"') -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Inno Setup installation failed: $($process.ExitCode)" }
