using Microsoft.AspNetCore.Mvc;
using KSeFPrinter;
using KSeFPrinter.API.Models;
using KSeFPrinter.API.Services;
using KSeFPrinter.Models.Common;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeFPrinter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InvoiceController : ControllerBase
{
    private readonly InvoicePrinterService _printerService;
    private readonly CertificateService _certificateService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        InvoicePrinterService printerService,
        CertificateService certificateService,
        ILogger<InvoiceController> logger)
    {
        _printerService = printerService;
        _certificateService = certificateService;
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
    public async Task<IActionResult> GeneratePdf([FromBody] GeneratePdfRequest request)
    {
        try
        {
            var xmlContent = request.IsBase64
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.XmlContent))
                : request.XmlContent;

            // Wczytaj certyfikat jeśli podany
            X509Certificate2? certificate = null;

            if (request.CertificateSource == "AzureKeyVault" &&
                !string.IsNullOrWhiteSpace(request.AzureKeyVaultUrl) &&
                !string.IsNullOrWhiteSpace(request.AzureKeyVaultCertificateName))
            {
                // Wczytaj z Azure Key Vault
                var keyVaultOptions = new KSeFPrinter.Models.Common.AzureKeyVaultOptions
                {
                    KeyVaultUrl = request.AzureKeyVaultUrl,
                    CertificateName = request.AzureKeyVaultCertificateName,
                    CertificateVersion = request.AzureKeyVaultCertificateVersion,
                    AuthenticationType = Enum.TryParse<KSeFPrinter.Models.Common.AzureAuthenticationType>(
                        request.AzureAuthenticationType, out var authType)
                        ? authType
                        : KSeFPrinter.Models.Common.AzureAuthenticationType.DefaultAzureCredential,
                    TenantId = request.AzureTenantId,
                    ClientId = request.AzureClientId,
                    ClientSecret = request.AzureClientSecret
                };

                certificate = await _certificateService.LoadFromKeyVaultAsync(keyVaultOptions);
            }
            else if (!string.IsNullOrWhiteSpace(request.CertificateThumbprint) ||
                     !string.IsNullOrWhiteSpace(request.CertificateSubject))
            {
                // Wczytaj z Windows Certificate Store
                certificate = _certificateService.LoadFromStore(
                    request.CertificateThumbprint,
                    request.CertificateSubject,
                    request.CertificateStoreName,
                    request.CertificateStoreLocation);
            }

            // Opcje generowania PDF
            var options = new PdfGenerationOptions
            {
                Certificate = certificate,
                UseProduction = request.UseProduction,
                IncludeQrCode1 = true,
                IncludeQrCode2 = certificate != null
            };

            // Generuj PDF jako bajty
            var pdfBytes = await _printerService.GeneratePdfFromXmlAsync(
                xmlContent: xmlContent,
                pdfOutputPath: null, // zwróć bajty
                options: options,
                numerKSeF: request.KSeFNumber,
                validateInvoice: request.ValidateInvoice);

            // Pobierz podstawowe dane dla response
            var context = await _printerService.ParseInvoiceFromXmlAsync(xmlContent, request.KSeFNumber);

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
    /// Healthcheck endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "KSeF Printer API",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        });
    }
}
