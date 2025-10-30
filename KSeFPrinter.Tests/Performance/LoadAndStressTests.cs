using FluentAssertions;
using KSeFPrinter;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Models.FA3;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace KSeFPrinter.Tests.Performance;

/// <summary>
/// Testy obciążeniowe i stresowe
/// </summary>
public class LoadAndStressTests : IDisposable
{
    private readonly InvoicePrinterService _service;
    private readonly InvoicePdfGenerator _generator;
    private readonly XmlInvoiceParser _parser;
    private readonly string _testOutputDir;

    public LoadAndStressTests()
    {
        _parser = new XmlInvoiceParser(NullLogger<XmlInvoiceParser>.Instance);
        var validator = new InvoiceValidator(NullLogger<InvoiceValidator>.Instance);
        var cryptoService = new CryptographyService(NullLogger<CryptographyService>.Instance);
        var linkService = new VerificationLinkService(cryptoService, NullLogger<VerificationLinkService>.Instance);
        var qrService = new QrCodeService(NullLogger<QrCodeService>.Instance);
        _generator = new InvoicePdfGenerator(linkService, qrService, NullLogger<InvoicePdfGenerator>.Instance);

        _service = new InvoicePrinterService(
            _parser,
            validator,
            _generator,
            NullLogger<InvoicePrinterService>.Instance
        );

        _testOutputDir = Path.Combine(Path.GetTempPath(), "KseFLoadTests", Guid.NewGuid().ToString());
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
    public async Task Should_Handle_10_Concurrent_Pdf_Generations()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var concurrentCount = 10;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, concurrentCount)
            .Select(async i =>
            {
                var pdfBytes = await _service.GeneratePdfFromFileAsync(
                    xmlPath,
                    pdfOutputPath: null,
                    validateInvoice: false
                );
                return pdfBytes;
            });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(concurrentCount);
        results.Should().AllSatisfy(pdf => pdf.Should().NotBeNull().And.NotBeEmpty());

