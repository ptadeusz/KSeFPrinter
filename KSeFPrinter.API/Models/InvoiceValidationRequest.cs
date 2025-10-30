namespace KSeFPrinter.API.Models;

/// <summary>
/// Request do walidacji faktury XML
/// </summary>
public class InvoiceValidationRequest
{
    /// <summary>
    /// Zawartość pliku XML faktury (base64 lub plain text)
    /// </summary>
    public string XmlContent { get; set; } = null!;

    /// <summary>
    /// Opcjonalny numer KSeF (jeśli nie jest w XML)
    /// </summary>
    public string? KSeFNumber { get; set; }

    /// <summary>
    /// Czy XML jest w formacie base64 (domyślnie: false)
    /// </summary>
    public bool IsBase64 { get; set; } = false;
}
