using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace KSeF.Client.Core.Models.Authorization
{
    [XmlRoot("AuthTokenRequest", Namespace = "http://ksef.mf.gov.pl/auth/token/2.0")]
    public class AuthTokenRequest
    {
        public string Challenge { get; set; }

        public AuthContextIdentifier ContextIdentifier { get; set; }

        public SubjectIdentifierTypeEnum SubjectIdentifierType { get; set; }

        public AuthorizationPolicy AuthorizationPolicy { get; set; }    
    }

    public class AuthContextIdentifier : IXmlSerializable
    {
        public ContextIdentifierType Type { get; set; }
        public string Value { get; set; }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Type.ToString());
            if (Value != null)
                writer.WriteString(Value);
            writer.WriteEndElement();
        }
    }
    public enum ContextIdentifierType
    {
        [EnumMember(Value = "Nip")]
        [XmlEnum("Nip")]
        Nip,
        [EnumMember(Value = "InternalId")]
        [XmlEnum("InternalId")]
        InternalId,
        [EnumMember(Value = "NipVatUe")]
        [XmlEnum("NipVatUe")]
        NipVatUe,
        [EnumMember(Value = "PeppolId")]
        [XmlEnum("PeppolId")]
        PeppolId
    }

    public enum SubjectIdentifierTypeEnum
    {
        [EnumMember(Value = "certificateSubject")]
        [XmlEnum("certificateSubject")]
        CertificateSubject,
        [EnumMember(Value = "certificateFingerprint")]
        [XmlEnum("certificateFingerprint")]
        CertificateFingerprint
    }

    public class AuthorizationPolicy
    {
        public AllowedIps AllowedIps { get; set; } = new AllowedIps();
    }

    public class AllowedIps
    {
        [XmlElement("Ip4Address")]
        public List<string> Ip4Addresses { get; set; } = new List<string>();

        [XmlElement("Ip4Range")]
        public List<string> Ip4Ranges { get; set; } = new List<string>();

        [XmlElement("Ip4Mask")]
        public List<string> Ip4Masks { get; set; } = new List<string>();
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    public static class AuthTokenRequestSerializer
    {
        public static string SerializeToXmlString(this AuthTokenRequest request)
        {
            var serializer = new XmlSerializer(typeof(AuthTokenRequest));

            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };

            using (var sw = new Utf8StringWriter())
            {
                using (var writer = XmlWriter.Create(sw, settings))
                {
                    serializer.Serialize(writer, request);
                }
                return sw.ToString();
            }
        }
    }
}
