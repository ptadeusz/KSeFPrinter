namespace KSeFPrinter.Exceptions;

/// <summary>
/// Wyjątek rzucany podczas błędu parsowania faktury XML
/// </summary>
public class InvoiceParseException : Exception
{
    /// <summary>
    /// Ścieżka do pliku XML (jeśli dotyczy)
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Fragment XML, który spowodował błąd (jeśli dostępny)
    /// </summary>
    public string? XmlFragment { get; }

    public InvoiceParseException(string message) : base(message)
    {
    }

    public InvoiceParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InvoiceParseException(string message, string? filePath, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }

    public InvoiceParseException(string message, string? filePath, string? xmlFragment, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
        XmlFragment = xmlFragment;
    }
}
