# language: pl
Potrzeba biznesowa: Operacje na sesji interaktywnej w schemacie FA(2) oraz FA(3)

  @smoke @regresja
  Scenariusz: Pytam o status aktualnej sesji interaktywnej
    Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
    Jeżeli odpytam o status tej sesji
    Wtedy dostanę odpowiedź zawierającą opis statusu sesji

  @smoke @regresja
  Scenariusz: Pytam o status innej sesji interaktywnej z mojego kontekstu
    Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
    Oraz znam numer innej sesji interaktywnej nawiązanej w imieniu tego kontekstu
    Jeżeli odpytam o status tej konkretnej sesji
    Wtedy dostanę odpowiedź zawierającą opis statusu sesji

  @smoke @regresja
  Scenariusz: Pytam o status innej sesji interaktywnej z innego kontekstu
    Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
    Oraz znam numer innej sesji interaktywnej nawiązanej w imieniu innego kontekstu
    Jeżeli odpytam o status tej konkretnej sesji
    Wtedy dostanę odpowiedź zawierającą błąd braku autoryzacji

  @smoke @regresja
  Scenariusz: Zamykam sesję interaktywną szyfrowaną
    Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
    Jeżeli spróbuję zamknąć sesję
    Wtedy sesja zostanie zamknięta
