#!/bin/bash

echo "========================================="
echo "Pobieranie plików z repozytoriów KSeF"
echo "========================================="
echo ""

# Utwórz strukturę katalogów
echo "Tworzenie struktury katalogów..."
mkdir -p ksef-client-csharp/{Templates,E2E/QrCode,Api/Services,Core}
mkdir -p ksef-docs/{faktury,offline}

# Pobierz pliki z ksef-client-csharp
echo ""
echo "Pobieranie plików z ksef-client-csharp..."
cd ksef-client-csharp

echo "  - Templates..."
wget -q -P Templates/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3.xml
wget -q -P Templates/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3-with-custom-Subject2.xml
wget -q -P Templates/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/Templates/invoice-template-fa-2.xml

echo "  - E2E Tests..."
wget -q -P E2E/QrCode/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeOfflineE2ETests.cs
wget -q -P E2E/QrCode/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeE2ETests.cs

echo "  - Api Services..."
wget -q -P Api/Services/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/CryptographyService.cs
wget -q -P Api/Services/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/VerificationLinkService.cs
wget -q -P Api/Services/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client/Api/Services/QrCodeService.cs

echo "  - Core..."
wget -q -P Core/ https://raw.githubusercontent.com/CIRFMF/ksef-client-csharp/main/KSeF.Client.Core/KsefNumberValidator.cs

cd ..

# Pobierz pliki z ksef-docs
echo ""
echo "Pobieranie plików z ksef-docs..."
cd ksef-docs

echo "  - Schemat XSD..."
wget -q -P faktury/ https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/schemat-FA\(3\)-v1-0E.xsd

echo "  - Dokumentacja..."
wget -q -P faktury/ https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/faktury/numer-ksef.md
wget -q https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/kody-qr.md
wget -q https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/certyfikaty-KSeF.md
wget -q -P offline/ https://raw.githubusercontent.com/CIRFMF/ksef-docs/main/offline/tryby-offline.md

cd ..

echo ""
echo "========================================="
echo "Pobieranie zakończone!"
echo "========================================="
echo ""
echo "Struktura pobranych plików:"
tree -L 3 2>/dev/null || find . -type f | head -20
