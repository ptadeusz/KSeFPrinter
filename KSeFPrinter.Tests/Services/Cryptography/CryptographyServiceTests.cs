using FluentAssertions;
using KSeFPrinter.Services.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Services.Cryptography;

/// <summary>
/// Testy serwisu kryptograficznego
/// </summary>
public class CryptographyServiceTests
{
    private readonly CryptographyService _service;

    public CryptographyServiceTests()
    {
        _service = new CryptographyService(NullLogger<CryptographyService>.Instance);
    }

    [Fact]
    public void ComputeSha256Hash_Should_Return_32_Bytes()
    {
        // Arrange
        var data = "test data"u8.ToArray();

        // Act
        var hash = _service.ComputeSha256Hash(data);

        // Assert
        hash.Should().HaveCount(32); // SHA-256 always produces 32 bytes
    }

    [Fact]
    public void ComputeSha256Hash_Should_Be_Deterministic()
    {
        // Arrange
        var data = "test data"u8.ToArray();

        // Act
        var hash1 = _service.ComputeSha256Hash(data);
        var hash2 = _service.ComputeSha256Hash(data);

        // Assert
        hash1.Should().Equal(hash2);
    }

    [Fact]
    public void ComputeSha256Hash_From_String_Should_Work()
    {
        // Arrange
        var text = "test data";

        // Act
        var hash = _service.ComputeSha256Hash(text);

        // Assert
        hash.Should().HaveCount(32);
    }

    [Fact]
    public void ComputeSha256HashBase64Url_Should_Return_Valid_Base64Url()
    {
        // Arrange
        var data = "test data"u8.ToArray();

        // Act
        var hash = _service.ComputeSha256HashBase64Url(data);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotContain("+");
        hash.Should().NotContain("/");
        hash.Should().NotContain("=");
    }

    [Fact]
    public void ToBase64Url_Should_Convert_Properly()
    {
        // Arrange
        var data = new byte[] { 0xFF, 0xFE, 0xFD };

        // Act
        var base64Url = _service.ToBase64Url(data);

        // Assert
        base64Url.Should().NotBeNullOrEmpty();
        base64Url.Should().NotContain("+");
        base64Url.Should().NotContain("/");
        base64Url.Should().NotEndWith("=");
    }

    [Fact]
    public void ToBase64Url_And_FromBase64Url_Should_Roundtrip()
    {
        // Arrange
        var originalData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF, 0xFE, 0xFD };

        // Act
        var base64Url = _service.ToBase64Url(originalData);
        var decodedData = _service.FromBase64Url(base64Url);

        // Assert
        decodedData.Should().Equal(originalData);
    }

    [Theory]
    [InlineData("")]
    [InlineData("test")]
    [InlineData("https://ksef-test.mf.gov.pl/client-app/certificate/nip/1234567890/1234567890/ABC123/hash")]
    public void ComputeSha256HashBase64Url_Should_Work_For_Various_Strings(string input)
    {
        // Act
        var hash = _service.ComputeSha256HashBase64Url(input);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ToBase64Url_Should_Replace_Special_Characters()
    {
        // Arrange - dane, które w Base64 dadzą '+' i '/'
        var data = new byte[] { 0xFB, 0xFF, 0xBF }; // To da /7+/ w Base64

        // Act
        var base64Url = _service.ToBase64Url(data);

        // Assert
        base64Url.Should().NotContain("+");
        base64Url.Should().NotContain("/");
        // Base64URL powinien zawierać '-' lub '_' (zamienione z '+' i '/')
        var hasSpecialChars = base64Url.Contains('-') || base64Url.Contains('_');
        hasSpecialChars.Should().BeTrue();
    }

    [Fact]
    public void FromBase64Url_Should_Handle_Missing_Padding()
    {
        // Arrange
        var base64UrlWithoutPadding = "YWJj"; // "abc" in Base64 without padding

        // Act
        var decoded = _service.FromBase64Url(base64UrlWithoutPadding);

        // Assert
        decoded.Should().NotBeEmpty();
    }
}
