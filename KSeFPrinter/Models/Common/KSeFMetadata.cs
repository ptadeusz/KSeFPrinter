namespace KSeFPrinter.Models.Common;

/// <summary>
/// Metadane KSeF dla faktury
/// </summary>
public class KSeFMetadata
{
    /// <summary>
    /// Numer KSeF faktury (format: 9999999999-RRRRMMDD-FFFFFFFFFFFF-FF)
    /// Null dla faktur offline bez numeru
    /// </summary>
    public string? NumerKSeF { get; set; }

    /// <summary>
    /// Tryb wystawienia faktury
    /// </summary>
    public TrybWystawienia Tryb { get; set; }

    /// <summary>
    /// Czy faktura została wysłana do systemu KSeF
    /// </summary>
    public bool CzyWyslanaDoKSeF => !string.IsNullOrEmpty(NumerKSeF);
}

/// <summary>
/// Tryb wystawienia faktury
/// </summary>
public enum TrybWystawienia
{
    /// <summary>
    /// Tryb online - faktura wysłana do KSeF i posiada numer
    /// </summary>
    Online,

    /// <summary>
    /// Tryb offline - faktura nie została jeszcze wysłana do KSeF
    /// </summary>
    Offline,

    /// <summary>
    /// Tryb offline24 - predefiniowany offline (do 24h)
    /// </summary>
    Offline24,

    /// <summary>
    /// Tryb awaryjny - awaria systemu KSeF
    /// </summary>
    Awaryjny
}
