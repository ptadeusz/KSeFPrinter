using KSeF.Client.Core.Models.Invoices;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;

namespace WebApplication.Controllers;
[Route("[controller]")]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly IKSeFClient ksefClient;

    public InvoicesController(IKSeFClient ksefClient)
    {
        this.ksefClient = ksefClient;
    }

    /// <summary>
    /// Pobranie faktury po numerze referencyjnym.
    /// </summary>
    [HttpGet("single")]
    public async Task<ActionResult<string>> GetInvoiceAsync([FromQuery] string ksefReferenceNumber, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        return await ksefClient.GetInvoiceAsync(ksefReferenceNumber, accessToken, cancellationToken);
    }

    /// <summary>
    /// Zapytanie o metadane faktur według filtrów.
    /// </summary>
    [HttpPost("query")]
    public async Task<ActionResult<PagedInvoiceResponse>> QueryInvoicesAsync(
        [FromBody] InvoiceQueryFilters body,
        [FromQuery] string accessToken,
        [FromQuery] int? pageOffset,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return await ksefClient.QueryInvoiceMetadataAsync(body, accessToken, pageOffset, pageSize, cancellationToken);
    }

    /// <summary>
    /// Eksport faktur zgodnie z podanymi filtrami.
    /// </summary>
    [HttpPost("exports")]
    [ProducesResponseType(typeof(ExportInvoicesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExportInvoicesResponse>> ExportInvoices(
        [FromBody] InvoiceExportRequest request,
        [FromHeader(Name = "Authorization")] string accessToken,
        [FromQuery] int? pageOffset,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await ksefClient.ExportInvoicesAsync(request, accessToken, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera status operacji eksportu faktur.
    /// </summary>
    [HttpGet("exports/{operationReferenceNumber}")]
    [ProducesResponseType(typeof(InvoiceExportStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceExportStatusResponse>> GetInvoiceExportStatus(
        string operationReferenceNumber,
        [FromHeader(Name = "Authorization")] string accessToken,
        CancellationToken cancellationToken)
    {
        var result = await ksefClient.GetInvoiceExportStatusAsync(operationReferenceNumber, accessToken, cancellationToken);
        return Ok(result);
    }
}