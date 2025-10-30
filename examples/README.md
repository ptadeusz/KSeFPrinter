# Przykłady KSeF Printer

Ten katalog zawiera przykładowe pliki XML faktur oraz wygenerowane z nich PDF-y.

## Struktura

### `xml/` - Przykładowe faktury XML

**FakturaTEST017.xml**
- Tryb: Offline
- Numer faktury: FA811/05/2025
- Sprzedawca: Elektrownia S.A. (NIP: 7309436499)
- Nabywca: F.H.U. Jan Kowalski (NIP: 1187654321)
- Kwota brutto: zgodnie z danymi w XML
- Format: KSeF FA(3) v1-0E

**6511153259-20251015-010020140418-0D.xml**
- Tryb: Online
- Numer KSeF: 6511153259-20251015-010020140418-0D
- Format: KSeF FA(3) v1-0E
- Zawiera numer referencyjny KSeF

### `output/` - Przykładowe wygenerowane PDF

Pliki PDF wygenerowane z powyższych XML-i:
- `FakturaTEST017.pdf` - PDF z trybu offline
- `6511153259-20251015-010020140418-0D.pdf` - PDF z trybu online
- `PRZYKLAD_FAKTURA_OFFLINE.pdf` - przykład offline
- `PRZYKLAD_FAKTURA_Z_NUMEREM_KSEF.pdf` - przykład online z numerem KSeF
- `TEST_CLI.pdf` - wygenerowany przez CLI

## Użycie

### Biblioteka
```csharp
var service = new InvoicePrinterService(...);
await service.GeneratePdfFromXmlAsync(
    xmlFilePath: "examples/xml/FakturaTEST017.xml",
    pdfOutputPath: "output.pdf"
);
```

### CLI
```bash
# Single file
ksef-pdf generate examples/xml/FakturaTEST017.xml -o output.pdf

# Batch mode
ksef-pdf batch examples/xml -o output/

# Watch mode
ksef-pdf watch examples/xml -o output/
```

### API
```bash
curl -X POST "http://localhost:5000/api/invoice/generate-pdf" \
  -H "Content-Type: application/json" \
  -d @request.json \
  --output faktura.pdf
```

## Dodawanie własnych przykładów

Aby dodać własny przykład:
1. Umieść plik XML w katalogu `xml/`
2. Upewnij się że jest zgodny ze schematem KSeF FA(3)
3. Wygeneruj PDF używając jednej z metod powyżej
4. (Opcjonalnie) Zapisz wygenerowany PDF w `output/` jako przykład

## Więcej informacji

Zobacz główny README.md projektu oraz:
- `INSTRUKCJA.md` - szczegółowa dokumentacja biblioteki
- `KSeFPrinter.CLI/README.md` - dokumentacja CLI
- `KSeFPrinter.API/README.md` - dokumentacja API
