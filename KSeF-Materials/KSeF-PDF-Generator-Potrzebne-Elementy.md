# Elementy potrzebne z repozytoriów do realizacji projektu KSeF PDF Generator

## Przegląd projektu
Projekt **KSeF PDF Generator** to komponent C# (.NET 9) do generowania wizualizacji faktur w formacie PDF na podstawie plików XML zgodnych ze schematem KSeF **FA(3) v1-0E**.

---

## 1. Elementy z repozytorium `ksef-docs`

### 1.1. Dokumentacja schematu FA(3)
📁 **Lokalizacja:** `/faktury/schemat-FA(3)-v1-0E.xsd`

**Zastosowanie:**
- Walidacja struktury XML faktury FA(3)
- Zrozumienie namespace: `http://crd.gov.pl/wzor/2025/06/25/13775/`
- Poznanie wszystkich możliwych elementów XML (Naglowek, Podmiot1, Podmiot2, Podmiot3, Fa, FaWiersz, Platnosc, itd.)
- Definicje typów danych (TKwotowy, TZnakowy, TZnakowy512, TNumer, itd.)
- Wymagane i opcjonalne pola
- Struktura hierarchii elementów

**Zastosowanie w projekcie:**
- Model danych C# (`FakturaFA3.cs`) - mapowanie na strukturę XSD
- Parser XML (`XmlInvoiceParser.cs`) - poprawne odczytanie wszystkich elementów
- Walidator (`InvoiceValidator.cs`) - sprawdzenie zgodności ze schematem
- Testy jednostkowe - przykłady poprawnych i niepoprawnych struktur

---

### 1.2. Dokumentacja kodów QR
📁 **Lokalizacja:** `/kody-qr.md`

**Zawartość kluczowa:**

#### **KOD I - Weryfikacja faktury (Online i Offline)**
- Struktura URL: `https://ksef-test.mf.gov.pl/client-app/invoice/{NIP}/{DD-MM-RRRR}/{SHA256-Base64URL}`
- Algorytm SHA-256 + kodowanie Base64URL
- Etykiety: numer KSeF (online) lub "OFFLINE" (offline)
- Specyfikacja QR: ISO/IEC 18004:2015, 5 pikseli na moduł

#### **KOD II - Weryfikacja certyfikatu (tylko Offline)**
- Struktura URL: `https://ksef-test.mf.gov.pl/client-app/certificate/{IdentifierType}/{IdentifierValue}/{NIP}/{CertificateSerial}/{SHA256-Base64URL}/{Signature-Base64URL}`
- **WYMAGA podpisu kryptograficznego!**
- Algorytmy podpisu:
  - **RSA-PSS** (preferowany): SHA-256, MGF1, salt 32 bajty, min. 2048 bitów
  - **ECDSA P-256**: krzywa NIST P-256, format IEEE P1363 (preferowany) lub ASN.1 DER
- Proces generowania podpisu:
  1. Ciąg do podpisu (bez `https://` i bez trailing `/`)
  2. Konwersja na bajty UTF-8
  3. Wygenerowanie podpisu (RSA-PSS lub ECDSA)
  4. Kodowanie Base64URL
- Etykieta: "Certyfikat KSeF wystawcy"

**Zastosowanie w projekcie:**
- Klasa `QrCodeGeneratorService.cs` - generowanie obu kodów QR
- Klasa `CryptoService.cs` - podpisy RSA-PSS i ECDSA dla KODU II
- Testy weryfikacji linków i kodów QR

---

### 1.3. Dokumentacja certyfikatów KSeF
📁 **Lokalizacja:** `/certyfikaty-KSeF.md`

**Kluczowe informacje:**
- **Typy certyfikatów:**
  - `Authentication` - tylko do uwierzytelniania (keyUsage: Digital Signature 80)
  - `Offline` - tylko do wystawiania faktur offline i KODU II (keyUsage: Non-Repudiation 40)
- **KRYTYCZNE**: Do KODU II można używać TYLKO certyfikatu typu `Offline`!
- Proces pozyskiwania certyfikatu Offline
- Wymagania dotyczące kluczy:
  - RSA: min. 2048 bitów
  - EC: krzywa NIST P-256 (secp256r1)
- Parametry CSR (Certificate Signing Request)
- Atrybuty X.509 certyfikatu

