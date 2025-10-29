using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;


namespace WebApplication.Controllers;

[Route("[controller]")]
[ApiController]
public class OnlineSessionController : ControllerBase
{
    private readonly ICryptographyService cryptographyService;
    private static EncryptionData? encryptionData;
    private readonly IKSeFClient ksefClient;

    public OnlineSessionController(IKSeFClient ksefClient, ICryptographyService cryptographyService)
    {
        this.cryptographyService = cryptographyService;
        this.ksefClient = ksefClient;
    }

    [HttpPost("open-session")]
    public async Task<ActionResult<OpenOnlineSessionResponse>> OpenOnlineSessionAsync(string accessToken, CancellationToken cancellationToken)
    {
        encryptionData = cryptographyService.GetEncryptionData();
        var request = OpenOnlineSessionRequestBuilder
         .Create()
         .WithFormCode(systemCode: "FA (2)", schemaVersion: "1-0E", value: "FA")
         .WithEncryption(
             encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
             initializationVector: encryptionData.EncryptionInfo.InitializationVector)
         .Build();

        var openSessionResponse = await ksefClient.OpenOnlineSessionAsync(request, accessToken, cancellationToken);
        return Ok(openSessionResponse);
    }

    [HttpPost("send-invoice")]
    public async Task<ActionResult<SendInvoiceResponse>> SendInvoiceOnlineSessionAsync(string sessionReferenceNumber, string accesToken, CancellationToken cancellationToken)
    {
        var invoice = System.IO.File.ReadAllBytes("faktura-online-fa(3).xml");

        var encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData!.CipherKey, encryptionData!.CipherIv);

        var invoiceMetadata = cryptographyService.GetMetaData(invoice);
        var encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        var sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .WithOfflineMode(false)
            .Build();

        var sendInvoiceResponse = await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accesToken, cancellationToken)
            .ConfigureAwait(false);
        return sendInvoiceResponse;
    }


    [HttpPost("send-technical-correction")]
    public async Task<ActionResult<SendInvoiceResponse>> SendTechnicalCorrectionAsync(string sessionReferenceNumber, string hashOfCorrectedInvoice, string accesToken, CancellationToken cancellationToken)
    {
        var invoice = System.IO.File.ReadAllBytes("faktura-online-fa(3).xml");
        var encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData!.CipherKey, encryptionData!.CipherIv);

        var invoiceMetadata = cryptographyService.GetMetaData(invoice);
        var encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        var sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .WithOfflineMode(true)
            .WithHashOfCorrectedInvoice(hashOfCorrectedInvoice)
            .Build();

        var sendInvoiceResponse = await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accesToken, cancellationToken)
            .ConfigureAwait(false);

        return sendInvoiceResponse;
    }

    [HttpPost("close-session")]
    public async Task CloseOnlineSessionAsync(string sessionReferenceNumber, string accesToken, CancellationToken cancellationToken)
    {
        await ksefClient.CloseOnlineSessionAsync(sessionReferenceNumber, accesToken, cancellationToken)
            .ConfigureAwait(false);
    }
}
