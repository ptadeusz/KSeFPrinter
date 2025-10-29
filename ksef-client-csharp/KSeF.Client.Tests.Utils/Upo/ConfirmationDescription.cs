using System.Xml.Serialization;

namespace KSeF.Client.Tests.Utils.Upo;

[XmlRoot("OpisPotwierdzenia")]
public class ConfirmationDescription
{
    [XmlElement("Strona")]
    public int Page { get; set; }

    [XmlElement("LiczbaStron")]
    public int TotalPages { get; set; }

    [XmlElement("ZakresDokumentowOd")]
    public int DocumentsRangeFrom { get; set; }

    [XmlElement("ZakresDokumentowDo")]
    public int DocumentsRangeTo { get; set; }

    [XmlElement("CalkowitaLiczbaDokumentow")]
    public int TotalDocuments { get; set; }
}
