namespace KSeFPrinter.API.Models;

/// <summary>
/// Response z wynikiem generowania PDF offline
/// </summary>
public class GeneratePdfOfflineResponse
{
    /// <summary>
    /// Czy operacja zakończona sukcesem
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Ścieżka do wygenerowanego pliku PDF
    /// </summary>
    public string PdfPath { get; set; } = null!;

    /// <summary>
    /// Rozmiar pliku PDF w bajtach
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Numer faktury
    /// </summary>
    public string InvoiceNumber { get; set; } = null!;

    /// <summary>
    /// Tryb wystawienia (zawsze "Offline" dla tego endpointu)
    /// </summary>
    public string Mode { get; set; } = "Offline";
}
