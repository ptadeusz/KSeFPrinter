using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Peppol;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class PeppolController : ControllerBase
{
    private readonly IKSeFClient _client;

    public PeppolController(IKSeFClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Pobranie listy dostawców usług Peppol.
    /// </summary>
    [HttpGet("query")]
    [ProducesResponseType(typeof(QueryPeppolProvidersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryPeppolProvidersResponse>> QueryProviders(
        [FromHeader(Name = "Authorization")] string accessToken,
        [FromQuery] int? pageOffset,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await _client.QueryPeppolProvidersAsync(accessToken, pageOffset, pageSize, cancellationToken);
        return Ok(result);
    }
}
