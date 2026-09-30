<img src="src/HOSASBridge.App/Assets/bridge.png" alt="HOSAS Bridge" width="128" />

# HOSAS Bridge

[English](README.md) | [Polski](README.pl.md)

**Two sticks in. Clean game input out.**

Clean controller routing for games that don’t play nicely with multiple sticks. HOSAS Bridge combines controller identification, mapping, a persistent virtual joystick and device hiding in one Windows application.

**0.9.0-beta.4 — public beta candidate, unsigned.** Existing dual-T.16000M / WARDOGS gameplay was tested by the project owner. The expanded beta still needs the [release acceptance checklist](docs/HARDWARE_ACCEPTANCE.md). Other controllers are not certified. No official vendor/game affiliation.

## Features

- Identical-device handling with local instance/container identity and movement-based assignment.
- Hybrid routing: WARDOGS sees Left natively and Right through vJoy.
- CombinedVirtual routing; support for throttle, pedals and another controller role.
- Persistent virtual controller and hiding; feeder start/stop does not recreate devices.
- Portable JSON profiles, calibration, curves, deadzones, saturation and inherited Toggle/Hold modes.
- WARDOGS NORMAL/MINIGUN: only Pitch receives an additional inversion.
- English by default, optional Polish interface, tray, Windows startup, local diagnostics and conservative repair.

## Quick start

1. Obtain the installer or portable ZIP from [GitHub Releases](https://github.com/Metruu1337/HOSAS-Bridge/releases). If no release is listed yet, packaging is still in progress.
2. Install the application. The application installer does not launch driver installers. Install missing drivers explicitly from Devices after application setup.
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

MIT licensed. Original icon created for the project using image generation. Thanks to vJoy, HidHide, Vortice, .NET and test-tool authors; see [notices](THIRD-PARTY-NOTICES.md).

## Gamepads / Pady

Xbox / XInput and PlayStation (DualShock 4, DualSense) presets: [setup, mapping and limitations / konfiguracja i ograniczenia](docs/GAMEPADS.md). Physical gamepad acceptance is pending / testy na fizycznych padach pozostają do wykonania.
