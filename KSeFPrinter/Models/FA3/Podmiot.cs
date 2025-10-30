using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Podmiot faktury (może być użyty jako Podmiot1, Podmiot2 lub Podmiot3)
/// </summary>
public class Podmiot
{
    /// <summary>
    /// Dane identyfikacyjne podmiotu (NIP, nazwa)
    /// </summary>
    [XmlElement("DaneIdentyfikacyjne")]
    public DaneIdentyfikacyjne DaneIdentyfikacyjne { get; set; } = null!;

    /// <summary>
    /// Adres podmiotu
    /// </summary>
    [XmlElement("Adres")]
    public Adres Adres { get; set; } = null!;

    /// <summary>
    /// Dane kontaktowe (email, telefon) - do 3 zestawów
    /// </summary>
    [XmlElement("DaneKontaktowe")]
    public List<DaneKontaktowe>? DaneKontaktowe { get; set; }

    /// <summary>
    /// Numer klienta (opcjonalny, dla Podmiot2)
    /// </summary>
    [XmlElement("NrKlienta")]
    public string? NrKlienta { get; set; }

    /// <summary>
    /// Jednostka samorządu terytorialnego (opcjonalny)
    /// </summary>
    [XmlElement("JST")]
    public string? JST { get; set; }

    /// <summary>
    /// Gospodarka wodno-ściekowa (opcjonalny)
    /// </summary>
    [XmlElement("GV")]
    public string? GV { get; set; }

    /// <summary>
    /// Rola podmiotu trzeciego (1-11, tylko dla Podmiot3)
    /// </summary>
    [XmlElement("Rola")]
    public string? Rola { get; set; }

    /// <summary>
    /// Udział procentowy (tylko dla Podmiot3)
    /// </summary>
    [XmlElement("UdzialProcentowy")]
    public decimal? UdzialProcentowy { get; set; }
}

/// <summary>
/// Dane identyfikacyjne podmiotu
/// </summary>
public class DaneIdentyfikacyjne
{
    /// <summary>
    /// Numer NIP podmiotu
    /// </summary>
    [XmlElement("NIP")]
    public string NIP { get; set; } = null!;

    /// <summary>
    /// Nazwa podmiotu
    /// </summary>
    [XmlElement("Nazwa")]
    public string Nazwa { get; set; } = null!;

    /// <summary>
    /// Numer PESEL (opcjonalny)
    /// </summary>
    [XmlElement("PESEL")]
    public string? PESEL { get; set; }
}

/// <summary>
/// Adres podmiotu
/// </summary>
public class Adres
{
    /// <summary>
    /// Kod kraju (ISO 3166-1 alpha-2, np. "PL")
    /// </summary>
    [XmlElement("KodKraju")]
    public string KodKraju { get; set; } = null!;

    /// <summary>
    /// Pierwsza linia adresu (ulica, numer)
    /// </summary>
    [XmlElement("AdresL1")]
    public string AdresL1 { get; set; } = null!;

    /// <summary>
    /// Druga linia adresu (kod pocztowy, miejscowość)
    /// </summary>
    [XmlElement("AdresL2")]
    public string AdresL2 { get; set; } = null!;
}

/// <summary>
/// Dane kontaktowe podmiotu
/// </summary>
public class DaneKontaktowe
{
    /// <summary>
    /// Adres email
    /// </summary>
    [XmlElement("Email")]
    public string? Email { get; set; }

    /// <summary>
    /// Numer telefonu
    /// </summary>
    [XmlElement("Telefon")]
    public string? Telefon { get; set; }
}
