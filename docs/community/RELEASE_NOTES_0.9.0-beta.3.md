# HOSAS Bridge 0.9.0-beta.3

- New installations default to English regardless of Windows language. Polish remains selectable; saved language preferences are retained.
- The Bridge installer no longer starts vJoy or HidHide installers. This prevents a nested vendor installer from blocking installation or updates of Bridge itself. Install missing drivers explicitly from the Devices page after application setup.
- Existing working drivers should be retained. This change does not fix the vendor vJoy installer's internal hang when explicitly installing that driver.

The owner's current Windows diagnostics report vJoy and HidHide healthy, and the installed beta.2 log shows active virtual output and both T.16000M inputs acquired. No forced driver termination or replacement was performed for this fix.

Local validation: 94 automated tests passed; eight WPF tabs and nine setup stages rendered in PL/EN without binding errors; installer and portable package verification passed. New settings and legacy settings without a language use English; explicitly saved Polish persists.

Still an early beta. Hardware/clean-install acceptance remains pending. Application and installer are unsigned; vendor driver installers have separate signatures. Back up configuration before testing upgrades.
