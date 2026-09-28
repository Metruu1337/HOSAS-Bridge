# Testing

Run dotnet test HOSASBridge.slnx -c Release after building. Tests include transforms/normalization/calibration/deadzones/curves/saturation/inversion, Hybrid/Combined mapping, modes/XOR, buttons/POV, disconnection/reconnect, identity ambiguity, profile validation/migration/portability and localization.

The integration worker test uses physical Y=+0.7: NORMAL→+0.7, MINIGUN→-0.7 with X/Z unchanged, using fake adapters. This is deterministic software evidence, not hardware testing.

scripts/Validate-Localization.ps1 checks duplicate keys, matching EN/PL keys, placeholders and new XAML labels; resource-hash coverage is tested by xUnit. Build runs .NET analyzers with warnings as errors.

scripts/Smoke-Test.ps1 renders eight WPF tabs and nine guide steps in each language. It does not capture other apps, install drivers, hide devices, acquire vJoy or certify hardware. Real DPI/keyboard/driver tests remain in docs/HARDWARE_ACCEPTANCE.md. Do not fake connected screenshots.

Exact final automated counts and limitations belong in RELEASE_REPORT.md. TRX artifacts record each run. Manual installation, reboot, sleep, USB-port and game-before-bridge tests require the project owner and a safe test machine.
