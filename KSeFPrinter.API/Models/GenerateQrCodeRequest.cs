namespace KSeFPrinter.API.Models;

/// <summary>
/// Request do generowania kodu QR dla numeru KSeF
/// </summary>
public class GenerateQrCodeRequest
{
    /// <summary>
    /// Numer KSeF faktury (format: XXXXXXXXXX-YYYYMMDD-XXXXXXXXXXXX-XX)
    /// </summary>
    public string KSeFNumber { get; set; } = null!;

    /// <summary>
    /// Użyj środowiska produkcyjnego (domyślnie: false = test)
    /// </summary>
    public bool UseProduction { get; set; } = false;

    /// <summary>
    /// Format kodu QR: "svg", "base64", "both" (domyślnie: "svg")
    /// </summary>
    public string Format { get; set; } = "svg";

    /// <summary>
    /// Rozmiar kodu QR w pikselach (dla PNG, domyślnie: 200)
    /// </summary>
    public int Size { get; set; } = 200;

    /// <summary>
    /// Czy dołączyć etykietę z numerem KSeF pod kodem (domyślnie: false)
    /// </summary>
    public bool IncludeLabel { get; set; } = false;
}
