using KSeF.Client.Core.Interfaces.Services;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using QRCoder;
using SkiaSharp;

namespace KSeF.Client.Api.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GenerateQrCode(string payloadUrl, int pixelsPerModule = 20, int qrCodeSize = 300)
    {
        using var gen = new QRCodeGenerator();
        using var qrData = gen.CreateQrCode(payloadUrl, QRCodeGenerator.ECCLevel.Default);

        int modules = qrData.ModuleMatrix.Count;
        float cellSize = qrCodeSize / (float)modules;

        var info = new SKImageInfo(qrCodeSize, qrCodeSize);
        using var surface = SKSurface.Create(info);
        var skCanvas = surface.Canvas;

        var canvas = new SkiaCanvas();
        canvas.Canvas = skCanvas;
        canvas.SetDisplayScale(1f);

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, qrCodeSize, qrCodeSize);

        canvas.FillColor = Colors.Black;
        for (int y = 0; y < modules; y++)
            for (int x = 0; x < modules; x++)
                if (qrData.ModuleMatrix[y][x])
                    canvas.FillRectangle(x * cellSize, y * cellSize, cellSize, cellSize);

        // Eksport PNG
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public byte[] ResizePng(byte[] pngBytes, int targetWidth, int targetHeight)
    {
        using var skBitmap = SKBitmap.Decode(pngBytes);
        var info = new SKImageInfo(targetWidth, targetHeight);
        using var surface = SKSurface.Create(info);
        var canvas = new SkiaCanvas() { Canvas = surface.Canvas };
        canvas.SetDisplayScale(1f);

        IImage image = new SkiaImage(skBitmap);
        canvas.DrawImage(image, 0, 0, targetWidth, targetHeight);

        using var snap = surface.Snapshot();
        using var encoded = snap.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    public byte[] AddLabelToQrCode(byte[] qrPng, string label, int fontSizePx = 14)
    {
        using var skBitmap = SKBitmap.Decode(qrPng);
        IImage qrImage = new SkiaImage(skBitmap);
        int width = skBitmap.Width;
        int height = skBitmap.Height;

        var font = new Font("Arial", fontSizePx);

        // Pomiar tekstu
        var measureCanvas = new SkiaCanvas() { Canvas = SKSurface.Create(new SKImageInfo(1, 1)).Canvas };
        measureCanvas.SetDisplayScale(1f);
        measureCanvas.Font = font;
        measureCanvas.FontSize = fontSizePx;
        var textSize = measureCanvas.GetStringSize(label, font, fontSizePx);
        float labelHeight = textSize.Height + 4;

        // Nowa powierzchnia dla połączonego obrazu
        var info = new SKImageInfo(width, height + (int)labelHeight);
        using var surface = SKSurface.Create(info);
        var canvas = new SkiaCanvas() { Canvas = surface.Canvas };
        canvas.SetDisplayScale(1f);

        // Tło
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, width, height + labelHeight);

        // Kod QR
        canvas.DrawImage(qrImage, 0, 0, width, height);

        // Rysuj etykietę
        canvas.Font = font;
        canvas.FontSize = fontSizePx;
        canvas.FontColor = Colors.Black;
        var rect = new RectF(0, height, width, labelHeight);
        canvas.DrawString(label, rect, HorizontalAlignment.Center, VerticalAlignment.Center);

        // Eksport PNG
        using var snap2 = surface.Snapshot();
        using var pngData = snap2.Encode(SKEncodedImageFormat.Png, 100);
        return pngData.ToArray();
    }
}