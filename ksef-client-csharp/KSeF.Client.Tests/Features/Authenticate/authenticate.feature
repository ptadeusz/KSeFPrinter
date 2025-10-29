Potrzeba biznesowa: Uwierzytelnienie
Jako użytkownik chcę uwierzytelnić się w KSeF
Za pomocą różnych wektorów
Aby móc wysyłać i pobierać faktury

@smoke @interaktywna
Scenariusz: Uwierzytelnienie za pomocą certyfikatu z identyfikatorem NIP, na uprawnienie właściciel
Zakładając, że posiadam NIP kontekstu
Gdy zażądam Wyzwania Autoryzacyjnego
Oraz podpiszę plik Inicjalizacji Sesji podpisem z NIP kontekstu
Oraz wyślę podpisany plik do autoryzacji
Wtedy uzyskam token sesyjny do otwartej sesji interaktywnej z uprawnieniem właściciel

@outline @KSEFK019R-16878
Szablon scenariusza: Uwierzytelnienie za pomocą certyfikatu z nip lub pesel, na różne uprawnienia
Zakładając że posiadam NIP kontekstu, który nadał na mój uprawnienie
Jeżeli zażądam Wyzwania Autoryzacyjnego
Oraz podpiszę plik Inicjalizacji Sesji certyfikatem zawierającym mój
Oraz wyślę podpisany plik do autoryzacji
Wtedy uzyskam token sesyjny do otwartej sesji interaktywnej z uprawnieniem

Przykłady:
  | identyfikator | uprawnienie                            |
  | pesel         | wystawianie faktur                     |
  | pesel         | przeglądanie faktur,wystawianie faktur |
  | pesel         | zarządzanie uprawnieniami              |
  | pesel         | przeglądanie uprawnień                 |
  | pesel         | historia sesji                         |
  | pesel         | zarządzanie jednostkami podrzędnymi    |
  | nip           | wystawianie faktur                     |
  | nip           | przeglądanie faktur                    |
  | nip           | zarządzanie uprawnieniami              |
  | nip           | przeglądanie uprawnień                 |
  | nip           | historia sesji                         |
  | nip           | zarządzanie jednostkami podrzędnymi    |
@outline @KSEFK019R-16880
Szablon scenariusza: Uwierzytelnienie za pomocą pieczęci z nip, na różne uprawnienia
Zakładając, że posiadam NIP kontekstu, który nadał na mój lub mojej firmy uprawnienie
Jeżeli zażądam Wyzwania Autoryzacyjnego
Oraz podpiszę plik Inicjalizacji Sesji pieczęcią zawierającą nip mojej firmy
Oraz wyślę podpisany plik do autoryzacji
Wtedy uzyskam token sesyjny do otwartej sesji interaktywnej z uprawnieniem

Przykłady:
  | identyfikator | uprawnienie                         |
  | nip           | wystawianie faktur                  |
  | nip           | przeglądanie faktur                 |
  | nip           | zarządzanie uprawnieniami           |
  | nip           | przeglądanie uprawnień              |
  | nip           | historia sesji                      |
  | nip           | zarządzanie jednostkami podrzędnymi |

@Negative
Scenariusz: Uwierzytelnienie za pomocą niepoprawnego certyfikatu z PESEL
Zakładając, że użytkownik posiada PESELem i niepoprawny certyfikat
Gdy zażąda Wyzwania Autoryzacyjnego
Oraz podpisze plik Wyzwania Autoryzacyjnego
Oraz wyślę podpisany plik do autoryzacji
Wtedy uzyskam token sesyjny

@Negative @outline
Szablon scenariusza: Scenariusz: Niepoprawne uwierzytelnienia - <typ błędu>
Zakładając, że użytkownik posiada NIP
Gdy zażąda Wyzwania Autoryzacyjnego
Oraz podpisze plik Wyzwania Autoryzacyjnego
Oraz wyślę podpisany plik do autoryzacji z błędem - <typ błędu>
Wtedy otrzyma informację o błędzie

Przykłady:
  | typ błędu                    |
  | brak żądania autoryzacyjnego |
  | zły nip w podpisie           |
  | błędny plik autoryzacyjny    |