using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Sessions
{

    public class SessionFailedInvoicesResponse
    {
        public ICollection<SessionInvoice> Invoices { get; set; }
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
    }
}