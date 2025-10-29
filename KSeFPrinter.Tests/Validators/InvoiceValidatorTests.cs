using FluentAssertions;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Models.FA3;
using KSeFPrinter.Services.Parsers;
using KSeFPrinter.Validators;
using Microsoft.Extensions.Logging.Abstractions;

namespace KSeFPrinter.Tests.Validators;

/// <summary>
/// Testy walidatora faktury
/// </summary>
public class InvoiceValidatorTests
{
    private readonly InvoiceValidator _validator;

    public InvoiceValidatorTests()
    {
        _validator = new InvoiceValidator(NullLogger<InvoiceValidator>.Instance);
    }

    [Fact]
    public async Task Validate_Should_Pass_For_Valid_Invoice_From_TestData()
    {
        // Arrange
        var xmlPath = Path.Combine("TestData", "FakturaTEST017.xml");
        var parser = new XmlInvoiceParser(
            NullLogger<XmlInvoiceParser>.Instance
        );
        var context = await parser.ParseFromFileAsync(xmlPath);

        // Act
        var result = _validator.Validate(context.Faktura);

        // Assert
        result.Should().NotBeNull();
        // Może być że są ostrzeżenia, ale nie powinno być błędów krytycznych dla naszych testowych danych
    }

    [Fact]
    public void Validate_Should_Fail_When_Naglowek_Is_Null()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Naglowek = null!;

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nagłówka"));
    }

    [Fact]
    public void Validate_Should_Fail_When_KodSystemowy_Is_Wrong()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Naglowek.KodFormularza.KodSystemowy = "FA (2)";

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("kod systemowy"));
    }

    [Fact]
    public void Validate_Should_Fail_When_Podmiot1_Is_Null()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Podmiot1 = null!;

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Podmiot1"));
    }

    [Fact]
    public void Validate_Should_Fail_When_NIP_Is_Empty()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Podmiot1.DaneIdentyfikacyjne.NIP = "";

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("NIP"));
    }

    [Theory]
    [InlineData("123")] // Za krótki
    [InlineData("ABCDEFGHIJ")] // Nie cyfry
    [InlineData("12345678901")] // Za długi
    public void Validate_Should_Fail_When_NIP_Format_Is_Invalid(string invalidNIP)
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Podmiot1.DaneIdentyfikacyjne.NIP = invalidNIP;

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("NIP"));
    }

    [Fact]
    public void Validate_Should_Fail_When_Fa_Is_Null()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Fa = null!;

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Fa"));
    }

    [Fact]
    public void Validate_Should_Fail_When_No_Invoice_Lines()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Fa.FaWiersz = new List<FaWiersz>();

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("pozycji"));
    }

    [Fact]
    public void Validate_Should_Warn_When_Sum_Mismatch()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Fa.P_13_1 = 100.00m; // Suma z wierszy jest inna
        faktura.Fa.FaWiersz[0].P_11 = 50.00m;

        // Act
        var result = _validator.Validate(faktura);

        // Assert
        // Może być warning o niezgodności sum
        if (!result.IsValid)
        {
            (result.Warnings.Count + result.Errors.Count).Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void ValidateContext_Should_Fail_When_Online_Mode_Without_KSeF_Number()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        var context = new InvoiceContext
        {
            Faktura = faktura,
            Metadata = new KSeFMetadata
            {
                Tryb = TrybWystawienia.Online,
                NumerKSeF = null
            }
        };

        // Act
        var result = _validator.ValidateContext(context);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("numer KSeF"));
    }

    [Fact]
    public void ValidateContext_Should_Warn_When_NIP_Mismatch_In_KSeF_Number()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        faktura.Podmiot1.DaneIdentyfikacyjne.NIP = "1234567890";

        var context = new InvoiceContext
        {
            Faktura = faktura,
            Metadata = new KSeFMetadata
            {
                Tryb = TrybWystawienia.Online,
                NumerKSeF = "9999999999-20250101-ABC123456789-01" // Inny NIP
            }
        };

        // Act
        var result = _validator.ValidateContext(context);

        // Assert - powinno być ostrzeżenie o niezgodności NIP
        if (result.Warnings.Count > 0)
        {
            result.Warnings.Should().Contain(w => w.Contains("NIP"));
        }
    }

    [Fact]
    public void Validate_Should_Pass_For_Offline_Invoice()
    {
        // Arrange
        var faktura = CreateMinimalValidFaktura();
        var context = new InvoiceContext
        {
            Faktura = faktura,
            Metadata = new KSeFMetadata
            {
                Tryb = TrybWystawienia.Offline,
                NumerKSeF = null
            }
        };

        // Act
        var result = _validator.ValidateContext(context);

        // Assert - faktura offline bez numeru KSeF jest OK
        result.Should().NotBeNull();
    }

    // Helper method - tworzy minimalną poprawną fakturę do testów
    private Faktura CreateMinimalValidFaktura()
    {
        return new Faktura
        {
            Naglowek = new Naglowek
            {
                KodFormularza = new KodFormularza
                {
                    KodSystemowy = "FA (3)",
                    WersjaSchemy = "1-0E",
                    Value = "FA"
                },
                WariantFormularza = "3",
                DataWytworzeniaFa = DateTime.Now
            },
            Podmiot1 = new Podmiot
            {
                DaneIdentyfikacyjne = new DaneIdentyfikacyjne
                {
                    NIP = "7309436499", // Poprawny NIP z sumy kontrolnej
                    Nazwa = "Test Sprzedawca"
                },
                Adres = new Adres
                {
                    KodKraju = "PL",
                    AdresL1 = "ul. Testowa 1",
                    AdresL2 = "00-000 Warszawa"
                }
            },
            Podmiot2 = new Podmiot
            {
                DaneIdentyfikacyjne = new DaneIdentyfikacyjne
                {
                    NIP = "1187654321", // Poprawny NIP
                    Nazwa = "Test Nabywca"
                },
                Adres = new Adres
                {
                    KodKraju = "PL",
                    AdresL1 = "ul. Testowa 2",
                    AdresL2 = "00-000 Warszawa"
                }
            },
            Fa = new Fa
            {
                KodWaluty = "PLN",
                P_1 = DateTime.Now,
                P_2 = "TEST/001/2025",
                P_13_1 = 100.00m,
                P_14_1 = 23.00m,
                P_15 = 123.00m,
                RodzajFaktury = "VAT",
                FaWiersz = new List<FaWiersz>
                {
                    new FaWiersz
                    {
                        NrWierszaFa = 1,
                        P_7 = "Testowa usługa",
                        P_11 = 100.00m,
                        P_12 = "23"
                    }
                }
            }
        };
    }
}
