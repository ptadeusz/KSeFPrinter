Potrzeba biznesowa: Nadawanie uprawnień
Jako użytkownik chcę nadać uprawnienia innym osobom i podmiotom KSeF

@smoke
Scenariusz: Nadanie uprawnienia wystawianie faktur
Zakładając, że uwierzytelniłem się do sesji jako właściciel
Gdy nadam uprawnienie wystawianie faktur na identyfikator pesel
Wtedy uzyskam potwierdzenie nadania uprawnienia

@outline @KSEFK019R-16881
Szablon scenariusza: Nadanie uprawnień
Zakładając, że uwierzytelniłem się do sesji jako właściciel
Gdy nadam uprawnienia na identyfikator
Wtedy uzyskam potwierdzenie nadania uprawnienia

#Przykłady w języku ogórkowym
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

@outline @KSEFK019R-16882
Szablon scenariusza: Nadanie uprawnień przez osobę z uprawnieniem do zarządzania uprawnieniami
Zakładając, że uwierzytelniłem się do sesji jako osoba z uprawnieniem zarządzanie uprawnieniami
Jeżeli nadam uprawnienia na identyfikator
Wtedy uzyskam potwierdzenie nadania uprawnienia

#Przykłady w języku ogórkowym
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