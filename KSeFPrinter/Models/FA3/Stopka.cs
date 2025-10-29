using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Stopka faktury z informacjami dodatkowymi
/// </summary>
public class Stopka
{
    /// <summary>
    /// Informacje dodatkowe w stopce
    /// </summary>
    [XmlElement("Informacje")]
    public Informacje? Informacje { get; set; }

    /// <summary>
    /// Rejestry (KRS, REGON, BDO)
    /// </summary>
    [XmlElement("Rejestry")]
    public Rejestry? Rejestry { get; set; }
}

/// <summary>
/// Informacje dodatkowe w stopce faktury
/// </summary>
public class Informacje
{
    /// <summary>
    /// Tekst stopki faktury
    /// </summary>
    [XmlElement("StopkaFaktury")]
    public string? StopkaFaktury { get; set; }
}

/// <summary>
/// Rejestry podmiotu
/// </summary>
public class Rejestry
{
    /// <summary>
    /// Numer KRS (Krajowy Rejestr Sądowy)
    /// </summary>
    [XmlElement("KRS")]
    public string? KRS { get; set; }

    /// <summary>
    /// Numer REGON
    /// </summary>
    [XmlElement("REGON")]
    public string? REGON { get; set; }

    /// <summary>
    /// Numer BDO (Baza Danych o Odpadach)
    /// </summary>
    [XmlElement("BDO")]
    public string? BDO { get; set; }
}
