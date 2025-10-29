namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public partial class AuthorizedIdentifier
    {
        public AuthorizedIdentifierType Type { get; set; }
        public string Value { get; set; }
    }
    public enum AuthorizedIdentifierType
    {
        Nip,
        Pesel,
        Fingerprint
    }
}
