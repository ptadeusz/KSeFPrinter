# KSeF Printer - Integracja przez Foldery (Watch Mode)

Dokumentacja opisująca integrację KSeF Printer z systemami zewnętrznymi poprzez automatyczne przetwarzanie plików w folderach.

## Spis treści

1. [Wprowadzenie](#wprowadzenie)
2. [Architektura Watch Mode](#architektura-watch-mode)
3. [Scenariusze integracji](#scenariusze-integracji)
4. [Konfiguracja](#konfiguracja)
5. [Workflow krok po kroku](#workflow-krok-po-kroku)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)
8. [Przykłady implementacji](#przykłady-implementacji)

---

## Wprowadzenie

**KSeF Printer CLI** oferuje tryb **Watch Mode**, który automatycznie monitoruje wskazane foldery i generuje pliki PDF dla nowo pojawiających się faktur XML. To rozwiązanie idealne do integracji z systemami ERP, KSeFConnector i innymi systemami generującymi faktury elektroniczne.

### Zalety Watch Mode

- ✅ **Automatyzacja** - brak konieczności ręcznego uruchamiania
- ✅ **Integracja bez API** - nie wymaga zmian w kodzie systemu źródłowego
- ✅ **Zero-configuration** - wystarczy wskazać folder
- ✅ **Obsługa wsadowa** - przetwarza istniejące i nowe pliki
- ✅ **Niezawodność** - automatyczne przetwarzanie bez interwencji

### Kiedy używać Watch Mode?

| Scenariusz | Zalecane rozwiązanie |
|------------|---------------------|
| System ERP generuje XMLe do folderu | ✅ **Watch Mode** |
| Integracja z KSeFConnector | ✅ **Watch Mode** |
| Pojedyncze faktury on-demand | ❌ CLI pojedynczy plik |
| Aplikacja webowa | ❌ API REST |
| Przetwarzanie historyczne | ❌ CLI wsadowy |

---

## Architektura Watch Mode

### Diagram przepływu danych

```
┌─────────────────┐
│  System ERP /   │
│  KSeFConnector  │
└────────┬────────┘
         │
         │ (1) Zapisuje XML
         ↓
┌─────────────────────┐
│   Folder INPUT      │
│   faktury/in/       │
└────────┬────────────┘
         │
         │ (2) FileSystemWatcher
         │     wykrywa nowy plik
         ↓
┌─────────────────────┐
│   KSeF Printer      │
│   (Watch Mode)      │
│   ksef-pdf --watch  │
└────────┬────────────┘
         │
         │ (3) Generuje PDF
         ↓
┌─────────────────────┐
│   Ten sam folder    │
│   faktura.xml       │
│   faktura.pdf  ←─── (4) PDF obok XML
└─────────────────────┘
```

### Jak działa Watch Mode?

1. **Start aplikacji**
   ```bash
   ksef-pdf --watch faktury/in/
   ```

2. **Przetwarzanie początkowe**
   - Skanuje folder `faktury/in/`
   - Przetwarza **wszystkie istniejące** pliki XML
   - Generuje PDF dla każdego znalezionego XML

3. **Monitorowanie (FileSystemWatcher)**
   - Nasłuchuje na zdarzenia `FileCreated`
   - Wykrywa nowe pliki `*.xml`
   - Automatycznie przetwarza każdy nowy XML

4. **Generowanie PDF**
   - Parsuje XML faktury
   - Waliduje (opcjonalnie)
   - Generuje PDF z kodami QR
   - Zapisuje PDF **w tym samym folderze** co XML

5. **Ciągłe działanie**
   - Działa do momentu **Ctrl+C**
   - Nie zużywa zasobów w trybie czuwania
   - Loguje wszystkie operacje

---

## Scenariusze integracji

### Scenariusz 1: Integracja z systemem ERP

**Przypadek użycia:** System ERP generuje faktury XML do dedykowanego folderu. KSeF Printer automatycznie tworzy PDF dla każdej faktury.

```
┌──────────────┐
│  System ERP  │
│  (SAP/Sage)  │
└──────┬───────┘
       │
       │ Eksport faktur XML
       ↓
┌─────────────────────────────┐
│  C:\ERP\Faktury\Export\     │  ← Folder eksportu z ERP
└──────────────┬──────────────┘
               │
               │ Watch Mode
               ↓
┌─────────────────────────────┐
│  KSeF Printer (Watch Mode)  │
│  ksef-pdf --watch           │
│    C:\ERP\Faktury\Export\   │
│    --no-validate            │
└──────────────┬──────────────┘
               │
               │ Generuje PDF
               ↓
┌─────────────────────────────┐
│  C:\ERP\Faktury\Export\     │
│  ├── FA001_2025.xml         │
│  ├── FA001_2025.pdf  ✓      │  ← PDF obok XML
│  ├── FA002_2025.xml         │
│  └── FA002_2025.pdf  ✓      │
└─────────────────────────────┘
```

**Komenda:**
```bash
ksef-pdf --watch "C:\ERP\Faktury\Export\" --no-validate --verbose
```

**Konfiguracja ERP:**
- Ustaw folder eksportu XML: `C:\ERP\Faktury\Export\`
- Format nazwy pliku: `FA{NumerFaktury}_2025.xml`
- Automatyczny eksport: co 5 minut lub po zapisaniu faktury

---

### Scenariusz 2: Integracja z KSeFConnector

**Przypadek użycia:** KSeFConnector pobiera faktury z systemu KSeF i zapisuje je w folderze `processed/`. KSeF Printer automatycznie generuje PDF z numerem KSeF wykrytym z nazwy pliku.

```
┌──────────────────┐
│  Portal KSeF     │
│  (Ministerstwo)  │
└────────┬─────────┘
         │
         │ API KSeF
         ↓
┌──────────────────────────────┐
│  KSeFConnector               │
│  Pobiera faktury z KSeF      │
└────────┬─────────────────────┘
         │
         │ Zapisuje XML z numerem KSeF w nazwie
         ↓
┌──────────────────────────────────────────────────┐
│  C:\KSeFConnector\processed\                     │
│  faktura_6511153259-20251015-010020140418-0D.xml │
└────────┬─────────────────────────────────────────┘
         │
         │ Watch Mode + --ksef-from-filename
         ↓
┌──────────────────────────────────────────────────┐
│  KSeF Printer (Watch Mode)                       │
│  ksef-pdf --watch                                │
│    C:\KSeFConnector\processed\                   │
│    --ksef-from-filename                          │  ← Wykrywa numer z nazwy
│    --production                                  │
│    --no-validate                                 │
└────────┬─────────────────────────────────────────┘
         │
         │ Generuje PDF ONLINE z linkiem weryfikacyjnym
         ↓
┌──────────────────────────────────────────────────────────┐
│  C:\KSeFConnector\processed\                             │
│  ├── faktura_6511153259-20251015-010020140418-0D.xml     │
│  └── faktura_6511153259-20251015-010020140418-0D.pdf  ✓  │
│      (z numerem KSeF i linkiem do portalu)               │
└──────────────────────────────────────────────────────────┘
```

**Komenda:**
```bash
ksef-pdf --watch "C:\KSeFConnector\processed\" ^
  --ksef-from-filename ^
  --production ^
  --no-validate ^
  --verbose
```

**Zalety:**
- ✅ Automatyczne wykrywanie numeru KSeF z nazwy pliku
- ✅ Tryb ONLINE - PDF zawiera link do weryfikacji w portalu KSeF
- ✅ Środowisko produkcyjne (`--production`)
- ✅ Brak walidacji (faktury z KSeF są już poprawne)

---

### Scenariusz 3: Workflow z folderami Input/Output

**Przypadek użycia:** Rozdzielenie folderów wejściowych i wyjściowych. System ERP generuje XML do folderu `input/`, a PDF trafiają do `output/`.

```
┌──────────────┐
│  System ERP  │
└──────┬───────┘
       │
       │ Generuje XML
       ↓
┌─────────────────────┐
│  faktury/input/     │  ← Folder INPUT (tylko XML)
└──────┬──────────────┘
       │
       │ Watch Mode
       ↓
┌─────────────────────────────┐
│  KSeF Printer (Watch Mode)  │
└──────┬──────────────────────┘
       │
       │ Generuje PDF
       ↓
┌─────────────────────┐
│  faktury/output/    │  ← Folder OUTPUT (XML + PDF)
│  ├── FA001.xml      │  ← Skopiowany XML
│  └── FA001.pdf  ✓   │  ← Wygenerowany PDF
└─────────────────────┘
```

**⚠️ Uwaga:** KSeF Printer domyślnie zapisuje PDF **w tym samym folderze co XML**. Aby zrealizować workflow Input/Output, potrzebny jest dodatkowy skrypt.

**Rozwiązanie: PowerShell Script**
```powershell
# watch-and-move.ps1
param(
    [string]$InputDir = "C:\Faktury\input",
    [string]$OutputDir = "C:\Faktury\output"
)

# 1. Uruchom Watch Mode w tle
Start-Process -FilePath "ksef-pdf.exe" `
    -ArgumentList "--watch", $InputDir, "--no-validate" `
    -NoNewWindow `
    -PassThru

Write-Host "Watch Mode started for: $InputDir"

# 2. Monitoruj folder INPUT i przenoś PDF do OUTPUT
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $InputDir
$watcher.Filter = "*.pdf"
$watcher.EnableRaisingEvents = $true

$action = {
    $path = $Event.SourceEventArgs.FullPath
    $name = $Event.SourceEventArgs.Name

    Write-Host "PDF created: $name"

    # Przenieś XML i PDF do OUTPUT
    $xmlPath = $path -replace "\.pdf$", ".xml"

    if (Test-Path $xmlPath) {
        Move-Item -Path $xmlPath -Destination "$OutputDir\$($name -replace '\.pdf$', '.xml')"
        Write-Host "  Moved XML to OUTPUT"
    }

    Move-Item -Path $path -Destination "$OutputDir\$name"
    Write-Host "  Moved PDF to OUTPUT"
}

Register-ObjectEvent -InputObject $watcher -EventName "Created" -Action $action

Write-Host "Monitoring for PDFs... Press Ctrl+C to stop"
Wait-Event
```

**Użycie:**
```bash
powershell -ExecutionPolicy Bypass -File watch-and-move.ps1
```

---

### Scenariusz 4: Hot Folder z certyfikatem

**Przypadek użycia:** Automatyczne generowanie PDF z certyfikatem dla podpisywania kodu QR II.

```bash
ksef-pdf --watch "C:\Faktury\Hot\" ^
  --cert-thumbprint "A1B2C3D4E5F6..." ^
  --production ^
  --verbose
```

**Co się dzieje:**
1. System ERP zapisuje `faktura.xml` do `C:\Faktury\Hot\`
2. KSeF Printer wykrywa nowy plik
3. Generuje PDF z **dwoma kodami QR**:
   - **KOD QR I**: Link weryfikacyjny
   - **KOD QR II**: Podpis cyfrowy certyfikatem wystawcy
4. Zapisuje `faktura.pdf` obok XML

---

## Konfiguracja

### Parametry Watch Mode

| Parametr | Opis | Przykład |
|----------|------|----------|
| `--watch <path>` | Folder do monitorowania | `--watch faktury/` |
| `--ksef-from-filename` | Wykryj numer KSeF z nazwy pliku | Włącz dla KSeFConnector |
| `--production` | Środowisko produkcyjne KSeF | Link do `ksef.mf.gov.pl` |
| `--no-validate` | Pomiń walidację XML | Dla faktur z KSeF |
| `--verbose` / `-v` | Szczegółowe logowanie | Debug/troubleshooting |
| `--cert <path>` | Certyfikat z pliku PFX | `--cert cert.pfx` |
| `--cert-password` | Hasło do certyfikatu | `--cert-password "pass"` |
| `--cert-thumbprint` | Certyfikat z Windows Store | `--cert-thumbprint "A1B2..."` |

### Minimalna konfiguracja

```bash
# Najprostsza konfiguracja
ksef-pdf --watch faktury/
```

### Rekomendowana konfiguracja (produkcja)

```bash
# Dla integracji produkcyjnej z KSeFConnector
ksef-pdf --watch "C:\KSeFConnector\processed\" ^
  --ksef-from-filename ^
  --production ^
  --no-validate ^
  --cert-thumbprint "12:34:56:78:90:AB:CD:EF" ^
  --verbose
```

### Konfiguracja jako Windows Service

**Opcja 1: NSSM (Non-Sucking Service Manager)**

```bash
# 1. Pobierz NSSM: https://nssm.cc/download
# 2. Zainstaluj serwis
nssm install KSeFPrinterWatch "C:\Program Files\KSeF Printer\CLI\ksef-pdf.exe"
nssm set KSeFPrinterWatch AppParameters "--watch C:\Faktury\Hot\ --production --no-validate"
nssm set KSeFPrinterWatch AppDirectory "C:\Program Files\KSeF Printer\CLI"
nssm set KSeFPrinterWatch DisplayName "KSeF Printer Watch Mode"
nssm set KSeFPrinterWatch Description "Automatyczne generowanie PDF dla faktur KSeF"
nssm set KSeFPrinterWatch Start SERVICE_AUTO_START

# 3. Uruchom serwis
nssm start KSeFPrinterWatch
```

**Opcja 2: Windows Task Scheduler**

```powershell
# Utwórz zadanie w Task Scheduler
$action = New-ScheduledTaskAction -Execute "ksef-pdf.exe" `
    -Argument "--watch C:\Faktury\Hot\ --production --no-validate" `
    -WorkingDirectory "C:\Program Files\KSeF Printer\CLI"

$trigger = New-ScheduledTaskTrigger -AtStartup

$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask -TaskName "KSeF Printer Watch Mode" `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -User "SYSTEM" `
    -RunLevel Highest
```

---

## Workflow krok po kroku

### Przygotowanie środowiska

**Krok 1: Instalacja KSeF Printer**

```bash
# Opcja A: MSI Installer
msiexec /i KSeFPrinter.msi

# Opcja B: Ręczna instalacja
# 1. Rozpakuj Release do C:\KSeFPrinter
# 2. Dodaj do PATH
```

**Krok 2: Weryfikacja instalacji**

```bash
ksef-pdf --version
# Powinno wyświetlić: KSeF Printer CLI v1.0.0
```

**Krok 3: Utworzenie struktury folderów**

```bash
mkdir C:\Faktury\Hot
mkdir C:\Faktury\Archive
```

**Krok 4: Test pojedynczego pliku**

```bash
# Wygeneruj testowy PDF
ksef-pdf C:\Faktury\Hot\test.xml --no-validate

# Sprawdź czy powstał PDF
dir C:\Faktury\Hot\test.pdf
```

---

### Uruchomienie Watch Mode

**Krok 1: Test w konsoli (foreground)**

```bash
# Uruchom w bieżącej konsoli
ksef-pdf --watch "C:\Faktury\Hot\" --verbose
```

**Wynik w konsoli:**
```
info: Program[0]
      === KSeF Printer - Watch Mode ===
info: Program[0]
      Monitorowanie folderów:
info: Program[0]
        - C:\Faktury\Hot\
info: Program[0]

info: Program[0]
      === Przetwarzanie istniejących plików ===
info: Program[0]
      Znaleziono 2 pliki XML
info: Program[0]

info: Program[0]
      [1/2] Przetwarzanie: FA001_2025.xml
info: Program[0]
      ✓ PDF wygenerowany: FA001_2025.pdf (105 KB)
info: Program[0]

info: Program[0]
      [2/2] Przetwarzanie: FA002_2025.xml
info: Program[0]
      ✓ PDF wygenerowany: FA002_2025.pdf (98 KB)
info: Program[0]

info: Program[0]
      === Oczekiwanie na nowe pliki ===
info: Program[0]
      Naciśnij Ctrl+C aby zatrzymać
```

**Krok 2: Test dodania nowego pliku**

W **drugim terminalu** skopiuj nowy plik:
```bash
copy faktura_nowa.xml C:\Faktury\Hot\
```

W **pierwszym terminalu** zobaczysz:
```
info: Program[0]

info: Program[0]
      Wykryto nowy plik: faktura_nowa.xml
info: KSeFPrinter.InvoicePrinterService[0]
      Rozpoczęcie generowania PDF z pliku: faktura_nowa.xml
info: Program[0]
      ✓ PDF wygenerowany: faktura_nowa.pdf (102 KB)
```

**Krok 3: Zatrzymanie Watch Mode**

Naciśnij **Ctrl+C** w terminalu:
```
info: Program[0]

info: Program[0]
      === Watch Mode zakończony ===
info: Program[0]
      Przetworzone pliki: 3
```

---

### Konfiguracja w produkcji

**Krok 1: Utwórz skrypt startowy**

Utwórz plik `start-watch-mode.cmd`:
```cmd
@echo off
cd "C:\Program Files\KSeF Printer\CLI"

echo Starting KSeF Printer Watch Mode...
echo Monitoring: C:\Faktury\Hot\
echo.

ksef-pdf.exe ^
  --watch "C:\Faktury\Hot\" ^
  --production ^
  --no-validate ^
  --cert-thumbprint "12:34:56:78:90:AB:CD:EF" ^
  --verbose

pause
```

**Krok 2: Test skryptu**

```bash
start-watch-mode.cmd
```

**Krok 3: Zainstaluj jako Windows Service**

Zobacz [Konfiguracja jako Windows Service](#konfiguracja-jako-windows-service)

---

## Best Practices

### 1. Monitorowanie i logowanie

**Zalecenie:** Zawsze używaj `--verbose` w produkcji dla pełnych logów.

```bash
ksef-pdf --watch faktury/ --verbose > logs\ksef-watch.log 2>&1
```

**Rotacja logów (PowerShell):**
```powershell
# rotate-logs.ps1
$logFile = "C:\Logs\ksef-watch.log"
$maxSize = 10MB

if ((Get-Item $logFile).Length -gt $maxSize) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    Move-Item $logFile "$logFile.$timestamp"

    # Usuń logi starsze niż 30 dni
    Get-ChildItem "C:\Logs\ksef-watch.log.*" |
        Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
        Remove-Item
}
```

### 2. Struktura folderów

**Zalecenie:** Rozdziel foldery według statusu przetwarzania.

```
C:\Faktury\
├── input\          ← System ERP zapisuje tutaj XML
├── processing\     ← Temporary folder (opcjonalnie)
├── output\         ← PDF gotowe do archiwizacji
└── error\          ← Pliki z błędami
```

**Skrypt przetwarzania:**
```powershell
# 1. Przenieś XML z input do processing
Move-Item "C:\Faktury\input\*.xml" "C:\Faktury\processing\"

# 2. Watch Mode w processing
ksef-pdf --watch "C:\Faktury\processing\" --no-validate

# 3. Po wygenerowaniu PDF, przenieś do output
Move-Item "C:\Faktury\processing\*" "C:\Faktury\output\"
```

### 3. Obsługa błędów

**Zalecenie:** Monitoruj błędy i przenoś problematyczne pliki do folderu `error/`.

```bash
# W razie błędu, XML trafia do error/
ksef-pdf --watch faktury/ --on-error move-to-error
```

**Skrypt PowerShell do obsługi błędów:**
```powershell
# error-handler.ps1
$watchFolder = "C:\Faktury\processing"
$errorFolder = "C:\Faktury\error"

# Monitoruj logi KSeF Printer
Get-Content "C:\Logs\ksef-watch.log" -Wait -Tail 1 | ForEach-Object {
    if ($_ -match "error.*faktura_(.+?)\.xml") {
        $fileName = $Matches[1]
        $xmlPath = "$watchFolder\faktura_$fileName.xml"

        if (Test-Path $xmlPath) {
            Move-Item $xmlPath "$errorFolder\"
            Write-Host "Moved to error: faktura_$fileName.xml"
        }
    }
}
```

### 4. Wydajność

**Zalecenie:** Dla dużych wolumenów używaj równoległego przetwarzania.

```bash
# Podziel faktury na wiele folderów i uruchom wiele instancji
ksef-pdf --watch faktury/batch1/ &
ksef-pdf --watch faktury/batch2/ &
ksef-pdf --watch faktury/batch3/ &
```

### 5. Backup i archiwizacja

**Zalecenie:** Regularnie archiwizuj przetworzone pliki.

```powershell
# archive-daily.ps1
$outputDir = "C:\Faktury\output"
$archiveDir = "C:\Faktury\archive"
$date = Get-Date -Format "yyyy-MM-dd"

# Utwórz archiwum dzienne
$zipFile = "$archiveDir\faktury_$date.zip"
Compress-Archive -Path "$outputDir\*" -DestinationPath $zipFile

# Usuń pliki z output (zostaw ostatnie 7 dni)
Get-ChildItem $outputDir -File |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
    Remove-Item
```

### 6. Bezpieczeństwo

**Zalecenie:** Ogranicz dostęp do folderów i certyfikatów.

```powershell
# Ustaw uprawnienia tylko dla użytkownika serwisu
$path = "C:\Faktury"
$acl = Get-Acl $path
$acl.SetAccessRuleProtection($true, $false)
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "NT AUTHORITY\SYSTEM", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.AddAccessRule($rule)
Set-Acl $path $acl
```

---

## Troubleshooting

### Problem 1: Watch Mode nie wykrywa nowych plików

**Objaw:**
```
info: Program[0]
      === Oczekiwanie na nowe pliki ===
```
Nowy plik jest kopiowany do folderu, ale nic się nie dzieje.

**Rozwiązanie:**

1. **Sprawdź uprawnienia do folderu:**
   ```bash
   icacls "C:\Faktury\Hot"
   # Powinno być: BUILTIN\Users:(OI)(CI)(RX)
   ```

2. **Sprawdź czy plik ma rozszerzenie `.xml`:**
   ```bash
   # Włącz wyświetlanie rozszerzeń w Windows Explorer
   # lub użyj:
   dir /x C:\Faktury\Hot\
   ```

3. **Sprawdź czy plik nie jest zablokowany:**
   ```powershell
   # Niektóre programy trzymają lock na pliku
   # Poczekaj aż plik przestanie być używany
   ```

4. **Użyj --verbose dla debugowania:**
   ```bash
   ksef-pdf --watch faktury/ --verbose
   ```

---

### Problem 2: "Faktura nie przeszła walidacji"

**Objaw:**
```
error: KSeFPrinter.Validators.InvoiceValidator[0]
       Faktura nie przeszła walidacji: Nieprawidłowy NIP sprzedawcy
```

**Rozwiązanie:**

Użyj `--no-validate` aby pominąć walidację:
```bash
ksef-pdf --watch faktury/ --no-validate
```

**Kiedy używać `--no-validate`:**
- ✅ Faktury z systemu KSeF (już zwalidowane)
- ✅ Testowe faktury z przykładowymi danymi
- ❌ Faktury z ERP (mogą zawierać błędy)

---

### Problem 3: PDF nie ma numeru KSeF

**Objaw:**
PDF pokazuje "Tryb: OFFLINE" zamiast numeru KSeF.

**Przyczyna:**
Numer KSeF nie jest wykrywany z nazwy pliku.

**Rozwiązanie:**

Dodaj parametr `--ksef-from-filename`:
```bash
ksef-pdf --watch faktury/ --ksef-from-filename
```

**Format nazwy pliku:**
```
faktura_6511153259-20251015-010020140418-0D.xml
        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        Numer KSeF musi być na końcu przed .xml
```

---

### Problem 4: "Certyfikat nie zawiera klucza prywatnego"

**Objaw:**
```
error: Certyfikat nie zawiera klucza prywatnego
```

**Rozwiązanie:**

1. **Sprawdź certyfikat w Windows Store:**
   ```bash
   certmgr.msc
   # Personal → Certificates
   # Sprawdź czy certyfikat ma ikonę klucza
   ```

2. **Użyj certyfikatu z pliku PFX:**
   ```bash
   ksef-pdf --watch faktury/ --cert cert.pfx --cert-password "haslo"
   ```

3. **Sprawdź uprawnienia do klucza:**
   ```powershell
   # Nadaj uprawnienia do klucza prywatnego
   $cert = Get-ChildItem Cert:\CurrentUser\My\<thumbprint>
   $keyPath = "$env:APPDATA\Microsoft\Crypto\RSA\" + $cert.PrivateKey.CspKeyContainerInfo.UniqueKeyContainerName
   icacls $keyPath /grant "NT AUTHORITY\SYSTEM:F"
   ```

---

### Problem 5: Watch Mode się zawiesza

**Objaw:**
Watch Mode przestaje reagować na nowe pliki po kilku godzinach.

**Rozwiązanie:**

1. **Uruchom jako Windows Service z auto-restart:**
   ```bash
   nssm set KSeFPrinterWatch AppExit Default Restart
   nssm set KSeFPrinterWatch AppRestartDelay 5000
   ```

2. **Monitoruj działanie przez Task Scheduler:**
   ```powershell
   # health-check.ps1
   $process = Get-Process ksef-pdf -ErrorAction SilentlyContinue
   if (-not $process) {
       Write-Host "Watch Mode is not running! Restarting..."
       Start-Process "ksef-pdf.exe" -ArgumentList "--watch", "C:\Faktury\Hot\"
   }
   ```

3. **Sprawdź pamięć:**
   ```bash
   # Watch Mode zużywa ~50 MB RAM
   # Jeśli więcej - restart może pomóc
   ```

---

## Przykłady implementacji

### Przykład 1: Integracja z SAP

**Scenariusz:** SAP eksportuje faktury XML do folderu sieciowego. KSeF Printer generuje PDF i archiwizuje.

**Struktura:**
```
\\SERVER\Faktury\
├── export\          ← SAP zapisuje tutaj
├── processing\      ← Watch Mode tutaj
├── archive\         ← Archiwum
└── logs\
```

**Skrypt PowerShell:**
```powershell
# sap-integration.ps1

$exportDir = "\\SERVER\Faktury\export"
$processingDir = "\\SERVER\Faktury\processing"
$archiveDir = "\\SERVER\Faktury\archive"

# 1. Przenieś nowe XMLe z export do processing
Write-Host "Moving new invoices from export..."
Get-ChildItem "$exportDir\*.xml" | ForEach-Object {
    Move-Item $_.FullName -Destination $processingDir
}

# 2. Uruchom Watch Mode
Write-Host "Starting Watch Mode..."
Start-Process -FilePath "ksef-pdf.exe" `
    -ArgumentList "--watch", $processingDir, "--no-validate", "--verbose" `
    -WorkingDirectory "C:\Program Files\KSeF Printer\CLI" `
    -PassThru

# 3. Archiwizuj przetworzone faktury
Write-Host "Archiving processed invoices..."
Start-Sleep -Seconds 5  # Poczekaj na wygenerowanie PDF

Get-ChildItem "$processingDir\*.pdf" | ForEach-Object {
    $xmlFile = $_.FullName -replace "\.pdf$", ".xml"

    # Przenieś PDF i XML do archiwum
    Move-Item $_.FullName -Destination $archiveDir
    if (Test-Path $xmlFile) {
        Move-Item $xmlFile -Destination $archiveDir
    }
}

Write-Host "Integration completed."
```

**Uruchomienie:**
```bash
# Ręcznie
powershell -ExecutionPolicy Bypass -File sap-integration.ps1

# Task Scheduler (co 10 minut)
schtasks /create /tn "SAP KSeF Integration" /tr "powershell -File C:\Scripts\sap-integration.ps1" /sc minute /mo 10
```

---

### Przykład 2: Integracja z KSeFConnector (kompletna)

**Konfiguracja KSeFConnector:**

```json
{
  "KSeF": {
    "Environment": "Production",
    "DownloadPath": "C:\\KSeFConnector\\processed"
  }
}
```

**Skrypt Watch Mode:**
```powershell
# ksef-connector-watch.ps1

$processedDir = "C:\KSeFConnector\processed"
$archiveDir = "C:\KSeFConnector\archive"
$logFile = "C:\Logs\ksef-connector-watch.log"

# Utwórz folder archiwum jeśli nie istnieje
if (-not (Test-Path $archiveDir)) {
    New-Item -ItemType Directory -Path $archiveDir | Out-Null
}

# Uruchom Watch Mode z logowaniem
ksef-pdf.exe `
    --watch $processedDir `
    --ksef-from-filename `
    --production `
    --no-validate `
    --cert-thumbprint "12:34:56:78:90:AB:CD:EF" `
    --verbose `
    2>&1 | Tee-Object -FilePath $logFile -Append
```

**Uruchomienie jako Windows Service:**
```bash
# Instalacja
nssm install KSeFConnectorWatch powershell.exe
nssm set KSeFConnectorWatch AppParameters "-ExecutionPolicy Bypass -File C:\Scripts\ksef-connector-watch.ps1"
nssm set KSeFConnectorWatch AppDirectory "C:\Scripts"
nssm set KSeFConnectorWatch DisplayName "KSeF Connector Watch Mode"
nssm set KSeFConnectorWatch Start SERVICE_AUTO_START

# Uruchomienie
nssm start KSeFConnectorWatch

# Status
nssm status KSeFConnectorWatch
```

---

### Przykład 3: Multi-tenant Watch Mode

**Scenariusz:** Jeden serwer obsługuje wiele firm. Każda firma ma własny folder.

**Struktura:**
```
C:\KSeF\Tenants\
├── Firma_A\
│   ├── input\
│   └── output\
├── Firma_B\
│   ├── input\
│   └── output\
└── Firma_C\
    ├── input\
    └── output\
```

**Skrypt Master:**
```powershell
# multi-tenant-watch.ps1

$tenantsDir = "C:\KSeF\Tenants"
$tenants = Get-ChildItem $tenantsDir -Directory

foreach ($tenant in $tenants) {
    $inputDir = "$($tenant.FullName)\input"
    $outputDir = "$($tenant.FullName)\output"

    Write-Host "Starting Watch Mode for: $($tenant.Name)"

    Start-Process -FilePath "ksef-pdf.exe" `
        -ArgumentList "--watch", $inputDir, "--no-validate", "--verbose" `
        -NoNewWindow `
        -PassThru

    # Każda instancja działa w osobnym procesie
    Start-Sleep -Seconds 2
}

Write-Host "All Watch Mode instances started."
Write-Host "Press Ctrl+C to stop all."
Wait-Event
```

---

## Podsumowanie

### Kluczowe punkty

✅ **Watch Mode** to najlepsze rozwiązanie do integracji z systemami generującymi pliki XML
✅ Automatyczne przetwarzanie bez konieczności zmian w kodzie systemu źródłowego
✅ Tryb ciągły (24/7) - idealny do uruchomienia jako Windows Service
✅ Obsługa certyfikatów i automatyczne wykrywanie numerów KSeF
✅ Elastyczna konfiguracja - od prostej do zaawansowanej

### Następne kroki

1. **Testowanie:** Przetestuj Watch Mode na próbnych danych
2. **Konfiguracja:** Dostosuj parametry do swoich potrzeb
3. **Produkcja:** Uruchom jako Windows Service
4. **Monitoring:** Wdróż skrypty monitorowania i archiwizacji

### Wsparcie

- **Dokumentacja CLI:** `KSeFPrinter.CLI\README.md`
- **Dokumentacja API:** `API_SPECIFICATION.md`
- **GitHub Issues:** https://github.com/ptadeusz/KSeFPrinter/issues

---

**Wersja dokumentu:** 1.0
**Data:** 2025-11-02
**Autor:** KSeF Printer Team
