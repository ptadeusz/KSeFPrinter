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
    /// Thumbprint certyfikatu z Windows Store (opcjonalny, dla KODU QR II)
    /// </summary>
    public string? CertificateThumbprint { get; set; }

    /// <summary>
    /// Subject certyfikatu z Windows Store (opcjonalny, dla KODU QR II)
    /// </summary>
    public string? CertificateSubject { get; set; }

    /// <summary>
    /// Nazwa Windows Certificate Store (domyślnie: My)
    /// </summary>
    public string CertificateStoreName { get; set; } = "My";

    /// <summary>
    /// Lokalizacja Windows Certificate Store (domyślnie: CurrentUser)
    /// </summary>
    public string CertificateStoreLocation { get; set; } = "CurrentUser";

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
}
