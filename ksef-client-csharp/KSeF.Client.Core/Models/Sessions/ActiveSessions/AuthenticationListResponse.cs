
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Sessions.ActiveSessions
{
    public class AuthenticationListResponse
    {
        public string ContinuationToken { get; set; }
        public ICollection<AuthenticationListItem> Items { get; set; }
    }
}
