using KSeFPrinter.Models.FA3;

namespace KSeFPrinter.Models.Common;

/// <summary>
/// Kontekst faktury zawierający dane XML oraz metadane KSeF
/// Używany jako główny model dla generatora PDF
/// </summary>
public class InvoiceContext
{
    /// <summary>
    /// Faktura FA(3) sparsowana z XML
    /// </summary>
    public Faktura Faktura { get; set; } = null!;

    /// <summary>
    /// Metadane KSeF (numer, tryb)
    /// </summary>
    public KSeFMetadata Metadata { get; set; } = null!;

    /// <summary>
    /// Oryginalny XML faktury (opcjonalny, do celów debugowania)
    /// </summary>
    public string? OriginalXml { get; set; }

    /// <summary>
    /// Data utworzenia kontekstu
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
