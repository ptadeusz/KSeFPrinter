using System.Text.Json.Serialization;

namespace KSeFPrinter.Models.License;

/// <summary>
/// Informacje o licencji (wspólna dla KSeFConnector i KSeFPrinter)
/// ✅ Kompatybilność wsteczna:
///    - Stare licencje: brak pól ksefConnector/ksefPrinter (null)
///    - KSeFConnector: null = true (domyślnie włączone dla starych licencji)
///    - KSeFPrinter: null = false (domyślnie wyłączone dla starych licencji)
/// </summary>
public class LicenseInfo
{
    [JsonPropertyName("licenseKey")]
    public string LicenseKey { get; set; } = string.Empty;

    [JsonPropertyName("issuedTo")]
    public string IssuedTo { get; set; } = string.Empty;

    [JsonPropertyName("expiryDate")]
    [JsonConverter(typeof(UnixTimestampDateTimeConverter))]
    public DateTime ExpiryDate { get; set; }

    [JsonPropertyName("allowedNips")]
    public List<string> AllowedNips { get; set; } = new();

    [JsonPropertyName("maxNips")]
    public int MaxNips { get; set; }

    [JsonPropertyName("features")]
    public LicenseFeatures Features { get; set; } = new();

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Funkcje włączone w licencji
/// ✅ v2.0: Dodano wsparcie dla wielu produktów (KSeFConnector, KSeFPrinter)
/// ✅ Kompatybilność wsteczna: pola produktów są nullable
/// </summary>
public class LicenseFeatures
{
    // === PRODUKTY ===

    /// <summary>
    /// Licencja na KSeF Connector
    /// null = BRAK POLA (stara licencja) → interpretowane jako TRUE (kompatybilność wsteczna)
    /// </summary>
    [JsonPropertyName("ksefConnector")]
    public bool? KSeFConnector { get; set; }

    /// <summary>
    /// Licencja na KSeF Printer
    /// null = BRAK POLA (stara licencja) → interpretowane jako FALSE (nowy produkt)
    /// </summary>
    [JsonPropertyName("ksefPrinter")]
    public bool? KSeFPrinter { get; set; }

    // === FUNKCJE KSEF CONNECTOR ===

    [JsonPropertyName("batchMode")]
    public bool BatchMode { get; set; }

    [JsonPropertyName("autoTokenGeneration")]
    public bool AutoTokenGeneration { get; set; }

    [JsonPropertyName("azureKeyVault")]
    public bool AzureKeyVault { get; set; }
}

/// <summary>
/// Wynik walidacji licencji
/// </summary>
public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public LicenseInfo? License { get; set; }

    public static LicenseValidationResult Success(LicenseInfo license) =>
        new() { IsValid = true, License = license };

    public static LicenseValidationResult Fail(string error) =>
        new() { IsValid = false, ErrorMessage = error };
}
