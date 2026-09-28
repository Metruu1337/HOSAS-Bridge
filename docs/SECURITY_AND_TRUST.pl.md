# Zaufanie i bezpieczeństwo

Odczyt: przypisane kontrolery DirectInput, metadane identyfikacji, własne profile/ustawienia i stan sterowników. Podczas jawnego rozpoznawania ruchem aplikacja obserwuje zgodne kontrolery przez maksymalnie 31 sekund. Nie rejestruje klawiatury/myszy.

Zapis: lokalne ustawienia, przypisania, profile, rotowane logi, vJoy, wybrane reguły HidHide i opcjonalny autostart. Administrator jest potrzebny tylko do konfiguracji sterowników. Współdzielone sterowniki nie są automatycznie usuwane.

Bez haków klawiatury, zapisu tekstu, odczytu/monitorowania schowka, przechwytywania pulpitu, mikrofonu/kamery, danych przeglądarki/haseł, pamięci/sieci gry i wstrzykiwania DLL. Kopiowanie informacji zapisuje tekst do schowka dopiero po kliknięciu. Test deweloperski renderuje tylko własny interfejs WPF, nie pulpit ani inne aplikacje.

Normalna praca jest offline: brak konta, telemetrii i wysyłania logów. Skrypty kompilacji pobierają przypięte oficjalne paczki. Konfiguracja używa dołączonych instalatorów i sprawdza sumy. Przycisk źródeł otwiera przeglądarkę na życzenie. Wykrywanie gry sprawdza wyłącznie obecność wybranej nazwy procesu.

Diagnostyka tworzy lokalny ZIP. Profil pomija identyfikatory i nazwę procesu. Podsumowanie zawiera model/producenta/VID/PID/liczbę osi, przycisków i POV. Logi mogą zawierać identyfikatory kontrolerów; ścieżki użytkownika są maskowane. Przejrzyj paczkę przed udostępnieniem. Nie dołączamy obcych dokumentów ani list procesów.

Sterowniki są zewnętrznym kodem uprzywilejowanym. Podpis/suma nie dowodzi braku błędów. vJoyInterface jest ładowany do Bridge, nie gry. Aplikacja i instalator są niepodpisane, sterowniki mają osobne podpisy.

[Weryfikacja](RELEASE.md) · [Przegląd kodu](SECURITY_REVIEW.md). Deterministyczny kod .NET nie oznacza identycznych bajtów paczek z datami.
