using KSeFPrinter;
using KSeFPrinter.API.Services;
using KSeFPrinter.Interfaces;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.License;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// === Konfiguracja serwisów ===

// Logowanie
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Kontrolery
builder.Services.AddControllers();

// KSeFPrinter serwisy (Singleton - bezstanowe, bezpieczne do reużycia)
builder.Services.AddSingleton<IXmlInvoiceParser>(provider =>
    new XmlInvoiceParser(provider.GetRequiredService<ILogger<XmlInvoiceParser>>()));

builder.Services.AddSingleton<InvoiceValidator>(provider =>
    new InvoiceValidator(provider.GetRequiredService<ILogger<InvoiceValidator>>()));

builder.Services.AddSingleton<CryptographyService>(provider =>
    new CryptographyService(provider.GetRequiredService<ILogger<CryptographyService>>()));

builder.Services.AddSingleton<VerificationLinkService>(provider =>
    new VerificationLinkService(
        provider.GetRequiredService<CryptographyService>(),
        provider.GetRequiredService<ILogger<VerificationLinkService>>()));

builder.Services.AddSingleton<QrCodeService>(provider =>
    new QrCodeService(provider.GetRequiredService<ILogger<QrCodeService>>()));

builder.Services.AddSingleton<IPdfGeneratorService>(provider =>
    new InvoicePdfGenerator(
        provider.GetRequiredService<VerificationLinkService>(),
        provider.GetRequiredService<QrCodeService>(),
        provider.GetRequiredService<ILogger<InvoicePdfGenerator>>()));

builder.Services.AddSingleton<InvoicePrinterService>();

// API serwisy
builder.Services.AddSingleton<CertificateService>();

// Licencjonowanie
builder.Services.AddSingleton<ILicenseValidator>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<LicenseValidator>>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var licenseFilePath = configuration["License:FilePath"];
    return new LicenseValidator(licenseFilePath, logger);
});

// TODO: Wariant B - KSeF Connector integracja
// builder.Services.AddHttpClient<IKSeFConnectorService, KSeFConnectorService>(client =>
// {
//     var baseUrl = builder.Configuration["KSeFConnector:BaseUrl"] ?? "http://localhost:5000";
//     client.BaseAddress = new Uri(baseUrl);
//     client.Timeout = TimeSpan.FromSeconds(30);
// });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KSeF Printer API",
        Version = "v1",
        Description = @"API do generowania wydruków PDF z faktur XML w formacie KSeF FA(3).

## Funkcjonalności (Wariant A):
- **Walidacja** faktury XML
- **Parsowanie** faktury do JSON
- **Generowanie PDF** z kodami QR

## Obsługiwane tryby:
- **Online** - faktury z numerem KSeF
- **Offline** - faktury bez numeru KSeF

## Certyfikaty (opcjonalnie dla KODU QR II):
- Z Windows Certificate Store (thumbprint/subject)
- Upload pliku PFX (TODO)

## Przyszłe funkcjonalności (Wariant B):
- Integracja z KSeF Connector API
- Pobieranie faktur bezpośrednio z KSeF
- Wysyłanie faktur do KSeF + generowanie PDF
- Wsadowe przetwarzanie",
        Contact = new OpenApiContact
        {
            Name = "KSeF Printer",
            Url = new Uri("https://github.com/")
        }
    });

    // Załącz komentarze XML z kodu (jeśli dostępne)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// CORS (jeśli potrzebne dla frontendu)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// === Walidacja licencji przy starcie ===
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var licenseValidator = app.Services.GetRequiredService<ILicenseValidator>();

logger.LogInformation("=== Sprawdzanie licencji ===");

try
{
    var validationResult = await licenseValidator.ValidateLicenseAsync();

    if (!validationResult.IsValid)
    {
        logger.LogCritical("❌ BŁĄD LICENCJI: {Error}", validationResult.ErrorMessage);
        logger.LogCritical("Aplikacja nie może zostać uruchomiona bez ważnej licencji.");
        logger.LogCritical("Skontaktuj się z dostawcą w celu uzyskania licencji dla KSeF Printer.");

        // Zatrzymaj aplikację
        Environment.Exit(1);
        return;
    }

    logger.LogInformation("✅ Licencja ważna");
    logger.LogInformation("   Właściciel: {IssuedTo}", validationResult.License!.IssuedTo);
    logger.LogInformation("   Ważna do: {ExpiryDate:yyyy-MM-dd}", validationResult.License.ExpiryDate);
    logger.LogInformation("   Dozwolone NIP-y: {Count}", validationResult.License.AllowedNips.Count);
}
catch (Exception ex)
{
    logger.LogCritical(ex, "❌ BŁĄD podczas walidacji licencji");
    logger.LogCritical("Aplikacja nie może zostać uruchomiona bez ważnej licencji.");
    Environment.Exit(1);
    return;
}

// === Konfiguracja middleware ===

// Swagger w development i production
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:EnableInProduction"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "KSeF Printer API v1");
        options.RoutePrefix = string.Empty; // Swagger UI jako główna strona
    });
}

app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Informacja startowa
logger.LogInformation("=== KSeF Printer API ===");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

// Wyświetl skonfigurowane URL-e
var addresses = app.Urls;
if (addresses.Any())
{
    foreach (var address in addresses)
    {
        logger.LogInformation("Listening on: {Address}", address);
        logger.LogInformation("Swagger UI: {Url}", address);
    }
}
else
{
    logger.LogInformation("Swagger UI: włączony w konfiguracji");
}

app.Run();

// Make Program class public for testing with WebApplicationFactory
public partial class Program { }
