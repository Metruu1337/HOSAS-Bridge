# Troubleshooting / Rozwiązywanie problemów

## Driver install appears stuck / Instalacja sterownika stoi

Look for a separate vendor/Windows dialog. Do not start another setup or kill an active PnP update. Follow a requested Windows restart, then check status before reinstalling. App updates leave driver tasks unchecked. A previous silent vJoy update stalled on this owner's machine; HidHide previously rejected incorrect switches. The beta uses visible vendor installers and corrected arguments.

Sprawdź osobne okno instalatora/Windows. Nie uruchamiaj kolejnej instalacji ani nie kończ aktywnej aktualizacji sterownika. Po wymaganym restarcie sprawdź stan. Aktualizacja aplikacji nie wymaga reinstalacji działających sterowników.

## Driver ready but no input

Allow this executable through HidHide, identify roles, repair hiding and start Bridge. Moving a portable folder changes the allowed path. In Hybrid, only Right is hidden. vJoy must remain visible. A busy vJoy belongs to another feeder; stop that feeder or choose a different device ID, never overwrite an unrelated device blindly.

Zezwól aplikacji w HidHide, rozpoznaj role, napraw ukrywanie i uruchom mostek. W Hybrid ukryty jest tylko prawy drążek. Zajęty vJoy może należeć do innego programu.

## Identity / USB changes

Re-identify when matching is ambiguous. VID/PID alone is insufficient for identical sticks. Reset roles to swap assignments; repair hiding afterwards. Never reinstall drivers merely to reassign sticks.

## Profiles and settings

Export before experimentation. Local data: LocalAppData/HOSASBridge. Logical profile is profile.json, local binding sidecar is profile.json.bindings.json. Legacy IDs migrate on save. Invalid values are rejected; backup recovery retains corrupt files for diagnosis. Imported profiles keep local bindings. Applying/resetting a preset resets tuning and bindings of mode buttons.

## Game already running

After setup/reboot, vJoy and hiding persist before Bridge starts. Start the feeder to acquire the same existing device. This does not guarantee a game refreshes bindings after a driver/device configuration change; do setup before starting the game. Record exact startup-order acceptance for each release.

## Diagnostics / Privacy

Use live raw/normalized/transformed/output values and local export. Neutral output health check must run with Bridge stopped. Review logs for controller identifiers before posting. Copy Device Info excludes local instance IDs. Source and license buttons require packaged docs/configured repository URL.

## Language

Settings → Polski / English → Save → restart application when prompted. Windows locale sets only the first-run default.
