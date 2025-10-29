namespace KSeFPrinter.Models.Common;

/// <summary>
/// Forma płatności zgodnie z KSeF
/// Kody numeryczne reprezentujące różne formy płatności
/// </summary>
public static class FormaPlatnosci
{
    /// <summary>
    /// 1 - Gotówka
    /// </summary>
    public const string Gotowka = "1";

    /// <summary>
    /// 2 - Karta płatnicza
    /// </summary>
    public const string KartaPlatnicza = "2";

    /// <summary>
    /// 3 - Bon
    /// </summary>
    public const string Bon = "3";

    /// <summary>
    /// 4 - Czek
    /// </summary>
    public const string Czek = "4";

    /// <summary>
    /// 5 - Kredyt
    /// </summary>
    public const string Kredyt = "5";

    /// <summary>
    /// 6 - Przelew
    /// </summary>
    public const string Przelew = "6";

    /// <summary>
    /// 7 - Mobilna płatność
    /// </summary>
    public const string MobilnaPlatnosc = "7";

    /// <summary>
    /// 8 - Inna forma płatności
    /// </summary>
    public const string Inna = "8";

    /// <summary>
    /// Sprawdza czy kod formy płatności jest prawidłowy
    /// </summary>
    public static bool IsValid(string kod)
    {
        return kod is Gotowka or KartaPlatnicza or Bon or Czek or Kredyt or Przelew or MobilnaPlatnosc or Inna;
    }

    /// <summary>
    /// Zwraca opis formy płatności dla danego kodu
    /// </summary>
    public static string GetOpis(string kod)
    {
        return kod switch
        {
            Gotowka => "Gotówka",
            KartaPlatnicza => "Karta płatnicza",
            Bon => "Bon",
            Czek => "Czek",
            Kredyt => "Kredyt",
            Przelew => "Przelew",
            MobilnaPlatnosc => "Mobilna płatność",
            Inna => "Inna forma płatności",
            _ => "Nieznana forma płatności"
        };
    }
}
