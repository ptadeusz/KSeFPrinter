using KSeFPrinter.Models.Common;
using KSeFPrinter.Models.FA3;
using Microsoft.Extensions.Logging;

namespace KSeFPrinter.Validators;

/// <summary>
/// Walidator faktury - sprawdza poprawność biznesową danych
/// </summary>
public class InvoiceValidator
{
    private readonly ILogger<InvoiceValidator> _logger;

    public InvoiceValidator(ILogger<InvoiceValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Waliduje kompletną fakturę
    /// </summary>
    public ValidationResult Validate(Faktura faktura)
    {
        _logger.LogDebug("Rozpoczęcie walidacji faktury");

        var result = new ValidationResult();

        // Walidacja nagłówka
        ValidateNaglowek(faktura.Naglowek, result);

        // Walidacja podmiotów
        ValidatePodmiot(faktura.Podmiot1, "Podmiot1 (Sprzedawca)", result);
        ValidatePodmiot(faktura.Podmiot2, "Podmiot2 (Nabywca)", result);

        if (faktura.Podmiot3 != null)
        {
            ValidatePodmiot(faktura.Podmiot3, "Podmiot3", result);
        }

        // Walidacja danych faktury
        ValidateFa(faktura.Fa, result);

        // Walidacja sum kontrolnych (tylko jeśli Fa nie jest null)
        if (faktura.Fa != null)
        {
            ValidateSumyKontrolne(faktura.Fa, result);
        }

        if (result.IsValid)
        {
            _logger.LogInformation("Walidacja faktury zakończona sukcesem");
        }
        else
        {
            _logger.LogWarning("Walidacja faktury znalazła {ErrorCount} błędów i {WarningCount} ostrzeżeń",
                result.Errors.Count, result.Warnings.Count);
        }

        return result;
    }

    /// <summary>
    /// Waliduje kontekst faktury (z metadanymi KSeF)
    /// </summary>
    public ValidationResult ValidateContext(InvoiceContext context)
    {
        var result = Validate(context.Faktura);

        // Dodatkowa walidacja metadanych
        if (context.Metadata.Tryb == TrybWystawienia.Online)
        {
            if (string.IsNullOrEmpty(context.Metadata.NumerKSeF))
            {
                result.AddError("Faktura w trybie Online musi posiadać numer KSeF");
            }
            else if (!KsefNumberValidator.IsValid(context.Metadata.NumerKSeF, out var error))
            {
                result.AddError($"Nieprawidłowy numer KSeF: {error}");
            }
            else
            {
                // Sprawdź zgodność NIP z numeru KSeF z NIP sprzedawcy
                var nipZKseF = KsefNumberValidator.ExtractNIP(context.Metadata.NumerKSeF);
                var nipSprzedawcy = context.Faktura.Podmiot1.DaneIdentyfikacyjne.NIP;

                if (nipZKseF != nipSprzedawcy)
                {
                    result.AddWarning($"NIP w numerze KSeF ({nipZKseF}) różni się od NIP sprzedawcy ({nipSprzedawcy})");
                }
            }
        }

        return result;
    }

    private void ValidateNaglowek(Naglowek naglowek, ValidationResult result)
    {
        if (naglowek == null)
        {
            result.AddError("Brak nagłówka faktury");
            return;
        }

        if (naglowek.KodFormularza == null)
        {
            result.AddError("Brak kodu formularza");
            return;
        }

        if (naglowek.KodFormularza.KodSystemowy != "FA (3)")
        {
            result.AddError($"Nieprawidłowy kod systemowy: {naglowek.KodFormularza.KodSystemowy}. Oczekiwano: FA (3)");
        }

        if (naglowek.KodFormularza.WersjaSchemy != "1-0E")
        {
            result.AddWarning($"Wersja schematu: {naglowek.KodFormularza.WersjaSchemy} może różnić się od oczekiwanej: 1-0E");
        }

        if (string.IsNullOrWhiteSpace(naglowek.WariantFormularza))
        {
            result.AddError("Brak wariantu formularza");
        }
        else if (naglowek.WariantFormularza != "3")
        {
            result.AddError($"Nieprawidłowy wariant formularza: {naglowek.WariantFormularza}. Oczekiwano: 3");
        }
    }

    private void ValidatePodmiot(Podmiot podmiot, string nazwaPodmiotu, ValidationResult result)
    {
        if (podmiot == null)
        {
            result.AddError($"Brak {nazwaPodmiotu}");
            return;
        }

        if (podmiot.DaneIdentyfikacyjne == null)
        {
            result.AddError($"{nazwaPodmiotu}: Brak danych identyfikacyjnych");
            return;
        }

        // Walidacja NIP
        if (string.IsNullOrWhiteSpace(podmiot.DaneIdentyfikacyjne.NIP))
        {
            result.AddError($"{nazwaPodmiotu}: Brak numeru NIP");
        }
        else if (!IsValidNIP(podmiot.DaneIdentyfikacyjne.NIP))
        {
            result.AddError($"{nazwaPodmiotu}: Nieprawidłowy format NIP: {podmiot.DaneIdentyfikacyjne.NIP}");
        }

        // Walidacja nazwy
        if (string.IsNullOrWhiteSpace(podmiot.DaneIdentyfikacyjne.Nazwa))
        {
            result.AddError($"{nazwaPodmiotu}: Brak nazwy");
        }

        // Walidacja adresu
        if (podmiot.Adres == null)
        {
            result.AddError($"{nazwaPodmiotu}: Brak adresu");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(podmiot.Adres.KodKraju))
            {
                result.AddError($"{nazwaPodmiotu}: Brak kodu kraju");
            }
            else if (podmiot.Adres.KodKraju.Length != 2)
            {
                result.AddWarning($"{nazwaPodmiotu}: Kod kraju powinien mieć 2 znaki (ISO 3166-1)");
            }

            if (string.IsNullOrWhiteSpace(podmiot.Adres.AdresL1))
            {
                result.AddError($"{nazwaPodmiotu}: Brak adresu (linia 1)");
            }

            if (string.IsNullOrWhiteSpace(podmiot.Adres.AdresL2))
            {
                result.AddError($"{nazwaPodmiotu}: Brak adresu (linia 2)");
            }
        }
    }

