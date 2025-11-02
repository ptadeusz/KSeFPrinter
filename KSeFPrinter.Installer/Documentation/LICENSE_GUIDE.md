# Przewodnik licencjonowania KSeF Printer

**Wersja:** 1.0.0
**Data:** 2025-01-02

---

## Spis treści

1. [Informacje ogólne](#informacje-ogólne)
2. [Wymagania](#wymagania)
3. [Jak uzyskać licencję](#jak-uzyskać-licencję)
4. [Instalacja licencji](#instalacja-licencji)
5. [Weryfikacja licencji](#weryfikacja-licencji)
6. [Rozwiązywanie problemów](#rozwiązywanie-problemów)
7. [FAQ](#faq)

---

## Informacje ogólne

KSeF Printer wymaga **ważnej licencji** do działania. Bez licencji:
- ❌ API nie uruchomi się (Exit code: 1)
- ❌ CLI nie uruchomi się (Exit code: 1)

Licencja jest sprawdzana przy **każdym uruchomieniu** aplikacji.

---

## Wymagania

### Format licencji

Plik licencji (`license.lic`) to plik JSON zawierający:
- Klucz licencji
- Dane właściciela
- Datę wygaśnięcia
- Listę dozwolonych NIP-ów
- Włączone funkcje (KSeF Connector / KSeF Printer)
- Podpis cyfrowy RSA

### Przykład

```json
{
  "licenseKey": "KSEF-20250102-A1B2C3D4",
  "issuedTo": "Firma ABC Sp. z o.o.",
  "expiryDate": "2026-12-31T00:00:00Z",
  "allowedNips": ["1234567890"],
  "maxNips": 5,
  "features": {
    "ksefConnector": false,
    "ksefPrinter": true,
    "batchMode": false,
    "autoTokenGeneration": false,
    "azureKeyVault": true
  },
  "signature": "..."
}
```

---

## Jak uzyskać licencję

### 1. Kontakt z dostawcą

Skontaktuj się z dostawcą oprogramowania:
- **Email:** [adres email dostawcy]
- **Telefon:** [numer telefonu]
- **Strona:** [strona www]

### 2. Podaj informacje

Dostawca będzie potrzebował:
- **Nazwa firmy** (pole `issuedTo`)
- **NIP firmy** (lista `allowedNips`)
- **Ilość NIP-ów** do obsługi (pole `maxNips`)
- **Produkt:** KSeF Printer (nie KSeF Connector)
- **Okres licencji** (domyślnie: 1 rok)
- **Funkcje dodatkowe:**
  - Azure Key Vault (opcjonalne)

### 3. Otrzymanie licencji

Dostawca wygeneruje plik `license.lic` i wyśle:
- **Plik licencji:** `license.lic` (lub `nazwafirmy.lic`)
- **Klucz publiczny:** `public_key.pem` (opcjonalnie)

**WAŻNE:** Sprawdź datę wygaśnięcia przed instalacją!

---

## Instalacja licencji

### Krok 1: Skopiuj plik licencji

Po instalacji KSeF Printer skopiuj plik `license.lic` do folderów:

**Dla CLI:**
```
C:\Program Files\KSeF Printer\CLI\license.lic
```

**Dla API:**
```
C:\Program Files\KSeF Printer\API\license.lic
```

### Krok 2: Sprawdź uprawnienia

Plik `license.lic` musi być **czytelny** dla użytkownika/serwisu uruchamiającego aplikację:

```powershell
# Sprawdź uprawnienia (PowerShell jako Administrator)
icacls "C:\Program Files\KSeF Printer\API\license.lic"

# Powinno być:
# NT AUTHORITY\SYSTEM:(F)
# BUILTIN\Administrators:(F)
# BUILTIN\Users:(R)
```

Jeśli API działa jako serwis Windows:
- Konto `LocalSystem` musi mieć dostęp do pliku (domyślnie ma)

### Krok 3: Uruchom aplikację

**CLI:**
```bash
cd "C:\Program Files\KSeF Printer\CLI"
ksef-pdf.exe --help
```

**API:**
```bash
# Jeśli serwis Windows:
net start KSeFPrinterAPI

# Jeśli uruchamianie ręczne:
cd "C:\Program Files\KSeF Printer\API"
.\KSeFPrinter.API.exe
```

### Krok 4: Sprawdź logi

**API - logi przy starcie:**
```
=== Sprawdzanie licencji ===
✅ Licencja ważna
   Właściciel: Firma ABC Sp. z o.o.
   Ważna do: 2026-12-31
   Dozwolone NIP-y: 1
```

**CLI - brak błędów:**
```
KSeF Printer CLI v1.0.0
...
```

---

## Weryfikacja licencji

### Sprawdzenie zawartości licencji

**PowerShell:**
```powershell
Get-Content "C:\Program Files\KSeF Printer\API\license.lic" | ConvertFrom-Json | Format-List

# Wynik:
# licenseKey       : KSEF-20250102-A1B2C3D4
# issuedTo         : Firma ABC Sp. z o.o.
# expiryDate       : 2026-12-31T00:00:00Z
# allowedNips      : {1234567890}
# ...
```

### Sprawdzenie daty wygaśnięcia

**PowerShell:**
```powershell
$license = Get-Content "C:\Program Files\KSeF Printer\API\license.lic" | ConvertFrom-Json
$expiryDate = [DateTime]::Parse($license.expiryDate)
$daysLeft = ($expiryDate - (Get-Date)).Days

Write-Host "Licencja ważna przez: $daysLeft dni"
```

### Sprawdzenie uprawnień produktu

```powershell
$license = Get-Content "C:\Program Files\KSeF Printer\API\license.lic" | ConvertFrom-Json

# Sprawdź KSeF Printer
if ($license.features.ksefPrinter -eq $true) {
    Write-Host "✅ Licencja obejmuje KSeF Printer"
} else {
    Write-Host "❌ Licencja NIE obejmuje KSeF Printer"
}
```

---

## Rozwiązywanie problemów

### Problem 1: "Plik licencji nie istnieje"

**Błąd:**
```
❌ BŁĄD LICENCJI: Plik licencji nie istnieje: C:\Program Files\KSeF Printer\API\license.lic
Aplikacja nie może zostać uruchomiona bez ważnej licencji.
```

**Rozwiązanie:**
1. Sprawdź, czy plik istnieje:
   ```powershell
   Test-Path "C:\Program Files\KSeF Printer\API\license.lic"
   ```
2. Jeśli nie - skopiuj plik `license.lic` do tego folderu
3. Uruchom aplikację ponownie

---

### Problem 2: "Licencja wygasła"

**Błąd:**
```
❌ BŁĄD LICENCJI: Licencja wygasła: 2025-12-31
```

**Rozwiązanie:**
1. Skontaktuj się z dostawcą w celu odnowienia licencji
2. Podaj aktualny klucz licencji: `KSEF-20250102-A1B2C3D4`
3. Otrzymasz nowy plik `license.lic` z przedłużonym terminem
4. Zastąp stary plik nowym
5. Uruchom aplikację ponownie (restart serwisu)

---

### Problem 3: "Nieprawidłowy podpis licencji"

**Błąd:**
```
❌ BŁĄD LICENCJI: Nieprawidłowy podpis licencji
```

**Możliwe przyczyny:**
- Plik licencji został zmodyfikowany ręcznie
- Plik licencji jest uszkodzony
- Plik licencji pochodzi z innego źródła

**Rozwiązanie:**
1. **NIE EDYTUJ** pliku `license.lic` ręcznie!
2. Pobierz oryginalny plik od dostawcy
3. Jeśli problem występuje nadal - skontaktuj się z dostawcą

---

### Problem 4: "Licencja nie obejmuje KSeF Printer"

**Błąd:**
```
❌ BŁĄD LICENCJI: Licencja nie obejmuje KSeF Printer. Skontaktuj się z dostawcą w celu zakupu licencji.
```

**Rozwiązanie:**
- Posiadasz licencję dla **KSeF Connector**, nie dla **KSeF Printer**
- Są to **osobne produkty** wymagające **osobnych licencji**
- Skontaktuj się z dostawcą w celu zakupu licencji dla KSeF Printer

**Sprawdzenie:**
```powershell
$license = Get-Content "C:\Program Files\KSeF Printer\API\license.lic" | ConvertFrom-Json
$license.features.ksefPrinter
# Powinno być: True
```

---

### Problem 5: Brak uprawnień do odczytu pliku

**Błąd (Windows Event Log):**
```
Access denied: C:\Program Files\KSeF Printer\API\license.lic
```

**Rozwiązanie:**
```powershell
# Nadaj uprawnienia (PowerShell jako Administrator)
icacls "C:\Program Files\KSeF Printer\API\license.lic" /grant "Users:(R)"
icacls "C:\Program Files\KSeF Printer\API\license.lic" /grant "SYSTEM:(R)"

# Restart serwisu
net stop KSeFPrinterAPI
net start KSeFPrinterAPI
```

---

## FAQ

### Czy mogę używać jednej licencji dla CLI i API?

**TAK** - skopiuj plik `license.lic` do obu folderów:
- `C:\Program Files\KSeF Printer\CLI\license.lic`
- `C:\Program Files\KSeF Printer\API\license.lic`

Obie aplikacje używają **tego samego pliku licencji**.

---

### Czy licencja jest sprawdzana online?

**NIE** - licencja jest sprawdzana **lokalnie** (offline):
1. Odczyt pliku `license.lic`
2. Weryfikacja podpisu RSA (klucz publiczny wbudowany w aplikację)
3. Sprawdzenie daty wygaśnięcia
4. Sprawdzenie uprawnień produktu

**Nie jest wymagane połączenie z internetem** do sprawdzenia licencji.

---

### Czy mogę przenieść licencję na inny serwer?

**TAK** - wystarczy skopiować plik `license.lic` na nowy serwer.

**UWAGA:** Niektóre licencje mogą być powiązane z:
- Listą dozwolonych NIP-ów (pole `allowedNips`)
- Maksymalną liczbą NIP-ów (pole `maxNips`)

Jeśli używasz aplikacji dla **różnych NIP-ów**, sprawdź czy są one na liście `allowedNips`.

---

### Jak sprawdzić, ile dni zostało do wygaśnięcia licencji?

**PowerShell:**
```powershell
$license = Get-Content "C:\Program Files\KSeF Printer\API\license.lic" | ConvertFrom-Json
$expiryDate = [DateTime]::Parse($license.expiryDate)
$daysLeft = ($expiryDate - (Get-Date)).Days

if ($daysLeft -gt 0) {
    Write-Host "✅ Licencja ważna przez: $daysLeft dni" -ForegroundColor Green
} else {
    Write-Host "❌ Licencja wygasła $([Math]::Abs($daysLeft)) dni temu" -ForegroundColor Red
}
```

---

### Co się stanie, gdy licencja wygaśnie?

- ❌ Aplikacja **nie uruchomi się**
- ❌ API zwróci błąd przy starcie (Exit code: 1)
- ❌ CLI zwróci błąd przy uruchomieniu (Exit code: 1)
- ⚠️ Jeśli aplikacja jest już uruchomiona, będzie działać do restartu

**Zalecenie:** Odnów licencję **przed** wygaśnięciem.

---

### Jak zabezpieczyć plik licencji?

1. **Uprawnienia:**
   ```powershell
   # Tylko odczyt dla użytkowników
   icacls "C:\Program Files\KSeF Printer\API\license.lic" /grant "Users:(R)"
   ```

2. **Backup:**
   - Zrób kopię zapasową pliku `license.lic`
   - Przechowuj w bezpiecznym miejscu (np. Azure Blob Storage, dysk sieciowy)

3. **Dokumentacja:**
   - Zapisz klucz licencji (`licenseKey`) w bezpiecznym miejscu
   - Ułatwi to kontakt z dostawcą przy odnowieniu

---

### Gdzie znajdę klucz publiczny RSA?

Klucz publiczny RSA jest **wbudowany w aplikację** (nie jest oddzielnym plikiem).

Można go znaleźć w kodzie źródłowym:
- **Lokalizacja:** `KSeFPrinter/Services/License/LicenseValidator.cs`
- **Klasa:** `LicenseVerifier_PublicKey`

**UWAGA:** Klucz publiczny jest **ten sam** dla KSeF Connector i KSeF Printer.

---

## Kontakt

W razie problemów z licencją:
- **Email:** [email dostawcy]
- **Telefon:** [telefon dostawcy]
- **Strona:** [strona www]

Podaj:
- Klucz licencji (`licenseKey`)
- Dokładny komunikat błędu
- Wersję aplikacji

---

**Koniec przewodnika**

Wygenerowano: 2025-01-02
Wersja dokumentu: 1.0.0
