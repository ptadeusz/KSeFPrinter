using System.Text;
using System.Text.RegularExpressions;

namespace KSeFPrinter.Validators;

/// <summary>
/// Walidator numeru KSeF z algorytmem CRC-8
/// Format: 9999999999-RRRRMMDD-FFFFFFFFFFFF-FF (35 znaków)
/// Polinom CRC-8: 0x07, wartość początkowa: 0x00
/// </summary>
public static class KsefNumberValidator
{
    private const byte Polynomial = 0x07;
    private const byte InitValue = 0x00;

    private const int ExpectedLength = 35;
    private const int DataLength = 32; // Wszystko poza ostatnimi 2 znakami (CRC)
    private const int ChecksumLength = 2;

    // Format: 10 cyfr NIP - 8 cyfr daty - 12 znaków hex - 2 znaki hex (CRC)
    private static readonly Regex KsefNumberPattern = new(
        @"^\d{10}-\d{8}-[0-9A-F]{12}-[0-9A-F]{2}$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Weryfikuje numer KSeF poprzez sprawdzenie jego formatu i sumy kontrolnej CRC-8
    /// </summary>
    /// <param name="ksefNumber">Numer KSeF do walidacji</param>
    /// <returns>True jeśli numer jest prawidłowy</returns>
    public static bool IsValid(string ksefNumber)
    {
        return IsValid(ksefNumber, out _);
    }

    /// <summary>
    /// Weryfikuje numer KSeF poprzez sprawdzenie jego formatu i sumy kontrolnej CRC-8
    /// </summary>
    /// <param name="ksefNumber">Numer KSeF do walidacji</param>
    /// <param name="errorMessage">Komunikat błędu, jeśli numer jest nieprawidłowy</param>
    /// <returns>True jeśli numer jest prawidłowy</returns>
    public static bool IsValid(string ksefNumber, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(ksefNumber))
        {
            errorMessage = "Numer KSeF jest pusty.";
            return false;
        }

        if (ksefNumber.Length != ExpectedLength)
        {
            errorMessage = $"Numer KSeF ma nieprawidłową długość: {ksefNumber.Length}. Oczekiwana długość to {ExpectedLength}.";
            return false;
        }

        // Walidacja formatu
        if (!KsefNumberPattern.IsMatch(ksefNumber))
        {
            errorMessage = "Numer KSeF ma nieprawidłowy format. Oczekiwany format: 9999999999-RRRRMMDD-FFFFFFFFFFFF-FF";
            return false;
        }

        // Ekstrakcja danych i sumy kontrolnej
        string data = ksefNumber.Substring(0, DataLength);
        string checksumFromNumber = ksefNumber.Substring(ExpectedLength - ChecksumLength, ChecksumLength);

        // Obliczenie sumy kontrolnej
        string calculatedChecksum = ComputeChecksum(Encoding.UTF8.GetBytes(data));

        if (calculatedChecksum != checksumFromNumber)
        {
            errorMessage = $"Nieprawidłowa suma kontrolna CRC-8. Oczekiwano: {calculatedChecksum}, otrzymano: {checksumFromNumber}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Oblicza sumę kontrolną CRC-8 dla podanych danych
    /// </summary>
    /// <param name="data">Dane do obliczenia sumy kontrolnej</param>
    /// <returns>Suma kontrolna jako 2-znakowy string hexadecymalny (wielkie litery)</returns>
    private static string ComputeChecksum(byte[] data)
    {
        byte crc = InitValue;

        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x80) != 0
                    ? (byte)((crc << 1) ^ Polynomial)
                    : (byte)(crc << 1);
            }
        }

        return crc.ToString("X2"); // Zawsze 2-znakowy hex z wielkimi literami
    }

    /// <summary>
    /// Ekstraktuje NIP sprzedawcy z numeru KSeF
    /// </summary>
    /// <param name="ksefNumber">Numer KSeF</param>
    /// <returns>NIP sprzedawcy (10 cyfr) lub null jeśli numer jest nieprawidłowy</returns>
    public static string? ExtractNIP(string ksefNumber)
    {
        if (string.IsNullOrWhiteSpace(ksefNumber) || ksefNumber.Length < 10)
        {
            return null;
        }

        return ksefNumber.Substring(0, 10);
    }

    /// <summary>
    /// Ekstraktuje datę z numeru KSeF
    /// </summary>
    /// <param name="ksefNumber">Numer KSeF</param>
    /// <returns>Data w formacie RRRRMMDD lub null jeśli numer jest nieprawidłowy</returns>
    public static string? ExtractDate(string ksefNumber)
    {
        if (string.IsNullOrWhiteSpace(ksefNumber) || ksefNumber.Length < 19)
        {
            return null;
        }

        return ksefNumber.Substring(11, 8); // Po pierwszym '-'
    }

    /// <summary>
    /// Parsuje datę z numeru KSeF do DateTime
    /// </summary>
    /// <param name="ksefNumber">Numer KSeF</param>
    /// <returns>DateTime lub null jeśli nie można sparsować</returns>
    public static DateTime? ParseDate(string ksefNumber)
    {
        var dateString = ExtractDate(ksefNumber);
        if (dateString == null || dateString.Length != 8)
        {
            return null;
        }

        if (DateTime.TryParseExact(
            dateString,
            "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var date))
        {
            return date;
        }

        return null;
    }
}
