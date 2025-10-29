# KSeF PDF CLI - Narzędzie wiersza poleceń

Aplikacja konsolowa do generowania wydruków PDF z faktur XML w formacie KSeF FA(3).

## Instalacja

### Budowanie z kodu źródłowego

```bash
cd KSeFPrinter.CLI
dotnet build --configuration Release
```

Plik wykonywalny zostanie utworzony w `bin/Release/net9.0/ksef-pdf.exe` (Windows) lub `bin/Release/net9.0/ksef-pdf` (Linux/macOS).

### Publikacja (opcjonalna)

Dla wygodnego użycia możesz opublikować jako samodzielny plik wykonywalny:

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained
```

## Użycie

### Tryb 1: Pojedynczy plik

Konwertuj jeden plik XML do PDF:

```bash
ksef-pdf faktura.xml

# Lub z własną nazwą pliku wyjściowego:
ksef-pdf faktura.xml -o wydruk.pdf

# Z numerem KSeF (jeśli nie jest w XML):
ksef-pdf faktura.xml --ksef-number "6511153259-20251015-010020140418-0D"
```

**Wynik:** Tworzy `faktura.pdf` (lub podaną nazwę) w tym samym katalogu.

### Tryb 2: Przetwarzanie wsadowe

Przetwórz wiele plików XML naraz:

```bash
# Wszystkie XMLe w bieżącym katalogu:
ksef-pdf *.xml

# Konkretne pliki:
ksef-pdf faktura1.xml faktura2.xml faktura3.xml

# Wszystkie XMLe w katalogu:
ksef-pdf faktury/
```

**Wynik:** Dla każdego `nazwa.xml` tworzy `nazwa.pdf`.

### Tryb 3: Watch mode (obserwowanie)

Automatycznie przetwarza nowe pliki XML po ich utworzeniu:

```bash
# Obserwuj bieżący katalog:
ksef-pdf --watch .

# Obserwuj konkretny katalog:
ksef-pdf --watch faktury/

# Obserwuj kilka katalogów:
ksef-pdf --watch katalog1/ katalog2/
```

**Działanie:**
- Najpierw przetworzy wszystkie istniejące XMLe
- Następnie będzie oczekiwać na nowe pliki
- Automatycznie wygeneruje PDF dla każdego nowego XML
- Zakończ przez naciśnięcie **Ctrl+C**

## Opcje

### Certyfikat (KOD QR II)

Dodaj certyfikat KSeF typu Offline, aby wygenerować drugi kod QR (weryfikacja certyfikatu):

```bash
# Z hasłem:
ksef-pdf faktura.xml --cert certyfikat.pfx --cert-password "mojeHaslo"

# Bez hasła (jeśli certyfikat nie jest chroniony):
ksef-pdf faktura.xml --cert certyfikat.pfx
```

**Uwaga:** Bez certyfikatu generowany jest tylko KOD QR I (weryfikacja faktury).

### Środowisko produkcyjne

Domyślnie używane jest środowisko testowe KSeF. Aby użyć produkcyjnego:

```bash
ksef-pdf faktura.xml --production
```

**Linki weryfikacyjne:**
- Test: `https://ksef-test.mf.gov.pl/...`
- Produkcja: `https://ksef.mf.gov.pl/...`

### Pomijanie walidacji

Domyślnie faktura jest walidowana przed generowaniem PDF. Aby pominąć walidację:

```bash
ksef-pdf faktura.xml --no-validate
```

**Przydatne gdy:**
- Przykładowe dane mają nieprawidłowe NIPy
- Chcesz szybko wygenerować PDF bez sprawdzania poprawności

### Szczegółowe logowanie

Wyświetl więcej informacji podczas przetwarzania:

```bash
ksef-pdf faktura.xml --verbose
# Lub krócej:
ksef-pdf faktura.xml -v
```

### Help

Wyświetl wszystkie dostępne opcje:

```bash
ksef-pdf --help
```

## Przykłady użycia

### Przykład 1: Szybkie generowanie PDF z jednej faktury

```bash
ksef-pdf FakturaTEST017.xml --no-validate
```

**Wynik:** `FakturaTEST017.pdf`

### Przykład 2: Przetwarzanie wszystkich faktur w katalogu

```bash
ksef-pdf faktury/*.xml --no-validate
```

**Wynik:** PDF dla każdego XML w katalogu `faktury/`

### Przykład 3: Faktura z certyfikatem i własną nazwą

```bash
ksef-pdf faktura.xml \
  -o "Faktura_Styczeń_2025.pdf" \
  --cert moj_cert.pfx \
  --cert-password "tajne123"
```

**Wynik:** `Faktura_Styczeń_2025.pdf` z dwoma kodami QR

### Przykład 4: Środowisko produkcyjne z numerem KSeF

