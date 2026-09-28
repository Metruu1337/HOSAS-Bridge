# Dependencies and provenance

Verified on 2026-09-26 from official repositories and downloaded artifacts. Driver installers were downloaded for packaging, not installed as part of development testing.

| Dependency | Pinned version | License / source | Selection |
|---|---|---|---|
| .NET SDK / self-contained runtime | SDK 10.0.401 / runtime 10.0.12 | MIT plus runtime notices; https://github.com/dotnet/runtime | Current LTS SDK downloaded from Microsoft; WPF x64 deployment |
| Vortice.DirectInput | 3.8.3 | MIT; https://github.com/amerkoleci/Vortice.Windows ; https://www.nuget.org/packages/Vortice.DirectInput/3.8.3 | Maintained DirectInput wrapper; no SharpDX package dependency |
| vJoy | 2.2.2.0, native API 0x222, V3 report | MIT; https://github.com/BrunnerInnovation/vJoy/tree/v2.2.2.0 | Brunner release explicitly supports Win10/Win11 and states Microsoft attestation signing |
| HidHide | 1.5.230.0 | MIT; https://github.com/nefarius/HidHide/tree/v1.5.230.0 | Official maintained device-visibility solution with supported CLI |
| xUnit | 2.9.3 | Apache-2.0; https://github.com/xunit/xunit | Unit and integration tests only |
| xunit.runner.visualstudio | 3.1.5 | Apache-2.0; https://github.com/xunit/visualstudio.xunit | Test discovery only |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT; https://github.com/microsoft/vstest | Test host only |
| Inno Setup | 6.7.3 | Inno Setup license; https://github.com/jrsoftware/issrc/tree/is-6_7_3 ; https://jrsoftware.org/isdl.php | One x64 installer, chained official dependencies, uninstall support; build tool only |

Resolved transitive runtime wrappers: `Vortice.DirectX 3.8.3` (same Vortice repository/license), `Vortice.Mathematics 2.1.0` (MIT; https://github.com/amerkoleci/Vortice.Mathematics), and `SharpGen.Runtime` / `SharpGen.Runtime.COM 2.4.2-beta` (MIT; https://github.com/SharpGenTools/SharpGenTools, commit `6990bcafe124a4c22515ad19cee5a081da8db67b`). The beta-labelled SharpGen packages are the dependencies selected by the stable Vortice package; they are not SharpDX. Their exact resolved versions are recorded here and their license notices are included.

Inno Setup's official site requests a commercial license for commercial use. This local build does not purchase or claim such a license. The Inno compiler is not redistributed in the application.

## Exact artifacts

vJoy installer: https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/vJoySetup_v2.2.2.0_Win10_Win11.exe

SHA-256 `EF569A3105CD301B89580F18F60C66B339E95296ACF2C0DFCAF4B4BBF8AB68FE`

Authenticode: **Valid**, signer **Brunner Elektronik AG**. Official release notes: https://github.com/BrunnerInnovation/vJoy/releases/tag/v2.2.2.0 — signed installer/driver, Microsoft Partner attestation, Windows 10/11. This supports selecting signed production binaries without test-signing or disabling Secure Boot. Actual Secure Boot/HVCI acceptance on the user's machine remains untested.

vJoy SDK: https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/SDK.zip

SHA-256 `0E796B185B66819D5FBEAE645F3F038ECBFBBDE837D3D3F06CBA82AE1DB07C67`

Bundled native `SDK/lib/x64/vJoyInterface.dll` SHA-256:
`199316AAEFAC95118E869C07E7F332CE492DD9A8B6DAC5A3CA20B1D5BB5058A1`

Its PE imports are Windows system DLLs (ADVAPI32, KERNEL32, SETUPAPI, USER32); it does not import a separate VC runtime. The official vJoy installer supplies its own accompanying utility DLLs. No additional VC redistributable is required by the Bridge native adapter. API declarations are checked against the pinned SDK headers, including report layout.

HidHide installer: https://github.com/nefarius/HidHide/releases/download/v1.5.230.0/HidHide_1.5.230_x64.exe

SHA-256 `F4BBBCB82E6258641B887C74BC81C4C5F66E4AA811808DFC304347687B7605F6`

Authenticode: **Valid**, signer **Nefarius Software Solutions e.U.** Official documentation: https://docs.nefarius.at/projects/HidHide/ ; official source/license: https://github.com/nefarius/HidHide/blob/v1.5.230.0/LICENSE

## Redistribution and installer behavior

The vJoy and HidHide MIT licenses permit redistribution with notices; full notices are in `dependencies/*LICENSE*` and copied beside bundled packages. HOSAS does not modify or re-sign driver packages. Build scripts validate pinned SHA-256 and Windows Authenticode before packaging. Setup validates SHA-256 again before launching official installers. No random mirror is used.

vJoy is launched visibly with /NORESTART; HidHide is launched without unsupported switches using its vendor wizard. Exit 3010 marks reboot required; follow vendor prompts as well. Driver tasks are explicit and unchecked on app install/upgrade. First-run guide provides health/repair. Dependency versions and hashes remain pinned. No normal runtime downloads. Uninstall retains shared drivers.

The Bridge EXE/installer themselves are unsigned because no publisher code-signing certificate was supplied. Driver signatures are separate and have been verified. NuGet packages and .NET carry their own license notices; copied notices cover the application’s direct and transitive native-wrapper runtime dependencies.
