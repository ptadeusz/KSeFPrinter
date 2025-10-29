using System.CommandLine;
using System.CommandLine.Invocation;
using System.Security.Cryptography.X509Certificates;
using KSeFPrinter;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging;

// === Konfiguracja komend CLI ===

var rootCommand = new RootCommand("KSeF PDF Generator - Generuje wydruki PDF z faktur XML w formacie KSeF FA(3)");

// Argumenty i opcje wspólne
var inputArgument = new Argument<string[]>(
    name: "input",
    description: "Pliki XML lub katalog do przetworzenia. Przykłady: faktura.xml, *.xml, faktury/")
{
    Arity = ArgumentArity.OneOrMore
};

var outputOption = new Option<string?>(
    aliases: new[] { "-o", "--output" },
    description: "Ścieżka do pliku PDF wyjściowego (tylko dla pojedynczego pliku)")
{
    IsRequired = false
};

var certOption = new Option<string?>(
    aliases: new[] { "--cert" },
    description: "Ścieżka do certyfikatu KSeF typu Offline (PFX/P12) dla KODU QR II")
{
    IsRequired = false
};

var certPasswordOption = new Option<string?>(
    aliases: new[] { "--cert-password" },
    description: "Hasło do certyfikatu")
{
    IsRequired = false
};

var productionOption = new Option<bool>(
    aliases: new[] { "--production" },
    description: "Użyj środowiska produkcyjnego KSeF (domyślnie: test)",
    getDefaultValue: () => false);

var noValidateOption = new Option<bool>(
    aliases: new[] { "--no-validate" },
    description: "Pomiń walidację faktury przed generowaniem PDF",
    getDefaultValue: () => false);

var ksefNumberOption = new Option<string?>(
    aliases: new[] { "--ksef-number" },
    description: "Numer KSeF (jeśli nie jest w XML)")
{
    IsRequired = false
};

var verboseOption = new Option<bool>(
    aliases: new[] { "-v", "--verbose" },
    description: "Szczegółowe logowanie",
    getDefaultValue: () => false);

var watchOption = new Option<bool>(
    aliases: new[] { "-w", "--watch" },
    description: "Tryb obserwowania - automatycznie przetwarza nowe pliki XML",
    getDefaultValue: () => false);

// Dodaj opcje do głównej komendy
rootCommand.AddArgument(inputArgument);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(certOption);
rootCommand.AddOption(certPasswordOption);
rootCommand.AddOption(productionOption);
rootCommand.AddOption(noValidateOption);
rootCommand.AddOption(ksefNumberOption);
rootCommand.AddOption(verboseOption);
rootCommand.AddOption(watchOption);

// === Handler komendy ===

