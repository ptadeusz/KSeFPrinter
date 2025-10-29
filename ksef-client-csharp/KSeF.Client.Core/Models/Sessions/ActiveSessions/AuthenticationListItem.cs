
using System;

namespace KSeF.Client.Core.Models.Sessions.ActiveSessions
{
    public class AuthenticationListItem
    {
        public DateTimeOffset StartDate { get; set; }
        public AuthenticationMethodEnum AuthenticationMethod { get; set; }
        public Status Status { get; set; }
        public bool IsTokenRedeemed { get; set; }
        public DateTimeOffset LastTokenRefreshDate { get; set; }
        public DateTimeOffset RefreshTokenValidUntil { get; set; }
        public string ReferenceNumber { get; set; }
        public bool IsCurrent { get; set; }
    }
}
