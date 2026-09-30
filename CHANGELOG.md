# Changelog

## 0.9.0-beta.3 — installation and language fixes

English is the default for new settings, regardless of Windows language. Explicit saved Polish/English preferences remain unchanged.

The application installer no longer launches nested vJoy/HidHide installers, preventing vendor-driver setup from blocking application installation or upgrades. Bundled driver packages remain available from Devices. This does not claim to fix the vendor installer's internal hang; existing healthy drivers should be retained.


## 0.9.0-beta.2 — beta candidate

Preserved WARDOGS Hybrid, persistent vJoy and Pitch-only MINIGUN. Added generic roles/eight axes/capabilities, portable schema 2 with local bindings, generic preset, mode transform/button overrides, duplicate/reset, guided setup, curve/calibration preview, About/build metadata, locale-based PL/EN and catalog validation.

Corrected dependency switches and concurrent setup; replaced synthetic output test with neutral health check; sanitized diagnostic export. Added locked dependencies, CI/tagged candidate builds, checksums/SBOM and bilingual trust/community docs.

Manual driver, reboot, startup-order, sleep and multi-device acceptance remains required. App and installer unsigned.
