using System.Reflection;
using System.Threading.RateLimiting;
using FluentValidation;
using KSeFPrinter;
using KSeFPrinter.API.Filters;
using KSeFPrinter.API.Middleware;
using KSeFPrinter.API.Services;
using KSeFPrinter.Interfaces;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.License;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// === Serilog Configuration (early initialization) ===
var baseDirectory = AppContext.BaseDirectory;

// Logi w folderze instalacji (gdzie usługa Windows ma pełne uprawnienia)
// Dla usługi Windows: C:\Program Files\KSeF Printer\API\logs\
var logsDirectory = Path.Combine(baseDirectory, "logs");

// Upewnij się że folder istnieje
try
{
    Directory.CreateDirectory(logsDirectory);
}
catch (Exception ex)
{
    // Fallback: jeśli nie można utworzyć folderu, logi będą tylko w Console i EventLog
    Console.WriteLine($"⚠️ OSTRZEŻENIE: Nie można utworzyć folderu logów: {logsDirectory}");
    Console.WriteLine($"   Błąd: {ex.Message}");
    Console.WriteLine($"   Logi będą zapisywane tylko do konsoli i Windows Event Log");
    logsDirectory = null; // Wyłącz file logging
}

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

// Dodaj file sink tylko jeśli udało się utworzyć folder logów
if (logsDirectory != null)
{
    loggerConfig.WriteTo.File(
        path: Path.Combine(logsDirectory, "ksef-printer-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
}

// Event Log - zawsze włączony (wymaga uprawnień administratora przy pierwszym uruchomieniu)
try
{
    loggerConfig.WriteTo.EventLog(
        source: "KSeF Printer API",
        logName: "Application",
        restrictedToMinimumLevel: LogEventLevel.Warning);
}
catch
{
    // Ignoruj błędy Event Log (może nie być uprawnień)
    Console.WriteLine("⚠️ OSTRZEŻENIE: Nie można utworzyć źródła Event Log (wymaga uprawnień administratora)");
}

Log.Logger = loggerConfig.CreateLogger();

// Odczytaj wersję aplikacji
var assembly = Assembly.GetExecutingAssembly();
var version = assembly.GetName().Version?.ToString() ?? "unknown";
var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

Log.Information("=== Uruchamianie KSeF Printer API ===");
Log.Information("Wersja: {Version} (Assembly: {AssemblyVersion})", informationalVersion, version);
Log.Information(".NET Runtime: {Runtime}", Environment.Version);

try
{
    var builder = WebApplication.CreateBuilder(args);

    // === Windows Service support ===
    builder.Host.UseWindowsService();

    // === Serilog jako domyślny logger ===
    builder.Host.UseSerilog();

    // === Konfiguracja serwisów ===

    // Kontrolery z automatyczną walidacją
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationActionFilter>();
    });

    // FluentValidation - Automatyczna walidacja requestów
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddScoped<ValidationActionFilter>();

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

    // Konfiguracja certyfikatów
    builder.Services.Configure<KSeFPrinter.API.Models.CertificatesConfiguration>(
        builder.Configuration.GetSection("Certificates"));

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

    // CORS - Bezpieczna konfiguracja
    var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("KSeFPrinterPolicy", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Development: Allow all dla testów lokalnych
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // Production: Tylko specific origins z konfiguracji
                if (corsAllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsAllowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    // Brak konfiguracji - disable CORS w production
                    policy.WithOrigins() // Pusty = brak dostępu
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            }
        });
    });

    // Rate Limiting - Ochrona przed abuse
    builder.Services.AddRateLimiter(options =>
    {
        // Policy globalny
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            // Partycjonuj po IP address lub Host
            var identifier = context.Connection.RemoteIpAddress?.ToString() ??
                           context.Request.Headers.Host.ToString();

            return RateLimitPartition.GetFixedWindowLimiter(identifier, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100),
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 10)
                });
        });

        // Odpowiedź gdy limit przekroczony
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            await context.HttpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                statusCode = 429,
                message = "Too many requests. Please try again later.",
                retryAfter = 60,
                timestamp = DateTime.UtcNow,
                path = context.HttpContext.Request.Path.ToString()
            }), cancellationToken);
        };
    });

    var app = builder.Build();

    // === Walidacja licencji przy starcie ===
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var licenseValidator = app.Services.GetRequiredService<ILicenseValidator>();

    Log.Information("=== Sprawdzanie licencji ===");

    try
    {
        var validationResult = await licenseValidator.ValidateLicenseAsync();

        if (!validationResult.IsValid)
        {
            Log.Fatal("❌ BŁĄD LICENCJI: {Error}", validationResult.ErrorMessage);
            Log.Fatal("Aplikacja nie może zostać uruchomiona bez ważnej licencji.");
            Log.Fatal("Skontaktuj się z dostawcą w celu uzyskania licencji dla KSeF Printer.");

            // Zatrzymaj aplikację
            Environment.Exit(1);
            return;
        }

        Log.Information("✅ Licencja ważna");
        Log.Information("   Właściciel: {IssuedTo}", validationResult.License!.IssuedTo);
        Log.Information("   Ważna do: {ExpiryDate:yyyy-MM-dd}", validationResult.License.ExpiryDate);
        Log.Information("   Dozwolone NIP-y: {Count}", validationResult.License.AllowedNips.Count);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "❌ BŁĄD podczas walidacji licencji");
        Log.Fatal("Aplikacja nie może zostać uruchomiona bez ważnej licencji.");
        Environment.Exit(1);
        return;
    }

    // === Wczytywanie certyfikatów z konfiguracji ===
    var certificateService = app.Services.GetRequiredService<CertificateService>();
    var certificatesConfig = app.Configuration.GetSection("Certificates").Get<KSeFPrinter.API.Models.CertificatesConfiguration>();

    if (certificatesConfig != null)
    {
        try
        {
            await certificateService.LoadCertificatesFromConfigurationAsync(certificatesConfig);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "⚠️ Błąd wczytywania certyfikatów - aplikacja będzie działać bez certyfikatów");
            Log.Warning("   KOD QR II nie będzie generowany (wymaga certyfikatu)");
        }
    }
    else
    {
        Log.Information("Brak konfiguracji certyfikatów - aplikacja będzie działać bez certyfikatów");
    }

    // === Konfiguracja middleware ===

    // Global Exception Handler - musi być PIERWSZY
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Serilog HTTP Request Logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            if (!string.IsNullOrEmpty(userAgent))
            {
                diagnosticContext.Set("UserAgent", userAgent);
            }
        };
    });

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

    // CORS - Używaj bezpiecznej policy
    app.UseCors("KSeFPrinterPolicy");

    // Rate Limiting - Musi być PRZED UseAuthorization
    app.UseRateLimiter();

    app.UseAuthorization();

    app.MapControllers();

    // Informacja startowa
    Log.Information("=== KSeF Printer API ===");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

    // Wyświetl skonfigurowane URL-e
    var addresses = app.Urls;
    if (addresses.Any())
    {
        foreach (var address in addresses)
        {
            Log.Information("Listening on: {Address}", address);
            Log.Information("Swagger UI: {Url}", address);
        }
    }
    else
    {
        Log.Information("Swagger UI: włączony w konfiguracji");
    }

    Log.Information("Aplikacja uruchomiona pomyślnie");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplikacja zatrzymana z powodu błędu");
}
finally
{
    Log.Information("Zamykanie aplikacji...");
    await Log.CloseAndFlushAsync();
}

// Make Program class public for testing with WebApplicationFactory
public partial class Program { }
