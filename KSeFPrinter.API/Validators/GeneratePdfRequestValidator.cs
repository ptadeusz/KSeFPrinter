using FluentValidation;
using KSeFPrinter.API.Models;

namespace KSeFPrinter.API.Validators;

/// <summary>
/// Validator dla GeneratePdfRequest
/// </summary>
public class GeneratePdfRequestValidator : AbstractValidator<GeneratePdfRequest>
{
    public GeneratePdfRequestValidator()
    {
        RuleFor(x => x.XmlContent)
            .NotEmpty()
            .WithMessage("XML content is required");

        RuleFor(x => x.XmlContent)
            .MaximumLength(10_000_000) // 10 MB
            .WithMessage("XML content too large (max 10 MB)")
            .When(x => !string.IsNullOrEmpty(x.XmlContent));

        RuleFor(x => x.XmlContent)
            .Must(BeValidXmlOrBase64)
            .WithMessage("Invalid XML format - must start with '<?xml' or be valid base64")
            .When(x => !string.IsNullOrEmpty(x.XmlContent));

        RuleFor(x => x.KSeFNumber)
            .Matches(@"^\d{10}-\d{8}-[A-F0-9]{12}-\d{2}$")
            .WithMessage("Invalid KSeF number format (expected: XXXXXXXXXX-YYYYMMDD-XXXXXXXXXXXX-XX)")
            .When(x => !string.IsNullOrEmpty(x.KSeFNumber));

        RuleFor(x => x.ReturnFormat)
            .Must(format => format.Equals("file", StringComparison.OrdinalIgnoreCase) ||
                           format.Equals("base64", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Invalid return format. Allowed values: 'file', 'base64'");

        // Source File Metadata validation (opcjonalne)
        When(x => x.SourceFile != null, () =>
        {
            RuleFor(x => x.SourceFile!.FileName)
                .NotEmpty()
                .WithMessage("File name is required when source file metadata is provided");

            RuleFor(x => x.SourceFile!.FileName)
                .MaximumLength(255)
                .WithMessage("File name too long (max 255 characters)")
                .When(x => !string.IsNullOrEmpty(x.SourceFile!.FileName));
        });
    }

    private bool BeValidXmlOrBase64(GeneratePdfRequest request, string xmlContent)
    {
        if (request.IsBase64)
        {
            // Sprawdź czy to valid base64
            try
            {
                var bytes = Convert.FromBase64String(xmlContent);
                var decoded = System.Text.Encoding.UTF8.GetString(bytes);
                return decoded.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        else
        {
            // Plain text - musi zaczynać się od <?xml
            return xmlContent.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
        }
    }
}
