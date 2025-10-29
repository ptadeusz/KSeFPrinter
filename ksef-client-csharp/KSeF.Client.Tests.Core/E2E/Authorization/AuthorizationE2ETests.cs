using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Token;

namespace KSeF.Client.Tests.Core.E2E.Authorization;

public class AuthorizationE2ETests : TestBase
{
    private const string OwnerRole = "owner";

    /// <summary>
    /// Uwierzytelnia klienta KSeF i sprawdza, czy zwrócony token dostępu jest poprawny
    /// </summary>
    [Theory]
    [InlineData(EncryptionMethodEnum.Rsa)]
    [InlineData(EncryptionMethodEnum.ECDsa)]
    public async Task AuthAsync_FullIntegrationFlow_ReturnsAccessToken(EncryptionMethodEnum encryptionMethodEnum)
    {
        // Arrange & Act
        Client.Core.Models.Authorization.AuthOperationStatusResponse authResult =
            await Utils.AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, default, encryptionMethodEnum);

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);

        PersonToken personToken = TokenService.MapFromJwt(authResult.AccessToken!.Token!);
        Assert.NotNull(personToken);
        Assert.Contains(OwnerRole, personToken.Roles, StringComparer.OrdinalIgnoreCase);
    }
}

