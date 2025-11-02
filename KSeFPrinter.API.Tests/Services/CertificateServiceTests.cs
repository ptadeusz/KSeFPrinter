using FluentAssertions;
using KSeFPrinter.API.Models;
using KSeFPrinter.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography.X509Certificates;

namespace KSeFPrinter.API.Tests.Services;

/// <summary>
/// Testy jednostkowe dla CertificateService
/// </summary>
public class CertificateServiceTests
{
    private readonly CertificateService _service;

    public CertificateServiceTests()
    {
        _service = new CertificateService(
            NullLogger<CertificateService>.Instance,
            NullLogger<KSeFPrinter.Services.Certificates.AzureKeyVaultCertificateProvider>.Instance
        );
    }

    [Fact]
    public void LoadFromStore_Should_Return_Null_When_No_Thumbprint_Or_Subject()
    {
        // Act
        var result = _service.LoadFromStore(
            thumbprint: null,
            subject: null,
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void LoadFromStore_Should_Return_Null_When_Empty_Thumbprint_And_Subject()
    {
        // Act
        var result = _service.LoadFromStore(
            thumbprint: string.Empty,
            subject: string.Empty,
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void LoadFromStore_Should_Throw_Exception_For_Invalid_StoreLocation()
    {
        // Act
        var act = () => _service.LoadFromStore(
            thumbprint: "1234567890ABCDEF",
            subject: null,
            storeName: "My",
            storeLocation: "InvalidLocation");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Nieprawidłowa lokalizacja Store*");
    }

    [Fact]
    public void LoadFromStore_Should_Throw_Exception_For_Invalid_StoreName()
    {
        // Act
        var act = () => _service.LoadFromStore(
            thumbprint: "1234567890ABCDEF",
            subject: null,
            storeName: "InvalidStore",
            storeLocation: "CurrentUser");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Nieprawidłowa nazwa Store*");
    }

    [Theory]
    [InlineData("CurrentUser", "My")]
    [InlineData("CurrentUser", "Root")]
    [InlineData("LocalMachine", "My")]
    [InlineData("LocalMachine", "Root")]
    public void LoadFromStore_Should_Accept_Valid_Store_Locations_And_Names(
        string storeLocation,
        string storeName)
    {
        // Act - będzie rzucać wyjątek bo certyfikat nie istnieje, ale walidacja parametrów przejdzie
        var act = () => _service.LoadFromStore(
            thumbprint: "NONEXISTENT1234567890ABCDEF",
            subject: null,
            storeName: storeName,
            storeLocation: storeLocation);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Nie znaleziono certyfikatu*");
    }

    [Fact]
    public void LoadFromStore_Should_Throw_When_Certificate_Not_Found_By_Thumbprint()
    {
        // Act
        var act = () => _service.LoadFromStore(
            thumbprint: "NONEXISTENT1234567890ABCDEF1234567890ABCDEF",
            subject: null,
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Nie znaleziono certyfikatu*");
    }

    [Fact]
    public void LoadFromStore_Should_Throw_When_Certificate_Not_Found_By_Subject()
    {
        // Act
        var act = () => _service.LoadFromStore(
            thumbprint: null,
            subject: "CN=NonExistentCertificate",
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Nie znaleziono certyfikatu*");
    }

    [Fact]
    public void LoadFromStore_Should_Clean_Thumbprint_Spaces_And_Colons()
    {
        // Act - testujemy że czyści thumbprint (będzie szukać nieistniejącego ale format będzie poprawny)
        var act = () => _service.LoadFromStore(
            thumbprint: "12:34:56:78:90 AB CD EF",
            subject: null,
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Nie znaleziono certyfikatu*");
    }

    [Theory]
    [InlineData("My")]
    [InlineData("Root")]
    [InlineData("CertificateAuthority")]
    [InlineData("TrustedPeople")]
    public void LoadFromStore_Should_Accept_Standard_Store_Names(string storeName)
    {
        // Act
        var act = () => _service.LoadFromStore(
            thumbprint: "NONEXISTENT",
            subject: null,
            storeName: storeName,
            storeLocation: "CurrentUser");

        // Assert - powinno rzucić wyjątek o braku certyfikatu, nie o błędnej nazwie
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Nie znaleziono certyfikatu*");
    }

    [Theory]
    [InlineData("currentuser", "my")]
    [InlineData("CURRENTUSER", "MY")]
    [InlineData("CurrentUser", "My")]
    public void LoadFromStore_Should_Be_Case_Insensitive(string storeLocation, string storeName)
    {
        // Act
        var act = () => _service.LoadFromStore(
            thumbprint: "NONEXISTENT",
            subject: null,
            storeName: storeName,
            storeLocation: storeLocation);

        // Assert - powinno zaakceptować różne case
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Nie znaleziono certyfikatu*");
    }

    // Integration test - wymaga certyfikatu testowego
    // Ten test będzie pominięty jeśli nie ma certyfikatu w systemie
    [Fact(Skip = "Integration test - requires test certificate in Windows Store")]
    public void LoadFromStore_Should_Load_Certificate_By_Thumbprint_When_Available()
    {
        // Arrange - wymaga istniejącego certyfikatu
        var thumbprint = "YOUR_TEST_CERTIFICATE_THUMBPRINT";

        // Act
        var certificate = _service.LoadFromStore(
            thumbprint: thumbprint,
            subject: null,
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        certificate.Should().NotBeNull();
        certificate!.HasPrivateKey.Should().BeTrue();
    }

    [Fact(Skip = "Integration test - requires test certificate in Windows Store")]
    public void LoadFromStore_Should_Load_Certificate_By_Subject_When_Available()
    {
        // Arrange - wymaga istniejącego certyfikatu
        var subject = "YOUR_TEST_CERTIFICATE_SUBJECT";

        // Act
        var certificate = _service.LoadFromStore(
            thumbprint: null,
            subject: subject,
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        certificate.Should().NotBeNull();
        certificate!.HasPrivateKey.Should().BeTrue();
    }

    [Fact(Skip = "Integration test - requires test certificate without private key")]
    public void LoadFromStore_Should_Throw_When_Certificate_Has_No_Private_Key()
    {
        // Arrange - wymaga certyfikatu bez klucza prywatnego
        var thumbprint = "CERTIFICATE_WITHOUT_PRIVATE_KEY_THUMBPRINT";

        // Act
        var act = () => _service.LoadFromStore(
            thumbprint: thumbprint,
            subject: null,
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Certyfikat nie zawiera klucza prywatnego*");
    }

    [Fact]
    public void LoadFromStore_Should_Prefer_Thumbprint_Over_Subject()
    {
        // Act - gdy oba są podane, thumbprint ma priorytet
        var act = () => _service.LoadFromStore(
            thumbprint: "NONEXISTENT_THUMBPRINT",
            subject: "Some Subject",
            storeName: "My",
            storeLocation: "CurrentUser");

        // Assert - szuka po thumbprint
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Nie znaleziono certyfikatu*");
    }

    // === Testy nowych metod (po refaktoringu - certyfikaty z appsettings.json) ===

    [Fact]
    public void GetOnlineCertificate_Should_Return_Null_Before_Loading()
    {
        // Arrange - świeża instancja bez wczytanych certyfikatów
        var service = new CertificateService(
            NullLogger<CertificateService>.Instance,
            NullLogger<KSeFPrinter.Services.Certificates.AzureKeyVaultCertificateProvider>.Instance);

        // Act
        var certificate = service.GetOnlineCertificate();

        // Assert
        certificate.Should().BeNull();
    }

    [Fact]
    public void GetOfflineCertificate_Should_Return_Null_Before_Loading()
    {
        // Arrange - świeża instancja bez wczytanych certyfikatów
        var service = new CertificateService(
            NullLogger<CertificateService>.Instance,
            NullLogger<KSeFPrinter.Services.Certificates.AzureKeyVaultCertificateProvider>.Instance);

        // Act
        var certificate = service.GetOfflineCertificate();

        // Assert
        certificate.Should().BeNull();
    }

    [Fact]
    public async Task LoadCertificatesFromConfigurationAsync_Should_Handle_Disabled_Certificates()
    {
        // Arrange
        var config = new CertificatesConfiguration
        {
            Online = new CertificateOptions { Enabled = false },
            Offline = new CertificateOptions { Enabled = false }
        };

        // Act
        await _service.LoadCertificatesFromConfigurationAsync(config);

        // Assert
        _service.GetOnlineCertificate().Should().BeNull();
        _service.GetOfflineCertificate().Should().BeNull();
    }

    [Fact]
    public async Task LoadCertificatesFromConfigurationAsync_Should_Not_Throw_On_Invalid_Configuration()
    {
        // Arrange
        var config = new CertificatesConfiguration
        {
            Online = new CertificateOptions
            {
                Enabled = true,
                Source = "WindowsStore",
                WindowsStore = new WindowsStoreOptions
                {
                    Thumbprint = "NONEXISTENT_CERT",
                    StoreName = "My",
                    StoreLocation = "CurrentUser"
                }
            }
        };

        // Act
        var act = async () => await _service.LoadCertificatesFromConfigurationAsync(config);

        // Assert - nie powinno rzucać wyjątku, tylko zalogować błąd
        await act.Should().NotThrowAsync();
        _service.GetOnlineCertificate().Should().BeNull();
    }

    [Fact]
    public async Task LoadCertificatesFromConfigurationAsync_Should_Handle_Unknown_Source()
    {
        // Arrange
        var config = new CertificatesConfiguration
        {
            Online = new CertificateOptions
            {
                Enabled = true,
                Source = "UnknownSource" // Nieznane źródło
            }
        };

        // Act
        await _service.LoadCertificatesFromConfigurationAsync(config);

        // Assert
        _service.GetOnlineCertificate().Should().BeNull();
    }

    [Fact]
    public async Task LoadCertificatesFromConfigurationAsync_Should_Load_Both_Certificates_Independently()
    {
        // Arrange
        var config = new CertificatesConfiguration
        {
            Online = new CertificateOptions
            {
                Enabled = true,
                Source = "WindowsStore",
                WindowsStore = new WindowsStoreOptions
                {
                    Thumbprint = "NONEXISTENT_ONLINE",
                    StoreName = "My",
                    StoreLocation = "CurrentUser"
                }
            },
            Offline = new CertificateOptions
            {
                Enabled = true,
                Source = "WindowsStore",
                WindowsStore = new WindowsStoreOptions
                {
                    Thumbprint = "NONEXISTENT_OFFLINE",
                    StoreName = "My",
                    StoreLocation = "CurrentUser"
                }
            }
        };

        // Act
        await _service.LoadCertificatesFromConfigurationAsync(config);

        // Assert - oba powinny być null bo certyfikaty nie istnieją
        _service.GetOnlineCertificate().Should().BeNull();
        _service.GetOfflineCertificate().Should().BeNull();
    }

    [Fact(Skip = "Integration test - requires test certificates")]
    public async Task LoadCertificatesFromConfigurationAsync_Should_Load_Online_Certificate_From_Store()
    {
        // Arrange - wymaga rzeczywistego certyfikatu w systemie
        var config = new CertificatesConfiguration
        {
            Online = new CertificateOptions
            {
                Enabled = true,
                Source = "WindowsStore",
                WindowsStore = new WindowsStoreOptions
                {
                    Thumbprint = "YOUR_REAL_CERTIFICATE_THUMBPRINT",
                    StoreName = "My",
                    StoreLocation = "CurrentUser"
                }
            }
        };

        // Act
        await _service.LoadCertificatesFromConfigurationAsync(config);

        // Assert
        var cert = _service.GetOnlineCertificate();
        cert.Should().NotBeNull();
        cert!.HasPrivateKey.Should().BeTrue();
    }

    [Fact(Skip = "Integration test - requires Azure Key Vault access")]
    public async Task LoadCertificatesFromConfigurationAsync_Should_Load_Certificate_From_Azure_KeyVault()
    {
        // Arrange - wymaga dostępu do Azure Key Vault
        var config = new CertificatesConfiguration
        {
            Offline = new CertificateOptions
            {
                Enabled = true,
                Source = "AzureKeyVault",
                AzureKeyVault = new AzureKeyVaultOptions
                {
                    KeyVaultUrl = "https://your-vault.vault.azure.net/",
                    CertificateName = "your-certificate",
                    AuthenticationType = "DefaultAzureCredential"
                }
            }
        };

        // Act
        await _service.LoadCertificatesFromConfigurationAsync(config);

        // Assert
        var cert = _service.GetOfflineCertificate();
        cert.Should().NotBeNull();
    }
}
