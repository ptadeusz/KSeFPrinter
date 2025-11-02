# KSeF Printer API - Specyfikacja

**Wersja:** 1.0.0
**Data:** 2025-01-02
**Framework:** ASP.NET Core 9.0
**Architektura:** REST API z Swagger UI

---

## Spis treści

1. [Informacje ogólne](#informacje-ogólne)
2. [Konfiguracja i uruchomienie](#konfiguracja-i-uruchomienie)
3. [Licencjonowanie](#licencjonowanie)
4. [Endpointy API](#endpointy-api)
   - [1. Walidacja faktury XML](#1-walidacja-faktury-xml)
   - [2. Parsowanie faktury XML do JSON](#2-parsowanie-faktury-xml-do-json)
   - [3. Generowanie PDF](#3-generowanie-pdf)
   - [4. Generowanie kodu QR](#4-generowanie-kodu-qr)
   - [5. Health Check](#5-health-check)
5. [Modele danych](#modele-danych)
6. [Kody błędów](#kody-błędów)
7. [Przykłady użycia](#przykłady-użycia)
   - [cURL](#curl)
   - [PowerShell](#powershell)
   - [C# (.NET)](#c-net)
   - [Python](#python)
8. [Dodatkowe informacje](#dodatkowe-informacje)

---

## Informacje ogólne

### Opis
KSeF Printer API to RESTful web service do przetwarzania faktur elektronicznych w formacie KSeF FA(3). Umożliwia:
- Walidację XML faktur
- Parsowanie XML do JSON
- Generowanie PDF z kodami QR
- Generowanie kodów QR dla numerów KSeF

### Technologia
- **Framework:** ASP.NET Core 9.0
- **Język:** C# (.NET 9)
- **Dokumentacja:** Swagger/OpenAPI
- **Format danych:** JSON
- **Obsługa certyfikatów:** Windows Certificate Store, Azure Key Vault

### Środowiska
- **Test:** `https://ksef-test.mf.gov.pl`
- **Produkcja:** `https://ksef.mf.gov.pl`

### Base URL
```
http://localhost:5000/api
https://localhost:5001/api
```

Swagger UI dostępny pod:
```
http://localhost:5000/
https://localhost:5001/
```

---

## Konfiguracja i uruchomienie

### Wymagania
- .NET 9.0 SDK
- Windows (dla certyfikatów z Windows Certificate Store)
- Opcjonalnie: Azure Key Vault (dla certyfikatów w chmurze)

### Uruchomienie

**Rozwój:**
```bash
cd KSeFPrinter.API
dotnet run
```

**Produkcja:**
```bash
dotnet publish -c Release
cd bin/Release/net9.0/publish
dotnet KSeFPrinter.API.dll
```

### Konfiguracja (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Swagger": {
    "EnableInProduction": true
  },
  "License": {
    "FilePath": "license.lic"
  },
  "AzureKeyVault": {
    "KeyVaultUrl": "",
    "CertificateName": "",
    "CertificateVersion": null,
    "AuthenticationType": "DefaultAzureCredential",
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "Note": "Konfiguracja dla certyfikatów z Azure Key Vault. Pozostaw puste jeśli nie używasz."
  }
}
```

**Uwaga:** Certyfikaty do generowania KOD QR II (podpis cyfrowy) mogą być konfigurowane w dwóch sposób:
1. **Windows Certificate Store** - certyfikat zainstalowany lokalnie
2. **Azure Key Vault** - certyfikat przechowywany w chmurze (konfiguracja w sekcji `AzureKeyVault`)

**W obecnej implementacji (Wariant A):** Certyfikat musi być przekazany w requeście do `/api/invoice/generate-pdf`.

### Zmienne środowiskowe

```bash
# URL aplikacji
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# Środowisko
ASPNETCORE_ENVIRONMENT=Development

# Licencja (opcjonalnie)
License__FilePath=C:\path\to\license.lic
```

---

## Licencjonowanie

### Wymagania
API wymaga ważnej licencji dla produktu **KSeF Printer**. Bez licencji aplikacja nie uruchomi się.

### Format licencji
Plik `license.lic` (JSON z podpisem RSA):
```json
{
  "licenseKey": "KSEF-20250102-XXXXXXXX",
  "issuedTo": "Firma ABC Sp. z o.o.",
  "expiryDate": "2026-12-31T00:00:00Z",
  "allowedNips": ["1234567890"],
  "maxNips": 5,
  "features": {
    "ksefConnector": false,
    "ksefPrinter": true,
    "batchMode": false,
    "autoTokenGeneration": false,
    "azureKeyVault": true
  },
  "signature": "..."
}
```

### Walidacja przy starcie
```
=== Sprawdzanie licencji ===
✅ Licencja ważna
   Właściciel: Firma ABC Sp. z o.o.
   Ważna do: 2026-12-31
   Dozwolone NIP-y: 1
```

Jeśli licencja jest nieprawidłowa lub wygasła, aplikacja zatrzyma się z kodem błędu `1`.

### Co jest sprawdzane przy starcie?
1. ✅ Czy plik licencji istnieje
2. ✅ Czy licencja nie wygasła (`expiryDate`)
3. ✅ Czy podpis RSA jest prawidłowy (weryfikacja z kluczem publicznym)
4. ✅ Czy `features.ksefPrinter = true` (uprawnienie do KSeF Printer)

### Co NIE jest sprawdzane (TODO):
⚠️ **UWAGA:** W obecnej wersji (1.0.0) **NIE jest sprawdzane**, czy NIP wystawcy/odbiorcy/podmiotu 3 z faktury znajduje się na liście `allowedNips`.

Oznacza to, że:
- Użytkownik z ważną licencją może generować PDF dla **dowolnych NIP-ów**
- Pole `allowedNips` w licencji jest odczytywane, ale nie jest wykorzystywane do walidacji
- Planowana jest implementacja walidacji NIP-ów w przyszłych wersjach

---

## Endpointy API

### 1. Walidacja faktury XML

**Endpoint:** `POST /api/invoice/validate`

**Opis:** Waliduje fakturę XML pod kątem zgodności ze schematem KSeF FA(3).

**Request:**
```json
{
  "xmlContent": "<?xml version=\"1.0\"?>...",
  "isBase64": false,
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF"
}
```

**Response (200 OK):**
```json
{
  "isValid": true,
  "errors": [],
  "warnings": ["Brak numeru telefonu sprzedawcy"],
  "invoiceNumber": "FV/2025/01/001",
  "sellerNip": "1234567890",
  "mode": "Online"
}
```

**Response (walidacja niepowodzenie):**
```json
{
  "isValid": false,
  "errors": [
    "Brak wymaganego pola: Podmiot1.DaneIdentyfikacyjne.NIP",
    "Nieprawidłowa suma kontrolna w wierszu 1"
  ],
  "warnings": [],
  "invoiceNumber": null,
  "sellerNip": null,
  "mode": null
}
```

**Response (400 Bad Request):**
```json
{
  "error": "Nieprawidłowy format XML"
}
```

---

### 2. Parsowanie faktury XML do JSON

**Endpoint:** `POST /api/invoice/parse`

**Opis:** Parsuje fakturę XML i zwraca dane w strukturalnym formacie JSON.

**Request:**
```json
{
  "xmlContent": "<?xml version=\"1.0\"?>...",
  "isBase64": false,
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF"
}
```

**Response (200 OK):**
```json
{
  "invoiceNumber": "FV/2025/01/001",
  "issueDate": "2025-01-02T00:00:00Z",
  "mode": "Online",
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "seller": {
    "name": "Firma ABC Sp. z o.o.",
    "nip": "1234567890",
    "address": "ul. Testowa 1, 00-001 Warszawa",
    "email": "kontakt@abc.pl",
    "phone": "+48123456789"
  },
  "buyer": {
    "name": "Firma XYZ S.A.",
    "nip": "0987654321",
    "address": "ul. Kupiecka 2, 00-002 Kraków",
    "email": "biuro@xyz.pl",
    "phone": "+48987654321"
  },
  "lines": [
    {
      "lineNumber": 1,
      "description": "Usługa konsultingowa",
      "quantity": 10.00,
      "unitPrice": 100.00,
      "netAmount": 1000.00,
      "vatRate": "23"
    }
  ],
  "summary": {
    "totalNet": 1000.00,
    "totalVat": 230.00,
    "totalGross": 1230.00,
    "currency": "PLN"
  },
  "payment": {
    "dueDates": ["2025-01-16T00:00:00Z"],
    "paymentMethod": "1",
    "bankAccounts": ["12345678901234567890123456"],
    "factorBankAccounts": null
  }
}
```

**Response (400 Bad Request):**
```json
{
  "error": "Błąd parsowania XML: nieprawidłowa struktura"
}
```

---

### 3. Generowanie PDF

**Endpoint:** `POST /api/invoice/generate-pdf`

**Opis:** Generuje PDF z faktury XML wraz z kodami QR (weryfikacja KSeF + opcjonalnie podpis certyfikatu).

#### Kody QR na PDF

Generator tworzy **dwa typy kodów QR** (zgodnie z wymogami KSeF):

**KOD QR I - Weryfikacja faktury (zawsze generowany):**
- Zawiera: NIP sprzedawcy + data wystawienia + hash XML + (opcjonalnie) numer KSeF
- URL: `https://ksef.mf.gov.pl/web/verify/{ksefNumber}` (dla ONLINE)
- URL: `https://ksef.mf.gov.pl/web/verify?nip={nip}&date={date}&hash={hash}` (dla OFFLINE)
- **Nie wymaga certyfikatu** - zawsze generowany

**KOD QR II - Podpis cyfrowy wystawcy (opcjonalny):**
- Zawiera: podpis cyfrowy XML z certyfikatu wystawcy
- URL: `https://ksef.mf.gov.pl/web/verify/signed?...`
- **Wymaga certyfikatu** - generowany tylko gdy certyfikat jest dostępny
- Służy do weryfikacji autentyczności dokumentu

**Request (podstawowy - bez certyfikatu):**
```json
{
  "xmlContent": "<?xml version=\"1.0\"?>...",
  "isBase64": false,
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "useProduction": false,
  "validateInvoice": true,
  "returnFormat": "file"
}
```

**Uwaga:** Request bez certyfikatu wygeneruje PDF z **tylko KOD QR I** (weryfikacja faktury).

**Request (z certyfikatem z Windows Store - KOD QR I + KOD QR II):**
```json
{
  "xmlContent": "<?xml version=\"1.0\"?>...",
  "isBase64": false,
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "certificateSource": "WindowsStore",
  "certificateThumbprint": "A1B2C3D4E5F6...",
  "certificateStoreName": "My",
  "certificateStoreLocation": "CurrentUser",
  "useProduction": false,
  "validateInvoice": true,
  "returnFormat": "file"
}
```

**Request (z certyfikatem z Azure Key Vault - KOD QR I + KOD QR II):**
```json
{
  "xmlContent": "<?xml version=\"1.0\"?>...",
  "isBase64": false,
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "certificateSource": "AzureKeyVault",
  "azureKeyVaultUrl": "https://myvault.vault.azure.net/",
  "azureKeyVaultCertificateName": "my-certificate",
  "azureKeyVaultCertificateVersion": null,
  "azureAuthenticationType": "DefaultAzureCredential",
  "useProduction": false,
  "validateInvoice": true,
  "returnFormat": "file"
}
```

**Uwaga:** Request z certyfikatem wygeneruje PDF z **KOD QR I + KOD QR II** (weryfikacja + podpis cyfrowy).

**Request (z Azure Key Vault - Client Secret):**
```json
{
  "xmlContent": "<?xml version=\"1.0\"?>...",
  "certificateSource": "AzureKeyVault",
  "azureKeyVaultUrl": "https://myvault.vault.azure.net/",
  "azureKeyVaultCertificateName": "my-certificate",
  "azureAuthenticationType": "ClientSecret",
  "azureTenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "azureClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "azureClientSecret": "secret",
  "returnFormat": "base64"
}
```

**Response (returnFormat="file", 200 OK):**
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="Faktura_FV_2025_01_001_20250102_143522.pdf"

[Binary PDF data]
```

**Response (returnFormat="base64", 200 OK):**
```json
{
  "pdfBase64": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PC9UeXBlL0NhdGFsb...",
  "fileSizeBytes": 45123,
  "invoiceNumber": "FV/2025/01/001",
  "mode": "Online"
}
```

**Response (400 Bad Request - błąd walidacji):**
```json
{
  "error": "Walidacja faktury nie powiodła się: brak wymaganego pola NIP"
}
```

**Response (400 Bad Request - błąd certyfikatu):**
```json
{
  "error": "Nie znaleziono certyfikatu z thumbprint: A1B2C3D4E5F6..."
}
```

---

### 4. Generowanie kodu QR

**Endpoint:** `POST /api/invoice/generate-qr-code`

**Opis:** Generuje **KOD QR I** (weryfikacja numeru KSeF) w formacie SVG lub PNG base64 (do osadzania w zewnętrznych systemach).

**Uwaga:** Ten endpoint generuje **tylko KOD QR I** (link do weryfikacji faktury po numerze KSeF). Nie generuje **KOD QR II** (podpis cyfrowy), który wymaga certyfikatu i jest dostępny tylko w `/api/invoice/generate-pdf`.

**Request:**
```json
{
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "useProduction": false,
  "format": "svg",
  "size": 200,
  "includeLabel": false
}
```

**Parametry:**
- `format`: `"svg"`, `"base64"`, `"both"`
- `size`: rozmiar w pikselach (domyślnie: 200)
- `useProduction`: `true` = produkcja, `false` = test

**Response (format="svg", 200 OK):**
```json
{
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "qrCodeSvg": "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">...</svg>",
  "qrCodeBase64": null,
  "format": "svg",
  "verificationUrl": "https://ksef-test.mf.gov.pl/web/verify/1234567890123456789012345-20250102-ABCD-EF",
  "size": 200,
  "environment": "test"
}
```

**Response (format="base64", 200 OK):**
```json
{
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "qrCodeSvg": null,
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAAAMgAAADICAYAAAC...",
  "format": "base64",
  "verificationUrl": "https://ksef-test.mf.gov.pl/web/verify/1234567890123456789012345-20250102-ABCD-EF",
  "size": 200,
  "environment": "test"
}
```

**Response (format="both", 200 OK):**
```json
{
  "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
  "qrCodeSvg": "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\">...</svg>",
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAAAMgAAADICAYAAAC...",
  "format": "both",
  "verificationUrl": "https://ksef-test.mf.gov.pl/web/verify/1234567890123456789012345-20250102-ABCD-EF",
  "size": 200,
  "environment": "test"
}
```

**Response (400 Bad Request - nieprawidłowy numer KSeF):**
```json
{
  "error": "Nieprawidłowy format numeru KSeF: 1234-INVALID"
}
```

**Response (400 Bad Request - nieprawidłowy format):**
```json
{
  "error": "Nieprawidłowy format: png. Dostępne: svg, base64, both"
}
```

---

### 5. Health Check

**Endpoint:** `GET /api/invoice/health`

**Opis:** Sprawdza status API.

**Response (200 OK):**
```json
{
  "status": "healthy",
  "service": "KSeF Printer API",
  "version": "1.0.0",
  "timestamp": "2025-01-02T14:35:22.123Z"
}
```

---

## Modele danych

### InvoiceValidationRequest
```csharp
{
  "xmlContent": string,          // Zawartość XML (plain text lub base64)
  "ksefNumber": string?,          // Opcjonalny numer KSeF
  "isBase64": bool                // Czy XML jest base64 (default: false)
}
```

### InvoiceValidationResponse
```csharp
{
  "isValid": bool,                // Czy faktura jest poprawna
  "errors": string[],             // Lista błędów
  "warnings": string[],           // Lista ostrzeżeń
  "invoiceNumber": string?,       // Numer faktury (jeśli sparsowano)
  "sellerNip": string?,           // NIP sprzedawcy (jeśli sparsowano)
  "mode": string?                 // Tryb: "Online" lub "Offline"
}
```

### GeneratePdfRequest
```csharp
{
  // Podstawowe
  "xmlContent": string,           // Zawartość XML
  "isBase64": bool,               // Czy XML jest base64 (default: false)
  "ksefNumber": string?,          // Opcjonalny numer KSeF
  "useProduction": bool,          // Środowisko (default: false = test)
  "validateInvoice": bool,        // Waliduj przed generowaniem (default: true)
  "returnFormat": string,         // "file" lub "base64" (default: "file")

  // Certyfikat - Windows Store
  "certificateSource": string,    // "WindowsStore", "AzureKeyVault", "ConfigFile"
  "certificateThumbprint": string?,
  "certificateSubject": string?,
  "certificateStoreName": string, // Default: "My"
  "certificateStoreLocation": string, // Default: "CurrentUser"

  // Certyfikat - Azure Key Vault
  "azureKeyVaultUrl": string?,
  "azureKeyVaultCertificateName": string?,
  "azureKeyVaultCertificateVersion": string?,
  "azureAuthenticationType": string, // Default: "DefaultAzureCredential"
  "azureTenantId": string?,
  "azureClientId": string?,
  "azureClientSecret": string?
}
```

### GeneratePdfResponse
```csharp
{
  "pdfBase64": string,            // PDF w base64 (tylko gdy returnFormat="base64")
  "fileSizeBytes": long,          // Rozmiar pliku w bajtach
  "invoiceNumber": string,        // Numer faktury
  "mode": string                  // Tryb: "Online" lub "Offline"
}
```

### GenerateQrCodeRequest
```csharp
{
  "ksefNumber": string,           // Numer KSeF (wymagany)
  "useProduction": bool,          // Środowisko (default: false = test)
  "format": string,               // "svg", "base64", "both" (default: "svg")
  "size": int,                    // Rozmiar w px (default: 200)
  "includeLabel": bool            // Etykieta z numerem (default: false)
}
```

### GenerateQrCodeResponse
```csharp
{
  "ksefNumber": string,           // Numer KSeF
  "qrCodeSvg": string?,           // SVG (jeśli format="svg" lub "both")
  "qrCodeBase64": string?,        // PNG base64 (jeśli format="base64" lub "both")
  "format": string,               // Format zwrócony
  "verificationUrl": string,      // URL weryfikacyjny
  "size": int,                    // Rozmiar w px
  "environment": string           // "test" lub "production"
}
```

### InvoiceParseResponse
```csharp
{
  "invoiceNumber": string,
  "issueDate": DateTime,
  "mode": string,                 // "Online" lub "Offline"
  "ksefNumber": string?,
  "seller": {
    "name": string,
    "nip": string,
    "address": string?,
    "email": string?,
    "phone": string?
  },
  "buyer": {
    "name": string,
    "nip": string,
    "address": string?,
    "email": string?,
    "phone": string?
  },
  "lines": [
    {
      "lineNumber": int,
      "description": string,
      "quantity": decimal?,
      "unitPrice": decimal?,
      "netAmount": decimal,
      "vatRate": string
    }
  ],
  "summary": {
    "totalNet": decimal,
    "totalVat": decimal,
    "totalGross": decimal,
    "currency": string
  },
  "payment": {
    "dueDates": DateTime[]?,
    "paymentMethod": string?,
    "bankAccounts": string[]?,
    "factorBankAccounts": string[]?
  }?
}
```

---

## Kody błędów

### HTTP Status Codes

| Kod | Znaczenie | Przykład |
|-----|-----------|----------|
| 200 | OK | Sukces |
| 400 | Bad Request | Nieprawidłowy XML, błąd walidacji |
| 401 | Unauthorized | Brak licencji (przy starcie, nie podczas requestu) |
| 500 | Internal Server Error | Błąd serwera |

### Błędy walidacji

```json
{
  "error": "Komunikat błędu"
}
```

**Przykłady:**
- `"Nieprawidłowy format XML"`
- `"Brak wymaganego pola: Podmiot1.DaneIdentyfikacyjne.NIP"`
- `"Nieprawidłowy format numeru KSeF: 1234-INVALID"`
- `"Nie znaleziono certyfikatu z thumbprint: A1B2C3D4E5F6..."`
- `"Walidacja faktury nie powiodła się: nieprawidłowa suma kontrolna"`

---

## Przykłady użycia

### cURL

**Walidacja faktury:**
```bash
curl -X POST "http://localhost:5000/api/invoice/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "xmlContent": "<?xml version=\"1.0\"?>...",
    "isBase64": false
  }'
```

**Generowanie PDF (plik):**
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-pdf" \
  -H "Content-Type: application/json" \
  -d '{
    "xmlContent": "<?xml version=\"1.0\"?>...",
    "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
    "returnFormat": "file"
  }' \
  -o faktura.pdf
```

**Generowanie PDF (base64):**
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-pdf" \
  -H "Content-Type: application/json" \
  -d '{
    "xmlContent": "<?xml version=\"1.0\"?>...",
    "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
    "returnFormat": "base64"
  }'
```

**Generowanie kodu QR:**
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-qr-code" \
  -H "Content-Type: application/json" \
  -d '{
    "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
    "format": "svg",
    "size": 300
  }'
```

### PowerShell

**Walidacja faktury:**
```powershell
$xmlContent = Get-Content "faktura.xml" -Raw
$body = @{
    xmlContent = $xmlContent
    isBase64 = $false
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/validate" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

**Generowanie PDF:**
```powershell
$xmlContent = Get-Content "faktura.xml" -Raw
$body = @{
    xmlContent = $xmlContent
    ksefNumber = "1234567890123456789012345-20250102-ABCD-EF"
    returnFormat = "file"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body `
    -OutFile "faktura.pdf"
```

**Generowanie kodu QR:**
```powershell
$body = @{
    ksefNumber = "1234567890123456789012345-20250102-ABCD-EF"
    format = "both"
    size = 300
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-qr-code" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

# Zapisz SVG
$response.qrCodeSvg | Out-File "qr.svg"

# Zapisz PNG
[Convert]::FromBase64String($response.qrCodeBase64) | Set-Content "qr.png" -Encoding Byte
```

### C# (.NET)

**Walidacja faktury:**
```csharp
using System.Net.Http.Json;

var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

var request = new
{
    xmlContent = File.ReadAllText("faktura.xml"),
    isBase64 = false
};

var response = await httpClient.PostAsJsonAsync("/api/invoice/validate", request);
var result = await response.Content.ReadFromJsonAsync<InvoiceValidationResponse>();

Console.WriteLine($"Poprawna: {result.IsValid}");
foreach (var error in result.Errors)
{
    Console.WriteLine($"Błąd: {error}");
}
```

**Generowanie PDF:**
```csharp
var request = new
{
    xmlContent = File.ReadAllText("faktura.xml"),
    ksefNumber = "1234567890123456789012345-20250102-ABCD-EF",
    returnFormat = "file"
};

var response = await httpClient.PostAsJsonAsync("/api/invoice/generate-pdf", request);
var pdfBytes = await response.Content.ReadAsByteArrayAsync();
await File.WriteAllBytesAsync("faktura.pdf", pdfBytes);
```

### Python

**Walidacja faktury:**
```python
import requests

with open("faktura.xml", "r", encoding="utf-8") as f:
    xml_content = f.read()

response = requests.post(
    "http://localhost:5000/api/invoice/validate",
    json={
        "xmlContent": xml_content,
        "isBase64": False
    }
)

result = response.json()
print(f"Poprawna: {result['isValid']}")
for error in result.get('errors', []):
    print(f"Błąd: {error}")
```

**Generowanie PDF:**
```python
response = requests.post(
    "http://localhost:5000/api/invoice/generate-pdf",
    json={
        "xmlContent": xml_content,
        "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
        "returnFormat": "file"
    }
)

with open("faktura.pdf", "wb") as f:
    f.write(response.content)
```

**Generowanie kodu QR:**
```python
response = requests.post(
    "http://localhost:5000/api/invoice/generate-qr-code",
    json={
        "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
        "format": "both",
        "size": 300
    }
)

result = response.json()

# Zapisz SVG
with open("qr.svg", "w") as f:
    f.write(result["qrCodeSvg"])

# Zapisz PNG
import base64
with open("qr.png", "wb") as f:
    f.write(base64.b64decode(result["qrCodeBase64"]))
```

---

## Dodatkowe informacje

### Swagger UI

Po uruchomieniu API, Swagger UI jest dostępny pod:
```
http://localhost:5000/
```

Dokumentacja interaktywna zawiera:
- Pełną specyfikację wszystkich endpointów
- Możliwość testowania API z poziomu przeglądarki
- Automatyczne przykłady requestów i responsów
- Walidację schematów JSON

### Limity

- **Maksymalny rozmiar XML:** brak ograniczeń (ograniczenie przez ASP.NET Core: domyślnie 30MB)
- **Maksymalny rozmiar PDF:** brak ograniczeń
- **Timeout:** 2 minuty (domyślnie)

### Bezpieczeństwo

- API nie wymaga uwierzytelniania (rekomendowane dodanie w środowisku produkcyjnym)
- Licencja jest walidowana przy starcie aplikacji
- Certyfikaty są ładowane bezpiecznie z Windows Certificate Store lub Azure Key Vault
- Brak przechowywania plików na dysku (wszystko w pamięci)

### Performance

- Wszystkie operacje są asynchroniczne
- Generowanie PDF: ~500ms (zależnie od rozmiaru faktury)
- Walidacja XML: ~100ms
- Generowanie QR: ~50ms

---

**Koniec specyfikacji**

Wygenerowano: 2025-01-02
Wersja dokumentu: 1.0.0
