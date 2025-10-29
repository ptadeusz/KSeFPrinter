using System;

namespace KSeF.Client.Core.Models.Permissions
{
    public class PermissionsAttachmentAllowedResponse
    {
        public bool IsAttachmentAllowed { get; set; }

        public DateTime? RevokedDate { get; set; }
    }
}
