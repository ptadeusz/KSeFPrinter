# Instrukcja pobierania plików z repozytoriów KSeF

## Pliki zawarte w tej paczce

### Z dokumentacji projektu (już pobrane):
✅ Dokumentacja kodów QR (kody-qr.md)
✅ Dokumentacja certyfikatów (certyfikaty-KSeF.md)
✅ Dokumentacja numeru KSeF (numer-ksef.md)
✅ Fragmenty schematu FA(3) (dostępne w project knowledge)
✅ Przykładowe szablony FA(3) XML

## Pliki do pobrania ręcznie z GitHub

### Z repozytorium `ksef-client-csharp`

#### 1. Szablony faktur XML
**Lokalizacja:** `KSeF.Client.Tests.Core/Templates/`

- `invoice-template-fa-3.xml`
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3.xml

- `invoice-template-fa-3-with-custom-Subject2.xml`
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3-with-custom-Subject2.xml

- `invoice-template-fa-2.xml` (opcjonalnie)
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-2.xml

#### 2. Testy E2E - Kody QR
**Lokalizacja:** `KSeF.Client.Tests.Core/E2E/QrCode/`

- `QrCodeOfflineE2ETests.cs` ⭐ NAJWAŻNIEJSZY!
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeOfflineE2ETests.cs

- `QrCodeE2ETests.cs`
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeE2ETests.cs

#### 3. Serwisy - Kryptografia i QR
**Lokalizacja:** `KSeF.Client/Api/Services/`

- `CryptographyService.cs` ⭐
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/CryptographyService.cs

- `VerificationLinkService.cs`
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/VerificationLinkService.cs

- `QrCodeService.cs`
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/QrCodeService.cs

#### 4. Walidator numeru KSeF
**Lokalizacja:** `KSeF.Client.Core/`

- `KsefNumberValidator.cs`
  https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Core/KsefNumberValidator.cs

### Z repozytorium `ksef-docs`

#### 1. Schemat XSD FA(3)
- `schemat-FA(3)-v1-0E.xsd`
  https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/schemat-FA(3)-v1-0E.xsd

#### 2. Dokumentacja
- `kody-qr.md` ✅ (już w paczce)
- `certyfikaty-KSeF.md` ✅ (już w paczce)
- `numer-ksef.md` 
  https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/numer-ksef.md

- `tryby-offline.md`
  https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/offline/tryby-offline.md

## Jak pobrać wszystkie pliki automatycznie

### Opcja 1: Ręczne pobieranie
1. Otwórz każdy link w przeglądarce
2. Kliknij prawym przyciskiem myszy → "Zapisz jako"
3. Zachowaj strukturę katalogów zgodnie z repozytorium

### Opcja 2: Użycie wget (Linux/Mac/WSL)
Stwórz plik `download-files.sh`:

```bash
#!/bin/bash

# Utwórz strukturę katalogów
mkdir -p ksef-client-csharp/{Templates,E2E/QrCode,Api/Services,Core}
mkdir -p ksef-docs/{faktury,offline}

# Pobierz pliki z ksef-client-csharp
cd ksef-client-csharp

# Templates
wget -P Templates/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3.xml
wget -P Templates/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3-with-custom-Subject2.xml
wget -P Templates/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-2.xml

# E2E Tests
wget -P E2E/QrCode/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeOfflineE2ETests.cs
wget -P E2E/QrCode/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeE2ETests.cs

# Api Services
wget -P Api/Services/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/CryptographyService.cs
wget -P Api/Services/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/VerificationLinkService.cs
wget -P Api/Services/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/QrCodeService.cs

# Core
wget -P Core/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Core/KsefNumberValidator.cs

cd ..

# Pobierz pliki z ksef-docs
cd ksef-docs

wget -P faktury/ https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/schemat-FA(3)-v1-0E.xsd
wget -P faktury/ https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/numer-ksef.md
wget https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/kody-qr.md
wget https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/certyfikaty-KSeF.md
wget -P offline/ https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/offline/tryby-offline.md

cd ..

echo "Pobieranie zakończone!"
```

Następnie:
```bash
chmod +x download-files.sh
./download-files.sh
```

### Opcja 3: Użycie PowerShell (Windows)
Stwórz plik `download-files.ps1`:

