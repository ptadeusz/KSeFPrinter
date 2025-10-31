# KSeF Printer API

REST API do walidacji, parsowania i generowania wydruków PDF z faktur XML w formacie KSeF FA(3).

## Uruchomienie

### Development
```bash
cd KSeFPrinter.API
dotnet run
```

API będzie dostępne pod adresem:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:7000
- Swagger UI: https://localhost:7000 (strona główna)

### Production
```bash
dotnet run --configuration Release
```

Lub opublikuj jako self-contained:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Architektura

**Wariant A** (obecnie zaimplementowany):
- Niezależne API do przetwarzania faktur XML
- Walidacja, parsowanie i generowanie PDF
- Obsługa certyfikatów z Windows Certificate Store
- Przygotowane do przyszłej integracji z KSeF Connector (Wariant B)

## Endpointy

### 1. GET /api/invoice/health
Sprawdzenie statusu API.

**Odpowiedź:**
```json
{
  "status": "healthy",
  "service": "KSeF Printer API",
  "version": "1.0.0",
  "timestamp": "2025-10-30T12:00:00Z"
}
```

### 2. POST /api/invoice/validate
Waliduje strukturę faktury XML zgodnie ze schematem KSeF FA(3).

**Request Body:**
```json
{
  "xmlContent": "<?xml version=\"1.0\" encoding=\"UTF-8\"?>...",
  "ksefNumber": "6511153259-20251015-010020140418-0D",
  "isBase64": false
}
```

**Parametry:**
- `xmlContent` (required): Zawartość XML faktury (tekst lub base64)
- `ksefNumber` (optional): Numer KSeF (dla trybu online)
- `isBase64` (optional): Czy `xmlContent` jest zakodowany w base64 (domyślnie: false)

**Odpowiedź:**
```json
{
  "isValid": true,
  "errors": [],
  "warnings": [],
  "invoiceNumber": "FA811/05/2025",
  "sellerNip": "7309436499",
  "mode": "Offline"
}
```

**Przykład użycia (curl):**
```bash
curl -X POST "http://localhost:5000/api/invoice/validate" \
  -H "Content-Type: application/json" \
  -d "{\"xmlContent\":\"...\",\"isBase64\":false}"
```

### 3. POST /api/invoice/parse
Parsuje fakturę XML i zwraca dane jako JSON.

**Request Body:** (jak w `/validate`)

**Odpowiedź:**
```json
{
  "invoiceNumber": "FA811/05/2025",
  "issueDate": "2025-09-01T00:00:00Z",
  "mode": "Offline",
  "ksefNumber": null,
  "seller": {
    "name": "Elektrownia S.A.",
    "nip": "7309436499",
    "address": "ul. Kwiatowa 1 m. 2, 00-001 Warszawa",
    "email": "elektrownia@elektrownia.pl",
    "phone": "667444555"
  },
  "buyer": {
    "name": "F.H.U. Jan Kowalski",
    "nip": "1187654321",
    "address": "ul. Polna 1, 00-001 Warszawa",
    "email": "jan@kowalski.pl",
    "phone": "555777999"
  },
  "lines": [
    {
      "lineNumber": 1,
      "description": "Energia elektryczna",
      "quantity": 100.00,
      "unitPrice": 10.00,
      "netAmount": 1000.00,
      "vatRate": 23
    }
  ],
  "summary": {
    "totalNet": 1000.00,
    "totalVat": 230.00,
    "totalGross": 1230.00,
    "currency": "PLN"
  },
  "payment": {
    "dueDate": "2025-10-01",
    "paymentMethod": "1",
    "bankAccount": "12345678901234567890123456"
  }
}
```

### 4. POST /api/invoice/generate-qr-code
Generuje kod QR dla numeru KSeF (do osadzania w zewnętrznych systemach).

**Request Body:**
```json
{
  "ksefNumber": "6511153259-20251015-010020140418-0D",
  "useProduction": false,
  "format": "svg",
  "size": 200,
  "includeLabel": false
}
```

**Parametry:**
- `ksefNumber` (required): Numer KSeF faktury (format: XXXXXXXXXX-YYYYMMDD-XXXXXXXXXXXX-XX)
- `useProduction` (optional): Użyj środowiska produkcyjnego (domyślnie: false = test)
- `format` (optional): Format kodu QR: "svg", "base64", "both" (domyślnie: "svg")
- `size` (optional): Rozmiar kodu QR w pikselach (domyślnie: 200)
- `includeLabel` (optional): Czy dołączyć etykietę z numerem KSeF (domyślnie: false)

