using System.Security.Cryptography.X509Certificates;
using KSeFPrinter.API.Models;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Certificates;

namespace KSeFPrinter.API.Services;

/// <summary>
/// Serwis do wczytywania certyfikatów z różnych źródeł (Windows Store, Azure Key Vault)
/// </summary>
public class CertificateService
{
    private readonly ILogger<CertificateService> _logger;
    private readonly AzureKeyVaultCertificateProvider _keyVaultProvider;

    // Cached certificates loaded from configuration
    private X509Certificate2? _onlineCertificate;
    private X509Certificate2? _offlineCertificate;

    public CertificateService(
        ILogger<CertificateService> logger,
        ILogger<AzureKeyVaultCertificateProvider> keyVaultLogger)
    {
        _logger = logger;
        _keyVaultProvider = new AzureKeyVaultCertificateProvider(keyVaultLogger);
    }

    /// <summary>
    /// Pobiera certyfikat ONLINE (dla faktur ONLINE - KOD QR II)
    /// </summary>
    public X509Certificate2? GetOnlineCertificate() => _onlineCertificate;

    /// <summary>
    /// Pobiera certyfikat OFFLINE (dla faktur OFFLINE - podpis cyfrowy)
    /// </summary>
    public X509Certificate2? GetOfflineCertificate() => _offlineCertificate;

    /// <summary>
    /// Wczytuje certyfikaty z konfiguracji przy starcie aplikacji
    /// </summary>
    public async Task LoadCertificatesFromConfigurationAsync(CertificatesConfiguration config, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Wczytywanie certyfikatów z konfiguracji ===");

        // Wczytaj certyfikat ONLINE
        if (config.Online.Enabled)
        {
            _logger.LogInformation("Wczytywanie certyfikatu ONLINE...");
            _onlineCertificate = await LoadCertificateFromOptionsAsync(config.Online, "ONLINE", cancellationToken);

            if (_onlineCertificate != null)
            {
                _logger.LogInformation("✅ Certyfikat ONLINE wczytany: {Subject}", _onlineCertificate.Subject);
            }
            else
            {
                _logger.LogWarning("⚠️ Certyfikat ONLINE nie został wczytany (KOD QR II nie będzie generowany dla faktur ONLINE)");
            }
        }
        else
        {
            _logger.LogInformation("Certyfikat ONLINE wyłączony w konfiguracji");
        }

        // Wczytaj certyfikat OFFLINE
        if (config.Offline.Enabled)
        {
            _logger.LogInformation("Wczytywanie certyfikatu OFFLINE...");
            _offlineCertificate = await LoadCertificateFromOptionsAsync(config.Offline, "OFFLINE", cancellationToken);

            if (_offlineCertificate != null)
            {
                _logger.LogInformation("✅ Certyfikat OFFLINE wczytany: {Subject}", _offlineCertificate.Subject);
            }
            else
            {
                _logger.LogWarning("⚠️ Certyfikat OFFLINE nie został wczytany (faktury OFFLINE nie będą podpisywane)");
            }
        }
        else
        {
            _logger.LogInformation("Certyfikat OFFLINE wyłączony w konfiguracji");
        }
    }

    /// <summary>
    /// Wczytuje certyfikat z opcji konfiguracji
    /// </summary>
    private async Task<X509Certificate2?> LoadCertificateFromOptionsAsync(
        CertificateOptions options,
        string certificateType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (options.Source.Equals("AzureKeyVault", StringComparison.OrdinalIgnoreCase))
            {
                var azureOptions = new KSeFPrinter.Models.Common.AzureKeyVaultOptions
                {
                    KeyVaultUrl = options.AzureKeyVault.KeyVaultUrl!,
                    CertificateName = options.AzureKeyVault.CertificateName!,
                    CertificateVersion = options.AzureKeyVault.CertificateVersion,
                    AuthenticationType = Enum.TryParse<KSeFPrinter.Models.Common.AzureAuthenticationType>(
                        options.AzureKeyVault.AuthenticationType, out var authType)
                        ? authType
                        : KSeFPrinter.Models.Common.AzureAuthenticationType.DefaultAzureCredential,
                    TenantId = options.AzureKeyVault.TenantId,
                    ClientId = options.AzureKeyVault.ClientId,
                    ClientSecret = options.AzureKeyVault.ClientSecret
                };

                return await LoadFromKeyVaultAsync(azureOptions, cancellationToken);
            }
            else if (options.Source.Equals("WindowsStore", StringComparison.OrdinalIgnoreCase))
            {
                return LoadFromStore(
                    options.WindowsStore.Thumbprint,
                    options.WindowsStore.Subject,
                    options.WindowsStore.StoreName,
                    options.WindowsStore.StoreLocation);
            }
            else
            {
                _logger.LogError("Nieznane źródło certyfikatu {Type}: {Source}", certificateType, options.Source);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wczytywania certyfikatu {Type}", certificateType);
            return null;
        }
    }

