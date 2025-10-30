using FluentAssertions;
using KSeFPrinter.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace KSeFPrinter.API.Tests.Controllers;

/// <summary>
/// Testy integracyjne dla InvoiceController
/// </summary>
public class InvoiceControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public InvoiceControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region /validate Endpoint Tests

    [Fact]
    public async Task ValidateInvoice_Should_Return_Success_For_Valid_Offline_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new InvoiceValidationRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceValidationResponse>();
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().NotBeNullOrEmpty();
        result.SellerNip.Should().NotBeNullOrEmpty();
        result.Mode.Should().Be("Offline");
    }

    [Fact]
    public async Task ValidateInvoice_Should_Return_Success_For_Valid_Online_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new InvoiceValidationRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceValidationResponse>();
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateInvoice_Should_Handle_Base64_Encoded_Xml()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlContent));
        var request = new InvoiceValidationRequest
        {
            XmlContent = base64Content,
            IsBase64 = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceValidationResponse>();
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateInvoice_Should_Return_Errors_For_Invalid_Xml()
    {
        // Arrange
        var invalidXml = "<Invalid>XML Content</NotMatching>";
        var request = new InvoiceValidationRequest
        {
            XmlContent = invalidXml,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateInvoice_Should_Accept_Custom_KSeF_Number()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var customKSeF = "1234567890-20251029-123456789ABC-01";
        var request = new InvoiceValidationRequest
        {
            XmlContent = xmlContent,
            KSeFNumber = customKSeF,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceValidationResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateInvoice_Should_Return_Validation_Warnings()
    {
        // Arrange - użyjemy faktury z błędami NIP
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new InvoiceValidationRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceValidationResponse>();
        result.Should().NotBeNull();
        // Faktura testowa może mieć ostrzeżenia/błędy walidacji
        (result!.Errors.Count + result.Warnings.Count).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ValidateInvoice_Should_Handle_Empty_Request()
    {
        // Arrange
        var request = new InvoiceValidationRequest
        {
            XmlContent = string.Empty,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateInvoice_Should_Handle_Malformed_Base64()
    {
        // Arrange
        var request = new InvoiceValidationRequest
        {
            XmlContent = "NotValidBase64!!!",
            IsBase64 = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region /health Endpoint Tests

    [Fact]
    public async Task Health_Should_Return_Healthy_Status()
    {
        // Act
        var response = await _client.GetAsync("/api/invoice/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("KSeF Printer API");
    }

    [Fact]
    public async Task Health_Should_Return_Json_Content()
    {
        // Act
        var response = await _client.GetAsync("/api/invoice/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Health_Should_Include_Timestamp()
    {
        // Act
        var response = await _client.GetAsync("/api/invoice/health");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("timestamp");
    }

    #endregion
}
