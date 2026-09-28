# Third-party notices

HOSAS Bridge is MIT licensed; LICENSE covers project code. Original generated icon is not a vendor logo. Compatibility names do not imply endorsement.

| Component | Version | License/source | Purpose and distribution |
|---|---|---|---|
| .NET SDK / runtime | 10.0.401 / resolved self-contained runtime | MIT + notices, github.com/dotnet/runtime | Build/runtime; ship runtime notices |
| Vortice.DirectInput / DirectX | 3.8.3 | MIT, github.com/amerkoleci/Vortice.Windows | DirectInput wrapper; retain notice |
| Vortice.Mathematics | 2.1.0 | MIT, github.com/amerkoleci/Vortice.Mathematics | Transitive runtime dependency |
| SharpGen.Runtime / COM | 2.4.2-beta | MIT, github.com/SharpGenTools/SharpGenTools | Wrapper runtime; retain notice |
| vJoy / SDK interface | 2.2.2.0 | MIT, github.com/BrunnerInnovation/vJoy | Virtual controller; vendor binaries unmodified |
| HidHide | 1.5.230.0 | MIT, github.com/nefarius/HidHide | Selected device hiding; vendor installer unmodified |
| xUnit / runner | 2.9.3 / 3.1.5 | Apache-2.0, github.com/xunit | Tests only; not shipped |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT, github.com/microsoft/vstest | Tests only |
| Inno Setup | 6.7.3 | Inno Setup license, github.com/jrsoftware/issrc | Build tool; compiler not redistributed; check commercial-use terms |

Full significant runtime notices are shipped in dependencies/DotNet-LICENSE.txt, DotNet-THIRD-PARTY-NOTICES.txt, Vortice-LICENSE.txt, Vortice.Mathematics-LICENSE.txt, SharpGen-LICENSE.txt, vJoy-LICENSE.txt and HidHide-LICENSE.txt. DEPENDENCIES.md gives exact official sources/hashes. MIT permits redistribution with notices; vendor installers and bundled runtime retain additional internal notices.

Package lockfiles and SBOM record actual resolved versions. SBOM covers application NuGet packages, bundled native/driver packages and framework versions; it does not claim complete vendor-internal source component enumeration. Signed vendor driver packages do not make the HOSAS application signed.
