using KSeF.Client.Api.Services;
using KSeF.Client.DI;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests.Features;

public class QrCodeTests
{
    private readonly VerificationLinkService _linkSvc;

    public QrCodeTests()
    {
        _linkSvc = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefEnviromentsUris.TEST });
    }

    [Theory]
    [InlineData("RSA", "TestRsaPublic")]
    [InlineData("ECC", "TestEccPublic")]
    [Trait("Category", "QRCode")]
    [Trait("Scenario", "Zbuduj link weryfikujący uzywając publiczny certyfikat")]
    [Trait("Expected", "InvalidOperationException")]
    public void GivenPublicOnlyCertificate_WhenBuildingVerificationUrl_ThenThrowsInvalidOperationException(
        string keyType, string subjectName)
    {
        // Arrange
        X509Certificate2 publicCert;
        if (keyType == "RSA")
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
            publicCert = new X509Certificate2(cert.Export(X509ContentType.Cert));
        }
        else
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest($"CN={subjectName}", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
            publicCert = new X509Certificate2(cert.Export(X509ContentType.Cert));
        }

        var nip = "0000000000";
        var xml = "<x/>";
        var serial = Guid.NewGuid().ToString();
        string invoiceHash;
        using (var sha256 = SHA256.Create())
            invoiceHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(xml)));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _linkSvc.BuildCertificateVerificationUrl(
                nip,
                Core.Models.QRCode.ContextIdentifierType.Nip,
                nip,
                serial,
                invoiceHash,
                publicCert
            )
        );
    }

}
