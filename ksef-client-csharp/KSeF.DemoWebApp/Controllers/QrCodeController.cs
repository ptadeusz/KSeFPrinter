using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.QRCode;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class QrCodeController(
    IVerificationLinkService linkSvc,
    IQrCodeService qrSvc) : ControllerBase
{
    private readonly IVerificationLinkService linkSvc = linkSvc ?? throw new ArgumentNullException(nameof(linkSvc));
    private readonly IQrCodeService qrSvc = qrSvc ?? throw new ArgumentNullException(nameof(qrSvc));

    // 1. Faktura z numerem KSeF (online)
    [HttpGet("invoice/ksef")]
    public ActionResult<QrCodeResult> GetInvoiceQrWithKsef(
        string nip,
        DateTime issueDate,
        string invoiceHash,
        string ksefNumber)
    {
        var url = linkSvc.BuildInvoiceVerificationUrl(nip, issueDate, invoiceHash);
        var qrCode = qrSvc.GenerateQrCode(url);
        var labeledQr = qrSvc.AddLabelToQrCode(qrCode, ksefNumber);

        return Ok(new QrCodeResult(url, Convert.ToBase64String(labeledQr)));
    }

    // 2. Faktura offline (przed wysyłką)
    [HttpGet("invoice/offline")]
    public ActionResult<QrCodeResult> GetInvoiceQrOffline(
        string nip,
        DateTime issueDate,
        string invoiceHash)
    {
        var url = linkSvc.BuildInvoiceVerificationUrl(nip, issueDate, invoiceHash);
        var qrCode = qrSvc.GenerateQrCode(url);
        var labeledQr = qrSvc.AddLabelToQrCode(qrCode, "OFFLINE");

        return Ok(new QrCodeResult(url, Convert.ToBase64String(labeledQr)));
    }

    // 3. Weryfikacja certyfikatu (Kod II)
    [HttpGet("certificate")]
    public ActionResult<QrCodeResult> GetCertificateQr(
        string sellerNip,
        ContextIdentifierType contextIdentifierType,
        string contextIdentifierValue,
        string certSerial,
        string invoiceHash,
        string certbase64,
        string privateKey = ""        
       )
    {
        var cert = new X509Certificate2(Convert.FromBase64String(certbase64));
        var url = linkSvc.BuildCertificateVerificationUrl(sellerNip,contextIdentifierType ,contextIdentifierValue ,certSerial, invoiceHash, cert, privateKey);
        var qrCode = qrSvc.GenerateQrCode(url);
        var labeledQr = qrSvc.AddLabelToQrCode(qrCode, "CERTYFIKAT");

        return Ok(new QrCodeResult(url, Convert.ToBase64String(labeledQr)));
    }
}
