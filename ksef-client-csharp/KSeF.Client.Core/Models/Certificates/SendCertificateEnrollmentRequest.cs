using System;

namespace KSeF.Client.Core.Models.Certificates
{
    public class SendCertificateEnrollmentRequest
    {
        public string CertificateName { get; set; }
        public CertificateType CertificateType { get; set; }
        public string Csr { get; set; }
        public DateTimeOffset? ValidFrom { get; set; }
    }
}
