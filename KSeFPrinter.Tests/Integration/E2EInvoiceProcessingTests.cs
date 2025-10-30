using FluentAssertions;
using KSeFPrinter;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Integration;

/// <summary>
/// End-to-end testy dla pełnego pipeline'u przetwarzania faktur
/// Parse -> Validate -> Generate PDF dla wszystkich przykładowych plików
/// </summary>
public class E2EInvoiceProcessingTests : IDisposable
{
    private readonly InvoicePrinterService _service;
    private readonly string _testOutputDir;

    public E2EInvoiceProcessingTests()
    {
        // Inicjalizacja pełnego stosu zależności
        var parser = new XmlInvoiceParser(NullLogger<XmlInvoiceParser>.Instance);
        var validator = new InvoiceValidator(NullLogger<InvoiceValidator>.Instance);
        var cryptoService = new CryptographyService(NullLogger<CryptographyService>.Instance);
        var linkService = new VerificationLinkService(cryptoService, NullLogger<VerificationLinkService>.Instance);
        var qrService = new QrCodeService(NullLogger<QrCodeService>.Instance);
        var pdfGenerator = new InvoicePdfGenerator(linkService, qrService, NullLogger<InvoicePdfGenerator>.Instance);

        _service = new InvoicePrinterService(
            parser,
            validator,
            pdfGenerator,
            NullLogger<InvoicePrinterService>.Instance
        );

        _testOutputDir = Path.Combine(Path.GetTempPath(), "KseFE2ETests", Guid.NewGuid().ToString());
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

    [Theory]
    [InlineData("FakturaTEST017.xml")]
    [InlineData("FakturaTEST017_Adnotacje.xml")]
    [InlineData("FakturaTEST017_EUR.xml")]
    [InlineData("FakturaTEST017_Extended.xml")]
    [InlineData("FakturaTEST017_Extended_Faza2.xml")]
    [InlineData("FakturaTEST017_Extended_Faza3.xml")]
    [InlineData("FakturaTEST017_Faza3_Kompletny.xml")]
    [InlineData("FakturaTEST017_Kompletny.xml")]
    [InlineData("FakturaTEST017_Korekta_Poprawna.xml")]
    [InlineData("FakturaTEST017_Podmiot2_Pola.xml")]
    [InlineData("FakturaTEST017_Podmiot3_JST.xml")]
    [InlineData("FakturaTEST017_Rozliczenie.xml")]
    [InlineData("FakturaTEST017_Umowy.xml")]
    [InlineData("FakturaTEST017_WieleKontaktow.xml")]
    [InlineData("FakturaTEST017_WielePodmiot3.xml")]
    [InlineData("FakturaTEST017_WieleRachunkow.xml")]
    [InlineData("FakturaTEST017_WieleTerminow.xml")]
    [InlineData("FakturaTEST017_Zamowienia.xml")]
    [InlineData("6511153259-20251015-010020140418-0D.xml")]
    public async Task E2E_Should_Process_Example_Invoice_Successfully(string xmlFileName)
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", xmlFileName);
        var pdfPath = Path.Combine(_testOutputDir, Path.ChangeExtension(xmlFileName, ".pdf"));

        // Act - pełny pipeline
        // 1. Parse
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);

        // 2. Validate (skip dla niektórych testowych faktur z błędami w NIP)
        // var validationResult = await _service.ValidateInvoiceFromFileAsync(xmlPath);

        // 3. Generate PDF
        var pdfBytes = await _service.GeneratePdfFromFileAsync(
            xmlPath,
            pdfPath,
            validateInvoice: false); // Skip validation

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();
        context.Faktura.Fa.Should().NotBeNull();
        context.Faktura.Fa.P_2.Should().NotBeNullOrEmpty();

        // PDF powinien zostać wygenerowany
        File.Exists(pdfPath).Should().BeTrue();
        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(1000);

        // Verify PDF signature
        var pdfBytesFromFile = await File.ReadAllBytesAsync(pdfPath);
        var pdfSignature = System.Text.Encoding.ASCII.GetString(pdfBytesFromFile.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF");
    }

    [Fact]
    public async Task E2E_Should_Process_Stress_Test_Large_Invoice()
    {
        // Arrange - duża faktura z wieloma pozycjami
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_Stress_Duza.xml");

        if (!File.Exists(xmlPath))
        {
            // Skip if stress test file doesn't exist
            return;
        }

        var pdfPath = Path.Combine(_testOutputDir, "stress_test.pdf");

        // Act
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        var pdfBytes = await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        context.Should().NotBeNull();
        File.Exists(pdfPath).Should().BeTrue();

        // Duża faktura powinna mieć duży PDF
        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(10000);
    }

    [Fact]
    public async Task E2E_Should_Handle_Invoice_With_EUR_Currency()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_EUR.xml");
        var pdfPath = Path.Combine(_testOutputDir, "eur_invoice.pdf");

