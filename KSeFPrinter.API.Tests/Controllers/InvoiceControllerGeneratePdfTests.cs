using FluentAssertions;
using KSeFPrinter.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace KSeFPrinter.API.Tests.Controllers;

/// <summary>
/// Testy integracyjne dla InvoiceController - endpoint /generate-pdf
/// </summary>
public class InvoiceControllerGeneratePdfTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InvoiceControllerGeneratePdfTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GeneratePdf_Should_Return_Pdf_File_For_Offline_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false,
            ReturnFormat = "file"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        // Verify PDF signature
        var pdfSignature = Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF");
    }

    [Fact]
    public async Task GeneratePdf_Should_Return_Base64_Pdf_For_Offline_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false,
            ReturnFormat = "base64"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var result = await response.Content.ReadFromJsonAsync<GeneratePdfResponse>();
        result.Should().NotBeNull();
        result!.PdfBase64.Should().NotBeNullOrEmpty();
        result.FileSizeBytes.Should().BeGreaterThan(1000);
        result.InvoiceNumber.Should().NotBeNullOrEmpty();
        result.Mode.Should().Be("Offline");

        // Verify base64 can be decoded to valid PDF
        var pdfBytes = Convert.FromBase64String(result.PdfBase64);
        var pdfSignature = Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF");
    }

    [Fact]
    public async Task GeneratePdf_Should_Handle_Base64_Encoded_Xml()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlContent));
        var request = new GeneratePdfRequest
        {
            XmlContent = base64Content,
            IsBase64 = true,
            ValidateInvoice = false,
            ReturnFormat = "file"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdf_Should_Return_Error_For_Invalid_Xml()
    {
        // Arrange
        var invalidXml = "<Invalid>XML Content</NotMatching>";
        var request = new GeneratePdfRequest
        {
            XmlContent = invalidXml,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePdf_Should_Fail_Validation_When_Enabled()
    {
        // Arrange - użyjemy faktury z błędami walidacji
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = true // Włączona walidacja
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        // Powinna zwrócić błąd walidacji
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePdf_Should_Skip_Validation_When_Disabled()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false // Wyłączona walidacja
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePdf_Should_Accept_Custom_KSeF_Number()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var customKSeF = "1234567890-20251029-123456789ABC-01";
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            KSeFNumber = customKSeF,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePdf_Should_Use_Production_Mode()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false,
            UseProduction = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdf_Should_Use_Test_Mode_By_Default()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false
            // UseProduction = false (domyślnie)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePdf_Should_Handle_Empty_Request()
    {
        // Arrange
        var request = new GeneratePdfRequest
        {
            XmlContent = string.Empty,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePdf_Should_Handle_Malformed_Base64()
    {
        // Arrange
        var request = new GeneratePdfRequest
        {
            XmlContent = "NotValidBase64!!!",
            IsBase64 = true,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GeneratePdf_Should_Return_Pdf_With_Content_Disposition_Header()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false,
            ReturnFormat = "file"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition.FileName.Should().Contain("Faktura_");
        response.Content.Headers.ContentDisposition.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task GeneratePdf_Should_Generate_Different_Pdfs_For_Different_Invoices()
    {
        // Arrange
        var xmlPath1 = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlPath2 = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var xmlContent1 = await File.ReadAllTextAsync(xmlPath1);
        var xmlContent2 = await File.ReadAllTextAsync(xmlPath2);

        var request1 = new GeneratePdfRequest
        {
            XmlContent = xmlContent1,
            IsBase64 = false,
            ValidateInvoice = false
        };

        var request2 = new GeneratePdfRequest
        {
            XmlContent = xmlContent2,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request1);
        var response2 = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var pdf1 = await response1.Content.ReadAsByteArrayAsync();
        var pdf2 = await response2.Content.ReadAsByteArrayAsync();

        pdf1.Should().NotEqual(pdf2);
    }

    // Note: Certyfikaty są teraz wczytywane z appsettings.json przy starcie aplikacji,
    // nie są przekazywane w requeście API. API automatycznie wybiera certyfikat
    // na podstawie trybu faktury (ONLINE/OFFLINE).
    // Testy certyfikatów znajdują się w CertificateServiceTests.cs
}
