namespace KSeFPrinter.API.Models;

/// <summary>
/// Response z wynikami walidacji faktury
/// </summary>
public class InvoiceValidationResponse
{
    /// <summary>
    /// Czy faktura jest poprawna
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Lista błędów walidacji
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Lista ostrzeżeń
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Numer faktury (jeśli udało się sparsować)
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// NIP sprzedawcy (jeśli udało się sparsować)
    /// </summary>
    public string? SellerNip { get; set; }

    /// <summary>
    /// Tryb wystawienia (Online/Offline)
    /// </summary>
    public string? Mode { get; set; }
}
