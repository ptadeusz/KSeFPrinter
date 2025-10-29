using Microsoft.Extensions.Logging;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace KSeFPrinter.Services.QrCode;

/// <summary>
/// Serwis do generowania kodów QR dla faktur KSeF
/// </summary>
public class QrCodeService
{
    private readonly ILogger<QrCodeService> _logger;

    // Minimalny rozmiar: 5 pikseli na moduł (zgodnie z dokumentacją KSeF)
    private const int DefaultPixelsPerModule = 5;

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generuje kod QR z URL
    /// </summary>
    /// <param name="url">URL do zakodowania</param>
    /// <param name="pixelsPerModule">Liczba pikseli na moduł (min 5)</param>
    /// <returns>Obraz QR jako tablica bajtów PNG</returns>
    public byte[] GenerateQrCode(string url, int pixelsPerModule = DefaultPixelsPerModule)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL nie może być pusty", nameof(url));
        }

        if (pixelsPerModule < 1)
        {
            throw new ArgumentException("Pikseli na moduł musi być >= 1", nameof(pixelsPerModule));
        }

        if (pixelsPerModule < DefaultPixelsPerModule)
        {
            _logger.LogWarning(
                "Rozmiar QR ({Size} px/moduł) jest mniejszy niż zalecany minimum ({Min} px/moduł)",
                pixelsPerModule, DefaultPixelsPerModule
            );
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            _logger.LogDebug(
                "Wygenerowano kod QR: URL length={UrlLength}, Size={Size} px/moduł, Image size={ImageSize} bytes",
                url.Length, pixelsPerModule, qrCodeImage.Length
            );

            return qrCodeImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania kodu QR");
            throw;
        }
    }

    /// <summary>
    /// Generuje kod QR z etykietą tekstową
    /// </summary>
    /// <param name="url">URL do zakodowania</param>
    /// <param name="label">Etykieta pod kodem QR</param>
    /// <param name="pixelsPerModule">Liczba pikseli na moduł</param>
    /// <returns>Obraz QR z etykietą jako tablica bajtów PNG</returns>
    public byte[] GenerateQrCodeWithLabel(
        string url,
        string label,
        int pixelsPerModule = DefaultPixelsPerModule)
    {
        var qrCodeImage = GenerateQrCode(url, pixelsPerModule);

        if (string.IsNullOrWhiteSpace(label))
        {
            return qrCodeImage;
        }

        return AddLabelToQrCode(qrCodeImage, label);
    }

    /// <summary>
    /// Dodaje etykietę tekstową pod kodem QR
    /// </summary>
    /// <param name="qrCodeImage">Obraz QR jako PNG</param>
    /// <param name="label">Tekst etykiety</param>
    /// <returns>Obraz QR z etykietą jako tablica bajtów PNG</returns>
    public byte[] AddLabelToQrCode(byte[] qrCodeImage, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return qrCodeImage;
        }

        try
        {
            using var qrBitmap = new Bitmap(new MemoryStream(qrCodeImage));

            // Oblicz wysokość dla tekstu (20% wysokości QR lub min 30px)
            var labelHeight = Math.Max(30, qrBitmap.Height / 5);
            var totalHeight = qrBitmap.Height + labelHeight;

            // Utwórz nowy bitmap z miejscem na etykietę
            using var resultBitmap = new Bitmap(qrBitmap.Width, totalHeight);
            using var graphics = Graphics.FromImage(resultBitmap);

            // Białe tło
            graphics.Clear(Color.White);

            // Rysuj kod QR
            graphics.DrawImage(qrBitmap, 0, 0);

            // Konfiguracja tekstu
            var fontSize = Math.Max(10, labelHeight / 3);
            using var font = new Font("Arial", fontSize, FontStyle.Regular);
            using var brush = new SolidBrush(Color.Black);

            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // Rysuj tekst
            var textRect = new RectangleF(
                0,
                qrBitmap.Height,
                qrBitmap.Width,
                labelHeight
            );
            graphics.DrawString(label, font, brush, textRect, format);

            // Konwertuj do PNG
            using var ms = new MemoryStream();
            resultBitmap.Save(ms, ImageFormat.Png);
            var result = ms.ToArray();

            _logger.LogDebug(
                "Dodano etykietę do kodu QR: Label='{Label}', Size={Size} bytes",
                label, result.Length
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dodawania etykiety do kodu QR");
            throw;
        }
    }

    /// <summary>
    /// Zapisuje kod QR do pliku
    /// </summary>
    /// <param name="qrCodeImage">Obraz QR jako PNG</param>
    /// <param name="filePath">Ścieżka do pliku</param>
    public void SaveQrCodeToFile(byte[] qrCodeImage, string filePath)
    {
        try
        {
            File.WriteAllBytes(filePath, qrCodeImage);
            _logger.LogInformation("Zapisano kod QR do pliku: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zapisywania kodu QR do pliku: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Generuje pełny kod QR I (weryfikacja faktury) z etykietą
    /// </summary>
    /// <param name="url">URL weryfikacyjny faktury</param>
    /// <param name="numerKSeF">Numer KSeF (jeśli null to "OFFLINE")</param>
    /// <param name="pixelsPerModule">Liczba pikseli na moduł</param>
    /// <returns>Obraz QR z etykietą</returns>
    public byte[] GenerateInvoiceQrCode(
        string url,
        string? numerKSeF = null,
        int pixelsPerModule = DefaultPixelsPerModule)
    {
        var label = numerKSeF ?? "OFFLINE";
        return GenerateQrCodeWithLabel(url, label, pixelsPerModule);
    }

    /// <summary>
    /// Generuje pełny kod QR II (weryfikacja certyfikatu) z etykietą
    /// </summary>
    /// <param name="url">URL weryfikacyjny certyfikatu</param>
    /// <param name="pixelsPerModule">Liczba pikseli na moduł</param>
    /// <returns>Obraz QR z etykietą</returns>
    public byte[] GenerateCertificateQrCode(
        string url,
        int pixelsPerModule = DefaultPixelsPerModule)
    {
        return GenerateQrCodeWithLabel(url, "Certyfikat KSeF wystawcy", pixelsPerModule);
    }
}
