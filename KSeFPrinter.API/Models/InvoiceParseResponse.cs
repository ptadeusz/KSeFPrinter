namespace KSeFPrinter.API.Models;

/// <summary>
/// Response z danymi sparsowanej faktury
/// </summary>
public class InvoiceParseResponse
{
    /// <summary>
    /// Numer faktury
    /// </summary>
    public string InvoiceNumber { get; set; } = null!;

    /// <summary>
    /// Data wystawienia
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Tryb wystawienia (Online/Offline)
    /// </summary>
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Numer KSeF (jeśli tryb Online)
    /// </summary>
    public string? KSeFNumber { get; set; }

    /// <summary>
    /// Dane sprzedawcy
    /// </summary>
    public SellerInfo Seller { get; set; } = null!;

    /// <summary>
    /// Dane nabywcy
    /// </summary>
    public BuyerInfo Buyer { get; set; } = null!;

    /// <summary>
    /// Pozycje faktury
    /// </summary>
    public List<InvoiceLineInfo> Lines { get; set; } = new();

    /// <summary>
    /// Kwoty podsumowania
    /// </summary>
    public SummaryInfo Summary { get; set; } = null!;

    /// <summary>
    /// Informacje o płatności (jeśli dostępne)
    /// </summary>
    public PaymentInfo? Payment { get; set; }
}

public class SellerInfo
{
    public string Name { get; set; } = null!;
    public string Nip { get; set; } = null!;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class BuyerInfo
{
    public string Name { get; set; } = null!;
    public string Nip { get; set; } = null!;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class InvoiceLineInfo
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = null!;
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal NetAmount { get; set; }
    public string VatRate { get; set; } = null!;
}

public class SummaryInfo
{
    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }
    public string Currency { get; set; } = null!;
}

public class PaymentInfo
{
    public List<DateTime>? DueDates { get; set; }
    public string? PaymentMethod { get; set; }
    public List<string>? BankAccounts { get; set; }
    public List<string>? FactorBankAccounts { get; set; }
}