**Odpowiedź (format = "svg"):**
```json
{
  "ksefNumber": "6511153259-20251015-010020140418-0D",
  "qrCodeSvg": "<svg xmlns='http://www.w3.org/2000/svg'>...</svg>",
  "qrCodeBase64": null,
  "format": "svg",
  "verificationUrl": "https://ksef-test.mf.gov.pl/web/verify/6511153259-20251015-010020140418-0D",
  "size": 200,
  "environment": "test"
}
```

**Odpowiedź (format = "base64"):**
```json
{
  "ksefNumber": "6511153259-20251015-010020140418-0D",
  "qrCodeSvg": null,
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "format": "base64",
  "verificationUrl": "https://ksef-test.mf.gov.pl/web/verify/6511153259-20251015-010020140418-0D",
  "size": 200,
  "environment": "test"
}
```

**Odpowiedź (format = "both"):**
```json
{
  "ksefNumber": "6511153259-20251015-010020140418-0D",
  "qrCodeSvg": "<svg xmlns='http://www.w3.org/2000/svg'>...</svg>",
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "format": "both",
  "verificationUrl": "https://ksef-test.mf.gov.pl/web/verify/6511153259-20251015-010020140418-0D",
  "size": 200,
  "environment": "test"
}
```

**Przypadki użycia:**
- System ERP generuje własne PDF-y i potrzebuje tylko kodu QR
- Portal klienta wyświetla kod QR do weryfikacji faktury
- Aplikacja mobilna skanuje kod QR z faktury
- System drukowania etykiet drukuje kody QR
- E-commerce dodaje kody QR do faktur online

**Format SVG vs Base64:**
- **SVG** (zalecany): Format wektorowy, idealny do osadzania w PDF-ach generowanych przez zewnętrzne systemy. Nieskończenie skalowalny bez utraty jakości.
- **Base64 PNG**: Format rastrowy, gotowy do wyświetlenia w przeglądarkach i aplikacjach webowych jako `<img src="data:image/png;base64,..."/>`.
- **Both**: Zwraca oba formaty jednocześnie.

### 5. POST /api/invoice/generate-pdf
Generuje PDF z faktury XML.

**Request Body:**
```json
{
  "xmlContent": "<?xml version=\"1.0\" encoding=\"UTF-8\"?>...",
  "isBase64": false,
  "ksefNumber": "6511153259-20251015-010020140418-0D",
  "certificateSource": "WindowsStore",
  "certificateThumbprint": null,
  "certificateSubject": null,
  "certificateStoreName": "My",
  "certificateStoreLocation": "CurrentUser",
  "azureKeyVaultUrl": null,
  "azureKeyVaultCertificateName": null,
  "azureKeyVaultCertificateVersion": null,
  "azureAuthenticationType": "DefaultAzureCredential",
  "azureTenantId": null,
  "azureClientId": null,
  "azureClientSecret": null,
  "useProduction": false,
  "validateInvoice": true,
  "returnFormat": "file"
}
```

