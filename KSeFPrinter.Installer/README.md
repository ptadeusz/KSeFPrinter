# KSeF Printer - Instalator MSI

Ten katalog zawiera projekt instalatora MSI dla KSeF Printer zbudowany przy użyciu **WiX Toolset 5**.

## 📋 Wymagania

- **WiX Toolset 5.0.2** lub nowszy
- **.NET SDK 9.0** lub nowszy
- **Windows 10/11** lub **Windows Server 2019/2022**

## 🔨 Budowanie instalatora

### 1. Zbuduj projekty Release

Przed budowaniem instalatora musisz zbudować projekty w trybie Release:

```bash
cd ..
dotnet build -c Release
```

### 2. Opublikuj aplikacje

```bash
cd KSeFPrinter.CLI
dotnet publish -c Release -r win-x64 --self-contained false

cd ../KSeFPrinter.API
dotnet publish -c Release -r win-x64 --self-contained false
cd ..
```

### 3. Zbuduj instalator MSI

```bash
cd KSeFPrinter.Installer
dotnet build -c Release
```

Plik MSI zostanie utworzony w: `bin\Release\net9.0\win-x64\KSeFPrinter.msi`

## Struktura instalacji

Instalator tworzy następującą strukturę:

```
C:\Program Files\KSeF Printer\
├── CLI\                       # Aplikacja konsolowa (ksef-pdf.exe)
├── API\                       # ASP.NET Core Web API
└── Examples\                  # Przykładowe faktury XML i PDF
    ├── xml\
    └── output\
```

## Komponenty instalacji

1. **KSeF Printer CLI** (wymagane)
   - Aplikacja konsolowa do generowania PDF z faktur XML
   - Dodaje CLI do PATH systemu
   - Skrót w menu Start

2. **KSeF Printer API** (opcjonalne)
   - ASP.NET Core Web API z interfejsem Swagger
   - Skrót w menu Start do uruchamiania API

3. **Przykłady** (opcjonalne)
   - Przykładowe faktury XML
   - Wygenerowane PDF-y

4. **Dokumentacja** (zalecane)
   - README.md
   - Dokumentacja użytkownika

## ✨ Nowe funkcje (zgodne z KSeF Connector)

Instalator KSeF Printer został zaktualizowany o następujące funkcje z KSeF Connector:

### 🔧 Mechanizmy czyszczenia przy deinstalacji

1. **Automatyczne zabijanie procesów** przed deinstalacją
   - `KSeFPrinter.API.exe` - proces API
   - `ksef-pdf.exe` - proces CLI
   - Zapobiega błędom "plik w użyciu" podczas deinstalacji

2. **Automatyczne usuwanie plików**
   - ✅ Pliki konfiguracyjne (`appsettings.json`, `appsettings.Development.json`)
   - ✅ Pliki licencji (`license*.lic`)
   - ✅ Wszystkie pliki logów w folderze `API\logs`

3. **Folder logów**
   - Automatycznie tworzony folder `API\logs` dla aplikacji API
   - Czyszczony podczas deinstalacji

### 🌍 Język polski

- Interfejs instalatora w języku polskim (`Language="1045"`, `Cultures="pl-PL"`)

### 📦 MediaTemplate

- Wbudowany plik CAB (`EmbedCab="yes"`) - pojedynczy plik MSI bez zewnętrznych zależności

## 📝 Notatki

- Ikona `icon.ico` powinna być umieszczona w tym katalogu przed budowaniem (opcjonalne)
- Licencja jest wyświetlana podczas instalacji (`License.rtf`)
- **UpgradeCode jest stały** - pozwala na bezproblemowe upgrady i reinstalacje
- Instalator obsługuje **tylko Windows x64** (64-bit)
