# KSeF Printer (KSeF PDF Generator)

Generator wydruków PDF dla faktur elektronicznych w formacie KSeF FA(3).

## Opis projektu

**KSeF Printer** to komponent .NET 9 do automatycznego generowania wizualizacji PDF z dokumentów XML zgodnych ze schematem **KSeF FA(3) v1-0E**.

## Funkcjonalności

### Wariant 1: Faktury z systemu KSeF (Online)
- Przetwarzanie dokumentów XML pobranych z systemu KSeF
- Generowanie PDF z numerem KSeF
- Kod QR I: weryfikacja faktury online
- Kod QR II: weryfikacja certyfikatu z podpisem kryptograficznym

### Wariant 2: Faktury z ERP bez numeru KSeF (Offline)
- Przetwarzanie dokumentów XML z systemu ERP
- Generowanie PDF bez numeru KSeF
- Kod QR I: weryfikacja faktury offline (etykieta "OFFLINE")
- Kod QR II: weryfikacja certyfikatu z podpisem kryptograficznym

### Wariant 3: Faktury z ERP z numerem KSeF
- Planowane w kolejnej wersji

## Wymagania techniczne

- .NET 9.0
- Schemat XML: FA(3) v1-0E
- Namespace: `http://crd.gov.pl/wzor/2025/06/25/13775/`

## Sposób użycia

### Opcja 1: Aplikacja CLI (wiersz poleceń)

Najszybszy sposób - konwertuj XML na PDF z linii poleceń:

```bash
# Pojedynczy plik
ksef-pdf faktura.xml

# Przetwarzanie wsadowe
ksef-pdf *.xml

# Watch mode (automatyczne przetwarzanie)
ksef-pdf --watch faktury/

# Z certyfikatem dla KODU QR II
ksef-pdf faktura.xml --cert certyfikat.pfx --cert-password "haslo"
```

**Zobacz:** [KSeFPrinter.CLI/README.md](KSeFPrinter.CLI/README.md) - pełna dokumentacja CLI

### Opcja 2: Biblioteka .NET

Zintegruj z własną aplikacją:

```csharp
using KSeFPrinter;

var service = new InvoicePrinterService(parser, validator, pdfGenerator, logger);

// Generuj PDF z pliku XML
await service.GeneratePdfFromFileAsync("faktura.xml", "faktura.pdf");
```

**Zobacz:** [INSTRUKCJA.md](INSTRUKCJA.md) - pełna dokumentacja API

## Struktura projektu

```
KSeFPrinter/              # Biblioteka główna
├── Models/               # Modele danych FA(3)
│   ├── FA3/             # Modele specyficzne dla FA(3)
│   └── Common/          # Wspólne typy
├── Services/             # Serwisy biznesowe
│   ├── Cryptography/    # Kryptografia (SHA-256, RSA-PSS, ECDSA)
│   ├── QrCode/          # Generowanie kodów QR
│   ├── Pdf/             # Generowanie PDF
│   └── Parsers/         # Parsowanie XML
├── Validators/           # Walidatory
├── Interfaces/           # Interfejsy
└── Exceptions/           # Wyjątki

KSeFPrinter.CLI/          # Aplikacja wiersza poleceń
├── Program.cs           # Obsługa CLI (pojedynczy, wsadowy, watch mode)
└── README.md            # Dokumentacja CLI

KSeFPrinter.Tests/       # Projekt testowy (130 testów)
ExampleGenerator/        # Generator przykładowych PDF
```

## Zależności

- **QuestPDF** - generowanie dokumentów PDF
- **QRCoder** - generowanie kodów QR
- **Microsoft.Extensions.Logging.Abstractions** - logowanie

### Zależności testowe
- **xUnit** - framework testowy
- **FluentAssertions** - asercje w testach
- **Moq** - mockowanie

## Algorytmy kryptograficzne

### Podpisy dla Kodu QR II
- **RSA-PSS**: SHA-256, MGF1, salt 32 bajty, min. 2048 bitów
- **ECDSA P-256**: krzywa secp256r1 (NIST P-256), format IEEE P1363

### Wymagania dla certyfikatów
- Typ certyfikatu: **Offline** (keyUsage: Non-Repudiation 40)
- Certyfikaty typu "Authentication" NIE mogą być używane do Kodu QR II

## Format PDF

- Rozmiar: A4 (210 x 297 mm)
- Kody QR: minimum 5 pikseli na moduł
- Etykiety kodów QR:
  - Kod QR I Online: numer KSeF
  - Kod QR I Offline: "OFFLINE"
  - Kod QR II: "Certyfikat KSeF wystawcy"

## Środowiska KSeF

- **Test**: https://ksef-test.mf.gov.pl
- **Produkcja**: https://ksef.mf.gov.pl

## Licencja

Projekt realizowany dla potrzeb integracji z systemem KSeF.

## Materiały źródłowe

Projekt wykorzystuje materiały i komponenty z repozytoriów:
- [ksef-client-csharp](https://github.com/CIRFMF/ksef-client-csharp)
- ksef-docs (dokumentacja schematu FA(3))
