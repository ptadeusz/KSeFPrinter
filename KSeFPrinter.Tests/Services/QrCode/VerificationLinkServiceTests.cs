using FluentAssertions;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.QrCode;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Services.QrCode;

/// <summary>
/// Testy serwisu budowania linków weryfikacyjnych
/// </summary>
public class VerificationLinkServiceTests
{
    private readonly VerificationLinkService _service;

    public VerificationLinkServiceTests()
    {
        var cryptoService = new CryptographyService(
            NullLogger<CryptographyService>.Instance
        );
        _service = new VerificationLinkService(
            cryptoService,
            NullLogger<VerificationLinkService>.Instance
        );
    }

    [Fact]
    public void BuildInvoiceVerificationUrl_Should_Build_Correct_Url_For_Test_Environment()
    {
        // Arrange
        var nip = "1234567890";
        var date = new DateTime(2025, 10, 15);
        var xmlContent = "<test>xml</test>";

        // Act
        var url = _service.BuildInvoiceVerificationUrl(
            nip,
            date,
            xmlContent,
            numerKSeF: null,
            useProduction: false
        );

        // Assert
        url.Should().StartWith("https://ksef-test.mf.gov.pl/client-app/invoice/");
        url.Should().Contain(nip);
        url.Should().Contain("15-10-2025");
    }

    [Fact]
    public void BuildInvoiceVerificationUrl_Should_Build_Correct_Url_For_Production()
    {
        // Arrange
        var nip = "1234567890";
        var date = new DateTime(2025, 10, 15);
        var xmlContent = "<test>xml</test>";

        // Act
        var url = _service.BuildInvoiceVerificationUrl(
            nip,
            date,
            xmlContent,
            numerKSeF: null,
            useProduction: true
        );

        // Assert
        url.Should().StartWith("https://ksef.mf.gov.pl/client-app/invoice/");
        url.Should().Contain(nip);
    }

    [Fact]
    public void BuildInvoiceVerificationUrl_Should_Include_Sha256_Hash()
    {
        // Arrange
        var nip = "1234567890";
        var date = new DateTime(2025, 10, 15);
        var xmlContent = "<test>xml</test>";

        // Act
        var url = _service.BuildInvoiceVerificationUrl(
            nip,
            date,
            xmlContent
        );

        // Assert
        // URL powinien składać się z wielu części oddzielonych /
        var parts = url.Split('/');
        parts.Length.Should().BeGreaterThanOrEqualTo(7); // https: // domain / client-app / invoice / nip / date / hash

        // Ostatnia część to hash Base64URL (nie zawiera +/=)
        var hash = parts.Last();
        hash.Should().NotContain("+");
        hash.Should().NotContain("/");
        hash.Should().NotContain("=");
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678901")]
    public void BuildInvoiceVerificationUrl_Should_Throw_For_Invalid_Nip(string invalidNip)
    {
        // Arrange
        var date = new DateTime(2025, 10, 15);
        var xmlContent = "<test>xml</test>";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.BuildInvoiceVerificationUrl(
                invalidNip,
                date,
                xmlContent
            )
        );
    }

    [Fact]
    public void BuildInvoiceVerificationUrl_Should_Format_Date_Correctly()
    {
        // Arrange
        var nip = "1234567890";
        var date = new DateTime(2025, 1, 5); // Single digit day and month
        var xmlContent = "<test>xml</test>";

        // Act
        var url = _service.BuildInvoiceVerificationUrl(
            nip,
            date,
            xmlContent
        );

        // Assert
        url.Should().Contain("05-01-2025"); // DD-MM-RRRR with leading zeros
    }

    [Fact]
    public void BuildInvoiceVerificationUrl_Should_Work_With_Different_Xml_Content()
    {
        // Arrange
        var nip = "1234567890";
        var date = new DateTime(2025, 10, 15);
        var xml1 = "<test>xml1</test>";
        var xml2 = "<test>xml2</test>";

        // Act
        var url1 = _service.BuildInvoiceVerificationUrl(nip, date, xml1);
        var url2 = _service.BuildInvoiceVerificationUrl(nip, date, xml2);

        // Assert - różne XML powinny dać różne hashe
        url1.Should().NotBe(url2);
    }

    [Fact]
    public void BuildInvoiceVerificationUrl_Should_Be_Deterministic()
    {
        // Arrange
        var nip = "1234567890";
        var date = new DateTime(2025, 10, 15);
        var xmlContent = "<test>xml</test>";

        // Act
        var url1 = _service.BuildInvoiceVerificationUrl(nip, date, xmlContent);
        var url2 = _service.BuildInvoiceVerificationUrl(nip, date, xmlContent);

        // Assert - ten sam input powinien dać ten sam URL
        url1.Should().Be(url2);
    }
}
