using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Certificates
{
    public class CertificateListRequest
    {
        public ICollection<string> CertificateSerialNumbers { get; set; }
    }
}
