namespace KSeFPrinter.Validators;

/// <summary>
/// Wynik walidacji
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Czy walidacja zakończyła się sukcesem
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Lista błędów walidacji
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Lista ostrzeżeń walidacji
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Dodaje błąd do wyniku walidacji
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Dodaje ostrzeżenie do wyniku walidacji
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Tworzy wynik walidacji zakończonej sukcesem
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Tworzy wynik walidacji z błędem
    /// </summary>
    public static ValidationResult Failure(string error)
    {
        var result = new ValidationResult();
        result.AddError(error);
        return result;
    }

    /// <summary>
    /// Tworzy wynik walidacji z listą błędów
    /// </summary>
    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        var result = new ValidationResult();
        foreach (var error in errors)
        {
            result.AddError(error);
        }
        return result;
    }
}
