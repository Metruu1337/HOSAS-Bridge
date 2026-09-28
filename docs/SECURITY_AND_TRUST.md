# Security & Trust

Reads: configured DirectInput controllers, identity/capability metadata, own settings/profiles and driver health. During visible movement identification compatible game controllers are observed for up to 31 seconds. Keyboard/mouse devices are not registered.

Writes: local settings, bindings, profiles, rolling logs, vJoy state, selected HidHide rules and optionally a startup entry. Only driver setup/configuration is elevated. Shared drivers are never automatically removed.

No keyboard hooks/polling, clipboard reads/monitoring, desktop screenshots, microphone/camera access, browser/password collection, game-memory access, network interception or injection. Copy Device Info writes sanitized controller metadata to the clipboard after a click; it never reads it. Developer smoke tooling renders only this application's WPF visual tree, not the desktop/other apps.

Normal runtime is offline: no telemetry, analytics, remote logs, account or upload. Build scripts download pinned official packages. Setup uses bundled hash-checked packages. The Source button explicitly opens a browser. Optional process association checks only a selected process name's presence.

Diagnostic ZIPs are created locally on request. Profile data omits identities/process association; controller summaries contain product/manufacturer/VID/PID/capabilities. Relevant controller IDs can remain in logs; home paths are redacted. Review before sharing. No unrelated documents or process inventory.

Driver installers are privileged third-party code. SHA/signature checks prove package identity, not freedom from bugs. vJoyInterface loads into Bridge, never into a game. App/installer unsigned; driver signatures separate.

[Release verification](RELEASE.md) · [Review findings](SECURITY_REVIEW.md). Deterministic managed compilation does not guarantee identical timestamped ZIPs/installers.
