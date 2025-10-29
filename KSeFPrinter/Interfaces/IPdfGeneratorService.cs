using KSeFPrinter.Models.Common;

namespace KSeFPrinter.Interfaces;

/// <summary>
/// Interfejs serwisu generowania PDF
/// </summary>
public interface IPdfGeneratorService
{
    /// <summary>
    /// Generuje PDF faktury z kontekstu
    /// </summary>
    /// <param name="context">Kontekst faktury (faktura + metadane KSeF)</param>
    /// <param name="outputPath">Ścieżka do pliku PDF (jeśli null, zwraca bajty)</param>
    /// <returns>Bajty PDF jeśli outputPath jest null</returns>
    Task<byte[]?> GeneratePdfAsync(InvoiceContext context, string? outputPath = null);

    /// <summary>
    /// Generuje PDF faktury i zapisuje do pliku
    /// </summary>
    /// <param name="context">Kontekst faktury</param>
    /// <param name="outputPath">Ścieżka do pliku PDF</param>
    Task GeneratePdfToFileAsync(InvoiceContext context, string outputPath);

    /// <summary>
    /// Generuje PDF faktury i zwraca jako bajty
    /// </summary>
    /// <param name="context">Kontekst faktury</param>
    /// <returns>Bajty PDF</returns>
    Task<byte[]> GeneratePdfToBytesAsync(InvoiceContext context);
}
