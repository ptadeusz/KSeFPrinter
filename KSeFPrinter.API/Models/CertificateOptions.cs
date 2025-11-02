namespace KSeFPrinter.API.Models;

/// <summary>
/// Konfiguracja certyfikatów z appsettings.json
/// </summary>
public class CertificatesConfiguration
{
    /// <summary>
    /// Certyfikat dla faktur ONLINE (KOD QR II)
    /// </summary>
    public CertificateOptions Online { get; set; } = new();

    /// <summary>
    /// Certyfikat dla faktur OFFLINE (podpis cyfrowy wystawcy)
    /// </summary>
    public CertificateOptions Offline { get; set; } = new();
}

/// <summary>
/// Opcje konfiguracji pojedynczego certyfikatu
/// </summary>
public class CertificateOptions
{
    /// <summary>
    /// Czy certyfikat jest włączony
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Źródło certyfikatu: WindowsStore lub AzureKeyVault
    /// </summary>
    public string Source { get; set; } = "WindowsStore";

    /// <summary>
    /// Ustawienia dla Windows Certificate Store
    /// </summary>
    public WindowsStoreOptions WindowsStore { get; set; } = new();

    /// <summary>
    /// Ustawienia dla Azure Key Vault
    /// </summary>
    public AzureKeyVaultOptions AzureKeyVault { get; set; } = new();
}

/// <summary>
/// Opcje dla Windows Certificate Store
/// </summary>
public class WindowsStoreOptions
{
    /// <summary>
    /// Thumbprint certyfikatu (hex string)
    /// </summary>
    public string? Thumbprint { get; set; }

    /// <summary>
    /// Subject certyfikatu (CN=...)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Nazwa Store (My, Root, CA, etc.)
    /// </summary>
    public string StoreName { get; set; } = "My";

    /// <summary>
    /// Lokalizacja Store (CurrentUser, LocalMachine)
    /// </summary>
    public string StoreLocation { get; set; } = "CurrentUser";
}

/// <summary>
/// Opcje dla Azure Key Vault
/// </summary>
public class AzureKeyVaultOptions
{
    /// <summary>
    /// URL Key Vault (https://myvault.vault.azure.net/)
    /// </summary>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Nazwa certyfikatu w Key Vault
    /// </summary>
    public string? CertificateName { get; set; }

    /// <summary>
    /// Wersja certyfikatu (opcjonalna, null = latest)
    /// </summary>
    public string? CertificateVersion { get; set; }

    /// <summary>
    /// Typ uwierzytelniania (DefaultAzureCredential, ClientSecret, ManagedIdentity)
    /// </summary>
    public string AuthenticationType { get; set; } = "DefaultAzureCredential";

    /// <summary>
    /// Tenant ID (dla ClientSecret)
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Client ID (dla ClientSecret lub ManagedIdentity)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client Secret (dla ClientSecret)
    /// </summary>
    public string? ClientSecret { get; set; }
}