        // 10 równoległych generacji powinno zakończyć się w rozsądnym czasie
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000,
            $"10 concurrent generations took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Handle_Multiple_Sequential_Batches()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var batchCount = 3;
        var itemsPerBatch = 5;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int batch = 0; batch < batchCount; batch++)
        {
            var tasks = Enumerable.Range(0, itemsPerBatch)
                .Select(async i =>
                {
                    var pdfBytes = await _service.GeneratePdfFromFileAsync(
                        xmlPath,
                        pdfOutputPath: null,
                        validateInvoice: false
                    );
                    return pdfBytes;
                });

            var results = await Task.WhenAll(tasks);
            results.Should().HaveCount(itemsPerBatch);
        }

        stopwatch.Stop();

        // Assert
        // 3 batche po 5 faktur = 15 faktur, powinno zająć mniej niż 60 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(60000,
            $"3 batches of 5 took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Handle_Invoice_With_Many_Line_Items()
    {
        // Arrange - stwórz fakturę z wieloma pozycjami
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Dodaj 100 pozycji fakturowych
        var manyLineItems = Enumerable.Range(1, 100).Select(i => new FaWiersz
        {
            NrWierszaFa = i,
            P_7 = $"Pozycja testowa nr {i}",
            P_8B = 1.0m,
            P_9A = 10.00m,
            P_11 = 10.00m,
            P_12 = "23"
        }).ToList();

        context.Faktura.Fa.FaWiersz = manyLineItems;

        var stopwatch = Stopwatch.StartNew();

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);
        stopwatch.Stop();

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        // Faktura z 100 pozycjami powinna być wygenerowana w mniej niż 15 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000,
            $"Generation with 100 line items took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Handle_Invoice_With_Very_Long_Text_Fields()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Dodaj bardzo długie teksty
        context.Faktura.Podmiot1.DaneIdentyfikacyjne.Nazwa = new string('X', 1000);
        context.Faktura.Podmiot2.DaneIdentyfikacyjne.Nazwa = new string('Y', 1000);

        if (context.Faktura.Stopka == null)
        {
            context.Faktura.Stopka = new Stopka();
        }
        context.Faktura.Stopka.Informacje = new Informacje
        {
            StopkaFaktury = new string('Z', 5000)
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);
        stopwatch.Stop();

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();

        // Długie teksty powinny być obsłużone w mniej niż 10 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000,
            $"Generation with long texts took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Handle_Rapid_Sequential_Requests()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var requestCount = 20;
        var stopwatch = Stopwatch.StartNew();

        // Act - szybkie sekwencyjne żądania
        for (int i = 0; i < requestCount; i++)
        {
            var pdfBytes = await _service.GeneratePdfFromFileAsync(
                xmlPath,
                pdfOutputPath: null,
                validateInvoice: false
            );
            pdfBytes.Should().NotBeNull();
        }

        stopwatch.Stop();

        // Assert
        // 20 sekwencyjnych żądań powinno zakończyć się w mniej niż 60 sekund
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(60000,
            $"20 sequential requests took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Handle_Mixed_Invoice_Types_Concurrently()
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

        // Act - przetwarzaj różne typy faktur równolegle
        var tasks = xmlFiles.SelectMany(xmlFile =>
            Enumerable.Range(0, 2).Select(async _ =>
            {
                var xmlPath = Path.Combine("TestData", xmlFile);
                var pdfBytes = await _service.GeneratePdfFromFileAsync(
                    xmlPath,
                    pdfOutputPath: null,
                    validateInvoice: false
                );
                return pdfBytes;
            })
        );

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(10); // 5 plików * 2 powtórzenia
        results.Should().AllSatisfy(pdf => pdf.Should().NotBeNull().And.NotBeEmpty());

        // 10 różnych faktur równolegle powinno zakończyć się w rozsądnym czasie
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(40000,
            $"10 mixed concurrent generations took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Handle_Parse_And_Generate_Pipeline_Under_Load()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var iterations = 10;
        var stopwatch = Stopwatch.StartNew();

        // Act - pełny pipeline: parse -> generate dla wielu faktur
        var tasks = Enumerable.Range(0, iterations)
            .Select(async i =>
            {
                var context = await _service.ParseInvoiceFromFileAsync(xmlPath);
                var pdfBytes = await _service.GeneratePdfFromContextAsync(context, validateInvoice: false);
                return (context, pdfBytes);
            });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(iterations);
        results.Should().AllSatisfy(r =>
        {
            r.context.Should().NotBeNull();
            r.pdfBytes.Should().NotBeNull().And.NotBeEmpty();
        });

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(40000,
            $"10 full pipelines took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Maintain_Memory_Stability_Under_Load()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var iterations = 20;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(false);

        // Act - wygeneruj 20 PDF-ów
        for (int i = 0; i < iterations; i++)
        {
            var pdfBytes = await _service.GeneratePdfFromFileAsync(
                xmlPath,
                pdfOutputPath: null,
                validateInvoice: false
            );
            pdfBytes.Should().NotBeNull();

            // Co 5 iteracji wymuś GC
            if (i % 5 == 4)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryGrowth = memoryAfter - memoryBefore;

        // Assert
        // Wzrost pamięci po 20 iteracjach nie powinien być dramatyczny (< 100 MB)
        memoryGrowth.Should().BeLessThan(100 * 1024 * 1024,
            $"Memory grew by {memoryGrowth / 1024 / 1024}MB after {iterations} iterations");
    }

    [Fact]
    public async Task Should_Handle_Concurrent_File_Writes()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var concurrentWrites = 5;
        var stopwatch = Stopwatch.StartNew();

        // Act - zapisz równolegle do różnych plików
        var tasks = Enumerable.Range(0, concurrentWrites)
            .Select(async i =>
            {
                var outputPath = Path.Combine(_testOutputDir, $"concurrent_{i}.pdf");
                await _service.GeneratePdfFromFileAsync(
                    xmlPath,
                    outputPath,
                    validateInvoice: false
                );
                return outputPath;
            });

        var outputPaths = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        outputPaths.Should().HaveCount(concurrentWrites);
        outputPaths.Should().AllSatisfy(path =>
        {
            File.Exists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().BeGreaterThan(1000);
        });

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(25000,
            $"5 concurrent file writes took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Should_Handle_Extreme_Decimal_Values()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Ustaw ekstremalne wartości
        context.Faktura.Fa.P_13_1 = 999999999999.99m;
        context.Faktura.Fa.P_14_1 = 999999999999.99m;
        context.Faktura.Fa.P_15 = 999999999999.99m;

        context.Faktura.Fa.FaWiersz.First().P_11 = 999999999999.99m;

        // Act
        var pdfBytes = await _generator.GeneratePdfToBytesAsync(context);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Should().NotBeEmpty();
    }
}
