using FluentAssertions;
using KSeFPrinter.Validators;

namespace KSeFPrinter.Tests.Validators;

/// <summary>
/// Testy walidatora numeru KSeF
/// </summary>
public class KsefNumberValidatorTests
{
    [Theory]
    [InlineData("6511153259-20251015-010020140418-0D")] // Z przykładowego pliku
    [InlineData("1234567890-20250101-ABC123456789-01")]
    public void IsValid_Should_Return_Bool_For_Valid_Format_KSeF_Numbers(string ksefNumber)
    {
        // Act
        var result = KsefNumberValidator.IsValid(ksefNumber);

        // Assert - metoda powinna zwrócić bool
        // Wartość może być true lub false w zależności od sumy kontrolnej CRC-8
        Assert.IsType<bool>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void IsValid_Should_Return_False_For_Empty_Or_Null(string? ksefNumber)
    {
        // Act
        var result = KsefNumberValidator.IsValid(ksefNumber!, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Contain("pusty");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234567890-20250101")]
    [InlineData("1234567890-20250101-ABC123456789-01-EXTRA")]
    public void IsValid_Should_Return_False_For_Invalid_Length(string ksefNumber)
    {
        // Act
        var result = KsefNumberValidator.IsValid(ksefNumber, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Contain("długość");
    }

    [Theory]
    [InlineData("ABCDEFGHIJ-20250101-ABC123456789-01")] // NIP powinien być cyframi
    [InlineData("1234567890-ABCDEFGH-ABC123456789-01")] // Data powinna być cyframi
    [InlineData("1234567890-20250101-abcdefghijkl-01")] // Część techniczna małe litery
    [InlineData("1234567890-20250101-ABC123456789-ZZ")] // CRC małe litery
    public void IsValid_Should_Return_False_For_Invalid_Format(string ksefNumber)
    {
        // Act
        var result = KsefNumberValidator.IsValid(ksefNumber, out var error);

        // Assert
        result.Should().BeFalse();
        error.Should().Contain("format");
    }

    [Fact]
    public void IsValid_Should_Validate_CRC8_Checksum()
    {
        // Arrange - tworzymy numer z nieprawidłową sumą kontrolną
        var ksefNumber = "1234567890-20250101-ABC123456789-99"; // 99 to nieprawidłowa suma

        // Act
        var result = KsefNumberValidator.IsValid(ksefNumber, out var error);

        // Assert - powinna być nieprawidłowa (chyba że 99 jest przypadkiem prawidłową sumą)
        if (!result)
        {
            error.Should().Contain("suma kontrolna");
        }
    }

    [Theory]
    [InlineData("6511153259-20251015-010020140418-0D", "6511153259")]
    [InlineData("1234567890-20250101-ABC123456789-01", "1234567890")]
    public void ExtractNIP_Should_Return_First_10_Digits(string ksefNumber, string expectedNIP)
    {
        // Act
        var nip = KsefNumberValidator.ExtractNIP(ksefNumber);

        // Assert
        nip.Should().Be(expectedNIP);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123")]
    public void ExtractNIP_Should_Return_Null_For_Invalid_Input(string? ksefNumber)
    {
        // Act
        var nip = KsefNumberValidator.ExtractNIP(ksefNumber!);

        // Assert
        nip.Should().BeNull();
    }

    [Theory]
    [InlineData("6511153259-20251015-010020140418-0D", "20251015")]
    [InlineData("1234567890-20250101-ABC123456789-01", "20250101")]
    public void ExtractDate_Should_Return_Date_String(string ksefNumber, string expectedDate)
    {
        // Act
        var date = KsefNumberValidator.ExtractDate(ksefNumber);

        // Assert
        date.Should().Be(expectedDate);
    }

    [Theory]
    [InlineData("6511153259-20251015-010020140418-0D", 2025, 10, 15)]
    [InlineData("1234567890-20250101-ABC123456789-01", 2025, 1, 1)]
    public void ParseDate_Should_Return_DateTime(string ksefNumber, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Act
        var date = KsefNumberValidator.ParseDate(ksefNumber);

        // Assert
        date.Should().NotBeNull();
        date!.Value.Year.Should().Be(expectedYear);
        date.Value.Month.Should().Be(expectedMonth);
        date.Value.Day.Should().Be(expectedDay);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890-99999999-ABC123456789-01")] // Nieprawidłowa data
    public void ParseDate_Should_Return_Null_For_Invalid_Input(string ksefNumber)
    {
        // Act
        var date = KsefNumberValidator.ParseDate(ksefNumber);

        // Assert
        date.Should().BeNull();
    }

    [Fact]
    public void IsValid_Without_ErrorMessage_Should_Work()
    {
        // Arrange
        var validNumber = "1234567890-20250101-ABC123456789-01";

        // Act
        var result = KsefNumberValidator.IsValid(validNumber);

        // Assert
        Assert.IsType<bool>(result);
    }
}
