using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;

namespace KSeFPrinter.Services.Cryptography;

/// <summary>
/// Serwis kryptograficzny dla generowania kodów QR KSeF
/// Obsługuje SHA-256, Base64URL, RSA-PSS i ECDSA P-256
/// </summary>
public class CryptographyService
{
    private readonly ILogger<CryptographyService> _logger;

    public CryptographyService(ILogger<CryptographyService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Oblicza skrót SHA-256 z danych
    /// </summary>
    /// <param name="data">Dane do hashowania</param>
    /// <returns>Skrót SHA-256 jako tablica bajtów</returns>
    public byte[] ComputeSha256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
    }

    /// <summary>
    /// Oblicza skrót SHA-256 z tekstu UTF-8
    /// </summary>
    /// <param name="text">Tekst do hashowania</param>
    /// <returns>Skrót SHA-256 jako tablica bajtów</returns>
    public byte[] ComputeSha256Hash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return ComputeSha256Hash(bytes);
    }

    /// <summary>
    /// Oblicza skrót SHA-256 i koduje do Base64URL
    /// </summary>
    /// <param name="data">Dane do hashowania</param>
    /// <returns>Skrót SHA-256 jako string Base64URL</returns>
    public string ComputeSha256HashBase64Url(byte[] data)
    {
        var hash = ComputeSha256Hash(data);
        return ToBase64Url(hash);
    }

    /// <summary>
    /// Oblicza skrót SHA-256 z tekstu i koduje do Base64URL
    /// </summary>
    /// <param name="text">Tekst do hashowania</param>
    /// <returns>Skrót SHA-256 jako string Base64URL</returns>
    public string ComputeSha256HashBase64Url(string text)
    {
        var hash = ComputeSha256Hash(text);
        return ToBase64Url(hash);
    }

    /// <summary>
    /// Generuje podpis RSA-PSS dla danych
    /// Algorytm: SHA-256, MGF1, salt 32 bajty
    /// </summary>
    /// <param name="data">Dane do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym RSA</param>
    /// <returns>Podpis jako tablica bajtów</returns>
    public byte[] SignWithRsaPss(byte[] data, X509Certificate2 certificate)
    {
        if (!certificate.HasPrivateKey)
        {
            throw new CryptographicException("Certyfikat nie zawiera klucza prywatnego");
        }

        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa == null)
        {
            throw new CryptographicException("Nie można pobrać klucza prywatnego RSA z certyfikatu");
        }

        // RSA-PSS z SHA-256, MGF1, salt 32 bajty
        var signature = rsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss
        );

        _logger.LogDebug("Wygenerowano podpis RSA-PSS, długość: {Length} bajtów", signature.Length);
        return signature;
    }

    /// <summary>
    /// Generuje podpis RSA-PSS dla tekstu UTF-8
    /// </summary>
    /// <param name="text">Tekst do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym RSA</param>
    /// <returns>Podpis jako tablica bajtów</returns>
    public byte[] SignWithRsaPss(string text, X509Certificate2 certificate)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return SignWithRsaPss(bytes, certificate);
    }

    /// <summary>
    /// Generuje podpis RSA-PSS i koduje do Base64URL
    /// </summary>
    /// <param name="data">Dane do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym RSA</param>
    /// <returns>Podpis jako string Base64URL</returns>
    public string SignWithRsaPssBase64Url(byte[] data, X509Certificate2 certificate)
    {
        var signature = SignWithRsaPss(data, certificate);
        return ToBase64Url(signature);
    }

    /// <summary>
    /// Generuje podpis RSA-PSS dla tekstu i koduje do Base64URL
    /// </summary>
    /// <param name="text">Tekst do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym RSA</param>
    /// <returns>Podpis jako string Base64URL</returns>
    public string SignWithRsaPssBase64Url(string text, X509Certificate2 certificate)
    {
        var signature = SignWithRsaPss(text, certificate);
        return ToBase64Url(signature);
    }

    /// <summary>
    /// Generuje podpis ECDSA P-256 dla danych
    /// Format: IEEE P1363 (r||s)
    /// </summary>
    /// <param name="data">Dane do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym ECDSA</param>
    /// <returns>Podpis jako tablica bajtów</returns>
    public byte[] SignWithEcdsa(byte[] data, X509Certificate2 certificate)
    {
        if (!certificate.HasPrivateKey)
        {
            throw new CryptographicException("Certyfikat nie zawiera klucza prywatnego");
        }

        using var ecdsa = certificate.GetECDsaPrivateKey();
        if (ecdsa == null)
        {
            throw new CryptographicException("Nie można pobrać klucza prywatnego ECDSA z certyfikatu");
        }

        // ECDSA z SHA-256, format IEEE P1363
        var signature = ecdsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation
        );

        _logger.LogDebug("Wygenerowano podpis ECDSA P-256, długość: {Length} bajtów", signature.Length);
        return signature;
    }

    /// <summary>
    /// Generuje podpis ECDSA P-256 dla tekstu UTF-8
    /// </summary>
    /// <param name="text">Tekst do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym ECDSA</param>
    /// <returns>Podpis jako tablica bajtów</returns>
    public byte[] SignWithEcdsa(string text, X509Certificate2 certificate)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return SignWithEcdsa(bytes, certificate);
    }

    /// <summary>
    /// Generuje podpis ECDSA P-256 i koduje do Base64URL
    /// </summary>
    /// <param name="data">Dane do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym ECDSA</param>
    /// <returns>Podpis jako string Base64URL</returns>
    public string SignWithEcdsaBase64Url(byte[] data, X509Certificate2 certificate)
    {
        var signature = SignWithEcdsa(data, certificate);
        return ToBase64Url(signature);
    }

    /// <summary>
    /// Generuje podpis ECDSA P-256 dla tekstu i koduje do Base64URL
    /// </summary>
    /// <param name="text">Tekst do podpisania</param>
    /// <param name="certificate">Certyfikat z kluczem prywatnym ECDSA</param>
    /// <returns>Podpis jako string Base64URL</returns>
    public string SignWithEcdsaBase64Url(string text, X509Certificate2 certificate)
    {
        var signature = SignWithEcdsa(text, certificate);
        return ToBase64Url(signature);
    }

    /// <summary>
    /// Koduje dane do Base64URL (RFC 4648)
    /// Base64URL różni się od standardowego Base64:
    /// - Zamienia '+' na '-'
    /// - Zamienia '/' na '_'
    /// - Usuwa padding '='
    /// </summary>
    /// <param name="data">Dane do zakodowania</param>
    /// <returns>String Base64URL</returns>
    public string ToBase64Url(byte[] data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Dekoduje dane z Base64URL
    /// </summary>
    /// <param name="base64Url">String Base64URL</param>
    /// <returns>Odkodowane dane jako tablica bajtów</returns>
    public byte[] FromBase64Url(string base64Url)
    {
        // Przywróć standardowy Base64
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Dodaj padding jeśli potrzebny
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Sprawdza czy certyfikat zawiera klucz RSA
    /// </summary>
    public bool IsRsaCertificate(X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPublicKey();
        return rsa != null;
    }

    /// <summary>
    /// Sprawdza czy certyfikat zawiera klucz ECDSA
    /// </summary>
    public bool IsEcdsaCertificate(X509Certificate2 certificate)
    {
        using var ecdsa = certificate.GetECDsaPublicKey();
        return ecdsa != null;
    }

    /// <summary>
    /// Pobiera typ algorytmu certyfikatu
    /// </summary>
    public string GetCertificateAlgorithm(X509Certificate2 certificate)
    {
        if (IsRsaCertificate(certificate))
        {
            return "RSA";
        }
        else if (IsEcdsaCertificate(certificate))
        {
            return "ECDSA";
        }
        else
        {
            return "Unknown";
        }
    }
}
