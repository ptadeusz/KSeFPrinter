namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public class TargetIdentifier
    {
        public TargetIdentifierType Type { get; set; }
        public string Value { get; set; }
    }
    public enum TargetIdentifierType
    {
        Nip,
        AllPartners
    }
}
