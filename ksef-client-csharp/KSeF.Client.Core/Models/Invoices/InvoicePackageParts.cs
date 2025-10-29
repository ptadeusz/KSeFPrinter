
using KSeF.Client.Core.Models.Sessions.BatchSession;
using System;

namespace KSeF.Client.Core.Models.Invoices
{
    public class InvoicePackageParts : PackagePartSignatureInitResponseType
    {
        public DateTimeOffset ExpirationDate { get; set; }
    }
}
