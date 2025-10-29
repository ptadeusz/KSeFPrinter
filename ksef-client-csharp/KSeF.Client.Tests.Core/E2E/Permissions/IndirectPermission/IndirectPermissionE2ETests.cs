using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.IndirectPermission;

/// <summary>
/// Testy end-to-end dla nadawania uprawnień w sposób pośredni systemie KSeF.
/// Obejmuje scenariusze nadawania i odwoływania uprawnień oraz ich weryfikację.
/// </summary>
[Collection("IndirectPermissionScenario")]
public class IndirectPermissionE2ETests : TestBase
{
    private const int OperationSuccessfulStatusCode = 200;
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(SleepTime);

    private string ownerAccessToken { get; set; }
    private string ownerNip { get; set; }

    private string delegateAccessToken { get; set; }
    private string delegateNip { get; set; }

    private Client.Core.Models.Permissions.IndirectEntity.SubjectIdentifier Subject { get; } =
        new Client.Core.Models.Permissions.IndirectEntity.SubjectIdentifier
        {
            Type = Client.Core.Models.Permissions.IndirectEntity.SubjectIdentifierType.Nip
        };


    public IndirectPermissionE2ETests()
    {
        ownerNip = MiscellaneousUtils.GetRandomNip();
        delegateNip = MiscellaneousUtils.GetRandomNip();
        Subject.Value = MiscellaneousUtils.GetRandomNip();
    }

