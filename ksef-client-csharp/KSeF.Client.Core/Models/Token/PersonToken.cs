using System;

namespace KSeF.Client.Core.Models.Token
{
    public sealed class PersonToken
    {
        public string Issuer { get; set; }
        public string[] Audiences { get; set; } = Array.Empty<string>();
        public DateTimeOffset? IssuedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }

        public string[] Roles { get; set; } = Array.Empty<string>();

        public string TokenType { get; set; }
        public string ContextIdType { get; set; }
        public string ContextIdValue { get; set; }
        public string AuthMethod { get; set; }
        public string AuthRequestNumber { get; set; }
        public TokenSubjectDetails SubjectDetails { get; set; }
        public string[] Permissions { get; set; } = Array.Empty<string>();
        public string[] PermissionsExcluded { get; set; } = Array.Empty<string>();
        public string[] RolesRaw { get; set; } = Array.Empty<string>();
        public string[] PermissionsEffective { get; set; } = Array.Empty<string>();
        public TokenIppPolicy IpPolicy { get; set; }
    }
}
