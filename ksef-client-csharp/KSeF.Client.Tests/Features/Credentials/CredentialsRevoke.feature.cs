using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;

[CollectionDefinition("RevokeCredentials.feature")]
[Trait("Category", "Features")]
[Trait("Features", "revoke_credentials.feature")]
public partial class CredentialsRevokeTests : KsefIntegrationTestBase
{
    [Fact]
    [Trait("Scenario", "Właściciel nadaje CredentialsManage dla delegata, delegat nadaje 'InvoiceWrite' dla PESEL i następnie odbiera.")]
    public async Task Delegate_GrantAndRevoke_InvoiceWrite_ForPesel_AsManager_LeavesNoActivePermission()
    {
        // Arrange
        string nipOwner = MiscellaneousUtils.GetRandomNip();
        string nipDelegate = MiscellaneousUtils.GetRandomNip();
        string pesel = MiscellaneousUtils.GetRandomPesel();

        string ownerToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nipOwner)).AccessToken.Token;
        string delegateToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nipDelegate)).AccessToken.Token;

        // Act
        // ========== Act: GRANT AS OWNER CredentialManage FOR DELEGATE ==========
        bool manageGranted = await CredentialsRevokeHelpers.GrantCredentialsManageToDelegateAsync(KsefClient, ownerToken, nipDelegate);
        Assert.True(manageGranted);

        // ========== Act: SEARCH CredentialManage FOR DELEGATE ==========
        IReadOnlyList<Core.Models.Permissions.PersonPermission> delegatePermissions = await CredentialsRevokeHelpers.SearchPersonPermissionsAsync(KsefClient, ownerToken, PermissionState.Active);
        Core.Models.Permissions.PersonPermission delegatePermission = Assert.Single(delegatePermissions);

        // ========== Act: GRANT AS DELEGATE InvoiceWrite FOR PESEL ==========
        bool invoiceWriteGranted = await CredentialsRevokeHelpers.GrantInvoiceWriteToPeselAsManagerAsync(KsefClient, delegateToken, nipOwner, pesel);
        Assert.True(invoiceWriteGranted);

        IReadOnlyList<Core.Models.Permissions.PersonPermission> peselPermissionsAfterGrant = await CredentialsRevokeHelpers.SearchPersonPermissionsAsync(KsefClient, delegateToken, PermissionState.Inactive);
        Core.Models.Permissions.PersonPermission grantedPermission = Assert.Single(peselPermissionsAfterGrant);

        // ========== Act: REVOKE AS DELEGATE InvoiceWrite FOR PESEL ==========
        bool revokeSuccessful = await CredentialsRevokeHelpers.RevokePersonPermissionAsync(KsefClient, delegateToken, grantedPermission.Id);
        Assert.True(revokeSuccessful);

        // Assert
        IReadOnlyList<Core.Models.Permissions.PersonPermission> activePermissionsAfterRevoke = await CredentialsRevokeHelpers.SearchPersonPermissionsAsync(KsefClient, delegateToken, PermissionState.Active);
        Assert.Empty(activePermissionsAfterRevoke);
    }
}
