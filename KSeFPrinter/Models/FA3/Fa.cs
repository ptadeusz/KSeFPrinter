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
    /// P_13_1: Suma wartości netto
    /// </summary>
    [XmlElement("P_13_1")]
    public decimal P_13_1 { get; set; }

    /// <summary>
    /// P_14_1: Suma podatku VAT
    /// </summary>
    [XmlElement("P_14_1")]
    public decimal P_14_1 { get; set; }

    /// <summary>
    /// P_15: Suma wartości brutto (do zapłaty)
    /// </summary>
    [XmlElement("P_15")]
    public decimal P_15 { get; set; }

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
