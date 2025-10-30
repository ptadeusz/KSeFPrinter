using FluentAssertions;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace KSeFPrinter.Tests.Performance;

/// <summary>
/// Testy wydajnościowe generowania PDF
/// </summary>
public class PdfGenerationPerformanceTests : IDisposable
{
    private readonly InvoicePdfGenerator _generator;
    private readonly XmlInvoiceParser _parser;
    private readonly string _testOutputDir;

    public PdfGenerationPerformanceTests()
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

        _testOutputDir = Path.Combine(Path.GetTempPath(), "KseFPerformanceTests", Guid.NewGuid().ToString());
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
    public async Task GeneratePdf_Should_Complete_Within_Acceptable_Time_For_Simple_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);
        stopwatch.Stop();

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        // Prosta faktura powinna być wygenerowana w mniej niż 5 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            $"Generation took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GeneratePdf_Should_Complete_Within_Acceptable_Time_For_Complex_Invoice()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_Kompletny.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);
        stopwatch.Stop();

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        // Złożona faktura powinna być wygenerowana w mniej niż 10 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000,
            $"Generation took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GeneratePdf_With_QrCodes_Should_Complete_Within_Acceptable_Time()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var options = new PdfGenerationOptions
        {
            IncludeQrCode1 = true,
            IncludeQrCode2 = true
        };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context, options);
        stopwatch.Stop();

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        // Z QR kodami powinno być maksymalnie 7 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(7000,
            $"Generation with QR codes took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ParseInvoice_Should_Complete_Within_Acceptable_Time()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_Kompletny.xml");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var context = await _parser.ParseFromFileAsync(xmlPath);
        stopwatch.Stop();

        // Assert
        context.Should().NotBeNull();

        // Parsowanie powinno być bardzo szybkie - poniżej 500ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            $"Parsing took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Sequential_Generation_Should_Maintain_Performance()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var iterations = 5;
        var times = new List<long>();

        // Act - generuj 5 razy z rzędu
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);
            stopwatch.Stop();

            times.Add(stopwatch.ElapsedMilliseconds);
            pdfBytes.Should().NotBeEmpty();
        }

        // Assert
        var averageTime = times.Average();
        var maxTime = times.Max();

        // Średni czas nie powinien przekraczać 5 sekund
        averageTime.Should().BeLessThan(5000,
            $"Average generation time was {averageTime}ms");

        // Żadna iteracja nie powinna być dramatycznie wolniejsza
        maxTime.Should().BeLessThan((long)(averageTime * 2),
            $"Max time {maxTime}ms is more than 2x average {averageTime}ms");
    }

    [Fact]
    public async Task GeneratePdf_Memory_Usage_Should_Be_Reasonable()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_Kompletny.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(false);

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);
        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - memoryBefore;

        // Assert
        pdfBytes.Should().NotBeNull();

        // Wykorzystanie pamięci powinno być rozsądne - poniżej 50 MB
        memoryUsed.Should().BeLessThan(50 * 1024 * 1024,
            $"Memory used: {memoryUsed / 1024 / 1024}MB");
    }

    [Fact]
    public async Task Batch_Processing_Should_Complete_Within_Acceptable_Time()
    {
        // Arrange
        var xmlFiles = new[]
        {
            "FakturaTEST017.xml",
            "FakturaTEST017_EUR.xml",
            "FakturaTEST017_Kompletny.xml",
            "FakturaTEST017_Adnotacje.xml",
            "FakturaTEST017_Rozliczenie.xml"
        };

        var stopwatch = Stopwatch.StartNew();
        var results = new List<byte[]>();

        // Act - przetwórz 5 faktur
        foreach (var xmlFile in xmlFiles)
        {
            var xmlPath = Path.Combine("TestData", xmlFile);
            var context = await _parser.ParseFromFileAsync(xmlPath);
            var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);
            results.Add(pdfBytes);
        }

        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(pdf => pdf.Should().NotBeEmpty());

        // 5 faktur powinno być przetworzonych w mniej niż 25 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(25000,
            $"Batch processing took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GeneratePdf_To_File_Should_Be_Fast()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);
        var outputPath = Path.Combine(_testOutputDir, "performance_test.pdf");
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _generator.GeneratePdfToFileAsync(context, outputPath);
        stopwatch.Stop();

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        // Zapis do pliku powinien być szybki - poniżej 6 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(6000,
            $"File generation took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ParseInvoice_With_Large_File_Should_Be_Fast()
    {
        // Arrange - jeśli nie ma dużego pliku testowego, użyj normalnego
        var xmlPath = Path.Combine("TestData", "FakturaTEST017_Kompletny.xml");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var context = await _parser.ParseFromFileAsync(xmlPath);
        stopwatch.Stop();

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();

        // Nawet duży plik powinien być sparsowany w poniżej 1 sekundy
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            $"Large file parsing took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task GeneratePdf_Without_QrCodes_Should_Be_Faster()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        var optionsWithQr = new PdfGenerationOptions
        {
            IncludeQrCode1 = true,
            IncludeQrCode2 = true
        };

        var optionsWithoutQr = new PdfGenerationOptions
        {
            IncludeQrCode1 = false,
            IncludeQrCode2 = false
        };

        // Act
        var stopwatchWithQr = Stopwatch.StartNew();
        await _generator.GeneratePdfToBytesAsync(context, optionsWithQr);
        stopwatchWithQr.Stop();

        var stopwatchWithoutQr = Stopwatch.StartNew();
        await _generator.GeneratePdfToBytesAsync(context, optionsWithoutQr);
        stopwatchWithoutQr.Stop();

        // Assert
        // Wersja bez QR powinna być szybsza (lub co najwyżej taka sama)
        stopwatchWithoutQr.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(
            stopwatchWithQr.ElapsedMilliseconds + 100, // +100ms tolerancji
            $"Without QR: {stopwatchWithoutQr.ElapsedMilliseconds}ms, With QR: {stopwatchWithQr.ElapsedMilliseconds}ms");
    }
}
