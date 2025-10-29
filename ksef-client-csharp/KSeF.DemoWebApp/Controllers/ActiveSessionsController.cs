using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;

namespace WebApplication.Controllers;
[Route("active-sessions")]
[ApiController]
public class ActiveSessionsController : ControllerBase
{
    private readonly IKSeFClient ksefClient;

    public ActiveSessionsController(IKSeFClient ksefClient)
    {
        this.ksefClient = ksefClient;
    }

    /// <summary>
    /// Pobranie listy aktywnych sesji.
    /// </summary>
    [HttpGet("list")]
    public async Task<ActionResult<ICollection<AuthenticationListItem>>> GetSessionsAsync([FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        const int pageSize = 20;
        string? continuationToken = null;
        var activeSessions = new List<AuthenticationListItem>();
        do
        {
            var response = await ksefClient.GetActiveSessions(accessToken, pageSize, continuationToken, cancellationToken);
            continuationToken = response.ContinuationToken;
            activeSessions.AddRange(response.Items);
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        return Ok(activeSessions);
    }

    /// <summary>
    /// Unieważnia sesję powiązaną z tokenem użytym do wywołania tej operacji.
    /// </summary>
    /// <param name="token">Acces token lub Refresh token.</param>
    /// <param name="cancellationToken"></param>
    [HttpDelete("revoke-current-session")]
    public async Task<ActionResult> RevokeCurrentSessionAsync([FromQuery] string token, CancellationToken cancellationToken)
    {
        await ksefClient.RevokeCurrentSessionAsync(token, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Unieważnia sesję o podanym numerze referencyjnym.
    /// </summary>
    [HttpDelete("revoke-session")]
    public async Task<ActionResult> RevokeSessionAsync([FromQuery] string sessionReferenceNumber, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        await ksefClient.RevokeSessionAsync(sessionReferenceNumber, accessToken, cancellationToken);
        return NoContent();
    }
}