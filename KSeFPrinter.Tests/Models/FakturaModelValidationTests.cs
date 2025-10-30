using FluentAssertions;
using KSeFPrinter.Models.FA3;
using KSeFPrinter.Models.Common;

namespace KSeFPrinter.Tests.Models;

/// <summary>
/// Testy walidacji i edge cases dla modeli FA3
/// </summary>
public class FakturaModelValidationTests
{
    [Fact]
    public void Faktura_Should_Allow_Null_Optional_Fields()
    {
        // Arrange & Act
        var faktura = new Faktura
        {
            Naglowek = new Naglowek
            {
                KodFormularza = new KodFormularza
                {
                    KodSystemowy = "FA (3)",
                    WersjaSchemy = "1-0E"
                },
                WariantFormularza = "3",
                DataWytworzeniaFa = DateTime.Now,
                SystemInfo = "Test"
            },
            Podmiot1 = new Podmiot(),
            Podmiot2 = new Podmiot(),
            Fa = new Fa()
        };

        // Assert - Stopka jest opcjonalna
        faktura.Stopka.Should().BeNull();
    }

    [Fact]
    public void Podmiot_Should_Handle_Optional_Fields()
    {
        // Arrange & Act
        var podmiot = new Podmiot
        {
            DaneIdentyfikacyjne = new DaneIdentyfikacyjne
            {
                NIP = "1234567890",
                Nazwa = "Test Company"
            }
        };

        // Assert - wszystkie pola opcjonalne mogą być null
        podmiot.Adres.Should().BeNull();
        podmiot.DaneKontaktowe.Should().BeNull();
        podmiot.Rola.Should().BeNull();
    }

    [Fact]
    public void FaWiersz_Should_Handle_Optional_Decimal_Fields()
    {
        // Arrange & Act
        var wiersz = new FaWiersz
        {
            NrWierszaFa = 1,
            P_7 = "Test item",
            P_11 = 100.00m,
            P_12 = "23"
        };

        // Assert - opcjonalne pola decimal
        wiersz.P_8A.Should().BeNull();
        wiersz.P_8B.Should().BeNull();
        wiersz.P_9A.Should().BeNull();
        wiersz.P_11A.Should().BeNull();
    }

    [Fact]
    public void Platnosc_Should_Handle_Empty_Lists()
    {
        // Arrange & Act
        var platnosc = new Platnosc
        {
            FormaPlatnosci = "6"
        };

        // Assert
        platnosc.TerminPlatnosci.Should().BeNull();
        platnosc.RachunekBankowy.Should().BeNull();
        platnosc.RachunekBankowyFaktora.Should().BeNull();
    }

    [Fact]
    public void Stopka_Should_Handle_Optional_Text_Fields()
    {
        // Arrange & Act
        var stopka = new Stopka();

        // Assert
        stopka.Informacje.Should().BeNull();
        stopka.Rejestry.Should().BeNull();
    }

    [Fact]
    public void Fa_Rozliczenie_Should_Handle_Optional_Fields()
    {
        // Arrange & Act
        var fa = new Fa
        {
            P_2 = "TEST/001",
            P_1 = DateTime.Now,
            KodWaluty = "PLN",
            P_13_1 = 100m,
            P_14_1 = 23m,
            P_15 = 123m,
            FaWiersz = new List<FaWiersz>()
        };

        // Assert - Rozliczenie jest opcjonalne
        fa.Rozliczenie.Should().BeNull();
    }

