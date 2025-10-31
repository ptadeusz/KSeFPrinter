namespace KSeFPrinter.API.Models;

/// <summary>
/// Response z wygenerowanym kodem QR
/// </summary>
public class GenerateQrCodeResponse
{
    /// <summary>
    /// Numer KSeF
    /// </summary>
    public string KSeFNumber { get; set; } = null!;

    /// <summary>
    /// Kod QR w formacie SVG (jeśli format="svg" lub format="both")
    /// </summary>
    public string? QrCodeSvg { get; set; }

    /// <summary>
    /// Kod QR w formacie PNG base64 (jeśli format="base64" lub format="both")
    /// </summary>
    public string? QrCodeBase64 { get; set; }

    /// <summary>
    /// Format zwróconego kodu QR
    /// </summary>
    public string Format { get; set; } = null!;

    /// <summary>
    /// URL weryfikacyjny do portalu KSeF
    /// </summary>
    public string VerificationUrl { get; set; } = null!;

    /// <summary>
    /// Rozmiar kodu QR w pikselach (dla PNG)
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Środowisko ("test" lub "production")
    /// </summary>
    public string Environment { get; set; } = null!;
}
