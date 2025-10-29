using System.Security.Cryptography;
using System.Text;
using KSeF.Client.Core;

namespace KSeF.Client.Tests.Core.UnitTests;

/// <summary>
/// Zestaw testów jednostkowych dla walidatora numeru KSeF.
/// Testy opisane komentarzami AAA (Arrange, Act, Assert).
/// </summary>
public class KsefNumberValidatorTests
{
    /// <summary>
    /// Gdy numer KSeF jest pusty lub zawiera wyłącznie białe znaki,
    /// walidacja powinna zwracać false oraz odpowiedni komunikat o pustej wartości.
    /// </summary>
    [Fact]
    public void IsValid_EmptyOrWhitespace_ReturnsFalseWithMessage()
    {
        // Arrange (Przygotowanie)
        var empty = string.Empty;
        var whitespace = "   ";

        // Act (Działanie)
        var resultEmpty = KsefNumberValidator.IsValid(empty, out var emptyMsg);
        var resultWs = KsefNumberValidator.IsValid(whitespace, out var wsMsg);

        // Assert (Weryfikacja)
        Assert.False(resultEmpty);
        Assert.Equal("Numer KSeF jest pusty.", emptyMsg);

        Assert.False(resultWs);
        Assert.Equal("Numer KSeF jest pusty.", wsMsg);
    }

    /// <summary>
    /// Gdy numer KSeF ma nieprawidłową długość (np. 34 zamiast 35),
    /// walidacja powinna zwracać false oraz komunikat z oczekiwaną długością.
    /// </summary>
    [Fact]
    public void IsValid_InvalidLength_ReturnsFalseWithMessage()
    {
        // Arrange (Przygotowanie)
        // 34 znaki: 32 dane + 2 suma kontrolna (brakuje jednego znaku do 35)
        var data32 = GetRandomConventionalData32(); // długość 32
        var checksum = ComputeChecksum(data32);
        var tooShort = data32 + checksum; // 34

        // Act (Działanie)
        var result = KsefNumberValidator.IsValid(tooShort, out var msg);

        // Assert (Weryfikacja)
        Assert.False(result);
        Assert.Equal("Numer KSeF ma nieprawidłową długość: 34. Oczekiwana długość to 35.", msg);
    }

    /// <summary>
    /// Dla poprawnych danych (32 znaki) i zgodnej sumy kontrolnej
    /// walidacja powinna zwrócić true bez komunikatu błędu.
    /// </summary>
    [Fact]
    public void IsValid_ValidDataAndChecksum_ReturnsTrue()
    {
        // Arrange (Przygotowanie)
        var data32 = GetRandomConventionalData32(); // długość 32
        var ksef = BuildKsefNumber(data32, 'X');    // 32 + 1 znak wypełniający + 2 suma kontrolna = 35

        // Act (Działanie)
        var result = KsefNumberValidator.IsValid(ksef, out var msg);

        // Assert (Weryfikacja)
        Assert.True(result);
        Assert.True(string.IsNullOrEmpty(msg));
    }

    /// <summary>
    /// Gdy suma kontrolna nie zgadza się z danymi,
    /// walidacja powinna zwrócić false; obecnie komunikat pozostaje pusty.
    /// </summary>
    [Fact]
    public void IsValid_MismatchedChecksum_ReturnsFalseAndEmptyMessage()
    {
        // Arrange (Przygotowanie)
        var data32 = GetRandomConventionalData32();
        var ksef = BuildKsefNumber(data32, 'X');

        // Zmień ostatnią cyfrę sumy kontrolnej, aby wymusić niezgodność
        var invalid = ksef[..^1] + (ksef[^1] == '0' ? '1' : '0');

        // Act (Działanie)
        var result = KsefNumberValidator.IsValid(invalid, out var msg);

        // Assert (Weryfikacja)
        Assert.False(result);
        // Obecna implementacja nie ustawia komunikatu błędu dla niezgodnej sumy kontrolnej
        Assert.True(string.IsNullOrEmpty(msg));
    }

    /// <summary>
    /// Modyfikacja 33. znaku (znaku wypełniającego) nie wpływa na wynik walidacji
    /// w bieżącym zachowaniu walidatora.
    /// </summary>
    [Fact]
    public void IsValid_Modifying33rdCharacter_DoesNotAffectValidation_CurrentBehavior()
    {
        // Arrange (Przygotowanie)
        var data32 = GetRandomConventionalData32();
        var baseKsef = BuildKsefNumber(data32, 'X');
        var alteredKsef = BuildKsefNumber(data32, 'Y'); // różni się tylko 33. znak

        // Act (Działanie)
        var resultBase = KsefNumberValidator.IsValid(baseKsef, out var msg1);
        var resultAltered = KsefNumberValidator.IsValid(alteredKsef, out var msg2);

        // Assert (Weryfikacja)
        Assert.True(resultBase);
        Assert.True(resultAltered);
        Assert.True(string.IsNullOrEmpty(msg1));
        Assert.True(string.IsNullOrEmpty(msg2));
    }

    private static string BuildKsefNumber(string data32, char filler)
    {
        var checksum = ComputeChecksum(data32);
        // UWAGA: Oczekiwana długość to 35, podczas gdy Dane(32) + SumaKontrolna(2) = 34.
        // Dodajemy znak wypełniający, aby uzyskać 35. Bieżący walidator ignoruje ten znak.
        return data32 + filler + checksum;
    }

    // Generuje losowe dane (32 znaki) w konwencji: yyyyMMdd-EE-XXXXXXXXXX-XXXXXXXXXX-XX,
    // a następnie ucina do 32 znaków, aby spełnić założenia walidatora.
    private static string GetRandomConventionalData32()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd"); // np. 20250916
        var part1 = RandomHex(10);                       // 10 znaków HEX
        var part2 = RandomHex(10);                       // 10 znaków HEX
        var suffix = RandomNumberGenerator.GetInt32(0, 100).ToString("D2"); // 2 cyfry 00-99

        var full = $"{date}-EE-{part1}-{part2}-{suffix}"; // długość 36
        return full[..32]; // obcięcie do 32 znaków
    }

    private static string RandomHex(int length)
    {
        var byteLen = (length + 1) / 2;
        Span<byte> bytes = stackalloc byte[byteLen];
        RandomNumberGenerator.Fill(bytes);

        var sb = new StringBuilder(byteLen * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));

        var hex = sb.ToString();
        return hex[..length]; // zwróć dokładnie 'length' znaków HEX (A-F wielkie)
    }

    // Replikuje logikę sumy kontrolnej używaną w KsefNumberValidator
    private static string ComputeChecksum(string data32)
    {
        byte crc = 0x00;
        foreach (var b in Encoding.UTF8.GetBytes(data32))
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x80) != 0
                    ? (byte)((crc << 1) ^ 0x07)
                    : (byte)(crc << 1);
            }
        }
        return crc.ToString("X2");
    }
}