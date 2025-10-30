# Budowanie Instalatora MSI

Ten dokument opisuje jak zbudować instalator MSI dla KSeF Printer.

## Wymagania

1. **WiX Toolset 5.x lub nowszy**
   - Instalacja: https://wixtoolset.org/
   - Lub przez dotnet: `dotnet tool install --global wix`

2. **.NET SDK 9.0 lub nowszy**
   - Pobierz z: https://dotnet.microsoft.com/download

## Szybki start

### Automatyczne budowanie (Windows)

Użyj dołączonego skryptu w katalogu `KSeFPrinter.Installer`:

```cmd
cd KSeFPrinter.Installer
build.cmd
```

Lub bezpośrednio przez PowerShell:

```powershell
cd KSeFPrinter.Installer
.\build.ps1
```

### Budowanie ręczne

1. **Opublikuj aplikacje CLI i API:**

```cmd
cd KSeFPrinter.CLI
dotnet publish -c Release -r win-x64 --self-contained false

cd ..\KSeFPrinter.API
dotnet publish -c Release -r win-x64 --self-contained false
cd ..
```

2. **Zbuduj instalator:**

```cmd
cd KSeFPrinter.Installer
dotnet build -c Release
```

3. **Plik MSI zostanie utworzony w:**

```
KSeFPrinter.Installer\bin\x64\Release\KSeFPrinter.msi
```

## Czyszczenie buildów

```cmd
cd KSeFPrinter.Installer
build.cmd clean
```

Lub przez PowerShell:

```powershell
.\build.ps1 -Clean
```

## Instalacja

### Instalacja standardowa (GUI)

Uruchom plik MSI dwukrotnie klikając lub przez wiersz poleceń:

```cmd
msiexec /i KSeFPrinter.msi
```

### Instalacja cicha (bez GUI)

```cmd
msiexec /i KSeFPrinter.msi /qn
```

### Instalacja z logowaniem

```cmd
msiexec /i KSeFPrinter.msi /l*v install.log
```

## Deinstalacja

### Przez Panel Sterowania

1. Otwórz "Dodaj lub usuń programy"
2. Znajdź "KSeF Printer"
3. Kliknij "Odinstaluj"

### Przez wiersz poleceń

```cmd
msiexec /x KSeFPrinter.msi
```

Lub cicha deinstalacja:

```cmd
msiexec /x KSeFPrinter.msi /qn
```

## Komponenty instalacji

Instalator zawiera następujące komponenty:

### 1. KSeF Printer CLI (wymagane)
- Aplikacja konsolowa `ksef-pdf.exe`
- Lokalizacja: `C:\Program Files\KSeF Printer\CLI`
- Dodaje CLI do zmiennej PATH
- Skrót w menu Start

### 2. KSeF Printer API (opcjonalne)
- ASP.NET Core Web API
- Lokalizacja: `C:\Program Files\KSeF Printer\API`
- Domyślny port: 5000
- Skrót w menu Start

### 3. Przykłady (opcjonalne)
- Przykładowe faktury XML
- Wygenerowane PDF-y
- Lokalizacja: `C:\Program Files\KSeF Printer\Examples`

### 4. Dokumentacja (zalecane)
- README.md
- Dokumentacja użytkownika

## Struktura katalogów po instalacji

```
C:\Program Files\KSeF Printer\
├── CLI\
│   ├── ksef-pdf.exe
│   ├── ksef-pdf.dll
│   └── ... (dependencies)
├── API\
│   ├── KSeFPrinter.API.exe
│   ├── KSeFPrinter.API.dll
│   ├── appsettings.json
│   └── ... (dependencies)
├── Examples\
│   ├── xml\
│   │   ├── FakturaTEST017.xml
│   │   └── ...
│   └── output\
│       ├── FakturaTEST017.pdf
│       └── ...
└── README.md
```

## Rozwiązywanie problemów

### Błąd: "WiX nie jest zainstalowany"

Zainstaluj WiX Toolset:

```cmd
dotnet tool install --global wix
```

### Błąd podczas budowania: "Cannot find publish directory"

Upewnij się, że aplikacje zostały opublikowane przed budowaniem instalatora:

```cmd
cd KSeFPrinter.CLI
dotnet publish -c Release -r win-x64

cd ..\KSeFPrinter.API
dotnet publish -c Release -r win-x64
```

### Błąd: "Codepage 1252"

Ten błąd został naprawiony w Package.wxs przez dodanie `Codepage="1250"`. Jeśli nadal występuje, sprawdź czy plik Package.wxs zawiera ten atrybut.

## Dostosowywanie instalatora

### Zmiana wersji

Edytuj plik `KSeFPrinter.Installer\Package.wxs`:

```xml
<Package Name="KSeF Printer"
         Version="1.0.0.0"
         ...>
```

Lub zaktualizuj `Directory.Build.props` w głównym katalogu:

```xml
<Version>1.0.0</Version>
```

### Dodanie ikony

1. Utwórz plik `icon.ico` (32x32 lub 48x48 px)
2. Umieść w katalogu `KSeFPrinter.Installer`
3. Odkomentuj w `Package.wxs`:

```xml
<Icon Id="ProductIcon" SourceFile="icon.ico" />
<Property Id="ARPPRODUCTICON" Value="ProductIcon" />
```

### Zmiana domyślnej lokalizacji instalacji

Edytuj `Package.wxs`:

```xml
<StandardDirectory Id="ProgramFiles64Folder">
  <Directory Id="INSTALLFOLDER" Name="KSeF Printer">
```

## Dodatkowe opcje msiexec

```cmd
# Instalacja z wyborem komponentów
msiexec /i KSeFPrinter.msi ADDLOCAL=CLIFeature,DocumentationFeature

# Instalacja z custom lokalizacją
msiexec /i KSeFPrinter.msi INSTALLFOLDER="C:\Custom\Path"

# Instalacja z restart systemu jeśli potrzebny
msiexec /i KSeFPrinter.msi /forcerestart
```

## Więcej informacji

- Dokumentacja WiX: https://wixtoolset.org/docs/
- .NET Deployment: https://learn.microsoft.com/en-us/dotnet/core/deploying/
