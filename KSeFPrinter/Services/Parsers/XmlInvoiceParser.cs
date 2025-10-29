using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using KSeFPrinter.Exceptions;
using KSeFPrinter.Interfaces;
using KSeFPrinter.Models.Common;
using KSeFPrinter.Models.FA3;
using Microsoft.Extensions.Logging;

namespace KSeFPrinter.Services.Parsers;

/// <summary>
/// Parser XML faktur KSeF FA(3) v1-0E
/// </summary>
public class XmlInvoiceParser : IXmlInvoiceParser
{
    private const string ExpectedNamespace = "http://crd.gov.pl/wzor/2025/06/25/13775/";
    private const string ExpectedKodSystemowy = "FA (3)";
    private const string ExpectedWersjaSchemy = "1-0E";

    // Pattern numeru KSeF: 9999999999-RRRRMMDD-FFFFFFFFFFFF-FF
    private static readonly Regex KSeFNumberRegex = new(
        @"^\d{10}-\d{8}-[0-9A-F]{12}-[0-9A-F]{2}$",
        RegexOptions.Compiled
    );

    private readonly ILogger<XmlInvoiceParser> _logger;
    private readonly XmlSerializer _serializer;

    public XmlInvoiceParser(ILogger<XmlInvoiceParser> logger)
    {
        _logger = logger;
        _serializer = new XmlSerializer(typeof(Faktura));
    }

    /// <inheritdoc />
    public async Task<InvoiceContext> ParseFromFileAsync(string xmlFilePath, string? numerKSeF = null)
    {
        _logger.LogInformation("Rozpoczęcie parsowania faktury z pliku: {FilePath}", xmlFilePath);

        if (!File.Exists(xmlFilePath))
        {
            throw new InvoiceParseException($"Plik nie istnieje: {xmlFilePath}", xmlFilePath);
        }

        try
        {
            var xmlContent = await File.ReadAllTextAsync(xmlFilePath);
            return await ParseFromStringAsync(xmlContent, numerKSeF);
        }
        catch (InvoiceParseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytu pliku: {FilePath}", xmlFilePath);
            throw new InvoiceParseException(
                $"Nie można odczytać pliku XML: {ex.Message}",
                xmlFilePath,
                ex
            );
        }
    }

    /// <inheritdoc />
#pragma warning disable CS1998 // Async method lacks 'await' operators
    public async Task<InvoiceContext> ParseFromStringAsync(string xmlContent, string? numerKSeF = null)
#pragma warning restore CS1998
    {
        _logger.LogDebug("Rozpoczęcie parsowania faktury z XML string");

        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            throw new InvoiceParseException("Zawartość XML jest pusta");
        }

        // Walidacja schematu
        ValidateSchemaInternal(xmlContent);

        // Deserializacja XML
        Faktura faktura;
        try
        {
            using var reader = new StringReader(xmlContent);
            faktura = (_serializer.Deserialize(reader) as Faktura)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd deserializacji XML");
            throw new InvoiceParseException(
                "Nie można zdeserializować XML do modelu faktury. Sprawdź poprawność struktury XML.",
                null,
                ex
            );
        }

        if (faktura == null)
        {
            throw new InvoiceParseException("Deserializacja zwróciła null - nieprawidłowa struktura XML");
        }

        // Wykryj numer KSeF z XML lub użyj przekazanego
        var detectedNumerKSeF = numerKSeF ?? DetectKSeFNumber(xmlContent);

        // Określ tryb wystawienia
        var tryb = DetermineTrybWystawienia(detectedNumerKSeF);

        // Utworzenie kontekstu
        var context = new InvoiceContext
        {
            Faktura = faktura,
            Metadata = new KSeFMetadata
            {
                NumerKSeF = detectedNumerKSeF,
                Tryb = tryb
            },
            OriginalXml = xmlContent,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Pomyślnie sparsowano fakturę. Numer: {NumerFaktury}, Tryb: {Tryb}, NIP: {NIP}",
            faktura.Fa.P_2,
            tryb,
            faktura.Podmiot1.DaneIdentyfikacyjne.NIP
        );

