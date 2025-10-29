using KSeF.Client.Core.Models.Sessions;

public interface ISendInvoiceOnlineSessionRequestBuilder
{
    ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash WithInvoiceHash(string documentHash, long documentSize);
}

public interface ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash
{
    ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash WithEncryptedDocumentHash(string encryptedDocumentHash, long encryptedDocumentSize);
}

public interface ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash
{
    ISendInvoiceOnlineSessionRequestBuilderBuild WithEncryptedDocumentContent(string encryptedDocumentContent);
}

public interface ISendInvoiceOnlineSessionRequestBuilderBuild
{
    ISendInvoiceOnlineSessionRequestBuilderBuild WithHashOfCorrectedInvoice(string hashOfCorrectedInvoice);
    ISendInvoiceOnlineSessionRequestBuilderBuild WithOfflineMode(bool offlineMode);
    SendInvoiceRequest Build();
}

internal class SendInvoiceOnlineSessionRequestBuilderImpl
    : ISendInvoiceOnlineSessionRequestBuilder
    , ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash
    , ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash
    , ISendInvoiceOnlineSessionRequestBuilderBuild
{
    private string _documentHash;
    private long _documentSize;
    private string _encryptedDocumentHash;
    private long _encryptedDocumentSize;
    private string _encryptedDocumentContent;
    private string _hashOfCorrectedInvoice;
    private bool _offlineMode = false;

    private SendInvoiceOnlineSessionRequestBuilderImpl() { }

    public static ISendInvoiceOnlineSessionRequestBuilder Create() => new SendInvoiceOnlineSessionRequestBuilderImpl();

    public ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash WithInvoiceHash(string documentHash, long documentSize)
    {
        if (string.IsNullOrWhiteSpace(documentHash) || documentSize < 0)
            throw new ArgumentException("Parametry InvoiceHash są nieprawidłowe.");

        _documentHash = documentHash;
        _documentSize = documentSize;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash WithEncryptedDocumentHash(string encryptedDocumentHash, long encryptedDocumentSize)
    {
        if (string.IsNullOrWhiteSpace(encryptedDocumentHash) || encryptedDocumentSize < 0)
            throw new ArgumentException("Parametry EncryptedInvoiceHash są nieprawidłowe.");

        _encryptedDocumentHash = encryptedDocumentHash;
        _encryptedDocumentSize = encryptedDocumentSize;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderBuild WithEncryptedDocumentContent(string encryptedDocumentContent)
    {
        if (string.IsNullOrWhiteSpace(encryptedDocumentContent))
            throw new ArgumentException("EncryptedInvoiceContent nie może być puste ani null.");

        _encryptedDocumentContent = encryptedDocumentContent;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderBuild WithHashOfCorrectedInvoice(string hashOfCorrectedInvoice)
    {
        if (string.IsNullOrWhiteSpace(hashOfCorrectedInvoice))
            throw new ArgumentException("HashOfCorrectedInvoice nie może być puste ani null.");

        _hashOfCorrectedInvoice = hashOfCorrectedInvoice;
        return this;
    }

    public ISendInvoiceOnlineSessionRequestBuilderBuild WithOfflineMode(bool offlineMode)
    {
        _offlineMode = offlineMode;
        return this;
    }

    public SendInvoiceRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_documentHash))
            throw new InvalidOperationException("InvoiceHash jest wymagany.");
        if (string.IsNullOrWhiteSpace(_encryptedDocumentHash))
            throw new InvalidOperationException("EncryptedInvoiceHash jest wymagany.");
        if (string.IsNullOrWhiteSpace(_encryptedDocumentContent))
            throw new InvalidOperationException("EncryptedInvoiceContent jest wymagany.");

        return new SendInvoiceRequest
        {
            InvoiceHash = _documentHash,
            InvoiceSize = _documentSize,
            EncryptedInvoiceHash = _encryptedDocumentHash,
            EncryptedInvoiceSize = _encryptedDocumentSize,
            EncryptedInvoiceContent = _encryptedDocumentContent,
            HashOfCorrectedInvoice = _hashOfCorrectedInvoice,
            OfflineMode = _offlineMode
        };
    }
}

public static class SendInvoiceOnlineSessionRequestBuilder
{
    public static ISendInvoiceOnlineSessionRequestBuilder Create() =>
        SendInvoiceOnlineSessionRequestBuilderImpl.Create();
}