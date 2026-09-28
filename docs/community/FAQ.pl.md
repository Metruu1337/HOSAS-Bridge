# FAQ

## Dlaczego nie Joystick Gremlin albo TARGET?

To przydatne narzędzia. Bridge skupia się na krótkiej konfiguracji identycznych drążków i trwałym wejściu dla gry. Nie wymaga ich do działania i nie obiecuje, że zastąpi każdą ich funkcję.

## Czy zmienia grę, wstrzykuje DLL lub czyta pamięć gry?

Nie. Gra dostaje zwykłe wejście kontrolera. Biblioteka vJoy jest ładowana tylko do procesu Bridge.

## Czy obchodzi anti-cheat?

Nie. Nie ma obejścia ani gwarancji akceptacji przez producenta gry lub anti-cheata.

## Czy automatyzuje rozgrywkę?

Nie ma makr, odtwarzania wejścia, automatycznego celowania ani strzelania. Tryby przekształcają bieżący ruch/przyciski użytkownika.

## Czy to keylogger?

Nie ma haków klawiatury ani globalnego zbierania tekstu. Aplikacja czyta kontrolery do gry oraz jawne działania w interfejsie.

## Czy jest telemetria i czy potrzebny jest internet?

Bez telemetrii i internetu podczas normalnej pracy. Kompilacja pobiera oficjalne zależności, a przycisk źródeł otwiera przeglądarkę na życzenie.

## Po co vJoy i HidHide?

vJoy tworzy trwały kontroler wirtualny. HidHide ukrywa wybrane fizyczne urządzenia przed grą, pozostawiając dostęp aplikacji.

## Czy mój joystick zadziała?

Spodziewamy się zgodności z DirectInput, ale nie certyfikujemy każdego modelu. Właściciel sprawdził dwa T.16000M w WARDOGS we wcześniejszej wersji. Zgłoszenie powinno zawierać model, VID/PID i możliwości z przycisku kopiowania.

## Czy mogę tworzyć i udostępniać profile?

Tak: duplikowanie, edycja, eksport/import JSON. Lokalne identyfikatory są osobno; import zachowuje przypisania komputera odbiorcy.

## Czy można uruchomić mostek po grze?

Takie jest założenie trwałego urządzenia. Najpierw konfiguracja i wymagany restart. Dokładny scenariusz gra-przed-mostkiem wymaga ręcznego testu każdej paczki.

## Czy mogę sam skompilować?

Tak, według BUILDING.md: publiczne zależności, SDK .NET i opcjonalnie Inno Setup. Kompilacja bez podpisu nie potrzebuje sekretów.

## Jak zweryfikować EXE?

Porównaj SHA256SUMS, tag/commit, przebieg CI i dostępne poświadczenie GitHub. Opis w docs/RELEASE.md. Lokalne pliki aplikacji nie mają podpisu cyfrowego.

## Gdzie są dane?

LocalAppData/HOSASBridge. Profile, przypisania, ustawienia i logi pozostają lokalnie. Eksport diagnostyki jest dobrowolny; przejrzyj ZIP przed udostępnieniem.
