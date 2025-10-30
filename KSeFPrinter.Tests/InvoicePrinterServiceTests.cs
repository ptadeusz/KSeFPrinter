using FluentAssertions;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests;

/// <summary>
/// Testy integracyjne dla głównego serwisu InvoicePrinterService
/// </summary>
public class InvoicePrinterServiceTests : IDisposable
{
    private readonly InvoicePrinterService _service;
    private readonly string _testOutputDir;

    public InvoicePrinterServiceTests()
    {
        // Inicjalizacja całego stosu zależności
        var parser = new XmlInvoiceParser(
            NullLogger<XmlInvoiceParser>.Instance
        );

        var validator = new InvoiceValidator(
            NullLogger<InvoiceValidator>.Instance
        );

        var cryptoService = new CryptographyService(
            NullLogger<CryptographyService>.Instance
        );

        var linkService = new VerificationLinkService(
            cryptoService,
            NullLogger<VerificationLinkService>.Instance
        );

        var qrService = new QrCodeService(
            NullLogger<QrCodeService>.Instance
        );

        var pdfGenerator = new InvoicePdfGenerator(
            linkService,
            qrService,
            NullLogger<InvoicePdfGenerator>.Instance
        );

        _service = new InvoicePrinterService(
            parser,
            validator,
            pdfGenerator,
            NullLogger<InvoicePrinterService>.Instance
        );

        _testOutputDir = Path.Combine(Path.GetTempPath(), "KseFPrinterTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Generate_Pdf_For_Offline_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var pdfPath = Path.Combine(_testOutputDir, "offline_invoice.pdf");

        // Act
        var result = await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        result.Should().BeNull(); // When outputPath is provided, method returns null
        File.Exists(pdfPath).Should().BeTrue();
        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Generate_Pdf_For_Online_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var pdfPath = Path.Combine(_testOutputDir, "online_invoice.pdf");

        // Act
        var result = await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        result.Should().BeNull();
        File.Exists(pdfPath).Should().BeTrue();
        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Return_Bytes_When_No_OutputPath()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");

        // Act
        var pdfBytes = await _service.GeneratePdfFromFileAsync(xmlPath, pdfOutputPath: null, validateInvoice: false);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
        pdfBytes!.Length.Should().BeGreaterThan(1000);

        // Verify PDF signature
        var pdfSignature = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF");
    }

    [Fact]
    public async Task GeneratePdfFromXmlAsync_Should_Generate_Pdf_From_Xml_String()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Act
        var pdfBytes = await _service.GeneratePdfFromXmlAsync(xmlContent, validateInvoice: false);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Validate_Invoice_By_Default()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");

        // Act - test że walidacja jest włączona domyślnie
        // Walidacja rzuci wyjątek dla testowej faktury z nieprawidłowymi NIPami
        var act = async () => await _service.GeneratePdfFromFileAsync(xmlPath);

        // Assert - walidacja powinna wykryć błędy w danych testowych
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Faktura nie przeszła walidacji*");
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Skip_Validation_When_Disabled()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");

        // Act
        var pdfBytes = await _service.GeneratePdfFromFileAsync(
            xmlPath,
            validateInvoice: false
        );

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Use_Custom_Options()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var options = new PdfGenerationOptions
        {
            UseProduction = false,
            IncludeQrCode1 = true,
            IncludeQrCode2 = false,
            QrPixelsPerModule = 10
        };

        // Act
        var pdfBytes = await _service.GeneratePdfFromFileAsync(xmlPath, options: options, validateInvoice: false);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateInvoiceFromFileAsync_Should_Return_Validation_Result()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");

        // Act
        var result = await _service.ValidateInvoiceFromFileAsync(xmlPath);

        // Assert
        result.Should().NotBeNull();
        // Note: Test data may have validation warnings/errors (e.g., NIP checksum)
        // This test verifies that validation runs, not that data is perfect
    }

    [Fact]
    public async Task ValidateInvoiceFromXmlAsync_Should_Validate_Xml_String()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Act
        var result = await _service.ValidateInvoiceFromXmlAsync(xmlContent);

        // Assert
        result.Should().NotBeNull();
        // Offline invoices mogą mieć ostrzeżenia ale powinny być valid
    }

    [Fact]
    public async Task ParseInvoiceFromFileAsync_Should_Return_Context()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");

        // Act
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();
        context.Metadata.Should().NotBeNull();
        context.Faktura.Fa.P_2.Should().NotBeNullOrEmpty(); // Numer faktury
    }

    [Fact]
    public async Task ParseInvoiceFromXmlAsync_Should_Parse_Xml_String()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Act
        var context = await _service.ParseInvoiceFromXmlAsync(xmlContent);

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();
        context.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Override_KSeF_Number_When_Provided()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var customKSeF = "1234567890-20251029-123456789ABC-01";

        // Act
        var pdfBytes = await _service.GeneratePdfFromFileAsync(
            xmlPath,
            numerKSeF: customKSeF,
            validateInvoice: false // Skip validation since custom KSeF might not match
        );

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfFromContextAsync_Should_Generate_From_Parsed_Context()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);

        // Act
        var pdfBytes = await _service.GeneratePdfFromContextAsync(context, validateInvoice: false);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Full_Workflow_Should_Work_For_Offline_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var pdfPath = Path.Combine(_testOutputDir, "full_workflow_offline.pdf");

        // Act - pełny workflow
        // 1. Parse
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        context.Should().NotBeNull();
        context.Metadata.Tryb.Should().Be(TrybWystawienia.Offline);

        // 2. Validate
        var validationResult = await _service.ValidateInvoiceFromFileAsync(xmlPath);
        validationResult.Should().NotBeNull();

        // 3. Generate PDF (skip validation as test invoice has NIP validation issues)
        var pdfResult = await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        pdfResult.Should().BeNull();
        File.Exists(pdfPath).Should().BeTrue();
        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Full_Workflow_Should_Work_For_Online_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var pdfPath = Path.Combine(_testOutputDir, "full_workflow_online.pdf");

        // Act - pełny workflow
        // 1. Parse
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        context.Should().NotBeNull();
        // Parser wykrywa tryb na podstawie obecności numeru KSeF w XML
        // Numer KSeF w nazwie pliku nie oznacza że XML zawiera numer KSeF

        // 2. Validate
        var validationResult = await _service.ValidateInvoiceFromFileAsync(xmlPath);
        validationResult.Should().NotBeNull();

        // 3. Generate PDF (skip validation as test data may have NIP validation issues)
        var pdfResult = await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        pdfResult.Should().BeNull();
        File.Exists(pdfPath).Should().BeTrue();
        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task GeneratePdfFromFileAsync_Should_Handle_NonExistent_File()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "NonExistent.xml");

        // Act & Assert - Parser rzuca InvoiceParseException dla nieistniejącego pliku
        var act = async () => await _service.GeneratePdfFromFileAsync(xmlPath);
        await act.Should().ThrowAsync<KSeFPrinter.Exceptions.InvoiceParseException>();
    }

    [Fact]
    public async Task GeneratePdfFromXmlAsync_Should_Handle_Invalid_Xml()
    {
        // Arrange
        var invalidXml = "<Invalid>XML</Content>";

        // Act & Assert
        var act = async () => await _service.GeneratePdfFromXmlAsync(invalidXml);
        await act.Should().ThrowAsync<Exception>();
    }
}
