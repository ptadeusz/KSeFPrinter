using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace KSeF.Client.Tests.Utils;

public static class MiscellaneousUtils
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generuje losowy poprawny NIP (10 cyfr).
    /// Wyrażenie regularne używane do walidacji:
    /// [1-9]((\d[1-9])|([1-9]\d))\d{7}
    /// </summary>
    public static string GetRandomNip(string prefixTwoNumbers = "")
    {
        int first;
        int second;

        if (prefixTwoNumbers.Length == 1)
        {
            first = int.Parse(prefixTwoNumbers[0].ToString());
            if (first == 0)
            {
                throw new ArgumentException("Wartość musi być większa od 0");
            }
            second = Random.Next(0, 10);
        }
        else if (prefixTwoNumbers.Length == 2)
        {
            first = int.Parse(prefixTwoNumbers[0].ToString());
            second = int.Parse(prefixTwoNumbers[1].ToString());
        }
        else if (prefixTwoNumbers.Length == 0)
        {
            first = Random.Next(1, 10);
            second = Random.Next(0, 10);
        }
        else
        {
            throw new ArgumentException("Prefiks musi mieć długość 0, 1 lub 2 cyfr.");
        }

        int third = Random.Next(0, 10);

        // regex wymaga, aby druga/trzecia cyfra nie tworzyły pary "00"
        if (second == 0 && third == 0)
        {
            third = Random.Next(1, 10);
        }

        string prefix = $"{first}{second}{third}";
        string rest = Random.Next(1000000, 9999999).ToString("D7");
        return prefix + rest;
    }

    /// <summary>
    /// Generuje losowy poprawny PESEL (11 cyfr, z częścią daty w formacie RRMMDD).
    /// Wyrażenie regularne używane do walidacji:
    /// ^\d{2}(?:0[1-9]|1[0-2]|2[1-9]|3[0-2]|4[1-9]|5[0-2]|6[1-9]|7[0-2]|8[1-9]|9[0-2])\d{7}$
    /// </summary>
    public static string GetRandomPesel()
    {
        int year = Random.Next(1900, 2299); // zakres zgodny z systemem PESEL
        int month = Random.Next(1, 12);
        int day = Random.Next(1, DateTime.DaysInMonth(year, month));

        // zakodowanie stulecia w miesiącu
        int encodedMonth = month + (year / 100 - 19) * 20;

        string datePart = $"{year % 100:D2}{encodedMonth:D2}{day:D2}";
        string serial = Random.Next(1000, 9999).ToString("D4");

        // suma kontrolna
        string basePesel = datePart + serial;
        int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
        int sum = basePesel.Select((c, i) => (c - '0') * weights[i]).Sum();
        int control = (10 - (sum % 10)) % 10;

        return basePesel + control;
    }

    /// <summary>
    /// Generuje losowy poprawny identyfikator NIP-VAT UE.
    /// Wyrażenie regularne używane do walidacji:
    /// ^(?&lt;nip&gt;[1-9](\d[1-9]|[1-9]\d)\d{7})-(?&lt;vat&gt;((AT)(U\d{8})|(BE)([01]{1}\d{9})|(BG)(\d{9,10})|(CY)(\d{8}[A-Z])|(CZ)(\d{8,10})|(DE)(\d{9})|(DK)(\d{8})|(EE)(\d{9})|(EL)(\d{9})|(ES)([A-Z]\d{8}|\d{8}[A-Z]|[A-Z]\d{7}[A-Z])|(FI)(\d{8})|(FR)[A-Z0-9]{2}\d{9}|(HR)(\d{11})|(HU)(\d{8})|(IE)(\d{7}[A-Z]{2}|\d[A-Z0-9+*]\d{5}[A-Z])|(IT)(\d{11})|(LT)(\d{9}|\d{12})|(LU)(\d{8})|(LV)(\d{11})|(MT)(\d{8})|(NL)([A-Z0-9+*]{12})|(PT)(\d{9})|(RO)(\d{2,10})|(SE)(\d{12})|(SI)(\d{8})|(SK)(\d{10})|(XI)((\d{9}|(\d{12}))|(GD|HA)(\d{3}))))$
    /// </summary>
    public static string GetRandomNipVatEU(string countryCode = "ES")
    {
        string nip = GetRandomNip();

        string vatPart = countryCode switch
        {
            "AT" => "U" + Random.Next(10000000, 99999999),
            "BE" => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            "BG" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "CY" => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            "CZ" => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "DE" => $"{Random.Next(100000000, 999999999)}",
            "DK" => $"{Random.Next(10000000, 99999999)}",
            "EE" => $"{Random.Next(100000000, 999999999)}",
            "EL" => $"{Random.Next(100000000, 999999999)}",
            "ES" => GenerateEsVat(),
            "FI" => $"{Random.Next(10000000, 99999999)}",
            "FR" => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            "HR" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "HU" => $"{Random.Next(10000000, 99999999)}",
            "IE" => GenerateIeVat(),
            "IT" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "LT" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            "LU" => $"{Random.Next(10000000, 99999999)}",
            "LV" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "MT" => $"{Random.Next(10000000, 99999999)}",
            "NL" => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            "PT" => $"{Random.Next(100000000, 999999999)}",
            "RO" => $"{Random.NextInt64(10, 9999999999)}",
            "SE" => $"{Random.NextInt64(100000000000, 999999999999)}",
            "SI" => $"{Random.Next(10000000, 99999999)}",
            "SK" => $"{Random.NextInt64(1000000000, 9999999999)}",
            "XI" => GenerateXiVat(),
            _ => throw new ArgumentException($"Niewspierany kod kraju {countryCode}")
        };

        return $"{nip}-{countryCode}{vatPart}";
    }

    /// <summary>
    /// Generuje losowy poprawny identyfikator NIP-VAT UE.
    /// Wyrażenie regularne używane do walidacji:
    /// ^(?&lt;nip&gt;[1-9](\d[1-9]|[1-9]\d)\d{7})-(?&lt;vat&gt;((AT)(U\d{8})|(BE)([01]{1}\d{9})|(BG)(\d{9,10})|(CY)(\d{8}[A-Z])|(CZ)(\d{8,10})|(DE)(\d{9})|(DK)(\d{8})|(EE)(\d{9})|(EL)(\d{9})|(ES)([A-Z]\d{8}|\d{8}[A-Z]|[A-Z]\d{7}[A-Z])|(FI)(\d{8})|(FR)[A-Z0-9]{2}\d{9}|(HR)(\d{11})|(HU)(\d{8})|(IE)(\d{7}[A-Z]{2}|\d[A-Z0-9+*]\d{5}[A-Z])|(IT)(\d{11})|(LT)(\d{9}|\d{12})|(LU)(\d{8})|(LV)(\d{11})|(MT)(\d{8})|(NL)([A-Z0-9+*]{12})|(PT)(\d{9})|(RO)(\d{2,10})|(SE)(\d{12})|(SI)(\d{8})|(SK)(\d{10})|(XI)((\d{9}|(\d{12}))|(GD|HA)(\d{3}))))$
    /// </summary>
    public static string GetRandomNipVatEU(string nip, string countryCode = "ES")
    {
        string vatPart = countryCode switch
        {
            "AT" => "U" + Random.Next(10000000, 99999999),
            "BE" => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            "BG" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "CY" => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            "CZ" => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "DE" => $"{Random.Next(100000000, 999999999)}",
            "DK" => $"{Random.Next(10000000, 99999999)}",
            "EE" => $"{Random.Next(100000000, 999999999)}",
            "EL" => $"{Random.Next(100000000, 999999999)}",
            "ES" => GenerateEsVat(),
            "FI" => $"{Random.Next(10000000, 99999999)}",
            "FR" => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            "HR" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "HU" => $"{Random.Next(10000000, 99999999)}",
            "IE" => GenerateIeVat(),
            "IT" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "LT" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            "LU" => $"{Random.Next(10000000, 99999999)}",
            "LV" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "MT" => $"{Random.Next(10000000, 99999999)}",
            "NL" => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            "PT" => $"{Random.Next(100000000, 999999999)}",
            "RO" => $"{Random.NextInt64(10, 9999999999)}",
            "SE" => $"{Random.NextInt64(100000000000, 999999999999)}",
            "SI" => $"{Random.Next(10000000, 99999999)}",
            "SK" => $"{Random.NextInt64(1000000000, 9999999999)}",
            "XI" => GenerateXiVat(),
            _ => throw new ArgumentException($"Niewspierany kod kraju {countryCode}")
        };

        return $"{nip}-{countryCode}{vatPart}";
    }

    /// <summary>
    /// Generuje losowy, poprawny identyfikator VAT UE.
    /// </summary>
    public static string GetRandomVatEU(string nip, string countryCode = "ES")
    {
        string vatPart = countryCode switch
        {
            "AT" => "U" + Random.Next(10000000, 99999999),
            "BE" => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            "BG" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "CY" => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            "CZ" => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "DE" => $"{Random.Next(100000000, 999999999)}",
            "DK" => $"{Random.Next(10000000, 99999999)}",
            "EE" => $"{Random.Next(100000000, 999999999)}",
            "EL" => $"{Random.Next(100000000, 999999999)}",
            "ES" => GenerateEsVat(),
            "FI" => $"{Random.Next(10000000, 99999999)}",
            "FR" => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            "HR" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "HU" => $"{Random.Next(10000000, 99999999)}",
            "IE" => GenerateIeVat(),
            "IT" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "LT" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            "LU" => $"{Random.Next(10000000, 99999999)}",
            "LV" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "MT" => $"{Random.Next(10000000, 99999999)}",
            "NL" => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            "PT" => $"{Random.Next(100000000, 999999999)}",
            "RO" => $"{Random.NextInt64(10, 9999999999)}",
            "SE" => $"{Random.NextInt64(100000000000, 999999999999)}",
            "SI" => $"{Random.Next(10000000, 99999999)}",
            "SK" => $"{Random.NextInt64(1000000000, 9999999999)}",
            "XI" => GenerateXiVat(),
            _ => throw new ArgumentException($"Niewspierany kod kraju {countryCode}")
        };

        return $"{countryCode}{vatPart}";
    }

    public static string GetNipVatEU(string nip, string vatue)
    {
        return $"{nip}-{vatue}";
    }

    // --- metody pomocnicze ---
    private static string RandomString(string chars, int length) =>
        new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());

    private static string GenerateEsVat()
    {
        int choice = Random.Next(3);
        return choice switch
        {
            0 => $"{(char)('A' + Random.Next(26))}{Random.Next(10000000, 99999999)}",
            1 => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            _ => $"{(char)('A' + Random.Next(26))}{Random.Next(1000000, 9999997)}{(char)('A' + Random.Next(26))}"
        };
    }

    private static string GenerateIeVat()
    {
        return Random.Next(2) == 0
            ? $"{Random.Next(1000000, 9999999)}{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 2)}"
            : $"{Random.Next(0, 9)}{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 1)}{Random.Next(10000, 99999)}{(char)('A' + Random.Next(26))}";
    }

    private static string GenerateXiVat()
    {
        int choice = Random.Next(3);
        return choice switch
        {
            0 => $"{Random.Next(100000000, 999999999)}",
            1 => $"{Random.NextInt64(100000000000, 999999999999)}",
            _ => $"{(Random.Next(2) == 0 ? "GD" : "HA")}{Random.Next(100, 999)}"
        };
    }

    /// <summary>
    /// Generuje polski IBAN (PL) z prawidłowymi cyframi kontrolnymi (ISO 13616 / mod 97)
    /// na podstawie 26 cyfr NRB: 8 cyfr bank/oddział + 16 cyfr rachunku (losowo, jeśli null).
    /// </summary>
    /// <param name="bankBranch8">8 cyfr bank/oddział; null → losowe.</param>
    /// <param name="account16">16 cyfr rachunku; null → losowe.</param>
    /// <returns>IBAN w formacie: PLkkBBBBBBBBAAAAAAAAAAAAAAAA.</returns>

    public static string GeneratePolishIban(string bankBranch8 = null, string account16 = null)
    {
        // Zbuduj 26-cyfrowy BBAN (NRB): 8 cyfr bank/oddział + 16 cyfr numeru rachunku
        string bban = (bankBranch8 ?? RandomDigits(8)) + (account16 ?? RandomDigits(16));

        // IBAN check digits: policz dla "PL00" + BBAN → przenieś "PL00" na koniec → mod 97
        string rearranged = bban + LettersToDigits("PL") + "00";
        int mod = Mod97(rearranged);
        int check = 98 - mod;
        string checkStr = check < 10 ? "0" + check : check.ToString(CultureInfo.InvariantCulture);

        return $"PL{checkStr}{bban}";

        static string RandomDigits(int len)
        {
            var sb = new StringBuilder(len);
            Span<byte> buf = stackalloc byte[1];
            for (int i = 0; i < len; i++)
            {
                // 0–9 równomiernie; prosty mapping z losowego bajtu
                RandomNumberGenerator.Fill(buf);
                sb.Append((buf[0] % 10).ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        static string LettersToDigits(string s)
        {
            var sb = new StringBuilder(s.Length * 2);
            foreach (char ch in s.ToUpperInvariant())
            {
                int val = ch - 'A' + 10; // A=10 ... Z=35
                sb.Append(val.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        static int Mod97(string numeric)
        {
            // Liczymy iteracyjnie na kawałkach (unikamy big-int)
            int chunkSize = 9;
            int start = 0;
            int rem = 0;
            while (start < numeric.Length)
            {
                int take = Math.Min(chunkSize, numeric.Length - start);
                string part = rem.ToString(CultureInfo.InvariantCulture) + numeric.Substring(start, take);
                rem = (int)(ulong.Parse(part, CultureInfo.InvariantCulture) % 97UL);
                start += take;
            }
            return rem;
        }
    }
}