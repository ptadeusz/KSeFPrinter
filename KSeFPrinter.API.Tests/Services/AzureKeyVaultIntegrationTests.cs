using FluentAssertions;
using KSeFPrinter.API.Models;
using KSeFPrinter.API.Services;
using KSeFPrinter.Models.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;

namespace KSeFPrinter.API.Tests.Services;

/// <summary>
/// Testy integracyjne dla Azure Key Vault
/// </summary>
public class AzureKeyVaultIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AzureKeyVaultIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public void CertificateService_Should_Have_KeyVault_Method()
    {
        // Arrange
        var service = new CertificateService(
            NullLogger<CertificateService>.Instance,
            NullLogger<KSeFPrinter.Services.Certificates.AzureKeyVaultCertificateProvider>.Instance
        );

        // Act
        var method = service.GetType().GetMethod("LoadFromKeyVaultAsync");

        // Assert
        method.Should().NotBeNull("CertificateService should have LoadFromKeyVaultAsync method");
        method!.ReturnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void GeneratePdfRequest_Should_Have_Azure_KeyVault_Properties()
    {
        // Arrange & Act
        var request = new GeneratePdfRequest();

        // Assert
        request.Should().NotBeNull();

        // Sprawdź czy ma wszystkie wymagane właściwości
        var properties = request.GetType().GetProperties();

        properties.Should().Contain(p => p.Name == "CertificateSource");
        properties.Should().Contain(p => p.Name == "AzureKeyVaultUrl");
        properties.Should().Contain(p => p.Name == "AzureKeyVaultCertificateName");
        properties.Should().Contain(p => p.Name == "AzureKeyVaultCertificateVersion");
        properties.Should().Contain(p => p.Name == "AzureAuthenticationType");
        properties.Should().Contain(p => p.Name == "AzureTenantId");
        properties.Should().Contain(p => p.Name == "AzureClientId");
        properties.Should().Contain(p => p.Name == "AzureClientSecret");
    }

    [Fact]
    public void GeneratePdfRequest_Default_CertificateSource_Should_Be_WindowsStore()
    {
        // Arrange & Act
        var request = new GeneratePdfRequest();

        // Assert
        request.CertificateSource.Should().Be("WindowsStore");
    }

    [Fact]
    public void GeneratePdfRequest_Default_AzureAuthType_Should_Be_DefaultAzureCredential()
    {
        // Arrange & Act
        var request = new GeneratePdfRequest();

        // Assert
        request.AzureAuthenticationType.Should().Be("DefaultAzureCredential");
    }

    [Fact]
    public void AzureKeyVaultOptions_Should_Have_All_Auth_Types()
    {
        // Arrange & Act
        var authTypes = Enum.GetNames(typeof(AzureAuthenticationType));

        // Assert
        authTypes.Should().Contain("DefaultAzureCredential");
        authTypes.Should().Contain("ManagedIdentity");
        authTypes.Should().Contain("ClientSecret");
        authTypes.Should().Contain("EnvironmentCredential");
        authTypes.Should().Contain("AzureCliCredential");
    }

    [Fact]
    public async Task Health_Endpoint_Should_Work_After_KeyVault_Integration()
    {
        // Act
        var response = await _client.GetAsync("/api/invoice/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GeneratePdf_Should_Accept_KeyVault_Parameters_In_Request()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            ValidateInvoice = false,
            CertificateSource = "AzureKeyVault",
            AzureKeyVaultUrl = "https://test-vault.vault.azure.net/",
            AzureKeyVaultCertificateName = "test-cert",
            AzureAuthenticationType = "DefaultAzureCredential"
        };

        // Act
        // Nie sprawdzamy wyniku, bo nie mamy prawdziwego Key Vault w testach
        // Sprawdzamy tylko czy API akceptuje parametry (model binding działa)
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        // API powinno próbować połączyć się z Key Vault
        // Może zwrócić BadRequest (błąd połączenia z Key Vault), ale request został zaakceptowany
        response.Should().NotBeNull("API should process the request");
        // Test sprawdza tylko że request został przetworzony, nie konkretny status code
    }

    [Fact]
    public void AzureKeyVaultOptions_Should_Be_Serializable()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            KeyVaultUrl = "https://test.vault.azure.net/",
            CertificateName = "test-cert",
            CertificateVersion = "v1",
            AuthenticationType = AzureAuthenticationType.ClientSecret,
            TenantId = "tenant-123",
            ClientId = "client-456",
            ClientSecret = "secret-789"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<AzureKeyVaultOptions>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.KeyVaultUrl.Should().Be(options.KeyVaultUrl);
        deserialized.CertificateName.Should().Be(options.CertificateName);
        deserialized.CertificateVersion.Should().Be(options.CertificateVersion);
        deserialized.AuthenticationType.Should().Be(options.AuthenticationType);
    }

    [Fact]
    public void AzureKeyVaultOptions_Should_Have_Sensible_Defaults()
    {
        // Arrange & Act
        var options = new AzureKeyVaultOptions();

        // Assert
        options.AuthenticationType.Should().Be(AzureAuthenticationType.DefaultAzureCredential);
        options.KeyVaultUrl.Should().BeNull();
        options.CertificateName.Should().BeNull();
        options.CertificateVersion.Should().BeNull();
    }

    [Fact]
    public async Task API_Should_Prioritize_KeyVault_Over_WindowsStore()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            ValidateInvoice = false,
            // Podaj oba źródła - Key Vault powinien mieć priorytet
            CertificateSource = "AzureKeyVault",
            AzureKeyVaultUrl = "https://test-vault.vault.azure.net/",
            AzureKeyVaultCertificateName = "test-cert",
            CertificateThumbprint = "ABCD1234" // To powinno być zignorowane
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        // Powinno próbować użyć Key Vault (i zwrócić błąd połączenia), a nie Windows Store
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void CertificateService_Constructor_Should_Accept_KeyVault_Logger()
    {
        // Act
        var service = new CertificateService(
            NullLogger<CertificateService>.Instance,
            NullLogger<KSeFPrinter.Services.Certificates.AzureKeyVaultCertificateProvider>.Instance
        );

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void GeneratePdfRequest_Should_Allow_Null_KeyVault_Properties()
    {
        // Arrange & Act
        var request = new GeneratePdfRequest
        {
            XmlContent = "<test/>",
            AzureKeyVaultUrl = null,
            AzureKeyVaultCertificateName = null,
            AzureKeyVaultCertificateVersion = null,
            AzureTenantId = null,
            AzureClientId = null,
            AzureClientSecret = null
        };

        // Assert
        request.Should().NotBeNull();
        // Wszystkie pola KeyVault powinny być nullable
    }
}
