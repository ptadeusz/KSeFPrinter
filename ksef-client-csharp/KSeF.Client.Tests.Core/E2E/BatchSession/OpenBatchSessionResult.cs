namespace KSeF.Client.Tests.Core.E2E.BatchSession;

public partial class BatchSessionE2ETests
{
    private sealed record OpenBatchSessionResult(
        string ReferenceNumber,
        Client.Core.Models.Sessions.BatchSession.OpenBatchSessionResponse OpenBatchSessionResponse,
        List<Client.Core.Models.Sessions.BatchSession.BatchPartSendingInfo> EncryptedParts
    );
}