using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.Person
{
    public class PersonalPermissionsQueryRequest
    {
        public ContextIdentifier ContextIdentifier { get; set; }
        public TargetIdentifier TargetIdentifier { get; set; }
        public List<PersonPermissionType> PermissionTypes { get; set; }
        public PermissionState? PermissionState { get; set; }
    }

    public partial class ContextIdentifier
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