```bash
ksef-pdf faktura.xml \
  --production \
  --ksef-number "6511153259-20251015-010020140418-0D"
```

**Wynik:** PDF z linkiem do produkcyjnego portalu KSeF

### Przykład 5: Watch mode dla automatycznego przetwarzania

```bash
# Terminal 1 - uruchom watch mode:
ksef-pdf --watch faktury/ --no-validate

# Terminal 2 - skopiuj nowe faktury:
cp nowa_faktura.xml faktury/
```

**Wynik:** `faktury/nowa_faktura.pdf` jest generowany automatycznie

### Przykład 6: Przetwarzanie wsadowe z filtrowaniem

```bash
# Tylko faktury z stycznia 2025:
ksef-pdf faktury/FA_2025_01_*.xml --no-validate

# Konkretny prefiks:
ksef-pdf faktury/SPRZEDAZ_*.xml --no-validate
```

## Kod wyjścia

Aplikacja zwraca następujące kody wyjścia:

- **0** - Sukces (wszystkie pliki przetworzone poprawnie)
- **1** - Błąd (walidacja nie powiodła się, błąd parsowania, brak plików itp.)

Przydatne w skryptach:

```bash
#!/bin/bash
ksef-pdf faktura.xml --no-validate

if [ $? -eq 0 ]; then
    echo "PDF wygenerowany!"
    # Wyślij email, przenieś plik, itp.
else
    echo "Błąd generowania PDF!"
    exit 1
fi
```

## Format logów

Aplikacja korzysta z Microsoft.Extensions.Logging:

```
info: Program[0]
      === Przetwarzanie pojedynczego pliku ===
info: Program[0]
      XML: faktura.xml
info: Program[0]
      PDF: faktura.pdf
info: KSeFPrinter.InvoicePrinterService[0]
      Rozpoczęcie generowania PDF z pliku: faktura.xml
...
info: Program[0]
      ✓ PDF wygenerowany pomyślnie! Rozmiar: 102,566 bajtów
```

## Integracja z systemami zewnętrznymi

### Bash/PowerShell Script

```bash
#!/bin/bash
# Przetwarzaj wszystkie faktury i przenieś PDFy do archiwum
for xml in faktury/*.xml; do
    ksef-pdf "$xml" --no-validate
    if [ $? -eq 0 ]; then
        pdf="${xml%.xml}.pdf"
        mv "$pdf" archiwum/
        echo "✓ $xml -> archiwum/$pdf"
    fi
done
```

### Cron (Linux)

```cron
# Codziennie o 23:00 przetwarzaj nowe faktury
0 23 * * * cd /home/user/faktury && /usr/local/bin/ksef-pdf *.xml --no-validate
```

### Task Scheduler (Windows)

```powershell
# Uruchom watch mode jako usługę
Start-Process -FilePath "ksef-pdf.exe" -ArgumentList "--watch C:\Faktury --no-validate" -WindowStyle Hidden
```

## Rozwiązywanie problemów

### Problem: "Nie znaleziono żadnych plików XML"

**Rozwiązanie:** Sprawdź, czy ścieżka jest poprawna:

```bash
# Sprawdź co znajduje się w katalogu:
ls *.xml

# Podaj pełną ścieżkę:
ksef-pdf /pełna/ścieżka/do/faktura.xml
```

### Problem: "Faktura nie przeszła walidacji"

**Rozwiązanie:** Użyj `--no-validate` lub napraw dane w XML:

```bash
ksef-pdf faktura.xml --no-validate
```

### Problem: "Certyfikat nie zawiera klucza prywatnego"

**Rozwiązanie:** Upewnij się, że plik PFX zawiera klucz prywatny:

```bash
# Wczytaj z hasłem:
ksef-pdf faktura.xml --cert cert.pfx --cert-password "haslo"
```

### Problem: Watch mode nie wykrywa nowych plików

**Rozwiązanie:** Upewnij się, że:
- Katalog istnieje przed uruchomieniem watch mode
- Pliki mają rozszerzenie `.xml`
- System ma uprawnienia do odczytu katalogu

## Wymagania systemowe

- **.NET 9.0 Runtime** lub nowszy
- System operacyjny:
  - Windows 10/11
  - Linux (Ubuntu, Debian, Fedora, itp.)
  - macOS (Intel i Apple Silicon)
- ~50 MB RAM na proces
- ~100 KB miejsca na dysku per PDF

## Licencja

Zobacz główny projekt KSeFPrinter.

## Wsparcie

Dla problemów i pytań:
- Zgłoszenia: [GitHub Issues](#)
- Dokumentacja: Zobacz `INSTRUKCJA.md` w głównym projekcie
- Portal KSeF: https://ksef-test.mf.gov.pl lub https://ksef.mf.gov.pl
