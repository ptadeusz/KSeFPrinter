
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Sessions
{
    public class SessionsListResponse
    {
        public string ContinuationToken { get; set; }
        public ICollection<Session> Sessions { get; set; }
    }
}