**Zastosowanie w projekcie:**
- Walidacja typu certyfikatu przed użyciem
- Dokumentacja API - wyjaśnienie wymagań dla użytkowników
- Testy z różnymi typami certyfikatów
- Ostrzeżenia w loggach przy błędnym typie certyfikatu

---

### 1.4. Dokumentacja numeru KSeF
📁 **Lokalizacja:** `/faktury/numer-ksef.md`

**Zawartość:**
- Struktura: `9999999999-RRRRMMDD-FFFFFFFFFFFF-FF` (35 znaków)
  - 10 cyfr NIP sprzedawcy
  - 8 cyfr daty (RRRRMMDD)
  - 12 znaków hex części technicznej
  - 2 znaki hex sumy kontrolnej CRC-8
- Algorytm walidacji CRC-8:
  - Polinom: `0x07`
  - Wartość początkowa: `0x00`
  - Format wyniku: 2-znakowy hex (wielkie litery)

**Zastosowanie w projekcie:**
- Klasa `KsefNumberValidator.cs` - walidacja numeru KSeF
- Walidacja przed użyciem w KODZIE I
- Testy jednostkowe walidacji
- Komunikaty błędów dla użytkowników

---

### 1.5. Dokumentacja trybów offline
📁 **Lokalizacja:** `/tryby-offline.md`

**Zawartość:**
- Typy trybów offline:
  - **offline24** - predefiniowany offline (do 24h)
  - **offline** - offline ze względu na niedostępność systemu
  - **tryb awaryjny** - awaria systemu KSeF
- Wymagania dla każdego trybu
- Kiedy stosować który tryb
- Konsekwencje wystawienia faktury w trybie offline

**Zastosowanie w projekcie:**
- Dokumentacja użytkownika - kiedy używać trybu offline
- Komunikaty w PDF dla użytkownika końcowego
- Testy scenariuszy offline

---

## 2. Elementy z repozytorium `ksef-client-csharp`

### 2.1. Przykładowe szablony faktur FA(3)
📁 **Lokalizacje:**
- `/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3.xml`
- `/KSeF.Client.Tests.Core/Templates/invoice-template-fa-3-with-custom-Subject2.xml`

**Zawartość:**
- Namespace: `http://crd.gov.pl/wzor/2025/06/25/13775/`
- Kompletne przykłady struktury XML FA(3):
  - Naglowek z kodSystemowy="FA (3)", wersjaSchemy="1-0E"
  - Podmiot1, Podmiot2, Podmiot3
  - Sekcja Fa z:
    - KodWaluty
    - Pola P_1 (data wystawienia), P_2 (numer faktury), P_6 (data sprzedaży)
    - OkresFa (zakres dat)
    - P_13_1, P_14_1, P_15 (podsumowania VAT)
    - Adnotacje
    - FaWiersz (pozycje faktury)
    - Rozliczenie
    - Platnosc
  - Stopka z Informacjami i Rejestrami

**Zastosowanie w projekcie:**
- **Wzorzec do testów** - podstawa testów parsowania XML
- **Przykłady w dokumentacji** - pokazanie użytkownikom struktury
- **Testy integracyjne** - weryfikacja poprawności parsowania
- **Zrozumienie rzeczywistych danych** - nie tylko XSD, ale realne przykłady
- **Walidacja parsera** - czy wszystkie elementy są prawidłowo odczytane

---

### 2.2. Przykładowe faktury FA(2) (opcjonalnie)
📁 **Lokalizacje:**
- `/KSeF.Client.Tests.Core/Templates/invoice-template-fa-2.xml`
- `/KSeF.DemoWebApp/faktura-online.xml` (FA(2))

**Uwaga:** Projekt skupia się na FA(3), ale FA(2) może być pomocne do:
- Zrozumienia różnic między wersjami schematu
- Testów walidacji - sprawdzenie, czy parser odrzuci FA(2)
- Dokumentacji dla użytkowników - wyjaśnienie dlaczego tylko FA(3)

---

### 2.3. Testy E2E kodów QR - QrCodeOfflineE2ETests.cs
📁 **Lokalizacja:** `/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeOfflineE2ETests.cs`

**Zawartość kluczowa:**

