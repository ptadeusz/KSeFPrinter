using System.Xml.Serialization;

namespace KSeFPrinter.Models.FA3;

/// <summary>
/// Nagłówek faktury FA(3)
/// </summary>
public class Naglowek
{
    /// <summary>
    /// Kod formularza
    /// </summary>
    [XmlElement("KodFormularza")]
    public KodFormularza KodFormularza { get; set; } = null!;

    /// <summary>
    /// Wariant formularza (np. "3" dla FA(3))
    /// </summary>
    [XmlElement("WariantFormularza")]
    public string WariantFormularza { get; set; } = null!;

    /// <summary>
    /// Data wytworzenia faktury w formacie ISO 8601
    /// </summary>
    [XmlElement("DataWytworzeniaFa")]
    public DateTime DataWytworzeniaFa { get; set; }

    /// <summary>
    /// Informacja o systemie wystawiającym fakturę
    /// </summary>
    [XmlElement("SystemInfo")]
    public string? SystemInfo { get; set; }
}

/// <summary>
/// Kod formularza z atrybutami
/// </summary>
public class KodFormularza
{
    /// <summary>
    /// Kod systemowy (np. "FA (3)")
    /// </summary>
    [XmlAttribute("kodSystemowy")]
    public string KodSystemowy { get; set; } = null!;

    /// <summary>
    /// Wersja schematu (np. "1-0E")
    /// </summary>
    [XmlAttribute("wersjaSchemy")]
    public string WersjaSchemy { get; set; } = null!;

    /// <summary>
    /// Wartość tekstowa (np. "FA")
    /// </summary>
    [XmlText]
    public string Value { get; set; } = null!;
}
