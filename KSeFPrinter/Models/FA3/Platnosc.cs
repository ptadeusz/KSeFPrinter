using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Informacje o płatności
/// </summary>
public class Platnosc
{
    /// <summary>
    /// Terminy płatności (do 100 - np. raty)
    /// </summary>
    [XmlElement("TerminPlatnosci")]
    public List<TerminPlatnosci>? TerminPlatnosci { get; set; }

    /// <summary>
    /// Forma płatności (kod numeryczny)
    /// </summary>
    [XmlElement("FormaPlatnosci")]
    public string FormaPlatnosci { get; set; } = null!;

    /// <summary>
    /// Rachunki bankowe (do 100 rachunków)
    /// </summary>
    [XmlElement("RachunekBankowy")]
    public List<RachunekBankowy>? RachunekBankowy { get; set; }

    /// <summary>
    /// Rachunki bankowe faktora (do 20 rachunków)
    /// </summary>
    [XmlElement("RachunekBankowyFaktora")]
    public List<RachunekBankowy>? RachunekBankowyFaktora { get; set; }

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
    public DateTime? Termin { get; set; }

    /// <summary>
    /// Opis terminu płatności (alternatywa dla daty)
    /// </summary>
    [XmlElement("TerminOpis")]
    public TerminOpis? TerminOpis { get; set; }
}

/// <summary>
/// Opisowy termin płatności (np. "30 dni od dostawy")
/// </summary>
public class TerminOpis
{
    /// <summary>
    /// Liczba jednostek
    /// </summary>
    [XmlElement("Ilosc")]
    public int Ilosc { get; set; }

    /// <summary>
    /// Jednostka czasu (dni, tygodni, miesięcy)
    /// </summary>
    [XmlElement("Jednostka")]
    public string Jednostka { get; set; } = null!;

    /// <summary>
    /// Zdarzenie początkowe (np. "od daty wystawienia", "od daty dostawy")
    /// </summary>
    [XmlElement("ZdarzeniePoczatkowe")]
    public string ZdarzeniePoczatkowe { get; set; } = null!;
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