#### **Generowanie KODU I i KODU II**
```csharp
// Przykłady kodu w teście:
- Generowanie CSR z RSA i ECDSA
- Pozyskanie certyfikatu KSeF typu Offline
- Generowanie skrótu SHA-256 faktury
- Budowanie URL weryfikacyjnego (KOD I)
- Generowanie podpisu kryptograficznego (RSA-PSS lub ECDSA)
- Budowanie URL certyfikatu z podpisem (KOD II)
- Generowanie obrazu QR
- Dodawanie etykiet do kodów QR
```

#### **Scenariusze testowe:**
1. Faktura FA(2) + certyfikat RSA
2. Faktura FA(2) + certyfikat ECDSA
3. Faktura FA(3) + certyfikat RSA
4. Faktura FA(3) + certyfikat ECDSA

**Kluczowe metody do zaimplementowania:**
- `CertificateUtils.GenerateCsrAndPrivateKeyWithRsaAsync()` - generowanie CSR RSA
- `CertificateUtils.GenerateCsrAndPrivateKeyWithEcdsaAsync()` - generowanie CSR ECDSA
- `GetCertWithRsaKey()` - wczytanie certyfikatu z kluczem RSA
- `GetCertWithEcdsaKey()` - wczytanie certyfikatu z kluczem ECDSA
- `linkService.BuildInvoiceVerificationUrl()` - KOD I
- `linkService.BuildCertificateVerificationUrl()` - KOD II z podpisem
- `qrCodeService.GenerateQrCode()` - obraz QR
- `qrCodeService.AddLabelToQrCode()` - dodanie etykiety

**Zastosowanie w projekcie:**
- **Wzór implementacji** - dokładne przykłady jak używać kryptografii
- **Testy kryptografii** - weryfikacja podpisów RSA-PSS i ECDSA
- **Scenariusze użycia** - różne kombinacje algorytmów
- **Debugowanie** - w razie problemów z podpisami

---

### 2.4. Testy E2E kodów QR - QrCodeE2ETests.cs
📁 **Lokalizacja:** `/KSeF.Client.Tests.Core/E2E/QrCode/QrCodeE2ETests.cs`

**Zawartość:**
- Testy generowania kodów QR dla certyfikatów
- Obsługa RSA 2048-bit
- Obsługa ECDSA P-256
- Walidacja formatu QR (Base64 PNG z prefiksem "iVBOR")
- Testowanie różnych konfiguracji

**Zastosowanie w projekcie:**
- Testy jednostkowe generatora QR
- Walidacja formatu obrazów
- Weryfikacja rozmiarów kodów QR

---

### 2.5. Klasy pomocnicze - CryptographyService
📁 **Lokalizacja:** `/KSeF.Client/Api/Services/CryptographyService.cs`

**Funkcjonalności:**
- Szyfrowanie/deszyfrowanie AES-256
- Szyfrowanie ECIES (ECDH + AES-GCM)
- Szyfrowanie RSA-OAEP
- **Generowanie podpisów RSA-PSS**
- **Generowanie podpisów ECDSA**
- Obliczanie skrótów SHA-256
- Kodowanie Base64URL
- Operacje na certyfikatach X.509
- Parsowanie kluczy prywatnych (PEM/DER)

**Zastosowanie w projekcie:**
- **Podstawa CryptoService.cs** - gotowe metody kryptograficzne
- Implementacja podpisów dla KODU II
- Generowanie skrótów SHA-256 faktur
- Kodowanie Base64URL

---

### 2.6. Klasy pomocnicze - VerificationLinkService
📁 **Lokalizacja:** `/KSeF.Client/Api/Services/VerificationLinkService.cs`

**Funkcjonalności:**
- `BuildInvoiceVerificationUrl()` - budowanie URL dla KODU I
- `BuildCertificateVerificationUrl()` - budowanie URL dla KODU II z podpisem
- Obsługa środowisk (Test/Production)
- Formatowanie dat
- Kodowanie Base64URL

**Zastosowanie w projekcie:**
- **Bezpośrednia implementacja** lub adaptacja
- Generowanie prawidłowych URL dla kodów QR
- Obsługa różnych środowisk KSeF

---

### 2.7. Klasy pomocnicze - QrCodeService
📁 **Lokalizacja:** `/KSeF.Client/Api/Services/QrCodeService.cs`

