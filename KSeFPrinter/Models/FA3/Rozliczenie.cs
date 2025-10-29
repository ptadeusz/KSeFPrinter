using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Rozliczenie faktury (obciążenia i odliczenia)
/// </summary>
public class Rozliczenie
{
    /// <summary>
    /// Lista obciążeń
    /// </summary>
    [XmlElement("Obciazenia")]
    public List<Obciazenia>? Obciazenia { get; set; }

    /// <summary>
    /// Suma obciążeń
    /// </summary>
    [XmlElement("SumaObciazen")]
    public decimal SumaObciazen { get; set; }

    /// <summary>
    /// Lista odliczeń
    /// </summary>
    [XmlElement("Odliczenia")]
    public List<Odliczenia>? Odliczenia { get; set; }

    /// <summary>
    /// Suma odliczeń
    /// </summary>
    [XmlElement("SumaOdliczen")]
    public decimal SumaOdliczen { get; set; }

    /// <summary>
    /// Kwota do zapłaty po rozliczeniu
    /// </summary>
    [XmlElement("DoZaplaty")]
    public decimal DoZaplaty { get; set; }
}

/// <summary>
/// Pozycja obciążenia
/// </summary>
public class Obciazenia
{
    /// <summary>
    /// Kwota obciążenia
    /// </summary>
    [XmlElement("Kwota")]
    public decimal Kwota { get; set; }

    /// <summary>
    /// Powód obciążenia
    /// </summary>
    [XmlElement("Powod")]
    public string Powod { get; set; } = null!;
}

/// <summary>
/// Pozycja odliczenia
/// </summary>
public class Odliczenia
{
    /// <summary>
    /// Kwota odliczenia
    /// </summary>
    [XmlElement("Kwota")]
    public decimal Kwota { get; set; }

    /// <summary>
    /// Powód odliczenia
    /// </summary>
    [XmlElement("Powod")]
    public string Powod { get; set; } = null!;
}
