# Przewodnik instalacji KSeF Printer

**Wersja:** 1.0.0
**Data:** 2025-01-02

---

## Wymagania systemowe

### System operacyjny
- Windows 10 (64-bit) lub nowszy
- Windows Server 2016 lub nowszy

### Oprogramowanie
- **.NET 9.0 Runtime** (wymagane!)
  - Pobierz: https://dotnet.microsoft.com/download/dotnet/9.0
  - Wybierz: "ASP.NET Core Runtime 9.0.x - Windows x64"

### Sprzęt
- Procesor: 2 GHz lub szybszy
- RAM: minimum 2 GB (zalecane: 4 GB)
- Dysk: 500 MB wolnego miejsca

---

## Instalacja

### Krok 1: Pobierz instalator

Pobierz plik `KSeFPrinter.msi` z:
- [Link do strony z releases]
- Lub od dostawcy oprogramowania

### Krok 2: Uruchom instalator

```powershell
# Instalacja interaktywna:
.\KSeFPrinter.msi

# Instalacja cicha (bez GUI):
msiexec /i KSeFPrinter.msi /qn
```

### Krok 3: Wybierz komponenty

Instalator pyta o komponenty do zainstalacji:

1. **KSeF Printer CLI** ✅ (wymagane)
   - Aplikacja konsolowa
   - Dodaje do PATH systemowego

2. **KSeF Printer API** ✅ (zalecane)
   - REST API z interfejsem Swagger
   - Instaluje jako serwis Windows

3. **Przykłady** ⬜ (opcjonalne)
   - Przykładowe faktury XML
   - Wygenerowane PDF-y

4. **Dokumentacja** ✅ (zalecane)
   - Przewodniki użytkownika
   - Specyfikacja API

### Krok 4: Wybierz folder instalacji

Domyślnie:
```
C:\Program Files\KSeF Printer\
```

### Krok 5: Dialog o licencji

⚠️ **WAŻNE:** Przeczytaj informacje o wymaganej licencji.

Aplikacja **nie uruchomi się** bez ważnej licencji!

### Krok 6: Instalacja

Kliknij "Zainstaluj" i poczekaj na zakończenie.

---

## Po instalacji

### 1. Dodaj licencję ⚠️ KRYTYCZNE

**Skopiuj plik `license.lic` do:**
```
C:\Program Files\KSeF Printer\CLI\license.lic
C:\Program Files\KSeF Printer\API\license.lic
```

Bez licencji aplikacja NIE uruchomi się!

Szczegóły: Zobacz `LICENSE_GUIDE.md`

### 2. (Opcjonalnie) Skonfiguruj certyfikat

Jeśli chcesz używać KOD QR II (z podpisem):

**Windows Certificate Store:**
```powershell
# Sprawdź dostępne certyfikaty
certmgr.msc
```

**Azure Key Vault:**
- Skonfiguruj URL Key Vault
- Dodaj nazwę certyfikatu

Szczegóły: Zobacz `CERTIFICATE_GUIDE.md`

### 3. Uruchom aplikację

**CLI:**
```bash
ksef-pdf.exe --help
```

**API (serwis Windows):**
```powershell
net start KSeFPrinterAPI

# Otwórz Swagger UI:
start http://localhost:5000/
```

---

## Weryfikacja instalacji

### Sprawdź wersję CLI

```bash
cd "C:\Program Files\KSeF Printer\CLI"
ksef-pdf.exe --version
```

### Sprawdź status serwisu API

```powershell
Get-Service KSeFPrinterAPI

# Powinno być:
# Status: Running (jeśli uruchomione)
# StartType: Manual
```

### Test API

```powershell
# Sprawdź health endpoint:
Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/health"

# Wynik:
# status  : healthy
# service : KSeF Printer API
# version : 1.0.0
```

---

## Odinstalowanie

### Standardowe

```
Panel Sterowania → Programy → Odinstaluj program → KSeF Printer
```

### Cicha odinstalacja

```powershell
# Znajdź GUID produktu:
Get-WmiObject Win32_Product | Where-Object { $_.Name -like "*KSeF Printer*" }

# Odinstaluj:
msiexec /x "{GUID}" /qn
```

**UWAGA:** Plik `license.lic` zostanie usunięty!

---

## Rozwiązywanie problemów

### "Instalacja wymaga uprawnień administratora"

Uruchom instalator jako Administrator:
```powershell
# PowerShell jako Administrator:
Start-Process -FilePath "KSeFPrinter.msi" -Verb RunAs
```

### "Nie można zainstalować - brak .NET 9.0"

Pobierz i zainstaluj .NET 9.0 Runtime:
https://dotnet.microsoft.com/download/dotnet/9.0

### Serwis API nie uruchamia się

```powershell
# Sprawdź Event Log:
Get-EventLog -LogName Application -Source KSeFPrinterAPI -Newest 10

# Najprawdopodobniej: brak licencji
```

---

## Kontakt

- **Email:** [email]
- **Telefon:** [telefon]
- **Dokumentacja:** `C:\Program Files\KSeF Printer\Documentation\`

---

**Koniec przewodnika**
