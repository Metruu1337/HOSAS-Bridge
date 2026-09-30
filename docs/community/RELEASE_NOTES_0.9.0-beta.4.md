# HOSAS Bridge 0.9.0-beta.4 — Xbox and PlayStation input

- Xbox/XInput input with independent LT/RT, both analog sticks, ten buttons and D-pad diagonals.
- PlayStation preset for native DualShock 4 / DualSense DirectInput input: both sticks, L2/R2, fourteen buttons and D-pad.
- Single-pad setup; gamepad presets leave the physical controller visible and do not require HidHide. Bind only vJoy in the game.
- Xbox assignments require re-identification after reconnecting or restarting the app; lost input neutralizes output.
- English remains the default; existing saved language preferences are preserved. No nested driver installation.

See [gamepad setup and limitations](https://github.com/Metruu1337/HOSAS-Bridge/blob/main/docs/GAMEPADS.md). This is beta input support, not hardware certification: physical pad/USB/Bluetooth gameplay testing remains outstanding. Output is vJoy, not an Xbox-emulation device; no rumble, gyro or adaptive-trigger output.

Unsigned application and installer. Use the provided checksums. Existing healthy drivers can be retained.