        return context;
    }

    /// <inheritdoc />
    public async Task<InvoiceContext> ParseFromStreamAsync(Stream xmlStream, string? numerKSeF = null)
    {
        _logger.LogDebug("Rozpoczęcie parsowania faktury ze strumienia");

        if (xmlStream == null || !xmlStream.CanRead)
        {
            throw new InvoiceParseException("Strumień XML jest null lub nie można z niego czytać");
        }

        try
        {
            using var reader = new StreamReader(xmlStream);
            var xmlContent = await reader.ReadToEndAsync();
            return await ParseFromStringAsync(xmlContent, numerKSeF);
        }
        catch (InvoiceParseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytu strumienia XML");
            throw new InvoiceParseException(
                $"Nie można odczytać strumienia XML: {ex.Message}",
                null,
                ex
            );
        }
    }

    /// <inheritdoc />
    public bool ValidateSchema(string xmlContent)
    {
        try
        {
            ValidateSchemaInternal(xmlContent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Wewnętrzna walidacja schematu XML
    /// </summary>
    private void ValidateSchemaInternal(string xmlContent)
    {
        try
        {
            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;

            if (root == null)
            {
                throw new InvoiceParseException("XML nie zawiera elementu głównego");
            }

            // Sprawdź namespace
            if (root.Name.NamespaceName != ExpectedNamespace)
            {
                throw new InvoiceParseException(
                    $"Nieprawidłowy namespace. Oczekiwano: {ExpectedNamespace}, otrzymano: {root.Name.NamespaceName}"
                );
            }

            // Sprawdź czy to element Faktura
            if (root.Name.LocalName != "Faktura")
            {
                throw new InvoiceParseException(
                    $"Główny element powinien być 'Faktura', otrzymano: {root.Name.LocalName}"
                );
            }

            // Sprawdź nagłówek
            var ns = XNamespace.Get(ExpectedNamespace);
            var naglowek = root.Element(ns + "Naglowek");
            if (naglowek == null)
            {
                throw new InvoiceParseException("Brak elementu Naglowek w XML");
            }

            var kodFormularza = naglowek.Element(ns + "KodFormularza");
            if (kodFormularza == null)
            {
                throw new InvoiceParseException("Brak elementu KodFormularza w Naglowek");
            }

            var kodSystemowy = kodFormularza.Attribute("kodSystemowy")?.Value;
            var wersjaSchemy = kodFormularza.Attribute("wersjaSchemy")?.Value;

            if (kodSystemowy != ExpectedKodSystemowy)
            {
                throw new InvoiceParseException(
                    $"Nieprawidłowy kodSystemowy. Oczekiwano: {ExpectedKodSystemowy}, otrzymano: {kodSystemowy}"
                );
            }

            if (wersjaSchemy != ExpectedWersjaSchemy)
            {
                _logger.LogWarning(
                    "Wersja schematu różni się od oczekiwanej. Oczekiwano: {Expected}, otrzymano: {Actual}",
                    ExpectedWersjaSchemy,
                    wersjaSchemy
                );
            }

            _logger.LogDebug("Walidacja schematu zakończona pomyślnie");
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Błąd parsowania XML");
            throw new InvoiceParseException("XML jest nieprawidłowy lub uszkodzony", null, ex);
        }
    }

    /// <summary>
    /// Wykrywa numer KSeF w XML (może być w różnych miejscach)
    /// </summary>
    private string? DetectKSeFNumber(string xmlContent)
    {
        try
        {
            // Szukaj wzorca numeru KSeF w całym XML
            var match = KSeFNumberRegex.Match(xmlContent);
            if (match.Success)
            {
                var numer = match.Value;
                _logger.LogDebug("Wykryto numer KSeF: {NumerKSeF}", numer);
                return numer;
            }

            _logger.LogDebug("Nie wykryto numeru KSeF w XML");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Błąd podczas wykrywania numeru KSeF");
            return null;
        }
    }

    /// <summary>
    /// Określa tryb wystawienia na podstawie obecności numeru KSeF
    /// </summary>
    private TrybWystawienia DetermineTrybWystawienia(string? numerKSeF)
    {
        if (!string.IsNullOrEmpty(numerKSeF))
        {
            return TrybWystawienia.Online;
        }

        // Domyślnie zakładamy tryb offline
        return TrybWystawienia.Offline;
    }
}