**Funkcjonalności:**
- `GenerateQrCode(string url, int pixelsPerModule)` - generowanie obrazu QR
- `AddLabelToQrCode(byte[] qrImage, string label)` - dodawanie etykiety pod kodem
- Obsługa biblioteki QRCoder
- Format wyjściowy: PNG jako byte[]

**Zastosowanie w projekcie:**
- **Gotowa implementacja** generowania QR
- Dodawanie etykiet (numer KSeF, "OFFLINE", "CERTYFIKAT")
- Konfiguracja rozmiaru QR

---

### 2.8. Walidator numeru KSeF
📁 **Lokalizacja:** `/KSeF.Client.Core/KsefNumberValidator.cs`

**Funkcjonalności:**
- Walidacja formatu numeru KSeF (35 znaków)
- Weryfikacja CRC-8
- Algorytm CRC-8 z polinomem 0x07
- Zwracanie szczegółowych komunikatów błędów

**Zastosowanie w projekcie:**
- **Bezpośrednie użycie** do walidacji numeru KSeF w trybie Online
- Testy jednostkowe walidacji
- Zabezpieczenie przed błędnymi numerami

---

### 2.9. Modele danych
📁 **Lokalizacje:**
- `/KSeF.Client.Core/Models/Authorization/`
- `/KSeF.Client.Core/Models/Certificates/`
- `/KSeF.Client.Core/Models/QRCode/`
- `/KSeF.Client.Core/Models/Sessions/`

**Modele przydatne:**
- `CertificateResponse` - informacje o certyfikacie
- `QrCodeResult` - wynik generowania QR
- `EncryptionData` - dane szyfrowania
- Enumeracje: `CertificateType`, `ContextIdentifierType`

**Zastosowanie w projekcie:**
- Wzorce dla własnych modeli danych
- Zrozumienie struktury odpowiedzi API KSeF
- Typy enumeracyjne do reużycia

---

## 3. Elementy do zaadaptowania vs. zaimplementowania od nowa

### 3.1. Elementy do bezpośredniej adaptacji (REUSE)

| Element | Źródło | Zastosowanie |
|---------|--------|--------------|
| **CryptographyService** | `ksef-client-csharp` | Podpisy RSA-PSS, ECDSA, SHA-256, Base64URL |
| **VerificationLinkService** | `ksef-client-csharp` | Budowanie URL dla KOD I i KOD II |
| **QrCodeService** | `ksef-client-csharp` | Generowanie obrazów QR z etykietami |
| **KsefNumberValidator** | `ksef-client-csharp` | Walidacja CRC-8 numeru KSeF |
| **Algorytmy kryptograficzne** | Testy E2E | Dokładne implementacje RSA-PSS i ECDSA |
| **Schemat FA(3) XSD** | `ksef-docs` | Walidacja struktury XML |

### 3.2. Elementy do zaimplementowania od nowa (BUILD)

| Element | Źródło inspiracji | Zastosowanie |
|---------|------------------|--------------|
| **XmlInvoiceParser** | Szablony FA(3) + XSD | Parsowanie XML do modeli C# |
| **FakturaFA3 (modele)** | XSD + przykłady | Reprezentacja faktury w C# |
| **PdfGeneratorService** | QuestPDF | Wizualizacja PDF z danymi faktury |
| **InvoiceValidator** | XSD | Walidacja biznesowa faktury |
| **InvoicePdfFacade** | - | Główny interfejs API generatora |

---

## 4. Plan wykorzystania elementów w projekcie

### Faza 1-2: Fundament i modele (Dzień 1-2)
- ✅ Struktura projektu
- ✅ Modele danych: wykorzystać strukturę z **XSD FA(3)**
- ✅ Enumeracje: bazować na modelach z `ksef-client-csharp`

### Faza 3: Parsowanie XML FA(3) (Dzień 3)
- ✅ Implementacja parsera bazująca na **szablonach FA(3)**
- ✅ Obsługa namespace `http://crd.gov.pl/wzor/2025/06/25/13775/`
- ✅ Testy z użyciem przykładowych XML

### Faza 4: Walidacja (Dzień 4)
- ✅ Walidacja względem **XSD FA(3)**
- ✅ Wykorzystać **KsefNumberValidator** dla numeru KSeF
- ✅ Walidacja typu certyfikatu (Offline vs Authentication)

