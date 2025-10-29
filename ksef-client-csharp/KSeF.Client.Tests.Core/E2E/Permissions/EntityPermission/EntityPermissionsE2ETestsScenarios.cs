using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission;


public class EntityPermissionsE2ETestsScenarios : TestBase
{
    /// <summary>
    /// Potwierdza że grantor widzi uprawnienia które nadał innemu podmiotowi.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPermissionsGrantedByGrantor()
    {
        // Arrange: NIP-y i Subjecty
        string contextNip = MiscellaneousUtils.GetRandomNip();
        string subjectNip = MiscellaneousUtils.GetRandomNip();

        Client.Core.Models.Permissions.Entity.SubjectIdentifier BR_subject =
            new Client.Core.Models.Permissions.Entity.SubjectIdentifier
            {
                Type = Client.Core.Models.Permissions.Entity.SubjectIdentifierType.Nip,
                Value = subjectNip
            };

        // Auth
        Client.Core.Models.Authorization.AuthOperationStatusResponse authorizationInfo =
            await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, contextNip);

        Client.Core.Models.Permissions.Entity.GrantPermissionsEntityRequest grantsPermissionsRequest =
            GrantEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(BR_subject)
                .WithPermissions(
                    Client.Core.Models.Permissions.Entity.Permission.New(
                        Client.Core.Models.Permissions.Entity.StandardPermissionType.InvoiceRead, true),
                    Client.Core.Models.Permissions.Entity.Permission.New(
                        Client.Core.Models.Permissions.Entity.StandardPermissionType.InvoiceWrite, false)
                )
                .WithDescription("Read and Write permissions")
                .Build();

        OperationResponse grantsPermissionsResponse =
            await KsefClient.GrantsPermissionEntityAsync(
                grantsPermissionsRequest,
                authorizationInfo.AccessToken.Token,
                CancellationToken);

        Assert.NotNull(grantsPermissionsResponse);

        Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest queryForAllPermissions =
            new Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest
            {
                QueryType = Client.Core.Models.Permissions.Person.QueryTypeEnum.PermissionsGrantedInCurrentContext
            };

