namespace KSeF.Client.Core.Models.Sessions
{
    public class EncryptionData
    {
        public byte[] CipherKey { get; set; }
        public byte[] CipherIv { get; set; }
        public EncryptionInfo EncryptionInfo { get; set; }
    }
}