    [Fact]
    public void Fa_Should_Handle_Large_Decimal_Values()
    {
        // Arrange & Act
        var fa = new Fa
        {
            P_2 = "TEST/001",
            P_1 = DateTime.Now,
            KodWaluty = "PLN",
            P_13_1 = 999999999.99m,
            P_14_1 = 229999999.99m,
            P_15 = 1229999999.98m,
            FaWiersz = new List<FaWiersz>()
        };

        // Assert - duże wartości powinny być obsługiwane
        fa.P_13_1.Should().BeGreaterThan(0);
        fa.P_14_1.Should().BeGreaterThan(0);
        fa.P_15.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Fa_Should_Handle_Zero_Values()
    {
        // Arrange & Act
        var fa = new Fa
        {
            P_2 = "TEST/001",
            P_1 = DateTime.Now,
            KodWaluty = "PLN",
            P_13_1 = 0m,
            P_14_1 = 0m,
            P_15 = 0m,
            FaWiersz = new List<FaWiersz>()
        };

        // Assert
        fa.P_13_1.Should().Be(0);
        fa.P_14_1.Should().Be(0);
        fa.P_15.Should().Be(0);
    }

    [Fact]
    public void Fa_Should_Handle_Negative_Values_For_Credit_Notes()
    {
        // Arrange & Act - faktury korygujące mogą mieć ujemne wartości
        var fa = new Fa
        {
            P_2 = "TEST/001/K",
            P_1 = DateTime.Now,
            KodWaluty = "PLN",
            P_13_1 = -100.00m,
            P_14_1 = -23.00m,
            P_15 = -123.00m,
            FaWiersz = new List<FaWiersz>()
        };

        // Assert
        fa.P_13_1.Should().BeNegative();
        fa.P_14_1.Should().BeNegative();
        fa.P_15.Should().BeNegative();
    }

    [Fact]
    public void FaWiersz_Should_Handle_Very_Small_Decimal_Values()
    {
        // Arrange & Act
        var wiersz = new FaWiersz
        {
            NrWierszaFa = 1,
            P_7 = "Test",
            P_8B = 0.001m,
            P_9A = 0.01m,
            P_11 = 0.00m,
            P_12 = "23"
        };

        // Assert
        wiersz.P_8B.Should().Be(0.001m);
        wiersz.P_9A.Should().Be(0.01m);
    }

    [Fact]
    public void Platnosc_Should_Handle_Multiple_Payment_Terms()
    {
        // Arrange & Act
        var platnosc = new Platnosc
        {
            FormaPlatnosci = "6",
            TerminPlatnosci = new List<TerminPlatnosci>
            {
                new TerminPlatnosci { Termin = DateTime.Now.AddDays(30) },
                new TerminPlatnosci { Termin = DateTime.Now.AddDays(60) },
                new TerminPlatnosci { Termin = DateTime.Now.AddDays(90) }
            }
        };

        // Assert
        platnosc.TerminPlatnosci.Should().HaveCount(3);
        platnosc.TerminPlatnosci.Should().AllSatisfy(t => t.Termin.Should().NotBeNull());
    }

    [Fact]
    public void Platnosc_Should_Handle_Multiple_Bank_Accounts()
    {
        // Arrange & Act
        var platnosc = new Platnosc
        {
            FormaPlatnosci = "6",
            RachunekBankowy = new List<RachunekBankowy>
            {
                new RachunekBankowy { NrRB = "12345678901234567890123456" },
                new RachunekBankowy { NrRB = "98765432109876543210987654" }
            }
        };

        // Assert
        platnosc.RachunekBankowy.Should().HaveCount(2);
        platnosc.RachunekBankowy.Should().AllSatisfy(rb => rb.NrRB.Should().HaveLength(26));
    }

    [Fact]
    public void Fa_Should_Handle_Empty_FaWiersz_List()
    {
        // Arrange & Act
        var fa = new Fa
        {
            P_2 = "TEST/001",
            P_1 = DateTime.Now,
            KodWaluty = "PLN",
            P_13_1 = 0m,
            P_14_1 = 0m,
            P_15 = 0m,
            FaWiersz = new List<FaWiersz>()
        };

        // Assert
        fa.FaWiersz.Should().NotBeNull();
        fa.FaWiersz.Should().BeEmpty();
    }

    [Fact]
    public void Fa_Should_Handle_Many_FaWiersz_Items()
    {
        // Arrange & Act
        var fa = new Fa
        {
            P_2 = "TEST/001",
            P_1 = DateTime.Now,
            KodWaluty = "PLN",
            P_13_1 = 1000m,
            P_14_1 = 230m,
            P_15 = 1230m,
            FaWiersz = Enumerable.Range(1, 100).Select(i => new FaWiersz
            {
                NrWierszaFa = i,
                P_7 = $"Item {i}",
                P_11 = 10m,
                P_12 = "23"
            }).ToList()
        };

        // Assert
        fa.FaWiersz.Should().HaveCount(100);
        fa.FaWiersz.Should().AllSatisfy(w => w.NrWierszaFa.Should().BeGreaterThan(0));
    }

    [Fact]
    public void Podmiot_Should_Handle_Long_Text_Fields()
    {
        // Arrange & Act
        var podmiot = new Podmiot
        {
            DaneIdentyfikacyjne = new DaneIdentyfikacyjne
            {
                NIP = "1234567890",
                Nazwa = new string('X', 500) // Bardzo długa nazwa
            }
        };

        // Assert
        podmiot.DaneIdentyfikacyjne.Nazwa.Should().HaveLength(500);
    }

    [Fact]
    public void Adres_Should_Handle_Special_Characters()
    {
        // Arrange & Act
        var adres = new Adres
        {
            KodKraju = "PL",
            AdresL1 = "ul. Królewska 123/45A",
            AdresL2 = "00-123 Warszawa, Śródmieście"
        };

        // Assert
        adres.AdresL1.Should().Contain("Królewska");
        adres.AdresL2.Should().Contain("Śródmieście");
    }

    [Fact]
    public void InvoiceContext_Should_Set_Metadata_Correctly()
    {
        // Arrange & Act
        var context = new InvoiceContext
        {
            Faktura = new Faktura(),
            Metadata = new KSeFMetadata
            {
                Tryb = TrybWystawienia.Online,
                NumerKSeF = "1234567890-20250101-123456789ABC-01"
            }
        };

        // Assert
        context.Metadata.Tryb.Should().Be(TrybWystawienia.Online);
        context.Metadata.NumerKSeF.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PdfGenerationOptions_Should_Have_Sensible_Defaults()
    {
        // Arrange & Act
        var options = new PdfGenerationOptions();

        // Assert
        options.UseProduction.Should().BeFalse();
        options.IncludeQrCode1.Should().BeTrue();
        options.IncludeQrCode2.Should().BeTrue(); // Domyślnie true
        options.QrPixelsPerModule.Should().Be(5);
        options.Certificate.Should().BeNull();
    }

    [Fact]
    public void KodWaluty_Should_Be_Valid_ISO_Currency_Code()
    {
        // Arrange & Act
        var fa = new Fa
        {
            P_2 = "TEST/001",
            P_1 = DateTime.Now,
            KodWaluty = "EUR", // Inna waluta niż PLN
            P_13_1 = 100m,
            P_14_1 = 23m,
            P_15 = 123m,
            FaWiersz = new List<FaWiersz>()
        };

        // Assert
        fa.KodWaluty.Should().HaveLength(3);
        fa.KodWaluty.Should().MatchRegex("^[A-Z]{3}$");
    }
}
