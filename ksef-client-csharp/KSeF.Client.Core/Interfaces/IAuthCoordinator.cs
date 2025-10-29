using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces
{
    /// <summary>
    /// Koordynuje proces uwierzytelniania w systemie KSeF API 2.0.
    /// Odpowiada za uzyskanie i odświeżanie tokenów dostępowych (accessToken/refreshToken).
    /// </summary>
    public interface IAuthCoordinator
    {
        /// <summary>
        /// Rozpoczyna proces uwierzytelniania w KSeF przy użyciu podpisu XAdES.
        /// 
        /// Kroki:
        /// 1. Pobiera <c>AuthChallenge</c> z systemu KSeF.
        /// 2. Buduje dokument <c>AuthTokenRequest</c>.
        /// 3. Podpisuje dokument XML przekazanym delegatem <paramref name="xmlSigner"/>.
        /// 4. Wysyła żądanie do KSeF i czeka na zakończenie procesu uwierzytelnienia.
        /// 5. Zwraca wynik zawierający <c>accessToken</c> i <c>refreshToken</c>.
        /// </summary>
        /// <param name="contextIdentifierType">Typ identyfikatora kontekstu (np. NIP, InternalId, VAT UE).</param>
        /// <param name="contextIdentifierValue">Wartość identyfikatora kontekstu.</param>
        /// <param name="identifierType">Sposób identyfikacji podmiotu uwierzytelniającego (SubjectIdentifierTypeEnum).</param>
        /// <param name="xmlSigner">
        /// Funkcja asynchroniczna odpowiedzialna za podpisanie XML w formacie XAdES.
        /// Przyjmuje niepodpisany XML, zwraca podpisany XML.
        /// </param>
        /// <param name="authorizationPolicy">Polityka walidacji autoryzacji (opcjonalna).</param>
        /// <param name="ct">Token anulowania.</param>
        /// <param name="verifyCertificateChain">
        /// Czy weryfikować łańcuch certyfikatu podczas uwierzytelniania.
        /// </param>
        /// <returns>
        /// Obiekt <see cref="AuthOperationStatusResponse"/> zawierający dane access/refresh tokenów 
        /// oraz informacje o statusie operacji uwierzytelniania.
        /// </returns>
        Task<AuthOperationStatusResponse> AuthAsync(
            ContextIdentifierType contextIdentifierType,
            string contextIdentifierValue,
            SubjectIdentifierTypeEnum identifierType,
            Func<string, Task<string>> xmlSigner,
            AuthorizationPolicy authorizationPolicy = default,
            CancellationToken ct = default,
            bool verifyCertificateChain = false);

        /// <summary>
        /// Odświeża <c>accessToken</c> przy użyciu wcześniej uzyskanego <c>refreshToken</c>.
        /// </summary>
        /// <param name="refreshToken">Ważny refresh token.</param>
        /// <param name="ct">Token anulowania.</param>
        /// <returns>
        /// Nowy obiekt <see cref="TokenInfo"/> zawierający odświeżony accessToken i refreshToken.
        /// </returns>
        Task<TokenInfo> RefreshAccessTokenAsync(
            string refreshToken,
            CancellationToken ct = default);

        /// <summary>
        /// Rozpoczyna proces uwierzytelniania w KSeF przy użyciu tokena KSeF.
        /// 
        /// Kroki:
        /// 1. Pobiera <c>AuthChallenge</c> z systemu KSeF.
        /// 2. Łączy token KSeF z timestampem z challenge.
        /// 3. Szyfruje ciąg przy użyciu <paramref name="cryptographyService"/> i wybranej metody <paramref name="encryptionMethod"/>.
        /// 4. Wysyła zaszyfrowany token do KSeF.
        /// 5. Zwraca wynik uwierzytelnienia (access/refresh tokeny).
        /// </summary>
        /// <param name="contextIdentifierType">Typ identyfikatora kontekstu (np. NIP).</param>
        /// <param name="contextIdentifierValue">Wartość identyfikatora kontekstu.</param>
        /// <param name="tokenKsef">Token KSeF (sekret uwierzytelniający).</param>
        /// <param name="cryptographyService">Usługa odpowiedzialna za szyfrowanie RSA/ECDSA.</param>
        /// <param name="encryptionMethod">Metoda szyfrowania (domyślnie ECDsa).</param>
        /// <param name="authorizationPolicy">Polityka walidacji autoryzacji (opcjonalna).</param>
        /// <param name="ct">Token anulowania.</param>
        /// <returns>
        /// Obiekt <see cref="AuthOperationStatusResponse"/> zawierający dane access/refresh tokenów 
        /// oraz status operacji.
        /// </returns>
        Task<AuthOperationStatusResponse> AuthKsefTokenAsync(
            ContextIdentifierType contextIdentifierType,
            string contextIdentifierValue,
            string tokenKsef,
            ICryptographyService cryptographyService,
            EncryptionMethodEnum encryptionMethod = EncryptionMethodEnum.ECDsa,
            AuthorizationPolicy authorizationPolicy = default,
            CancellationToken ct = default);
    }
}
