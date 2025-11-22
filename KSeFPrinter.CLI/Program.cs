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
using System.Reflection;

// === Sprawdź --version przed innymi argumentami ===
if (args.Length > 0 && args[0] == "--version")
{
    var assembly = Assembly.GetExecutingAssembly();
    var version = assembly.GetName().Version?.ToString() ?? "unknown";
    var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

    Console.WriteLine($"ksef-pdf version {informationalVersion}");
    Console.WriteLine($"Assembly version: {version}");
    Console.WriteLine($".NET Runtime: {Environment.Version}");
    return 0;
}

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
    description: "Hasło do certyfikatu (tylko dla pliku PFX)")
{
    IsRequired = false
};

var certThumbprintOption = new Option<string?>(
    aliases: new[] { "--cert-thumbprint" },
    description: "Thumbprint (odcisk palca) certyfikatu z Windows Certificate Store")
{
    IsRequired = false
};

var certSubjectOption = new Option<string?>(
    aliases: new[] { "--cert-subject" },
    description: "Subject (CN) certyfikatu z Windows Certificate Store")
{
    IsRequired = false
};

var certStoreNameOption = new Option<string>(
    aliases: new[] { "--cert-store-name" },
    description: "Nazwa Store w Windows (My, Root, CA, itp.)",
    getDefaultValue: () => "My");

var certStoreLocationOption = new Option<string>(
    aliases: new[] { "--cert-store-location" },
    description: "Lokalizacja Store (CurrentUser lub LocalMachine)",
    getDefaultValue: () => "CurrentUser");

// Azure Key Vault options
var azureKeyVaultUrlOption = new Option<string?>(
    aliases: new[] { "--azure-keyvault-url" },
    description: "URL Azure Key Vault (np. https://myvault.vault.azure.net/)")
{
    IsRequired = false
};

var azureKeyVaultCertOption = new Option<string?>(
    aliases: new[] { "--azure-keyvault-cert" },
    description: "Nazwa certyfikatu w Azure Key Vault")
{
    IsRequired = false
};

var azureKeyVaultVersionOption = new Option<string?>(
    aliases: new[] { "--azure-keyvault-version" },
    description: "Wersja certyfikatu w Key Vault (opcjonalna)")
{
    IsRequired = false
};

var azureAuthTypeOption = new Option<string>(
    aliases: new[] { "--azure-auth-type" },
    description: "Typ uwierzytelniania Azure (DefaultAzureCredential, ManagedIdentity, ClientSecret, EnvironmentCredential, AzureCliCredential)",
    getDefaultValue: () => "DefaultAzureCredential");

var azureTenantIdOption = new Option<string?>(
    aliases: new[] { "--azure-tenant-id" },
    description: "Azure Tenant ID (dla ClientSecret)")
{
    IsRequired = false
};

var azureClientIdOption = new Option<string?>(
    aliases: new[] { "--azure-client-id" },
    description: "Azure Client ID (dla ClientSecret lub ManagedIdentity)")
{
    IsRequired = false
};

var azureClientSecretOption = new Option<string?>(
    aliases: new[] { "--azure-client-secret" },
    description: "Azure Client Secret (dla ClientSecret)")
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

