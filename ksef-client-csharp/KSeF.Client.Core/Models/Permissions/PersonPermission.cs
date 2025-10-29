using KSeF.Client.Core.Models.Permissions.Identifiers;
using System;

namespace KSeF.Client.Core.Models.Permissions
{
    public class PersonPermission
    {
        public string Id { get; set; }
        public AuthorizedIdentifier AuthorizedIdentifier { get; set; }
        public ContextIdentifier ContextIdentifier { get; set; }
        public TargetIdentifier TargetIdentifier { get; set; }
        public AuthorIdentifier AuthorIdentifier { get; set; }
        public string PermissionScope { get; set; }
        public string Description { get; set; }
        public string PermissionState { get; set; }
        public DateTime StartDate { get; set; }
        public bool CanDelegate { get; set; }
    }
}