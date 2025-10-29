using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Wiersz faktury (pozycja)
/// </summary>
public class FaWiersz
{
    /// <summary>
    /// Numer wiersza faktury
    /// </summary>
    [XmlElement("NrWierszaFa")]
    public int NrWierszaFa { get; set; }

    /// <summary>
    /// P_6A: Indeks (kod towaru/usługi) - opcjonalny
    /// </summary>
    [XmlElement("P_6A")]
    public string? P_6A { get; set; }

    /// <summary>
    /// P_7: Nazwa towaru lub usługi
    /// </summary>
    [XmlElement("P_7")]
    public string P_7 { get; set; } = null!;

    /// <summary>
    /// P_8A: Jednostka miary - opcjonalna
    /// </summary>
    [XmlElement("P_8A")]
    public string? P_8A { get; set; }

    /// <summary>
    /// P_8B: Ilość - opcjonalna
    /// </summary>
    [XmlElement("P_8B")]
    public decimal? P_8B { get; set; }

    /// <summary>
    /// P_9A: Cena jednostkowa netto - opcjonalna
    /// </summary>
    [XmlElement("P_9A")]
    public decimal? P_9A { get; set; }

    /// <summary>
    /// P_11: Wartość netto pozycji
    /// </summary>
    [XmlElement("P_11")]
    public decimal P_11 { get; set; }

    /// <summary>
    /// P_12: Stawka VAT (procent lub kod)
    /// </summary>
    [XmlElement("P_12")]
    public string P_12 { get; set; } = null!;

    /// <summary>
    /// P_11A: Wartość brutto pozycji - opcjonalna
    /// </summary>
    [XmlElement("P_11A")]
    public decimal? P_11A { get; set; }

    /// <summary>
    /// GTU: Grupa Towarowa Usługowa - opcjonalna
    /// </summary>
    [XmlElement("GTU")]
    public string? GTU { get; set; }

    /// <summary>
    /// CN: Kod CN (Combined Nomenclature) - opcjonalny
    /// </summary>
    [XmlElement("CN")]
    public string? CN { get; set; }

    /// <summary>
    /// PKOB: Kod PKOB (Polska Klasyfikacja Obiektów Budowlanych) - opcjonalny
    /// </summary>
    [XmlElement("PKOB")]
    public string? PKOB { get; set; }
}
