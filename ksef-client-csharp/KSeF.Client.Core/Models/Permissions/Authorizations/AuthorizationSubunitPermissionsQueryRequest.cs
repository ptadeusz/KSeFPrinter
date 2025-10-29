namespace KSeF.Client.Core.Models.Permissions.Authorizations
{
    public class AuthorizationSubunitPermissionsQueryRequest
    {
        public SubunitPermissionsSubunitIdentifier SubunitIdentifier { get; set; }
    }
    public class SubunitPermissionsSubunitIdentifier
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
