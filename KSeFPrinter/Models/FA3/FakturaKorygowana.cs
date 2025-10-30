using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Informacje o fakturze korygowanej (dla faktur korygujących: KOR, KOR_ZAL, KOR_ROZ)
/// </summary>
public class FaKorygowana
{
    /// <summary>
    /// Data wystawienia faktury korygowanej
    /// </summary>
    [XmlElement("DataWystFaKorygowanej", DataType = "date")]
    public DateTime? DataWystFaKorygowanej { get; set; }

    /// <summary>
    /// Numer faktury korygowanej
    /// </summary>
    [XmlElement("NrFaKorygowanej")]
    public string? NrFaKorygowanej { get; set; }

    /// <summary>
    /// Numer KSeF faktury korygowanej
    /// </summary>
    [XmlElement("NrKSeFFaKorygowanej")]
    public string? NrKSeFFaKorygowanej { get; set; }

    /// <summary>
    /// Przyczyna korekty
    /// </summary>
    [XmlElement("PrzycznyKorekty")]
    public string? PrzycznyKorekty { get; set; }

    /// <summary>
    /// Numer zamówienia na refundację (opcjonalny)
    /// </summary>
    [XmlElement("NrZamowieniaRefund")]
    public string? NrZamowieniaRefund { get; set; }

    /// <summary>
    /// Numer faktury wewnętrznej korygowanej (opcjonalny)
    /// </summary>
    [XmlElement("OkresFaKorygowanej")]
    public OkresFa? OkresFaKorygowanej { get; set; }
}
