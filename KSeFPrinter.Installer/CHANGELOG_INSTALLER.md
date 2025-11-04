# KSeF Printer Installer - Changelog

## [2025-11-04] - Aktualizacja zgodnie z KSeF Connector

### ✅ Dodane funkcje

#### 1. **Custom Actions - Automatyczne zabijanie procesów**
- Dodano `KillKSeFPrinterProcesses` - zabija procesy przed deinstalacją:
  - `KSeFPrinter.API.exe`
  - `ksef-pdf.exe`
- Zapobiega błędom "plik w użyciu" podczas deinstalacji/reinstalacji

#### 2. **Automatyczne czyszczenie plików przy deinstalacji**
- Pliki konfiguracyjne API:
  - `appsettings.json`
  - `appsettings.Development.json`
- Pliki licencji:
  - `license*.lic` (w głównym folderze i API)

#### 3. **Folder logów z automatycznym czyszczeniem**
- Utworzenie folderu `API\logs`
- Automatyczne usuwanie wszystkich plików logów przy deinstalacji
- Component `APILogsComponent` zarządza cyklem życia folderu

#### 4. **Język polski**
- `Language="1045"` w Package.wxs
- `Cultures="pl-PL"` w .wixproj
- Polska lokalizacja interfejsu instalatora

#### 5. **MediaTemplate**
- `<MediaTemplate EmbedCab="yes" />` - wszystko w jednym pliku MSI
- Brak zewnętrznych zależności (plików CAB)

#### 6. **Namespace util**
- Dodano `xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util"`
- Przygotowanie pod przyszłe rozszerzenia (np. Windows Service)

### 🔧 Techniczne zmiany

#### Package.wxs
```xml
<!-- PRZED -->
<Package Name="KSeF Printer"
         Manufacturer="KSeF Team"
         Version="1.0.0.0"
         ...
         Codepage="1250">

<!-- PO -->
<Package Name="KSeF Printer"
         Manufacturer="KSeF Team"
         Version="1.0.0.0"
         Language="1045"
         Codepage="1250"
         ...>
```

#### .wixproj
```xml
<!-- DODANE -->
<TargetFramework>net9.0-windows</TargetFramework>
<Version>1.0.0</Version>
<Cultures>pl-PL</Cultures>
```

### 📋 Struktura katalogów (AKTUALIZOWANA)

```
C:\Program Files\KSeF Printer\
├── CLI\                       # Aplikacja konsolowa
├── API\                       # ASP.NET Core Web API
│   └── logs\                  # ✅ NOWE: Folder logów (auto-czyszczony)
├── Examples\
│   ├── xml\
│   └── output\
└── Documentation\
```

### 🔄 Komponenty instalacyjne

| Component ID | Cel | Nowy? |
|--------------|-----|-------|
| `INSTALLFOLDER_CleanupComponent` | Czyszczenie głównego folderu | ✅ TAK |
| `APILogsComponent` | Zarządzanie folderem logów | ✅ TAK |
| `CLIComponents` | Pliki CLI | NIE |
| `APIComponents` | Pliki API | NIE |

### 📝 Zgodność z KSeF Connector

Instalator KSeF Printer jest teraz zgodny z najlepszymi praktykami z KSeF Connector:

| Funkcja | KSeF Connector | KSeF Printer (przed) | KSeF Printer (po) |
|---------|----------------|----------------------|-------------------|
| Kill processes przed uninstall | ✅ | ❌ | ✅ |
| Cleanup config files | ✅ | ❌ | ✅ |
| Cleanup license files | ✅ | ❌ | ✅ |
| Folder logs z auto-cleanup | ✅ | ❌ | ✅ |
| Język polski | ✅ | ❌ | ✅ |
| MediaTemplate (embedded CAB) | ✅ | ❌ | ✅ |
| Multiple features | ✅ | ✅ | ✅ |
| Start Menu shortcuts | ✅ | ✅ | ✅ |

### 🎯 Dlaczego te zmiany?

1. **Lepsza obsługa reinstalacji** - zabijanie procesów zapobiega błędom
2. **Czystsza deinstalacja** - nie zostawia śmieci w systemie
3. **Zgodność z ekosystemem** - KSeF Printer i KSeF Connector działają podobnie
4. **Profesjonalny wygląd** - polski interfejs, lepsze UX

### ⚠️ Breaking Changes

**BRAK** - wszystkie zmiany są backwards compatible. Istniejące instalacje można zaktualizować bez problemów dzięki stałemu `UpgradeCode`.

### 📚 Dokumentacja

- Zaktualizowano `README.md` z opisem nowych funkcji
- Dodano sekcję "Nowe funkcje (zgodne z KSeF Connector)"
- Dodano ten plik CHANGELOG

---

**Autor:** Claude (Anthropic)
**Data:** 2025-11-04
**Wersja instalatora:** 1.0.0
**WiX Toolset:** 5.0.2