        Client.Core.Models.Permissions.PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>
            queryForAllPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonPermissionsAsync(
                    queryForAllPermissions,
                    authorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == 2,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForAllPermissionsResponse);
        Assert.NotEmpty(queryForAllPermissionsResponse.Permissions);
        Assert.Equal(2, queryForAllPermissionsResponse.Permissions.Count);
    }

    /// <summary>
    /// Potwierdza że podmiot któremu nadano uprawnienia widzi je w swoim kontekście.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPersonalPermissions()
    {
        // Arrange + Grants
        string contextNip = MiscellaneousUtils.GetRandomNip();
        string subjectNip = MiscellaneousUtils.GetRandomNip();

        Client.Core.Models.Permissions.Entity.SubjectIdentifier subject =
            new Client.Core.Models.Permissions.Entity.SubjectIdentifier
            {
                Type = Client.Core.Models.Permissions.Entity.SubjectIdentifierType.Nip,
                Value = subjectNip
            };

        AuthOperationStatusResponse authorizationInfo = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, contextNip);

        GrantPermissionsEntityRequest grantPermissionsEntityRequest = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                Client.Core.Models.Permissions.Entity.Permission.New(
                    Client.Core.Models.Permissions.Entity.StandardPermissionType.InvoiceRead, true),
                Client.Core.Models.Permissions.Entity.Permission.New(
                    Client.Core.Models.Permissions.Entity.StandardPermissionType.InvoiceWrite, false)
            )
            .WithDescription("Grant read and write permissions")
            .Build();

        OperationResponse grantPermissionsEntityResponse = await KsefClient.GrantsPermissionEntityAsync(
            grantPermissionsEntityRequest, authorizationInfo.AccessToken.Token, CancellationToken);
        Assert.NotNull(grantPermissionsEntityResponse);

        // Auth: Entity we własnym kontekście
        AuthOperationStatusResponse entityAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, subjectNip);

        PersonalPermissionsQueryRequest queryForAllPermissions = new PersonalPermissionsQueryRequest();
        PagedPermissionsResponse<PersonalPermission> queryForAllPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryForAllPermissions, entityAuthorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == 2,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForAllPermissionsResponse);
        Assert.NotEmpty(queryForAllPermissionsResponse.Permissions);
        Assert.Equal(2, queryForAllPermissionsResponse.Permissions.Count);

        List<PersonalPermission> permissionsGrantedByEntity = queryForAllPermissionsResponse.Permissions.Where(p => p.ContextIdentifier.Value == contextNip).ToList();
        Assert.Equal(2, permissionsGrantedByEntity.Count);
    }

    /// <summary>
    /// Potwierdza że podmiot któremu nadano uprawnienia widzi je w swoim kontekście, 
    /// a nie widzi uprawnień nadanych przez inny podmiot.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPermissionsSearchedBySubjectInEntityContext()
    {
        // Arrange
        string jdgNip = MiscellaneousUtils.GetRandomNip(); // jdg
        string otherJdgNip = MiscellaneousUtils.GetRandomNip(); // inna jdg
        string brNip = MiscellaneousUtils.GetRandomNip(); // biuro rachunkowe
        string kdpNip = MiscellaneousUtils.GetRandomNip(); // kancelaria doradztwa podatkowego

        Client.Core.Models.Permissions.Entity.SubjectIdentifier brSubject =
            new Client.Core.Models.Permissions.Entity.SubjectIdentifier
            {
                Type = Client.Core.Models.Permissions.Entity.SubjectIdentifierType.Nip,
                Value = brNip
            };

        Client.Core.Models.Permissions.Entity.SubjectIdentifier kdpSubject =
            new Client.Core.Models.Permissions.Entity.SubjectIdentifier
            {
                Type = Client.Core.Models.Permissions.Entity.SubjectIdentifierType.Nip,
                Value = kdpNip
            };

        Permission[] permissions = new Permission[]
        {
            Client.Core.Models.Permissions.Entity.Permission.New(
                Client.Core.Models.Permissions.Entity.StandardPermissionType.InvoiceRead, true),
            Client.Core.Models.Permissions.Entity.Permission.New(
                Client.Core.Models.Permissions.Entity.StandardPermissionType.InvoiceWrite, false)
        };

        // Act
        // uwierzytelnienie jdg we własnym kontekście
        AuthOperationStatusResponse authorizationInfo = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, jdgNip);
        // nadanie uprawnień biuru rachunkowemu
        OperationResponse brGrantInJdg = await GrantPermissionsAsync(brSubject, authorizationInfo, permissions);
        Assert.NotNull(brGrantInJdg);
        // nadanie uprawnień kancelarii doradztwa podatkowego
        OperationResponse kdpGrantInJdg = await GrantPermissionsAsync(kdpSubject, authorizationInfo, permissions);
        Assert.NotNull(kdpGrantInJdg);

        // uwierzytelnienie otherJdg we własnym kontekście
        AuthOperationStatusResponse otherJdgAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, otherJdgNip);
        // nadanie uprawnień biuru rachunkowemu
        OperationResponse brGrantInOtherJdg = await GrantPermissionsAsync(brSubject, otherJdgAuthorizationInfo, permissions);
        Assert.NotNull(brGrantInOtherJdg);
        // nadanie uprawnień kancelarii doradztwa podatkowego
        OperationResponse kdpGrantInOtherJdg = await GrantPermissionsAsync(kdpSubject, otherJdgAuthorizationInfo, permissions);
        Assert.NotNull(kdpGrantInOtherJdg);

        // w tym momencie:
        // biuro rachunkowe ma uprawnienia w kontekście jdg i otherJdg (razem 4 uprawnienia)
        // kancelaria doradztwa podatkowego ma uprawnienia w kontekście jdg i otherJdg (razem 4 uprawnienia)

        // Assert
        // uwierzytelnienie: biuro rachunkowe w kontekście jdg
        X509Certificate2 personalCertificate = CertificateUtils.GetPersonalCertificate(
            givenName: "Jan",
            surname: "Kowalski",
            serialNumberPrefix: "TINPL",
            serialNumber: brNip,
            commonName: "Jan Kowalski Certificate");

        AuthOperationStatusResponse entityAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(
            KsefClient,
            SignatureService,
            jdgNip,
            Client.Core.Models.Authorization.ContextIdentifierType.Nip,
            personalCertificate);

        Client.Core.Models.Permissions.Person.PersonalPermissionsQueryRequest queryForContextPermissions =
            new Client.Core.Models.Permissions.Person.PersonalPermissionsQueryRequest();

        // uprawnienia biura rachunkowego w kontekście jdg
        Client.Core.Models.Permissions.PagedPermissionsResponse<Client.Core.Models.Permissions.PersonalPermission>
            queryForContextPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryForContextPermissions,
                    entityAuthorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == 2,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForContextPermissionsResponse);
        Assert.Equal(2, queryForContextPermissionsResponse.Permissions.Count); // biuro rachunkowe ma 2 uprawnienia w kontekście jdg
    }

    private async Task<OperationResponse> GrantPermissionsAsync(
            Client.Core.Models.Permissions.Entity.SubjectIdentifier subject,
            AuthOperationStatusResponse authorizationInfo,
            Permission[] permissions)
    {
        GrantPermissionsEntityRequest grantEntityPermissionsRequest = GrantEntityPermissionsRequestBuilder
                    .Create()
                    .WithSubject(subject)
                    .WithPermissions(permissions)
                    .WithDescription("Uprawnienia do odczytu i wystawiania faktur")
                    .Build();

        OperationResponse grantEntityPermissionsResponse = await KsefClient.GrantsPermissionEntityAsync(
            grantEntityPermissionsRequest, authorizationInfo.AccessToken.Token, CancellationToken);

        return grantEntityPermissionsResponse;
    }
}