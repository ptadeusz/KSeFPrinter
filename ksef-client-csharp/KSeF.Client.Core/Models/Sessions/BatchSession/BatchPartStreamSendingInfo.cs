using System.IO;

namespace KSeF.Client.Core.Models.Sessions.BatchSession
{
    public class BatchPartStreamSendingInfo
    {
        public Stream DataStream { get; set; }
        public FileMetadata Metadata { get; set; }
        public int OrdinalNumber { get; set; }
    }
}
