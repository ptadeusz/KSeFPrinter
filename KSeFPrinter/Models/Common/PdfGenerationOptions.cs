using System.Security.Cryptography.X509Certificates;

namespace KSeFPrinter.Models.Common;

/// <summary>
/// Opcje generowania PDF
/// </summary>
public class PdfGenerationOptions
{
    /// <summary>
    /// Certyfikat Offline dla podpisów QR (opcjonalny)
    /// Jeśli null, QR II nie zostanie wygenerowany
    /// </summary>
    public X509Certificate2? Certificate { get; set; }

    /// <summary>
    /// Czy używać środowiska produkcyjnego (domyślnie: false - test)
    /// </summary>
    public bool UseProduction { get; set; } = false;

    /// <summary>
    /// Liczba pikseli na moduł dla kodów QR (min 5, domyślnie 5)
    /// </summary>
    public int QrPixelsPerModule { get; set; } = 5;

    /// <summary>
    /// Czy dołączyć kod QR I (weryfikacja faktury)
    /// </summary>
    public bool IncludeQrCode1 { get; set; } = true;

    /// <summary>
    /// Czy dołączyć kod QR II (weryfikacja certyfikatu)
    /// Wymaga podania Certificate
    /// </summary>
    public bool IncludeQrCode2 { get; set; } = true;

    /// <summary>
    /// Tytuł dokumentu PDF
    /// </summary>
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Autor dokumentu PDF
    /// </summary>
    public string? DocumentAuthor { get; set; }
}
