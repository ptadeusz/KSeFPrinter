using System.Runtime.Serialization;

namespace KSeF.Client.Core.Models.Authorization
{
    public class SubjectIdentifier
    {
        public SubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum SubjectIdentifierType
    {
        None,
        [EnumMember(Value = "nip")]
        Nip,
        [EnumMember(Value = "pesel")]
        Pesel,
        [EnumMember(Value = "fingerprint")]
        Fingerprint,
        [EnumMember(Value = "token")]
        Token
    }
}