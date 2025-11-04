namespace KSeFPrinter.Models.Common;

/// <summary>
/// Metadata pliku źródłowego (CSV/JSON/Excel) użytego do wygenerowania faktury FA(3)
/// Opcjonalne rozszerzenie dla śledzenia źródła faktury
/// </summary>
public class SourceFileMetadata
{
    /// <summary>
    /// Nazwa pliku źródłowego (np. "invoice_123.csv")
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Hash SHA-256 pliku źródłowego (dla weryfikacji integralności)
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// Format pliku źródłowego ("csv", "json", "excel", "xml")
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Data i czas konwersji pliku źródłowego do FA(3)
    /// </summary>
    public DateTime? ConvertedAt { get; set; }

    /// <summary>
    /// ID konwersji z historii (opcjonalne, jeśli integracja z KSeFConverter)
    /// </summary>
    public int? ConversionId { get; set; }

    /// <summary>
    /// Dodatkowe informacje o źródle (np. nazwa systemu ERP)
    /// </summary>
    public string? AdditionalInfo { get; set; }
}
