# Instrukcja instalacji KSeF Printer - rozwiązanie problemu z usługą

## ⚠️ Problem: "Nie można zainstalować usługi - brak uprawnień"

Jeśli dostajesz błąd podczas instalacji o braku uprawnień do instalacji usługi, wykonaj poniższe kroki:

---

## 🔧 ROZWIĄZANIE (krok po kroku):

### Krok 1: Usuń starą usługę (jeśli istnieje)

**Uruchom PowerShell jako Administrator:**
1. Naciśnij `Win + X`
2. Wybierz **"Windows PowerShell (Administrator)"** lub **"Terminal (Administrator)"**

**Wykonaj polecenie:**
```powershell
# Sprawdź czy usługa istnieje
Get-Service -Name "KSeFPrinterAPI" -ErrorAction SilentlyContinue

# Jeśli usługa istnieje, zatrzymaj i usuń ją:
Stop-Service -Name "KSeFPrinterAPI" -Force -ErrorAction SilentlyContinue
sc.exe delete KSeFPrinterAPI
```

**LUB użyj gotowego skryptu:**
```powershell
cd C:\aplikacje\ksefprinter
.\cleanup-service.ps1
```

---

### Krok 2: Zainstaluj KSeF Printer

**Z PowerShell (jako Administrator):**
```powershell
msiexec /i "C:\Instalki\KSeFSuite 20251106\KSeFPrinter.msi"
```

**LUB przez GUI:**
1. Zlokalizuj plik `KSeFPrinter.msi`
2. **Kliknij prawym przyciskiem myszy**
3. Wybierz **"Uruchom jako administrator"**
4. Postępuj zgodnie z instrukcjami instalatora

---

### Krok 3: Skopiuj licencję

Po instalacji skopiuj plik licencji:
```powershell
Copy-Item "C:\ścieżka\do\license.lic" "C:\Program Files\KSeF Printer\license.lic"
```

---

### Krok 4: Uruchom usługę

**Sprawdź status usługi:**
```powershell
Get-Service KSeFPrinterAPI | Format-List Name, Status, StartType
```

**Uruchom usługę:**
```powershell
Start-Service KSeFPrinterAPI
```

**LUB użyj skrótu z Menu Start:**
- Menu Start → KSeF Printer → "Uruchom KSeF Printer API (Serwis)"

---

### Krok 5: Sprawdź logi

**Sprawdź czy aplikacja działa:**
```powershell
Get-Content "C:\Program Files\KSeF Printer\API\logs\ksef-printer-*.log" -Tail 20
```

**Sprawdź czy licencja jest prawidłowa:**
Powinieneś zobaczyć w logach:
```
✅ Licencja ważna
   Właściciel: Twoja Firma Sp. z o.o.
   Ważna do: 2040-12-30
```

---

## 📋 Dodatkowe informacje:

### Co zostało zmienione w nowym instalatorze:

1. **Vital="no"** - Instalacja nie zatrzymuje się jeśli usługa już istnieje
2. **Remove="uninstall"** - Usługa jest usuwana tylko podczas deinstalacji (nie upgrade)
3. **Launch Condition** - Jasny komunikat jeśli brak uprawnień administratora

### Sprawdzenie uprawnień:

Upewnij się, że jesteś administratorem:
```powershell
net localgroup Administrators
```

Twoja nazwa użytkownika powinna być na liście.

---

## ❓ Często zadawane pytania:

**Q: Usługa nie chce się usunąć, co robić?**
A: Zrestartuj komputer i spróbuj ponownie. Niektóre procesy mogą trzymać usługę.

**Q: Gdzie znajdę logi błędów?**
A:
- `C:\Program Files\KSeF Printer\API\logs\ksef-printer-*.log`
- Windows Event Viewer → Application → źródło "KSeF Printer API"

**Q: Czy mogę zainstalować bez uprawnień administratora?**
A: Nie. Instalacja usług Windows wymaga uprawnień administratora.

---

**Data aktualizacji:** 2025-11-06
**Wersja instalatora:** 1.0.0.0