rootCommand.SetHandler(async (InvocationContext context) =>
{
    // Pobierz wartości z kontekstu
    var input = context.ParseResult.GetValueForArgument(inputArgument);
    var output = context.ParseResult.GetValueForOption(outputOption);
    var certPath = context.ParseResult.GetValueForOption(certOption);
    var certPassword = context.ParseResult.GetValueForOption(certPasswordOption);
    var production = context.ParseResult.GetValueForOption(productionOption);
    var noValidate = context.ParseResult.GetValueForOption(noValidateOption);
    var ksefNumber = context.ParseResult.GetValueForOption(ksefNumberOption);
    var verbose = context.ParseResult.GetValueForOption(verboseOption);
    var watch = context.ParseResult.GetValueForOption(watchOption);

    // Konfiguracja logowania
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
    });

    var logger = loggerFactory.CreateLogger<Program>();

    try
    {
        // Inicjalizacja serwisów
        var parser = new XmlInvoiceParser(loggerFactory.CreateLogger<XmlInvoiceParser>());
        var validator = new InvoiceValidator(loggerFactory.CreateLogger<InvoiceValidator>());
        var cryptoService = new CryptographyService(loggerFactory.CreateLogger<CryptographyService>());
        var linkService = new VerificationLinkService(cryptoService, loggerFactory.CreateLogger<VerificationLinkService>());
        var qrService = new QrCodeService(loggerFactory.CreateLogger<QrCodeService>());
        var pdfGenerator = new InvoicePdfGenerator(linkService, qrService, loggerFactory.CreateLogger<InvoicePdfGenerator>());
        var service = new InvoicePrinterService(parser, validator, pdfGenerator, loggerFactory.CreateLogger<InvoicePrinterService>());

        // Wczytaj certyfikat jeśli podany
        X509Certificate2? certificate = null;
        if (!string.IsNullOrEmpty(certPath))
        {
            logger.LogInformation("Wczytywanie certyfikatu z: {CertPath}", certPath);
            certificate = string.IsNullOrEmpty(certPassword)
                ? new X509Certificate2(certPath)
                : new X509Certificate2(certPath, certPassword);
            logger.LogInformation("Certyfikat wczytany: {Subject}", certificate.Subject);
        }

        // Opcje generowania PDF
        var options = new PdfGenerationOptions
        {
            Certificate = certificate,
            UseProduction = production,
            IncludeQrCode1 = true,
            IncludeQrCode2 = certificate != null
        };

        // Zbierz pliki do przetworzenia
        var xmlFiles = CollectXmlFiles(input);

        if (xmlFiles.Count == 0)
        {
            logger.LogError("Nie znaleziono żadnych plików XML do przetworzenia");
            Environment.Exit(1);
            return;
        }

        if (watch)
        {
            // === TRYB WATCH ===
            await RunWatchMode(xmlFiles, service, options, !noValidate, ksefNumber, logger);
        }
        else if (xmlFiles.Count == 1 && !string.IsNullOrEmpty(output))
        {
            // === TRYB POJEDYNCZEGO PLIKU Z WŁASNĄ ŚCIEŻKĄ ===
            await ProcessSingleFile(xmlFiles[0], output, service, options, !noValidate, ksefNumber, logger);
        }
        else
        {
            // === TRYB WSADOWY ===
            await ProcessBatch(xmlFiles, service, options, !noValidate, ksefNumber, logger);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Błąd krytyczny: {Message}", ex.Message);
        Environment.Exit(1);
    }
});

return await rootCommand.InvokeAsync(args);

// === Funkcje pomocnicze ===

static List<string> CollectXmlFiles(string[] input)
{
    var files = new List<string>();

    foreach (var item in input)
    {
        if (Directory.Exists(item))
        {
            // Katalog - znajdź wszystkie XMLe
            files.AddRange(Directory.GetFiles(item, "*.xml", SearchOption.TopDirectoryOnly));
        }
        else if (item.Contains('*') || item.Contains('?'))
        {
            // Wildcard - rozwiń
            var directory = Path.GetDirectoryName(item) ?? ".";
            var pattern = Path.GetFileName(item);
            files.AddRange(Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly));
        }
        else if (File.Exists(item))
        {
            // Pojedynczy plik
            files.Add(item);
        }
    }

    return files.Distinct().OrderBy(f => f).ToList();
}

static async Task ProcessSingleFile(
    string xmlPath,
    string pdfPath,
    InvoicePrinterService service,
    PdfGenerationOptions options,
    bool validate,
    string? ksefNumber,
    ILogger logger)
{
    logger.LogInformation("=== Przetwarzanie pojedynczego pliku ===");
    logger.LogInformation("XML: {XmlPath}", xmlPath);
    logger.LogInformation("PDF: {PdfPath}", pdfPath);

    try
    {
        await service.GeneratePdfFromFileAsync(
            xmlFilePath: xmlPath,
            pdfOutputPath: pdfPath,
            options: options,
            numerKSeF: ksefNumber,
            validateInvoice: validate
        );

        var fileInfo = new FileInfo(pdfPath);
        logger.LogInformation("✓ PDF wygenerowany pomyślnie! Rozmiar: {Size:N0} bajtów", fileInfo.Length);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "✗ Błąd: {Message}", ex.Message);
        throw;
    }
}

