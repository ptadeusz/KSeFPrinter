using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Model główny faktury FA(3) zgodny ze schematem KSeF v1-0E
/// Namespace: http://crd.gov.pl/wzor/2025/06/25/13775/
/// </summary>
[XmlRoot("Faktura", Namespace = "http://crd.gov.pl/wzor/2025/06/25/13775/")]
public class Faktura
{
    /// <summary>
    /// Nagłówek faktury zawierający metadane
    /// </summary>
    [XmlElement("Naglowek")]
    public Naglowek Naglowek { get; set; } = null!;

    /// <summary>
    /// Podmiot 1 - Sprzedawca (wystawca faktury)
    /// </summary>
    [XmlElement("Podmiot1")]
    public Podmiot Podmiot1 { get; set; } = null!;

    /// <summary>
    /// Podmiot 2 - Nabywca (odbiorca faktury)
    /// </summary>
    [XmlElement("Podmiot2")]
    public Podmiot Podmiot2 { get; set; } = null!;

    /// <summary>
    /// Podmiot 3 - Opcjonalny podmiot trzeci
    /// </summary>
    [XmlElement("Podmiot3")]
    public Podmiot? Podmiot3 { get; set; }

    /// <summary>
    /// Główne dane faktury
    /// </summary>
    [XmlElement("Fa")]
    public Fa Fa { get; set; } = null!;

    /// <summary>
    /// Stopka faktury
    /// </summary>
    [XmlElement("Stopka")]
    public Stopka? Stopka { get; set; }
}
