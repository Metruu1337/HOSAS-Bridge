# Public beta engineering report

Candidate: 0.9.0-beta.2. This is preparation for public release, not a claim of completed hardware acceptance or executed public CI.

1. Existing architecture: nine modular projects, WPF MVVM, dedicated timed input worker, native adapters and three test projects. No initial Git history/remote was present.
2. Changes: generalized roles and eight axes/capabilities; portable schema 2/local bindings; presets in data; configurable role routing; inherited mode transforms/buttons; setup guide; axis preview/calibration; About/build/trust metadata.
3. Refactors: retained module boundaries. Profile logic owns modes/routing; adapters remain game-independent. No hardware/game switches in Core or output adapter. MainViewModel remains coordinator.
4. Public UX: nine-step setup, optional mode binding, status refresh feedback, generic roles, copy sanitized info, diagnostics/log access, duplicate/reset, original icon/tray.
5. Hardware: Right/Left/Throttle/Pedals/Other, manufacturer/capabilities/axis descriptors and safe identity resolution. One device per role, one output.
6. Profiles: legacy migration, atomic logical profile writes/backups, separate local sidecar, export without IDs/process association, receiver bindings preserved. Embedded presets remain immutable.
7. Localization: EN/PL catalogs, English first-run default, persistent override, explicit restart instruction; CI validates key/placeholder/markup consistency.
8. Setup: corrected HidHide arguments, visible vJoy installer, serialized elevated operations, optional unchecked driver tasks on updates, conservative uninstall/settings choice.
9. Trust: source API review documented; no telemetry/keyboard logging/game memory/injection/automation. Explicit clipboard write only. Neutral-only output health test. Local diagnostic redaction.
10. CI: read-only PR build/test/analyzers/localization; version-tagged protected-environment candidate packaging, checksums/SBOM/attestation. No remote/public run exists yet.
11. Documentation: bilingual README/security/FAQ/community drafts, contributor/conduct/security policies, issue/PR templates, extension/build/release/manual testing guides.
12. Automated evidence: 94 tests (48 Core, 39 Profiles/localization/settings, 7 Integration) passed before final packaging; final TRX included in build artifacts. No coverage removed.
13. Build/analyzers: Release, warnings-as-errors, deterministic compilation, pinned toolchain/NuGet lockfiles. Final packaging must finish successfully before delivery.
14. Artifacts: artifacts/HOSASBridge-0.9.0-beta.2-win-x64.zip and artifacts/installer/HOSASBridge-0.9.0-beta.2-win-x64-Setup.exe.
15. Inventory: artifacts/SHA256SUMS.txt, artifacts/SBOM.cdx.json, artifacts/build-metadata.json and THIRD-PARTY-NOTICES.md.
16. Not hardware-tested: this new beta's clean install/upgrade/uninstall, game startup order, unplug/replug, sleep/resume, changed USB port, mixed controllers and DPI/keyboard navigation. Earlier owner confirmed T.16000M/WARDOGS gameplay only.
17. Exact owner checklist: docs/HARDWARE_ACCEPTANCE.md (A–M, installation, privacy, DPI and screenshot checks). Game-before-bridge after reboot is a release blocker.
18. Limits: no actual public repository/CI/provenance yet; no publisher certificate; one vJoy output/one device per semantic role; advanced mode overrides through JSON; driver reboot/dialog behavior depends on Windows/vendor; no latency or setup-duration measurement.
19. Recommended first version: 0.9.0-beta.2. Promote to 1.0 only after manual acceptance and public release verification.
20. Signing: app/installer unsigned. Bundled vendor driver installer signatures checked separately; native SDK DLL hash-pinned.
21. Source revision: exact commit/dirty marker and tag recorded by Build.ps1 in build-metadata.json and About. Local candidates have no public attestation. No fabricated repository/tag.

UI evidence: eight actual WPF tabs and nine wizard stages rendered in each language; no fake connected-controller screenshots. Installed owner's app remains untouched during development.

External completion steps: publish reviewed source to the chosen repository, configure branch/tag/environment protections/private reports, execute tagged CI, complete owner hardware/VM checklist, then publish candidate artifacts. Add legitimate signing if available; never weaken Windows security to install.
