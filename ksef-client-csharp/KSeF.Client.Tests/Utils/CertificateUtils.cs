using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Api.Builders.Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;

namespace KSeF.Client.Tests.Utils;

/// <summary>
/// Zestaw metod pomocniczych do obsługi cyklu życia certyfikatów w testach:
/// generowanie CSR i klucza prywatnego, wysyłka wniosku certyfikacyjnego,
/// unieważnianie certyfikatu oraz łączenie certyfikatu z kluczem prywatnym.
/// </summary>
internal static class CertificateUtils
{
    /// <summary>
    /// Generuje CSR (Certificate Signing Request) oraz klucz prywatny w formacie Base64.
    /// </summary>
    /// <remarks>
    /// Najpierw pobiera dane niezbędne do wygenerowania CSR z API KSeF, a następnie
    /// używa <see cref="ICryptographyService"/> do przygotowania CSR z wykorzystaniem RSA.
    /// </remarks>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="cryptographyService">Usługa kryptograficzna do generowania CSR.</param>
    /// <returns>Krotka: (csrBase64Encoded, privateKeyBase64Encoded) — oba w Base64.</returns>
    /// <exception cref="Exception">Wyjątki mogą pochodzić z warstwy API lub kryptograficznej.</exception>
    internal static async Task<(string csrBase64Encoded, string privateKeyBase64Encoded)> GenerateCsrAndPrivateKeyAsync(IKSeFClient ksefClient, string accessToken, ICryptographyService cryptographyService, RSASignaturePadding padding)
    {
        var enrollmentData = await ksefClient
            .GetCertificateEnrollmentDataAsync(accessToken)
            .ConfigureAwait(false);

            return cryptographyService.GenerateCsrWithRsa(enrollmentData, padding);
        }

    /// <summary>
    /// Wysyła wniosek certyfikacyjny (CSR) do KSeF i zwraca numer referencyjny operacji.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="csrBase64Encoded">CSR w Base64 (DER).</param>
    /// <param name="certificateType">Typ certyfikatu (domyślnie Authentication).</param>
    /// <returns>Odpowiedź z numerem referencyjnym i znacznikiem czasu.</returns>
    internal static async Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(IKSeFClient ksefClient, string accessToken, string csrBase64Encoded, CertificateType certificateType = CertificateType.Authentication)
    {
        var request = SendCertificateEnrollmentRequestBuilder.Create()
                   .WithCertificateName("Test Certificate")
                   .WithCertificateType(certificateType)
                   .WithCsr(csrBase64Encoded)
                   .WithValidFrom(DateTimeOffset.UtcNow)
                   .Build();

        var certificateEnrollmentResponse = await ksefClient.SendCertificateEnrollmentAsync(request, accessToken)
            .ConfigureAwait(false);

        return certificateEnrollmentResponse;
    }

    /// <summary>
    /// Unieważnia certyfikat o podanym numerze seryjnym.
    /// </summary>
    /// <param name="ksefClient">Klient KSeF do komunikacji z API.</param>
    /// <param name="accessToken">Token dostępu.</param>
    /// <param name="certificateSerialNumber">Numer seryjny certyfikatu do unieważnienia.</param>
    internal static async Task RevokeCertificateAsync(IKSeFClient ksefClient, string accessToken, string certificateSerialNumber)
    {
        var request = RevokeCertificateRequestBuilder.Create()
            .Build();

        await ksefClient.RevokeCertificateAsync(request, certificateSerialNumber, accessToken)
             .ConfigureAwait(false);
    }

    /// <summary>
    /// Tworzy egzemplarz <see cref="X509Certificate2"/> zawierający klucz prywatny
    /// na podstawie odpowiedzi z API (DER w Base64) oraz klucza prywatnego (Base64).
    /// </summary>
    /// <param name="response">Odpowiedź z certyfikatem w Base64 (DER).</param>
    /// <param name="privateKeyBase64Encoded">Klucz prywatny RSA w Base64.</param>
    /// <returns>Certyfikat X509 połączony z kluczem prywatnym.</returns>
    /// <exception cref="FormatException">Gdy dane Base64 mają niepoprawny format.</exception>
    /// <exception cref="CryptographicException">Gdy import klucza prywatnego nie powiedzie się.</exception>
    internal static X509Certificate2 CreateCertificateWithPrivateKey(CertificateResponse response, string privateKeyBase64Encoded)
    {
        byte[] certBytes = Convert.FromBase64String(response.Certificate);
        var certificate = new X509Certificate2(certBytes);

        using RSA rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64Encoded), out _);

        return certificate.CopyWithPrivateKey(rsa);
    }
}