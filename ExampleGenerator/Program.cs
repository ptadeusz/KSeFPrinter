using KSeFPrinter;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging.Abstractions;

// Generuj oba przykłady
var examples = new[]
{
    (XmlPath: Path.Combine("..", "KSeFPrinter.Tests", "TestData", "FakturaTEST017.xml"),
     PdfPath: Path.Combine("..", "PRZYKLAD_FAKTURA_OFFLINE.pdf"),
     Type: "OFFLINE"),
    (XmlPath: Path.Combine("..", "KSeFPrinter.Tests", "TestData", "6511153259-20251015-010020140418-0D.xml"),
     PdfPath: Path.Combine("..", "PRZYKLAD_FAKTURA_Z_NUMEREM_KSEF.pdf"),
     Type: "Z NUMEREM KSeF")
};

// Inicjalizacja serwisów (raz dla wszystkich)
var parser = new XmlInvoiceParser(NullLogger<XmlInvoiceParser>.Instance);
var validator = new InvoiceValidator(NullLogger<InvoiceValidator>.Instance);
var cryptoService = new CryptographyService(NullLogger<CryptographyService>.Instance);
var linkService = new VerificationLinkService(cryptoService, NullLogger<VerificationLinkService>.Instance);
var qrService = new QrCodeService(NullLogger<QrCodeService>.Instance);
var pdfGenerator = new InvoicePdfGenerator(linkService, qrService, NullLogger<InvoicePdfGenerator>.Instance);
var service = new InvoicePrinterService(parser, validator, pdfGenerator, NullLogger<InvoicePrinterService>.Instance);

foreach (var example in examples)
{
    var xmlPath = example.XmlPath;
    var pdfOutputPath = example.PdfPath;

    Console.WriteLine($"=== Generator przykładowego PDF KSeF ({example.Type}) ===\n");

    if (!File.Exists(xmlPath))
    {
        Console.WriteLine($"BŁĄD: Nie znaleziono pliku XML: {xmlPath}");
        Console.WriteLine($"Pełna ścieżka: {Path.GetFullPath(xmlPath)}");
        continue;
    }

    Console.WriteLine($"Parsowanie XML: {Path.GetFileName(xmlPath)}");
    var context = await service.ParseInvoiceFromFileAsync(xmlPath);

    Console.WriteLine($"  Faktura: {context.Faktura.Fa.P_2}");
    Console.WriteLine($"  Data: {context.Faktura.Fa.P_1:yyyy-MM-dd}");
    Console.WriteLine($"  Sprzedawca: {context.Faktura.Podmiot1.DaneIdentyfikacyjne.Nazwa}");
    Console.WriteLine($"  Nabywca: {context.Faktura.Podmiot2.DaneIdentyfikacyjne.Nazwa}");
    Console.WriteLine($"  Tryb: {context.Metadata.Tryb}");
    if (!string.IsNullOrEmpty(context.Metadata.NumerKSeF))
        Console.WriteLine($"  Numer KSeF: {context.Metadata.NumerKSeF}");
    Console.WriteLine($"  Kwota brutto: {context.Faktura.Fa.P_15:N2} {context.Faktura.Fa.KodWaluty}");

    Console.WriteLine($"\nGenerowanie PDF...");
    await service.GeneratePdfFromFileAsync(xmlPath, pdfOutputPath, validateInvoice: false);

    var fileInfo = new FileInfo(pdfOutputPath);
    Console.WriteLine($"✓ PDF wygenerowany pomyślnie!");
    Console.WriteLine($"  Rozmiar: {fileInfo.Length:N0} bajtów");
    Console.WriteLine($"  Lokalizacja: {Path.GetFullPath(pdfOutputPath)}");
    Console.WriteLine("\n=== Gotowe! ===\n");
}

return 0;
