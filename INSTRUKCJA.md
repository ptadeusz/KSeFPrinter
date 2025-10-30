# Instrukcja użytkownika KSeF Printer

## Spis treści

1. [Wprowadzenie](#wprowadzenie)
2. [Instalacja](#instalacja)
3. [Szybki start](#szybki-start)
4. [Szczegółowe użycie API](#szczegółowe-użycie-api)
5. [Konfiguracja opcji PDF](#konfiguracja-opcji-pdf)
6. [Tryby pracy](#tryby-pracy)
7. [Obsługa certyfikatów](#obsługa-certyfikatów)
8. [Przykłady użycia](#przykłady-użycia)
9. [Rozwiązywanie problemów](#rozwiązywanie-problemów)
10. [FAQ](#faq)

---

## Wprowadzenie

**KSeF Printer** to biblioteka .NET 9 do generowania wydruków PDF z faktur elektronicznych w formacie KSeF FA(3) v1-0E. Biblioteka automatycznie:

- Parsuje pliki XML zgodne ze schematem FA(3)
- Waliduje dane faktury
- Generuje profesjonalny dokument PDF
- Dodaje kody QR do weryfikacji faktury i certyfikatu

### Obsługiwane scenariusze

1. **Faktury z systemu KSeF (Online)** - z numerem KSeF
2. **Faktury z systemu ERP (Offline)** - bez numeru KSeF
3. **Tylko parsowanie** - odczyt danych bez generowania PDF
4. **Walidacja** - sprawdzenie poprawności faktury

---

## Instalacja

### Wymagania

- .NET 9.0 SDK lub nowszy
- System Windows, Linux lub macOS

### Dodanie do projektu

#### Opcja 1: Jako projekt w solution

```bash
# Sklonuj lub skopiuj projekt KSeFPrinter do swojego solution
dotnet sln add KSeFPrinter/KSeFPrinter.csproj

# Dodaj referencję do swojego projektu
dotnet add YourProject.csproj reference KSeFPrinter/KSeFPrinter.csproj
```

#### Opcja 2: Jako pakiet NuGet (po publikacji)

```bash
dotnet add package KSeFPrinter
```

### Dodanie do Dependency Injection

```csharp
using KSeFPrinter;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.DependencyInjection;

// W ConfigureServices lub Program.cs:
services.AddSingleton<IXmlInvoiceParser, XmlInvoiceParser>();
services.AddSingleton<InvoiceValidator>();
services.AddSingleton<ICryptographyService, CryptographyService>();
services.AddSingleton<IVerificationLinkService, VerificationLinkService>();
services.AddSingleton<IQrCodeService, QrCodeService>();
services.AddSingleton<IPdfGeneratorService, InvoicePdfGenerator>();
services.AddSingleton<InvoicePrinterService>();
```

---

## Szybki start

### Przykład 1: Najprostsze użycie

```csharp
using KSeFPrinter;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging.Abstractions;

// Inicjalizacja serwisów
var parser = new XmlInvoiceParser(NullLogger<XmlInvoiceParser>.Instance);
var validator = new InvoiceValidator(NullLogger<InvoiceValidator>.Instance);
var cryptoService = new CryptographyService(NullLogger<CryptographyService>.Instance);
var linkService = new VerificationLinkService(cryptoService, NullLogger<VerificationLinkService>.Instance);
var qrService = new QrCodeService(NullLogger<QrCodeService>.Instance);
var pdfGenerator = new InvoicePdfGenerator(linkService, qrService, NullLogger<InvoicePdfGenerator>.Instance);

var service = new InvoicePrinterService(parser, validator, pdfGenerator, NullLogger<InvoicePrinterService>.Instance);

// Wygeneruj PDF z pliku XML
await service.GeneratePdfFromFileAsync(
    xmlFilePath: "faktura.xml",
    pdfOutputPath: "faktura.pdf"
);

Console.WriteLine("PDF wygenerowany!");
```

### Przykład 2: Z logowaniem

```csharp
using Microsoft.Extensions.Logging;

// Konfiguracja logowania
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var parser = new XmlInvoiceParser(loggerFactory.CreateLogger<XmlInvoiceParser>());
var validator = new InvoiceValidator(loggerFactory.CreateLogger<InvoiceValidator>());
var cryptoService = new CryptographyService(loggerFactory.CreateLogger<CryptographyService>());
var linkService = new VerificationLinkService(cryptoService, loggerFactory.CreateLogger<VerificationLinkService>());
var qrService = new QrCodeService(loggerFactory.CreateLogger<QrCodeService>());
var pdfGenerator = new InvoicePdfGenerator(linkService, qrService, loggerFactory.CreateLogger<InvoicePdfGenerator>());

var service = new InvoicePrinterService(parser, validator, pdfGenerator, loggerFactory.CreateLogger<InvoicePrinterService>());

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf");
```

---

## Szczegółowe użycie API

### Główny serwis: `InvoicePrinterService`

Klasa `InvoicePrinterService` jest głównym punktem wejścia do biblioteki.

#### Metody generowania PDF

##### 1. `GeneratePdfFromFileAsync` - z pliku XML

```csharp
public async Task<byte[]?> GeneratePdfFromFileAsync(
    string xmlFilePath,
    string? pdfOutputPath = null,
    PdfGenerationOptions? options = null,
    string? numerKSeF = null,
    bool validateInvoice = true)
```

**Parametry:**
- `xmlFilePath` - ścieżka do pliku XML faktury
- `pdfOutputPath` - ścieżka do zapisu PDF (jeśli `null`, zwraca bajty)
- `options` - opcje generowania (zobacz [Konfiguracja opcji PDF](#konfiguracja-opcji-pdf))
- `numerKSeF` - numer KSeF (jeśli nie jest w XML)
- `validateInvoice` - czy walidować przed generowaniem (domyślnie: `true`)

**Zwraca:**
- `byte[]?` - bajty PDF jeśli `pdfOutputPath` jest `null`, w przeciwnym razie `null`

**Przykład:**

```csharp
// Zapis do pliku
await service.GeneratePdfFromFileAsync(
    xmlFilePath: "faktura.xml",
    pdfOutputPath: "faktura.pdf"
);

// Zwrócenie bajtów
byte[] pdfBytes = await service.GeneratePdfFromFileAsync(
    xmlFilePath: "faktura.xml",
    pdfOutputPath: null
);
```

##### 2. `GeneratePdfFromXmlAsync` - z XML jako string

```csharp
public async Task<byte[]?> GeneratePdfFromXmlAsync(
    string xmlContent,
    string? pdfOutputPath = null,
    PdfGenerationOptions? options = null,
    string? numerKSeF = null,
    bool validateInvoice = true)
```

**Przykład:**

```csharp
string xmlContent = await File.ReadAllTextAsync("faktura.xml");

await service.GeneratePdfFromXmlAsync(
    xmlContent: xmlContent,
    pdfOutputPath: "faktura.pdf"
);
```

##### 3. `GeneratePdfFromContextAsync` - z kontekstu faktury

```csharp
public async Task<byte[]?> GeneratePdfFromContextAsync(
    InvoiceContext context,
    string? pdfOutputPath = null,
    PdfGenerationOptions? options = null,
    bool validateInvoice = true)
```

**Przykład:**

```csharp
// Najpierw sparsuj XML
var context = await service.ParseInvoiceFromFileAsync("faktura.xml");

// Sprawdź dane
Console.WriteLine($"Faktura: {context.Faktura.Fa.P_2}");
Console.WriteLine($"Kwota: {context.Faktura.Fa.P_15} {context.Faktura.Fa.KodWaluty}");

// Wygeneruj PDF
await service.GeneratePdfFromContextAsync(context, "faktura.pdf");
```

#### Metody walidacji

##### 1. `ValidateInvoiceFromFileAsync` - walidacja z pliku

```csharp
public async Task<ValidationResult> ValidateInvoiceFromFileAsync(
    string xmlFilePath,
    string? numerKSeF = null)
```

**Przykład:**

```csharp
var result = await service.ValidateInvoiceFromFileAsync("faktura.xml");

if (result.IsValid)
{
    Console.WriteLine("Faktura jest poprawna!");
}
else
{
    Console.WriteLine("Błędy walidacji:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}

if (result.Warnings.Any())
{
    Console.WriteLine("Ostrzeżenia:");
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}
```

##### 2. `ValidateInvoiceFromXmlAsync` - walidacja z XML string

```csharp
public async Task<ValidationResult> ValidateInvoiceFromXmlAsync(
    string xmlContent,
    string? numerKSeF = null)
```

#### Metody parsowania

##### 1. `ParseInvoiceFromFileAsync` - parsowanie z pliku

```csharp
public async Task<InvoiceContext> ParseInvoiceFromFileAsync(
    string xmlFilePath,
    string? numerKSeF = null)
```

**Przykład:**

```csharp
var context = await service.ParseInvoiceFromFileAsync("faktura.xml");

// Dostęp do danych faktury
var faktura = context.Faktura;
var metadata = context.Metadata;

Console.WriteLine($"Sprzedawca: {faktura.Podmiot1.DaneIdentyfikacyjne.Nazwa}");
Console.WriteLine($"NIP: {faktura.Podmiot1.DaneIdentyfikacyjne.NIP}");
Console.WriteLine($"Nabywca: {faktura.Podmiot2.DaneIdentyfikacyjne.Nazwa}");
Console.WriteLine($"Data wystawienia: {faktura.Fa.P_1:yyyy-MM-dd}");
Console.WriteLine($"Numer faktury: {faktura.Fa.P_2}");
Console.WriteLine($"Kwota brutto: {faktura.Fa.P_15} {faktura.Fa.KodWaluty}");
Console.WriteLine($"Tryb: {metadata.Tryb}");

// Pozycje faktury
foreach (var wiersz in faktura.Fa.FaWiersz)
{
    Console.WriteLine($"  {wiersz.NrWierszaFa}. {wiersz.P_7} - {wiersz.P_11} (VAT: {wiersz.P_12})");
}
```

##### 2. `ParseInvoiceFromXmlAsync` - parsowanie z XML string

```csharp
public async Task<InvoiceContext> ParseInvoiceFromXmlAsync(
    string xmlContent,
    string? numerKSeF = null)
```

---

## Konfiguracja opcji PDF

### Klasa `PdfGenerationOptions`

```csharp
public class PdfGenerationOptions
{
    // Certyfikat Offline dla podpisów QR II (opcjonalny)
    public X509Certificate2? Certificate { get; set; }

    // Czy używać środowiska produkcyjnego (domyślnie: false - test)
    public bool UseProduction { get; set; } = false;

    // Liczba pikseli na moduł dla kodów QR (min 5, domyślnie 5)
    public int QrPixelsPerModule { get; set; } = 5;

    // Czy dołączyć kod QR I (weryfikacja faktury)
    public bool IncludeQrCode1 { get; set; } = true;

    // Czy dołączyć kod QR II (weryfikacja certyfikatu)
    // Wymaga podania Certificate
    public bool IncludeQrCode2 { get; set; } = true;

    // Tytuł dokumentu PDF
    public string? DocumentTitle { get; set; }

    // Autor dokumentu PDF
    public string? DocumentAuthor { get; set; }
}
```

### Przykłady konfiguracji

#### Podstawowa konfiguracja (środowisko testowe)

```csharp
var options = new PdfGenerationOptions
{
    UseProduction = false,
    QrPixelsPerModule = 5,
    IncludeQrCode1 = true,
    IncludeQrCode2 = false // Bez certyfikatu
};

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
```

#### Konfiguracja produkcyjna z certyfikatem

```csharp
// Wczytaj certyfikat
var cert = new X509Certificate2("cert.pfx", "haslo");

var options = new PdfGenerationOptions
{
    Certificate = cert,
    UseProduction = true,
    IncludeQrCode1 = true,
    IncludeQrCode2 = true,
    DocumentTitle = "Faktura VAT",
    DocumentAuthor = "Moja Firma Sp. z o.o."
};

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
```

#### Tylko KOD QR I (bez certyfikatu)

```csharp
var options = new PdfGenerationOptions
{
    IncludeQrCode1 = true,
    IncludeQrCode2 = false
};

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
```

#### Bez kodów QR (tylko wizualizacja)

```csharp
var options = new PdfGenerationOptions
{
    IncludeQrCode1 = false,
    IncludeQrCode2 = false
};

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
```

---

## Tryby pracy

### 1. Tryb Online (z numerem KSeF)

Używany dla faktur pobranych z systemu KSeF lub gdy numer KSeF jest znany.

**Automatyczne wykrywanie:**
```csharp
// Jeśli XML zawiera numer KSeF w treści, zostanie wykryty automatycznie
await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf");
```

**Ręczne podanie numeru:**
```csharp
// Jeśli numer KSeF nie jest w XML, podaj go ręcznie
await service.GeneratePdfFromFileAsync(
    xmlFilePath: "faktura.xml",
    pdfOutputPath: "faktura.pdf",
    numerKSeF: "6511153259-20251015-010020140418-0D"
);
```

**Wynik w PDF:**
- Napis: "Numer KSeF: 6511153259-20251015-010020140418-0D"
- Etykieta KOD QR I: numer KSeF
- Link weryfikacyjny zawiera numer KSeF

### 2. Tryb Offline (bez numeru KSeF)

Używany dla faktur z systemu ERP, które nie zostały jeszcze wysłane do KSeF.

```csharp
// Nie podawaj numerKSeF - zostanie wykryty tryb Offline
await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf");
```

**Wynik w PDF:**
- Napis: "Tryb: OFFLINE" (pomarańczowy)
- Etykieta KOD QR I: "OFFLINE"
- Link weryfikacyjny dla trybu offline

---

## Obsługa certyfikatów

### Wymagania dla certyfikatu

**WAŻNE:** Do generowania KODU QR II można używać **TYLKO certyfikatu typu Offline** z KSeF!

#### Typ certyfikatu: Offline
- KeyUsage: **Non-Repudiation (40)**
- Przeznaczenie: wystawianie faktur offline i podpisywanie kodów QR
- Algorytmy: RSA (min. 2048 bit) lub ECDSA P-256

#### Typ certyfikatu: Authentication (NIE dla QR II)
- KeyUsage: **Digital Signature (80)**
- Przeznaczenie: **tylko uwierzytelnianie w API**
- **UWAGA:** Ten typ NIE może być używany do KODU QR II!

### Wczytywanie certyfikatu

#### Z pliku PFX/P12 (z hasłem)

```csharp
var cert = new X509Certificate2("moj-certyfikat.pfx", "mojeHaslo");

var options = new PdfGenerationOptions
{
    Certificate = cert,
    IncludeQrCode2 = true
};

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
```

#### Z magazynu certyfikatów Windows

**Opcja A: Po thumbprint (odcisk palca)**

```csharp
using System.Security.Cryptography.X509Certificates;

// Wyszukaj po thumbprint
var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
store.Open(OpenFlags.ReadOnly);

// Usuń spacje z thumbprinta jeśli są
var thumbprint = "AB 12 CD 34 EF 56...".Replace(" ", "");

var certs = store.Certificates
    .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);

store.Close();

if (certs.Count > 0)
{
    var cert = certs[0];

    // Sprawdź czy ma klucz prywatny
    if (!cert.HasPrivateKey)
    {
        Console.WriteLine("Certyfikat nie zawiera klucza prywatnego!");
        return;
    }

    var options = new PdfGenerationOptions
    {
        Certificate = cert,
        IncludeQrCode2 = true
    };

    await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
}
```

**Opcja B: Po subject (CN)**

```csharp
using System.Security.Cryptography.X509Certificates;

// Wyszukaj po subject (np. "Jan Kowalski" lub "CN=Jan Kowalski")
var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
store.Open(OpenFlags.ReadOnly);

var certs = store.Certificates
    .Find(X509FindType.FindBySubjectName, "Jan Kowalski", validOnly: false);

store.Close();

if (certs.Count > 0)
{
    // Jeśli znaleziono wiele, wybierz pierwszy lub daj użytkownikowi wybór
    var cert = certs[0];

    Console.WriteLine($"Znaleziono certyfikat: {cert.Subject}");
    Console.WriteLine($"Thumbprint: {cert.Thumbprint}");
    Console.WriteLine($"Ważny do: {cert.NotAfter:yyyy-MM-dd}");

    var options = new PdfGenerationOptions
    {
        Certificate = cert,
        IncludeQrCode2 = true
    };

    await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
}
```

**Opcja C: Z LocalMachine Store (certyfikaty systemowe)**

```csharp
// Certyfikaty w LocalMachine wymagają uprawnień administratora
var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
store.Open(OpenFlags.ReadOnly);

// ... reszta kodu jak wyżej
```

#### Z pliku PEM (Linux/macOS)

```csharp
// Wczytaj certyfikat i klucz prywatny z PEM
var certPem = await File.ReadAllTextAsync("cert.pem");
var keyPem = await File.ReadAllTextAsync("key.pem");

var cert = X509Certificate2.CreateFromPem(certPem, keyPem);

var options = new PdfGenerationOptions
{
    Certificate = cert,
    IncludeQrCode2 = true
};

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
```

### Weryfikacja typu certyfikatu

Biblioteka automatycznie sprawdza, czy certyfikat ma prawidłowy typ (Non-Repudiation). Jeśli nie, wyświetli ostrzeżenie w logach:

```
WARNING: Certyfikat nie ma rozszerzenia Key Usage ustawionego na Non-Repudiation.
         Upewnij się, że jest to certyfikat typu 'Offline' z KSeF!
```

---

## Przykłady użycia

### Przykład 1: Przetwarzanie wsadowe faktur

```csharp
var xmlFiles = Directory.GetFiles("faktury", "*.xml");

foreach (var xmlFile in xmlFiles)
{
    var pdfFile = Path.ChangeExtension(xmlFile, ".pdf");

    try
    {
        await service.GeneratePdfFromFileAsync(xmlFile, pdfFile);
        Console.WriteLine($"✓ {Path.GetFileName(xmlFile)} -> {Path.GetFileName(pdfFile)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ {Path.GetFileName(xmlFile)}: {ex.Message}");
    }
}
```

### Przykład 2: API Web (ASP.NET Core)

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly InvoicePrinterService _printerService;

    public InvoiceController(InvoicePrinterService printerService)
    {
        _printerService = printerService;
    }

    [HttpPost("generate-pdf")]
    public async Task<IActionResult> GeneratePdf([FromBody] string xmlContent)
    {
        try
        {
            // Generuj PDF jako bajty
            var pdfBytes = await _printerService.GeneratePdfFromXmlAsync(
                xmlContent: xmlContent,
                pdfOutputPath: null, // Zwróć bajty
                validateInvoice: true
            );

            return File(pdfBytes, "application/pdf", "faktura.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Błąd generowania PDF" });
        }
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateInvoice([FromBody] string xmlContent)
    {
        var result = await _printerService.ValidateInvoiceFromXmlAsync(xmlContent);

        return Ok(new
        {
            isValid = result.IsValid,
            errors = result.Errors,
            warnings = result.Warnings
        });
    }
}
```

### Przykład 3: Walidacja przed generowaniem

```csharp
// Najpierw zwaliduj
var validationResult = await service.ValidateInvoiceFromFileAsync("faktura.xml");

if (!validationResult.IsValid)
{
    Console.WriteLine("Faktura zawiera błędy:");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"  ✗ {error}");
    }
    return;
}

if (validationResult.Warnings.Any())
{
    Console.WriteLine("Ostrzeżenia:");
    foreach (var warning in validationResult.Warnings)
    {
        Console.WriteLine($"  ⚠ {warning}");
    }
}

// Jeśli OK, generuj PDF
await service.GeneratePdfFromFileAsync(
    xmlFilePath: "faktura.xml",
    pdfOutputPath: "faktura.pdf",
    validateInvoice: false // Już zwalidowane
);

Console.WriteLine("✓ PDF wygenerowany pomyślnie!");
```

### Przykład 4: Własne opcje dla środowiska produkcyjnego

```csharp
// Wczytaj certyfikat z konfiguracji
var certPath = configuration["KSeF:CertificatePath"];
var certPassword = configuration["KSeF:CertificatePassword"];
var cert = new X509Certificate2(certPath, certPassword);

var options = new PdfGenerationOptions
{
    Certificate = cert,
    UseProduction = true, // Środowisko produkcyjne
    IncludeQrCode1 = true,
    IncludeQrCode2 = true,
    QrPixelsPerModule = 5,
    DocumentTitle = "Faktura VAT",
    DocumentAuthor = "Moja Firma Sp. z o.o."
};

await service.GeneratePdfFromFileAsync(
    xmlFilePath: "faktura.xml",
    pdfOutputPath: "faktura.pdf",
    options: options,
    validateInvoice: true
);
```

### Przykład 5: Integracja z systemem ERP

```csharp
public class ErpInvoiceProcessor
{
    private readonly InvoicePrinterService _printerService;

    public async Task<byte[]> GeneratePdfForErpInvoice(int invoiceId)
    {
        // 1. Pobierz XML z systemu ERP
        var xmlContent = await GetXmlFromErp(invoiceId);

        // 2. Sprawdź czy faktura ma już numer KSeF
        var ksefNumber = await GetKsefNumberIfExists(invoiceId);

        // 3. Wygeneruj PDF
        var pdfBytes = await _printerService.GeneratePdfFromXmlAsync(
            xmlContent: xmlContent,
            pdfOutputPath: null,
            numerKSeF: ksefNumber, // null jeśli offline
            validateInvoice: true
        );

        // 4. Zapisz PDF w systemie ERP
        await SavePdfToErp(invoiceId, pdfBytes);

        return pdfBytes;
    }
}
```

---

## Rozwiązywanie problemów

### Problem 1: Błąd parsowania XML

**Objaw:**
```
System.Xml.XmlException: Unexpected XML element
```

**Rozwiązanie:**
- Sprawdź, czy XML jest zgodny ze schematem FA(3) v1-0E
- Zweryfikuj namespace: `http://crd.gov.pl/wzor/2025/06/25/13775/`
- Użyj walidacji przed parsowaniem:

```csharp
var result = await service.ValidateInvoiceFromFileAsync("faktura.xml");
Console.WriteLine(string.Join("\n", result.Errors));
```

### Problem 2: Nieprawidłowy numer KSeF

**Objaw:**
```
InvalidOperationException: Faktura nie przeszła walidacji: Nieprawidłowy format numeru KSeF
```

**Rozwiązanie:**
- Sprawdź format: `9999999999-RRRRMMDD-XXXXXXXXXXXX-XX` (35 znaków)
- Weryfikuj sumę kontrolną CRC-8
- Jeśli nie masz numeru KSeF, nie podawaj go (tryb Offline)

```csharp
// Tryb offline - nie podawaj numerKSeF
await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", numerKSeF: null);
```

### Problem 3: Błąd certyfikatu

**Objaw:**
```
WARNING: Certyfikat nie ma rozszerzenia Key Usage ustawionego na Non-Repudiation
```

**Rozwiązanie:**
- Upewnij się, że używasz certyfikatu typu **Offline** (nie Authentication)
- Certyfikat musi mieć KeyUsage = Non-Repudiation (40)
- Sprawdź typ certyfikatu w portalu KSeF przed pobraniem

### Problem 4: Błąd walidacji NIP

**Objaw:**
```
InvalidOperationException: Faktura nie przeszła walidacji: Nieprawidłowy NIP sprzedawcy
```

**Rozwiązanie:**
- Sprawdź poprawność numeru NIP (10 cyfr + suma kontrolna)
- Użyj walidatora do sprawdzenia:

```csharp
var result = await service.ValidateInvoiceFromFileAsync("faktura.xml");
foreach (var error in result.Errors)
{
    Console.WriteLine(error);
}
```

### Problem 5: Brak klucza prywatnego w certyfikacie

**Objaw:**
```
CryptographicException: Certyfikat nie zawiera klucza prywatnego
```

**Rozwiązanie:**
- Wczytaj certyfikat z hasłem (PFX):
```csharp
var cert = new X509Certificate2("cert.pfx", "haslo", X509KeyStorageFlags.Exportable);
```
- Upewnij się, że plik zawiera klucz prywatny (nie tylko certyfikat publiczny)

### Problem 6: Plik PDF jest zbyt duży

**Rozwiązanie:**
- Zmniejsz rozmiar kodów QR:
```csharp
var options = new PdfGenerationOptions
{
    QrPixelsPerModule = 5 // Minimum zalecane przez KSeF
};
```
- Wyłącz KOD QR II jeśli niepotrzebny:
```csharp
var options = new PdfGenerationOptions
{
    IncludeQrCode2 = false
};
```

---

## FAQ

### 1. Czy mogę używać biblioteki bez certyfikatu?

**Tak!** Jeśli nie potrzebujesz KODU QR II (weryfikacja certyfikatu), możesz używać biblioteki bez certyfikatu:

```csharp
var options = new PdfGenerationOptions
{
    IncludeQrCode2 = false // Bez KOD QR II
};

await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf", options);
```

PDF będzie zawierał tylko KOD QR I (weryfikacja faktury).

### 2. Czy biblioteka obsługuje FA(2)?

**Nie.** Biblioteka obsługuje tylko schemat **FA(3) v1-0E**. Schemat FA(2) jest starszy i nie jest wspierany.

### 3. Jak sprawdzić czy moja faktura jest zgodna ze schematem?

Użyj metody walidacji:

```csharp
var result = await service.ValidateInvoiceFromFileAsync("faktura.xml");

if (result.IsValid)
{
    Console.WriteLine("✓ Faktura zgodna ze schematem FA(3)");
}
else
{
    Console.WriteLine("✗ Błędy:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### 4. Czy mogę wyłączyć walidację?

**Tak**, ale nie jest to zalecane:

```csharp
await service.GeneratePdfFromFileAsync(
    xmlFilePath: "faktura.xml",
    pdfOutputPath: "faktura.pdf",
    validateInvoice: false // Wyłącz walidację
);
```

### 5. Czy biblioteka działa na Linux i macOS?

**Tak!** Biblioteka jest w pełni cross-platformowa (.NET 9). Testowana na:
- Windows 10/11
- Linux (Ubuntu, Debian)
- macOS

### 6. Jak uzyskać certyfikat Offline z KSeF?

1. Zaloguj się do portalu KSeF: https://ksef-test.mf.gov.pl (test) lub https://ksef.mf.gov.pl (produkcja)
2. Przejdź do sekcji "Certyfikaty"
3. Wygeneruj CSR dla certyfikatu typu **Offline**
4. Pobierz certyfikat wraz z kluczem prywatnym (PFX)
5. Wczytaj do biblioteki:

```csharp
var cert = new X509Certificate2("cert.pfx", "haslo");
```

### 7. Czy biblioteka cache'uje dane?

**Nie.** Każde wywołanie metody parsuje XML od nowa. Jeśli chcesz cache'ować, możesz użyć `ParseInvoiceFromFileAsync` i przechowywać `InvoiceContext`:

```csharp
// Parsuj raz
var context = await service.ParseInvoiceFromFileAsync("faktura.xml");

// Używaj wielokrotnie
await service.GeneratePdfFromContextAsync(context, "faktura1.pdf", options1);
await service.GeneratePdfFromContextAsync(context, "faktura2.pdf", options2);
```

### 8. Czy mogę dostosować wygląd PDF?

Obecnie nie ma API do dostosowania wyglądu. PDF jest generowany zgodnie ze standardowym layoutem faktur KSeF. W przyszłych wersjach planowane jest dodanie opcji customizacji.

### 9. Jak zintegrować z Dependency Injection w ASP.NET Core?

```csharp
// Program.cs
builder.Services.AddSingleton<IXmlInvoiceParser, XmlInvoiceParser>();
builder.Services.AddSingleton<InvoiceValidator>();
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<IVerificationLinkService, VerificationLinkService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddSingleton<IPdfGeneratorService, InvoicePdfGenerator>();
builder.Services.AddSingleton<InvoicePrinterService>();

// W kontrolerze
public class InvoiceController : ControllerBase
{
    private readonly InvoicePrinterService _service;

    public InvoiceController(InvoicePrinterService service)
    {
        _service = service;
    }
}
```

### 10. Czy biblioteka loguje wrażliwe dane?

**Nie.** Biblioteka loguje tylko metadane (numer faktury, NIP, tryb) na poziomie Information. Pełne dane XML nie są logowane. Możesz kontrolować poziom logowania:

```csharp
builder.Services.AddLogging(config =>
{
    config.SetMinimumLevel(LogLevel.Warning); // Tylko błędy i ostrzeżenia
});
```

---

## Wsparcie

### Zgłaszanie błędów

Jeśli napotkasz błąd, zgłoś issue z informacjami:
- Wersja biblioteki
- Wersja .NET
- System operacyjny
- Przykładowy XML (zanonimizowany)
- Stack trace błędu

### Dokumentacja KSeF

- Portal KSeF Test: https://ksef-test.mf.gov.pl
- Portal KSeF Produkcja: https://ksef.mf.gov.pl
- Dokumentacja API: https://ksef-test.mf.gov.pl/docs/v2/

### Repozytorium

- GitHub: (dodaj link do repozytorium)

---

**Wersja dokumentu:** 1.0
**Data:** 2025-10-29
**Schemat:** FA(3) v1-0E
