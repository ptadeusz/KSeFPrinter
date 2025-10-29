namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public class AuthorizedEntityIdentifier
    {
        public AuthorizedEntityIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum AuthorizedEntityIdentifierType
    {
        Nip,
        PeppolId
    }
}
