<p align="center">
  <img src="src/HOSASBridge.App/Assets/bridge.png" alt="HOSAS Bridge" width="128" />
</p>

<h1 align="center">HOSAS Bridge</h1>

<p align="center"><strong>Two sticks in. Clean game input out.</strong></p>

<p align="center">
  A Windows controller-routing app for HOSAS setups, identical joysticks and games that don't handle multiple controllers cleanly.
</p>

<p align="center">
  <a href="https://github.com/Metruu1337/HOSAS-Bridge/releases"><strong>Download / Releases</strong></a>
  ·
  <a href="TROUBLESHOOTING.md">Troubleshooting</a>
  ·
  <a href="docs/community/FAQ.md">FAQ</a>
  ·
  <a href="README.pl.md">Polski</a>
</p>

<p align="center">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows-blue" />
  <img alt="Release" src="https://img.shields.io/badge/release-0.9.0--beta.2-orange" />
  <img alt="License" src="https://img.shields.io/badge/license-MIT-green" />
  <img alt="Telemetry" src="https://img.shields.io/badge/telemetry-none-brightgreen" />
</p>

---

## Stop fighting your controllers

Using two identical joysticks, but your game can't reliably tell them apart? Tired of manually juggling vJoy, HidHide, device order and mappings every time something changes?

**HOSAS Bridge puts that setup behind one interface.**

It identifies physical controllers, routes their inputs, maintains a persistent virtual joystick and handles device hiding so games receive a clean, predictable controller layout.

No gameplay macros. No game injection. No reading or writing game memory. No telemetry. Normal operation is offline.

> **Current flagship setup:** dual Thrustmaster T.16000M + WARDOGS.  
> HOSAS Bridge is not limited to WARDOGS: Generic HOSAS and Custom routing are built in for other DirectInput controllers and games. Wider hardware/game validation is still in progress.

<!--
README DEMO SLOT
Add a short GIF or video preview here once available.
Ideal flow: physical Left + Right sticks -> HOSAS Bridge identification -> mapping -> Start Bridge -> clean in-game input.
Suggested path: docs/media/hosas-bridge-demo.gif
Example:
<p align="center"><img src="docs/media/hosas-bridge-demo.gif" alt="HOSAS Bridge demo" width="900" /></p>
-->

## Why HOSAS Bridge?

| Problem | HOSAS Bridge |
|---|---|
| Two identical sticks are difficult to distinguish | Local device identity + movement-based assignment |
| A game handles multiple controllers poorly | Native, hybrid or combined virtual routing |
| vJoy/HidHide setup is fragmented | Driver checks, repair and hiding integrated into the app |
| Device recreation breaks bindings | Persistent virtual controller |
| Raw axes need tuning | Calibration, curves, deadzones and saturation |
| Different games need different layouts | Portable JSON profiles, presets and custom mappings |

## Features

- Identical-device handling with local instance/container identity and movement-based assignment.
- Hybrid routing: WARDOGS sees Left natively and Right through vJoy.
- CombinedVirtual routing; support for throttle, pedals and another controller role.
- Persistent virtual controller and hiding; feeder start/stop does not recreate devices.
- Portable JSON profiles, calibration, curves, deadzones, saturation and inherited Toggle/Hold modes.
- WARDOGS NORMAL/MINIGUN: only Pitch receives an additional inversion.
- PL/EN interface, tray, Windows startup, local diagnostics and conservative repair.

**0.9.0-beta.2 — public beta candidate, unsigned.** Existing dual-T.16000M / WARDOGS gameplay was tested by the project owner. The expanded beta still needs the [release acceptance checklist](docs/HARDWARE_ACCEPTANCE.md). Other controllers are not certified. No official vendor/game affiliation.

## Quick start

1. Obtain the installer or portable ZIP from [GitHub Releases](https://github.com/Metruu1337/HOSAS-Bridge/releases). If no release is listed yet, packaging is still in progress.
2. Install the application. Driver tasks are optional and unchecked to avoid reinstalling shared drivers on upgrades.
3. In Devices follow the guide: check drivers, install missing dependencies, repair the virtual controller, allow the application through HidHide and identify each stick by movement.
4. Choose WARDOGS + Dual T.16000M, Generic HOSAS or Custom. Applying a preset resets tuning/mode bindings; skip this if retaining an existing setup.
5. Test live input, optionally bind modes, repair hiding, finish setup and start the bridge.
6. In WARDOGS Hybrid, bind vJoy to Roll/Pitch/Yaw and native Left Y to Collective.

Driver operations may need administrator permission and a restart. Normal operation is not elevated. Do not run concurrent driver installers or terminate an active driver update. Restart whenever the vendor requests it.

Portable means no application installer: system drivers and a stable executable path are still required. Moving the folder requires allowing the new path through HidHide.

## Compatibility

| Hardware / game | Status | Notes |
|---|---|---|
| Dual T.16000M + WARDOGS, previous build | TESTED BY PROJECT OWNER | Axes and actual gameplay confirmed; not full beta acceptance |
| Same setup, new 0.9 beta | ACCEPTANCE PENDING | Rerun manual checklist |
| Other standard DirectInput devices | EXPECTED COMPATIBLE | Eight axis channels; correct mappings/community tests needed |
| VKB, Virpil, pedals, mixed devices | UNTESTED | No universal compatibility claim |
| Other games | UNTESTED | Generic HOSAS; game bindings remain manual |

## Trust & Security

HOSAS Bridge does not inject into games, read/write game memory, record typed text, collect telemetry, upload controller data, modify game files or bypass anti-cheat. It has no gameplay macros. Normal operation is offline.

Open source alone does not prove binary/source correspondence. [Tagged CI](.github/workflows/release.yml) builds/tests candidates with SHA-256, CycloneDX SBOM and provenance where available. About/build metadata identify source revisions. Local uncommitted builds are labelled. App/installer signing is unavailable without a certificate; driver packages have separate vendor signatures.

[Security & Trust](docs/SECURITY_AND_TRUST.md) · [Verify releases](docs/RELEASE.md) · [Report security issues](SECURITY.md). No anti-cheat acceptance guarantee.

## Help and contributing

[FAQ](docs/community/FAQ.md) · [Troubleshooting](TROUBLESHOOTING.md) · [Building](BUILDING.md) · [Architecture](ARCHITECTURE.md) · [Extending](docs/EXTENDING.md) · [Contributing](CONTRIBUTING.md)

If HOSAS Bridge solves a problem for your setup, consider starring the repository. It helps other HOSAS users discover the project.

MIT licensed. Original icon created for the project using image generation. Thanks to vJoy, HidHide, Vortice, .NET and test-tool authors; see [notices](THIRD-PARTY-NOTICES.md).
