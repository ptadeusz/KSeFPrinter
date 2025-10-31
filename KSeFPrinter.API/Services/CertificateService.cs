using System.Security.Cryptography.X509Certificates;
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

    public CertificateService(
        ILogger<CertificateService> logger,
        ILogger<AzureKeyVaultCertificateProvider> keyVaultLogger)
    {
        _logger = logger;
        _keyVaultProvider = new AzureKeyVaultCertificateProvider(keyVaultLogger);
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
        AzureKeyVaultOptions options,
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
