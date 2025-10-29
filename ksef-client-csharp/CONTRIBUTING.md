# **Zasady współtworzenia projektu KSeF Client**

Dziękujemy za zainteresowanie współtworzeniem KSeF Client!  
Celem projektu jest dostarczanie stabilnego, otwartego i szeroko użytecznego klienta w C#.  
Poniżej znajdziesz zasady, które pomogą nam wspólnie rozwijać kod w sposób uporządkowany i zgodny z najlepszymi praktykami.

---

## Jak zgłaszać zmiany?

### 1. Zgłoś problem lub propozycję
- Użyj zakładki *Issues* aby zgłosić błąd lub propozycję nowej funkcjonalności.  
- Opisz możliwie dokładnie kontekst i oczekiwane zachowanie.  
- Jeśli **chcesz się problemem sama/sam zająć**, oznacz issue etykietą **`community-contribution`** lub poproś o przypisanie do siebie.  
- Jeśli nie planujesz rozwiązać problemu – nie dodawaj etykiety, wtedy issue pozostaje otwarte dla innych.  

### 2. Wprowadzaj zmiany poprzez Pull Request
- Wykonaj fork repozytorium.  
- Utwórz osobny branch (`feature/...`, `fix/...`).  
- Wprowadź zmiany i upewnij się, że wszystkie testy przechodzą.  
- Otwórz PR z czytelnym opisem *dlaczego* i *jak* wprowadzono zmianę.  
- Każdy PR powinien odnosić się do konkretnego issue.  
- Dzięki temu unikniemy sytuacji, w której kilka osób pracuje nad tym samym zadaniem.

---

## Zasady dotyczące kodu

### Zgodność wsteczna
- Nie usuwaj ani nie zmieniaj istniejących metod w sposób, który łamie kompatybilność.  
- Rozszerzenia funkcjonalności wprowadzaj w sposób zgodny wstecznie np. jako metody rozszerzające, nadpisania lub parametry opcjonalne.
- W przypadku zupełnie nowych funkcjonalności należy je implementować jako oddzielne, spójne moduły, które nie ingerują w dotychczasowe API i nie powodują niekompatybilności.

### Styl i jakość
- Kod powinien być zgodny z regułami C# i przyjętymi konwencjami w projekcie.  
- Stosuj zasady SOLID i trzymaj się istniejącej architektury.  
- Każda publiczna metoda/klasa musi posiadać komentarz XML.  

### Testy
- Do każdej nowej funkcjonalności wymagane są testy jednostkowe i/lub integracyjne.  
- Nie obniżaj istniejącego pokrycia testami.  

---

## Proces akceptacji

- Każdy PR jest sprawdzany przez właścicieli projektu.  
- PR musi przejść automatyczne sprawdzenia CI (build, testy).  
- Zmiany wchodzą do głównej gałęzi tylko przez PR – nie wykonujemy commit'ów bezpośrednio.  

### Czas rozpatrywania Pull Requestów

Pull Requesty i zgłoszenia będą rozpatrywane w miarę dostępnego czasu zespołu.  
Wszystkie zgłoszenia są dla nas cenne, nawet jeśli nie możemy zająć się nimi natychmiast.  

---

## Kiedy Pull Request może zostać odrzucony?

PR może zostać odrzucony, jeśli:  
* **Łamie zgodność wsteczną** bez uzasadnienia i planu migracji.  
* **Nie zawiera testów** dla nowych funkcjonalności lub pogarsza pokrycie testami.  
* **Nie przechodzi automatycznej weryfikacji CI** (build, testy, styl).  
* **Zmienia istniejącą architekturę** w sposób sprzeczny z przyjętymi założeniami.  
* **Faworyzuje wąski przypadek użycia** kosztem ogólnej użyteczności biblioteki.  
* **Nie zawiera wymaganej dokumentacji** lub komentarzy XML dla publicznych API.  
* **Jest zbyt duży i trudny do przejrzenia** – zalecamy małe, granularne zmiany.  
* **Brak reakcji autorki/autora** na uwagi w review przez dłuższy czas.  

---

## Zasady współpracy

- Zachowuj kulturę i szacunek wobec innych współtwórców.  
- Nie wprowadzaj zmian, które faworyzują wąski przypadek użycia kosztem ogólnej użyteczności biblioteki.  
- Wszelkie większe decyzje architektoniczne będą omawiane w publicznych dyskusjach lub wewnątrz zespołu projektowego.  

---

Dziękujemy za Twój wkład!
