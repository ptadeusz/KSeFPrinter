namespace KSeFPrinter.API.Models;

/// <summary>
/// Response z wygenerowanym PDF (tylko gdy ReturnFormat = "base64")
/// </summary>
public class GeneratePdfResponse
{
    /// <summary>
    /// PDF w formacie base64
    /// </summary>
    public string PdfBase64 { get; set; } = null!;

    /// <summary>
    /// Rozmiar pliku w bajtach
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Numer faktury
    /// </summary>
    public string InvoiceNumber { get; set; } = null!;

    /// <summary>
    /// Tryb wystawienia (Online/Offline)
    /// </summary>
    public string Mode { get; set; } = null!;
}