var ksefFromFilenameOption = new Option<bool>(
    aliases: new[] { "--ksef-from-filename" },
    description: "Automatycznie wykryj numer KSeF z nazwy pliku (format: nazwa_NUMER.xml)",
    getDefaultValue: () => false);

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
rootCommand.AddOption(certThumbprintOption);
rootCommand.AddOption(certSubjectOption);
rootCommand.AddOption(certStoreNameOption);
rootCommand.AddOption(certStoreLocationOption);
rootCommand.AddOption(azureKeyVaultUrlOption);
rootCommand.AddOption(azureKeyVaultCertOption);
rootCommand.AddOption(azureKeyVaultVersionOption);
rootCommand.AddOption(azureAuthTypeOption);
rootCommand.AddOption(azureTenantIdOption);
rootCommand.AddOption(azureClientIdOption);
rootCommand.AddOption(azureClientSecretOption);
rootCommand.AddOption(productionOption);
rootCommand.AddOption(noValidateOption);
rootCommand.AddOption(ksefNumberOption);
rootCommand.AddOption(ksefFromFilenameOption);
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
    var certThumbprint = context.ParseResult.GetValueForOption(certThumbprintOption);
    var certSubject = context.ParseResult.GetValueForOption(certSubjectOption);
    var certStoreName = context.ParseResult.GetValueForOption(certStoreNameOption);
    var certStoreLocation = context.ParseResult.GetValueForOption(certStoreLocationOption);
    var azureKeyVaultUrl = context.ParseResult.GetValueForOption(azureKeyVaultUrlOption);
    var azureKeyVaultCert = context.ParseResult.GetValueForOption(azureKeyVaultCertOption);
    var azureKeyVaultVersion = context.ParseResult.GetValueForOption(azureKeyVaultVersionOption);
    var azureAuthType = context.ParseResult.GetValueForOption(azureAuthTypeOption);
    var azureTenantId = context.ParseResult.GetValueForOption(azureTenantIdOption);
    var azureClientId = context.ParseResult.GetValueForOption(azureClientIdOption);
    var azureClientSecret = context.ParseResult.GetValueForOption(azureClientSecretOption);
    var production = context.ParseResult.GetValueForOption(productionOption);
    var noValidate = context.ParseResult.GetValueForOption(noValidateOption);
    var ksefNumber = context.ParseResult.GetValueForOption(ksefNumberOption);
    var ksefFromFilename = context.ParseResult.GetValueForOption(ksefFromFilenameOption);
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

        // Sprawdź, czy podano więcej niż jedną metodę wczytywania certyfikatu
        var hasKeyVault = !string.IsNullOrEmpty(azureKeyVaultUrl) && !string.IsNullOrEmpty(azureKeyVaultCert);
        var certMethodsCount = new[] { certPath, certThumbprint, certSubject }
            .Count(x => !string.IsNullOrEmpty(x));

        if (hasKeyVault && certMethodsCount > 0)
        {
            logger.LogError("Błąd: Można podać tylko jedną metodę wczytywania certyfikatu: --cert, --cert-thumbprint, --cert-subject lub Azure Key Vault");
            Environment.Exit(1);
            return;
        }

        if (certMethodsCount > 1)
        {
            logger.LogError("Błąd: Można podać tylko jedną metodę wczytywania certyfikatu: --cert, --cert-thumbprint lub --cert-subject");
            Environment.Exit(1);
            return;
        }

        if (hasKeyVault)
        {
            // Wczytaj z Azure Key Vault
            logger.LogInformation("Wczytywanie certyfikatu z Azure Key Vault: {Vault}/{Cert}", azureKeyVaultUrl, azureKeyVaultCert);

            var keyVaultOptions = new KSeFPrinter.Models.Common.AzureKeyVaultOptions
            {
                KeyVaultUrl = azureKeyVaultUrl,
                CertificateName = azureKeyVaultCert,
                CertificateVersion = azureKeyVaultVersion,
                AuthenticationType = Enum.TryParse<KSeFPrinter.Models.Common.AzureAuthenticationType>(azureAuthType, out var authType)
                    ? authType
                    : KSeFPrinter.Models.Common.AzureAuthenticationType.DefaultAzureCredential,
                TenantId = azureTenantId,
                ClientId = azureClientId,
                ClientSecret = azureClientSecret
            };

            var keyVaultProvider = new KSeFPrinter.Services.Certificates.AzureKeyVaultCertificateProvider(
                loggerFactory.CreateLogger<KSeFPrinter.Services.Certificates.AzureKeyVaultCertificateProvider>()
            );

            try
            {
                certificate = await keyVaultProvider.LoadCertificateAsync(keyVaultOptions);
                if (certificate == null)
                {
                    logger.LogError("Nie udało się wczytać certyfikatu z Azure Key Vault");
                    Environment.Exit(1);
                    return;
                }
                logger.LogInformation("Certyfikat wczytany z Azure Key Vault: {Subject}", certificate.Subject);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd podczas wczytywania certyfikatu z Azure Key Vault");
                Environment.Exit(1);
                return;
            }
        }
        else if (!string.IsNullOrEmpty(certPath))
        {
            // Wczytaj z pliku PFX
            logger.LogInformation("Wczytywanie certyfikatu z pliku: {CertPath}", certPath);
            certificate = string.IsNullOrEmpty(certPassword)
                ? new X509Certificate2(certPath)
                : new X509Certificate2(certPath, certPassword);
            logger.LogInformation("Certyfikat wczytany z pliku: {Subject}", certificate.Subject);
        }
        else if (!string.IsNullOrEmpty(certThumbprint) || !string.IsNullOrEmpty(certSubject))
        {
            // Wczytaj z Windows Certificate Store
            certificate = LoadCertificateFromStore(
                certThumbprint,
                certSubject,
                certStoreName,
                certStoreLocation,
                logger);

            if (certificate == null)
            {
                logger.LogError("Nie znaleziono certyfikatu w Windows Certificate Store");
                Environment.Exit(1);
                return;
            }

            logger.LogInformation("Certyfikat wczytany z Windows Store: {Subject}", certificate.Subject);
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

        if (xmlFiles.Count == 0 && !watch)
        {
            logger.LogError("Nie znaleziono żadnych plików XML do przetworzenia");
            Environment.Exit(1);
            return;
        }

        if (watch)
        {
            // === TRYB WATCH ===
            await RunWatchMode(input, xmlFiles, service, options, !noValidate, ksefNumber, ksefFromFilename, logger);
        }
        else if (xmlFiles.Count == 1 && !string.IsNullOrEmpty(output))
        {
            // === TRYB POJEDYNCZEGO PLIKU Z WŁASNĄ ŚCIEŻKĄ ===
            await ProcessSingleFile(xmlFiles[0], output, service, options, !noValidate, ksefNumber, ksefFromFilename, logger);
        }
        else
        {
            // === TRYB WSADOWY ===
            await ProcessBatch(xmlFiles, service, options, !noValidate, ksefNumber, ksefFromFilename, logger);
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
            // Katalog - znajdź wszystkie XMLe (włącznie z podkatalogami NIP)
            files.AddRange(Directory.GetFiles(item, "*.xml", SearchOption.AllDirectories));
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

    // Wyklucz pliki UPO (mają inny namespace XML i będą przenoszone razem z fakturami)
    files = files.Where(f => !f.EndsWith("_UPO.xml", StringComparison.OrdinalIgnoreCase)).ToList();

    return files.Distinct().OrderBy(f => f).ToList();
}

static async Task ProcessSingleFile(
    string xmlPath,
    string pdfPath,
    InvoicePrinterService service,
    PdfGenerationOptions options,
    bool validate,
    string? ksefNumber,
    bool ksefFromFilename,
    ILogger logger)
{
    logger.LogInformation("=== Przetwarzanie pojedynczego pliku ===");
    logger.LogInformation("XML: {XmlPath}", xmlPath);
    logger.LogInformation("PDF: {PdfPath}", pdfPath);

    // Parsuj numer KSeF z nazwy pliku jeśli włączona opcja
    var effectiveKsefNumber = ksefNumber;
    if (ksefFromFilename && string.IsNullOrEmpty(ksefNumber))
    {
        effectiveKsefNumber = ParseKSeFFromFileName(xmlPath, logger);
    }

    try
    {
        await service.GeneratePdfFromFileAsync(
            xmlFilePath: xmlPath,
            pdfOutputPath: pdfPath,
            options: options,
            numerKSeF: effectiveKsefNumber,
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
    bool ksefFromFilename,
    ILogger logger)
{
    // Load configuration for file moving
    var config = KSeFPrinter.CLI.ConfigurationLoader.LoadConfiguration();

    logger.LogInformation("=== Przetwarzanie wsadowe ===");
    logger.LogInformation("Znaleziono {Count} plików XML", xmlFiles.Count);

    int successCount = 0;
    int errorCount = 0;

    foreach (var xmlPath in xmlFiles)
    {
        var pdfPath = Path.ChangeExtension(xmlPath, ".pdf");

        // Parsuj numer KSeF z nazwy pliku jeśli włączona opcja
        var effectiveKsefNumber = ksefNumber;
        if (ksefFromFilename && string.IsNullOrEmpty(ksefNumber))
        {
            effectiveKsefNumber = ParseKSeFFromFileName(xmlPath, logger);
        }

        string? generatedPdfPath = null;
        string? pdfErrorMessage = null;

        try
        {
            logger.LogInformation("Przetwarzanie: {FileName}...", Path.GetFileName(xmlPath));

            await service.GeneratePdfFromFileAsync(
                xmlFilePath: xmlPath,
                pdfOutputPath: pdfPath,
                options: options,
                numerKSeF: effectiveKsefNumber,
                validateInvoice: validate
            );

            var fileInfo = new FileInfo(pdfPath);
            logger.LogInformation("  ✓ {PdfFile} ({Size:N0} bajtów)", Path.GetFileName(pdfPath), fileInfo.Length);
            generatedPdfPath = pdfPath;
            successCount++;
        }
        catch (Exception ex)
        {
            logger.LogError("  ✗ Błąd: {Message}", ex.Message);
            pdfErrorMessage = ex.Message;
            errorCount++;
        }

        // Move files to target folder if configured (even if PDF generation failed)
        if (config.FileProcessing.MoveAfterProcessing)
        {
            var targetFolder = KSeFPrinter.CLI.FileProcessingHelpers.GetTargetFolder(xmlPath, config);
            if (targetFolder != null)
            {
                try
                {
                    KSeFPrinter.CLI.FileProcessingHelpers.MoveProcessedFiles(
                        xmlPath,
                        generatedPdfPath,
                        targetFolder,
                        logger,
                        pdfErrorMessage);
                }
                catch (Exception moveEx)
                {
                    logger.LogError(moveEx, "  ✗ Błąd przenoszenia plików: {Message}", moveEx.Message);
                }
            }
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
    string[] inputPaths,
    List<string> initialFiles,
    InvoicePrinterService service,
    PdfGenerationOptions options,
    bool validate,
    string? ksefNumber,
    bool ksefFromFilename,
    ILogger logger)
{
    // Load configuration
    var config = KSeFPrinter.CLI.ConfigurationLoader.LoadConfiguration();

    // Określ katalogi do obserwowania - z argumentów wejściowych (nie z plików!)
    var directories = new List<string>();

    foreach (var item in inputPaths)
    {
        if (Directory.Exists(item))
        {
            directories.Add(Path.GetFullPath(item));
        }
        else if (File.Exists(item))
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(item));
            if (dir != null)
                directories.Add(dir);
        }
    }

    directories = directories.Distinct().ToList();

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
    logger.LogInformation("Konfiguracja:");
    logger.LogInformation("  File stability check: {Delay}ms", config.FileProcessing.StabilityCheckDelayMs);
    logger.LogInformation("  Skip if PDF exists: {Skip}", config.FileProcessing.SkipIfPdfExists);
    logger.LogInformation("  Move after processing: {Move}", config.FileProcessing.MoveAfterProcessing);
    logger.LogInformation("");
    logger.LogInformation("Oczekiwanie na nowe pliki XML... (Ctrl+C aby zakończyć)");
    logger.LogInformation("");

    // Przetworz istniejące pliki najpierw (jeśli nie mają PDF)
    var filesToProcess = initialFiles.Where(f =>
    {
        var pdfPath = Path.ChangeExtension(f, ".pdf");
        if (config.FileProcessing.SkipIfPdfExists && File.Exists(pdfPath))
        {
            logger.LogInformation("⏭️  Pominięto (PDF istnieje): {FileName}", Path.GetFileName(f));
            return false;
        }
        return true;
    }).ToList();

    if (filesToProcess.Any())
    {
        await ProcessBatch(filesToProcess, service, options, validate, ksefNumber, ksefFromFilename, logger);
    }

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
            IncludeSubdirectories = true,  // Obserwuj podkatalogi NIP
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

            logger.LogInformation("Wykryto nowy plik: {FileName}", Path.GetFileName(e.FullPath));

            try
            {
                // Initial delay from config
                await Task.Delay(config.FileProcessing.WatcherDelayMs);

                // File stability check
                if (!await KSeFPrinter.CLI.FileProcessingHelpers.IsFileStableAsync(
                    e.FullPath,
                    config.FileProcessing.StabilityCheckDelayMs,
                    logger))
                {
                    logger.LogWarning("  ⏭️  Plik nie gotowy, spróbuj ponownie później");
                    lock (processedFiles) { processedFiles.Remove(fullPath); }
                    return;
                }

                var pdfPath = Path.ChangeExtension(e.FullPath, ".pdf");

                // Skip if PDF exists
                if (config.FileProcessing.SkipIfPdfExists && File.Exists(pdfPath))
                {
                    logger.LogInformation("  ⏭️  PDF już istnieje, pomijam");
                    return;
                }

                // Parsuj numer KSeF z nazwy pliku jeśli włączona opcja
                var effectiveKsefNumber = ksefNumber;
                if (ksefFromFilename && string.IsNullOrEmpty(ksefNumber))
                {
                    effectiveKsefNumber = ParseKSeFFromFileName(e.FullPath, logger);
                }

                // Try to generate PDF
                string? generatedPdfPath = null;
                string? pdfErrorMessage = null;

                try
                {
                    await service.GeneratePdfFromFileAsync(
                        xmlFilePath: e.FullPath,
                        pdfOutputPath: pdfPath,
                        options: options,
                        numerKSeF: effectiveKsefNumber,
                        validateInvoice: validate
                    );

                    var fileInfo = new FileInfo(pdfPath);
                    logger.LogInformation("  ✓ PDF utworzony: {PdfFile} ({Size:N0} bajtów)",
                        Path.GetFileName(pdfPath), fileInfo.Length);

                    generatedPdfPath = pdfPath;
                }
                catch (Exception pdfEx)
                {
                    logger.LogError(pdfEx, "  ✗ Błąd generowania PDF dla: {FileName}", Path.GetFileName(e.FullPath));
                    pdfErrorMessage = pdfEx.Message;
                    // Continue - move files anyway (without PDF)
                }

                // Move files to target folder if configured (even if PDF generation failed)
                if (config.FileProcessing.MoveAfterProcessing)
                {
                    var targetFolder = KSeFPrinter.CLI.FileProcessingHelpers.GetTargetFolder(e.FullPath, config);
                    if (targetFolder != null)
                    {
                        KSeFPrinter.CLI.FileProcessingHelpers.MoveProcessedFiles(
                            e.FullPath,
                            generatedPdfPath,
                            targetFolder,
                            logger,
                            pdfErrorMessage);
                    }
                    else
                    {
                        logger.LogDebug("  ℹ️  Brak docelowego folderu, pliki pozostają na miejscu");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("  ✗ Błąd krytyczny: {Message}", ex.Message);
                lock (processedFiles) { processedFiles.Remove(fullPath); }
            }
        };

        watchers.Add(watcher);
    }

    // Czekaj w nieskończoność (Ctrl+C aby zakończyć)
    await Task.Delay(Timeout.Infinite);
}

static X509Certificate2? LoadCertificateFromStore(
    string? thumbprint,
    string? subject,
    string storeName,
    string storeLocation,
    ILogger logger)
{
    // Parsuj lokalizację
    if (!Enum.TryParse<StoreLocation>(storeLocation, ignoreCase: true, out var location))
    {
        logger.LogError("Nieprawidłowa lokalizacja Store: {Location}. Dozwolone: CurrentUser, LocalMachine", storeLocation);
        return null;
    }

    // Parsuj nazwę store
    if (!Enum.TryParse<StoreName>(storeName, ignoreCase: true, out var name))
    {
        logger.LogError("Nieprawidłowa nazwa Store: {Name}. Dozwolone: My, Root, CA, TrustedPeople, itp.", storeName);
        return null;
    }

    logger.LogInformation("Wyszukiwanie certyfikatu w Windows Certificate Store:");
    logger.LogInformation("  Lokalizacja: {Location}", location);
    logger.LogInformation("  Store: {Store}", name);

    X509Store? store = null;
    try
    {
        store = new X509Store(name, location);
        store.Open(OpenFlags.ReadOnly);

        X509Certificate2Collection foundCertificates;

        if (!string.IsNullOrEmpty(thumbprint))
        {
            // Wyszukiwanie po thumbprint
            logger.LogInformation("  Thumbprint: {Thumbprint}", thumbprint);

            // Usuń spacje i dwukropki z thumbprinta (formatowanie)
            var cleanThumbprint = thumbprint.Replace(" ", "").Replace(":", "");

            foundCertificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                cleanThumbprint,
                validOnly: false);
        }
        else if (!string.IsNullOrEmpty(subject))
        {
            // Wyszukiwanie po subject
            logger.LogInformation("  Subject: {Subject}", subject);

            foundCertificates = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                subject,
                validOnly: false);
        }
        else
        {
            logger.LogError("Nie podano thumbprint ani subject");
            return null;
        }

        if (foundCertificates.Count == 0)
        {
            logger.LogError("Nie znaleziono certyfikatu spełniającego kryteria");
            return null;
        }

        if (foundCertificates.Count > 1)
        {
            logger.LogWarning("Znaleziono {Count} certyfikatów. Używam pierwszego:", foundCertificates.Count);
            foreach (var cert in foundCertificates)
            {
                logger.LogWarning("  - {Subject} (Thumbprint: {Thumbprint})", cert.Subject, cert.Thumbprint);
            }
        }

        var certificate = foundCertificates[0];

        // Sprawdź czy ma klucz prywatny
        if (!certificate.HasPrivateKey)
        {
            logger.LogError("Certyfikat nie zawiera klucza prywatnego!");
            logger.LogError("Upewnij się, że certyfikat został zainstalowany z kluczem prywatnym.");
            return null;
        }

        logger.LogInformation("✓ Znaleziono certyfikat:");
        logger.LogInformation("  Subject: {Subject}", certificate.Subject);
        logger.LogInformation("  Thumbprint: {Thumbprint}", certificate.Thumbprint);
        logger.LogInformation("  Ważny od: {NotBefore:yyyy-MM-dd}", certificate.NotBefore);
        logger.LogInformation("  Ważny do: {NotAfter:yyyy-MM-dd}", certificate.NotAfter);
        logger.LogInformation("  Ma klucz prywatny: Tak");

        return certificate;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Błąd wczytywania certyfikatu z Windows Store: {Message}", ex.Message);
        return null;
    }
    finally
    {
        store?.Close();
    }
}

/// <summary>
/// Parsuje numer KSeF z nazwy pliku
/// Format: {dowolna_nazwa}_{NUMER_KSEF}.xml
/// Przykład: faktura_6511153259-20251015-010020140418-0D.xml
/// </summary>
static string? ParseKSeFFromFileName(string filePath, ILogger logger)
{
    try
    {
        var fileName = Path.GetFileName(filePath);

        // Regex: opcjonalny underscore + numer KSeF + .xml na końcu
        var match = System.Text.RegularExpressions.Regex.Match(
            fileName,
            @"_?(\d{10}-\d{8}-[0-9A-Fa-f]{12}-[0-9A-Fa-f]{2})\.xml$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            logger.LogWarning("Nie znaleziono numeru KSeF w nazwie pliku: {FileName}", fileName);
            return null;
        }

        var potentialKSeF = match.Groups[1].Value.ToUpper();

        // Walidacja przez istniejący walidator
        if (!KSeFPrinter.Validators.KsefNumberValidator.IsValid(potentialKSeF, out var error))
        {
            logger.LogWarning(
                "Niepoprawny format numeru KSeF w nazwie pliku: {FileName}. Błąd: {Error}",
                fileName, error);
            return null;
        }

        logger.LogInformation("✓ Wykryto numer KSeF z nazwy pliku: {KSeFNumber}", potentialKSeF);
        return potentialKSeF;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Błąd podczas parsowania numeru KSeF z nazwy pliku");
        return null;
    }
}