### Faza 5: Kryptografia (Dzień 5-6)
- ✅ Zaadaptować **CryptographyService** dla podpisów RSA-PSS i ECDSA
- ✅ Implementacja SHA-256 + Base64URL
- ✅ Testy podpisów - wzorować się na **QrCodeOfflineE2ETests.cs**

### Faza 6: Kody QR (Dzień 7)
- ✅ Zaadaptować **VerificationLinkService** dla KOD I i KOD II
- ✅ Zaadaptować **QrCodeService** dla generowania obrazów
- ✅ Implementacja dodawania etykiet
- ✅ Testy weryfikacji linków

### Faza 7: Generowanie PDF (Dzień 8-10)
- ✅ QuestPDF - wizualizacja wszystkich sekcji faktury
- ✅ Integracja kodów QR w PDF
- ✅ Formatowanie zgodne z przykładami FA(3)

### Faza 8: Integracja i testy (Dzień 11)
- ✅ Testy E2E z przykładowymi fakturami FA(3)
- ✅ Testy scenariuszy online i offline
- ✅ Weryfikacja kodów QR

---

## 5. Zależności zewnętrzne (NuGet)

### Z projektu ksef-client-csharp:
- **QRCoder** - generowanie kodów QR (już używany w projekcie)
- **System.Security.Cryptography** (.NET 9) - kryptografia
- **System.Xml.Linq** - parsowanie XML

### Dodatkowe dla PDF Generator:
- **QuestPDF** - generowanie PDF (MIT License)
- **FluentAssertions** - testy (już używany w projekcie)
- **Moq** - mocki w testach (już używany w projekcie)
- **xUnit** - framework testowy (już używany w projekcie)

---

## 6. Priorytety implementacji

### 🔴 Krytyczne (MUST HAVE)
1. **Schemat FA(3) XSD** - absolutna podstawa parsowania
2. **Szablony FA(3) XML** - niezbędne do testów
3. **Dokumentacja kodów QR** - KOD I i KOD II z podpisami
4. **CryptographyService** - podpisy RSA-PSS i ECDSA
5. **QrCodeOfflineE2ETests.cs** - przykłady implementacji podpisów

### 🟡 Ważne (SHOULD HAVE)
1. **VerificationLinkService** - budowanie URL
2. **QrCodeService** - generowanie obrazów QR
3. **KsefNumberValidator** - walidacja CRC-8
4. **Dokumentacja certyfikatów** - typy, wymagania
5. **Dokumentacja numeru KSeF** - struktura, walidacja

### 🟢 Przydatne (NICE TO HAVE)
1. **Dokumentacja trybów offline** - kontekst użycia
2. **Przykłady FA(2)** - zrozumienie różnic
3. **Modele danych** - wzorce do adaptacji
4. **QrCodeE2ETests.cs** - dodatkowe testy

---

## 7. Podsumowanie - co pobrać z repozytoriów

### Z repozytorium `ksef-docs`:
```
📦 ksef-docs/
├── 📄 /faktury/schemat-FA(3)-v1-0E.xsd          ⭐ KRYTYCZNE
├── 📄 /kody-qr.md                               ⭐ KRYTYCZNE
├── 📄 /certyfikaty-KSeF.md                      ⭐ WAŻNE
├── 📄 /faktury/numer-ksef.md                    ⭐ WAŻNE
└── 📄 /tryby-offline.md                         ⭐ PRZYDATNE
```

### Z repozytorium `ksef-client-csharp`:
```
📦 ksef-client-csharp/
├── 📁 /KSeF.Client.Tests.Core/Templates/
│   ├── 📄 invoice-template-fa-3.xml                           ⭐ KRYTYCZNE
│   └── 📄 invoice-template-fa-3-with-custom-Subject2.xml      ⭐ KRYTYCZNE
├── 📁 /KSeF.Client.Tests.Core/E2E/QrCode/
│   ├── 📄 QrCodeOfflineE2ETests.cs                            ⭐ KRYTYCZNE
│   └── 📄 QrCodeE2ETests.cs                                   ⭐ WAŻNE
├── 📁 /KSeF.Client/Api/Services/
│   ├── 📄 CryptographyService.cs                              ⭐ KRYTYCZNE
│   ├── 📄 VerificationLinkService.cs                          ⭐ WAŻNE
│   └── 📄 QrCodeService.cs                                    ⭐ WAŻNE
├── 📁 /KSeF.Client.Core/
│   └── 📄 KsefNumberValidator.cs                              ⭐ WAŻNE
└── 📁 /KSeF.Client.Core/Models/                               ⭐ PRZYDATNE
```

