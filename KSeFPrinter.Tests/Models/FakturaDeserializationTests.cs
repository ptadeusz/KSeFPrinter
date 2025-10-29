using System.Xml.Serialization;
using FluentAssertions;
using KSeFPrinter.Models.FA3;

namespace KSeFPrinter.Tests.Models;

/// <summary>
/// Testy deserializacji XML do modelu Faktura
/// </summary>
public class FakturaDeserializationTests
{
    private readonly XmlSerializer _serializer;

    public FakturaDeserializationTests()
    {
        _serializer = new XmlSerializer(typeof(Faktura));
    }

    [Fact]
    public void Should_Deserialize_FakturaTEST017_Successfully()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        Faktura? faktura;
        using (var reader = new StringReader(xmlContent))
        {
            faktura = _serializer.Deserialize(reader) as Faktura;
        }

        // Assert
        faktura.Should().NotBeNull();
        faktura!.Naglowek.Should().NotBeNull();
        faktura.Naglowek.KodFormularza.Should().NotBeNull();
        faktura.Naglowek.KodFormularza.KodSystemowy.Should().Be("FA (3)");
        faktura.Naglowek.KodFormularza.WersjaSchemy.Should().Be("1-0E");
        faktura.Naglowek.WariantFormularza.Should().Be("3");
    }

    [Fact]
    public void Should_Deserialize_OnlineInvoice_Successfully()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "6511153259-20251015-010020140418-0D.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        Faktura? faktura;
        using (var reader = new StringReader(xmlContent))
        {
            faktura = _serializer.Deserialize(reader) as Faktura;
        }

        // Assert
        faktura.Should().NotBeNull();
        faktura!.Naglowek.Should().NotBeNull();
        faktura.Podmiot1.Should().NotBeNull();
        faktura.Podmiot2.Should().NotBeNull();
        faktura.Fa.Should().NotBeNull();
    }

    [Fact]
    public void Should_Deserialize_Podmiot1_Correctly()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        Faktura? faktura;
        using (var reader = new StringReader(xmlContent))
        {
            faktura = _serializer.Deserialize(reader) as Faktura;
        }

        // Assert
        var podmiot1 = faktura!.Podmiot1;
        podmiot1.Should().NotBeNull();
        podmiot1.DaneIdentyfikacyjne.Should().NotBeNull();
        podmiot1.DaneIdentyfikacyjne.NIP.Should().Be("7309436499");
        podmiot1.DaneIdentyfikacyjne.Nazwa.Should().Be("Elektrownia S.A.");
        podmiot1.Adres.Should().NotBeNull();
        podmiot1.Adres.KodKraju.Should().Be("PL");
    }

    [Fact]
    public void Should_Deserialize_Fa_Section_Correctly()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        Faktura? faktura;
        using (var reader = new StringReader(xmlContent))
        {
            faktura = _serializer.Deserialize(reader) as Faktura;
        }

        // Assert
        var fa = faktura!.Fa;
        fa.Should().NotBeNull();
        fa.KodWaluty.Should().Be("PLN");
        fa.P_2.Should().Be("FA811/05/2025");
        fa.P_13_1.Should().Be(43.60m);
        fa.P_14_1.Should().Be(10.03m);
        fa.P_15.Should().Be(53.63m);
        fa.RodzajFaktury.Should().Be("VAT");
    }

    [Fact]
    public void Should_Deserialize_FaWiersz_Correctly()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        Faktura? faktura;
        using (var reader = new StringReader(xmlContent))
        {
            faktura = _serializer.Deserialize(reader) as Faktura;
        }

        // Assert
        var wiersze = faktura!.Fa.FaWiersz;
        wiersze.Should().NotBeNull();
        wiersze.Should().HaveCount(2);

        var wiersz1 = wiersze[0];
        wiersz1.NrWierszaFa.Should().Be(1);
        wiersz1.P_7.Should().Be("Razem za energię czynną");
        wiersz1.P_11.Should().Be(18.00m);
        wiersz1.P_12.Should().Be("23");
    }

    [Fact]
    public void Should_Deserialize_Platnosc_Correctly()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        Faktura? faktura;
        using (var reader = new StringReader(xmlContent))
        {
            faktura = _serializer.Deserialize(reader) as Faktura;
        }

        // Assert
        var platnosc = faktura!.Fa.Platnosc;
        platnosc.Should().NotBeNull();
        platnosc!.FormaPlatnosci.Should().Be("6"); // Przelew
        platnosc.RachunekBankowy.Should().NotBeNull();
        platnosc.RachunekBankowy!.NrRB.Should().Be("73111111111111111111111111");
    }

    [Fact]
    public void Should_Deserialize_Stopka_Correctly()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var xmlContent = File.ReadAllText(xmlPath);

        // Act
        Faktura? faktura;
        using (var reader = new StringReader(xmlContent))
        {
            faktura = _serializer.Deserialize(reader) as Faktura;
        }

        // Assert
        var stopka = faktura!.Stopka;
        stopka.Should().NotBeNull();
        stopka!.Rejestry.Should().NotBeNull();
        stopka.Rejestry!.KRS.Should().Be("0000099999");
        stopka.Rejestry.REGON.Should().Be("999999999");
    }
}
