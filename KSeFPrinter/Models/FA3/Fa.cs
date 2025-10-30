using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Główne dane faktury
/// </summary>
public class Fa
{
    /// <summary>
    /// Kod waluty (ISO 4217, np. "PLN", "EUR")
    /// </summary>
    [XmlElement("KodWaluty")]
    public string KodWaluty { get; set; } = null!;

    /// <summary>
    /// P_1: Data wystawienia faktury
    /// </summary>
    [XmlElement("P_1", DataType = "date")]
    public DateTime P_1 { get; set; }

    /// <summary>
    /// P_1M: Miejsce wystawienia faktury
    /// </summary>
    [XmlElement("P_1M")]
    public string? P_1M { get; set; }

    /// <summary>
    /// P_2: Numer faktury
    /// </summary>
    [XmlElement("P_2")]
    public string P_2 { get; set; } = null!;

    /// <summary>
    /// Okres objęty fakturą (zakres dat)
    /// </summary>
    [XmlElement("OkresFa")]
    public OkresFa? OkresFa { get; set; }

    /// <summary>
    /// P_13_1: Suma wartości netto ze stawką podstawową 23%/22%
    /// </summary>
    [XmlElement("P_13_1")]
    public decimal P_13_1 { get; set; }

    /// <summary>
    /// P_14_1: Suma podatku VAT 23%/22%
    /// </summary>
    [XmlElement("P_14_1")]
    public decimal P_14_1 { get; set; }

    /// <summary>
    /// P_14_1W: VAT 23%/22% w PLN (gdy faktura w walucie obcej)
    /// </summary>
    [XmlElement("P_14_1W")]
    public decimal? P_14_1W { get; set; }

    /// <summary>
    /// P_13_2: Suma wartości netto ze stawką obniżoną pierwszą 8%/7%
    /// </summary>
    [XmlElement("P_13_2")]
    public decimal? P_13_2 { get; set; }

    /// <summary>
    /// P_14_2: Suma podatku VAT 8%/7%
    /// </summary>
    [XmlElement("P_14_2")]
    public decimal? P_14_2 { get; set; }

    /// <summary>
    /// P_14_2W: VAT 8%/7% w PLN (gdy faktura w walucie obcej)
    /// </summary>
    [XmlElement("P_14_2W")]
    public decimal? P_14_2W { get; set; }

    /// <summary>
    /// P_13_3: Suma wartości netto ze stawką obniżoną drugą 5%
    /// </summary>
    [XmlElement("P_13_3")]
    public decimal? P_13_3 { get; set; }

    /// <summary>
    /// P_14_3: Suma podatku VAT 5%
    /// </summary>
    [XmlElement("P_14_3")]
    public decimal? P_14_3 { get; set; }

    /// <summary>
    /// P_14_3W: VAT 5% w PLN (gdy faktura w walucie obcej)
    /// </summary>
    [XmlElement("P_14_3W")]
    public decimal? P_14_3W { get; set; }

    /// <summary>
    /// P_13_4: Suma wartości netto z pozostałymi stawkami VAT
    /// </summary>
    [XmlElement("P_13_4")]
    public decimal? P_13_4 { get; set; }

    /// <summary>
    /// P_14_4: Suma podatku VAT z pozostałych stawek
    /// </summary>
    [XmlElement("P_14_4")]
    public decimal? P_14_4 { get; set; }

    /// <summary>
    /// P_14_4W: VAT z pozostałych stawek w PLN (gdy faktura w walucie obcej)
    /// </summary>
    [XmlElement("P_14_4W")]
    public decimal? P_14_4W { get; set; }

    /// <summary>
    /// P_13_5: Suma wartości netto ze stawką 0%
    /// </summary>
    [XmlElement("P_13_5")]
    public decimal? P_13_5 { get; set; }

    /// <summary>
    /// P_14_5: Suma podatku VAT ze stawką 0% (zawsze 0)
    /// </summary>
    [XmlElement("P_14_5")]
    public decimal? P_14_5 { get; set; }

    /// <summary>
    /// P_15: Suma wartości brutto (do zapłaty)
    /// W przypadku faktur korygujących - RÓŻNICA (korekta kwoty wynikającej z faktury korygowanej)
    /// </summary>
    [XmlElement("P_15")]
    public decimal P_15 { get; set; }

    /// <summary>
    /// Informacje o fakturze korygowanej (tylko dla faktur korygujących: KOR, KOR_ZAL, KOR_ROZ)
    /// </summary>
    [XmlElement("FaKorygowana")]
    public FaKorygowana? FaKorygowana { get; set; }

    /// <summary>
    /// PrzyczynaKorekty: Przyczyna korekty dla faktur korygujących (opcjonalne)
    /// </summary>
    [XmlElement("PrzyczynaKorekty")]
    public string? PrzyczynaKorekty { get; set; }

    /// <summary>
    /// TypKorekty: Typ korekty (opcjonalne)
    /// 1 - Korekta skutkująca w dacie sprzedaży
    /// 2 - Korekta skutkująca w dacie wystawienia faktury korygującej
    /// 3 - Korekta skutkująca w dacie innej
    /// </summary>
    [XmlElement("TypKorekty")]
    public string? TypKorekty { get; set; }

    /// <summary>
    /// Adnotacje faktury
    /// </summary>
    [XmlElement("Adnotacje")]
    public Adnotacje? Adnotacje { get; set; }

    /// <summary>
    /// Rodzaj faktury (np. "VAT", "ROZ", "UPR")
    /// </summary>
    [XmlElement("RodzajFaktury")]
    public string RodzajFaktury { get; set; } = null!;

