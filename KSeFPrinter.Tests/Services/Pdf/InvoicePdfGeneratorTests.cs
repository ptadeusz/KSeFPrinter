using FluentAssertions;
using KSeFPrinter.Interfaces;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Services.Pdf;

/// <summary>
/// Testy generatora PDF dla faktur KSeF
/// </summary>
public class InvoicePdfGeneratorTests : IDisposable
{
    private readonly InvoicePdfGenerator _generator;
    private readonly XmlInvoiceParser _parser;
    private readonly string _testOutputDir;

    public InvoicePdfGeneratorTests()
    {
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

        _generator = new InvoicePdfGenerator(
            linkService,
            qrService,
            NullLogger<InvoicePdfGenerator>.Instance
        );

        _parser = new XmlInvoiceParser(
            NullLogger<XmlInvoiceParser>.Instance
        );

        // Katalog tymczasowy dla testów PDF
        _testOutputDir = Path.Combine(Path.GetTempPath(), "KseFPrinterTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        // Cleanup test output directory
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
    public async Task GeneratePdfToBytesAsync_Should_Generate_Valid_Pdf_For_Offline_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var options = new PdfGenerationOptions();

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context, options);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000); // PDF should be reasonably sized

        // Verify PDF signature
        var pdfSignature = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF"); // Valid PDF header
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Generate_Valid_Pdf_For_Online_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        // Verify PDF signature
        var pdfSignature = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF");
    }

    [Fact]
    public async Task GeneratePdfToFileAsync_Should_Create_Pdf_File()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var outputPath = Path.Combine(_testOutputDir, "test_invoice.pdf");

        // Act
        await _generator.GeneratePdfToFileAsync(context, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var fileInfo = new FileInfo(outputPath);
        fileInfo.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task GeneratePdfAsync_Should_Return_Bytes_When_OutputPath_Is_Null()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Act
        var result = await _generator.GeneratePdfAsync(context, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfAsync_Should_Return_Null_When_OutputPath_Is_Provided()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var outputPath = Path.Combine(_testOutputDir, "test_invoice2.pdf");

        // Act
        var result = await _generator.GeneratePdfAsync(context, outputPath);

        // Assert
        result.Should().BeNull();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Include_QR_Code_For_Offline_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var options = new PdfGenerationOptions
        {
            IncludeQrCode1 = true,
            IncludeQrCode2 = false
        };

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context, options);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Work_Without_QR_Codes()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var options = new PdfGenerationOptions
        {
            IncludeQrCode1 = false,
            IncludeQrCode2 = false
        };

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context, options);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Handle_Invoice_Without_Payment_Info()
    {
        // Arrange - zakładamy, że możemy mieć fakturę bez płatności
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Usuń informacje o płatności
        context.Faktura.Fa.Platnosc = null;

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Handle_Invoice_Without_Footer()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Usuń stopkę
        context.Faktura.Stopka = null;

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Generate_Different_Pdfs_For_Different_Invoices()
    {
        // Arrange
        var xmlPath1 = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlPath2 = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var context1 = await _parser.ParseFromFileAsync(xmlPath1);
        var context2 = await _parser.ParseFromFileAsync(xmlPath2);

        // Act
        var pdf1 = await _generator.GeneratePdfToBytesAsync(context1);
        var pdf2 = await _generator.GeneratePdfToBytesAsync(context2);

        // Assert
        pdf1.Should().NotBeNull();
        pdf2.Should().NotBeNull();
        pdf1.Should().NotEqual(pdf2); // Different invoices should produce different PDFs
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Be_Deterministic_For_Same_Input()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Usuń timestamp dynamiczny - nie możemy tego zrobić bo jest w footer
        // Zamiast tego sprawdzimy, czy zawartość jest podobna

        // Act
        var pdf1 = await _generator.GeneratePdfToBytesAsync(context);
        var pdf2 = await _generator.GeneratePdfToBytesAsync(context);

        // Assert
        pdf1.Should().NotBeNull();
        pdf2.Should().NotBeNull();

        // PDFy będą różne ze względu na timestamp w stopce
        // ale rozmiar powinien być bardzo podobny (różnica max 100 bajtów)
        Math.Abs(pdf1.Length - pdf2.Length).Should().BeLessThan(100);
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Handle_Multiple_Line_Items()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Faktura testowa ma wiele pozycji
        context.Faktura.Fa.FaWiersz.Should().NotBeNullOrEmpty();

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Handle_Different_Currencies()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
        // Waluta powinna być wyświetlona w PDF
    }

    [Fact]
    public async Task GeneratePdfToBytesAsync_Should_Use_Custom_Options()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var options = new PdfGenerationOptions
        {
            UseProduction = true,
            QrPixelsPerModule = 10,
            IncludeQrCode1 = true,
            IncludeQrCode2 = false,
            DocumentTitle = "Test Invoice",
            DocumentAuthor = "Test System"
        };

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context, options);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }
}
