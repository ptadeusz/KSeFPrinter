using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KSeFPrinter.Services.Cryptography;
using Microsoft.Extensions.Logging;

namespace KSeFPrinter.Services.QrCode;

/// <summary>
/// Serwis do budowania linków weryfikacyjnych dla kodów QR KSeF
/// </summary>
public class VerificationLinkService
{
    private readonly CryptographyService _cryptographyService;
    private readonly ILogger<VerificationLinkService> _logger;

    // URL-e środowisk KSeF
    private const string ProductionBaseUrl = "https://ksef.mf.gov.pl/client-app";
    private const string TestBaseUrl = "https://ksef-test.mf.gov.pl/client-app";

    public VerificationLinkService(
        CryptographyService cryptographyService,
        ILogger<VerificationLinkService> logger)
    {
        _cryptographyService = cryptographyService;
        _logger = logger;
    }

    /// <summary>
    /// Buduje URL weryfikacyjny dla faktury (KOD QR I)
    /// Format: https://ksef-test.mf.gov.pl/client-app/invoice/{NIP}/{DD-MM-RRRR}/{SHA256-Base64URL}
    /// </summary>
    /// <param name="nip">NIP sprzedawcy (10 cyfr)</param>
    /// <param name="dataWystawienia">Data wystawienia faktury</param>
    /// <param name="xmlContent">Zawartość XML faktury do hashowania</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF (jeśli null to tryb offline)</param>
    /// <param name="useProduction">Czy użyć środowiska produkcyjnego (domyślnie: false - test)</param>
    /// <returns>Pełny URL weryfikacyjny</returns>
    public string BuildInvoiceVerificationUrl(
        string nip,
        DateTime dataWystawienia,
        string xmlContent,
        string? numerKSeF = null,
        bool useProduction = false)
    {
        if (string.IsNullOrWhiteSpace(nip) || nip.Length != 10)
        {
            throw new ArgumentException("NIP musi mieć dokładnie 10 cyfr", nameof(nip));
        }

        // Oblicz SHA-256 z XML i zakoduj do Base64URL
        var sha256Hash = _cryptographyService.ComputeSha256HashBase64Url(xmlContent);

        // Format daty: DD-MM-RRRR
        var dateString = dataWystawienia.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        // Wybierz base URL
        var baseUrl = useProduction ? ProductionBaseUrl : TestBaseUrl;

        // Zbuduj URL
        var url = $"{baseUrl}/invoice/{nip}/{dateString}/{sha256Hash}";

        _logger.LogDebug(
            "Zbudowano URL weryfikacyjny faktury: NIP={NIP}, Data={Date}, KSeF={KSeF}, Env={Env}",
            nip, dateString, numerKSeF ?? "OFFLINE", useProduction ? "PROD" : "TEST"
        );

        return url;
    }

    /// <summary>
    /// Buduje URL weryfikacyjny certyfikatu (KOD QR II)
    /// Format: https://ksef-test.mf.gov.pl/client-app/certificate/{IdentifierType}/{IdentifierValue}/{NIP}/{CertificateSerial}/{SHA256-Base64URL}/{Signature-Base64URL}
    /// </summary>
    /// <param name="certificate">Certyfikat offline wystawcy</param>
    /// <param name="nip">NIP sprzedawcy</param>
    /// <param name="identifierType">Typ identyfikatora (np. "nip", "pesel")</param>
    /// <param name="identifierValue">Wartość identyfikatora</param>
    /// <param name="xmlContent">Zawartość XML faktury do hashowania</param>
    /// <param name="useProduction">Czy użyć środowiska produkcyjnego</param>
    /// <returns>Pełny URL weryfikacyjny certyfikatu z podpisem</returns>
    public string BuildCertificateVerificationUrl(
        X509Certificate2 certificate,
        string nip,
        string identifierType,
        string identifierValue,
        string xmlContent,
        bool useProduction = false)
    {
        if (!certificate.HasPrivateKey)
        {
            throw new ArgumentException("Certyfikat musi zawierać klucz prywatny do podpisania", nameof(certificate));
        }

        if (string.IsNullOrWhiteSpace(nip) || nip.Length != 10)
        {
            throw new ArgumentException("NIP musi mieć dokładnie 10 cyfr", nameof(nip));
        }

        // Oblicz SHA-256 z XML i zakoduj do Base64URL
        var sha256Hash = _cryptographyService.ComputeSha256HashBase64Url(xmlContent);

        // Pobierz numer seryjny certyfikatu (hex)
        var certificateSerial = certificate.SerialNumber;

        // Wybierz base URL
        var baseUrl = useProduction ? ProductionBaseUrl : TestBaseUrl;

        // Zbuduj ciąg do podpisu (bez https:// i bez trailing /)
        var stringToSign = $"{baseUrl.Replace("https://", "")}/certificate/{identifierType}/{identifierValue}/{nip}/{certificateSerial}/{sha256Hash}";

        _logger.LogDebug("Ciąg do podpisu: {StringToSign}", stringToSign);

        // Wygeneruj podpis
        string signatureBase64Url;
        if (_cryptographyService.IsRsaCertificate(certificate))
        {
            _logger.LogDebug("Generowanie podpisu RSA-PSS");
            signatureBase64Url = _cryptographyService.SignWithRsaPssBase64Url(stringToSign, certificate);
        }
        else if (_cryptographyService.IsEcdsaCertificate(certificate))
        {
            _logger.LogDebug("Generowanie podpisu ECDSA P-256");
            signatureBase64Url = _cryptographyService.SignWithEcdsaBase64Url(stringToSign, certificate);
        }
        else
        {
            throw new CryptographicException("Certyfikat musi być typu RSA lub ECDSA");
        }

        // Zbuduj pełny URL z podpisem
        var fullUrl = $"https://{stringToSign}/{signatureBase64Url}";

        _logger.LogInformation(
            "Zbudowano URL weryfikacyjny certyfikatu: Type={Type}, NIP={NIP}, Algo={Algo}",
            identifierType, nip, _cryptographyService.GetCertificateAlgorithm(certificate)
        );

        return fullUrl;
    }

    /// <summary>
    /// Pomocnicza metoda - buduje URL weryfikacyjny certyfikatu z domyślnym identifierType=nip
    /// </summary>
    public string BuildCertificateVerificationUrl(
        X509Certificate2 certificate,
        string nip,
        string xmlContent,
        bool useProduction = false)
    {
        return BuildCertificateVerificationUrl(
            certificate,
            nip,
            "nip",
            nip,
            xmlContent,
            useProduction
        );
    }
}
