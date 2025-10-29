using KSeF.Client.Api.Builders.EUEntityPermissions;
using KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EuRepresentativePermission;

public class EuRepresentativePermissionE2ETests : TestBase
{
    /*
     * 1. Autentykacja jako owner - context NIP
     * 2. Owner nadaje uprawnienia administracyjne jednostce organizacyjnej - context NipVatEu 
     * 3. Pobranie uprawnień nadanych contextowi
     * 4. Autentykacja jako jednostka organizacyjna - context NipVatEu
     * 5. W tokenie powinny być role jednostki
     * 6. Nadanie uprawnień reprezentanta 
     * 7. Pobranie/sprawdzenie nadanych uprawnień
     * 8. Odwołanie uprawnień (wymaga odczekania kilku sekund...)
     * 9. Sprawdzenie uprawnień po odwołaniu
     */

    [Fact]
    public async Task GrantAdministrativePermission_E2E_ReturnsExpectedResults()
    {
        // Przygotowanie
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string ownerVatEu = MiscellaneousUtils.GetRandomVatEU(ownerNip);
        string ownerNipVatEu = MiscellaneousUtils.GetNipVatEU(ownerNip, ownerVatEu);

        string euEntityNip = MiscellaneousUtils.GetRandomNip();
        string euEntityVatEu = MiscellaneousUtils.GetRandomVatEU(euEntityNip);
        string euEntityNipVatEu = MiscellaneousUtils.GetNipVatEU(euEntityNip, euEntityVatEu);

        string euRepresentativeEntityNip = MiscellaneousUtils.GetRandomNip();
        string euRepresentativeEntityVatEu = MiscellaneousUtils.GetRandomVatEU(euRepresentativeEntityNip);
        string euRepresentativeEntityNipVatEu = MiscellaneousUtils.GetNipVatEU(euRepresentativeEntityNip, euRepresentativeEntityVatEu);

        X509Certificate2 ownerCertificate = CertificateUtils.GetPersonalCertificate("Jan", "Kowalski", "TINPL", ownerNip, "M B");
        string ownerCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(ownerCertificate);

        X509Certificate2 euEntitySealCertificate = CertificateUtils.GetCompanySeal("Kowalski sp. z o.o", euEntityNipVatEu, "Kowalski");
        string euEntitySealCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(euEntitySealCertificate);

        X509Certificate2 euEntityPersonalCertificate = CertificateUtils.GetPersonalCertificate("Paweł", "Testowy", "TINPL", euEntityNip, "T P");
        string euEntityPersonalCertificateFingerprint = CertificateUtils.GetSha256Fingerprint(euEntityPersonalCertificate);

        X509Certificate2 euRepresentativeEntityCerticate = CertificateUtils.GetPersonalCertificate(
            "Reprezentant M",
            "Reprezentant B",
            "TINPL",
            euRepresentativeEntityNip,
            "M B"
            );
        string euRepresentativeEntityCerticateFingerprint = CertificateUtils.GetSha256Fingerprint(euRepresentativeEntityCerticate);

        // Działanie
        // 1) Uwierzytelnij właściciela (kontekst NIP)
        Client.Core.Models.Authorization.AuthOperationStatusResponse ownerAuthInfo = await AuthenticationUtils.AuthenticateAsync(
            KsefClient,
            SignatureService,
            ownerNip,
            Client.Core.Models.Authorization.ContextIdentifierType.Nip,
            ownerCertificate);

        // 2) Właściciel nadaje uprawnienia administracyjne jednostce unijnej (kontekst NipVatEu)
        GrantPermissionsRequest grantAdministrativePermissionsRequest = GrantEUEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(new SubjectIdentifier
            {
                Type = Client.Core.Models.Permissions.EUEntity.SubjectIdentifierType.Fingerprint,
                Value = euEntityPersonalCertificateFingerprint
            })
            .WithSubjectName("MB Company")
            .WithContext(new ContextIdentifier { Type = ContextIdentifierType.NipVatUe, Value = ownerNipVatEu })
            .WithDescription("EU Company")
            .Build();

        Client.Core.Models.Permissions.OperationResponse response = await KsefClient
            .GrantsPermissionEUEntityAsync(grantAdministrativePermissionsRequest, ownerAuthInfo.AccessToken.Token, CancellationToken);

        // 3) Odpytywanie aż uprawnienia nadane dla kontekstu będą widoczne
        EuEntityPermissionsQueryRequest permissionsQueryRequest = new EuEntityPermissionsQueryRequest { };

        var grantedPermissions = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.SearchGrantedEuEntityPermissionsAsync(permissionsQueryRequest, ownerAuthInfo.AccessToken.Token),
            condition: result => result is not null && result.Permissions is { Count: > 0 },
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        // 4) Uwierzytelnij jako jednostka unijna (kontekst NipVatEu) używając certyfikatu osobistego jednostki unijnej
        Client.Core.Models.Authorization.AuthOperationStatusResponse euAuthInfo = await AuthenticationUtils.AuthenticateAsync(
            KsefClient,
            SignatureService,
            ownerNipVatEu, // nipvateu kontekstu
            Client.Core.Models.Authorization.ContextIdentifierType.NipVatUe, // typ identyfikatora kontekstu
            euEntityPersonalCertificate, // certyfikat jednostki eu
            Client.Core.Models.Authorization.SubjectIdentifierTypeEnum.CertificateFingerprint); // typ identyfiktora jednostki eu

        // 6) Nadaj uprawnienia reprezentanta
        Client.Core.Models.Permissions.EUEntityRepresentative.GrantPermissionsEUEntitRepresentativeRequest grantRepresentativePermissionsRequest =
            GrantEUEntityRepresentativePermissionsRequestBuilder
                .Create()
                .WithSubject(new Client.Core.Models.Permissions.EUEntityRepresentative.SubjectIdentifier
                {
                    Type = Client.Core.Models.Permissions.EUEntityRepresentative.SubjectIdentifierType.Fingerprint,
                    Value = euRepresentativeEntityCerticateFingerprint
                })
                .WithPermissions(
                    Client.Core.Models.Permissions.EUEntityRepresentative.StandardPermissionType.InvoiceWrite,
                    Client.Core.Models.Permissions.EUEntityRepresentative.StandardPermissionType.InvoiceRead
                )
                .WithDescription("Representative for EU Entity")
                .Build();

        Client.Core.Models.Permissions.OperationResponse grantRepresentativePermissionResponse =
            await KsefClient.GrantsPermissionEUEntityRepresentativeAsync(
                grantRepresentativePermissionsRequest,
                euAuthInfo.AccessToken.Token,
                CancellationToken);

        // 7) Odpytywanie aż uprawnienia reprezentanta będą widoczne
        var grantedRepresentativePermission = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.SearchGrantedEuEntityPermissionsAsync(permissionsQueryRequest, euAuthInfo.AccessToken.Token),
            condition: result => result is not null && result.Permissions is { Count: > 0 },
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        // 8) Odwołaj uprawnienia reprezentanta (wszystkie zwrócone)
        foreach (var permission in grantedRepresentativePermission.Permissions)
        {
            await KsefClient.RevokeCommonPermissionAsync(permission.Id, euAuthInfo.AccessToken.Token);
        }

        // 9) Odpytywanie aż uprawnienia znikną
        var afterRevoke = await AsyncPollingUtils.PollAsync(
            action: async () => await KsefClient.SearchGrantedEuEntityPermissionsAsync(permissionsQueryRequest, euAuthInfo.AccessToken.Token),
            condition: result => result is not null && (result.Permissions is null || result.Permissions.Count == 0),
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken
        );

        // Weryfikacja
        Assert.NotNull(grantedPermissions);
        Assert.NotEmpty(grantedPermissions.Permissions);

        Assert.NotNull(euAuthInfo);
        Assert.NotNull(euAuthInfo.AccessToken);
        Assert.NotNull(euAuthInfo.RefreshToken);

        Client.Core.Models.Token.PersonToken personTokenInfo = TokenService.MapFromJwt(euAuthInfo.AccessToken.Token);
        Assert.NotNull(personTokenInfo);

        Assert.NotNull(grantRepresentativePermissionResponse);
        Assert.NotNull(grantedRepresentativePermission);
        Assert.NotEmpty(grantedRepresentativePermission.Permissions);

        Assert.NotNull(afterRevoke);
        Assert.Empty(afterRevoke.Permissions);
    }
}