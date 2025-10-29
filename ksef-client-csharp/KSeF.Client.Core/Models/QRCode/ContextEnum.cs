using System.Runtime.Serialization;

namespace KSeF.Client.Core.Models.QRCode
{
    public enum ContextIdentifierType
    {
        [EnumMember(Value = "Nip")]
        Nip,
        [EnumMember(Value = "InternalId")]
        InternalId,
        [EnumMember(Value = "NipVatUe")]
        NipVatUe
    }
}
