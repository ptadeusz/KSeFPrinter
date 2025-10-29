using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.AuthorizationPermission;

public class AuthorizationPermissionsScenarioE2EFixture
{
    public SubjectIdentifier SubjectIdentifier { get; } =
        new Client.Core.Models.Permissions.Authorizations.SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = MiscellaneousUtils.GetRandomNip()
        };

    public OperationResponse GrantResponse { get; set; }
    public List<PermissionsOperationStatusResponse> RevokeStatusResults { get; set; } = new();
    public required PagedAuthorizationsResponse<AuthorizationGrant> SearchResponse { get; set; }
}