**Parametry:**
- `xmlContent` (required): Zawartość XML faktury
- `isBase64` (optional): Czy XML jest w base64 (domyślnie: false)
- `ksefNumber` (optional): Numer KSeF (dla trybu online)
- `certificateSource` (optional): Źródło certyfikatu: "WindowsStore" lub "AzureKeyVault" (domyślnie: "WindowsStore")
- `certificateThumbprint` (optional): Thumbprint certyfikatu z Windows Store (dla QR kodu II)
- `certificateSubject` (optional): Subject certyfikatu (alternatywa dla thumbprint)
- `certificateStoreName` (optional): Nazwa store (domyślnie: "My")
- `certificateStoreLocation` (optional): Lokalizacja store (domyślnie: "CurrentUser")
- `azureKeyVaultUrl` (optional): URL Azure Key Vault (np. https://myvault.vault.azure.net/)
- `azureKeyVaultCertificateName` (optional): Nazwa certyfikatu w Key Vault
- `azureKeyVaultCertificateVersion` (optional): Wersja certyfikatu w Key Vault (domyślnie: latest)
- `azureAuthenticationType` (optional): Typ uwierzytelniania Azure (domyślnie: "DefaultAzureCredential")
- `azureTenantId` (optional): Azure Tenant ID (dla ClientSecret)
- `azureClientId` (optional): Azure Client ID (dla ClientSecret i Managed Identity)
- `azureClientSecret` (optional): Azure Client Secret (dla ClientSecret)
- `useProduction` (optional): Użyj produkcyjnego API KSeF (domyślnie: false = test)
- `validateInvoice` (optional): Waliduj przed generowaniem (domyślnie: true)
- `returnFormat` (optional): Format odpowiedzi: "file" lub "base64" (domyślnie: "file")

**Odpowiedź (returnFormat = "file"):**
Plik PDF do pobrania (`application/pdf`)

**Odpowiedź (returnFormat = "base64"):**
```json
{
  "pdfBase64": "JVBERi0xLjQKJeLjz9MKNSAwIG9iago8PC9GaWx0ZXIvRmxhdGVEZWNvZGU...",
  "fileSizeBytes": 45678,
  "invoiceNumber": "FA811/05/2025",
  "mode": "Offline"
}
```

## Obsługa Certyfikatów

API obsługuje certyfikaty do generowania QR kodu II (weryfikacja kwalifikowanym podpisem elektronicznym) z **dwóch źródeł**:

### 1. Windows Certificate Store

Wyszukiwanie po Thumbprint:
```json
{
  "certificateSource": "WindowsStore",
  "certificateThumbprint": "1234567890ABCDEF1234567890ABCDEF12345678",
  "certificateStoreName": "My",
  "certificateStoreLocation": "CurrentUser"
}
```

Wyszukiwanie po Subject:
```json
{
  "certificateSource": "WindowsStore",
  "certificateSubject": "CN=Jan Kowalski, O=Firma, C=PL",
  "certificateStoreName": "My",
  "certificateStoreLocation": "CurrentUser"
}
```

**Dostępne Store Names:**
- `My` - Personal (certyfikaty użytkownika)
- `Root` - Trusted Root Certification Authorities
- `CA` - Intermediate Certification Authorities
- `TrustedPeople` - Trusted People

**Dostępne Store Locations:**
- `CurrentUser` - Store użytkownika (domyślnie)
- `LocalMachine` - Store systemowy (wymaga uprawnień administratora)

### 2. Azure Key Vault

API obsługuje certyfikaty z Azure Key Vault z różnymi metodami uwierzytelniania:

**DefaultAzureCredential (domyślna):**
```json
{
  "certificateSource": "AzureKeyVault",
  "azureKeyVaultUrl": "https://myvault.vault.azure.net/",
  "azureKeyVaultCertificateName": "ksef-offline-cert"
}
```

**ManagedIdentity (dla Azure VMs i App Service):**
```json
{
  "certificateSource": "AzureKeyVault",
  "azureKeyVaultUrl": "https://myvault.vault.azure.net/",
  "azureKeyVaultCertificateName": "ksef-offline-cert",
  "azureAuthenticationType": "ManagedIdentity"
}
```

**ClientSecret (dla Service Principal):**
```json
{
  "certificateSource": "AzureKeyVault",
  "azureKeyVaultUrl": "https://myvault.vault.azure.net/",
  "azureKeyVaultCertificateName": "ksef-offline-cert",
  "azureAuthenticationType": "ClientSecret",
  "azureTenantId": "00000000-0000-0000-0000-000000000000",
  "azureClientId": "11111111-1111-1111-1111-111111111111",
  "azureClientSecret": "your-secret-here"
}
```

**Z konkretną wersją certyfikatu:**
```json
{
  "certificateSource": "AzureKeyVault",
  "azureKeyVaultUrl": "https://myvault.vault.azure.net/",
  "azureKeyVaultCertificateName": "ksef-offline-cert",
  "azureKeyVaultCertificateVersion": "abc123def456"
}
```

**Dostępne typy uwierzytelniania Azure:**
- `DefaultAzureCredential` - automatycznie wykrywa środowisko (domyślnie)
- `ManagedIdentity` - dla Azure VMs i App Service
- `ClientSecret` - dla aplikacji z Service Principal
- `EnvironmentCredential` - ze zmiennych środowiskowych
- `AzureCliCredential` - z zalogowanego Azure CLI

**Wymagane uprawnienia w Azure:**
- **Key Vault Certificates User** lub **Key Vault Reader** - odczyt certyfikatów
- **Key Vault Secrets User** - odczyt klucza prywatnego

## Ważne: Konwencja nazewnictwa JSON

API używa konwencji **camelCase** dla nazw właściwości JSON (zgodnie ze standardem ASP.NET Core).

**Poprawne nazwy parametrów (generate-pdf):**
- `xmlContent` - zawartość XML faktury
- `isBase64` - czy XML jest zakodowany w base64
- `ksefNumber` - numer KSeF (opcjonalny)
- `validateInvoice` - czy walidować fakturę (domyślnie: true)
- `returnFormat` - format odpowiedzi: "file" lub "base64"
- `certificateSource` - źródło certyfikatu: "WindowsStore" lub "AzureKeyVault" (domyślnie: "WindowsStore")
- `certificateThumbprint` - thumbprint certyfikatu z Windows Store
- `certificateSubject` - subject certyfikatu
- `certificateStoreName` - nazwa store (domyślnie: "My")
- `certificateStoreLocation` - lokalizacja store (domyślnie: "CurrentUser")
- `azureKeyVaultUrl` - URL Azure Key Vault
- `azureKeyVaultCertificateName` - nazwa certyfikatu w Key Vault
- `azureKeyVaultCertificateVersion` - wersja certyfikatu w Key Vault (opcjonalna)
- `azureAuthenticationType` - typ uwierzytelniania Azure (domyślnie: "DefaultAzureCredential")
- `azureTenantId` - Azure Tenant ID (dla ClientSecret)
- `azureClientId` - Azure Client ID (dla ClientSecret i Managed Identity)
- `azureClientSecret` - Azure Client Secret (dla ClientSecret)
- `useProduction` - użyj środowiska produkcyjnego (domyślnie: false)

**Poprawne nazwy parametrów (generate-qr-code):**
- `ksefNumber` - numer KSeF faktury (wymagany)
- `useProduction` - użyj środowiska produkcyjnego (domyślnie: false)
- `format` - format kodu QR: "svg", "base64", "both" (domyślnie: "svg")
- `size` - rozmiar kodu QR w pikselach (domyślnie: 200)
- `includeLabel` - czy dołączyć etykietę z numerem KSeF (domyślnie: false)

**Odpowiedź (returnFormat = "base64"):**
- `pdfBase64` - PDF zakodowany w base64
- `fileSizeBytes` - rozmiar pliku w bajtach
- `invoiceNumber` - numer faktury
- `mode` - tryb wystawienia ("Online" lub "Offline")

## Testowanie

### 1. Przez Swagger UI
Uruchom API i otwórz http://localhost:5000 (lub https://localhost:7000 w trybie development).

Swagger UI pozwala na interaktywne testowanie wszystkich endpointów z przeglądarki.

### 2. Przez cURL

**Walidacja:**
```bash
curl -X POST "http://localhost:5000/api/invoice/validate" \
  -H "Content-Type: application/json" \
  -d @test-request.json
```

**Parsowanie:**
```bash
curl -X POST "http://localhost:5000/api/invoice/parse" \
  -H "Content-Type: application/json" \
  -d @test-request.json
```

**Generowanie PDF (base64):**
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-pdf" \
  -H "Content-Type: application/json" \
  -d "{\"xmlContent\":\"...\",\"returnFormat\":\"base64\"}"
```

**Generowanie PDF (plik):**
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-pdf" \
  -H "Content-Type: application/json" \
  -d @test-request.json \
  --output faktura.pdf
```

### 3. Przez PowerShell

**Przykład 1: XML jako tekst**
```powershell
$xml = Get-Content "faktura.xml" -Raw
$body = @{
    xmlContent = $xml
    isBase64 = $false
    validateInvoice = $false
    returnFormat = "base64"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
    -Method Post -Body $body -ContentType "application/json"

$pdfBytes = [Convert]::FromBase64String($response.pdfBase64)
[System.IO.File]::WriteAllBytes("faktura.pdf", $pdfBytes)
```

**Przykład 2: XML jako base64 (zalecane)**
```powershell
$xmlBytes = [System.IO.File]::ReadAllBytes("faktura.xml")
$xmlBase64 = [Convert]::ToBase64String($xmlBytes)

$body = @{
    xmlContent = $xmlBase64
    isBase64 = $true
    validateInvoice = $false
    returnFormat = "base64"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
    -Method Post -Body $body -ContentType "application/json"

Write-Host "Invoice: $($response.invoiceNumber)"
Write-Host "Mode: $($response.mode)"
Write-Host "Size: $($response.fileSizeBytes) bytes"

$pdfBytes = [Convert]::FromBase64String($response.pdfBase64)
[System.IO.File]::WriteAllBytes("faktura.pdf", $pdfBytes)
```

**Przykład 3: Z certyfikatem z Azure Key Vault**
```powershell
$xmlBytes = [System.IO.File]::ReadAllBytes("faktura.xml")
$xmlBase64 = [Convert]::ToBase64String($xmlBytes)

$body = @{
    xmlContent = $xmlBase64
    isBase64 = $true
    validateInvoice = $false
    returnFormat = "base64"
    certificateSource = "AzureKeyVault"
    azureKeyVaultUrl = "https://myvault.vault.azure.net/"
    azureKeyVaultCertificateName = "ksef-offline-cert"
    azureAuthenticationType = "DefaultAzureCredential"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
    -Method Post -Body $body -ContentType "application/json"

Write-Host "Invoice: $($response.invoiceNumber)"
Write-Host "Mode: $($response.mode)"
Write-Host "Size: $($response.fileSizeBytes) bytes"

$pdfBytes = [Convert]::FromBase64String($response.pdfBase64)
[System.IO.File]::WriteAllBytes("faktura.pdf", $pdfBytes)
```

**Przykład 4: Z Service Principal (dla automatyzacji)**
```powershell
$body = @{
    xmlContent = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes("faktura.xml"))
    isBase64 = $true
    validateInvoice = $false
    returnFormat = "base64"
    certificateSource = "AzureKeyVault"
    azureKeyVaultUrl = "https://myvault.vault.azure.net/"
    azureKeyVaultCertificateName = "ksef-offline-cert"
    azureAuthenticationType = "ClientSecret"
    azureTenantId = "00000000-0000-0000-0000-000000000000"
    azureClientId = "11111111-1111-1111-1111-111111111111"
    azureClientSecret = $env:AZURE_CLIENT_SECRET  # Ze zmiennej środowiskowej
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
    -Method Post -Body $body -ContentType "application/json"

[System.IO.File]::WriteAllBytes("faktura.pdf",
    [Convert]::FromBase64String($response.pdfBase64))
```

### 4. Przez C# (Generate PDF)
```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

var request = new
{
    xmlContent = File.ReadAllText("faktura.xml"),
    isBase64 = false,
    validateInvoice = false,
    returnFormat = "base64"
};

var response = await client.PostAsJsonAsync("/api/invoice/generate-pdf", request);
var result = await response.Content.ReadFromJsonAsync<dynamic>();

Console.WriteLine($"Invoice: {result.invoiceNumber}");
Console.WriteLine($"Mode: {result.mode}");
Console.WriteLine($"Size: {result.fileSizeBytes} bytes");

File.WriteAllBytes("faktura.pdf", Convert.FromBase64String(result.pdfBase64.ToString()));
```

### 5. Przez C# (Generate QR Code)

**Przykład 1: SVG dla systemu generującego PDF-y**
```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

var request = new
{
    ksefNumber = "6511153259-20251015-010020140418-0D",
    useProduction = false,
    format = "svg",
    size = 200
};

var response = await client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);
var result = await response.Content.ReadFromJsonAsync<dynamic>();

Console.WriteLine($"KSeF Number: {result.ksefNumber}");
Console.WriteLine($"Verification URL: {result.verificationUrl}");

// Osadź SVG w swoim PDF-ie
string svgContent = result.qrCodeSvg;
File.WriteAllText("qr-code.svg", svgContent);
```

**Przykład 2: Base64 dla aplikacji webowej**
```csharp
var request = new
{
    ksefNumber = "6511153259-20251015-010020140418-0D",
    useProduction = false,
    format = "base64",
    size = 300
};

var response = await client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);
var result = await response.Content.ReadFromJsonAsync<dynamic>();

// Wygeneruj HTML z obrazem
string html = $@"
<html>
<body>
    <h1>Faktura KSeF</h1>
    <img src=""data:image/png;base64,{result.qrCodeBase64}"" alt=""QR Code"" />
    <p><a href=""{result.verificationUrl}"">Zweryfikuj fakturę</a></p>
</body>
</html>";

File.WriteAllText("faktura.html", html);
```

**Przykład 3: Oba formaty jednocześnie**
```csharp
var request = new
{
    ksefNumber = "6511153259-20251015-010020140418-0D",
    useProduction = true,
    format = "both",
    size = 200
};

var response = await client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);
var result = await response.Content.ReadFromJsonAsync<dynamic>();

// SVG dla druku wysokiej jakości
File.WriteAllText("qr-print.svg", result.qrCodeSvg);

// PNG dla wyświetlenia na ekranie
var pngBytes = Convert.FromBase64String(result.qrCodeBase64.ToString());
File.WriteAllBytes("qr-display.png", pngBytes);
```

## Przykładowe Pliki XML

Przykładowe faktury znajdują się w:
- `KSeFPrinter.Tests\TestData\FakturaTEST017.xml` - tryb offline
- `KSeFPrinter.Tests\TestData\6511153259-20251015-010020140418-0D.xml` - tryb online

## Konfiguracja

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Swagger": {
    "EnableInProduction": true
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
  },
  "KSeFConnector": {
    "BaseUrl": "http://localhost:5000",
    "Timeout": 30,
    "Note": "Konfiguracja dla przyszłej integracji (Wariant B)"
  }
}
```

**Uwaga:** Konfiguracja Azure Key Vault w `appsettings.json` jest opcjonalna. Parametry można przekazywać również bezpośrednio w request body do endpointu `/generate-pdf`.

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Urls": "https://localhost:7000;http://localhost:5000"
}
```

