
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Sessions.ActiveSessions
{
    public class Status
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public ICollection<string> Details { get; set; }
    }
}