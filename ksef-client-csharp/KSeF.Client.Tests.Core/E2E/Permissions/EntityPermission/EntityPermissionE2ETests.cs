using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission;

/// <summary>
/// Testy E2E nadawania i cofania uprawnień dla podmiotów:
/// - nadanie uprawnień
/// - wyszukanie nadanych uprawnień
/// - cofnięcie uprawnień
/// - ponowne wyszukanie (weryfikacja, że zostały cofnięte)
/// </summary>
[Collection("EntityPermissionScenario")]
public partial class EntityPermissionE2ETests : TestBase
{
    private const string PermissionDescription = "E2E test grant";
    private const int OperationSuccessfulStatusCode = 200;

    // Zamiast fixture: prywatne readonly pola
    private string accessToken = string.Empty;
    private KSeF.Client.Core.Models.Permissions.Entity.SubjectIdentifier Entity { get; } = new();

    public EntityPermissionE2ETests()
    {
        Client.Core.Models.Authorization.AuthOperationStatusResponse authOperationStatusResponse = AuthenticationUtils
            .AuthenticateAsync(KsefClient, SignatureService)
            .GetAwaiter().GetResult();

        accessToken = authOperationStatusResponse.AccessToken.Token;
        Entity.Value = MiscellaneousUtils.GetRandomNip();
        Entity.Type = SubjectIdentifierType.Nip;
    }

    [Fact]
    public async Task EntityPermissions_FullFlow_GrantSearchRevokeSearch()
    {
        // 1) Nadaj uprawnienia dla podmiotu
        OperationResponse grantResponse = await GrantEntityPermissionsAsync(Entity, PermissionDescription, accessToken);
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.OperationReferenceNumber));

        // 2) Wyszukaj nadane uprawnienia — poll aż będą widoczne i kompletne
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterGrant =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                result =>
                    result is not null
                    && result.Permissions is { Count: > 0 }
                    && result.Permissions.Any(x =>
                        x.Description == PermissionDescription &&
                        x.CanDelegate == true &&
                        Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceRead)
                    && result.Permissions.Any(x =>
                        x.Description == PermissionDescription &&
                        x.CanDelegate == false &&
                        Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceWrite),
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);
        Assert.True(searchAfterGrant.Permissions.All(x => x.Description == PermissionDescription));
        Assert.True(searchAfterGrant.Permissions.First(x => x.CanDelegate == true && Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceRead) is not null);
        Assert.True(searchAfterGrant.Permissions.First(x => x.CanDelegate == false && Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceWrite) is not null);

        // 3) Cofnij nadane uprawnienia (ze sprawdzeniem statusów przez polling)
        List<PermissionsOperationStatusResponse> revokeResult = await RevokePermissionsAsync(searchAfterGrant.Permissions, accessToken);
        Assert.NotNull(revokeResult);
        Assert.NotEmpty(revokeResult);
        Assert.Equal(searchAfterGrant.Permissions.Count, revokeResult.Count);
        Assert.All(revokeResult, r =>
            Assert.True(r.Status.Code == OperationSuccessfulStatusCode,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );

        // 4) Wyszukaj ponownie — poll aż lista będzie pusta
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                result => result is not null && result.Permissions is { Count: 0 },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(searchAfterRevoke);
        Assert.Empty(searchAfterRevoke.Permissions);
    }

    /// <summary>
    /// Nadaje uprawnienia dla podmiotu i zwraca numer referencyjny operacji.
    /// </summary>
    private async Task<OperationResponse> GrantEntityPermissionsAsync(
        KSeF.Client.Core.Models.Permissions.Entity.SubjectIdentifier subject,
        string description,
        string accessToken)
    {
        GrantPermissionsEntityRequest request = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                Permission.New(StandardPermissionType.InvoiceRead, true),
                Permission.New(StandardPermissionType.InvoiceWrite, false)
            )
            .WithDescription(description)
            .Build();

        OperationResponse response = await KsefClient.GrantsPermissionEntityAsync(request, accessToken, CancellationToken);
        return response;
    }

    /// <summary>
    /// Wyszukuje nadane uprawnienia dla osób i zwraca wynik wyszukiwania.
    /// </summary>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>>
        SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest query = new Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest();
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> response = await KsefClient.SearchGrantedPersonPermissionsAsync(query, accessToken);
        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    private async Task<List<Client.Core.Models.Permissions.PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<Client.Core.Models.Permissions.PersonPermission> grantedPermissions,
        string accessToken)
    {
        List<Client.Core.Models.Permissions.OperationResponse> revokeResponses = new List<Client.Core.Models.Permissions.OperationResponse>();

        // Uruchomienie operacji cofania
        foreach (Client.Core.Models.Permissions.PersonPermission permission in grantedPermissions)
        {
            Client.Core.Models.Permissions.OperationResponse response = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(response);
        }

        // Sprawdzenie statusów wszystkich operacji — polling do 200
        List<Client.Core.Models.Permissions.PermissionsOperationStatusResponse> statuses = new List<Client.Core.Models.Permissions.PermissionsOperationStatusResponse>();

        foreach (Client.Core.Models.Permissions.OperationResponse revokeResponse in revokeResponses)
        {
            Client.Core.Models.Permissions.PermissionsOperationStatusResponse status =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeResponse.OperationReferenceNumber, accessToken),
                    result => result.Status.Code == OperationSuccessfulStatusCode,
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 30,
                    cancellationToken: CancellationToken);

            statuses.Add(status);
        }

        return statuses;
    }
}