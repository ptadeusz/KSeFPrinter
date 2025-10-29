
using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Certificates
{
    public class PemCertificateInfo
    {
        public string Certificate { get; set; }
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public ICollection<PublicKeyCertificateUsage> Usage { get; set; }
    }
}
