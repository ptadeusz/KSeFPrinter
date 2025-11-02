using System.Text.Json;
using System.Text.Json.Serialization;

namespace KSeFPrinter.Models.License;

/// <summary>
/// JsonConverter dla DateTime - zapewnia spójne formatowanie bez milisekund
/// KRYTYCZNE: Ten sam konwerter musi być używany w:
/// - KSeFPrinter (podczas weryfikacji licencji)
/// - KSeFConnector.Core (podczas weryfikacji licencji)
/// - KSeFConnector.LicenseGenerator (podczas generowania licencji)
///
/// UWAGA: Plik ten musi być IDENTYCZNY we wszystkich projektach!
/// Lokalizacje:
/// - KSeFPrinter/Models/License/UnixTimestampDateTimeConverter.cs
/// - KSeFConnector.Core/Models/UnixTimestampDateTimeConverter.cs
/// - KSeFConnector.LicenseGenerator/Models/UnixTimestampDateTimeConverter.cs
/// </summary>
public class UnixTimestampDateTimeConverter : JsonConverter<DateTime>
{
    // Format: ISO 8601 bez milisekund, zawsze UTC
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return DateTime.MinValue;
        }

        // Parsuj różne formaty (z milisekundami i bez)
        // Akceptujemy: "2026-12-31T00:00:00Z" oraz "2026-12-31T00:00:00.0000000Z"
        if (DateTime.TryParse(dateString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
        {
            return result.ToUniversalTime();
        }

        throw new JsonException($"Unable to parse DateTime: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // ZAWSZE zapisuj w UTC bez milisekund
        var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

        // Obetnij milisekundy dla spójności
        var withoutMilliseconds = new DateTime(
            utcValue.Year,
            utcValue.Month,
            utcValue.Day,
            utcValue.Hour,
            utcValue.Minute,
            utcValue.Second,
            0, // milisekundy = 0
            DateTimeKind.Utc
        );

        // ZAWSZE ten sam format: "2026-12-31T00:00:00Z"
        writer.WriteStringValue(withoutMilliseconds.ToString(DateTimeFormat));
    }
}
