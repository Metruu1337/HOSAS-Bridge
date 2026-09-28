# Contributing

Read BUILDING.md, ARCHITECTURE.md and docs/EXTENDING.md. Keep Core independent of WPF, DirectInput, vJoy and game-specific code. Preserve Hybrid routing and Pitch-only MINIGUN.

Run Release build/analyzers, all tests, localization validation and PL/EN UI smoke. Add meaningful behavior/migration/disconnect tests. Mocks are not hardware certification.

Keep local identities out of shared profiles. Keep PL/EN resources synchronized. No telemetry, keyboard monitoring, injection, game memory or macros. Document privileged/network operations and third-party licenses.

Use sanitized Copy Device Info for issues; review ZIPs before sharing. Security reports follow SECURITY.md. Contributions must be compatible with MIT.