static async Task ProcessBatch(
    List<string> xmlFiles,
    InvoicePrinterService service,
    PdfGenerationOptions options,
    bool validate,
    string? ksefNumber,
    ILogger logger)
{
    logger.LogInformation("=== Przetwarzanie wsadowe ===");
    logger.LogInformation("Znaleziono {Count} plików XML", xmlFiles.Count);

    int successCount = 0;
    int errorCount = 0;

    foreach (var xmlPath in xmlFiles)
    {
        var pdfPath = Path.ChangeExtension(xmlPath, ".pdf");

        try
        {
            logger.LogInformation("Przetwarzanie: {FileName}...", Path.GetFileName(xmlPath));

            await service.GeneratePdfFromFileAsync(
                xmlFilePath: xmlPath,
                pdfOutputPath: pdfPath,
                options: options,
                numerKSeF: ksefNumber,
                validateInvoice: validate
            );

            var fileInfo = new FileInfo(pdfPath);
            logger.LogInformation("  ✓ {PdfFile} ({Size:N0} bajtów)", Path.GetFileName(pdfPath), fileInfo.Length);
            successCount++;
        }
        catch (Exception ex)
        {
            logger.LogError("  ✗ Błąd: {Message}", ex.Message);
            errorCount++;
        }
    }

    logger.LogInformation("");
    logger.LogInformation("=== Podsumowanie ===");
    logger.LogInformation("Przetworzone pomyślnie: {Success}", successCount);
    if (errorCount > 0)
    {
        logger.LogWarning("Błędy: {Errors}", errorCount);
    }
}

static async Task RunWatchMode(
    List<string> initialFiles,
    InvoicePrinterService service,
    PdfGenerationOptions options,
    bool validate,
    string? ksefNumber,
    ILogger logger)
{
    // Określ katalogi do obserwowania
    var directories = initialFiles
        .Select(f => Path.GetDirectoryName(Path.GetFullPath(f)) ?? ".")
        .Distinct()
        .ToList();

    if (directories.Count == 0)
    {
        directories.Add(".");
    }

    logger.LogInformation("=== Tryb obserwowania (watch mode) ===");
    logger.LogInformation("Obserwowane katalogi:");
    foreach (var dir in directories)
    {
        logger.LogInformation("  - {Directory}", Path.GetFullPath(dir));
    }
    logger.LogInformation("");
    logger.LogInformation("Oczekiwanie na nowe pliki XML... (Ctrl+C aby zakończyć)");
    logger.LogInformation("");

    // Przetworz istniejące pliki najpierw
    await ProcessBatch(initialFiles, service, options, validate, ksefNumber, logger);

    // Przechowuj przetworzone pliki
    var processedFiles = new HashSet<string>(initialFiles.Select(f => Path.GetFullPath(f)));

    // Utwórz FileSystemWatcher dla każdego katalogu
    var watchers = new List<FileSystemWatcher>();

    foreach (var directory in directories)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            Filter = "*.xml",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Created += async (sender, e) =>
        {
            var fullPath = Path.GetFullPath(e.FullPath);

            // Sprawdź czy już przetworzony
            lock (processedFiles)
            {
                if (processedFiles.Contains(fullPath))
                    return;
                processedFiles.Add(fullPath);
            }

            // Czekaj chwilę aby plik był w pełni zapisany
            await Task.Delay(500);

            logger.LogInformation("Wykryto nowy plik: {FileName}", Path.GetFileName(e.FullPath));

            var pdfPath = Path.ChangeExtension(e.FullPath, ".pdf");

            try
            {
                await service.GeneratePdfFromFileAsync(
                    xmlFilePath: e.FullPath,
                    pdfOutputPath: pdfPath,
                    options: options,
                    numerKSeF: ksefNumber,
                    validateInvoice: validate
                );

                var fileInfo = new FileInfo(pdfPath);
                logger.LogInformation("  ✓ {PdfFile} ({Size:N0} bajtów)", Path.GetFileName(pdfPath), fileInfo.Length);
            }
            catch (Exception ex)
            {
                logger.LogError("  ✗ Błąd: {Message}", ex.Message);
            }
        };

        watchers.Add(watcher);
    }

    // Czekaj w nieskończoność (Ctrl+C aby zakończyć)
    await Task.Delay(Timeout.Infinite);
}
