using FluentValidation;
using KSeFPrinter.API.Models;

namespace KSeFPrinter.API.Validators;

/// <summary>
/// Validator dla GenerateQrCodeRequest
/// </summary>
public class GenerateQrCodeRequestValidator : AbstractValidator<GenerateQrCodeRequest>
{
    private static readonly string[] AllowedFormats = { "svg", "base64", "both" };

    public GenerateQrCodeRequestValidator()
    {
        RuleFor(x => x.KSeFNumber)
            .NotEmpty()
            .WithMessage("KSeF number is required");

        RuleFor(x => x.KSeFNumber)
            .Matches(@"^\d{10}-\d{8}-[A-F0-9]{12}-\d{2}$")
            .WithMessage("Invalid KSeF number format (expected: XXXXXXXXXX-YYYYMMDD-XXXXXXXXXXXX-XX)")
            .When(x => !string.IsNullOrEmpty(x.KSeFNumber));

        RuleFor(x => x.Format)
            .Must(format => AllowedFormats.Contains(format.ToLower()))
            .WithMessage($"Invalid format. Allowed values: {string.Join(", ", AllowedFormats)}");

        RuleFor(x => x.Size)
            .InclusiveBetween(50, 1000)
            .WithMessage("QR code size must be between 50 and 1000 pixels");
    }
}
