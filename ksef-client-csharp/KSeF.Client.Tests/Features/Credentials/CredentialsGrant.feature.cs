using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features
{
    [Collection("CredentialsGrantScenario")]
    [Trait("Category", "Features")]
    [Trait("Features", "credentials_grant.feature")]
    public class CredentialsGrantTests : KsefIntegrationTestBase
    {
        [Theory]
        [InlineData("90091309123", new[] { StandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { StandardPermissionType.InvoiceRead, StandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { StandardPermissionType.CredentialsManage })]
        [InlineData("90091309123", new[] { StandardPermissionType.CredentialsRead })]
        [InlineData("90091309123", new[] { StandardPermissionType.Introspection })]
        [InlineData("90091309123", new[] { StandardPermissionType.SubunitManage })]

        [InlineData("6651887777", new[] { StandardPermissionType.InvoiceWrite })]
        [InlineData("6651887777", new[] { StandardPermissionType.InvoiceRead })]
        [InlineData("6651887777", new[] { StandardPermissionType.CredentialsManage })]
        [InlineData("6651887777", new[] { StandardPermissionType.CredentialsRead })]
        [InlineData("6651887777", new[] { StandardPermissionType.Introspection })]
        [InlineData("6651887777", new[] { StandardPermissionType.InvoiceWrite, StandardPermissionType.InvoiceRead })]
        [Trait("Scenario", "Nadanie uprawnienia wystawianie faktur")]
        public async Task GivenOwnerIsAuthenticated_WhenGrantInvoiceIssuingPermissionToEntity_ThenPermissionIsConfirmed(string identyficator, StandardPermissionType[] permissions)
        {
            var ownerNIP = MiscellaneousUtils.GetRandomNip();

            await TestGrantPermissions(identyficator, permissions, ownerNIP);
        }

        [Theory]
        [InlineData("90091309123", new[] { StandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { StandardPermissionType.InvoiceRead, StandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { StandardPermissionType.CredentialsManage })]
        [InlineData("90091309123", new[] { StandardPermissionType.CredentialsRead })]
        [InlineData("90091309123", new[] { StandardPermissionType.Introspection })]
        [InlineData("90091309123", new[] { StandardPermissionType.SubunitManage })]

        [InlineData("6651887777", new[] { StandardPermissionType.InvoiceWrite })]
        [InlineData("6651887777", new[] { StandardPermissionType.InvoiceRead })]
        [InlineData("6651887777", new[] { StandardPermissionType.CredentialsManage })]
        [InlineData("6651887777", new[] { StandardPermissionType.CredentialsRead })]
        [InlineData("6651887777", new[] { StandardPermissionType.Introspection })]
        [InlineData("6651887777", new[] { StandardPermissionType.InvoiceWrite, StandardPermissionType.InvoiceRead })]
        [Trait("Scenario", "Nadanie uprawnień przez osobę z uprawnieniem do zarządzania uprawnieniami")]
        public async Task GivenDelegatedByOwnerIsAuthenticated_WhenGrantInvoiceIssuingPermissionToEntity_ThenPermissionIsConfirmed(string identyficator, StandardPermissionType[] permissions)
        {
            var ownerNIP = MiscellaneousUtils.GetRandomNip();
            var authToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, ownerNIP)).AccessToken.Token;

            var nipWhichWillDelegatePermissions = MiscellaneousUtils.GetRandomNip();

            var subjectIdentifier = new SubjectIdentifier { Type = SubjectIdentifierType.Nip, Value = nipWhichWillDelegatePermissions };

            var managePermission = new[] { StandardPermissionType.CredentialsManage };

            var operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient, authToken, subjectIdentifier, permissions);

            await Task.Delay(1000);
            //tests
            await TestGrantPermissions(identyficator, permissions, nipWhichWillDelegatePermissions);

            //revoke permissions to delegate
            var grantedPermissions = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PermissionState.Active);
            Assert.True(grantedPermissions.Any());

            foreach (var item in grantedPermissions)
            {
                var revokeSuccessful = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, authToken, item.Id);
                Assert.NotNull(revokeSuccessful);
                await Task.Delay(3000);
            }

            var activePermissionsAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PermissionState.Active);
            Assert.Empty(activePermissionsAfterRevoke);
        }

        private async Task TestGrantPermissions(string identyficator, StandardPermissionType[] permissions, string nip)
        {
            var authToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nip)).AccessToken.Token;

            bool isNIP = identyficator.Length == 10;

            var subjectIdentifier = new SubjectIdentifier { Type = isNIP ? SubjectIdentifierType.Nip : SubjectIdentifierType.Pesel, Value = identyficator };
            var grantPermissionsResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient,
                    authToken,
                    subjectIdentifier,
                    permissions, "CredentialsGrantTests");

            var grantPermissionsActionStatus = await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, grantPermissionsResponse.OperationReferenceNumber, authToken);

            await Task.Delay(3000);
            var grantedPermissions = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PermissionState.Active);
            Assert.True(grantedPermissions.Count == permissions.Length);

            foreach (var item in grantedPermissions)
            {
                var revokeSuccessful = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, authToken, item.Id);
                Assert.NotNull(revokeSuccessful);
                await Task.Delay(3000);
            }

            var activePermissionsAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PermissionState.Active);
            Assert.Empty(activePermissionsAfterRevoke);
        }
    }
}