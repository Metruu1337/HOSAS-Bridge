# Source review — 2026-09-28

Scope: app source, native imports, dependency execution, build scripts. Not a penetration test or driver audit.

| Surface | Finding |
|---|---|
| Keyboard hooks, GetAsyncKeyState, keyboard Raw Input | No implementation found; GameControl enumeration only |
| Clipboard | Explicit SetText only; no reads/monitoring |
| Screenshots | UiSmokeTest renders own Window; no desktop capture API |
| Microphone/camera | No implementation found |
| HTTP/analytics/telemetry | No runtime client/SDK; downloads only in build scripts |
| Read/WriteProcessMemory, VirtualAllocEx, CreateRemoteThread | No implementation found |
| Native imports | vJoy, SetupAPI/CfgMgr32 identity and kernel32 waitable timers |
| Process execution | Driver installers, vJoyConfig, HidHideCLI, elevated self-helper, joy.cpl and explicit docs/source links |
| Secrets | No embedded credentials or signing keys found |
| Diagnostics | Local export; home redaction; relevant controller IDs may remain in logs |
| Unsigned binaries | App/installer unsigned; native SDK hash-pinned; driver installers signature-checked |
| Generated gameplay input | Old sine/button test replaced with neutral-only health check |

Addressed: incorrect/silent setup switches; simultaneous setup operations; exported local identities; fixed PL-only default; duplicate JSON keys; mode graph size; generic-role fallback to Left; partial axis coverage; synthetic output test.

Risks remaining: privileged vendor code; Windows PnP update may block/reboot; public CI/provenance/certificate unavailable locally; real USB/sleep/startup-order acceptance required. Abrupt process termination cannot guarantee a final neutral report, unlike normal stop/disconnect paths. No anti-cheat approval claimed.
