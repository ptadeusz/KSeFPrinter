using System.Xml.Serialization;

namespace KSeF.Client.Tests.Utils.Upo;

public class UpoAuthentication
{
    [XmlElement("IdKontekstu")]
    public required UpoContextId ContextId { get; set; }

    [XmlElement("SkrotDokumentuUwierzytelniajacego")]
    public required string AuthenticationDocumentHash { get; set; }
}