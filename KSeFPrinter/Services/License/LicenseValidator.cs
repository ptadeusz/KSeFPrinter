using KSeFPrinter.Models.License;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KSeFPrinter.Services.License;

public interface ILicenseValidator
{
    Task<LicenseValidationResult> ValidateLicenseAsync(CancellationToken ct = default);
    List<string> GetAllowedNips();
    bool IsProductLicensed(string productName);
}

/// <summary>
/// Walidator licencji dla KSeF Printer
/// ✅ Kompatybilność wsteczna: stare licencje KSeFConnector są odrzucane (Features.KSeFPrinter = null lub false)
/// </summary>
public class LicenseValidator : ILicenseValidator
{
    private readonly string _licenseFilePath;
    private readonly ILogger<LicenseValidator> _logger;
    private LicenseInfo? _cachedLicense;

    public LicenseValidator(
        string? licenseFilePath = null,
        ILogger<LicenseValidator>? logger = null)
    {
        // Szukaj licencji w głównym folderze instalacji (folder nadrzędny)
        // API: C:\Program Files\KSeF Printer\API\ -> C:\Program Files\KSeF Printer\license.lic
        // CLI: C:\Program Files\KSeF Printer\CLI\ -> C:\Program Files\KSeF Printer\license.lic
        var installRootDir = Directory.GetParent(AppContext.BaseDirectory)?.FullName ?? AppContext.BaseDirectory;

        // Jeśli licenseFilePath jest relatywną ścieżką, łącz z installRootDir
        // Jeśli jest absolutną ścieżką, użyj jej bezpośrednio
        if (string.IsNullOrEmpty(licenseFilePath))
        {
            _licenseFilePath = Path.Combine(installRootDir, "license.lic");
        }
        else if (Path.IsPathRooted(licenseFilePath))
        {
            // Absolutna ścieżka (np. "C:\Licenses\license.lic")
            _licenseFilePath = licenseFilePath;
        }
        else
        {
            // Relatywna ścieżka (np. "license.lic") - łącz z installRootDir
            _licenseFilePath = Path.Combine(installRootDir, licenseFilePath);
        }

        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LicenseValidator>.Instance;
    }

    /// <summary>
    /// Waliduje licencję: sprawdza datę, podpis RSA i uprawnienia do KSeF Printer
    /// </summary>
    public async Task<LicenseValidationResult> ValidateLicenseAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_licenseFilePath))
            {
                _logger.LogError("Plik licencji nie istnieje: {Path}", _licenseFilePath);
                return LicenseValidationResult.Fail($"Plik licencji nie istnieje: {_licenseFilePath}");
            }

            _logger.LogInformation("Wczytuję licencję z pliku: {Path}", _licenseFilePath);

            var licenseJson = await File.ReadAllTextAsync(_licenseFilePath, ct);
            var license = JsonSerializer.Deserialize<LicenseInfo>(licenseJson);

            if (license == null)
            {
                return LicenseValidationResult.Fail("Nieprawidłowy format licencji");
            }

            // 1. Sprawdź datę wygaśnięcia
            if (license.ExpiryDate < DateTime.UtcNow)
            {
                return LicenseValidationResult.Fail(
                    $"Licencja wygasła: {license.ExpiryDate:yyyy-MM-dd}");
            }

            // 2. Weryfikuj podpis RSA
            if (!VerifySignature(license))
            {
                return LicenseValidationResult.Fail("Nieprawidłowy podpis licencji");
            }

            // 3. Sprawdź uprawnienia do KSeF Printer
            // ✅ KOMPATYBILNOŚĆ WSTECZNA:
            //    - Jeśli Features.KSeFPrinter == null (stara licencja) → ODRZUĆ
            //    - Jeśli Features.KSeFPrinter == false → ODRZUĆ
            //    - Jeśli Features.KSeFPrinter == true → OK
            if (license.Features.KSeFPrinter != true)
            {
                return LicenseValidationResult.Fail(
                    "Licencja nie obejmuje KSeF Printer. Skontaktuj się z dostawcą w celu zakupu licencji.");
            }

            _cachedLicense = license;

            _logger.LogInformation(
                "✅ Licencja ważna do {ExpiryDate}, Dozwolone NIP-y: {NipCount}, Właściciel: {IssuedTo}",
                license.ExpiryDate, license.AllowedNips.Count, license.IssuedTo);

            return LicenseValidationResult.Success(license);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd walidacji licencji");
            return LicenseValidationResult.Fail($"Błąd: {ex.Message}");
        }
    }

    internal static class LicenseVerifier_PublicKey
    {
        // ✅ Ten sam klucz publiczny co w KSeFConnector (wspólna para kluczy RSA)
        internal const string PublicKeyPem = """
-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEApRyIz4m/Uzt81BHXb9G+SEhFH87FakfisH0U83JyDlrzsIa846Uk
zRRZz/FHT4m4F16t/BduK8bUhh52FqO0NAX7OEH/U75mj+tlMeUzGnIzmdtOqWDN
/+9Tfkek3cjVkuni1t/TcM8OjuHv+/RwVTvYnqWOqOpfJewhM0sZgrQLhPDw/2Qy
R/DiRAc/DUVA6g/6lJooamwabP8ojNq7Ug/MvG9GEAciPC5uK7LlXRQ1iXINvgQU
zTUQRok993sndBLw+1zkJD51LjJu8gHbqNYF0Xwsv+gxmj0Zkqr0PvCbF2G/S+Se
uSGkuGtcw+OuVlnkJ+n++5PYI/kTzuAFSQIDAQAB
-----END RSA PUBLIC KEY-----
""";
    }

    /// <summary>
    /// Weryfikuje podpis RSA licencji
    /// KRYTYCZNE: Logika MUSI być identyczna z KSeFConnector.Core i generatorem!
    /// </summary>
    private bool VerifySignature(LicenseInfo license)
    {
        try
        {
            var publicKeyPem = LicenseVerifier_PublicKey.PublicKeyPem;

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            // ✅ IDENTYCZNA LOGIKA JAK W GENERATORZE
            // Dane do weryfikacji (bez pola Signature)
            var dataToVerify = JsonSerializer.Serialize(new
            {
                license.LicenseKey,
                license.IssuedTo,
                license.ExpiryDate,
                license.AllowedNips,
                license.MaxNips,
                license.Features
            }, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var dataBytes = Encoding.UTF8.GetBytes(dataToVerify);
            var signatureBytes = Convert.FromBase64String(license.Signature);

            var isValid = rsa.VerifyData(
                dataBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (!isValid)
            {
                _logger.LogWarning("⚠️ Podpis licencji jest nieprawidłowy!");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas weryfikacji podpisu");
            return false;
        }
    }

    /// <summary>
    /// Pobiera listę dozwolonych NIP-ów z licencji
    /// </summary>
    public List<string> GetAllowedNips()
    {
        if (_cachedLicense == null)
        {
            _logger.LogWarning("Licencja nie została jeszcze zwalidowana");
            return new List<string>();
        }

        return _cachedLicense.AllowedNips;
    }

    /// <summary>
    /// Sprawdza czy dany produkt jest objęty licencją
    /// </summary>
    public bool IsProductLicensed(string productName)
    {
        if (_cachedLicense == null)
        {
            _logger.LogWarning("Licencja nie została jeszcze zwalidowana");
            return false;
        }

        return productName.ToLowerInvariant() switch
        {
            "ksefprinter" => _cachedLicense.Features.KSeFPrinter == true,
            "ksefconnector" => _cachedLicense.Features.KSeFConnector ?? true, // ✅ Kompatybilność wsteczna
            _ => false
        };
    }
}
