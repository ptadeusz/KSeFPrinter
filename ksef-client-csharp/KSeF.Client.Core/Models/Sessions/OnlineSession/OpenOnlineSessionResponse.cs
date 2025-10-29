using System;

namespace KSeF.Client.Core.Models.Sessions.OnlineSession
{
    public class OpenOnlineSessionResponse
    {
        public string ReferenceNumber { get; set; }

        public DateTimeOffset ValidUntil { get; set; }
    }
}
