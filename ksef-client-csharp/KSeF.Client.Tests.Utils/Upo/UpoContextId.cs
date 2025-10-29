using System.Xml.Serialization;

namespace KSeF.Client.Tests.Utils.Upo;

public class UpoContextId
{
    [XmlElement("Nip")]
    public required string Nip { get; set; }
}