using KSeFPrinter.Models.Common;

namespace KSeFPrinter.API.Models;

/// <summary>
/// Request do generowania PDF
/// </summary>
public class GeneratePdfRequest
{
    /// <summary>
    /// Zawartość pliku XML faktury (base64 lub plain text)
    /// </summary>
    public string XmlContent { get; set; } = null!;

    /// <summary>
    /// Czy XML jest w formacie base64 (domyślnie: false)
    /// </summary>
    public bool IsBase64 { get; set; } = false;

    /// <summary>
    /// Opcjonalny numer KSeF (jeśli nie jest w XML)
    /// </summary>
    public string? KSeFNumber { get; set; }

    /// <summary>
    /// Czy używać środowiska produkcyjnego (domyślnie: false - test)
    /// </summary>
    public bool UseProduction { get; set; } = false;

    /// <summary>
    /// Czy walidować fakturę przed generowaniem (domyślnie: true)
    /// </summary>
    public bool ValidateInvoice { get; set; } = true;

    /// <summary>
    /// Format zwracania PDF: "file" (binary) lub "base64" (domyślnie: file)
    /// </summary>
    public string ReturnFormat { get; set; } = "file";

    /// <summary>
    /// Metadata pliku źródłowego (opcjonalne - dla audytu i śledzenia źródła faktury)
    /// Jeśli podane, zostaną zapisane w metadanych PDF
    /// </summary>
    public SourceFileMetadata? SourceFile { get; set; }
}
