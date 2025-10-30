using KSeFPrinter.Interfaces;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Cryptography;
using KSeFPrinter.Services.QrCode;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KSeFPrinter.Services.Pdf;

/// <summary>
/// Generator PDF dla faktur KSeF FA(3)
/// </summary>
public class InvoicePdfGenerator : IPdfGeneratorService
{
    private readonly VerificationLinkService _linkService;
    private readonly QrCodeService _qrService;
    private readonly ILogger<InvoicePdfGenerator> _logger;

    public InvoicePdfGenerator(
        VerificationLinkService linkService,
        QrCodeService qrService,
        ILogger<InvoicePdfGenerator> logger)
    {
        _linkService = linkService;
        _qrService = qrService;
        _logger = logger;

        // Konfiguracja licencji QuestPDF (Community License)
        QuestPDF.Settings.License = LicenseType.Community;

        // Włącz tryb debugowania dla lepszych komunikatów błędów
        QuestPDF.Settings.EnableDebugging = true;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GeneratePdfAsync(InvoiceContext context, string? outputPath = null)
    {
        if (outputPath == null)
        {
            return await GeneratePdfToBytesAsync(context);
        }

        await GeneratePdfToFileAsync(context, outputPath);
        return null;
    }

    /// <inheritdoc />
    public async Task GeneratePdfToFileAsync(InvoiceContext context, string outputPath)
    {
        _logger.LogInformation("Generowanie PDF do pliku: {OutputPath}", outputPath);

        var document = CreateDocument(context, new PdfGenerationOptions());
        document.GeneratePdf(outputPath);

        _logger.LogInformation("PDF został zapisany: {OutputPath}", outputPath);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<byte[]> GeneratePdfToBytesAsync(InvoiceContext context)
    {
        return await GeneratePdfToBytesAsync(context, new PdfGenerationOptions());
    }

    /// <summary>
    /// Generuje PDF z opcjami
    /// </summary>
    public async Task<byte[]> GeneratePdfToBytesAsync(InvoiceContext context, PdfGenerationOptions options)
    {
        _logger.LogInformation("Generowanie PDF do bajtów");

        var document = CreateDocument(context, options);
        var pdfBytes = document.GeneratePdf();

        _logger.LogInformation("PDF został wygenerowany: {Size} bajtów", pdfBytes.Length);
        return await Task.FromResult(pdfBytes);
    }

    /// <summary>
    /// Tworzy dokument QuestPDF
    /// </summary>
    private IDocument CreateDocument(InvoiceContext context, PdfGenerationOptions options)
    {
        var faktura = context.Faktura;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Calibri"));

                page.Header().Element(c => ComposeHeader(c, faktura, context.Metadata));
                page.Content().Element(c => ComposeContent(c, faktura, context, options));
                page.Footer().Element(c => ComposeFooter(c, faktura));
            });
        });
    }

    /// <summary>
    /// Komponuje nagłówek strony
    /// </summary>
    private void ComposeHeader(IContainer container, Models.FA3.Faktura faktura, KSeFMetadata metadata)
    {
        container.Column(column =>
        {
            // Sprzedawca i Nabywca
            column.Item().Row(row =>
            {
                // Lewa strona - Sprzedawca
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("SPRZEDAWCA").FontSize(8).Bold();
                    col.Item().Text(faktura.Podmiot1.DaneIdentyfikacyjne.Nazwa).FontSize(12).Bold();
                    col.Item().Text($"NIP: {faktura.Podmiot1.DaneIdentyfikacyjne.NIP}").FontSize(9);

                    // PESEL (jeśli wypełniony - dla osób fizycznych)
                    if (!string.IsNullOrEmpty(faktura.Podmiot1.DaneIdentyfikacyjne.PESEL))
                        col.Item().Text($"PESEL: {faktura.Podmiot1.DaneIdentyfikacyjne.PESEL}").FontSize(9);

                    col.Item().Text(faktura.Podmiot1.Adres.AdresL1).FontSize(9);

                    // Kod kraju (jeśli inny niż PL)
                    var adresL2 = faktura.Podmiot1.Adres.AdresL2;
                    if (!string.IsNullOrEmpty(faktura.Podmiot1.Adres.KodKraju) && faktura.Podmiot1.Adres.KodKraju != "PL")
                        adresL2 += $", {faktura.Podmiot1.Adres.KodKraju}";
                    col.Item().Text(adresL2).FontSize(9);

                    if (faktura.Podmiot1.DaneKontaktowe != null)
                    {
                        if (!string.IsNullOrEmpty(faktura.Podmiot1.DaneKontaktowe.Email))
                            col.Item().Text($"Email: {faktura.Podmiot1.DaneKontaktowe.Email}").FontSize(8);
                        if (!string.IsNullOrEmpty(faktura.Podmiot1.DaneKontaktowe.Telefon))
                            col.Item().Text($"Tel: {faktura.Podmiot1.DaneKontaktowe.Telefon}").FontSize(8);
                    }
                });

                row.ConstantItem(50);

                // Prawa strona - Nabywca
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("NABYWCA").FontSize(8).Bold();
                    col.Item().Text(faktura.Podmiot2.DaneIdentyfikacyjne.Nazwa).FontSize(12).Bold();
                    col.Item().Text($"NIP: {faktura.Podmiot2.DaneIdentyfikacyjne.NIP}").FontSize(9);

                    // PESEL (jeśli wypełniony - dla osób fizycznych)
                    if (!string.IsNullOrEmpty(faktura.Podmiot2.DaneIdentyfikacyjne.PESEL))
                        col.Item().Text($"PESEL: {faktura.Podmiot2.DaneIdentyfikacyjne.PESEL}").FontSize(9);

                    col.Item().Text(faktura.Podmiot2.Adres.AdresL1).FontSize(9);

                    // Kod kraju (jeśli inny niż PL)
                    var adresL2 = faktura.Podmiot2.Adres.AdresL2;
                    if (!string.IsNullOrEmpty(faktura.Podmiot2.Adres.KodKraju) && faktura.Podmiot2.Adres.KodKraju != "PL")
                        adresL2 += $", {faktura.Podmiot2.Adres.KodKraju}";
                    col.Item().Text(adresL2).FontSize(9);

                    if (faktura.Podmiot2.DaneKontaktowe != null)
                    {
                        if (!string.IsNullOrEmpty(faktura.Podmiot2.DaneKontaktowe.Email))
                            col.Item().Text($"Email: {faktura.Podmiot2.DaneKontaktowe.Email}").FontSize(8);
                        if (!string.IsNullOrEmpty(faktura.Podmiot2.DaneKontaktowe.Telefon))
                            col.Item().Text($"Tel: {faktura.Podmiot2.DaneKontaktowe.Telefon}").FontSize(8);
                    }
                });
            });

            // Separator
            column.Item().PaddingTop(10).PaddingBottom(5);

            // Informacje o fakturze
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"FAKTURA {faktura.Fa.RodzajFaktury}").FontSize(16).Bold();
                    col.Item().Text($"Nr: {faktura.Fa.P_2}").FontSize(12);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignRight().Text($"Data wystawienia: {faktura.Fa.P_1:dd.MM.yyyy}").FontSize(10);
                    if (!string.IsNullOrEmpty(faktura.Fa.P_1M))
                        col.Item().AlignRight().Text($"Miejsce: {faktura.Fa.P_1M}").FontSize(10);

                    // Okres objęty fakturą (OkresFa)
                    if (faktura.Fa.OkresFa != null)
                    {
                        col.Item().AlignRight().Text($"Okres: {faktura.Fa.OkresFa.P_6_Od:dd.MM.yyyy} - {faktura.Fa.OkresFa.P_6_Do:dd.MM.yyyy}")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    }

                    if (metadata.Tryb == TrybWystawienia.Online && !string.IsNullOrEmpty(metadata.NumerKSeF))
                    {
                        col.Item().AlignRight().Text($"Numer KSeF: {metadata.NumerKSeF}").FontSize(9).Bold();
                    }
                    else
                    {
                        col.Item().AlignRight().Text("Tryb: OFFLINE").FontSize(9).Bold().FontColor(Colors.Orange.Darken2);
                    }
                });
            });
        });
    }

    /// <summary>
    /// Komponuje zawartość dokumentu
    /// </summary>
    private void ComposeContent(IContainer container, Models.FA3.Faktura faktura, InvoiceContext context, PdfGenerationOptions options)
    {
        container.PaddingTop(20).Column(column =>
        {
            // Tabela pozycji
            column.Item().Element(c => ComposeItemsTable(c, faktura));

            // Podsumowanie
            column.Item().PaddingTop(15).Element(c => ComposeSummary(c, faktura));

            // Adnotacje (jeśli są wypełnione) - pod podsumowaniem
            if (faktura.Fa.Adnotacje != null)
            {
                column.Item().PaddingTop(10).Element(c => ComposeAnnotations(c, faktura));
            }

            // Płatność
            if (faktura.Fa.Platnosc != null)
            {
                column.Item().PaddingTop(15).Element(c => ComposePayment(c, faktura));
            }

            // Kody QR
            column.Item().PaddingTop(20).Element(c => ComposeQrCodes(c, context, options));
        });
    }

    /// <summary>
    /// Komponuje adnotacje prawne faktury
    /// </summary>
    private void ComposeAnnotations(IContainer container, Models.FA3.Faktura faktura)
    {
        var adnotacje = faktura.Fa.Adnotacje!;

        // Sprawdź czy są jakiekolwiek aktywne adnotacje
        var hasSplitPayment = !string.IsNullOrEmpty(adnotacje.P_18A) && adnotacje.P_18A == "1";
        var hasReverseCharge = !string.IsNullOrEmpty(adnotacje.P_18) && adnotacje.P_18 == "1";
        var hasMarginProcedure = adnotacje.PMarzy != null && adnotacje.PMarzy.P_PMarzyN != "1";
        var hasReceiptInvoice = !string.IsNullOrEmpty(adnotacje.P_23) && adnotacje.P_23 == "1";

        var hasAnyAnnotation = hasSplitPayment || hasReverseCharge || hasMarginProcedure || hasReceiptInvoice;

        // Jeśli nie ma żadnych aktywnych adnotacji, nie renderuj sekcji wcale
        if (!hasAnyAnnotation)
        {
            return;
        }

        // Księgowy styl - delikatna szara ramka z jasnym tłem
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4).Padding(8).Column(column =>
        {
            // Nagłówek sekcji
            column.Item().Text("Adnotacje:").FontSize(9).Bold();

            // Split Payment (Mechanizm podzielonej płatności) - P_18A
            if (hasSplitPayment)
            {
                column.Item().PaddingTop(3).Text("• Mechanizm podzielonej płatności (split payment)")
                    .FontSize(9);
            }

            // Odwrotne obciążenie (Reverse charge) - P_18
            if (hasReverseCharge)
            {
                column.Item().PaddingTop(3).Text("• Odwrotne obciążenie - podatek rozlicza nabywca")
                    .FontSize(9);
            }

            // Procedura marży - PMarzy
            if (hasMarginProcedure)
            {
                column.Item().PaddingTop(3).Text("• Procedura marży")
                    .FontSize(9);
            }

            // Faktura do paragonu - P_23
            if (hasReceiptInvoice)
            {
                column.Item().PaddingTop(3).Text("• Faktura wystawiona do paragonu")
                    .FontSize(9);
            }
        });
    }

    /// <summary>
    /// Komponuje tabelę pozycji faktury
    /// </summary>
    private void ComposeItemsTable(IContainer container, Models.FA3.Faktura faktura)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);  // Lp
                columns.RelativeColumn(4);    // Nazwa
                columns.ConstantColumn(60);   // Ilość
                columns.ConstantColumn(70);   // Cena jedn.
                columns.ConstantColumn(70);   // Wartość netto
                columns.ConstantColumn(50);   // VAT %
            });

            // Nagłówek tabeli
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Lp.").FontSize(9).Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Nazwa towaru/usługi").FontSize(9).Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Ilość").FontSize(9).Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Cena jedn.").FontSize(9).Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Wartość netto").FontSize(9).Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("VAT %").FontSize(9).Bold();
            });

            // Wiersze
            foreach (var wiersz in faktura.Fa.FaWiersz)
            {
                // Lp.
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(wiersz.NrWierszaFa.ToString()).FontSize(9);

                // Nazwa towaru/usługi (z opcjonalnym kodem towaru i GTU)
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(col =>
                {
                    col.Item().Text(wiersz.P_7).FontSize(9);

                    // Kod towaru (P_6A) - jeśli wypełniony
                    if (!string.IsNullOrEmpty(wiersz.P_6A))
                    {
                        col.Item().Text($"Kod: {wiersz.P_6A}").FontSize(7).FontColor(Colors.Grey.Darken1);
                    }

                    // GTU (Grupa Towarowa Usługowa) - jeśli wypełniony
                    if (!string.IsNullOrEmpty(wiersz.GTU))
                    {
                        col.Item().Text($"GTU: {wiersz.GTU}").FontSize(7).FontColor(Colors.Grey.Darken1);
                    }
                });

                // Ilość (z opcjonalną jednostką miary)
                var iloscText = wiersz.P_8B?.ToString("N2") ?? "-";
                if (wiersz.P_8B.HasValue && !string.IsNullOrEmpty(wiersz.P_8A))
                {
                    iloscText += $" {wiersz.P_8A}";
                }
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(iloscText).FontSize(9);

                // Cena jednostkowa
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(wiersz.P_9A?.ToString("N2") ?? "-").FontSize(9);

                // Wartość netto
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(wiersz.P_11.ToString("N2")).FontSize(9);

                // VAT %
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(wiersz.P_12).FontSize(9);
            }
        });
    }

    /// <summary>
    /// Komponuje podsumowanie kwot
    /// </summary>
    private void ComposeSummary(IContainer container, Models.FA3.Faktura faktura)
    {
        container.AlignRight().Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(150).Text("Suma netto:").FontSize(10);
                row.ConstantItem(100).AlignRight().Text($"{faktura.Fa.P_13_1:N2} {faktura.Fa.KodWaluty}").FontSize(10);
            });

            column.Item().Row(row =>
            {
                row.ConstantItem(150).Text("Podatek VAT:").FontSize(10);
                row.ConstantItem(100).AlignRight().Text($"{faktura.Fa.P_14_1:N2} {faktura.Fa.KodWaluty}").FontSize(10);
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.ConstantItem(150).Text("RAZEM BRUTTO:").FontSize(12).Bold();
                row.ConstantItem(100).AlignRight().Text($"{faktura.Fa.P_15:N2} {faktura.Fa.KodWaluty}").FontSize(12).Bold();
            });

            if (faktura.Fa.Rozliczenie != null && faktura.Fa.Rozliczenie.DoZaplaty != faktura.Fa.P_15)
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.ConstantItem(150).Text("DO ZAPŁATY:").FontSize(12).Bold().FontColor(Colors.Green.Darken2);
                    row.ConstantItem(100).AlignRight().Text($"{faktura.Fa.Rozliczenie.DoZaplaty:N2} {faktura.Fa.KodWaluty}")
                        .FontSize(12).Bold().FontColor(Colors.Green.Darken2);
                });
            }
        });
    }

    /// <summary>
    /// Komponuje informacje o płatności
    /// </summary>
    private void ComposePayment(IContainer container, Models.FA3.Faktura faktura)
    {
        var platnosc = faktura.Fa.Platnosc!;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("PŁATNOŚĆ").FontSize(10).Bold();

            if (platnosc.TerminPlatnosci != null)
            {
                column.Item().Text($"Termin płatności: {platnosc.TerminPlatnosci.Termin:dd.MM.yyyy}").FontSize(9);
            }

            column.Item().Text($"Forma płatności: {Models.Common.FormaPlatnosci.GetOpis(platnosc.FormaPlatnosci)}").FontSize(9);

            if (platnosc.RachunekBankowy != null)
            {
                column.Item().PaddingTop(5).Text("Dane do przelewu:").FontSize(9).Bold();
                column.Item().Text($"Nr rachunku: {platnosc.RachunekBankowy.NrRB}").FontSize(9);
                if (!string.IsNullOrEmpty(platnosc.RachunekBankowy.NazwaBanku))
                    column.Item().Text($"Bank: {platnosc.RachunekBankowy.NazwaBanku}").FontSize(8);
            }
        });
    }

    /// <summary>
    /// Komponuje kody QR
    /// </summary>
    private void ComposeQrCodes(IContainer container, InvoiceContext context, PdfGenerationOptions options)
    {
        var faktura = context.Faktura;
        var metadata = context.Metadata;

        // Przygotuj dane do kodów QR
        byte[]? qrCode1 = null;
        byte[]? qrCode2 = null;

        if (options.IncludeQrCode1 && context.OriginalXml != null)
        {
            try
            {
                var url1 = _linkService.BuildInvoiceVerificationUrl(
                    faktura.Podmiot1.DaneIdentyfikacyjne.NIP,
                    faktura.Fa.P_1,
                    context.OriginalXml,
                    metadata.NumerKSeF,
                    options.UseProduction
                );

                qrCode1 = _qrService.GenerateInvoiceQrCode(url1, metadata.NumerKSeF, options.QrPixelsPerModule);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie udało się wygenerować kodu QR I");
            }
        }

        if (options.IncludeQrCode2 && options.Certificate != null && context.OriginalXml != null)
        {
            try
            {
                var url2 = _linkService.BuildCertificateVerificationUrl(
                    options.Certificate,
                    faktura.Podmiot1.DaneIdentyfikacyjne.NIP,
                    context.OriginalXml,
                    options.UseProduction
                );

                qrCode2 = _qrService.GenerateCertificateQrCode(url2, options.QrPixelsPerModule);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie udało się wygenerować kodu QR II");
            }
        }

        // Wyświetl kody QR jeśli są dostępne
        if (qrCode1 != null || qrCode2 != null)
        {
            container.Row(row =>
            {
                if (qrCode1 != null)
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().AlignCenter().Width(150).Image(qrCode1);
                    });
                }

                if (qrCode1 != null && qrCode2 != null)
                {
                    row.ConstantItem(20); // Odstęp
                }

                if (qrCode2 != null)
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().AlignCenter().Width(150).Image(qrCode2);
                    });
                }
            });
        }
    }

    /// <summary>
    /// Komponuje stopkę dokumentu
    /// </summary>
    private void ComposeFooter(IContainer container, Models.FA3.Faktura faktura)
    {
        container.Column(column =>
        {
            if (faktura.Stopka != null)
            {
                if (faktura.Stopka.Informacje?.StopkaFaktury != null)
                {
                    column.Item().PaddingTop(10).Text(faktura.Stopka.Informacje.StopkaFaktury).FontSize(8).Italic();
                }

                if (faktura.Stopka.Rejestry != null)
                {
                    var rejestry = new List<string>();
                    if (!string.IsNullOrEmpty(faktura.Stopka.Rejestry.KRS))
                        rejestry.Add($"KRS: {faktura.Stopka.Rejestry.KRS}");
                    if (!string.IsNullOrEmpty(faktura.Stopka.Rejestry.REGON))
                        rejestry.Add($"REGON: {faktura.Stopka.Rejestry.REGON}");
                    if (!string.IsNullOrEmpty(faktura.Stopka.Rejestry.BDO))
                        rejestry.Add($"BDO: {faktura.Stopka.Rejestry.BDO}");

                    if (rejestry.Any())
                    {
                        column.Item().PaddingTop(5).Text(string.Join(" | ", rejestry)).FontSize(8);
                    }
                }
            }

            // Data generowania
            column.Item().PaddingTop(10).AlignCenter().Text($"Dokument wygenerowany: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .FontSize(7).FontColor(Colors.Grey.Medium);
        });
    }
}