```powershell
# Utwórz strukturę katalogów
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\Templates"
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\E2E\QrCode"
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\Api\Services"
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\Core"
New-Item -ItemType Directory -Force -Path "ksef-docs\faktury"
New-Item -ItemType Directory -Force -Path "ksef-docs\offline"

# Pobierz pliki z ksef-client-csharp
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3.xml" -OutFile "ksef-client-csharp\Templates\invoice-template-fa-3.xml"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3-with-custom-Subject2.xml" -OutFile "ksef-client-csharp\Templates\invoice-template-fa-3-with-custom-Subject2.xml"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-2.xml" -OutFile "ksef-client-csharp\Templates\invoice-template-fa-2.xml"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeOfflineE2ETests.cs" -OutFile "ksef-client-csharp\E2E\QrCode\QrCodeOfflineE2ETests.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeE2ETests.cs" -OutFile "ksef-client-csharp\E2E\QrCode\QrCodeE2ETests.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/CryptographyService.cs" -OutFile "ksef-client-csharp\Api\Services\CryptographyService.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/VerificationLinkService.cs" -OutFile "ksef-client-csharp\Api\Services\VerificationLinkService.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/QrCodeService.cs" -OutFile "ksef-client-csharp\Api\Services\QrCodeService.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Core/KsefNumberValidator.cs" -OutFile "ksef-client-csharp\Core\KsefNumberValidator.cs"

# Pobierz pliki z ksef-docs
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/schemat-FA(3)-v1-0E.xsd" -OutFile "ksef-docs\faktury\schemat-FA(3)-v1-0E.xsd"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/numer-ksef.md" -OutFile "ksef-docs\faktury\numer-ksef.md"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/kody-qr.md" -OutFile "ksef-docs\kody-qr.md"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/certyfikaty-KSeF.md" -OutFile "ksef-docs\certyfikaty-KSeF.md"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/offline/tryby-offline.md" -OutFile "ksef-docs\offline\tryby-offline.md"

Write-Host "Pobieranie zakończone!"
```

Następnie uruchom w PowerShell:
```powershell
.\download-files.ps1
```

### Opcja 4: Klonowanie repozytoriów
Najprostsze podejście - sklonuj całe repozytoria:

```bash
# Klonuj oba repozytoria
git clone https://github.com/CIRFMF/ksef-client-csharp.git
git clone https://github.com/CIRFMF/ksef-docs.git

# Następnie skopiuj tylko potrzebne pliki zgodnie z listą powyżej
```

## Priorytety pobierania

### 🔴 KRYTYCZNE (pobierz najpierw):
1. ⭐ `QrCodeOfflineE2ETests.cs` - przykłady implementacji podpisów
2. ⭐ `CryptographyService.cs` - gotowa implementacja kryptografii
3. ⭐ `invoice-template-fa-3.xml` - przykład faktury do testów
4. ⭐ `schemat-FA(3)-v1-0E.xsd` - schemat do walidacji

### 🟡 WAŻNE (pobierz potem):
5. `VerificationLinkService.cs` - budowanie URL dla QR
6. `QrCodeService.cs` - generowanie obrazów QR
7. `KsefNumberValidator.cs` - walidacja CRC-8
8. `invoice-template-fa-3-with-custom-Subject2.xml` - dodatkowe przykłady

### 🟢 PRZYDATNE (opcjonalnie):
9. `QrCodeE2ETests.cs` - dodatkowe testy
10. `invoice-template-fa-2.xml` - dla porównania
11. Dokumentacja markdown (już w paczce)

## Linki do repozytoriów

- **ksef-client-csharp:** https://github.com/CIRFMF/ksef-client-csharp
- **ksef-docs:** https://github.com/CIRFMF/ksef-docs

## Wsparcie

W razie problemów z pobieraniem:
1. Sprawdź czy masz dostęp do GitHub
2. Użyj VPN jeśli GitHub jest zablokowany
3. Pobierz pliki bezpośrednio z interfejsu webowego GitHub (przycisk "Raw")
4. Sklonuj całe repozytoria używając `git clone`

---

**Data:** 2025-10-24  
**Projekt:** KSeF PDF Generator  
**Wersja instrukcji:** 1.0
