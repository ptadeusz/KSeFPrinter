using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Interfaces.Clients;

namespace WebApplication.Controllers;

[Route("[controller]")]
[ApiController]
public class KsefTokenController : ControllerBase
{
    private readonly IKSeFClient ksefClient;
    public KsefTokenController(IKSeFClient ksefClient)
    {
        this.ksefClient = ksefClient;
    }

    [HttpGet("get-new-token")]
    public async Task<ActionResult<KsefTokenResponse>> GetNewTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        var tokenRequest = new KsefTokenRequest
        {
            Permissions = [
                KsefTokenPermissionType.InvoiceRead,
                KsefTokenPermissionType.InvoiceWrite
                ],
            Description = "Demo token",
        };
        KsefTokenResponse ksefToken = await ksefClient.GenerateKsefTokenAsync(tokenRequest, accessToken, cancellationToken);
        return Ok(ksefToken);
    }

    [HttpGet("query-tokens")]
    public async Task<ActionResult<AuthenticationKsefToken>> QueryTokensAsync(string accessToken, CancellationToken cancellationToken)
    {
        var result = new List<AuthenticationKsefToken>();
        const int pageSize = 20;
        var status = AuthenticationKsefTokenStatus.Active;
        var continuationToken = string.Empty;

        do
        {
            var tokens = await ksefClient.QueryKsefTokensAsync(accessToken, [status], continuationToken, pageSize : pageSize, cancellationToken: cancellationToken);
            result.AddRange(tokens.Tokens);
            continuationToken = tokens.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return Ok(result);
    }
    [HttpGet("get-token")]
    public async Task<ActionResult<AuthenticationKsefToken>> GetTokenAsync(string tokenRefrenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        AuthenticationKsefToken ksefToken = await ksefClient.GetKsefTokenAsync(tokenRefrenceNumber, accessToken, cancellationToken);
        return Ok(ksefToken);
    }

    [HttpDelete]
    public async Task<ActionResult> RevokeAsync(string tokenRefrenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        await ksefClient.RevokeKsefTokenAsync(tokenRefrenceNumber, accessToken, cancellationToken);
        return NoContent();
    }
}
