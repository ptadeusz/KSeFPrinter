using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests;

public class Authorization : KsefIntegrationTestBase
{
    [Fact]
    public async Task RefreshToken_Receive_ShouldReturnTokenDifferentThanInitialToken()
    {
        // Arrange & Act
        var authInfo = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, MiscellaneousUtils.GetRandomNip());
        var refreshTokenResult = await KsefClient.RefreshAccessTokenAsync(authInfo.RefreshToken.Token);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.NotNull(refreshTokenResult);
            Assert.NotEqual(authInfo.RefreshToken.Token, refreshTokenResult.AccessToken.Token);
        });
    }

    [Fact]
    /// <summary>
    /// Uwierzytelnia przy użyciu certyfikatu i NIP-u kontekstu, sesja szyfrowana, rola: właściciel.
    /// </summary>
    public async Task AuthAsync_FullIntegrationFlow_ReturnsAccessToken()
    {
        // Arrange & Act
        var authResult = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, MiscellaneousUtils.GetRandomNip());

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }

    [Fact]
    /// <summary>
    /// Uwierzytelnia przy użyciu certyfikatu i NIP-u kontekstu, sesja szyfrowana, rola: właściciel.
    /// </summary>
    public async Task AuthAsync_FullIntegrationFlowWithKSeFTokenRSA_ReturnsAccessToken()
    {
        // Arrange
        // Uwierzytelnij
        var authInfo = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, MiscellaneousUtils.GetRandomNip());
        // Najpierw trzeba uwierzytelnić jako właściciel, aby otrzymać token KSeF
        var permissions = new KsefTokenPermissionType[]
        {
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        };

        await Task.Delay(SleepTime);
        var ownerToken = await KsefClient.GenerateKsefTokenAsync(new KsefTokenRequest() { Description = $"Wystawianie i przeglądanie faktur", Permissions = permissions }, authInfo.AccessToken.Token);

        await Task.Delay(SleepTime);
        var ksefTokenStatus = await KsefClient.GetKsefTokenAsync(ownerToken.ReferenceNumber, authInfo.AccessToken.Token);

        await Task.Delay(SleepTime);
        var authCoordinator = new AuthCoordinator(KsefClient) as IAuthCoordinator;
        await Task.Delay(SleepTime);

        var contextType = ContextIdentifierType.Nip;
        var contextValue = ksefTokenStatus.ContextIdentifier.Value;

        AuthorizationPolicy? authorizationPolicy = null;


        // Act
        var result = await authCoordinator.AuthKsefTokenAsync(
            contextType,
            contextValue,
            ownerToken.Token,
            CryptographyService,
            EncryptionMethodEnum.Rsa,
            authorizationPolicy,
            default
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }

    [Theory]
    [InlineData(EncryptionMethodEnum.Rsa)]
    public async Task KsefClientAuthorization_AuthCoordinatorService_Positive(EncryptionMethodEnum encryptionMethod)
    {
        // Arrange
        var authCoordinatorService = new AuthCoordinator(KsefClient) as IAuthCoordinator;
        var testNip = MiscellaneousUtils.GetRandomNip();
        var contextIdentifierType = ContextIdentifierType.Nip;
        var authInfo = await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, testNip, contextIdentifierType);
        var permissions = new KsefTokenPermissionType[]
        {
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        };
        var ownerToken = await KsefClient.GenerateKsefTokenAsync(new KsefTokenRequest()
        {
            Description = $"Wystawianie i przeglądanie faktur",
            Permissions = permissions
        },
            authInfo.AccessToken.Token);

        // Act
        var authResult = await authCoordinatorService.AuthKsefTokenAsync(ContextIdentifierType.Nip,
            testNip,
            ownerToken.Token,
            CryptographyService,
            encryptionMethod);

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);
        Assert.NotNull(authResult.AccessToken.Token);
        Assert.NotNull(authResult.RefreshToken);
        Assert.NotNull(authResult.RefreshToken.Token);
    }
}