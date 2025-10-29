namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public class SubordinateEntityIdentifier
    {
        public SubordinateEntityIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum SubordinateEntityIdentifierType
    {
        Nip,
    }
}
