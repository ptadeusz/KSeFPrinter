using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Core.Models;
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;

namespace KSeF.Client.Tests.Utils;
public static class AuthenticationUtils
{
    private const int AuthInProgressCode = 100;
    private const int AuthSuccessCode = 200;

    /// <summary>
    /// Przeprowadza pełny proces uwierzytelnienia w KSeF z wykorzystaniem podpisu XAdES dla wskazanego NIP.
    /// </summary>
    public static async Task<AuthOperationStatusResponse> AuthenticateAsync(
        IKSeFClient ksefClient,
        ISignatureService signatureService,
        string nip,
        ContextIdentifierType contextIdentifierType = ContextIdentifierType.Nip,
        EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.Rsa)
    {
        AuthChallengeResponse challengeResponse = await ksefClient
            .GetAuthChallengeAsync();

        AuthTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            nip,
            SubjectIdentifierTypeEnum.CertificateSubject);

        string unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R");

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await ksefClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(ksefClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        AuthOperationStatusResponse authResult =
            await ksefClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
        return authResult;
    }

    /// <summary>
    /// Przeprowadza pełny proces uwierzytelnienia w KSeF z wykorzystaniem podpisu XAdES dla wskazanego numeru NIP w kontekście innego NIP.
    /// </summary>
    public static async Task<AuthOperationStatusResponse> AuthenticateAsync(
        IKSeFClient ksefClient,
        ISignatureService signatureService,
        string nip,
        string contextNip,
        ContextIdentifierType contextIdentifierType = ContextIdentifierType.Nip)
    {
        AuthChallengeResponse challengeResponse = await ksefClient
            .GetAuthChallengeAsync();

        AuthTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            contextNip,
            SubjectIdentifierTypeEnum.CertificateSubject);

        string unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate =
            CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R");

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await ksefClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(ksefClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        return await ksefClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
    }

    /// <summary>
    /// Przeprowadza proces uwierzytelnienia w KSeF generując losowy NIP (test) i wykorzystując podpis XAdES.
    /// </summary>
    public static async Task<AuthOperationStatusResponse> AuthenticateAsync(
        IKSeFClient ksefClient,
        ISignatureService signatureService,
        ContextIdentifierType contextIdentifierType = ContextIdentifierType.Nip,
        EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.Rsa
        )
    {
        string nip = MiscellaneousUtils.GetRandomNip();

        AuthChallengeResponse challengeResponse = await ksefClient
            .GetAuthChallengeAsync();

        AuthTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            nip,
            SubjectIdentifierTypeEnum.CertificateSubject);

        string unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        X509Certificate2 certificate = CertificateUtils.GetPersonalCertificate("A", "R", "TINPL", nip, "A R", encryptionMethod);

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await ksefClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(ksefClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        return await ksefClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
    }

    /// <summary>
    /// Przeprowadza uwierzytelnienie dla dostarczonego certyfikatu i parametrów identyfikatora kontekstu.
    /// </summary>
    public static async Task<AuthOperationStatusResponse> AuthenticateAsync(
        IKSeFClient ksefClient,
        ISignatureService signatureService,
        string contextIdentifierValue,
        ContextIdentifierType contextIdentifierType,
        X509Certificate2 certificate,
        SubjectIdentifierTypeEnum subjectIdentifierType = SubjectIdentifierTypeEnum.CertificateSubject)
    {
        AuthChallengeResponse challengeResponse = await ksefClient
            .GetAuthChallengeAsync();

        AuthTokenRequest authTokenRequest = GetAuthorizationTokenRequest(
            challengeResponse.Challenge,
            contextIdentifierType,
            contextIdentifierValue,
            subjectIdentifierType);

        string unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        string signedXml = signatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await ksefClient
            .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus finalStatus = await WaitForAuthCompletionAsync(ksefClient, authOperationInfo);
        EnsureSuccess(finalStatus);

        return await ksefClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
    }

    /// <summary>
    /// Buduje żądanie tokenu autoryzacyjnego (AuthTokenRequest).
    /// </summary>
    public static AuthTokenRequest GetAuthorizationTokenRequest(
        string challengeToken,
        ContextIdentifierType contextIdentifierType,
        string nip,
        SubjectIdentifierTypeEnum subjectIdentifierTypeEnum = SubjectIdentifierTypeEnum.CertificateSubject)
    {
        AuthTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeToken)
           .WithContext(contextIdentifierType, nip)
           .WithIdentifierType(subjectIdentifierTypeEnum)
           .WithAuthorizationPolicy(null)
           .Build();

        return authTokenRequest;
    }

    /// <summary>
    /// Wspólna logika oczekiwania na zakończenie operacji uwierzytelnienia.
    /// Zwraca finalny AuthStatus (kod != 100) lub ostatni status po przekroczeniu limitu czasu.
    /// </summary>
    private static async Task<AuthStatus> WaitForAuthCompletionAsync(
        IKSeFClient ksefClient,
        SignatureResponse authOperationInfo,
        TimeSpan? timeout = null,
        TimeSpan? pollDelay = null)
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromMinutes(2);
        TimeSpan delay = pollDelay ?? TimeSpan.FromSeconds(1);

        // Wylicz liczbę prób (>=1)
        int maxAttempts = (int)Math.Ceiling(effectiveTimeout.TotalMilliseconds / delay.TotalMilliseconds);
        if (maxAttempts <= 0) maxAttempts = 1;

        DateTime startTime = DateTime.UtcNow;
        AuthStatus? lastStatus = null;

        try
        {
            // Pollujemy aż status != 100 (czyli zakończony sukcesem lub błędem).
            AuthStatus finalStatus = await AsyncPollingUtils.PollAsync(
                action: async () =>
                {
                    var status = await ksefClient
                        .GetAuthStatusAsync(authOperationInfo.ReferenceNumber, authOperationInfo.AuthenticationToken.Token)
                        .ConfigureAwait(false);

                    lastStatus = status;

                    Console.WriteLine(
                        $"Odpytanie: KodStatusu={status.Status.Code}, " +
                        $"Opis='{status.Status.Description}', " +
                        $"Upłynęło={DateTime.UtcNow - startTime:mm\\:ss}");

                    return status;
                },
                condition: s => s.Status.Code != AuthInProgressCode,
                description: "Oczekiwanie na zakończenie uwierzytelnienia",
                delay: delay,
                maxAttempts: maxAttempts
            ).ConfigureAwait(false);

            return finalStatus;
        }
        catch (TimeoutException)
        {
            return lastStatus ?? new AuthStatus
            {
                Status = new StatusInfo
                {
                    Code = AuthInProgressCode,
                    Description = "Brak finalnego statusu przed upływem limitu czasu."
                }
            };
        }
    }

    private static void EnsureSuccess(AuthStatus status)
    {
        if (status.Status.Code != AuthSuccessCode)
        {
            string msg = $"Uwierzytelnienie nie powiodło się. Kod statusu: {status?.Status.Code}, opis: {status?.Status.Description}.";
            throw new Exception(msg);
        }
    }
}