    private void ValidateFa(Fa fa, ValidationResult result)
    {
        if (fa == null)
        {
            result.AddError("Brak sekcji Fa (dane faktury)");
            return;
        }

        // Walidacja waluty
        if (string.IsNullOrWhiteSpace(fa.KodWaluty))
        {
            result.AddError("Brak kodu waluty");
        }
        else if (!KodWaluty.IsValid(fa.KodWaluty))
        {
            result.AddWarning($"Nieprawidłowy format kodu waluty: {fa.KodWaluty}");
        }

        // Walidacja numeru faktury
        if (string.IsNullOrWhiteSpace(fa.P_2))
        {
            result.AddError("Brak numeru faktury (P_2)");
        }

        // Walidacja rodzaju faktury
        if (string.IsNullOrWhiteSpace(fa.RodzajFaktury))
        {
            result.AddError("Brak rodzaju faktury");
        }

        // Walidacja dat
        if (fa.P_1 == default)
        {
            result.AddError("Brak daty wystawienia faktury (P_1)");
        }

        // Walidacja wierszy
        if (fa.FaWiersz == null || fa.FaWiersz.Count == 0)
        {
            result.AddError("Faktura nie zawiera żadnych pozycji (FaWiersz)");
        }
        else
        {
            for (int i = 0; i < fa.FaWiersz.Count; i++)
            {
                ValidateFaWiersz(fa.FaWiersz[i], i + 1, result);
            }
        }

        // Walidacja kwot
        if (fa.P_15 <= 0)
        {
            result.AddWarning("Kwota brutto faktury (P_15) jest mniejsza lub równa 0");
        }
    }

    private void ValidateFaWiersz(FaWiersz wiersz, int numer, ValidationResult result)
    {
        if (wiersz == null)
        {
            result.AddError($"Wiersz {numer}: Brak danych");
            return;
        }

        if (string.IsNullOrWhiteSpace(wiersz.P_7))
        {
            result.AddError($"Wiersz {numer}: Brak nazwy towaru/usługi (P_7)");
        }

        if (string.IsNullOrWhiteSpace(wiersz.P_12))
        {
            result.AddError($"Wiersz {numer}: Brak stawki VAT (P_12)");
        }
    }

    private void ValidateSumyKontrolne(Fa fa, ValidationResult result)
    {
        // Suma wartości netto z wierszy
        decimal sumaNetto = fa.FaWiersz?.Sum(w => w.P_11) ?? 0;

        // Tolerancja 0.01 dla zaokrągleń
        const decimal tolerance = 0.01m;

        if (Math.Abs(fa.P_13_1 - sumaNetto) > tolerance)
        {
            result.AddWarning(
                $"Suma wartości netto (P_13_1={fa.P_13_1}) nie zgadza się z sumą wartości z wierszy ({sumaNetto})"
            );
        }

        // Sprawdzenie P_15 = P_13_1 + P_14_1
        decimal expectedP15 = fa.P_13_1 + fa.P_14_1;
        if (Math.Abs(fa.P_15 - expectedP15) > tolerance)
        {
            result.AddWarning(
                $"Kwota brutto (P_15={fa.P_15}) nie zgadza się z sumą netto + VAT ({expectedP15})"
            );
        }

        // Jeśli jest rozliczenie, sprawdź DoZaplaty
        if (fa.Rozliczenie != null)
        {
            decimal expectedDoZaplaty = fa.P_15 + fa.Rozliczenie.SumaObciazen - fa.Rozliczenie.SumaOdliczen;
            if (Math.Abs(fa.Rozliczenie.DoZaplaty - expectedDoZaplaty) > tolerance)
            {
                result.AddWarning(
                    $"Kwota do zapłaty (DoZaplaty={fa.Rozliczenie.DoZaplaty}) nie zgadza się z obliczoną ({expectedDoZaplaty})"
                );
            }
        }
    }

    private bool IsValidNIP(string nip)
    {
        // Usunięcie myślników i spacji
        nip = nip.Replace("-", "").Replace(" ", "");

        // NIP powinien mieć 10 cyfr
        if (nip.Length != 10 || !nip.All(char.IsDigit))
        {
            return false;
        }

        // Algorytm walidacji sumy kontrolnej NIP
        int[] weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };
        int sum = 0;

        for (int i = 0; i < 9; i++)
        {
            sum += (nip[i] - '0') * weights[i];
        }

        int checksum = sum % 11;
        int lastDigit = nip[9] - '0';

        return checksum == lastDigit;
    }
}
