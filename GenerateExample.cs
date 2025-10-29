using KSeFPrinter;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Services.Pdf;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging.Abstractions;

// Ścieżki
var xmlPath = Path.Combine("KSeFPrinter.Tests", "TestData", "FakturaTEST017.xml");
var pdfOutputPath = "PRZYKLAD_FAKTURA_OFFLINE.pdf";

Console.WriteLine("=== Generator przykładowego PDF KSeF ===\n");

if (!File.Exists(xmlPath))
{
    Console.WriteLine($"BŁĄD: Nie znaleziono pliku XML: {xmlPath}");
    return 1;
}

// Inicjalizacja serwisów
var parser = new XmlInvoiceParser(NullLogger<XmlInvoiceParser>.Instance);
var validator = new InvoiceValidator(NullLogger<InvoiceValidator>.Instance);
var cryptoService = new CryptographyService(NullLogger<CryptographyService>.Instance);
var linkService = new VerificationLinkService(cryptoService, NullLogger<VerificationLinkService>.Instance);
var qrService = new QrCodeService(NullLogger<QrCodeService>.Instance);
var pdfGenerator = new InvoicePdfGenerator(linkService, qrService, NullLogger<InvoicePdfGenerator>.Instance);

var service = new InvoicePrinterService(parser, validator, pdfGenerator, NullLogger<InvoicePrinterService>.Instance);

Console.WriteLine($"Parsowanie XML: {xmlPath}");
var context = await service.ParseInvoiceFromFileAsync(xmlPath);

Console.WriteLine($"  Faktura: {context.Faktura.Fa.P_2}");
Console.WriteLine($"  Data: {context.Faktura.Fa.P_1:yyyy-MM-dd}");
Console.WriteLine($"  Sprzedawca: {context.Faktura.Podmiot1.DaneIdentyfikacyjne.Nazwa}");
Console.WriteLine($"  Nabywca: {context.Faktura.Podmiot2.DaneIdentyfikacyjne.Nazwa}");
Console.WriteLine($"  Tryb: {context.Metadata.Tryb}");
Console.WriteLine($"  Kwota brutto: {context.Faktura.Fa.P_15:N2} {context.Faktura.Fa.KodWaluty}");

Console.WriteLine($"\nGenerowanie PDF: {pdfOutputPath}");
await service.GeneratePdfFromFileAsync(xmlPath, pdfOutputPath, validateInvoice: false);

var fileInfo = new FileInfo(pdfOutputPath);
Console.WriteLine($"✓ PDF wygenerowany pomyślnie!");
Console.WriteLine($"  Rozmiar: {fileInfo.Length:N0} bajtów");
Console.WriteLine($"  Lokalizacja: {Path.GetFullPath(pdfOutputPath)}");

Console.WriteLine("\n=== Gotowe! ===");
return 0;
