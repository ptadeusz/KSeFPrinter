using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Informacje o płatności
/// </summary>
public class Platnosc
{
    /// <summary>
    /// Termin płatności
    /// </summary>
    [XmlElement("TerminPlatnosci")]
    public TerminPlatnosci? TerminPlatnosci { get; set; }

    /// <summary>
    /// Forma płatności (kod numeryczny)
    /// </summary>
    [XmlElement("FormaPlatnosci")]
    public string FormaPlatnosci { get; set; } = null!;

    /// <summary>
    /// Rachunek bankowy
    /// </summary>
    [XmlElement("RachunekBankowy")]
    public RachunekBankowy? RachunekBankowy { get; set; }

    /// <summary>
    /// Kwota zapłacona (dla faktur częściowo lub całkowicie opłaconych)
    /// </summary>
    [XmlElement("KwotaZaplacona")]
    public decimal? KwotaZaplacona { get; set; }
}

/// <summary>
/// Termin płatności
/// </summary>
public class TerminPlatnosci
{
    /// <summary>
    /// Data terminu płatności
    /// </summary>
    [XmlElement("Termin", DataType = "date")]
    public DateTime Termin { get; set; }
}

/// <summary>
/// Rachunek bankowy do płatności
/// </summary>
public class RachunekBankowy
{
    /// <summary>
    /// Numer rachunku bankowego
    /// </summary>
    [XmlElement("NrRB")]
    public string NrRB { get; set; } = null!;

    /// <summary>
    /// Nazwa banku
    /// </summary>
    [XmlElement("NazwaBanku")]
    public string? NazwaBanku { get; set; }

    /// <summary>
    /// Opis rachunku
    /// </summary>
    [XmlElement("OpisRachunku")]
    public string? OpisRachunku { get; set; }

    /// <summary>
    /// SWIFT/BIC banku (dla rachunków zagranicznych)
    /// </summary>
    [XmlElement("SWIFT")]
    public string? SWIFT { get; set; }
}
