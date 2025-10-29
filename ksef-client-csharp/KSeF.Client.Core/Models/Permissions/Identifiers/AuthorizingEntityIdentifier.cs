namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public class AuthorizingEntityIdentifier
    {
        public AuthorizingEntityIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum AuthorizingEntityIdentifierType
    {
        Nip
    }
}
