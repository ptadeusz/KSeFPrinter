using System;

namespace KSeF.Client.Core.Models.Certificates
{
    public class CertificateInfo
    {
        public string CertificateSerialNumber { get; set; }
        public string Name { get; set; }
        public CertificateType Type { get; set; }
        public string CommonName { get; set; }
        public string Status { get; set; }
        public SubjectIdentifier SubjectIdentifier { get; set; }
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public DateTimeOffset LastUseDate { get; set; }
    }
}
