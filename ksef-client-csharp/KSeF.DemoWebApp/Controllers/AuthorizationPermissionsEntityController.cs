using KSeF.Client.Api.Builders.AuthorizationPermissions;
using KSeF.Client.Core.Models.Permissions;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Permissions.Authorizations;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthorizationPermissionsEntityController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-authorization-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, SubjectIdentifier subjectIdentifier, CancellationToken cancellationToken)
    {
        var request = GrantAuthorizationPermissionsRequestBuilder
            .Create()
            .WithSubject(subjectIdentifier)
            .WithPermission(AuthorizationPermissionType.TaxRepresentative)
            .WithDescription("Access for quarterly review")
            .Build();

        return await ksefClient.GrantsAuthorizationPermissionAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-authorization-permissions-for-entity")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(
    string accessToken,
    string permissionId,
    CancellationToken cancellationToken)
    {

        return await ksefClient.RevokeAuthorizationsPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
