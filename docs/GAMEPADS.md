# Xbox and PlayStation gamepads (beta)

Xbox controllers use Windows XInput (up to four connected player slots). DualShock 4 and DualSense use their native Windows DirectInput interface. Both presets route two sticks, two independent analog triggers, buttons and an eight-direction D-pad into vJoy. Xbox exposes ten standard buttons; PlayStation preserves fourteen DirectInput buttons, including the touchpad click when exposed by the device.

## Setup

1. Connect the pad. For an initial PlayStation test, use USB and close controller remapping tools so the native device remains available. If using an Xbox-emulating wrapper intentionally, select the Xbox preset for that virtual input.
2. In Devices, advance to Choose preset and apply **Xbox gamepad** or **PlayStation gamepad**. Applying resets mappings and device assignments; export an existing custom profile first if you want to keep it.
3. Go Back to Assign roles, choose **Move RIGHT stick**, and move one analog on the pad. A single pad occupies RIGHT; LEFT is not needed.
4. Check Diagnostics: both sticks, each trigger separately and together, buttons, and D-pad diagonals. Configure vJoy if required, finish setup and start the bridge.
5. Bind the **vJoy** device in your game. Physical pads remain visible with these presets; clear native-pad bindings to avoid double input. HidHide is optional. Automatic hiding of Xbox player slots is unsupported.

Xbox assignments last for the current connection and app session. Re-identify after a detected disconnection or app restart: XInput exposes a slot number, not a durable physical ID. The bridge does not automatically adopt a replacement controller in a freed slot. A controller swap occurring entirely between polls cannot be detected by XInput; stop the bridge before swapping controllers.

## Default axis mapping

| Control | vJoy axis |
| --- | --- |
| Left analog horizontal / vertical | X / Y |
| Right analog horizontal / vertical | Rx / Ry |
| Left / right trigger | Z / Rz |
| D-pad | POV 1 |

Xbox buttons 1–10: A, B, X, Y, LB, RB, View/Back, Menu/Start, left-stick click, right-stick click.
PlayStation buttons 1–14 follow DirectInput: Square, Cross, Circle, Triangle, L1, R1, L2 click, R2 click, Share/Create, Options, L3, R3, PS, touchpad click. Games/Windows may reserve system buttons.

Output remains a DirectInput vJoy joystick, not an emulated Xbox controller. Games that only accept XInput output are outside this feature. Rumble, adaptive triggers, gyro, touchpad gestures, DualSense mute and extra Edge/Elite controls are not mapped. Bluetooth depends on the Windows driver and report mode; enhanced PlayStation report modes enabled by other applications can prevent native DirectInput access. Use USB if diagnostics show no input.

Automated tests exercise decoding, independent triggers, full stick range, button release, D-pad diagonals, profiles and disconnect neutralization. Physical Xbox/DS4/DualSense USB/Bluetooth and in-game acceptance have **not** been verified for this release. Report model, connection method and anonymized Copy Device Info with any issue.

Implementation references: [Microsoft XInput/DirectInput](https://learn.microsoft.com/en-us/windows/win32/xinput/xinput-and-directinput), [SDL controller mappings](https://github.com/libsdl-org/SDL/blob/main/src/joystick/SDL_gamepad_db.h), [SDL PS5 Bluetooth report-mode notes](https://wiki.libsdl.org/SDL2/SDL_HINT_JOYSTICK_HIDAPI_PS5_RUMBLE).

## Po polsku

W Urządzeniach wybierz profil **Xbox gamepad** lub **PlayStation gamepad**, zastosuj go, wróć o krok i rozpoznaj pad jako **PRAWY**. Sprawdź analogi, spusty i przyciski w Diagnostyce, uruchom mostek, a w grze przypisz vJoy. Nie potrzebujesz drugiego pada. Profile padów nie wymagają ukrywania; usuń w grze równoległe przypisania fizycznego pada, aby uniknąć podwójnych reakcji.

Po odłączeniu Xbox lub restarcie aplikacji trzeba ponownie rozpoznać pad. Dla PS4/PS5 zacznij od USB. Wyjście to vJoy, bez emulacji Xbox, wibracji, żyroskopu i adaptacyjnych spustów. Testy automatyczne przeszły; testy na fizycznych padach i przez Bluetooth pozostają do wykonania.
