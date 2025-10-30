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

                // Header pusty - wszystkie dane są w Content
                page.Header().Height(0);
                page.Content().Element(c => ComposeContent(c, faktura, context, options));
                page.Footer().Element(c => ComposeFooter(c, faktura));
            });
        });
    }

    /// <summary>
    /// Komponuje nagłówek faktury z podmiotami (tylko pierwsza strona)
    /// </summary>
    private void ComposeInvoiceHeader(IContainer container, Models.FA3.Faktura faktura, KSeFMetadata metadata)
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

                    if (faktura.Podmiot1.DaneKontaktowe != null && faktura.Podmiot1.DaneKontaktowe.Any())
                    {
                        foreach (var kontakt in faktura.Podmiot1.DaneKontaktowe)
                        {
                            if (!string.IsNullOrEmpty(kontakt.Email))
                                col.Item().Text($"Email: {kontakt.Email}").FontSize(8);
                            if (!string.IsNullOrEmpty(kontakt.Telefon))
                                col.Item().Text($"Tel: {kontakt.Telefon}").FontSize(8);
                        }
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

                    if (faktura.Podmiot2.DaneKontaktowe != null && faktura.Podmiot2.DaneKontaktowe.Any())
                    {
                        foreach (var kontakt in faktura.Podmiot2.DaneKontaktowe)
                        {
                            if (!string.IsNullOrEmpty(kontakt.Email))
                                col.Item().Text($"Email: {kontakt.Email}").FontSize(8);
                            if (!string.IsNullOrEmpty(kontakt.Telefon))
                                col.Item().Text($"Tel: {kontakt.Telefon}").FontSize(8);
                        }
                    }

                    // Pola specjalne Podmiot2
                    if (!string.IsNullOrEmpty(faktura.Podmiot2.NrKlienta))
                        col.Item().PaddingTop(3).Text($"Nr klienta: {faktura.Podmiot2.NrKlienta}").FontSize(8);

                    if (faktura.Podmiot2.JST == "1")
                        col.Item().PaddingTop(2).Text("Jednostka samorządu terytorialnego").FontSize(8).Italic();

                    if (faktura.Podmiot2.GV == "1")
                        col.Item().PaddingTop(2).Text("Gospodarka wodno-ściekowa").FontSize(8).Italic();
                });
            });

            // Podmiot3 (jeśli występuje - może być wiele)
            if (faktura.Podmiot3 != null && faktura.Podmiot3.Any())
            {
                for (int i = 0; i < faktura.Podmiot3.Count; i++)
                {
                    column.Item().PaddingTop(i == 0 ? 10 : 5).Element(c => ComposeInnyPodmiot(c, faktura.Podmiot3[i], i + 1));
                }
            }

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

            // Informacje o fakturze korygowanej (dla faktur korygujących: KOR, KOR_ZAL, KOR_ROZ)
            var rodzajFaktury = faktura.Fa.RodzajFaktury?.ToUpper();
            bool isCorrectionInvoice = rodzajFaktury == "KOR" || rodzajFaktury == "KOR_ZAL" || rodzajFaktury == "KOR_ROZ";

            if (isCorrectionInvoice && faktura.Fa.FaKorygowana != null)
            {
                column.Item().PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Darken1).PaddingTop(10)
                    .Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
                {
                    col.Item().Text("Faktura korygująca").FontSize(11).Bold();

                    var fk = faktura.Fa.FaKorygowana;

                    if (!string.IsNullOrEmpty(fk.NrFaKorygowanej))
                    {
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.ConstantItem(130).Text("Do faktury nr:").FontSize(9);
                            row.RelativeItem().Text(fk.NrFaKorygowanej).FontSize(9);
                        });
                    }

                    if (fk.DataWystFaKorygowanej.HasValue)
                    {
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(130).Text("z dnia:").FontSize(9);
                            row.RelativeItem().Text(fk.DataWystFaKorygowanej.Value.ToString("dd.MM.yyyy")).FontSize(9);
                        });
                    }

                    if (!string.IsNullOrEmpty(fk.NrKSeFFaKorygowanej))
                    {
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(130).Text("Nr KSeF:").FontSize(8);
                            row.RelativeItem().Text(fk.NrKSeFFaKorygowanej).FontSize(8);
                        });
                    }

                    // PrzyczynaKorekty jest w Fa, nie w FaKorygowana
                    if (!string.IsNullOrEmpty(faktura.Fa.PrzyczynaKorekty))
                    {
                        col.Item().PaddingTop(8).Text("Przyczyna korekty:").FontSize(9).Bold();
                        col.Item().PaddingTop(3).Text(faktura.Fa.PrzyczynaKorekty).FontSize(9);
                    }
                });
            }
        });
    }

    /// <summary>
    /// Komponuje zawartość dokumentu
    /// </summary>
    private void ComposeContent(IContainer container, Models.FA3.Faktura faktura, InvoiceContext context, PdfGenerationOptions options)
    {
        container.Column(column =>
        {
            // Nagłówek z podmiotami (tylko na pierwszej stronie)
            column.Item().Element(c => ComposeInvoiceHeader(c, faktura, context.Metadata));

            // Tabela pozycji
            column.Item().PaddingTop(20).Element(c => ComposeItemsTable(c, faktura));

            // Podsumowanie
            column.Item().PaddingTop(15).Element(c => ComposeSummary(c, faktura));

            // Adnotacje (jeśli są wypełnione) - pod podsumowaniem
            if (faktura.Fa.Adnotacje != null)
            {
                column.Item().PaddingTop(10).Element(c => ComposeAnnotations(c, faktura));
            }

            // Dodatkowe opisy (jeśli są wypełnione)
            if (faktura.Fa.DodatkowyOpis != null && faktura.Fa.DodatkowyOpis.Any())
            {
                column.Item().PaddingTop(10).Element(c => ComposeDodatkowyOpis(c, faktura));
            }

            // Płatność
            if (faktura.Fa.Platnosc != null)
            {
                column.Item().PaddingTop(15).Element(c => ComposePayment(c, faktura));
            }

            // Umowy (jeśli są)
            if (faktura.Fa.Umowy != null && faktura.Fa.Umowy.Any())
            {
                column.Item().PaddingTop(10).Element(c => ComposeUmowy(c, faktura));
            }

            // Zamówienia (jeśli są)
            if (faktura.Fa.Zamowienia != null && faktura.Fa.Zamowienia.Any())
            {
                column.Item().PaddingTop(10).Element(c => ComposeZamowienia(c, faktura));
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
        var hasExemption = !string.IsNullOrEmpty(adnotacje.P_16) && adnotacje.P_16 == "1";
        var hasSpecialProcedure = !string.IsNullOrEmpty(adnotacje.P_17) && adnotacje.P_17 == "1";
        var hasExemptionDetails = adnotacje.Zwolnienie != null && adnotacje.Zwolnienie.P_19N != "1";
        var hasNewVehicles = adnotacje.NoweSrodkiTransportu != null && adnotacje.NoweSrodkiTransportu.P_22N != "1";

        var hasAnyAnnotation = hasSplitPayment || hasReverseCharge || hasMarginProcedure || hasReceiptInvoice
            || hasExemption || hasSpecialProcedure || hasExemptionDetails || hasNewVehicles;

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

            // Zwolnienie - P_16
            if (hasExemption)
            {
                column.Item().PaddingTop(3).Text("• Zwolnienie z VAT")
                    .FontSize(9);
            }

            // Procedura szczególna - P_17
            if (hasSpecialProcedure)
            {
                column.Item().PaddingTop(3).Text("• Procedura szczególna")
                    .FontSize(9);
            }

            // Szczegóły zwolnienia - Zwolnienie.P_19N != "1"
            if (hasExemptionDetails)
            {
                column.Item().PaddingTop(3).Text("• Dotyczy zwolnienia (szczegóły w podstawie prawnej)")
                    .FontSize(9);
            }

            // Nowe środki transportu - NoweSrodkiTransportu.P_22N != "1"
            if (hasNewVehicles)
            {
                column.Item().PaddingTop(3).Text("• Dotyczy nowych środków transportu")
                    .FontSize(9);
            }
        });
    }

    /// <summary>
    /// Komponuje dodatkowe opisy faktury
    /// </summary>
    private void ComposeDodatkowyOpis(IContainer container, Models.FA3.Faktura faktura)
    {
        var opisy = faktura.Fa.DodatkowyOpis!;

        // Księgowy styl - delikatna szara ramka z jasnym tłem
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4).Padding(8).Column(column =>
        {
            // Nagłówek sekcji
            column.Item().Text("Informacje dodatkowe:").FontSize(9).Bold();

            // Każda para klucz-wartość
            foreach (var opis in opisy)
            {
                column.Item().PaddingTop(3).Text(text =>
                {
                    text.Span($"{opis.Klucz}: ").FontSize(9).SemiBold();
                    text.Span(opis.Wartosc).FontSize(9);
                });
            }
        });
    }

    /// <summary>
    /// Komponuje tabelę pozycji faktury
    /// </summary>
    private void ComposeItemsTable(IContainer container, Models.FA3.Faktura faktura)
    {
        // Sprawdź czy to faktura korygująca z wierszami StanPrzed
        var rodzajFaktury = faktura.Fa.RodzajFaktury?.ToUpper();
        bool isCorrectionInvoice = rodzajFaktury == "KOR" || rodzajFaktury == "KOR_ZAL" || rodzajFaktury == "KOR_ROZ";
        bool hasBeforeAfterLines = faktura.Fa.FaWiersz.Any(w => w.StanPrzed == "1");

        if (isCorrectionInvoice && hasBeforeAfterLines)
        {
            ComposeItemsTableWithCorrection(container, faktura);
            return;
        }

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

                // Nazwa towaru/usługi (z opcjonalnymi kodami)
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

                    // CN (kod nomenklatury celnej) - jeśli wypełniony
                    if (!string.IsNullOrEmpty(wiersz.CN))
                    {
                        col.Item().Text($"CN: {wiersz.CN}").FontSize(7).FontColor(Colors.Grey.Darken1);
                    }

                    // PKOB (klasyfikacja obiektów budowlanych) - jeśli wypełniony
                    if (!string.IsNullOrEmpty(wiersz.PKOB))
                    {
                        col.Item().Text($"PKOB: {wiersz.PKOB}").FontSize(7).FontColor(Colors.Grey.Darken1);
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

                // Wartość netto (opcjonalnie z brutto)
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Column(col =>
                {
                    col.Item().Text(wiersz.P_11.ToString("N2")).FontSize(9);

                    // P_11A (wartość brutto) - jeśli wypełnione
                    if (wiersz.P_11A.HasValue)
                    {
                        col.Item().Text($"brutto: {wiersz.P_11A.Value:N2}").FontSize(7).FontColor(Colors.Grey.Darken1);
                    }
                });

                // VAT %
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(wiersz.P_12).FontSize(9);
            }
        });
    }

    /// <summary>
    /// Komponuje 3 osobne tabele dla faktury korygującej: PRZED / RÓŻNICA / PO
    /// </summary>
    private void ComposeItemsTableWithCorrection(IContainer container, Models.FA3.Faktura faktura)
    {
        // Grupowanie wierszy przed/po
        var wiersze = faktura.Fa.FaWiersz;
        var grupy = new List<(Models.FA3.FaWiersz? przed, Models.FA3.FaWiersz? po)>();

        for (int i = 0; i < wiersze.Count; i++)
        {
            var wiersz = wiersze[i];

            if (wiersz.StanPrzed == "1")
            {
                var wierszPo = i + 1 < wiersze.Count && wiersze[i + 1].StanPrzed != "1" ? wiersze[i + 1] : null;
                grupy.Add((wiersz, wierszPo));
                if (wierszPo != null) i++;
            }
            else
            {
                grupy.Add((null, wiersz));
            }
        }

        container.Column(column =>
        {
            // 1. TABELA: Stan PRZED korektą
            column.Item().Text("Stan przed korektą").FontSize(10).Bold();
            column.Item().PaddingTop(5).Table(table =>
            {
                DefineStandardColumns(table);
                RenderStandardTableHeader(table);

                int lp = 1;
                foreach (var (przed, _) in grupy)
                {
                    if (przed != null)
                    {
                        RenderStandardRow(table, lp++, przed);
                    }
                }
            });

            // 2. TABELA: Różnice (korekta)
            column.Item().PaddingTop(15).Text("Wartość korekty").FontSize(10).Bold();
            column.Item().PaddingTop(5).Table(table =>
            {
                DefineStandardColumns(table);
                RenderStandardTableHeader(table);

                int lp = 1;
                foreach (var (przed, po) in grupy)
                {
                    if (przed != null && po != null)
                    {
                        RenderDifferenceRow(table, lp++, przed, po);
                    }
                }
            });

            // 3. TABELA: Stan PO korekcie (wszystkie pozycje - zmienione i niezmienione)
            column.Item().PaddingTop(15).Text("Stan po korekcie").FontSize(10).Bold();
            column.Item().PaddingTop(5).Table(table =>
            {
                DefineStandardColumns(table);
                RenderStandardTableHeader(table);

                int lp = 1;
                foreach (var (przed, po) in grupy)
                {
                    // Jeśli jest para (przed, po) - pokazujemy PO
                    // Jeśli jest tylko przed (bez zmian) - pokazujemy PRZED (bo to jest stan PO)
                    var wierszDoWyswietlenia = po ?? przed;
                    if (wierszDoWyswietlenia != null)
                    {
                        RenderStandardRow(table, lp++, wierszDoWyswietlenia);
                    }
                }
            });
        });
    }

    private void DefineStandardColumns(TableDescriptor table)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.ConstantColumn(25);      // Lp
            columns.ConstantColumn(60);      // Kod
            columns.RelativeColumn(3);       // Nazwa
            columns.ConstantColumn(40);      // JM
            columns.ConstantColumn(50);      // Ilość
            columns.ConstantColumn(60);      // Cena jedn.
            columns.ConstantColumn(70);      // Wartość netto
            columns.ConstantColumn(40);      // VAT%
        });
    }

    private void RenderStandardTableHeader(TableDescriptor table)
    {
        table.Header(header =>
        {
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Lp.").FontSize(8).Bold();
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Kod").FontSize(8).Bold();
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Nazwa towaru/usługi").FontSize(8).Bold();
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("JM").FontSize(8).Bold();
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Ilość").FontSize(8).Bold();
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Cena jedn.").FontSize(8).Bold();
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Wartość netto").FontSize(8).Bold();
            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("VAT%").FontSize(8).Bold();
        });
    }

    private void RenderStandardRow(TableDescriptor table, int lp, Models.FA3.FaWiersz wiersz)
    {
        // Lp
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignCenter().Text(lp.ToString()).FontSize(8);

        // Kod
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .Text(wiersz.P_6A ?? "").FontSize(7);

        // Nazwa
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .Text(wiersz.P_7).FontSize(8);

        // JM
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignCenter().Text(wiersz.P_8A ?? "").FontSize(8);

        // Ilość
        var ilosc = wiersz.P_8B.HasValue ? wiersz.P_8B.Value.ToString("N2") : "";
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignRight().Text(ilosc).FontSize(8);

        // Cena jedn.
        var cena = wiersz.P_9A.HasValue ? wiersz.P_9A.Value.ToString("N2") : "";
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignRight().Text(cena).FontSize(8);

        // Wartość netto
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignRight().Text(wiersz.P_11.ToString("N2")).FontSize(8);

        // VAT%
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignCenter().Text(wiersz.P_12).FontSize(8);
    }

    private void RenderDifferenceRow(TableDescriptor table, int lp, Models.FA3.FaWiersz przed, Models.FA3.FaWiersz po)
    {
        // Lp
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignCenter().Text(lp.ToString()).FontSize(8);

        // Kod
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .Text(po.P_6A ?? "").FontSize(7);

        // Nazwa
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .Text(po.P_7).FontSize(8);

        // JM
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignCenter().Text(po.P_8A ?? "").FontSize(8);

        // Różnica ilości
        var roznicaIlosc = (po.P_8B ?? 0) - (przed.P_8B ?? 0);
        var iloscText = roznicaIlosc != 0
            ? (roznicaIlosc > 0 ? "+" : "") + roznicaIlosc.ToString("N2")
            : "";
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignRight().Text(iloscText).FontSize(8);

        // Różnica ceny
        var roznicaCena = (po.P_9A ?? 0) - (przed.P_9A ?? 0);
        var cenaText = roznicaCena != 0
            ? (roznicaCena > 0 ? "+" : "") + roznicaCena.ToString("N2")
            : "";
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignRight().Text(cenaText).FontSize(8);

        // Różnica wartości netto
        var roznicaNetto = po.P_11 - przed.P_11;
        var nettoText = (roznicaNetto > 0 ? "+" : "") + roznicaNetto.ToString("N2");
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignRight().Text(nettoText).FontSize(8).Bold();

        // VAT%
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5)
            .AlignCenter().Text(po.P_12).FontSize(8);
    }


    /// <summary>
    /// Komponuje podsumowanie kwot
    /// </summary>
    private void ComposeSummary(IContainer container, Models.FA3.Faktura faktura)
    {
        var rodzajFaktury = faktura.Fa.RodzajFaktury?.ToUpper();
        bool isCorrectionInvoice = rodzajFaktury == "KOR" || rodzajFaktury == "KOR_ZAL" || rodzajFaktury == "KOR_ROZ";

        var isForeignCurrency = faktura.Fa.KodWaluty?.ToUpper() != "PLN";

        container.AlignRight().Column(column =>
        {
            // Podsumowanie wg stawek VAT
            var vatRates = new List<(string label, decimal? netto, decimal? vat, decimal? vatPln)>
            {
                ("23%/22%", faktura.Fa.P_13_1 != 0 ? faktura.Fa.P_13_1 : null, faktura.Fa.P_14_1 != 0 ? faktura.Fa.P_14_1 : null, faktura.Fa.P_14_1W),
                ("8%/7%", faktura.Fa.P_13_2, faktura.Fa.P_14_2, faktura.Fa.P_14_2W),
                ("5%", faktura.Fa.P_13_3, faktura.Fa.P_14_3, faktura.Fa.P_14_3W),
                ("pozostałe", faktura.Fa.P_13_4, faktura.Fa.P_14_4, faktura.Fa.P_14_4W),
                ("0%", faktura.Fa.P_13_5, faktura.Fa.P_14_5, null)
            };

            bool hasMultipleRates = vatRates.Count(r => r.netto.HasValue || r.vat.HasValue) > 1;

            if (hasMultipleRates)
            {
                column.Item().Text("Podsumowanie wg stawek VAT:").FontSize(9).Bold();
                column.Item().PaddingTop(3);

                foreach (var rate in vatRates.Where(r => r.netto.HasValue || r.vat.HasValue))
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(80).Text($"VAT {rate.label}:").FontSize(8);
                        if (rate.netto.HasValue)
                            row.ConstantItem(70).AlignRight().Text($"{rate.netto.Value:N2}").FontSize(8);
                        else
                            row.ConstantItem(70);
                        row.ConstantItem(20).AlignCenter().Text("+").FontSize(8);
                        if (rate.vat.HasValue)
                            row.ConstantItem(70).AlignRight().Text($"{rate.vat.Value:N2}").FontSize(8);
                        else
                            row.ConstantItem(70);
                        row.ConstantItem(20).AlignCenter().Text(faktura.Fa.KodWaluty).FontSize(7);
                    });
                }
                column.Item().PaddingTop(5);
            }

            // Suma netto
            column.Item().Row(row =>
            {
                var label = isCorrectionInvoice ? "Różnica netto:" : "Suma netto:";
                row.ConstantItem(150).Text(label).FontSize(10);

                var nettoPrefix = isCorrectionInvoice && faktura.Fa.P_13_1 >= 0 ? "+" : "";
                row.ConstantItem(100).AlignRight().Text($"{nettoPrefix}{faktura.Fa.P_13_1:N2} {faktura.Fa.KodWaluty}")
                    .FontSize(10);
            });

            // VAT w walucie obcej
            column.Item().Row(row =>
            {
                var label = isCorrectionInvoice ? "Różnica VAT:" : "Podatek VAT:";
                row.ConstantItem(150).Text(label).FontSize(10);

                var vatPrefix = isCorrectionInvoice && faktura.Fa.P_14_1 >= 0 ? "+" : "";
                row.ConstantItem(100).AlignRight().Text($"{vatPrefix}{faktura.Fa.P_14_1:N2} {faktura.Fa.KodWaluty}")
                    .FontSize(10);
            });

            // VAT w PLN (dla faktur w walucie obcej)
            if (isForeignCurrency && (faktura.Fa.P_14_1W.HasValue || faktura.Fa.P_14_2W.HasValue ||
                faktura.Fa.P_14_3W.HasValue || faktura.Fa.P_14_4W.HasValue))
            {
                column.Item().PaddingTop(8).Text("VAT przeliczony na PLN (art. 106e ust. 11):").FontSize(9).Bold();
                column.Item().PaddingTop(2);

                var vatPlnRates = new List<(string label, decimal? vatPln)>
                {
                    ("23%/22%", faktura.Fa.P_14_1W),
                    ("8%/7%", faktura.Fa.P_14_2W),
                    ("5%", faktura.Fa.P_14_3W),
                    ("pozostałe", faktura.Fa.P_14_4W)
                };

                bool hasMultipleVatPlnRates = vatPlnRates.Count(r => r.vatPln.HasValue && r.vatPln.Value != 0) > 1;

                // Pokaż rozbicie tylko jeśli jest więcej niż jedna stawka
                if (hasMultipleVatPlnRates)
                {
                    foreach (var rate in vatPlnRates.Where(r => r.vatPln.HasValue && r.vatPln.Value != 0))
                    {
                        column.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text($"VAT {rate.label} w PLN:").FontSize(9);
                            var vatPlnValue = rate.vatPln!.Value;
                            var vatPrefix = isCorrectionInvoice && vatPlnValue >= 0 ? "+" : "";
                            row.ConstantItem(100).AlignRight().Text($"{vatPrefix}{vatPlnValue:N2} PLN")
                                .FontSize(9);
                        });
                    }
                    column.Item().PaddingTop(3);
                }

                // Suma VAT w PLN (zawsze)
                var totalVatPln = (faktura.Fa.P_14_1W ?? 0) + (faktura.Fa.P_14_2W ?? 0) +
                                  (faktura.Fa.P_14_3W ?? 0) + (faktura.Fa.P_14_4W ?? 0);

                if (totalVatPln != 0)
                {
                    var label = isCorrectionInvoice ? "RÓŻNICA VAT w PLN:" : "RAZEM VAT w PLN:";
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(150).Text(label).FontSize(10).Bold();
                        var vatPrefix = isCorrectionInvoice && totalVatPln >= 0 ? "+" : "";
                        row.ConstantItem(100).AlignRight().Text($"{vatPrefix}{totalVatPln:N2} PLN")
                            .FontSize(10).Bold();
                    });
                }

                column.Item().PaddingTop(3);
            }

            // Suma brutto
            column.Item().PaddingTop(5).Row(row =>
            {
                var label = isCorrectionInvoice ? "RÓŻNICA BRUTTO:" : "RAZEM BRUTTO:";
                row.ConstantItem(150).Text(label).FontSize(12).Bold();

                var bruttoPrefix = isCorrectionInvoice && faktura.Fa.P_15 >= 0 ? "+" : "";
                row.ConstantItem(100).AlignRight().Text($"{bruttoPrefix}{faktura.Fa.P_15:N2} {faktura.Fa.KodWaluty}")
                    .FontSize(12).Bold();
            });

            // Rozliczenie - szczegółowe obciążenia i odliczenia
            if (faktura.Fa.Rozliczenie != null)
            {
                var rozliczenie = faktura.Fa.Rozliczenie;
                bool hasObciazenia = rozliczenie.Obciazenia != null && rozliczenie.Obciazenia.Any(o => o.Kwota > 0);
                bool hasOdliczenia = rozliczenie.Odliczenia != null && rozliczenie.Odliczenia.Any(o => o.Kwota > 0);

                if (hasObciazenia || hasOdliczenia)
                {
                    column.Item().PaddingTop(10).PaddingBottom(5);

                    // Obciążenia
                    if (hasObciazenia)
                    {
                        column.Item().Text("Obciążenia:").FontSize(9).Bold();
                        foreach (var obciazenie in rozliczenie.Obciazenia!.Where(o => o.Kwota > 0))
                        {
                            column.Item().Row(row =>
                            {
                                row.ConstantItem(150).PaddingLeft(10).Text($"• {obciazenie.Powod}").FontSize(9);
                                row.ConstantItem(100).AlignRight().Text($"{obciazenie.Kwota:N2} {faktura.Fa.KodWaluty}")
                                    .FontSize(9).FontColor(Colors.Red.Darken1);
                            });
                        }

                        if (rozliczenie.SumaObciazen > 0)
                        {
                            column.Item().Row(row =>
                            {
                                row.ConstantItem(150).Text("Suma obciążeń:").FontSize(9).Bold();
                                row.ConstantItem(100).AlignRight().Text($"{rozliczenie.SumaObciazen:N2} {faktura.Fa.KodWaluty}")
                                    .FontSize(9).Bold().FontColor(Colors.Red.Darken1);
                            });
                        }

                        column.Item().PaddingTop(5);
                    }

                    // Odliczenia
                    if (hasOdliczenia)
                    {
                        column.Item().Text("Odliczenia:").FontSize(9).Bold();
                        foreach (var odliczenie in rozliczenie.Odliczenia!.Where(o => o.Kwota > 0))
                        {
                            column.Item().Row(row =>
                            {
                                row.ConstantItem(150).PaddingLeft(10).Text($"• {odliczenie.Powod}").FontSize(9);
                                row.ConstantItem(100).AlignRight().Text($"-{odliczenie.Kwota:N2} {faktura.Fa.KodWaluty}")
                                    .FontSize(9).FontColor(Colors.Green.Darken1);
                            });
                        }

                        if (rozliczenie.SumaOdliczen > 0)
                        {
                            column.Item().Row(row =>
                            {
                                row.ConstantItem(150).Text("Suma odliczeń:").FontSize(9).Bold();
                                row.ConstantItem(100).AlignRight().Text($"-{rozliczenie.SumaOdliczen:N2} {faktura.Fa.KodWaluty}")
                                    .FontSize(9).Bold().FontColor(Colors.Green.Darken1);
                            });
                        }

                        column.Item().PaddingTop(5);
                    }
                }

                // DO ZAPŁATY (zawsze jeśli jest Rozliczenie)
                if (rozliczenie.DoZaplaty != faktura.Fa.P_15)
                {
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.ConstantItem(150).Text("DO ZAPŁATY:").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                        row.ConstantItem(100).AlignRight().Text($"{rozliczenie.DoZaplaty:N2} {faktura.Fa.KodWaluty}")
                            .FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    });
                }
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

            // Terminy płatności (może być wiele - np. raty)
            if (platnosc.TerminPlatnosci != null && platnosc.TerminPlatnosci.Any())
            {
                if (platnosc.TerminPlatnosci.Count > 1)
                    column.Item().Text($"Terminy płatności ({platnosc.TerminPlatnosci.Count} rat):").FontSize(9).Bold();
                else
                    column.Item().Text("Termin płatności:").FontSize(9).Bold();

                for (int i = 0; i < platnosc.TerminPlatnosci.Count; i++)
                {
                    var termin = platnosc.TerminPlatnosci[i];

                    if (termin.Termin.HasValue)
                    {
                        if (platnosc.TerminPlatnosci.Count > 1)
                            column.Item().Text($"  Rata {i + 1}: {termin.Termin.Value:dd.MM.yyyy}").FontSize(9);
                        else
                            column.Item().Text($"{termin.Termin.Value:dd.MM.yyyy}").FontSize(9);
                    }
                    else if (termin.TerminOpis != null)
                    {
                        var opis = $"{termin.TerminOpis.Ilosc} {termin.TerminOpis.Jednostka} {termin.TerminOpis.ZdarzeniePoczatkowe}";
                        if (platnosc.TerminPlatnosci.Count > 1)
                            column.Item().Text($"  Rata {i + 1}: {opis}").FontSize(9);
                        else
                            column.Item().Text(opis).FontSize(9);
                    }
                }
            }

            column.Item().Text($"Forma płatności: {Models.Common.FormaPlatnosci.GetOpis(platnosc.FormaPlatnosci)}").FontSize(9);

            // Kwota zapłacona (dla faktur częściowo/całkowicie opłaconych)
            if (platnosc.KwotaZaplacona.HasValue && platnosc.KwotaZaplacona.Value > 0)
            {
                column.Item().PaddingTop(3).Text($"Kwota zapłacona: {platnosc.KwotaZaplacona.Value:N2} {faktura.Fa.KodWaluty}")
                    .FontSize(9).FontColor(Colors.Green.Darken2).Bold();
            }

            // Rachunki bankowe (do 100)
            if (platnosc.RachunekBankowy != null && platnosc.RachunekBankowy.Any())
            {
                column.Item().PaddingTop(5).Text(platnosc.RachunekBankowy.Count > 1
                    ? $"Dane do przelewu ({platnosc.RachunekBankowy.Count} rachunków):"
                    : "Dane do przelewu:").FontSize(9).Bold();

                for (int i = 0; i < platnosc.RachunekBankowy.Count; i++)
                {
                    var rachunek = platnosc.RachunekBankowy[i];

                    if (i > 0)
                        column.Item().PaddingTop(5);

                    if (platnosc.RachunekBankowy.Count > 1)
                        column.Item().Text($"Rachunek {i + 1}:").FontSize(8).Bold();

                    column.Item().Text($"Nr rachunku: {rachunek.NrRB}").FontSize(9);

                    if (!string.IsNullOrEmpty(rachunek.NazwaBanku))
                        column.Item().Text($"Bank: {rachunek.NazwaBanku}").FontSize(8);

                    // SWIFT (dla rachunków zagranicznych)
                    if (!string.IsNullOrEmpty(rachunek.SWIFT))
                        column.Item().Text($"SWIFT/BIC: {rachunek.SWIFT}").FontSize(8);

                    // Opis rachunku (np. "PLN", "EUR", "Rachunek dewizowy")
                    if (!string.IsNullOrEmpty(rachunek.OpisRachunku))
                        column.Item().Text($"Opis: {rachunek.OpisRachunku}").FontSize(8).FontColor(Colors.Grey.Darken1);
                }
            }

            // Rachunki faktora (do 20)
            if (platnosc.RachunekBankowyFaktora != null && platnosc.RachunekBankowyFaktora.Any())
            {
                column.Item().PaddingTop(5).Text(platnosc.RachunekBankowyFaktora.Count > 1
                    ? $"Rachunki faktora ({platnosc.RachunekBankowyFaktora.Count}):"
                    : "Rachunek faktora:").FontSize(9).Bold().FontColor(Colors.Orange.Darken2);

                for (int i = 0; i < platnosc.RachunekBankowyFaktora.Count; i++)
                {
                    var rachunek = platnosc.RachunekBankowyFaktora[i];

                    if (i > 0)
                        column.Item().PaddingTop(5);

                    if (platnosc.RachunekBankowyFaktora.Count > 1)
                        column.Item().Text($"Rachunek faktora {i + 1}:").FontSize(8).Bold();

                    column.Item().Text($"Nr rachunku: {rachunek.NrRB}").FontSize(9);

                    if (!string.IsNullOrEmpty(rachunek.NazwaBanku))
                        column.Item().Text($"Bank: {rachunek.NazwaBanku}").FontSize(8);

                    if (!string.IsNullOrEmpty(rachunek.SWIFT))
                        column.Item().Text($"SWIFT/BIC: {rachunek.SWIFT}").FontSize(8);

                    if (!string.IsNullOrEmpty(rachunek.OpisRachunku))
                        column.Item().Text($"Opis: {rachunek.OpisRachunku}").FontSize(8).FontColor(Colors.Grey.Darken1);
                }
            }
        });
    }

    /// <summary>
    /// Komponuje sekcję umów
    /// </summary>
    private void ComposeUmowy(IContainer container, Models.FA3.Faktura faktura)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text($"UMOWY ({faktura.Fa.Umowy!.Count})").FontSize(10).Bold();

            foreach (var umowa in faktura.Fa.Umowy)
            {
                column.Item().PaddingTop(3).Row(row =>
                {
                    if (umowa.DataUmowy.HasValue)
                        row.ConstantItem(100).Text($"{umowa.DataUmowy.Value:dd.MM.yyyy}").FontSize(9);

                    if (!string.IsNullOrEmpty(umowa.NrUmowy))
                        row.RelativeItem().Text($"Nr: {umowa.NrUmowy}").FontSize(9);
                });
            }
        });
    }

    /// <summary>
    /// Komponuje sekcję zamówień
    /// </summary>
    private void ComposeZamowienia(IContainer container, Models.FA3.Faktura faktura)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text($"ZAMÓWIENIA ({faktura.Fa.Zamowienia!.Count})").FontSize(10).Bold();

            foreach (var zamowienie in faktura.Fa.Zamowienia)
            {
                column.Item().PaddingTop(3).Row(row =>
                {
                    if (zamowienie.DataZamowienia.HasValue)
                        row.ConstantItem(100).Text($"{zamowienie.DataZamowienia.Value:dd.MM.yyyy}").FontSize(9);

                    if (!string.IsNullOrEmpty(zamowienie.NrZamowienia))
                        row.RelativeItem().Text($"Nr: {zamowienie.NrZamowienia}").FontSize(9);
                });
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

            // SystemInfo (jeśli wypełniony)
            if (!string.IsNullOrEmpty(faktura.Naglowek?.SystemInfo))
            {
                column.Item().PaddingTop(5).AlignCenter().Text($"System: {faktura.Naglowek.SystemInfo}")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
            }

            // Data generowania
            column.Item().PaddingTop(10).AlignCenter().Text($"Dokument wygenerowany: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .FontSize(7).FontColor(Colors.Grey.Medium);
        });
    }

    /// <summary>
    /// Komponuje sekcję podmiotu trzeciego (Podmiot3) - według oficjalnego szablonu styl.xsl
    /// </summary>
    private void ComposeInnyPodmiot(IContainer container, Models.FA3.Podmiot podmiot, int numerPodmiotu = 1)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5).Padding(7).Column(column =>
        {
            // Nagłówek - taki sam styl jak SPRZEDAWCA/NABYWCA
            column.Item().Text($"PODMIOT TRZECI ({numerPodmiotu})").FontSize(8).Bold();

            column.Item().PaddingTop(3).Row(row =>
            {
                // Dane identyfikacyjne i adres
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"NIP: {podmiot.DaneIdentyfikacyjne.NIP}").FontSize(9);

                    if (!string.IsNullOrEmpty(podmiot.DaneIdentyfikacyjne.PESEL))
                        col.Item().Text($"PESEL: {podmiot.DaneIdentyfikacyjne.PESEL}").FontSize(9);

                    col.Item().Text($"Imię i nazwisko lub nazwa: {podmiot.DaneIdentyfikacyjne.Nazwa}").FontSize(9);

                    col.Item().Text(podmiot.Adres.AdresL1).FontSize(9);

                    var adresL2 = podmiot.Adres.AdresL2;
                    if (!string.IsNullOrEmpty(podmiot.Adres.KodKraju) && podmiot.Adres.KodKraju != "PL")
                        adresL2 += $", {podmiot.Adres.KodKraju}";
                    col.Item().Text(adresL2).FontSize(9);
                });

                // Dane kontaktowe
                row.RelativeItem().Column(col =>
                {
                    if (podmiot.DaneKontaktowe != null && podmiot.DaneKontaktowe.Any())
                    {
                        foreach (var kontakt in podmiot.DaneKontaktowe)
                        {
                            if (!string.IsNullOrEmpty(kontakt.Email))
                                col.Item().Text($"Email: {kontakt.Email}").FontSize(8);
                            if (!string.IsNullOrEmpty(kontakt.Telefon))
                                col.Item().Text($"Tel: {kontakt.Telefon}").FontSize(8);
                        }
                    }

                    if (!string.IsNullOrEmpty(podmiot.NrKlienta))
                        col.Item().PaddingTop(3).Text($"Nr klienta: {podmiot.NrKlienta}").FontSize(8);
                });
            });

            // Rola (jako osobna sekcja pod danymi kontaktowymi)
            if (!string.IsNullOrEmpty(podmiot.Rola))
            {
                column.Item().PaddingTop(3).Column(col =>
                {
                    col.Item().Text("Rola").FontSize(8).Bold();
                    col.Item().Text(GetRolaDescription(podmiot.Rola)).FontSize(8);
                });
            }

            // Udział procentowy (jeśli występuje)
            if (podmiot.UdzialProcentowy.HasValue)
            {
                column.Item().PaddingTop(2).Column(col =>
                {
                    col.Item().Text("Udział procentowy").FontSize(9).Bold();
                    col.Item().Text($"{podmiot.UdzialProcentowy.Value:F2}%").FontSize(9);
                });
            }
        });
    }

    /// <summary>
    /// Mapuje kod roli na pełny opis słowny (według oficjalnego szablonu styl.xsl)
    /// </summary>
    private string GetRolaDescription(string rola)
    {
        return rola switch
        {
            "1" => "Faktor",
            "2" => "Odbiorca (np. inny niż nabywca)",
            "3" => "Podmiot, który został przejęty lub przekształcony",
            "4" => "Dodatkowy nabywca (współnabywca)",
            "5" => "Podmiot wystawiający fakturę w imieniu i na rzecz podatnika",
            "6" => "Podmiot, na rzecz którego dokonywana jest płatność",
            "7" => "Jednostka samorządu terytorialnego - wystawca",
            "8" => "Jednostka samorządu terytorialnego - odbiorca",
            "9" => "Członek grupy VAT - wystawca",
            "10" => "Członek grupy VAT - odbiorca",
            "11" => "Pracownik (osoba fizyczna wykonująca pracę na podstawie stosunku pracy)",
            _ => $"Rola {rola}"
        };
    }
}
