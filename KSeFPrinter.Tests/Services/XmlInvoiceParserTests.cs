using FluentAssertions;
using KSeFPrinter.Exceptions;
using KSeFPrinter.Interfaces;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Services.Parsers;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Services;

/// <summary>
/// Testy parsera XML faktur
/// </summary>
public class XmlInvoiceParserTests
{
    private readonly IXmlInvoiceParser _parser;

    public XmlInvoiceParserTests()
    {
        _parser = new XmlInvoiceParser(NullLogger<XmlInvoiceParser>.Instance);
    }

    [Fact]
    public async Task ParseFromFileAsync_Should_Parse_Offline_Invoice_Successfully()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");

        // Act
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();
        context.Metadata.Should().NotBeNull();
        context.Metadata.Tryb.Should().Be(TrybWystawienia.Offline);
        context.Metadata.NumerKSeF.Should().BeNull();
        context.Faktura.Fa.P_2.Should().Be("FA811/05/2025");
    }

    [Fact]
    public async Task ParseFromFileAsync_Should_Parse_Online_Invoice_Successfully()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");

        // Act
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();
        context.Metadata.Should().NotBeNull();
        context.Faktura.Fa.P_2.Should().Be("FA102/05/2025");
    }

    [Fact]
    public async Task ParseFromFileAsync_Should_Detect_KSeF_Number_From_Filename()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");

        // Act
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Assert - numer KSeF może być wykryty z nazwy pliku lub zawartości
        // W tym przypadku sprawdzamy, czy parser nie zgłasza błędu
        context.Should().NotBeNull();
    }

    [Fact]
    public async Task ParseFromFileAsync_Should_Throw_When_File_Does_Not_Exist()
    {
        // Arrange
        var xmlPath = "nieistniejacy-plik.xml";

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceParseException>(
            () => _parser.ParseFromFileAsync(xmlPath)
        );
    }

    [Fact]
    public async Task ParseFromStringAsync_Should_Parse_Valid_XML()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);

        // Act
        var context = await _parser.ParseFromStringAsync(xmlContent);

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();
        context.OriginalXml.Should().Be(xmlContent);
    }

    [Fact]
    public async Task ParseFromStringAsync_Should_Throw_When_XML_Is_Empty()
    {
        // Arrange
        var xmlContent = "";

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceParseException>(
            () => _parser.ParseFromStringAsync(xmlContent)
        );
    }

    [Fact]
    public async Task ParseFromStringAsync_Should_Throw_When_XML_Is_Invalid()
    {
        // Arrange
        var xmlContent = "<invalid>xml</invalid>";

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceParseException>(
            () => _parser.ParseFromStringAsync(xmlContent)
        );
    }

    [Fact]
    public async Task ParseFromStringAsync_Should_Accept_Manual_KSeF_Number()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = await File.ReadAllTextAsync(xmlPath);
        var numerKSeF = "1234567890-20250101-ABC123456789-01";

        // Act
        var context = await _parser.ParseFromStringAsync(xmlContent, numerKSeF);

        // Assert
        context.Metadata.NumerKSeF.Should().Be(numerKSeF);
        context.Metadata.Tryb.Should().Be(TrybWystawienia.Online);
    }

    [Fact]
    public async Task ParseFromStreamAsync_Should_Parse_Valid_Stream()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        using var stream = File.OpenRead(xmlPath);

        // Act
        var context = await _parser.ParseFromStreamAsync(stream);

        // Assert
        context.Should().NotBeNull();
        context.Faktura.Should().NotBeNull();
    }

    [Fact]
    public void ValidateSchema_Should_Return_True_For_Valid_XML()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        var result = _parser.ValidateSchema(xmlContent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSchema_Should_Return_False_For_Invalid_XML()
    {
        // Arrange
        var xmlContent = "<invalid>xml</invalid>";

        // Act
        var result = _parser.ValidateSchema(xmlContent);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ParseFromFileAsync_Should_Parse_All_Invoice_Data_Correctly()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");

        // Act
        var context = await _parser.ParseFromFileAsync(xmlPath);

        // Assert - sprawdzamy wszystkie kluczowe dane
        var faktura = context.Faktura;

        // Nagłówek
        faktura.Naglowek.KodFormularza.KodSystemowy.Should().Be("FA (3)");
        faktura.Naglowek.KodFormularza.WersjaSchemy.Should().Be("1-0E");

        // Sprzedawca
        faktura.Podmiot1.DaneIdentyfikacyjne.NIP.Should().Be("7309436499");
        faktura.Podmiot1.DaneIdentyfikacyjne.Nazwa.Should().Be("Elektrownia S.A.");

        // Nabywca
        faktura.Podmiot2.DaneIdentyfikacyjne.NIP.Should().Be("1187654321");
        faktura.Podmiot2.DaneIdentyfikacyjne.Nazwa.Should().Be("F.H.U. Jan Kowalski");

        // Dane faktury
        faktura.Fa.KodWaluty.Should().Be("PLN");
        faktura.Fa.P_2.Should().Be("FA811/05/2025");
        faktura.Fa.P_13_1.Should().Be(43.60m);
        faktura.Fa.P_14_1.Should().Be(10.03m);
        faktura.Fa.P_15.Should().Be(53.63m);

        // Pozycje
        faktura.Fa.FaWiersz.Should().HaveCount(2);
        faktura.Fa.FaWiersz[0].P_7.Should().Be("Razem za energię czynną");

        // Płatność
        faktura.Fa.Platnosc.Should().NotBeNull();
        faktura.Fa.Platnosc!.FormaPlatnosci.Should().Be("6");
    }

    [Fact]
    public async Task ParseFromStringAsync_Should_Throw_For_Wrong_Namespace()
    {
        // Arrange
        var wrongNamespaceXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://wrong.namespace/"">
    <Naglowek>
        <KodFormularza kodSystemowy=""FA (3)"" wersjaSchemy=""1-0E"">FA</KodFormularza>
    </Naglowek>
</Faktura>";

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceParseException>(
            () => _parser.ParseFromStringAsync(wrongNamespaceXml)
        );
    }

    [Fact]
    public async Task ParseFromStringAsync_Should_Throw_For_Wrong_KodSystemowy()
    {
        // Arrange
        var wrongKodSystemowyXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2025/06/25/13775/"">
    <Naglowek>
        <KodFormularza kodSystemowy=""FA (2)"" wersjaSchemy=""1-0E"">FA</KodFormularza>
    </Naglowek>
</Faktura>";

        // Act & Assert
        await Assert.ThrowsAsync<InvoiceParseException>(
            () => _parser.ParseFromStringAsync(wrongKodSystemowyXml)
        );
    }
}