## Deployment na Windows Server

### 1. Jako Windows Service

**Publikacja:**
```bash
dotnet publish -c Release -r win-x64 --self-contained -o C:\publish\KSeFPrinterAPI
```

**Instalacja serwisu (sc.exe):**
```cmd
sc create KSeFPrinterAPI binPath="C:\publish\KSeFPrinterAPI\KSeFPrinter.API.exe" start=auto
sc description KSeFPrinterAPI "KSeF Printer REST API"
sc start KSeFPrinterAPI
```

**Alternatywnie (NSSM):**
```cmd
nssm install KSeFPrinterAPI "C:\publish\KSeFPrinterAPI\KSeFPrinter.API.exe"
nssm set KSeFPrinterAPI AppDirectory "C:\publish\KSeFPrinterAPI"
nssm set KSeFPrinterAPI Start SERVICE_AUTO_START
nssm start KSeFPrinterAPI
```

### 2. Jako IIS Application

**Wymagania:**
- ASP.NET Core Runtime 9.0
- ASP.NET Core Module (ANCM) dla IIS

**Kroki:**
1. Opublikuj aplikację:
   ```bash
   dotnet publish -c Release -o C:\inetpub\KSeFPrinterAPI
   ```

2. Utwórz Application Pool w IIS:
   - .NET CLR Version: No Managed Code
   - Managed Pipeline Mode: Integrated

