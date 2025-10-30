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

### 4. POST /api/invoice/generate-pdf
Generuje PDF z faktury XML.

**Request Body:**
```json
{
  "xmlContent": "<?xml version=\"1.0\" encoding=\"UTF-8\"?>...",
  "isBase64": false,
  "ksefNumber": "6511153259-20251015-010020140418-0D",
  "certificateThumbprint": null,
  "certificateSubject": null,
  "certificateStoreName": "My",
  "certificateStoreLocation": "CurrentUser",
  "useProduction": false,
  "validateInvoice": true,
  "returnFormat": "file"
}
```

**Parametry:**
- `xmlContent` (required): Zawartość XML faktury
- `isBase64` (optional): Czy XML jest w base64 (domyślnie: false)
- `ksefNumber` (optional): Numer KSeF (dla trybu online)
- `certificateThumbprint` (optional): Thumbprint certyfikatu z Windows Store (dla QR kodu II)
- `certificateSubject` (optional): Subject certyfikatu (alternatywa dla thumbprint)
- `certificateStoreName` (optional): Nazwa store (domyślnie: "My")
- `certificateStoreLocation` (optional): Lokalizacja store (domyślnie: "CurrentUser")
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

API obsługuje certyfikaty z **Windows Certificate Store** do generowania QR kodu II (weryfikacja kwalifikowanym podpisem elektronicznym).

### Wyszukiwanie po Thumbprint
```json
{
  "certificateThumbprint": "1234567890ABCDEF1234567890ABCDEF12345678",
  "certificateStoreName": "My",
  "certificateStoreLocation": "CurrentUser"
}
```

### Wyszukiwanie po Subject
```json
{
  "certificateSubject": "CN=Jan Kowalski, O=Firma, C=PL",
  "certificateStoreName": "My",
  "certificateStoreLocation": "CurrentUser"
}
```

### Dostępne Store Names
- `My` - Personal (certyfikaty użytkownika)
- `Root` - Trusted Root Certification Authorities
- `CA` - Intermediate Certification Authorities
- `TrustedPeople` - Trusted People

### Dostępne Store Locations
- `CurrentUser` - Store użytkownika (domyślnie)
- `LocalMachine` - Store systemowy (wymaga uprawnień administratora)

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
```powershell
$xml = Get-Content "faktura.xml" -Raw
$body = @{
    XmlContent = $xml
    IsBase64 = $false
    ReturnFormat = "base64"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
    -Method Post -Body $body -ContentType "application/json"

[Convert]::FromBase64String($response.PdfBase64) | Set-Content "faktura.pdf" -Encoding Byte
```

### 4. Przez C#
```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

var request = new
{
    XmlContent = File.ReadAllText("faktura.xml"),
    IsBase64 = false,
    ReturnFormat = "base64"
};

var response = await client.PostAsJsonAsync("/api/invoice/generate-pdf", request);
var result = await response.Content.ReadFromJsonAsync<PdfResponse>();

File.WriteAllBytes("faktura.pdf", Convert.FromBase64String(result.PdfBase64));
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
  "KSeFConnector": {
    "BaseUrl": "http://localhost:5000",
    "Timeout": 30,
    "Note": "Konfiguracja dla przyszłej integracji (Wariant B)"
  }
}
```

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
