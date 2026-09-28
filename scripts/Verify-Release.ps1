param([string]$Root = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$version = ([xml](Get-Content (Join-Path $Root 'Directory.Build.props'))).Project.PropertyGroup.Version
$portable = Join-Path $Root 'artifacts/portable'
$archivePath = Join-Path $Root "artifacts/HOSASBridge-$version-win-x64.zip"
$installer = Join-Path $Root "artifacts/installer/HOSASBridge-$version-win-x64-Setup.exe"
if (-not (Test-Path $installer)) { throw 'Installer missing' }
$runtime = Get-Content (Join-Path $portable 'HOSASBridge.runtimeconfig.json') -Raw | ConvertFrom-Json
if ($runtime.runtimeOptions.includedFrameworks.Count -ne 2) { throw 'Portable runtime is not self-contained' }
$zip = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
$verified = 0
try {
    $files = Get-ChildItem $portable -Recurse -File
    foreach ($file in $files) {
        $relative = [System.IO.Path]::GetRelativePath($portable, $file.FullName).Replace('\','/')
        $entry = $zip.Entries | Where-Object { $_.FullName.Replace('\','/') -eq $relative }
        if ($null -eq $entry) { throw "ZIP missing $relative" }
        $stream = $entry.Open()
        try { $hash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($stream)) }
        finally { $stream.Dispose() }
        if ($hash -ne (Get-FileHash $file.FullName).Hash) { throw "ZIP mismatch: $relative" }
        $verified++
    }
} finally { $zip.Dispose() }
$signatures = Get-ChildItem (Join-Path $portable 'dependencies') -Filter '*.exe' | ForEach-Object {
    $signature = Get-AuthenticodeSignature $_.FullName
    if ($signature.Status -ne 'Valid') { throw "Driver signature failed: $($_.Name)" }
    @{ File = $_.Name; Status = $signature.Status.ToString(); Publisher = $signature.SignerCertificate.Subject; Sha256 = (Get-FileHash $_.FullName).Hash }
}
$artifacts = @($archivePath, $installer, (Join-Path $portable 'HOSASBridge.exe'), (Join-Path $portable 'HOSASBridge.dll')) | ForEach-Object {
    @{ File = [System.IO.Path]::GetRelativePath($Root, $_); Size = (Get-Item $_).Length; Sha256 = (Get-FileHash $_).Hash }
}
$result = @{ VerifiedAtUtc = [DateTime]::UtcNow.ToString('O'); ArchiveFilesMatched = $verified; SelfContained = $true; Drivers = @($signatures); Artifacts = @($artifacts); HardwareAcceptance = 'NOT RUN' }
$result | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $Root 'artifacts/release-checks.json')
"Verified $verified archive files against portable output; 2 signed driver packages; self-contained runtime."
