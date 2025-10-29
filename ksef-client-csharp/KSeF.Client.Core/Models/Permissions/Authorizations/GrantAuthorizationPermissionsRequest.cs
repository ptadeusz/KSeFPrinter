namespace KSeF.Client.Core.Models.Permissions.Authorizations
{
    public class GrantAuthorizationPermissionsRequest
    {
        public SubjectIdentifier SubjectIdentifier { get; set; }
        public AuthorizationPermissionType Permission { get; set; }
        public string Description { get; set; }
    }

    public enum AuthorizationPermissionType
    {
        SelfInvoicing,
        RRInvoicing,
        TaxRepresentative,
        PefInvoicing
    }

    public partial class SubjectIdentifier
    {
        public SubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum SubjectIdentifierType
    {
        Nip,
        PeppolId
    }
}
