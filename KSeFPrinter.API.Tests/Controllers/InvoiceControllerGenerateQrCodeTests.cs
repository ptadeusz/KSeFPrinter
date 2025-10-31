using FluentAssertions;
using KSeFPrinter.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace KSeFPrinter.API.Tests.Controllers;

/// <summary>
/// Testy integracyjne dla endpointu generate-qr-code
/// </summary>
public class InvoiceControllerGenerateQrCodeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public InvoiceControllerGenerateQrCodeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GenerateQrCode_Should_Return_SVG_For_Valid_KSeF_Number()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "6511153259-20251015-010020140418-0D",
            UseProduction = false,
            Format = "svg",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
        result.Should().NotBeNull();
        result!.KSeFNumber.Should().Be(request.KSeFNumber);
        result.QrCodeSvg.Should().NotBeNullOrEmpty();
        result.QrCodeSvg.Should().Contain("<svg");
        result.QrCodeBase64.Should().BeNull();
        result.Format.Should().Be("svg");
        result.VerificationUrl.Should().Contain("ksef-test.mf.gov.pl");
        result.VerificationUrl.Should().Contain(request.KSeFNumber);
        result.Environment.Should().Be("test");
    }

    [Fact]
    public async Task GenerateQrCode_Should_Return_Base64_For_Valid_KSeF_Number()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "6511153259-20251015-010020140418-0D",
            UseProduction = false,
            Format = "base64",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
        result.Should().NotBeNull();
        result!.KSeFNumber.Should().Be(request.KSeFNumber);
        result.QrCodeBase64.Should().NotBeNullOrEmpty();
        result.QrCodeSvg.Should().BeNull();
        result.Format.Should().Be("base64");
        result.VerificationUrl.Should().Contain("ksef-test.mf.gov.pl");
        result.Environment.Should().Be("test");
    }

    [Fact]
    public async Task GenerateQrCode_Should_Return_Both_Formats_When_Requested()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "6511153259-20251015-010020140418-0D",
            UseProduction = false,
            Format = "both",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
        result.Should().NotBeNull();
        result!.KSeFNumber.Should().Be(request.KSeFNumber);
        result.QrCodeSvg.Should().NotBeNullOrEmpty();
        result.QrCodeSvg.Should().Contain("<svg");
        result.QrCodeBase64.Should().NotBeNullOrEmpty();
        result.Format.Should().Be("both");
        result.VerificationUrl.Should().Contain("ksef-test.mf.gov.pl");
    }

    [Fact]
    public async Task GenerateQrCode_Should_Use_Production_URL_When_Requested()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "6511153259-20251015-010020140418-0D",
            UseProduction = true,
            Format = "svg",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
        result.Should().NotBeNull();
        result!.VerificationUrl.Should().Contain("ksef.mf.gov.pl");
        result.VerificationUrl.Should().NotContain("-test");
        result.Environment.Should().Be("production");
    }

    [Fact]
    public async Task GenerateQrCode_Should_Return_BadRequest_For_Invalid_KSeF_Number()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "invalid-ksef-number",
            UseProduction = false,
            Format = "svg",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateQrCode_Should_Return_BadRequest_For_Empty_KSeF_Number()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "",
            UseProduction = false,
            Format = "svg",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateQrCode_Should_Return_BadRequest_For_Invalid_Format()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "6511153259-20251015-010020140418-0D",
            UseProduction = false,
            Format = "invalid-format",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Nieprawidłowy format");
    }

    [Fact]
    public async Task GenerateQrCode_Should_Handle_Different_Sizes()
    {
        // Arrange
        var sizes = new[] { 100, 200, 400 };

        foreach (var size in sizes)
        {
            var request = new GenerateQrCodeRequest
            {
                KSeFNumber = "6511153259-20251015-010020140418-0D",
                UseProduction = false,
                Format = "base64",
                Size = size
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
            result.Should().NotBeNull();
            result!.Size.Should().Be(size);
            result.QrCodeBase64.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GenerateQrCode_Should_Work_With_Valid_KSeF_Number_With_CRC()
    {
        // Arrange - używamy numeru z poprawną sumą kontrolną CRC-8
        var ksefNumber = "6511153259-20251015-010020140418-0D";
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = ksefNumber,
            UseProduction = false,
            Format = "svg",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
        result.Should().NotBeNull();
        result!.KSeFNumber.Should().Be(ksefNumber);
        result.VerificationUrl.Should().Contain(ksefNumber);
    }

    [Theory]
    [InlineData("123")]  // Za krótki
    [InlineData("ABCD-EFGH-IJKL-MNOP")]  // Niewłaściwy format
    [InlineData("1234567890-2025010X-ABCDEF123456-AB")]  // Nieprawidłowa data
    [InlineData("123456789-20250101-ABCDEF123456-AB")]  // Za mało cyfr NIP
    public async Task GenerateQrCode_Should_Return_BadRequest_For_Invalid_Formats(string ksefNumber)
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = ksefNumber,
            UseProduction = false,
            Format = "svg",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateQrCode_SVG_Should_Be_Valid_XML()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "6511153259-20251015-010020140418-0D",
            UseProduction = false,
            Format = "svg",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
        result.Should().NotBeNull();
        result!.QrCodeSvg.Should().NotBeNullOrEmpty();

        // Sprawdź czy SVG jest poprawnym XML
        result.QrCodeSvg.Should().StartWith("<svg");
        result.QrCodeSvg.Should().Contain("xmlns");
        result.QrCodeSvg.Should().EndWith("</svg>");
    }

    [Fact]
    public async Task GenerateQrCode_Base64_Should_Be_Valid_Base64()
    {
        // Arrange
        var request = new GenerateQrCodeRequest
        {
            KSeFNumber = "6511153259-20251015-010020140418-0D",
            UseProduction = false,
            Format = "base64",
            Size = 200
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-qr-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GenerateQrCodeResponse>();
        result.Should().NotBeNull();
        result!.QrCodeBase64.Should().NotBeNullOrEmpty();

        // Sprawdź czy Base64 jest poprawny
        var act = () => Convert.FromBase64String(result.QrCodeBase64!);
        act.Should().NotThrow();

        var bytes = Convert.FromBase64String(result.QrCodeBase64!);
        bytes.Length.Should().BeGreaterThan(0);
    }
}
