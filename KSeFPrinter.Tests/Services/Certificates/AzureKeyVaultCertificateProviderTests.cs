using FluentAssertions;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Certificates;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Services.Certificates;

/// <summary>
/// Testy dla AzureKeyVaultCertificateProvider
/// </summary>
public class AzureKeyVaultCertificateProviderTests
{
    private readonly AzureKeyVaultCertificateProvider _provider;

    public AzureKeyVaultCertificateProviderTests()
    {
        _provider = new AzureKeyVaultCertificateProvider(
            NullLogger<AzureKeyVaultCertificateProvider>.Instance
        );
    }

    [Fact]
    public void ValidateOptions_Should_Return_False_When_KeyVaultUrl_Is_Empty()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "",
            CertificateName = "test-cert"
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOptions_Should_Return_False_When_CertificateName_Is_Empty()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = ""
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOptions_Should_Return_False_When_KeyVaultUrl_Is_Invalid()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "not-a-valid-url",
            CertificateName = "test-cert"
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOptions_Should_Return_True_For_Valid_Options()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert"
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateOptions_Should_Return_False_When_ClientSecret_Missing_TenantId()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.ClientSecret,
            ClientId = "client-id",
            ClientSecret = "secret"
            // TenantId missing
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOptions_Should_Return_False_When_ClientSecret_Missing_ClientId()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.ClientSecret,
            TenantId = "tenant-id",
            ClientSecret = "secret"
            // ClientId missing
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOptions_Should_Return_False_When_ClientSecret_Missing_Secret()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.ClientSecret,
            TenantId = "tenant-id",
            ClientId = "client-id"
            // ClientSecret missing
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOptions_Should_Return_True_When_ClientSecret_Has_All_Required_Fields()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.ClientSecret,
            TenantId = "tenant-id",
            ClientId = "client-id",
            ClientSecret = "secret"
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateOptions_Should_Accept_Non_Azure_Vault_Url_With_Warning()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://custom-vault.example.com/",
            CertificateName = "test-cert"
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        // Powinno zaakceptować, ale z ostrzeżeniem w logach
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateOptions_Should_Accept_ManagedIdentity_Without_ClientId()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.ManagedIdentity
            // ClientId opcjonalne dla Managed Identity
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateOptions_Should_Accept_DefaultAzureCredential()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.DefaultAzureCredential
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateOptions_Should_Accept_EnvironmentCredential()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.EnvironmentCredential
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateOptions_Should_Accept_AzureCliCredential()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            AuthenticationType = AzureAuthenticationType.AzureCliCredential
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LoadCertificateAsync_Should_Throw_When_KeyVaultUrl_Is_Empty()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "",
            CertificateName = "test-cert"
        };

        // Act
        var act = async () => await _provider.LoadCertificateAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*KeyVaultUrl*");
    }

    [Fact]
    public async Task LoadCertificateAsync_Should_Throw_When_CertificateName_Is_Empty()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = ""
        };

        // Act
        var act = async () => await _provider.LoadCertificateAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*CertificateName*");
    }

    [Fact]
    public void ValidateOptions_Should_Handle_Null_Values_Gracefully()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = null,
            CertificateName = null
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateOptions_Should_Accept_Certificate_Version()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            CertificateVersion = "abc123def456"
        };

        // Act
        var result = _provider.ValidateOptions(options);

        // Assert
        result.Should().BeTrue();
    }
}