    /// <summary>
    /// Dodatkowe opisy (wielokrotne)
    /// </summary>
    [XmlElement("DodatkowyOpis")]
    public List<DodatkowyOpis>? DodatkowyOpis { get; set; }

    /// <summary>
    /// Wiersze faktury (pozycje)
    /// </summary>
    [XmlElement("FaWiersz")]
    public List<FaWiersz> FaWiersz { get; set; } = new();

    /// <summary>
    /// Rozliczenie faktury (obciążenia, odliczenia)
    /// </summary>
    [XmlElement("Rozliczenie")]
    public Rozliczenie? Rozliczenie { get; set; }

    /// <summary>
    /// Informacje o płatności
    /// </summary>
    [XmlElement("Platnosc")]
    public Platnosc? Platnosc { get; set; }

    /// <summary>
    /// Umowy związane z fakturą (do 100)
    /// </summary>
    [XmlElement("Umowy")]
    public List<Umowa>? Umowy { get; set; }

    /// <summary>
    /// Zamówienia związane z fakturą (do 100)
    /// </summary>
    [XmlElement("Zamowienia")]
    public List<Zamowienie>? Zamowienia { get; set; }
}

/// <summary>
/// Okres objęty fakturą
/// </summary>
public class OkresFa
{
    /// <summary>
    /// P_6_Od: Data początku okresu
    /// </summary>
    [XmlElement("P_6_Od", DataType = "date")]
    public DateTime P_6_Od { get; set; }

    /// <summary>
    /// P_6_Do: Data końca okresu
    /// </summary>
    [XmlElement("P_6_Do", DataType = "date")]
    public DateTime P_6_Do { get; set; }
}

/// <summary>
/// Adnotacje faktury
/// </summary>
public class Adnotacje
{
    /// <summary>
    /// P_16: Zwolnienie (1=TAK, 2=NIE)
    /// </summary>
    [XmlElement("P_16")]
    public string? P_16 { get; set; }

    /// <summary>
    /// P_17: Procedura szczególna (1=TAK, 2=NIE)
    /// </summary>
    [XmlElement("P_17")]
    public string? P_17 { get; set; }

    /// <summary>
    /// P_18: Odwrotne obciążenie (1=TAK, 2=NIE)
    /// </summary>
    [XmlElement("P_18")]
    public string? P_18 { get; set; }

    /// <summary>
    /// P_18A: Mechanizm podzielonej płatności (1=TAK, 2=NIE)
    /// </summary>
    [XmlElement("P_18A")]
    public string? P_18A { get; set; }

    /// <summary>
    /// Szczegóły zwolnienia
    /// </summary>
    [XmlElement("Zwolnienie")]
    public Zwolnienie? Zwolnienie { get; set; }

    /// <summary>
    /// Nowe środki transportu
    /// </summary>
    [XmlElement("NoweSrodkiTransportu")]
    public NoweSrodkiTransportu? NoweSrodkiTransportu { get; set; }

    /// <summary>
    /// P_23: Faktura do paragonu (1=TAK, 2=NIE)
    /// </summary>
    [XmlElement("P_23")]
    public string? P_23 { get; set; }

    /// <summary>
    /// Procedura marży
    /// </summary>
    [XmlElement("PMarzy")]
    public PMarzy? PMarzy { get; set; }
}

/// <summary>
/// Szczegóły zwolnienia
/// </summary>
public class Zwolnienie
{
    /// <summary>
    /// P_19N: Nie dotyczy (1=TAK)
    /// </summary>
    [XmlElement("P_19N")]
    public string? P_19N { get; set; }
}

/// <summary>
/// Nowe środki transportu
/// </summary>
public class NoweSrodkiTransportu
{
    /// <summary>
    /// P_22N: Nie dotyczy (1=TAK)
    /// </summary>
    [XmlElement("P_22N")]
    public string? P_22N { get; set; }
}

/// <summary>
/// Procedura marży
/// </summary>
public class PMarzy
{
    /// <summary>
    /// P_PMarzyN: Nie dotyczy (1=TAK)
    /// </summary>
    [XmlElement("P_PMarzyN")]
    public string? P_PMarzyN { get; set; }
}

/// <summary>
/// Dodatkowy opis w fakturze
/// </summary>
public class DodatkowyOpis
{
    /// <summary>
    /// Klucz opisu
    /// </summary>
    [XmlElement("Klucz")]
    public string Klucz { get; set; } = null!;

    /// <summary>
    /// Wartość opisu
    /// </summary>
    [XmlElement("Wartosc")]
    public string Wartosc { get; set; } = null!;
}

/// <summary>
/// Umowa związana z fakturą
/// </summary>
public class Umowa
{
    /// <summary>
    /// Data umowy
    /// </summary>
    [XmlElement("DataUmowy", DataType = "date")]
    public DateTime? DataUmowy { get; set; }

    /// <summary>
    /// Numer umowy
    /// </summary>
    [XmlElement("NrUmowy")]
    public string? NrUmowy { get; set; }
}

/// <summary>
/// Zamówienie związane z fakturą
/// </summary>
public class Zamowienie
{
    /// <summary>
    /// Data zamówienia
    /// </summary>
    [XmlElement("DataZamowienia", DataType = "date")]
    public DateTime? DataZamowienia { get; set; }

    /// <summary>
    /// Numer zamówienia
    /// </summary>
    [XmlElement("NrZamowienia")]
    public string? NrZamowienia { get; set; }
}
