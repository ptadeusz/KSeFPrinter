using KSeF.Client.Core.Models.Permissions.Identifiers;
using System;

namespace KSeF.Client.Core.Models.Permissions
{
    public class SubunitPermission
    {
        public string Id { get; set; }
        public AuthorizedIdentifier AuthorizedIdentifier { get; set; }
        public SubunitIdentifier SubunitIdentifier { get; set; }
        public AuthorIdentifier AuthorIdentifier { get; set; }
        public SubunitPermissionType PermissionScope { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
    }

    public enum SubunitPermissionType
    {
        CredentialsManage
    }
}
