namespace KSeFPrinter.API.Models;

/// <summary>
/// Request do generowania PDF w trybie offline (dla faktur z błędami)
/// </summary>
public class GeneratePdfOfflineRequest
{
    /// <summary>
    /// Ścieżka do pliku XML faktury
    /// </summary>
    public string XmlFilePath { get; set; } = null!;

    /// <summary>
    /// Opcjonalna ścieżka do zapisu PDF (domyślnie: obok XML)
    /// </summary>
    public string? OutputPdfPath { get; set; }

    /// <summary>
    /// Czy używać środowiska produkcyjnego (domyślnie: false - test)
    /// </summary>
    public bool UseProduction { get; set; } = false;

    /// <summary>
    /// Czy walidować fakturę przed generowaniem (domyślnie: false dla błędnych faktur)
    /// </summary>
    public bool ValidateInvoice { get; set; } = false;
}
