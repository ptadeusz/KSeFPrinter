namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public class SubunitIdentifier
    {
        public SubunitIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum SubunitIdentifierType
    {
        InternalId,
        Nip
    }
}
