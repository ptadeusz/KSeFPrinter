namespace KSeFPrinter.Models.Common;

/// <summary>
/// Kody walut zgodnie z ISO 4217
/// </summary>
public static class KodWaluty
{
    /// <summary>
    /// Polski złoty
    /// </summary>
    public const string PLN = "PLN";

    /// <summary>
    /// Euro
    /// </summary>
    public const string EUR = "EUR";

    /// <summary>
    /// Dolar amerykański
    /// </summary>
    public const string USD = "USD";

    /// <summary>
    /// Funt szterling
    /// </summary>
    public const string GBP = "GBP";

    /// <summary>
    /// Frank szwajcarski
    /// </summary>
    public const string CHF = "CHF";

    /// <summary>
    /// Korona czeska
    /// </summary>
    public const string CZK = "CZK";

    /// <summary>
    /// Korona szwedzka
    /// </summary>
    public const string SEK = "SEK";

    /// <summary>
    /// Korona norweska
    /// </summary>
    public const string NOK = "NOK";

    /// <summary>
    /// Korona duńska
    /// </summary>
    public const string DKK = "DKK";

    /// <summary>
    /// Jen japoński
    /// </summary>
    public const string JPY = "JPY";

    /// <summary>
    /// Sprawdza czy kod waluty jest prawidłowy (podstawowa walidacja)
    /// </summary>
    public static bool IsValid(string kod)
    {
        return !string.IsNullOrWhiteSpace(kod) && kod.Length == 3 && kod.All(char.IsUpper);
    }
}
