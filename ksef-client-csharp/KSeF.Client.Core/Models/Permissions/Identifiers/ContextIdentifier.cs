namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public class ContextIdentifier
    {
        public ContextIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum ContextIdentifierType
    {
        Nip,
        InternalId,
    }
}
