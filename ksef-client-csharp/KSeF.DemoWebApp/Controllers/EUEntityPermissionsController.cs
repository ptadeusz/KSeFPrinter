using KSeF.Client.Api.Builders.EUEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class EUEntityPermissionsController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-eu-entity-permissions")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, GrantPermissionsRequest grantPermissionsRequest, CancellationToken cancellationToken)
    {
        var request = GrantEUEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(grantPermissionsRequest.SubjectIdentifier)
            .WithSubjectName("Sample Subject Name")
            .WithContext(grantPermissionsRequest.ContextIdentifier)
            .WithDescription("Access for quarterly review")
            .Build();

        return await ksefClient.GrantsPermissionEUEntityAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-eu-entity-permissions")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(string permissionId, string accessToken, CancellationToken cancellationToken)
    {
      
        return await ksefClient.RevokeCommonPermissionAsync(permissionId, accessToken, cancellationToken);
    }

}
