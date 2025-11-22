using Microsoft.AspNetCore.Mvc;
using KSeFPrinter;
using KSeFPrinter.API.Models;
using KSeFPrinter.API.Services;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.QrCode;
using KSeFPrinter.Validators;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Reflection;

namespace KSeFPrinter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InvoiceController : ControllerBase
{
    private readonly InvoicePrinterService _printerService;
    private readonly CertificateService _certificateService;
    private readonly QrCodeService _qrCodeService;
    private readonly VerificationLinkService _verificationLinkService;
    private readonly KSeFPrinter.Services.License.ILicenseValidator _licenseValidator;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        InvoicePrinterService printerService,
        CertificateService certificateService,
        QrCodeService qrCodeService,
        VerificationLinkService verificationLinkService,
        KSeFPrinter.Services.License.ILicenseValidator licenseValidator,
        ILogger<InvoiceController> logger)
    {
        _printerService = printerService;
        _certificateService = certificateService;
        _qrCodeService = qrCodeService;
        _verificationLinkService = verificationLinkService;
        _licenseValidator = licenseValidator;
        _logger = logger;
    }

    /// <summary>
    /// Waliduje fakturę XML
    /// </summary>
    /// <param name="request">Request z zawartością XML</param>
    /// <returns>Wynik walidacji</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(InvoiceValidationResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InvoiceValidationResponse>> ValidateInvoice(
        [FromBody] InvoiceValidationRequest request)
    {
        try
        {
            var xmlContent = request.IsBase64
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.XmlContent))
                : request.XmlContent;

            var validationResult = await _printerService.ValidateInvoiceFromXmlAsync(
                xmlContent,
                request.KSeFNumber);

            // Próbuj sparsować podstawowe dane
            string? invoiceNumber = null;
            string? sellerNip = null;
            string? mode = null;

            try
            {
                var context = await _printerService.ParseInvoiceFromXmlAsync(xmlContent, request.KSeFNumber);
                invoiceNumber = context.Faktura.Fa.P_2;
                sellerNip = context.Faktura.Podmiot1.DaneIdentyfikacyjne.NIP;
                mode = context.Metadata.Tryb.ToString();
            }
            catch
            {
                // Ignoruj błędy parsowania - walidacja może się nie powieść
            }

            return Ok(new InvoiceValidationResponse
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors.ToList(),
                Warnings = validationResult.Warnings.ToList(),
                InvoiceNumber = invoiceNumber,
                SellerNip = sellerNip,
                Mode = mode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas walidacji faktury");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Parsuje fakturę XML i zwraca dane jako JSON
    /// </summary>
    /// <param name="request">Request z zawartością XML</param>
    /// <returns>Dane faktury</returns>
    [HttpPost("parse")]
    [ProducesResponseType(typeof(InvoiceParseResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InvoiceParseResponse>> ParseInvoice(
        [FromBody] InvoiceValidationRequest request)
    {
        try
        {
            var xmlContent = request.IsBase64
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.XmlContent))
                : request.XmlContent;

            var context = await _printerService.ParseInvoiceFromXmlAsync(
                xmlContent,
                request.KSeFNumber);

            var faktura = context.Faktura;
            var metadata = context.Metadata;

            // Mapuj dane na response
            var response = new InvoiceParseResponse
            {
                InvoiceNumber = faktura.Fa.P_2,
                IssueDate = faktura.Fa.P_1,
                Mode = metadata.Tryb.ToString(),
                KSeFNumber = metadata.NumerKSeF,
                Seller = new SellerInfo
                {
                    Name = faktura.Podmiot1.DaneIdentyfikacyjne.Nazwa,
                    Nip = faktura.Podmiot1.DaneIdentyfikacyjne.NIP,
                    Address = $"{faktura.Podmiot1.Adres?.AdresL1}, {faktura.Podmiot1.Adres?.AdresL2}".Trim(',', ' '),
                    Email = faktura.Podmiot1.DaneKontaktowe?.FirstOrDefault()?.Email,
                    Phone = faktura.Podmiot1.DaneKontaktowe?.FirstOrDefault()?.Telefon
                },
                Buyer = new BuyerInfo
                {
                    Name = faktura.Podmiot2.DaneIdentyfikacyjne.Nazwa,
                    Nip = faktura.Podmiot2.DaneIdentyfikacyjne.NIP,
                    Address = $"{faktura.Podmiot2.Adres?.AdresL1}, {faktura.Podmiot2.Adres?.AdresL2}".Trim(',', ' '),
                    Email = faktura.Podmiot2.DaneKontaktowe?.FirstOrDefault()?.Email,
                    Phone = faktura.Podmiot2.DaneKontaktowe?.FirstOrDefault()?.Telefon
                },
                Lines = faktura.Fa.FaWiersz.Select(w => new InvoiceLineInfo
                {
                    LineNumber = w.NrWierszaFa,
                    Description = w.P_7,
                    Quantity = w.P_8B,
                    UnitPrice = w.P_9A,
                    NetAmount = w.P_11,
                    VatRate = w.P_12
                }).ToList(),
                Summary = new SummaryInfo
                {
                    TotalNet = faktura.Fa.P_13_1,
                    TotalVat = faktura.Fa.P_14_1,
                    TotalGross = faktura.Fa.P_15,
                    Currency = faktura.Fa.KodWaluty
                },
                Payment = faktura.Fa.Platnosc != null ? new PaymentInfo
                {
                    DueDates = faktura.Fa.Platnosc.TerminPlatnosci?
                        .Where(t => t.Termin.HasValue)
                        .Select(t => t.Termin!.Value)
                        .ToList(),
                    PaymentMethod = faktura.Fa.Platnosc.FormaPlatnosci,
                    BankAccounts = faktura.Fa.Platnosc.RachunekBankowy?.Select(r => r.NrRB).ToList(),
                    FactorBankAccounts = faktura.Fa.Platnosc.RachunekBankowyFaktora?.Select(r => r.NrRB).ToList()
                } : null
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas parsowania faktury");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generuje PDF z faktury XML
    /// </summary>
    /// <param name="request">Request z zawartością XML i opcjami</param>
    /// <returns>Plik PDF lub base64</returns>
    [HttpPost("generate-pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200, "application/pdf")]
    [ProducesResponseType(typeof(GeneratePdfResponse), 200, "application/json")]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GeneratePdf([FromBody] GeneratePdfRequest request)
    {
        try
        {
            var xmlContent = request.IsBase64
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.XmlContent))
                : request.XmlContent;

            // Parsuj fakturę aby uzyskać NIP-y
            var context = await _printerService.ParseInvoiceFromXmlAsync(xmlContent, request.KSeFNumber);

            // === WALIDACJA NIP - sprawdź czy przynajmniej jeden NIP jest dozwolony ===
            var allowedNips = _licenseValidator.GetAllowedNips();

            var nipSprzedawcy = context.Faktura.Podmiot1?.DaneIdentyfikacyjne?.NIP;
            var nipNabywcy = context.Faktura.Podmiot2?.DaneIdentyfikacyjne?.NIP;
            var nipyPodmiot3 = context.Faktura.Podmiot3?.Select(p => p.DaneIdentyfikacyjne?.NIP).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string?>();

            // Sprawdź czy KTÓRYKOLWIEK z NIP-ów jest na liście dozwolonych
            bool anyNipAllowed = false;
            List<string> foundNips = new();

            if (!string.IsNullOrEmpty(nipSprzedawcy) && allowedNips.Contains(nipSprzedawcy))
            {
                anyNipAllowed = true;
                foundNips.Add($"Sprzedawca: {nipSprzedawcy}");
            }

            if (!string.IsNullOrEmpty(nipNabywcy) && allowedNips.Contains(nipNabywcy))
            {
                anyNipAllowed = true;
                foundNips.Add($"Nabywca: {nipNabywcy}");
            }

            foreach (var nip in nipyPodmiot3.Where(n => !string.IsNullOrEmpty(n)))
            {
                if (allowedNips.Contains(nip!))
                {
                    anyNipAllowed = true;
                    foundNips.Add($"Podmiot3: {nip}");
                }
            }

            if (!anyNipAllowed)
            {
                var allNips = new List<string>();
                if (!string.IsNullOrEmpty(nipSprzedawcy)) allNips.Add($"Sprzedawca: {nipSprzedawcy}");
                if (!string.IsNullOrEmpty(nipNabywcy)) allNips.Add($"Nabywca: {nipNabywcy}");
                allNips.AddRange(nipyPodmiot3.Select((n, i) => $"Podmiot3[{i}]: {n}"));

                _logger.LogWarning(
                    "❌ Brak uprawnień: żaden NIP z faktury nie jest dozwolony w licencji. NIP-y w fakturze: {Nips}",
                    string.Join(", ", allNips));

                return StatusCode(403, new
                {
                    error = "Licencja nie obejmuje żadnego NIP-u z tej faktury",
                    invoiceNips = allNips,
                    allowedNips = allowedNips.Count,
                    message = "Przynajmniej jeden NIP (sprzedawcy, nabywcy lub podmiotu 3) musi być na liście dozwolonych NIP-ów w licencji"
                });
            }

            _logger.LogInformation("✅ Walidacja NIP pomyślna: {FoundNips}", string.Join(", ", foundNips));

            // === CERTYFIKAT - wybierz odpowiedni dla trybu faktury ===
            X509Certificate2? certificate = null;
            var isOnline = context.Metadata.Tryb == TrybWystawienia.Online;

            if (isOnline)
            {
                certificate = _certificateService.GetOnlineCertificate();
                if (certificate != null)
                {
                    _logger.LogDebug("Używam certyfikatu ONLINE: {Subject}", certificate.Subject);
                }
            }
            else
            {
                certificate = _certificateService.GetOfflineCertificate();
                if (certificate != null)
                {
                    _logger.LogDebug("Używam certyfikatu OFFLINE: {Subject}", certificate.Subject);
                }
            }

            // Opcje generowania PDF
            var options = new PdfGenerationOptions
            {
                Certificate = certificate,
                UseProduction = request.UseProduction,
                IncludeQrCode1 = true,
                IncludeQrCode2 = certificate != null,
                SourceFile = request.SourceFile // Przekaż metadata pliku źródłowego (opcjonalne)
            };

            // Generuj PDF jako bajty
            var pdfBytes = await _printerService.GeneratePdfFromXmlAsync(
                xmlContent: xmlContent,
                pdfOutputPath: null, // zwróć bajty
                options: options,
                numerKSeF: request.KSeFNumber,
                validateInvoice: request.ValidateInvoice);

            if (request.ReturnFormat.Equals("base64", StringComparison.OrdinalIgnoreCase))
            {
                // Zwróć jako JSON z base64
                return Ok(new GeneratePdfResponse
                {
                    PdfBase64 = Convert.ToBase64String(pdfBytes!),
                    FileSizeBytes = pdfBytes!.Length,
                    InvoiceNumber = context.Faktura.Fa.P_2,
                    Mode = context.Metadata.Tryb.ToString()
                });
            }
            else
            {
                // Zwróć jako plik binarny
                var fileName = $"Faktura_{context.Faktura.Fa.P_2.Replace("/", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes!, "application/pdf", fileName);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Błąd walidacji podczas generowania PDF");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania PDF");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generuje PDF offline dla faktury z błędem (zapisuje obok pliku XML)
    /// </summary>
    /// <param name="request">Request z ścieżką do pliku XML</param>
    /// <returns>Ścieżka do wygenerowanego PDF</returns>
    [HttpPost("generate-pdf-offline")]
    [ProducesResponseType(typeof(GeneratePdfOfflineResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GeneratePdfOffline([FromBody] GeneratePdfOfflineRequest request)
    {
        try
        {
            // Sprawdź czy plik istnieje
            if (!System.IO.File.Exists(request.XmlFilePath))
            {
                _logger.LogWarning("Plik XML nie znaleziony: {FilePath}", request.XmlFilePath);
                return NotFound(new { error = "Plik XML nie znaleziony", path = request.XmlFilePath });
            }

            // Odczytaj XML
            var xmlContent = await System.IO.File.ReadAllTextAsync(request.XmlFilePath);

            // Parsuj fakturę aby uzyskać NIP-y
            var context = await _printerService.ParseInvoiceFromXmlAsync(xmlContent, numerKSeF: null);

            // === WALIDACJA NIP ===
            var allowedNips = _licenseValidator.GetAllowedNips();
            var nipSprzedawcy = context.Faktura.Podmiot1?.DaneIdentyfikacyjne?.NIP;
            var nipNabywcy = context.Faktura.Podmiot2?.DaneIdentyfikacyjne?.NIP;

            bool anyNipAllowed = false;
            if (!string.IsNullOrEmpty(nipSprzedawcy) && allowedNips.Contains(nipSprzedawcy))
                anyNipAllowed = true;
            if (!string.IsNullOrEmpty(nipNabywcy) && allowedNips.Contains(nipNabywcy))
                anyNipAllowed = true;

            if (!anyNipAllowed)
            {
                _logger.LogWarning("Brak uprawnień dla NIP-ów w fakturze: {NipSprzedawcy}, {NipNabywcy}",
                    nipSprzedawcy, nipNabywcy);
                return StatusCode(403, new
                {
                    error = "Licencja nie obejmuje NIP-ów z tej faktury",
                    sellerNip = nipSprzedawcy,
                    buyerNip = nipNabywcy
                });
            }

            // Tryb OFFLINE - bez certyfikatu, bez KSeF number
            var options = new PdfGenerationOptions
            {
                Certificate = null, // OFFLINE mode
                UseProduction = request.UseProduction,
                IncludeQrCode1 = false, // Brak QR code bez KSeF number
                IncludeQrCode2 = false
            };

            // Określ ścieżkę PDF
            var pdfPath = request.OutputPdfPath ??
                          Path.Combine(
                              Path.GetDirectoryName(request.XmlFilePath)!,
                              Path.GetFileNameWithoutExtension(request.XmlFilePath) + ".pdf");

            // Generuj PDF
            await _printerService.GeneratePdfFromXmlAsync(
                xmlContent: xmlContent,
                pdfOutputPath: pdfPath,
                options: options,
                numerKSeF: null, // OFFLINE - brak numeru KSeF
                validateInvoice: request.ValidateInvoice);

            var fileInfo = new FileInfo(pdfPath);

            _logger.LogInformation("PDF offline utworzony: {PdfPath} ({Size} bajtów)",
                pdfPath, fileInfo.Length);

            return Ok(new GeneratePdfOfflineResponse
            {
                Success = true,
                PdfPath = pdfPath,
                FileSizeBytes = fileInfo.Length,
                InvoiceNumber = context.Faktura.Fa.P_2,
                Mode = "Offline"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania PDF offline");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generuje kod QR dla numeru KSeF (do osadzania w zewnętrznych systemach)
    /// </summary>
    /// <param name="request">Request z numerem KSeF i opcjami</param>
    /// <returns>Kod QR w formacie SVG i/lub PNG base64</returns>
    [HttpPost("generate-qr-code")]
    [ProducesResponseType(typeof(GenerateQrCodeResponse), 200)]
    [ProducesResponseType(400)]
    public IActionResult GenerateQrCode([FromBody] GenerateQrCodeRequest request)
    {
        try
        {
            // Waliduj numer KSeF
            if (!KsefNumberValidator.IsValid(request.KSeFNumber))
            {
                return BadRequest(new { error = $"Nieprawidłowy format numeru KSeF: {request.KSeFNumber}" });
            }

            // Zbuduj URL weryfikacyjny
            var verificationUrl = _verificationLinkService.BuildKsefNumberVerificationUrl(
                request.KSeFNumber,
                request.UseProduction);

            _logger.LogInformation(
                "Generowanie kodu QR dla numeru KSeF: {KSeF}, Format: {Format}, Env: {Env}",
                request.KSeFNumber, request.Format, request.UseProduction ? "PROD" : "TEST");

            // Oblicz rozmiar w pikselach na moduł (minimalna wartość to 5)
            var pixelsPerModule = Math.Max(5, request.Size / 50); // 200px / 50 modułów ≈ 4-5 px/module

            var response = new GenerateQrCodeResponse
            {
                KSeFNumber = request.KSeFNumber,
                VerificationUrl = verificationUrl,
                Format = request.Format.ToLower(),
                Size = request.Size,
                Environment = request.UseProduction ? "production" : "test"
            };

            // Generuj w wybranym formacie
            switch (request.Format.ToLower())
            {
                case "svg":
                    response.QrCodeSvg = _qrCodeService.GenerateQrCodeSvg(verificationUrl, pixelsPerModule);
                    break;

                case "base64":
                    response.QrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(verificationUrl, pixelsPerModule);
                    break;

                case "both":
                    response.QrCodeSvg = _qrCodeService.GenerateQrCodeSvg(verificationUrl, pixelsPerModule);
                    response.QrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(verificationUrl, pixelsPerModule);
                    break;

                default:
                    return BadRequest(new { error = $"Nieprawidłowy format: {request.Format}. Dostępne: svg, base64, both" });
            }

            _logger.LogInformation("Kod QR wygenerowany pomyślnie dla numeru KSeF: {KSeF}", request.KSeFNumber);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania kodu QR dla numeru KSeF: {KSeF}", request.KSeFNumber);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Healthcheck endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    public IActionResult Health()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        var informationalVersion = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

        return Ok(new
        {
            status = "healthy",
            service = "KSeF Printer API",
            version = informationalVersion,
            assemblyVersion = version,
            timestamp = DateTime.UtcNow
        });
    }
}
