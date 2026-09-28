# FAQ

## Why not Joystick Gremlin or TARGET?

Those are useful tools. Bridge focuses on a short setup for identical sticks and persistent game input. Neither is a runtime dependency; this is not a claim of superior universal functionality.

## Does it modify the game, inject DLLs or access game memory?

No. Games receive ordinary controller input. The vJoy SDK DLL loads only inside Bridge.

## Does it bypass anti-cheat?

No. There is no bypass or guaranteed approval from any game/anti-cheat vendor.

## Does it automate gameplay?

No macros, input replay, auto-aim or automatic firing. Modes transform current physical input.

## Is it a keylogger?

No keyboard hooks or global typed-text collection. Only game-controller input and explicit UI actions.

## Does it collect telemetry or require internet?

No telemetry or runtime internet requirement. Build downloads use official sources; clicking Source opens a browser explicitly.

## Does it need vJoy? Why HidHide?

vJoy supplies the persistent virtual controller. HidHide hides selected physical devices to avoid duplicate game input while allowing Bridge to read them.

## Does it support my joystick?

Standard DirectInput devices are expected compatible, not certified. Dual T.16000M / WARDOGS was tested by the owner on the earlier build. Report model, VID/PID and capabilities using Copy Device Info.

## Can I create and share profiles?

Yes: duplicate, edit, export/import JSON. Local controller identities are separate; import retains the receiving machine's bindings.

## Can I launch it after the game?

That is the intended persistent-device workflow. Do setup/reboot first. The exact game-before-bridge scenario is a release-blocking manual test for each candidate.

## Can I build it myself?

Yes, follow BUILDING.md using public dependencies, .NET SDK and optional Inno Setup. No secrets are required for unsigned builds.

## How do I verify binaries?

Compare SHA256SUMS, tag/commit, CI run and available GitHub attestation; see docs/RELEASE.md. These local application binaries are unsigned.

## Where is my data?

LocalAppData/HOSASBridge. Profiles, bindings, settings and rolling logs stay local. Diagnostic export is optional; inspect it before sharing.
