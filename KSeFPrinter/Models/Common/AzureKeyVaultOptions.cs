namespace KSeFPrinter.Models.Common;

/// <summary>
/// Opcje konfiguracji Azure Key Vault
/// </summary>
public class AzureKeyVaultOptions
{
    /// <summary>
    /// URL do Azure Key Vault (np. https://myvault.vault.azure.net/)
    /// </summary>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Nazwa certyfikatu w Key Vault
    /// </summary>
    public string? CertificateName { get; set; }

    /// <summary>
    /// Opcjonalna wersja certyfikatu (jeśli null, użyje najnowszej)
    /// </summary>
    public string? CertificateVersion { get; set; }

    /// <summary>
    /// Typ uwierzytelniania: ManagedIdentity, ClientSecret, EnvironmentCredential
    /// </summary>
    public AzureAuthenticationType AuthenticationType { get; set; } = AzureAuthenticationType.DefaultAzureCredential;

    /// <summary>
    /// Azure Tenant ID (dla ClientSecret)
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure Client ID (dla ClientSecret lub ManagedIdentity)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Azure Client Secret (dla ClientSecret)
    /// </summary>
    public string? ClientSecret { get; set; }
}

/// <summary>
/// Typ uwierzytelniania Azure
/// </summary>
public enum AzureAuthenticationType
{
    /// <summary>
    /// DefaultAzureCredential (próbuje różne metody automatycznie)
    /// </summary>
    DefaultAzureCredential,

    /// <summary>
    /// Managed Identity (dla aplikacji w Azure)
    /// </summary>
    ManagedIdentity,

    /// <summary>
    /// Client Secret (Service Principal)
    /// </summary>
    ClientSecret,

    /// <summary>
    /// Environment Variables (zmienne środowiskowe)
    /// </summary>
    EnvironmentCredential,

    /// <summary>
    /// Azure CLI credentials
    /// </summary>
    AzureCliCredential
}
