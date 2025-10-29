using KSeFPrinter.Models.Common;

namespace KSeFPrinter.Interfaces;

/// <summary>
/// Interfejs parsera XML faktur KSeF FA(3)
/// </summary>
public interface IXmlInvoiceParser
{
    /// <summary>
    /// Parsuje fakturę z pliku XML
    /// </summary>
    /// <param name="xmlFilePath">Ścieżka do pliku XML</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF (jeśli nie jest zawarty w XML)</param>
    /// <returns>Kontekst faktury gotowy do generowania PDF</returns>
    Task<InvoiceContext> ParseFromFileAsync(string xmlFilePath, string? numerKSeF = null);

    /// <summary>
    /// Parsuje fakturę z XML jako string
    /// </summary>
    /// <param name="xmlContent">Zawartość XML jako string</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF (jeśli nie jest zawarty w XML)</param>
    /// <returns>Kontekst faktury gotowy do generowania PDF</returns>
    Task<InvoiceContext> ParseFromStringAsync(string xmlContent, string? numerKSeF = null);

    /// <summary>
    /// Parsuje fakturę ze strumienia XML
    /// </summary>
    /// <param name="xmlStream">Strumień z XML</param>
    /// <param name="numerKSeF">Opcjonalny numer KSeF (jeśli nie jest zawarty w XML)</param>
    /// <returns>Kontekst faktury gotowy do generowania PDF</returns>
    Task<InvoiceContext> ParseFromStreamAsync(Stream xmlStream, string? numerKSeF = null);

    /// <summary>
    /// Waliduje czy XML zawiera prawidłowy schemat FA(3)
    /// </summary>
    /// <param name="xmlContent">Zawartość XML</param>
    /// <returns>True jeśli XML jest poprawny</returns>
    bool ValidateSchema(string xmlContent);
}
