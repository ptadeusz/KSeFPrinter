# Skrypt PowerShell do pobierania plików z repozytoriów KSeF

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Pobieranie plików z repozytoriów KSeF" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Utwórz strukturę katalogów
Write-Host "Tworzenie struktury katalogów..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\Templates" | Out-Null
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\E2E\QrCode" | Out-Null
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\Api\Services" | Out-Null
New-Item -ItemType Directory -Force -Path "ksef-client-csharp\Core" | Out-Null
New-Item -ItemType Directory -Force -Path "ksef-docs\faktury" | Out-Null
New-Item -ItemType Directory -Force -Path "ksef-docs\offline" | Out-Null

# Pobierz pliki z ksef-client-csharp
Write-Host ""
Write-Host "Pobieranie plików z ksef-client-csharp..." -ForegroundColor Yellow

Write-Host "  - Templates..." -ForegroundColor Gray
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3.xml" -OutFile "ksef-client-csharp\Templates\invoice-template-fa-3.xml"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3-with-custom-Subject2.xml" -OutFile "ksef-client-csharp\Templates\invoice-template-fa-3-with-custom-Subject2.xml"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-2.xml" -OutFile "ksef-client-csharp\Templates\invoice-template-fa-2.xml"

Write-Host "  - E2E Tests..." -ForegroundColor Gray
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeOfflineE2ETests.cs" -OutFile "ksef-client-csharp\E2E\QrCode\QrCodeOfflineE2ETests.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeE2ETests.cs" -OutFile "ksef-client-csharp\E2E\QrCode\QrCodeE2ETests.cs"

Write-Host "  - Api Services..." -ForegroundColor Gray
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/CryptographyService.cs" -OutFile "ksef-client-csharp\Api\Services\CryptographyService.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/VerificationLinkService.cs" -OutFile "ksef-client-csharp\Api\Services\VerificationLinkService.cs"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/QrCodeService.cs" -OutFile "ksef-client-csharp\Api\Services\QrCodeService.cs"

Write-Host "  - Core..." -ForegroundColor Gray
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Core/KsefNumberValidator.cs" -OutFile "ksef-client-csharp\Core\KsefNumberValidator.cs"

# Pobierz pliki z ksef-docs
Write-Host ""
Write-Host "Pobieranie plików z ksef-docs..." -ForegroundColor Yellow

Write-Host "  - Schemat XSD..." -ForegroundColor Gray
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/schemat-FA(3)-v1-0E.xsd" -OutFile "ksef-docs\faktury\schemat-FA(3)-v1-0E.xsd"

Write-Host "  - Dokumentacja..." -ForegroundColor Gray
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/numer-ksef.md" -OutFile "ksef-docs\faktury\numer-ksef.md"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/kody-qr.md" -OutFile "ksef-docs\kody-qr.md"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/certyfikaty-KSeF.md" -OutFile "ksef-docs\certyfikaty-KSeF.md"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/offline/tryby-offline.md" -OutFile "ksef-docs\offline\tryby-offline.md"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Pobieranie zakończone!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Pobrane pliki znajdują się w katalogach:" -ForegroundColor White
Write-Host "  - ksef-client-csharp\" -ForegroundColor Gray
Write-Host "  - ksef-docs\" -ForegroundColor Gray
