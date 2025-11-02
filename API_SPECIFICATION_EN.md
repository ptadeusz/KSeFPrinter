# KSeF Printer API - Specification

**Version:** 1.0.0
**Date:** 2025-01-02
**Framework:** ASP.NET Core 9.0
**Architecture:** REST API with Swagger UI

---

## Table of Contents

1. [General Information](#general-information)
2. [Configuration and Setup](#configuration-and-setup)
3. [Licensing](#licensing)
4. [API Endpoints](#api-endpoints)
   - [1. Invoice XML Validation](#1-invoice-xml-validation)
   - [2. Parse Invoice XML to JSON](#2-parse-invoice-xml-to-json)
   - [3. Generate PDF](#3-generate-pdf)
   - [4. Generate QR Code](#4-generate-qr-code)
   - [5. Health Check](#5-health-check)
5. [Data Models](#data-models)
6. [Error Codes](#error-codes)
7. [Usage Examples](#usage-examples)
   - [cURL](#curl)
   - [PowerShell](#powershell)
   - [C# (.NET)](#c-net)
   - [Python](#python)
8. [Additional Information](#additional-information)

---

## General Information

### Description
KSeF Printer API is a RESTful web service for processing electronic invoices in KSeF FA(3) format. Features:
- Invoice XML validation
- XML to JSON parsing
- PDF generation with QR codes
- QR code generation for KSeF numbers

### Technology
- **Framework:** ASP.NET Core 9.0
- **Language:** C# (.NET 9)
- **Documentation:** Swagger/OpenAPI
- **Data Format:** JSON
- **Certificate Support:** Windows Certificate Store, Azure Key Vault

### Environments
- **Test:** `https://ksef-test.mf.gov.pl`
- **Production:** `https://ksef.mf.gov.pl`

### Base URL
```
http://localhost:5000/api
https://localhost:5001/api
```

Swagger UI available at:
```
http://localhost:5000/
https://localhost:5001/
```

---

## Configuration and Setup

### Requirements
- .NET 9.0 SDK
- Windows (for Windows Certificate Store certificates)
- Optional: Azure Key Vault (for cloud certificates)

### Running the Application

**Development:**
```bash
cd KSeFPrinter.API
dotnet run
```

**Production:**
```bash
dotnet publish -c Release
cd bin/Release/net9.0/publish
dotnet KSeFPrinter.API.dll
```

### Configuration (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "Swagger": {
    "EnableInProduction": true
  },
  "License": {
    "FilePath": "license.lic"
  },
  "Certificates": {
    "Online": {
      "Enabled": false,
      "Source": "WindowsStore",
      "WindowsStore": {
        "Thumbprint": "",
        "Subject": "",
        "StoreName": "My",
        "StoreLocation": "CurrentUser"
      },
      "AzureKeyVault": {
        "KeyVaultUrl": "",
        "CertificateName": "",
        "CertificateVersion": null,
        "AuthenticationType": "DefaultAzureCredential",
        "TenantId": "",
        "ClientId": "",
        "ClientSecret": ""
      }
    },
    "Offline": {
      "Enabled": false,
      "Source": "WindowsStore",
      "WindowsStore": {
        "Thumbprint": "",
        "Subject": "",
        "StoreName": "My",
        "StoreLocation": "CurrentUser"
      },
      "AzureKeyVault": {
        "KeyVaultUrl": "",
        "CertificateName": "",
        "CertificateVersion": null,
        "AuthenticationType": "DefaultAzureCredential",
        "TenantId": "",
        "ClientId": "",
        "ClientSecret": ""
      }
    }
  }
}
```

**Certificates:**
- **Online Certificate** - used for generating QR CODE II for ONLINE invoices
- **Offline Certificate** - used for digital signing of OFFLINE invoices
- Certificates are loaded **at application startup** from configuration
- **NOT provided in API request** - API automatically selects appropriate certificate based on invoice mode
- Supported sources: **Windows Certificate Store** or **Azure Key Vault**
- API port configured in `Kestrel.Endpoints.Http.Url`

### Environment Variables

```bash
# Application URL
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# Environment
ASPNETCORE_ENVIRONMENT=Development

# License (optional)
License__FilePath=C:\path\to\license.lic
```

---

## Licensing

### Requirements
The API requires a valid license for the **KSeF Printer** product. The application will not start without a license.

### License Format
`license.lic` file (JSON with RSA signature):
```json
{
  "licenseKey": "KSEF-20250102-XXXXXXXX",
  "issuedTo": "ABC Company Ltd.",
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

### Validation at Startup
```
=== License Check ===
✅ License valid
   Owner: ABC Company Ltd.
   Valid until: 2026-12-31
   Allowed NIPs: 1
```

If the license is invalid or expired, the application will stop with exit code `1`.

### Validation at application startup:
1. ✅ License file exists
2. ✅ License has not expired (`expiryDate`)
3. ✅ RSA signature is valid (verified with public key)
4. ✅ `features.ksefPrinter = true` (permission for KSeF Printer)

### Validation when generating PDF:
✅ **NIP Validation** - on every PDF generation request, it is validated that **at least one** NIP from the invoice is in the `allowedNips` list:
- Checked: **Seller** NIP (Podmiot1)
- Checked: **Buyer** NIP (Podmiot2)
- Checked: All **Subject3** NIPs (there can be many)

**Validation logic:** If **ANY** of the NIPs is in the `allowedNips` list, PDF generation is allowed.

**If no NIP is allowed:**
- HTTP 403 Forbidden is returned
- Response contains list of all NIPs from the invoice
- Warning is logged with information about missing permissions

---

## API Endpoints

### 1. Invoice XML Validation

**Endpoint:** `POST /api/invoice/validate`

**Description:** Validates invoice XML for compliance with KSeF FA(3) schema.

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
  "warnings": ["Seller phone number missing"],
  "invoiceNumber": "FV/2025/01/001",
  "sellerNip": "1234567890",
  "mode": "Online"
}
```

**Response (validation failed):**
```json
{
  "isValid": false,
  "errors": [
    "Required field missing: Podmiot1.DaneIdentyfikacyjne.NIP",
    "Invalid checksum in line 1"
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
  "error": "Invalid XML format"
}
```

---

### 2. Parse Invoice XML to JSON

**Endpoint:** `POST /api/invoice/parse`

**Description:** Parses invoice XML and returns data in structured JSON format.

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
    "name": "ABC Company Ltd.",
    "nip": "1234567890",
    "address": "Test Street 1, 00-001 Warsaw",
    "email": "contact@abc.pl",
    "phone": "+48123456789"
  },
  "buyer": {
    "name": "XYZ Corp.",
    "nip": "0987654321",
    "address": "Business Street 2, 00-002 Krakow",
    "email": "office@xyz.pl",
    "phone": "+48987654321"
  },
  "lines": [
    {
      "lineNumber": 1,
      "description": "Consulting service",
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
  "error": "XML parsing error: invalid structure"
}
```

---

### 3. Generate PDF

**Endpoint:** `POST /api/invoice/generate-pdf`

**Description:** Generates PDF from invoice XML with QR codes (KSeF verification + optionally certificate signature).

#### QR Codes on PDF

The generator creates **two types of QR codes** (according to KSeF requirements):

**QR CODE I - Invoice verification (always generated):**
- Contains: Seller NIP + issue date + XML hash + (optionally) KSeF number
- URL: `https://ksef.mf.gov.pl/web/verify/{ksefNumber}` (for ONLINE)
- URL: `https://ksef.mf.gov.pl/web/verify?nip={nip}&date={date}&hash={hash}` (for OFFLINE)
- **Does not require certificate** - always generated

**QR CODE II - Issuer's digital signature (optional):**
- Contains: digital signature of XML with issuer's certificate
- URL: `https://ksef.mf.gov.pl/web/verify/signed?...`
- **Requires certificate** - generated only when certificate is available
- Used for document authenticity verification

**Request:**
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

**Parameters:**
- `xmlContent` - invoice XML content (plain text or base64)
- `isBase64` - whether XML is base64 encoded (default: false)
- `ksefNumber` - optional KSeF number (if not in XML)
- `useProduction` - verification environment (true = production, false = test)
- `validateInvoice` - whether to validate invoice before generating (default: true)
- `returnFormat` - return format: "file" (binary) or "base64" (JSON)

**Certificates:**
- Certificates are loaded automatically from `appsettings.json` at startup
- API automatically selects certificate based on invoice mode:
  - **ONLINE mode** → uses `Certificates.Online` certificate
  - **OFFLINE mode** → uses `Certificates.Offline` certificate
- If certificate is available → **QR CODE I + QR CODE II** are generated
- If certificate is NOT available → **only QR CODE I** is generated

**Response (returnFormat="file", 200 OK):**
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="Invoice_FV_2025_01_001_20250102_143522.pdf"

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

**Response (400 Bad Request - validation error):**
```json
{
  "error": "Invoice validation failed: required field NIP missing"
}
```

**Response (403 Forbidden - NIP not covered by license):**
```json
{
  "error": "License does not cover any NIP from this invoice",
  "invoiceNips": [
    "Seller: 1234567890",
    "Buyer: 0987654321"
  ],
  "allowedNips": 5,
  "message": "At least one NIP (seller, buyer, or subject3) must be on the allowed NIPs list in the license"
}
```

---

### 4. Generate QR Code

**Endpoint:** `POST /api/invoice/generate-qr-code`

**Description:** Generates **QR CODE I** (KSeF number verification) in SVG or PNG base64 format (for embedding in external systems).

**Note:** This endpoint generates **only QR CODE I** (invoice verification link by KSeF number). It does not generate **QR CODE II** (digital signature), which requires a certificate and is only available in `/api/invoice/generate-pdf`.

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

**Parameters:**
- `format`: `"svg"`, `"base64"`, `"both"`
- `size`: size in pixels (default: 200)
- `useProduction`: `true` = production, `false` = test

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

**Response (400 Bad Request - invalid KSeF number):**
```json
{
  "error": "Invalid KSeF number format: 1234-INVALID"
}
```

**Response (400 Bad Request - invalid format):**
```json
{
  "error": "Invalid format: png. Available: svg, base64, both"
}
```

---

### 5. Health Check

**Endpoint:** `GET /api/invoice/health`

**Description:** Checks API status.

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

## Data Models

### InvoiceValidationRequest
```csharp
{
  "xmlContent": string,          // XML content (plain text or base64)
  "ksefNumber": string?,          // Optional KSeF number
  "isBase64": bool                // Whether XML is base64 (default: false)
}
```

### InvoiceValidationResponse
```csharp
{
  "isValid": bool,                // Whether invoice is valid
  "errors": string[],             // List of errors
  "warnings": string[],           // List of warnings
  "invoiceNumber": string?,       // Invoice number (if parsed)
  "sellerNip": string?,           // Seller NIP (if parsed)
  "mode": string?                 // Mode: "Online" or "Offline"
}
```

### GeneratePdfRequest
```csharp
{
  // Basic
  "xmlContent": string,           // XML content
  "isBase64": bool,               // Whether XML is base64 (default: false)
  "ksefNumber": string?,          // Optional KSeF number
  "useProduction": bool,          // Environment (default: false = test)
  "validateInvoice": bool,        // Validate before generating (default: true)
  "returnFormat": string,         // "file" or "base64" (default: "file")

  // Certificate - Windows Store
  "certificateSource": string,    // "WindowsStore", "AzureKeyVault", "ConfigFile"
  "certificateThumbprint": string?,
  "certificateSubject": string?,
  "certificateStoreName": string, // Default: "My"
  "certificateStoreLocation": string, // Default: "CurrentUser"

  // Certificate - Azure Key Vault
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
  "pdfBase64": string,            // PDF in base64 (only when returnFormat="base64")
  "fileSizeBytes": long,          // File size in bytes
  "invoiceNumber": string,        // Invoice number
  "mode": string                  // Mode: "Online" or "Offline"
}
```

### GenerateQrCodeRequest
```csharp
{
  "ksefNumber": string,           // KSeF number (required)
  "useProduction": bool,          // Environment (default: false = test)
  "format": string,               // "svg", "base64", "both" (default: "svg")
  "size": int,                    // Size in pixels (default: 200)
  "includeLabel": bool            // Label with number (default: false)
}
```

### GenerateQrCodeResponse
```csharp
{
  "ksefNumber": string,           // KSeF number
  "qrCodeSvg": string?,           // SVG (if format="svg" or "both")
  "qrCodeBase64": string?,        // PNG base64 (if format="base64" or "both")
  "format": string,               // Returned format
  "verificationUrl": string,      // Verification URL
  "size": int,                    // Size in pixels
  "environment": string           // "test" or "production"
}
```

### InvoiceParseResponse
```csharp
{
  "invoiceNumber": string,
  "issueDate": DateTime,
  "mode": string,                 // "Online" or "Offline"
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

## Error Codes

### HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | OK | Success |
| 400 | Bad Request | Invalid XML, validation error |
| 401 | Unauthorized | No license (at startup, not during request) |
| 500 | Internal Server Error | Server error |

### Validation Errors

```json
{
  "error": "Error message"
}
```

**Examples:**
- `"Invalid XML format"`
- `"Required field missing: Podmiot1.DaneIdentyfikacyjne.NIP"`
- `"Invalid KSeF number format: 1234-INVALID"`
- `"Certificate not found with thumbprint: A1B2C3D4E5F6..."`
- `"Invoice validation failed: invalid checksum"`

---

## Usage Examples

### cURL

**Validate invoice:**
```bash
curl -X POST "http://localhost:5000/api/invoice/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "xmlContent": "<?xml version=\"1.0\"?>...",
    "isBase64": false
  }'
```

**Generate PDF (file):**
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-pdf" \
  -H "Content-Type: application/json" \
  -d '{
    "xmlContent": "<?xml version=\"1.0\"?>...",
    "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
    "returnFormat": "file"
  }' \
  -o invoice.pdf
```

**Generate PDF (base64):**
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-pdf" \
  -H "Content-Type: application/json" \
  -d '{
    "xmlContent": "<?xml version=\"1.0\"?>...",
    "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
    "returnFormat": "base64"
  }'
```

**Generate QR code:**
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

**Validate invoice:**
```powershell
$xmlContent = Get-Content "invoice.xml" -Raw
$body = @{
    xmlContent = $xmlContent
    isBase64 = $false
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/validate" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

**Generate PDF:**
```powershell
$xmlContent = Get-Content "invoice.xml" -Raw
$body = @{
    xmlContent = $xmlContent
    ksefNumber = "1234567890123456789012345-20250102-ABCD-EF"
    returnFormat = "file"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body `
    -OutFile "invoice.pdf"
```

**Generate QR code:**
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

# Save SVG
$response.qrCodeSvg | Out-File "qr.svg"

# Save PNG
[Convert]::FromBase64String($response.qrCodeBase64) | Set-Content "qr.png" -Encoding Byte
```

### C# (.NET)

**Validate invoice:**
```csharp
using System.Net.Http.Json;

var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

var request = new
{
    xmlContent = File.ReadAllText("invoice.xml"),
    isBase64 = false
};

var response = await httpClient.PostAsJsonAsync("/api/invoice/validate", request);
var result = await response.Content.ReadFromJsonAsync<InvoiceValidationResponse>();

Console.WriteLine($"Valid: {result.IsValid}");
foreach (var error in result.Errors)
{
    Console.WriteLine($"Error: {error}");
}
```

**Generate PDF:**
```csharp
var request = new
{
    xmlContent = File.ReadAllText("invoice.xml"),
    ksefNumber = "1234567890123456789012345-20250102-ABCD-EF",
    returnFormat = "file"
};

var response = await httpClient.PostAsJsonAsync("/api/invoice/generate-pdf", request);
var pdfBytes = await response.Content.ReadAsByteArrayAsync();
await File.WriteAllBytesAsync("invoice.pdf", pdfBytes);
```

### Python

**Validate invoice:**
```python
import requests

with open("invoice.xml", "r", encoding="utf-8") as f:
    xml_content = f.read()

response = requests.post(
    "http://localhost:5000/api/invoice/validate",
    json={
        "xmlContent": xml_content,
        "isBase64": False
    }
)

result = response.json()
print(f"Valid: {result['isValid']}")
for error in result.get('errors', []):
    print(f"Error: {error}")
```

**Generate PDF:**
```python
response = requests.post(
    "http://localhost:5000/api/invoice/generate-pdf",
    json={
        "xmlContent": xml_content,
        "ksefNumber": "1234567890123456789012345-20250102-ABCD-EF",
        "returnFormat": "file"
    }
)

with open("invoice.pdf", "wb") as f:
    f.write(response.content)
```

**Generate QR code:**
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

# Save SVG
with open("qr.svg", "w") as f:
    f.write(result["qrCodeSvg"])

# Save PNG
import base64
with open("qr.png", "wb") as f:
    f.write(base64.b64decode(result["qrCodeBase64"]))
```

---

## Additional Information

### Swagger UI

After starting the API, Swagger UI is available at:
```
http://localhost:5000/
```

Interactive documentation includes:
- Complete specification of all endpoints
- API testing from browser
- Automatic request and response examples
- JSON schema validation

### Limits

- **Maximum XML size:** no limits (limited by ASP.NET Core: default 30MB)
- **Maximum PDF size:** no limits
- **Timeout:** 2 minutes (default)

### Security

- API does not require authentication (recommended to add in production)
- License is validated at application startup
- Certificates are loaded securely from Windows Certificate Store or Azure Key Vault
- No file storage on disk (everything in memory)

### Performance

- All operations are asynchronous
- PDF generation: ~500ms (depending on invoice size)
- XML validation: ~100ms
- QR generation: ~50ms

---

**End of specification**

Generated: 2025-01-02
Document version: 1.0.0
