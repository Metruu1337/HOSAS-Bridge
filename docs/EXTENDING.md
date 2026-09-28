# Extending profiles and adapters

Core knows roles, transforms and output channels, not WARDOGS/T.16000M. Game data lives in src/HOSASBridge.Profiles/Presets/wardogs.json. Controller labels live in controllers.json; VID/PID is descriptive, never sufficient to resolve physical identity.

Schema 2 has axes/buttons/povs/modes/routing/virtualDevice/hiding and optional roleRoutes. Physical axes preserve legacy indices: X, Y, Twist (RotationZ), Throttle (Slider0), Z, Rx, Ry, Slider1.

roleRoutes maps a role to native/hidden/virtualized booleans. Native+hidden is invalid. Without explicit routes, legacy Hybrid/CombinedVirtual rules apply. Current output is one selected persistent vJoy (ID 1–16), eight axes, 32–128 buttons, 2–4 continuous POVs. Multiple outputs/instances per role remain extension work.

Modes support id, parent, invertAxes, axisOverrides keyed by output axis and buttonOverrides. Inversions XOR; axis overrides replace full transforms; button overrides replace the inherited table. Advanced overrides use JSON and are preserved by the simple Modes editor.

profile.json.bindings.json stores local identities. Schema 0/1 loading preserves embedded identities; next save splits them. Export removes identity/process association; import keeps receiving-machine bindings. Embedded presets cannot be overwritten; customization saves locally.

Add a profile by exporting, editing, validating/importing and testing actual hardware/game behavior. JSON is data, never scripts or runtime types.

IPhysicalInputProvider and IVirtualJoystick are extension points. InputPipeline accepts role-state dictionaries. UI is not the router. Device creation belongs only to explicit setup.