3. Dodaj aplikację w IIS:
   - Physical Path: C:\inetpub\KSeFPrinterAPI
   - Application Pool: (utworzony w kroku 2)

4. Skonfiguruj uprawnienia:
   - Folder aplikacji: IIS_IUSRS (Read & Execute)

### 3. Standalone (Kestrel)

Uruchom bezpośrednio:
```bash
dotnet KSeFPrinter.API.dll --urls "http://0.0.0.0:5000"
```

Lub jako scheduled task / bat script.

## CORS

API domyślnie ma włączony CORS dla wszystkich origin (`AllowAll`).

Dla produkcji rozważ ograniczenie do konkretnych domen w `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("Production");
```

## Przyszłe Funkcjonalności (Wariant B)

W przyszłości planowana jest integracja z **KSeF Connector API**:

- Pobieranie faktur bezpośrednio z KSeF
- Wysyłanie faktur do KSeF
- Automatyczne generowanie PDF po wysłaniu faktury
- Wsadowe przetwarzanie faktur

Interfejs `IKSeFConnectorService` jest już przygotowany w projekcie.

## Błędy i Rozwiązywanie

### "Unable to resolve service for type IXmlInvoiceParser"
DI nie jest poprawnie skonfigurowane. Sprawdź `Program.cs`.

### "Failed to determine the https port for redirect"
W trybie production brak konfiguracji HTTPS. Ustaw zmienną środowiskową:
```bash
$env:ASPNETCORE_URLS = "http://localhost:5000;https://localhost:7000"
```

Lub usuń `app.UseHttpsRedirection()` z `Program.cs` dla HTTP-only.

### "Nie znaleziono certyfikatu"
- Sprawdź thumbprint certyfikatu: `certmgr.msc` (Windows)
- Upewnij się że certyfikat ma klucz prywatny
- Sprawdź lokalizację store (CurrentUser vs LocalMachine)

### "Validation failed"
- Sprawdź czy XML jest zgodny ze schematem KSeF FA(3)
- Sprawdź namespace i wersję schematu
- Użyj endpointu `/validate` aby zobaczyć szczegółowe błędy

## Licencja

Projekt wewnętrzny.

## Kontakt

W razie problemów sprawdź logi aplikacji lub uruchom z poziomem logowania `Debug`.
