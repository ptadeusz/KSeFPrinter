using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.Sessions.ActiveSessions;
using KSeF.Client.Tests.Utils;


namespace KSeF.Client.Tests.Core.E2E.Authorization.Sessions;

public class SessionE2ETests : TestBase
{
    private readonly string accessToken;
    private readonly string refreshToken;
    private readonly string nip;

    private const string ExpectedErrorMessage = "21304: Brak uwierzytelnienia. - Nieprawidłowy token.";
    public SessionE2ETests()
    {
        nip = MiscellaneousUtils.GetRandomNip();

        Client.Core.Models.Authorization.AuthOperationStatusResponse auth = AuthenticationUtils
            .AuthenticateAsync(KsefClient, SignatureService, nip)
            .GetAwaiter()
            .GetResult();

        accessToken = auth.AccessToken.Token;
        refreshToken = auth.RefreshToken.Token;
    }

    /// <summary>
    /// Pobiera listę aktywnych sesji i weryfikuje, że istnieje co najmniej jedna,
    /// a wśród nich bieżąca (IsCurrent = true).
    /// </summary>
    [Fact]
    public async Task GetActiveSessionsAsync_ListActiveSessions_ReturnsAtLeastOneWithCurrent()
    {
        // Arrange
        const int pageSize = 20;
        string? continuationToken = null;
        List<AuthenticationListItem> all = new List<AuthenticationListItem>();

        // Act
        do
        {
            AuthenticationListResponse page = await KsefClient.GetActiveSessions(accessToken, pageSize, continuationToken, CancellationToken.None);
            continuationToken = page.ContinuationToken;
            if (page.Items != null)
                all.AddRange(page.Items);
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        // Assert
        Assert.NotNull(all);
        Assert.NotEmpty(all);
        Assert.Contains(all, x => x.IsCurrent);
    }

    /// <summary>
    /// Unieważnia bieżącą sesję (po refresh tokenie) i weryfikuje, że odświeżenie tokenu
    /// kończy się błędem 21304: Brak uwierzytelnienia. - Nieprawidłowy token.
    /// </summary>
    [Fact]
    public async Task RevokeCurrentSessionAsync_RevokeByRefreshToken_RefreshFailsWth21304Code()
    {
        // Arrange
        string tokenToRevoke = refreshToken;
        
        // Act
        await KsefClient.RevokeCurrentSessionAsync(tokenToRevoke, CancellationToken.None);

        // Assert
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            KsefClient.RefreshAccessTokenAsync(tokenToRevoke, CancellationToken.None));
        Assert.Equal(ExpectedErrorMessage, ex.Message);
    }

    /// <summary>
    /// Tworzy drugą sesję w tym samym kontekście, unieważnia ją po numerze referencyjnym
    /// i sprawdza, że jej refresh token nie działa kończąc błędem 21304: Brak uwierzytelnienia. - Nieprawidłowy token.
    /// </summary>
    [Fact]
    public async Task RevokeSessionAsync_RevokeByReferenceNumber_TargetRefreshFailsWith401()
    {
        // Arrange
        Client.Core.Models.Authorization.AuthOperationStatusResponse secondAuth = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nip);
        string secondAccessToken = secondAuth.AccessToken.Token;
        string secondRefreshToken = secondAuth.RefreshToken.Token;

        AuthenticationListResponse list = await KsefClient.GetActiveSessions(secondAccessToken, 20, null, CancellationToken.None);
        string? secondSessionRef = list.Items?.FirstOrDefault(i => i.IsCurrent)?.ReferenceNumber;

        Assert.False(string.IsNullOrWhiteSpace(secondSessionRef));

        // Act
        await KsefClient.RevokeSessionAsync(secondSessionRef!, accessToken, CancellationToken.None);

        // Assert
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(() =>
            KsefClient.RefreshAccessTokenAsync(secondRefreshToken, CancellationToken.None));
        Assert.Equal(ExpectedErrorMessage, ex.Message);
    }
}