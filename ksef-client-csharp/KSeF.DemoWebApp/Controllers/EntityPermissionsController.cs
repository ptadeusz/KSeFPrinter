using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class EntityPermissionsController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-permissions-for-Entity")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, Client.Core.Models.Permissions.Entity.SubjectIdentifier subjectIdentifier, CancellationToken cancellationToken)
    {
        var request = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subjectIdentifier)
            .WithPermissions(
                Permission.New(StandardPermissionType.InvoiceRead, true),
                Permission.New(StandardPermissionType.InvoiceRead, false)
                )
            .WithDescription("Access for quarterly review")
            .Build();

        return await ksefClient.GrantsPermissionEntityAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-permissions-for-Entity")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(
    string accessToken,
    string permissionId,
    CancellationToken cancellationToken)
    {
 
        return await ksefClient.RevokeCommonPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