---

## 8. Kolejność studiowania materiałów

### Dzień 0 (przed rozpoczęciem kodowania):
1. **schemat-FA(3)-v1-0E.xsd** - 2-3 godziny
   - Zrozumienie struktury XML
   - Lista wszystkich elementów i atrybutów
   - Typy danych i walidacje
2. **invoice-template-fa-3.xml** - 30 minut
   - Przykład rzeczywistej faktury
   - Mapowanie XSD na konkretne wartości
3. **kody-qr.md** - 1 godzina
   - Struktura KOD I i KOD II
   - Algorytmy podpisów (RSA-PSS, ECDSA)
   - Proces generowania

### Dzień 1-2 (podczas implementacji parsera):
1. **invoice-template-fa-3-with-custom-Subject2.xml** - 20 minut
   - Dodatkowe przykłady
   - Edge cases
2. **CryptographyService.cs** - 1 godzina
   - Zrozumienie implementacji SHA-256
   - Base64URL encoding
   - Struktura klas

### Dzień 5-6 (podczas implementacji kryptografii):
1. **QrCodeOfflineE2ETests.cs** - 2-3 godziny ⚡
   - **NAJWAŻNIEJSZY plik** dla podpisów!
   - Dokładne przykłady RSA-PSS i ECDSA
   - Generowanie CSR
   - Budowanie URL z podpisem
2. **certyfikaty-KSeF.md** - 45 minut
   - Typy certyfikatów
   - Wymagania keyUsage
   - Ostrzeżenia o typach

### Dzień 7 (podczas implementacji QR):
1. **VerificationLinkService.cs** - 30 minut
2. **QrCodeService.cs** - 30 minut
3. **numer-ksef.md** - 30 minut
4. **KsefNumberValidator.cs** - 20 minut

---

## 9. Najważniejsze uwagi techniczne

### ⚠️ KRYTYCZNE:
1. **Tylko certyfikat typu `Offline`** może być używany do KODU II
2. **Namespace FA(3):** `http://crd.gov.pl/wzor/2025/06/25/13775/`
3. **Podpis KODU II** jest WYMAGANY - bez niego kod jest bezwartościowy
4. **Format podpisu:** ciąg bez `https://` i bez trailing `/`
5. **CRC-8 dla numeru KSeF:** polinom 0x07, init 0x00

### 🔐 Algorytmy kryptograficzne:
- **RSA-PSS:** SHA-256, MGF1, salt 32 bajty, min. 2048 bitów
- **ECDSA P-256:** krzywa secp256r1, format IEEE P1363 (preferowany)
- **SHA-256:** dla skrótów faktur
- **Base64URL:** dla kodowania w URL

### 📐 Format PDF:
- Rozmiar: A4 (210 x 297 mm)
- Kody QR: minimum 5 pikseli na moduł
- Etykiety: numer KSeF / "OFFLINE" / "Certyfikat KSeF wystawcy"

---

## 10. Kontakt i wsparcie

**Repozytoria:**
- ksef-client-csharp: https://github.com/CIRFMF/ksef-client-csharp
- ksef-docs: (wskazane w specyfikacji)

**Środowiska KSeF:**
- Test: https://ksef-test.mf.gov.pl
- Produkcja: https://ksef.mf.gov.pl

**Dokumentacja API:**
- https://ksef-test.mf.gov.pl/docs/v2/

---

**Dokument przygotowany:** 2025-10-24  
**Wersja projektu:** KSeF PDF Generator v1.0  
**Schemat:** FA(3) v1-0E

---

## Gotowość do implementacji ✅

Po pobraniu i przestudiowaniu wymienionych elementów będziesz miał:
- ✅ Pełne zrozumienie struktury FA(3)
- ✅ Przykłady rzeczywistych faktur do testów
- ✅ Gotowe implementacje kryptografii do adaptacji
- ✅ Wzorce generowania kodów QR
- ✅ Dokumentację wszystkich wymagań KSeF
- ✅ Testy E2E jako przykłady użycia

**Szacowany czas realizacji:** 8-11 dni roboczych zgodnie z planem!
