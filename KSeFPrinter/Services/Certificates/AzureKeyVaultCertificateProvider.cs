using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using KSeFPrinter.Models.Common;
using Microsoft.Extensions.Logging;

namespace KSeFPrinter.Services.Certificates;

/// <summary>
/// Provider certyfikatów z Azure Key Vault
/// </summary>
public class AzureKeyVaultCertificateProvider
{
    private readonly ILogger<AzureKeyVaultCertificateProvider> _logger;

    public AzureKeyVaultCertificateProvider(ILogger<AzureKeyVaultCertificateProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Wczytuje certyfikat z Azure Key Vault
    /// </summary>
    public async Task<X509Certificate2?> LoadCertificateAsync(AzureKeyVaultOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.KeyVaultUrl))
        {
            _logger.LogError("KeyVaultUrl nie został podany");
            throw new ArgumentException("KeyVaultUrl jest wymagany", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.CertificateName))
        {
            _logger.LogError("CertificateName nie został podany");
            throw new ArgumentException("CertificateName jest wymagany", nameof(options));
        }

        _logger.LogInformation(
            "Wczytywanie certyfikatu z Azure Key Vault: {Vault}/{Certificate}",
            options.KeyVaultUrl,
            options.CertificateName
        );

        try
        {
            // Wybierz credential na podstawie typu uwierzytelniania
            var credential = CreateCredential(options);

            // Utwórz klienta Key Vault
            var client = new CertificateClient(new Uri(options.KeyVaultUrl), credential);

            // Pobierz certyfikat
            byte[] certificateBytes;
            if (!string.IsNullOrWhiteSpace(options.CertificateVersion))
            {
                _logger.LogDebug("Pobieranie wersji certyfikatu: {Version}", options.CertificateVersion);
                var response = await client.GetCertificateVersionAsync(
                    options.CertificateName,
                    options.CertificateVersion,
                    cancellationToken
                );
                certificateBytes = response.Value.Cer;
            }
            else
            {
                _logger.LogDebug("Pobieranie najnowszej wersji certyfikatu");
                var response = await client.GetCertificateAsync(
                    options.CertificateName,
                    cancellationToken
                );
                certificateBytes = response.Value.Cer;
            }

            // Konwertuj na X509Certificate2
            var certificate = new X509Certificate2(certificateBytes);

            _logger.LogInformation(
                "Certyfikat wczytany z Key Vault: Subject={Subject}, Thumbprint={Thumbprint}, HasPrivateKey={HasPrivateKey}",
                certificate.Subject,
                certificate.Thumbprint,
                certificate.HasPrivateKey
            );

            // UWAGA: Certyfikaty z Key Vault mogą nie mieć klucza prywatnego w samym obiekcie certyfikatu
            // Klucz prywatny jest dostępny poprzez Key Vault API podczas operacji podpisywania
            // Dla pełnej integracji z kluczem prywatnym, trzeba użyć dodatkowych metod

            if (!certificate.HasPrivateKey)
            {
                _logger.LogWarning(
                    "Certyfikat z Key Vault nie zawiera klucza prywatnego lokalnie. " +
                    "Dla operacji podpisywania należy użyć Azure Key Vault Cryptography API."
                );

                // Opcjonalnie: Pobierz pełny certyfikat z kluczem prywatnym jeśli jest dostępny
                certificate = await TryLoadWithPrivateKeyAsync(client, options, cancellationToken);
            }

            return certificate;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Błąd podczas wczytywania certyfikatu z Key Vault: {Message}",
                ex.Message
            );
            throw new InvalidOperationException(
                $"Nie udało się wczytać certyfikatu z Azure Key Vault: {ex.Message}",
                ex
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Nieoczekiwany błąd podczas wczytywania certyfikatu z Key Vault"
            );
            throw;
        }
    }

    /// <summary>
    /// Próbuje pobrać certyfikat z kluczem prywatnym
    /// </summary>
    private async Task<X509Certificate2> TryLoadWithPrivateKeyAsync(
        CertificateClient client,
        AzureKeyVaultOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Próba pobrania certyfikatu z kluczem prywatnym poprzez Secret API");

            // W Key Vault, pełny certyfikat (z kluczem prywatnym) jest przechowywany jako Secret
            var secretClient = new Azure.Security.KeyVault.Secrets.SecretClient(
                new Uri(options.KeyVaultUrl!),
                CreateCredential(options)
            );

            var secret = await secretClient.GetSecretAsync(options.CertificateName!, cancellationToken: cancellationToken);
            var privateKeyBytes = Convert.FromBase64String(secret.Value.Value);

            var certificate = new X509Certificate2(
                privateKeyBytes,
                (string?)null,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable
            );

            if (certificate.HasPrivateKey)
            {
                _logger.LogInformation("Pomyślnie wczytano certyfikat z kluczem prywatnym");
                return certificate;
            }

            _logger.LogWarning("Secret nie zawiera klucza prywatnego");
            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie udało się pobrać certyfikatu z kluczem prywatnym poprzez Secret API");

            // Zwróć certyfikat bez klucza prywatnego
            var certificateWithPolicy = await client.GetCertificateAsync(options.CertificateName!, cancellationToken);
            return new X509Certificate2(certificateWithPolicy.Value.Cer);
        }
    }

    /// <summary>
    /// Tworzy odpowiedni credential na podstawie typu uwierzytelniania
    /// </summary>
    private Azure.Core.TokenCredential CreateCredential(AzureKeyVaultOptions options)
    {
        _logger.LogDebug("Tworzenie credential typu: {Type}", options.AuthenticationType);

        return options.AuthenticationType switch
        {
            AzureAuthenticationType.DefaultAzureCredential => new DefaultAzureCredential(),

            AzureAuthenticationType.ManagedIdentity => !string.IsNullOrWhiteSpace(options.ClientId)
                ? new ManagedIdentityCredential(options.ClientId)
                : new ManagedIdentityCredential(),

            AzureAuthenticationType.ClientSecret => new ClientSecretCredential(
                options.TenantId ?? throw new ArgumentException("TenantId jest wymagany dla ClientSecret"),
                options.ClientId ?? throw new ArgumentException("ClientId jest wymagany dla ClientSecret"),
                options.ClientSecret ?? throw new ArgumentException("ClientSecret jest wymagany dla ClientSecret")
            ),

            AzureAuthenticationType.EnvironmentCredential => new EnvironmentCredential(),

            AzureAuthenticationType.AzureCliCredential => new AzureCliCredential(),

            _ => throw new NotSupportedException($"Typ uwierzytelniania {options.AuthenticationType} nie jest obsługiwany")
        };
    }

    /// <summary>
    /// Waliduje opcje Key Vault
    /// </summary>
    public bool ValidateOptions(AzureKeyVaultOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.KeyVaultUrl))
        {
            _logger.LogError("KeyVaultUrl jest pusty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.CertificateName))
        {
            _logger.LogError("CertificateName jest pusty");
            return false;
        }

        // Walidacja URI
        if (!Uri.TryCreate(options.KeyVaultUrl, UriKind.Absolute, out var uri))
        {
            _logger.LogError("KeyVaultUrl nie jest prawidłowym URI: {Url}", options.KeyVaultUrl);
            return false;
        }

        if (!uri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("KeyVaultUrl nie wygląda jak Azure Key Vault URL: {Url}", options.KeyVaultUrl);
        }

        // Walidacja uwierzytelniania ClientSecret
        if (options.AuthenticationType == AzureAuthenticationType.ClientSecret)
        {
            if (string.IsNullOrWhiteSpace(options.TenantId))
            {
                _logger.LogError("TenantId jest wymagany dla ClientSecret");
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.ClientId))
            {
                _logger.LogError("ClientId jest wymagany dla ClientSecret");
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                _logger.LogError("ClientSecret jest wymagany dla ClientSecret");
                return false;
            }
        }

        return true;
    }
}
