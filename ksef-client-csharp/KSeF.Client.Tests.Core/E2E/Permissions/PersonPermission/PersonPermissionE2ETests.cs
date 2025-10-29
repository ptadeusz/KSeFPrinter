using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermission;

public class PersonPermissionE2ETests : TestBase
{
    private const string PermissionDescription = "E2E test grant";
    private const int OperationSuccessfulStatusCode = 200;

    private string accessToken = string.Empty;
    private KSeF.Client.Core.Models.Permissions.Person.SubjectIdentifier Person { get; } = new();

    public PersonPermissionE2ETests()
    {
        // Arrange: uwierzytelnienie i przygotowanie danych testowych
        Client.Core.Models.Authorization.AuthOperationStatusResponse auth = AuthenticationUtils
            .AuthenticateAsync(KsefClient, SignatureService)
            .GetAwaiter().GetResult();

        accessToken = auth.AccessToken.Token;

        // Ustaw dane osoby testowej (PESEL)
        Person.Value = MiscellaneousUtils.GetRandomPesel();
        Person.Type = SubjectIdentifierType.Pesel;
    }

    /// <summary>
    /// Testy E2E nadawania i cofania uprawnień dla osób:
    /// - nadanie uprawnień
    /// - wyszukanie nadanych uprawnień
    /// - cofnięcie uprawnień
    /// - ponowne wyszukanie (weryfikacja, że zostały cofnięte)
    /// </summary>
    [Fact]
    public async Task PersonPermissions_FullFlow_GrantSearchRevokeSearch()
    {
        // Arrange: dane wejściowe i oczekiwane typy uprawnień
        string description = PermissionDescription;

        // Act: nadaj uprawnienia dla osoby
        OperationResponse grantResponse =
            await GrantPersonPermissionsAsync(Person, description, accessToken);

        // Assert: weryfikacja poprawności odpowiedzi operacji nadania
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.OperationReferenceNumber));

        // Act: odpytywanie do momentu, aż nadane uprawnienia będą widoczne (obie pule: Read i Write)
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterGrant =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                result =>
                {
                    if (result is null || result.Permissions is null) return false;

                    List<KSeF.Client.Core.Models.Permissions.PersonPermission> byDescription =
                        result.Permissions.Where(p => p.Description == description).ToList();

                    bool hasRead = byDescription.Any(x => Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceRead);
                    bool hasWrite = byDescription.Any(x => Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceWrite);

                    return byDescription.Count > 0 && hasRead && hasWrite;
                },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // Assert: upewnij się, że uprawnienia są widoczne i zawierają oczekiwane zakresy
        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);

        List<KSeF.Client.Core.Models.Permissions.PersonPermission> grantedNow =
            searchAfterGrant.Permissions
                .Where(p => p.Description == description)
                .ToList();

        Assert.NotEmpty(grantedNow);
        Assert.Contains(grantedNow, x => Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceRead);
        Assert.Contains(grantedNow, x => Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceWrite);

        // Act: cofnij nadane uprawnienia
        List<PermissionsOperationStatusResponse> revokeResult = await RevokePermissionsAsync(searchAfterGrant.Permissions, accessToken);

        // Assert: weryfikacja wyników cofania
        Assert.NotNull(revokeResult);
        Assert.NotEmpty(revokeResult);
        Assert.Equal(searchAfterGrant.Permissions.Count, revokeResult.Count);
        Assert.All(revokeResult, r =>
            Assert.True(r.Status.Code == OperationSuccessfulStatusCode,
                $"Operacja cofnięcia uprawnień nie powiodła się: {r.Status.Description}, szczegóły: [{string.Join(",", r.Status.Details ?? Array.Empty<string>())}]")
        );

        // Act: odpytywanie do momentu, aż uprawnienia o danym opisie znikną
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterRevoke =
            await AsyncPollingUtils.PollAsync(
                async () => await SearchGrantedPersonPermissionsAsync(accessToken),
                result =>
                {
                    if (result is null || result.Permissions is null) return false;

                    List<KSeF.Client.Core.Models.Permissions.PersonPermission> remainingLocal =
                        result.Permissions.Where(p => p.Description == description).ToList();

                    return remainingLocal.Count == 0;
                },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // Assert: upewnij się, że nie pozostały wpisy z danym opisem
        Assert.NotNull(searchAfterRevoke);

        List<KSeF.Client.Core.Models.Permissions.PersonPermission> remaining =
            searchAfterRevoke.Permissions
                .Where(p => p.Description == description)
                .ToList();

        Assert.Empty(remaining);
    }

    /// <summary>
    /// Nadaje uprawnienia dla osoby i zwraca odpowiedź operacji.
    /// </summary>
    private async Task<OperationResponse> GrantPersonPermissionsAsync(
        Client.Core.Models.Permissions.Person.SubjectIdentifier subject,
        string description,
        string accessToken)
    {
        // Arrange: zbudowanie żądania nadania uprawnień
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                StandardPermissionType.InvoiceRead,
                StandardPermissionType.InvoiceWrite)
            .WithDescription(description)
            .Build();

        // Act: wywołanie API nadawania uprawnień
        OperationResponse response =
            await KsefClient.GrantsPermissionPersonAsync(request, accessToken, CancellationToken);

        // Assert: zwrócenie odpowiedzi (asercje są wykonywane w teście)
        return response;
    }

    /// <summary>
    /// Wyszukuje nadane uprawnienia dla osób i zwraca wynik wyszukiwania.
    /// </summary>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>>
        SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        // Arrange: budowa zapytania wyszukującego uprawnienia
        PersonPermissionsQueryRequest query = new PersonPermissionsQueryRequest
        {
            PermissionTypes = new List<PersonPermissionType>
            {
                PersonPermissionType.InvoiceRead,
                PersonPermissionType.InvoiceWrite
            }
        };

        // Act: wywołanie API wyszukiwania
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> response =
            await KsefClient.SearchGrantedPersonPermissionsAsync(query, accessToken, pageOffset: 0, pageSize: 10, CancellationToken);

        // Assert: zwrócenie wyniku wyszukiwania
        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień i zwrócenie statusów operacji.
    /// </summary>
    private async Task<List<Client.Core.Models.Permissions.PermissionsOperationStatusResponse>> RevokePermissionsAsync(
        IEnumerable<KSeF.Client.Core.Models.Permissions.PersonPermission> grantedPermissions,
        string accessToken)
    {
        // Arrange: lista odpowiedzi z operacji cofania
        List<Client.Core.Models.Permissions.OperationResponse> revokeResponses = new List<Client.Core.Models.Permissions.OperationResponse>();

        // Act: uruchomienie operacji cofania dla każdej pozycji
        foreach (Client.Core.Models.Permissions.PersonPermission permission in grantedPermissions)
        {
            Client.Core.Models.Permissions.OperationResponse response = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken.None);
            revokeResponses.Add(response);
        }

        // Act: odpytywanie statusów do skutku (sukces) i zebranie wyników
        List<Client.Core.Models.Permissions.PermissionsOperationStatusResponse> statuses = new List<Client.Core.Models.Permissions.PermissionsOperationStatusResponse>();

        foreach (Client.Core.Models.Permissions.OperationResponse revokeResponse in revokeResponses)
        {
            Client.Core.Models.Permissions.PermissionsOperationStatusResponse status =
                await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(revokeResponse.OperationReferenceNumber, accessToken),
                    result => result is not null && result.Status is not null && result.Status.Code == OperationSuccessfulStatusCode,
                    delay: TimeSpan.FromMilliseconds(SleepTime),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken);

            statuses.Add(status);
        }

        // Assert: zwrócenie statusów (asercje w teście nadrzędnym)
        return statuses;
    }
}