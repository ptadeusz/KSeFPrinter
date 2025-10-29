namespace KSeFPrinter.Models.Common;

/// <summary>
/// Rodzaj faktury zgodnie z KSeF
/// </summary>
public enum RodzajFaktury
{
    /// <summary>
    /// Faktura VAT
    /// </summary>
    VAT,

    /// <summary>
    /// Faktura korygująca
    /// </summary>
    KOR,

    /// <summary>
    /// Faktura rozliczeniowa
    /// </summary>
    ROZ,

    /// <summary>
    /// Faktura uproszczona
    /// </summary>
    UPR,

    /// <summary>
    /// Faktura zaliczkowa
    /// </summary>
    ZAL,

    /// <summary>
    /// Nota korygująca
    /// </summary>
    NOT,

    /// <summary>
    /// Faktura wewnętrzna
    /// </summary>
    WEW
}
