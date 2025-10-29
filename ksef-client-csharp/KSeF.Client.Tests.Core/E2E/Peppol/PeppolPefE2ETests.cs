using KSeF.Client.Api.Builders.AuthorizationPermissions;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Peppol;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace KSeF.Client.Tests.Core.E2E.Peppol
{
    /// <summary>
    /// Scenariusz E2E:
    /// 1) Dostawca rejestruje się automatycznie (pierwsze uwierzytelnienie pieczęcią z O + CN=PeppolId).
    /// 2) Firma nadaje uprawnienie PefInvoiceWrite (PefInvoicing) temu dostawcy.
    /// 3) Dostawca wysyła fakturę PEF w imieniu firmy.
    /// </summary>
    [Collection("OnlineSessionScenario")]
    public class PeppolPefE2ETests : TestBase
    {
        private const int StatusProcessing = 100;
        private const int StatusSessionClosed = 170;
        private const int StatusInvoiceProcessing = 150;
        private const string PefTemplate = "invoice-template-fa-3-pef.xml";

        // Wymaganie PeppolId (CN):
        private static readonly Regex PeppolIdRegex =
            new(@"^P[A-Z]{2}[0-9]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string _accessToken; // token firmy (NIP) – do odczytów i ewentualnie innych operacji
        private readonly string _companyNip;
        private readonly string _buyerNip;
        private readonly string _iban;
        private string _peppol;
        private string _privateKeyBase64;
        private string _publicKeyBase64;

        public PeppolPefE2ETests()
        {
            // Token firmy (XAdES) – jak w pozostałych E2E (posłuży do odczytów/list i ewentualnej sesji, ale wysyłkę robi dostawca)
            _companyNip = MiscellaneousUtils.GetRandomNip();
            _buyerNip = MiscellaneousUtils.GetRandomNip();
            _iban = MiscellaneousUtils.GeneratePolishIban();

            AuthOperationStatusResponse auth = AuthenticationUtils
                .AuthenticateAsync(KsefClient, SignatureService, _companyNip)
                .GetAwaiter().GetResult();

            _accessToken = auth.AccessToken.Token;
        }

        [Fact]
        public async Task Peppol_PEF_FullFlow_AutoRegister_Grant_Send()
        {
            // === 0) AUTO-REJESTRACJA dostawcy: pierwsze uwierzytelnienie pieczęcią z O + CN=PeppolId
            // Arrange
            // (dane przygotowane w konstruktorze)

            // Act
            (string peppolId, string providerToken) = await AutoRegisterProviderAsync();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(peppolId));
            Assert.Matches(PeppolIdRegex, peppolId);
            Assert.False(string.IsNullOrWhiteSpace(providerToken));
            _peppol = peppolId;

            // === 1) RESOLVE PROVIDER===
            // Arrange

            // Act
            PeppolProvider? provider = await FindProviderAsync(peppolId);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(peppolId, provider!.Id);

            // === 2) GRANT: Firma -> Dostawca (PefInvoicing) ===
            // Arrange
            // (peppolId + _accessToken)

            // Act
            await GrantPefInvoicingAsync(peppolId);

            // Assert (lekka weryfikacja, bez przeszukiwania całej listy jeśli nie chcesz)
            var query = new EntityAuthorizationsQueryRequest
            {
                AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier { Type = "Nip", Value = _companyNip },
                AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier { Type = "PeppolId", Value = peppolId },
                QueryType = QueryType.Granted,
                PermissionTypes = new() { InvoicePermissionType.PefInvoicing }
            };

            var authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
                requestPayload: query,
                accessToken: _accessToken,
                pageOffset: 0,
                pageSize: 10,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(authz);

            // === 3) WYSYŁKA PEF przez dostawcę ===
            // Arrange
            // (providerToken, NIPy, IBAN, template)

            // Act
            string upo = await SendPefInvoiceFlowAsync(providerToken);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(upo));
        }

        // -----------------------------
        // KROK 0: Auto-rejestracja
        // -----------------------------
        private async Task<(string peppolId, string providerToken)> AutoRegisterProviderAsync()
        {
            string country = (Environment.GetEnvironmentVariable("KSEF_PEP_COUNTRY") ?? "PL").ToUpperInvariant();
            string peppolId = $"P{country}{new Random().Next(0, 1_000_000):000000}";
            Assert.True(PeppolIdRegex.IsMatch(peppolId), $"PeppolId '{peppolId}' nie spełnia ^P[A-Z]{{2}}[0-9]{{6}}$.");

            string organizationName = Environment.GetEnvironmentVariable("KSEF_PEP_ORG") ?? "E2E Peppol Test Provider";
            string organizationIdentifier = Environment.GetEnvironmentVariable("KSEF_PEP_ORG_ID") ?? peppolId;

            // Pieczęć ustawia O + CN (spełniamy wymagania Peppol)
            X509Certificate2 providerSeal = CertificateUtils.GetCompanySeal(
                organizationName: organizationName,
                organizationIdentifier: organizationIdentifier,
                commonName: peppolId);

            using RSA rsaPrivateKey = providerSeal.GetRSAPrivateKey()!;
            using RSA rsaPublicKey = providerSeal.GetRSAPublicKey()!;

            _privateKeyBase64 =
                "-----BEGIN PRIVATE KEY-----\n" +
                Convert.ToBase64String(rsaPrivateKey.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks) +
                "\n-----END PRIVATE KEY-----";

            _publicKeyBase64 =
                "-----BEGIN PUBLIC KEY-----\n" +
                Convert.ToBase64String(rsaPublicKey.ExportSubjectPublicKeyInfo(), Base64FormattingOptions.InsertLineBreaks) +
                "\n-----END PUBLIC KEY-----";

            AuthOperationStatusResponse providerAuth = await AuthenticationUtils.AuthenticateAsync(
                ksefClient: KsefClient,
                signatureService: SignatureService,
                certificate: providerSeal,
                contextIdentifierType: Client.Core.Models.Authorization.ContextIdentifierType.PeppolId,
                contextIdentifierValue: peppolId);

            Assert.NotNull(providerAuth?.AccessToken);
            return (peppolId, providerAuth.AccessToken.Token);
        }

        // -----------------------------
        // KROK 1: Znalezienie dostawcy - Lista dostawców – paginacja HasMore + krótki retry, szukamy KONKRETNEGO peppolId
        // -----------------------------
        private async Task<PeppolProvider?> FindProviderAsync(string peppolId)
        {
            PeppolProvider? resolved = null;

            await AsyncPollingUtils.PollAsync(
                description: $"Znaleziono PeppolId {peppolId}",
                check: async () =>
                {
                    int? pageOffset = null;
                    const int pageSize = 100;
                    int guardPages = 200;

                    do
                    {
                        QueryPeppolProvidersResponse page = await KsefClient.QueryPeppolProvidersAsync(
                            accessToken: _accessToken,
                            pageOffset: pageOffset,
                            pageSize: pageSize,
                            cancellationToken: CancellationToken.None);

                        PeppolProvider? hit = page?.PeppolProviders?.FirstOrDefault(p => p.Id == peppolId);
                        if (hit != null)
                        {
                            resolved = hit;
                            return true;
                        }

                        if (page?.HasMore == true)
                        { 
                            pageOffset = (pageOffset ?? 0) + (page?.PeppolProviders?.Count ?? 0);
                        }
                        else break;
                    } while (guardPages-- > 0);

                    return false;
                },
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 3);

            Assert.True(resolved != null,
                $"Brak PeppolId '{peppolId}' po auto-rejestracji. Sprawdź CN/O oraz zwiększ retry.");

            return resolved;
        }

        // -----------------------------
        // KROK 2: Grant PefInvoicing - Firma nadaje uprawnienie PefInvoiceWrite (PefInvoicing - uprawnenie sesyjne) dla DOSTAWCY (kontekst żądania: PeppolId)
        //    - bearer = token firny 
        //    - Subject = PeppolId (komu przyznajemy uprawnienie)
        // -----------------------------
        private async Task GrantPefInvoicingAsync(string peppolId)
        {
            GrantAuthorizationPermissionsRequest grantReq = GrantAuthorizationPermissionsRequestBuilder
                .Create()
                .WithSubject(new Client.Core.Models.Permissions.Authorizations.SubjectIdentifier
                {
                    Type = Client.Core.Models.Permissions.Authorizations.SubjectIdentifierType.PeppolId, 
                    Value = peppolId
                })
                .WithPermission(AuthorizationPermissionType.PefInvoicing)
                .WithDescription($"E2E: Nadanie uprawnienia do wystawiania faktur PEF dla firmy {_companyNip} (na wniosek {peppolId})")
                .Build();

            Client.Core.Models.Permissions.OperationResponse grantResp = await KsefClient.GrantsAuthorizationPermissionAsync(
                requestPayload: grantReq,
                accessToken: _accessToken,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(grantResp);

            // opcjonalnie: szybka walidacja listy grantów (w niektórych env może nie być 1:1)
            EntityAuthorizationsQueryRequest query = new EntityAuthorizationsQueryRequest
            {
                AuthorizingIdentifier = new EntityAuthorizationsAuthorizingEntityIdentifier
                {
                    Type = "Nip",
                    Value = _companyNip
                },
                AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier
                {
                    Type = "PeppolId",
                    Value = peppolId
                },
                QueryType = QueryType.Granted,
                PermissionTypes = new() { InvoicePermissionType.PefInvoicing }
            };

            Client.Core.Models.Permissions.PagedAuthorizationsResponse<Client.Core.Models.Permissions.AuthorizationGrant> authz = await KsefClient.SearchEntityAuthorizationGrantsAsync(
                requestPayload: query,
                accessToken: _accessToken,
                pageOffset: 0,
                pageSize: 10,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(authz);
        }

        // -----------------------------
        // KROK 3: Wysyłka PEF (sesja online)
        // -----------------------------
        private async Task<string> SendPefInvoiceFlowAsync(string providerToken)
        {
            Client.Core.Models.Sessions.EncryptionData encryptionData = CryptographyService.GetEncryptionData();

            Client.Core.Models.Sessions.OnlineSession.OpenOnlineSessionResponse openSession = await OnlineSessionUtils.OpenOnlineSessionAsync(
                ksefClient: KsefClient,
                encryptionData: encryptionData,
                accessToken: providerToken,
                systemCode: SystemCodeEnum.FAPEF);

            Assert.NotNull(openSession);
            Assert.False(string.IsNullOrWhiteSpace(openSession.ReferenceNumber));

            Client.Core.Models.Sessions.OnlineSession.SendInvoiceResponse sendResp = await OnlineSessionUtils.SendPefInvoiceAsync(
                ksefClient: KsefClient,
                sessionReferenceNumber: openSession.ReferenceNumber,
                accessToken: providerToken,
                supplierNip: $"PL{_companyNip}",
                customerNip: $"PL{_buyerNip}",
                buyerReference: $"PL{_buyerNip}",
                iban: _iban,
                templatePath: PefTemplate,
                encryptionData: encryptionData,
                cryptographyService: CryptographyService);

            Assert.NotNull(sendResp);

            // status: oczekujemy Processing (100), bez błędów
            Client.Core.Models.Sessions.SessionStatusResponse statusProcessing = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);
            Assert.NotNull(statusProcessing);            

            SessionFailedInvoicesResponse failedInvoices;
            if (statusProcessing.FailedInvoiceCount is not null)
            {
                failedInvoices = await KsefClient.GetSessionFailedInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10, continuationToken: string.Empty, CancellationToken.None);
            }

            Assert.Null(statusProcessing.FailedInvoiceCount);
            Assert.Equal(StatusProcessing, statusProcessing.Status.Code);
            Assert.Equal(StatusProcessing, statusProcessing.Status.Code);

            await KsefClient.CloseOnlineSessionAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);

            // czekamy na zamknięcie sesji deterministycznym pollingiem
            await AsyncPollingUtils.PollAsync(
               description: $"Sesja zamknięta dla {_peppol}",
               check: async () =>
               {
                   Client.Core.Models.Sessions.SessionStatusResponse st = await KsefClient.GetSessionStatusAsync(openSession.ReferenceNumber, providerToken, CancellationToken.None);
                   return st?.Status?.Code == StatusSessionClosed;
               },
               delay: TimeSpan.FromMilliseconds(SleepTime),
               maxAttempts: 10);

            var invoices = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10);
            Assert.NotNull(invoices);
            Assert.NotEmpty(invoices.Invoices);

            Client.Core.Models.Sessions.SessionInvoice sessionInvoice = invoices.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);

            // jeżeli faktura jeszcze jest w statusie „processing”, spróbuj odświeżyć kilka razy zamiast jednego sleepa
            if (sessionInvoice.Status.Code == StatusInvoiceProcessing)
            {
                await AsyncPollingUtils.PollAsync(
                   description: "faktura gotowa (status inny niż processing)",
                   check: async () =>
                   {
                   Client.Core.Models.Sessions.SessionInvoicesResponse inv = await KsefClient.GetSessionInvoicesAsync(openSession.ReferenceNumber, providerToken, pageSize: 10);
                   Client.Core.Models.Sessions.SessionInvoice refreshed = inv.Invoices.First(x => x.ReferenceNumber == sendResp.ReferenceNumber);
                       return refreshed.Status.Code != StatusInvoiceProcessing;
                   },
                   delay: TimeSpan.FromMilliseconds(SleepTime),
                   maxAttempts: 5);
            }

            // UPO:
            var upo = await KsefClient.GetSessionInvoiceUpoByReferenceNumberAsync(
                openSession.ReferenceNumber,
                sendResp.ReferenceNumber,
                providerToken);

            Assert.NotNull(upo);
            return upo;
        }
    }
}