        // Act
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        context.Faktura.Fa.KodWaluty.Should().Be("EUR");
        File.Exists(pdfPath).Should().BeTrue();
    }

    [Fact]
    public async Task E2E_Should_Handle_Invoice_With_Correction()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_Korekta_Poprawna.xml");
        var pdfPath = Path.Combine(_testOutputDir, "korekta_invoice.pdf");

        // Act
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        // Faktura korygująca powinna mieć odpowiednie dane
        File.Exists(pdfPath).Should().BeTrue();
    }

    [Fact]
    public async Task E2E_Should_Handle_Invoice_With_Multiple_Bank_Accounts()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_WieleRachunkow.xml");
        var pdfPath = Path.Combine(_testOutputDir, "multiple_accounts.pdf");

        // Act
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        context.Faktura.Fa.Platnosc.Should().NotBeNull();
        context.Faktura.Fa.Platnosc!.RachunekBankowy.Should().NotBeNullOrEmpty();
        context.Faktura.Fa.Platnosc.RachunekBankowy!.Count.Should().BeGreaterThan(1);
        File.Exists(pdfPath).Should().BeTrue();
    }

    [Fact]
    public async Task E2E_Should_Handle_Invoice_With_Multiple_Payment_Terms()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_WieleTerminow.xml");
        var pdfPath = Path.Combine(_testOutputDir, "multiple_terms.pdf");

        // Act
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        context.Faktura.Fa.Platnosc.Should().NotBeNull();
        context.Faktura.Fa.Platnosc!.TerminPlatnosci.Should().NotBeNullOrEmpty();
        context.Faktura.Fa.Platnosc.TerminPlatnosci!.Count.Should().BeGreaterThan(1);
        File.Exists(pdfPath).Should().BeTrue();
    }

    [Fact]
    public async Task E2E_Should_Generate_Different_PDFs_For_Different_Invoices()
    {
        // Arrange
        var xml1Path = Path.Combine("TestData", "FakturaTEST017.xml");
        var xml2Path = Path.Combine("TestData", "FakturaTEST017_EUR.xml");
        var pdf1Path = Path.Combine(_testOutputDir, "invoice1.pdf");
        var pdf2Path = Path.Combine(_testOutputDir, "invoice2.pdf");

        // Act
        await _service.GeneratePdfFromFileAsync(xml1Path, pdf1Path, validateInvoice: false);
        await _service.GeneratePdfFromFileAsync(xml2Path, pdf2Path, validateInvoice: false);

        // Assert
        var pdf1Bytes = await File.ReadAllBytesAsync(pdf1Path);
        var pdf2Bytes = await File.ReadAllBytesAsync(pdf2Path);

        pdf1Bytes.Should().NotEqual(pdf2Bytes);
    }

    [Fact]
    public async Task E2E_Should_Handle_Batch_Processing()
    {
        // Arrange
        var xmlFiles = new[]
        {
            "FakturaTEST017.xml",
            "FakturaTEST017_EUR.xml",
            "FakturaTEST017_Kompletny.xml"
        };

        // Act & Assert
        foreach (var xmlFile in xmlFiles)
        {
            var xmlPath = Path.Combine("TestData", xmlFile);
            var pdfPath = Path.Combine(_testOutputDir, Path.ChangeExtension(xmlFile, ".pdf"));

            await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

            File.Exists(pdfPath).Should().BeTrue($"PDF for {xmlFile} should exist");
        }

        // Wszystkie PDF-y powinny istnieć
        var pdfFiles = Directory.GetFiles(_testOutputDir, "*.pdf");
        pdfFiles.Should().HaveCount(xmlFiles.Length);
    }

    [Fact]
    public async Task E2E_Should_Parse_And_Regenerate_Consistently()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var pdf1Path = Path.Combine(_testOutputDir, "consistent1.pdf");
        var pdf2Path = Path.Combine(_testOutputDir, "consistent2.pdf");

        // Act - wygeneruj dwa razy ten sam PDF
        await _service.GeneratePdfFromFileAsync(xmlPath, pdf1Path, validateInvoice: false);

        // Poczekaj chwilę aby timestamp był inny
        await Task.Delay(1000);

        await _service.GeneratePdfFromFileAsync(xmlPath, pdf2Path, validateInvoice: false);

        // Assert - rozmiary powinny być bardzo podobne (timestamp w stopce może się różnić)
        var file1Info = new FileInfo(pdf1Path);
        var file2Info = new FileInfo(pdf2Path);

        Math.Abs(file1Info.Length - file2Info.Length).Should().BeLessThan(200);
    }

    [Fact]
    public async Task E2E_Should_Handle_Online_Invoice_With_KSeF_Number()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var pdfPath = Path.Combine(_testOutputDir, "online_ksef.pdf");

        // Act
        var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
        await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, validateInvoice: false);

        // Assert
        // Sprawdź czy to online (może mieć numer KSeF w metadanych lub nazwie)
        File.Exists(pdfPath).Should().BeTrue();
    }

    [Fact]
    public async Task E2E_Should_Include_QR_Codes_In_PDF()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var pdfPath = Path.Combine(_testOutputDir, "with_qr.pdf");

        var options = new PdfGenerationOptions
        {
            IncludeQrCode1 = true,
            IncludeQrCode2 = false,
            UseProduction = false
        };

        // Act
        await _service.GeneratePdfFromFileAsync(xmlPath, pdfPath, options: options, validateInvoice: false);

        // Assert
        File.Exists(pdfPath).Should().BeTrue();

        // PDF z QR code powinien być większy
        var fileInfo = new FileInfo(pdfPath);
        fileInfo.Length.Should().BeGreaterThan(10000);
    }

    [Fact]
    public async Task E2E_Should_Return_Bytes_When_No_Output_Path()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");

        // Act
        var pdfBytes = await _service.GeneratePdfFromFileAsync(xmlPath, pdfOutputPath: null, validateInvoice: false);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes!.Length.Should().BeGreaterThan(1000);

        // Verify PDF signature
        var pdfSignature = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
        pdfSignature.Should().Be("%PDF");
    }
}
