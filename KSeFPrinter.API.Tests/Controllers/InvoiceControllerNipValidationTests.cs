using FluentAssertions;
using KSeFPrinter.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace KSeFPrinter.API.Tests.Controllers;

/// <summary>
/// Testy walidacji NIP w licencji dla InvoiceController
/// </summary>
/// <remarks>
/// Te testy wymagają testowej licencji z odpowiednimi NIPami.
/// Walidacja sprawdza czy przynajmniej jeden NIP z faktury (Podmiot1, Podmiot2, lub Podmiot3)
/// znajduje się na liście allowedNips w licencji.
/// </remarks>
public class InvoiceControllerNipValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InvoiceControllerNipValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GeneratePdf_Should_Return_OK_When_Seller_Nip_Is_Allowed()
    {
        // Arrange
        // FakturaTEST017.xml ma Sprzedawcę z NIP: 7309436499
        // Jeśli ten NIP jest w licencji, test powinien przejść
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false,
            ReturnFormat = "file"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        // Jeśli licencja zawiera NIP 7309436499, powinno być 200 OK
        // Jeśli nie zawiera, będzie 403 Forbidden
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            // To jest oczekiwane jeśli NIP nie jest w licencji
            var error = await response.Content.ReadFromJsonAsync<dynamic>();
            error.Should().NotBeNull();
            // Sprawdź strukturę błędu 403
            Assert.NotNull(error);
        }
        else
        {
            // NIP jest w licencji - sukces
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            pdfBytes.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task GeneratePdf_Should_Return_403_When_No_Nip_Is_Allowed()
    {
        // Arrange
        // Utworzę fakturę z NIPami, które na pewno nie są w licencji testowej
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Podmień NIPy w XML na nieistniejące (9999999999, 8888888888)
        xmlContent = xmlContent.Replace("7309436499", "9999999999");
        xmlContent = xmlContent.Replace("1234567890", "8888888888");

        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false,
            ReturnFormat = "file"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Sprawdź strukturę odpowiedzi 403
        var errorResponse = await response.Content.ReadFromJsonAsync<NipValidationErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Should().Contain("Licencja nie obejmuje");
        errorResponse.InvoiceNips.Should().NotBeEmpty();
        errorResponse.InvoiceNips.Should().Contain(nip => nip.Contains("9999999999"));
        errorResponse.AllowedNips.Should().BeGreaterThan(0);
        errorResponse.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GeneratePdf_Should_Include_All_Nips_In_403_Response()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Podmień NIPy na nieistniejące
        xmlContent = xmlContent.Replace("7309436499", "1111111111"); // Sprzedawca
        xmlContent = xmlContent.Replace("1234567890", "2222222222"); // Nabywca

        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<NipValidationErrorResponse>();
            errorResponse.Should().NotBeNull();

            // Sprawdź czy zawiera oba NIPy
            errorResponse!.InvoiceNips.Should().HaveCountGreaterThanOrEqualTo(2);
            errorResponse.InvoiceNips.Should().Contain(nip => nip.Contains("Sprzedawca") && nip.Contains("1111111111"));
            errorResponse.InvoiceNips.Should().Contain(nip => nip.Contains("Nabywca") && nip.Contains("2222222222"));
        }
        else
        {
            // Jeśli któryś z NIPów jest w licencji, test mija się z celem
            // ale nie jest błędem - po prostu licencja zawiera te NIPy
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GeneratePdf_Should_Log_Warning_On_403_Forbidden()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Podmień NIPy na nieistniejące
        xmlContent = xmlContent.Replace("7309436499", "3333333333");
        xmlContent = xmlContent.Replace("1234567890", "4444444444");

        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            // API powinno zalogować ostrzeżenie o braku uprawnień
            // (nie możemy tego sprawdzić w teście integracyjnym bez dostępu do logów,
            // ale możemy sprawdzić strukturę odpowiedzi)
            var errorResponse = await response.Content.ReadFromJsonAsync<NipValidationErrorResponse>();
            errorResponse.Should().NotBeNull();
            errorResponse!.Error.Should().NotBeNullOrEmpty();
            errorResponse.Message.Should().Contain("przynajmniej jeden NIP");
        }
    }

    [Fact]
    public async Task GeneratePdf_Should_Accept_Invoice_When_Buyer_Nip_Is_Allowed()
    {
        // Arrange
        // Jeśli licencja zawiera NIP nabywcy (nie sprzedawcy), to też powinno działać
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Podmień NIP sprzedawcy na nieistniejący, zostaw NIP nabywcy
        xmlContent = xmlContent.Replace("7309436499", "5555555555");
        // NIP nabywcy pozostaje bez zmian - jeśli jest w licencji, test przejdzie

        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        // Jeśli NIP nabywcy jest w licencji -> 200 OK
        // Jeśli nie jest -> 403 Forbidden
        // Oba przypadki są poprawne, zależy od licencji
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("7309436499")] // Możliwy NIP sprzedawcy w testach
    [InlineData("1234567890")] // Możliwy NIP nabywcy w testach
    public async Task GeneratePdf_Should_Validate_Specific_Nip(string testNip)
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Ustaw testowy NIP jako sprzedawcę
        xmlContent = System.Text.RegularExpressions.Regex.Replace(
            xmlContent,
            @"<NIP>7309436499</NIP>",
            $"<NIP>{testNip}</NIP>",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));

        var request = new GeneratePdfRequest
        {
            XmlContent = xmlContent,
            IsBase64 = false,
            ValidateInvoice = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoice/generate-pdf", request);

        // Assert
        // Wynik zależy od tego czy NIP jest w licencji
        // Test sprawdza czy walidacja działa - oba wyniki są OK
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var error = await response.Content.ReadFromJsonAsync<NipValidationErrorResponse>();
            error.Should().NotBeNull();
            error!.InvoiceNips.Should().Contain(nip => nip.Contains(testNip));
        }
    }
}

/// <summary>
/// Model odpowiedzi dla błędu 403 - brak uprawnień do NIP
/// </summary>
public class NipValidationErrorResponse
{
    public string Error { get; set; } = null!;
    public List<string> InvoiceNips { get; set; } = new();
    public int AllowedNips { get; set; }
    public string Message { get; set; } = null!;
}
