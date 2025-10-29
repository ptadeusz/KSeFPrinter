using KSeFPrinter.Interfaces;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging;

namespace KSeFPrinter;

/// <summary>
/// Główny serwis do drukowania faktur KSeF
/// Facade integrujący parser, walidator i generator PDF
/// </summary>
public class InvoicePrinterService
{
    private readonly IXmlInvoiceParser _parser;
    private readonly InvoiceValidator _validator;
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly ILogger<InvoicePrinterService> _logger;

    public InvoicePrinterService(
        IXmlInvoiceParser parser,
        InvoiceValidator validator,
        IPdfGeneratorService pdfGenerator,
        ILogger<InvoicePrinterService> logger)
    {
        _parser = parser;
        _validator = validator;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Generuje PDF z pliku XML faktury
    /// </summary>
    /// <param name="xmlFilePath">Ścieżka do pliku XML</param>
    /// <param name="pdfOutputPath">Ścieżka do pliku PDF (jeśli null, zwraca bajty)</param>
    /// <param name="options">Opcje generowania PDF</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF (jeśli nie jest w XML)</param>
    /// <param name="validateInvoice">Czy walidować fakturę przed generowaniem PDF</param>
    /// <returns>Bajty PDF jeśli pdfOutputPath jest null</returns>
    public async Task<byte[]?> GeneratePdfFromFileAsync(
        string xmlFilePath,
        string? pdfOutputPath = null,
        PdfGenerationOptions? options = null,
        string? numerKSeF = null,
        bool validateInvoice = true)
    {
        _logger.LogInformation("Rozpoczęcie generowania PDF z pliku: {XmlFile}", xmlFilePath);

        // Parsuj XML
        var context = await _parser.ParseFromFileAsync(xmlFilePath, numerKSeF);

        return await GeneratePdfFromContextAsync(context, pdfOutputPath, options, validateInvoice);
    }

    /// <summary>
    /// Generuje PDF z XML jako string
    /// </summary>
    /// <param name="xmlContent">Zawartość XML</param>
    /// <param name="pdfOutputPath">Ścieżka do pliku PDF (jeśli null, zwraca bajty)</param>
    /// <param name="options">Opcje generowania PDF</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF (jeśli nie jest w XML)</param>
    /// <param name="validateInvoice">Czy walidować fakturę przed generowaniem PDF</param>
    /// <returns>Bajty PDF jeśli pdfOutputPath jest null</returns>
    public async Task<byte[]?> GeneratePdfFromXmlAsync(
        string xmlContent,
        string? pdfOutputPath = null,
        PdfGenerationOptions? options = null,
        string? numerKSeF = null,
        bool validateInvoice = true)
    {
        _logger.LogInformation("Rozpoczęcie generowania PDF z XML string");

        // Parsuj XML
        var context = await _parser.ParseFromStringAsync(xmlContent, numerKSeF);

        return await GeneratePdfFromContextAsync(context, pdfOutputPath, options, validateInvoice);
    }

    /// <summary>
    /// Generuje PDF z kontekstu faktury
    /// </summary>
    /// <param name="context">Kontekst faktury</param>
    /// <param name="pdfOutputPath">Ścieżka do pliku PDF (jeśli null, zwraca bajty)</param>
    /// <param name="options">Opcje generowania PDF</param>
    /// <param name="validateInvoice">Czy walidować fakturę przed generowaniem PDF</param>
    /// <returns>Bajty PDF jeśli pdfOutputPath jest null</returns>
    public async Task<byte[]?> GeneratePdfFromContextAsync(
        InvoiceContext context,
        string? pdfOutputPath = null,
        PdfGenerationOptions? options = null,
        bool validateInvoice = true)
    {
        _logger.LogInformation(
            "Generowanie PDF: Numer={Numer}, Tryb={Tryb}, NIP={NIP}",
            context.Faktura.Fa.P_2,
            context.Metadata.Tryb,
            context.Faktura.Podmiot1.DaneIdentyfikacyjne.NIP
        );

        // Waliduj jeśli wymagane
        if (validateInvoice)
        {
            var validationResult = _validator.ValidateContext(context);

            if (!validationResult.IsValid)
            {
                _logger.LogError(
                    "Walidacja faktury nie powiodła się: {Errors}",
                    string.Join(", ", validationResult.Errors)
                );

                throw new InvalidOperationException(
                    $"Faktura nie przeszła walidacji: {string.Join("; ", validationResult.Errors)}"
                );
            }

            if (validationResult.Warnings.Any())
            {
                _logger.LogWarning(
                    "Ostrzeżenia walidacji: {Warnings}",
                    string.Join(", ", validationResult.Warnings)
                );
            }
        }

        // Generuj PDF
        options ??= new PdfGenerationOptions();

        if (pdfOutputPath != null)
        {
            await _pdfGenerator.GeneratePdfToFileAsync(context, pdfOutputPath);
            _logger.LogInformation("PDF zapisany do: {PdfPath}", pdfOutputPath);
            return null;
        }
        else
        {
            var pdfBytes = await _pdfGenerator.GeneratePdfToBytesAsync(context);
            _logger.LogInformation("PDF wygenerowany: {Size} bajtów", pdfBytes.Length);
            return pdfBytes;
        }
    }

    /// <summary>
    /// Waliduje fakturę z pliku XML bez generowania PDF
    /// </summary>
    /// <param name="xmlFilePath">Ścieżka do pliku XML</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF</param>
    /// <returns>Wynik walidacji</returns>
    public async Task<ValidationResult> ValidateInvoiceFromFileAsync(
        string xmlFilePath,
        string? numerKSeF = null)
    {
        _logger.LogInformation("Walidacja faktury z pliku: {XmlFile}", xmlFilePath);

        var context = await _parser.ParseFromFileAsync(xmlFilePath, numerKSeF);
        return _validator.ValidateContext(context);
    }

    /// <summary>
    /// Waliduje fakturę z XML jako string bez generowania PDF
    /// </summary>
    /// <param name="xmlContent">Zawartość XML</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF</param>
    /// <returns>Wynik walidacji</returns>
    public async Task<ValidationResult> ValidateInvoiceFromXmlAsync(
        string xmlContent,
        string? numerKSeF = null)
    {
        _logger.LogInformation("Walidacja faktury z XML string");

        var context = await _parser.ParseFromStringAsync(xmlContent, numerKSeF);
        return _validator.ValidateContext(context);
    }

    /// <summary>
    /// Parsuje fakturę z pliku XML bez generowania PDF
    /// </summary>
    /// <param name="xmlFilePath">Ścieżka do pliku XML</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF</param>
    /// <returns>Kontekst faktury</returns>
    public async Task<InvoiceContext> ParseInvoiceFromFileAsync(
        string xmlFilePath,
        string? numerKSeF = null)
    {
        return await _parser.ParseFromFileAsync(xmlFilePath, numerKSeF);
    }

    /// <summary>
    /// Parsuje fakturę z XML jako string bez generowania PDF
    /// </summary>
    /// <param name="xmlContent">Zawartość XML</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF</param>
    /// <returns>Kontekst faktury</returns>
    public async Task<InvoiceContext> ParseInvoiceFromXmlAsync(
        string xmlContent,
        string? numerKSeF = null)
    {
        return await _parser.ParseFromStringAsync(xmlContent, numerKSeF);
    }
}
