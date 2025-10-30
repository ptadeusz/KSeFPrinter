using FluentAssertions;
using KSeFPrinter.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace KSeFPrinter.API.Tests.Controllers;

/// <summary>
/// Testy integracyjne dla InvoiceController - endpoint /parse
/// </summary>
public class InvoiceControllerParseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InvoiceControllerParseTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ParseInvoice_Should_Return_Parsed_Invoice_Data_For_Offline_Invoice()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().NotBeNullOrEmpty();
        result.IssueDate.Should().BeAfter(DateTime.MinValue);
        result.Mode.Should().Be("Offline");
        result.Seller.Should().NotBeNull();
        result.Seller.Name.Should().NotBeNullOrEmpty();
        result.Seller.Nip.Should().NotBeNullOrEmpty();
        result.Buyer.Should().NotBeNull();
        result.Buyer.Name.Should().NotBeNullOrEmpty();
        result.Lines.Should().NotBeEmpty();
        result.Summary.Should().NotBeNull();
        result.Summary.TotalGross.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseInvoice_Should_Return_Parsed_Invoice_Data_For_Online_Invoice()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().NotBeNullOrEmpty();
        result.Seller.Should().NotBeNull();
        result.Buyer.Should().NotBeNull();
        result.Lines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseInvoice_Should_Handle_Base64_Encoded_Xml()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseInvoice_Should_Return_Error_For_Invalid_Xml()
    {
        // Arrange
        var invalidXml = "<Invalid>XML Content</NotMatching>";
        var request = new InvoiceValidationRequest
        {
            XmlContent = invalidXml,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ParseInvoice_Should_Include_Payment_Information()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.Payment.Should().NotBeNull();
        // Jeśli faktura ma informacje o płatności
    }

    [Fact]
    public async Task ParseInvoice_Should_Include_All_Invoice_Lines()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.Lines.Should().NotBeEmpty();
        result.Lines.Should().AllSatisfy(line =>
        {
            line.LineNumber.Should().BeGreaterThan(0);
            line.Description.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task ParseInvoice_Should_Include_Currency_Information()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.Summary.Currency.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseInvoice_Should_Accept_Custom_KSeF_Number()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.KSeFNumber.Should().Be(customKSeF);
    }

    [Fact]
    public async Task ParseInvoice_Should_Handle_Empty_Request()
    {
        // Arrange
        var request = new InvoiceValidationRequest
        {
            XmlContent = string.Empty,
            IsBase64 = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ParseInvoice_Should_Include_Contact_Information()
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
        var response = await _client.PostAsJsonAsync("/api/invoice/parse", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceParseResponse>();
        result.Should().NotBeNull();
        result!.Seller.Should().NotBeNull();
        result.Buyer.Should().NotBeNull();
        // Email i telefon mogą być opcjonalne
    }
}
