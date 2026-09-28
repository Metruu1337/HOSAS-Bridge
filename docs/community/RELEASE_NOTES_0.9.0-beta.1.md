# HOSAS Bridge 0.9.0-beta.2 — early testing release

A Windows controller-routing utility built around a dual-T.16000M setup in WARDOGS. This is an early beta for testing, not a stable 1.0 release.

## Download

- **HOSASBridge-0.9.0-beta.2-win-x64-Setup.exe**: installer for Windows x64.
- **HOSASBridge-0.9.0-beta.2-win-x64.zip**: portable application; vJoy/HidHide still require installation when needed.
- **SHA256SUMS.txt**, **SBOM.cdx.json**, **build-metadata.json**: checksums, component inventory and source/build identification.

The application and installer are unsigned. Bundled vendor driver installers are separately signed. Do not disable Windows security protections to install. Keep existing working drivers when upgrading.

## Included

- WARDOGS hybrid routing: native Left for Collective, virtual Right for Roll/Pitch/Yaw.
- NORMAL/MINIGUN modes, with an additional Pitch inversion only.
- Generic HOSAS presets, additional controller roles and configurable routing.
- Movement-based device assignment, portable profiles, axis calibration and curves.
- Polish/English interface, guided setup, tray controls and local diagnostics.

## Validation and limits

Local Release build and 94 automated tests passed. All eight WPF pages and nine setup stages rendered in both languages without binding errors. Local packaging verification matched 523 ZIP files with the portable output and checked both vendor installer signatures.

Earlier dual-T.16000M/WARDOGS gameplay was confirmed by the project owner. The expanded beta has **not** completed hardware acceptance, clean-machine installation/upgrade/uninstall, game startup-order, USB reconnection or sleep/resume testing. Other controllers and games are untested. There are no measured latency or universal compatibility claims.

Back up your existing settings and profiles before trying this beta. Report your Windows version, controller models, selected preset and reproduction steps; remove device identifiers from shared logs.

MIT licensed. No official WARDOGS, Thrustmaster, vJoy or HidHide affiliation. Normal operation is offline; no telemetry, game injection, game-memory access or gameplay macros. No anti-cheat compatibility guarantee.
