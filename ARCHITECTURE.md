# Architecture

Nine production modules remain intact: Core, Input.DirectInput, Output.VJoy, DeviceHiding, Profiles, Infrastructure, Diagnostics, Setup and WPF App. Core has no WPF/native driver dependency. Tests separate transforms/routing, profiles/localization and runtime integration using explicitly fake adapters.

Core owns normalized transforms, generic modes, semantic roles, identity resolution and mapping. Profile data owns WARDOGS behavior. MINIGUN is a child mode with Virtual Y inversion; XOR composition preserves base inversion and leaves Roll/Yaw unchanged.

DirectInput discovers GameControl devices, reads metadata/capabilities and eight channels. Instance GUID + PnP identity, then unique container, resolve local bindings; VID/PID alone and USB port alone never prove identity. Unassigned controllers are read only during explicit identification. Discovery is asynchronous; disconnected handles use bounded retries.

BridgeRuntime owns a dedicated timed worker (2ms waitable timer), role-state buffers, output acquisition/retry and neutralization. UI gets copies around 20Hz. Profile route changes stop feeding until hiding is checked again. Profiles choose native/hidden/virtualized roles. Start/Stop never creates or removes a virtual device.

vJoyAdapter validates the pinned 2.2.2 ABI/report layout and maps normalized output to native device ranges. One persistent output is selected per profile. HidHideService uses the supported CLI, allows the app before hiding, rejects inverse-mode conflicts, verifies virtual visibility and journals owned changes for conservative restoration.

Setup runs only requested elevated helpers. Main executable is asInvoker. Driver install/update is explicit and serialized; official package bytes are checked. Dependency windows are visible. Windows PnP may require reboot.

Profiles schema 2 separates local identity sidecars from portable logical JSON, migrates legacy schema, validates enums/ranges/parents/duplicate keys and writes backups. Embedded presets are immutable resources; local edits are customizations. Advanced mode transform/button overrides are data-driven and preserved by UI edits.

WPF view models coordinate commands/services. SetupWizardViewModel controls guide navigation only; routing remains in Core. AxisPreviewViewModel previews transformations/calibration; diagnostics reports actual output separately. About exposes build metadata/trust. Localization has matching EN/PL catalogs; changing language requests restart.

Single-instance mutex/pipe activates existing UI; smoke scope is separate and never feeds output. Runtime process association checks a user-chosen process name, not game memory. Future adapters and multiple-output support should compose interfaces rather than add game switches.