    /// <summary>
    /// Wczytuje certyfikat z Windows Certificate Store
    /// </summary>
    public X509Certificate2? LoadFromStore(
        string? thumbprint,
        string? subject,
        string storeName,
        string storeLocation)
    {
        if (string.IsNullOrEmpty(thumbprint) && string.IsNullOrEmpty(subject))
        {
            return null;
        }

        // Parsuj lokalizację
        if (!Enum.TryParse<StoreLocation>(storeLocation, ignoreCase: true, out var location))
        {
            _logger.LogError("Nieprawidłowa lokalizacja Store: {Location}", storeLocation);
            throw new ArgumentException($"Nieprawidłowa lokalizacja Store: {storeLocation}");
        }

        // Parsuj nazwę store
        if (!Enum.TryParse<StoreName>(storeName, ignoreCase: true, out var name))
        {
            _logger.LogError("Nieprawidłowa nazwa Store: {Name}", storeName);
            throw new ArgumentException($"Nieprawidłowa nazwa Store: {storeName}");
        }

        _logger.LogInformation("Wyszukiwanie certyfikatu w Windows Certificate Store: {Location}/{Store}", location, name);

        X509Store? store = null;
        try
        {
            store = new X509Store(name, location);
            store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection foundCertificates;

            if (!string.IsNullOrEmpty(thumbprint))
            {
                // Wyszukiwanie po thumbprint
                var cleanThumbprint = thumbprint.Replace(" ", "").Replace(":", "");
                _logger.LogInformation("Wyszukiwanie po thumbprint: {Thumbprint}", cleanThumbprint);

                foundCertificates = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    cleanThumbprint,
                    validOnly: false);
            }
            else
            {
                // Wyszukiwanie po subject
                _logger.LogInformation("Wyszukiwanie po subject: {Subject}", subject);

                foundCertificates = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    subject!,
                    validOnly: false);
            }

            if (foundCertificates.Count == 0)
            {
                _logger.LogError("Nie znaleziono certyfikatu");
                throw new InvalidOperationException("Nie znaleziono certyfikatu w Windows Certificate Store");
            }

            if (foundCertificates.Count > 1)
            {
                _logger.LogWarning("Znaleziono {Count} certyfikatów. Używam pierwszego", foundCertificates.Count);
            }

            var certificate = foundCertificates[0];

            // Sprawdź czy ma klucz prywatny
            if (!certificate.HasPrivateKey)
            {
                _logger.LogError("Certyfikat nie zawiera klucza prywatnego");
                throw new InvalidOperationException("Certyfikat nie zawiera klucza prywatnego");
            }

            _logger.LogInformation("Certyfikat wczytany: {Subject}", certificate.Subject);
            return certificate;
        }
        finally
        {
            store?.Close();
        }
    }

    /// <summary>
    /// Wczytuje certyfikat z Azure Key Vault
    /// </summary>
    public async Task<X509Certificate2?> LoadFromKeyVaultAsync(
        KSeFPrinter.Models.Common.AzureKeyVaultOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!_keyVaultProvider.ValidateOptions(options))
        {
            return null;
        }

        _logger.LogInformation("Wczytywanie certyfikatu z Azure Key Vault");

        return await _keyVaultProvider.LoadCertificateAsync(options, cancellationToken);
    }
}
