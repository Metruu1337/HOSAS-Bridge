# Build on Windows x64

Prerequisites: PowerShell 7, .NET SDK 10.0.401 (global.json servicing policy), Git and Inno Setup 6.7.3 for installer packaging. Network is needed only for initial public dependency restore/downloads. No Visual Studio, private feed, credential or signing certificate is needed for an unsigned build.

From a clean clone, in PowerShell:

    ./scripts/Fetch-Dependencies.ps1
    dotnet restore HOSASBridge.slnx --locked-mode
    dotnet build HOSASBridge.slnx -c Release --no-restore
    dotnet test HOSASBridge.slnx -c Release --no-build
    ./scripts/Validate-Localization.ps1
    dotnet publish src/HOSASBridge.App/HOSASBridge.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/portable

For full packaging:

    ./scripts/Install-BuildTools.ps1
    ./scripts/Build.ps1 -Dotnet dotnet

Install-BuildTools downloads the official checksum/signature-pinned Inno compiler into .tools. Alternatively install it yourself and pass -InnoCompiler with the actual ISCC.exe path. Use -SkipInstaller if unavailable, -SkipSmoke on noninteractive CI. The full script copies documentation, runs tests and PL/EN smoke, archives portable files, compiles installer, checks consistency, generates SBOM and SHA256SUMS.

Explicit installer compilation after publish/document copying:

    ./.tools/inno/ISCC.exe installer/HOSASBridge.iss

This repository's .tools runtimes are convenience caches, not required/private source. Locked NuGet package versions are committed. To intentionally update dependencies, update references, regenerate lockfiles, review changes and rerun tests.

Artifacts: artifacts/HOSASBridge-0.9.0-beta.3-win-x64.zip; artifacts/installer/HOSASBridge-0.9.0-beta.3-win-x64-Setup.exe; artifacts/SHA256SUMS.txt; artifacts/SBOM.cdx.json; artifacts/build-metadata.json; test-results; ui-smoke-pl/en.

HOSASBRIDGE_DATA selects an isolated app-data folder. Smoke tests render real WPF pages/wizard states without installing drivers, feeding vJoy or modifying HidHide; disconnected screenshots are not hardware acceptance. Smoke uses a separate instance namespace so a running installed Bridge is not interrupted.

Set BUILD_SOURCE_URL to the real HTTPS repository URL and BUILD_TAG for trusted release builds. Build.ps1 embeds the actual Git revision and marks dirty/uncommitted sources. Do not label a local package an official CI artifact. See docs/RELEASE.md.