    /// <summary>
    /// Wykonuje kompletny scenariusz obsługi uprawnień pośrednich E2E:
    /// 1. Nadaje uprawnienia CredentialsManage dla pośrednika
    /// 2. Nadaje uprawnienia pośrednie
    /// 3. Wyszukuje nadane uprawnienia
    /// 4. Usuwa uprawnienia
    /// 5. Sprawdza, że uprawnienia zostały poprawnie usunięte
    /// </summary>
    [Fact]
    public async Task IndirectPermission_E2E_GrantSearchRevokeSearch()
    {
        // Arrange: Uwierzytelnienie właściciela 
        ownerAccessToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, ownerNip)).AccessToken.Token;

        // Act: 1) Nadanie uprawnień CredentialsManage dla pośrednika
        PermissionsOperationStatusResponse personGrantStatus = await GrantCredentialsManageToDelegateAsync();

        // Assert
        Assert.NotNull(personGrantStatus);
        Assert.Equal(OperationSuccessfulStatusCode, personGrantStatus.Status.Code);

        // Arrange: Uwierzytelnienie pośrednika (Arrange)
        delegateAccessToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, delegateNip)).AccessToken.Token;

        // Act: 2) Nadanie uprawnień pośrednich przez pośrednika
        PermissionsOperationStatusResponse indirectGrantStatus = await GrantIndirectPermissionsAsync();

        // Assert
        Assert.NotNull(indirectGrantStatus);
        Assert.Equal(OperationSuccessfulStatusCode, indirectGrantStatus.Status.Code);

        // Act: 3) Wyszukanie nadanych uprawnień (w bieżącym kontekście, nieaktywne)
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> permissionsAfterGrant =
            await SearchGrantedPersonPermissionsInCurrentContextAsync(
                accessToken: delegateAccessToken,
                includeInactive: true,
                pageOffset: 0,
                pageSize: 10);

        // Assert
        Assert.NotNull(permissionsAfterGrant);
        Assert.NotEmpty(permissionsAfterGrant.Permissions);

        // Poll: upewnij się, że uprawnienia są widoczne/ustabilizowane przed cofnięciem
        permissionsAfterGrant = await AsyncPollingUtils.PollAsync(
            action: () => SearchGrantedPersonPermissionsInCurrentContextAsync(
                accessToken: delegateAccessToken,
                includeInactive: true,
                pageOffset: 0,
                pageSize: 10),
            condition: r => r.Permissions is { Count: > 0 },
            delay: PollDelay,
            maxAttempts: 30,
            cancellationToken: CancellationToken
        );

        // Act: 4) Cofnięcie nadanych uprawnień
        List<PermissionsOperationStatusResponse> revokeResult = await RevokePermissionsAsync(permissionsAfterGrant.Permissions, delegateAccessToken);

        // Assert
        Assert.NotNull(revokeResult);
        Assert.NotEmpty(revokeResult);
        Assert.Equal(permissionsAfterGrant.Permissions.Count, revokeResult.Count);
        Assert.All(revokeResult, r =>
            Assert.True(r.Status.Code == OperationSuccessfulStatusCode,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );

        // Poll: 5) Wyszukanie po cofnięciu – oczekujemy pustej listy
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> permissionsAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                action: () => SearchGrantedPersonPermissionsInCurrentContextAsync(
                    accessToken: delegateAccessToken,
                    includeInactive: true,
                    pageOffset: 0,
                    pageSize: 10),
                condition: r => r.Permissions is { Count: 0 },
                delay: PollDelay,
                maxAttempts: 60,
                cancellationToken: CancellationToken
            );

        // Assert
        Assert.NotNull(permissionsAfterRevoke);
        Assert.Empty(permissionsAfterRevoke.Permissions);
    }

    /// <summary>
    /// Nadaje uprawnienie CredentialsManage przez właściciela dla pośrednika
    /// </summary>
    /// <returns>Status operacji nadania uprawnień osobowych</returns>
    private async Task<PermissionsOperationStatusResponse> GrantCredentialsManageToDelegateAsync()
    {
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(
                new Client.Core.Models.Permissions.Person.SubjectIdentifier
                {
                    Type = Client.Core.Models.Permissions.Person.SubjectIdentifierType.Nip,
                    Value = delegateNip
                }
            )
            .WithPermissions(Client.Core.Models.Permissions.Person.StandardPermissionType.CredentialsManage)
            .WithDescription("E2E test - nadanie uprawnień CredentialsManage do zarządzania uprawnieniami")
            .Build();

        OperationResponse grantOperationResponse =
            await KsefClient.GrantsPermissionPersonAsync(request, ownerAccessToken, CancellationToken);

        Assert.NotNull(grantOperationResponse);
        Assert.False(string.IsNullOrEmpty(grantOperationResponse.OperationReferenceNumber));

        // Poll zamiast stałego opóźnienia
        PermissionsOperationStatusResponse grantOperationStatus =
            await WaitForOperationSuccessAsync(grantOperationResponse.OperationReferenceNumber, ownerAccessToken);

        return grantOperationStatus;
    }

    /// <summary>
    /// Nadaje uprawnienia pośrednie dla podmiotu w kontekście wskazanego NIP przez pośrednika.
    /// </summary>
    /// <returns>Status operacji nadania uprawnień pośrednich</returns>
    private async Task<PermissionsOperationStatusResponse> GrantIndirectPermissionsAsync()
    {
        GrantPermissionsIndirectEntityRequest request = GrantIndirectEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(Subject)
            .WithContext(
                new Client.Core.Models.Permissions.IndirectEntity.TargetIdentifier
                {
                    Type = Client.Core.Models.Permissions.IndirectEntity.TargetIdentifierType.Nip,
                    Value = ownerNip
                }
            )
            .WithPermissions(
                Client.Core.Models.Permissions.IndirectEntity.StandardPermissionType.InvoiceRead,
                Client.Core.Models.Permissions.IndirectEntity.StandardPermissionType.InvoiceWrite
            )
            .WithDescription("E2E test - przekazanie uprawnień (InvoiceRead, InvoiceWrite) przez pośrednika")
            .Build();

        OperationResponse grantOperationResponse =
            await KsefClient.GrantsPermissionIndirectEntityAsync(request, delegateAccessToken, CancellationToken);

        Assert.NotNull(grantOperationResponse);
        Assert.False(string.IsNullOrEmpty(grantOperationResponse.OperationReferenceNumber));

        // Poll zamiast stałego opóźnienia
        PermissionsOperationStatusResponse grantOperationStatus =
            await WaitForOperationSuccessAsync(grantOperationResponse.OperationReferenceNumber, delegateAccessToken);

        return grantOperationStatus;
    }

    /// <summary>
    /// Wyszukuje uprawnienia nadane osobom w bieżącym kontekście.
    /// Możliwe jest włączenie filtracji po stanie (np. nieaktywne).
    /// </summary>
    /// <returns>Zwraca stronicowaną listę uprawnień.</returns>
    private async Task<Client.Core.Models.Permissions.PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>>
        SearchGrantedPersonPermissionsInCurrentContextAsync(
            string accessToken,
            bool includeInactive,
            int pageOffset,
            int pageSize)
    {
        Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest query = new Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest
        {
            QueryType = Client.Core.Models.Permissions.Person.QueryTypeEnum.PermissionsGrantedInCurrentContext,
            PermissionState = includeInactive
                ? Client.Core.Models.Permissions.Person.PermissionState.Inactive
                : Client.Core.Models.Permissions.Person.PermissionState.Active
        };

        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> pagedPermissionsResponse = await KsefClient.SearchGrantedPersonPermissionsAsync(
            query,
            accessToken,
            pageOffset: pageOffset,
            pageSize: pageSize,
            CancellationToken);
        return pagedPermissionsResponse;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    private async Task<List<PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<Client.Core.Models.Permissions.PersonPermission> permissions,
        string accessToken)
    {
        List<KSeF.Client.Core.Models.Permissions.OperationResponse> revokeResponses = new List<KSeF.Client.Core.Models.Permissions.OperationResponse>();

        // Uruchomienie operacji cofania
        foreach (Client.Core.Models.Permissions.PersonPermission permission in permissions)
        {
            Client.Core.Models.Permissions.OperationResponse response = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(response);
        }

        // Poll statusów wszystkich operacji (równolegle)
        var revokeStatusTasks = revokeResponses
            .Select(r => WaitForOperationSuccessAsync(r.OperationReferenceNumber, accessToken))
            .ToArray();

        Client.Core.Models.Permissions.PermissionsOperationStatusResponse[] revokeStatusResults =
            await Task.WhenAll(revokeStatusTasks);

        return revokeStatusResults.ToList();
    }

    /// <summary>
    /// Czeka aż status operacji będzie pomyślny (200) z wykorzystaniem pollingu.
    /// </summary>
    private Task<Client.Core.Models.Permissions.PermissionsOperationStatusResponse> WaitForOperationSuccessAsync(
        string operationReferenceNumber,
        string accessToken)
        => AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(operationReferenceNumber, accessToken),
            condition: r => r.Status.Code == OperationSuccessfulStatusCode,
            delay: PollDelay,
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );
}