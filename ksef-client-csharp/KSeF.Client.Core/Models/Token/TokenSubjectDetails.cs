using System;

namespace KSeF.Client.Core.Models.Token
{
    public sealed class TokenSubjectDetails
    {
        public TokenSubjectIdentifier SubjectIdentifier { get; set; }
        public string[] GivenNames { get; set; } = Array.Empty<string>();
        public string Surname { get; set; }
        public string SerialNumber { get; set; }
        public string CommonName { get; set; }
        public string CountryName { get; set; }
    }
}
