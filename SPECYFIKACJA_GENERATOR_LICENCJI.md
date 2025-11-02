# Specyfikacja zmian - Generator licencji v2.0

## 1. WPROWADZENIE

Generator licencji musi zostać zaktualizowany w celu wsparcia wielu produktów (KSeFConnector i KSeFPrinter) z pełną kompatybilnością wsteczną dla istniejących licencji.

**KRYTYCZNE WYMAGANIE:** Wszystkie istniejące licencje KSeFConnector muszą działać bez zmian!

---

## 2. ZMIANY W MODELACH LICENCJI

### 2.1. Plik: `KSeFLicenseGeneratorINFO/Models/LicenseModels.cs`

**Aktualna zawartość klasy `LicenseFeatures`:**
```csharp
public class LicenseFeatures
{
    [JsonPropertyName("batchMode")]
    public bool BatchMode { get; set; }

    [JsonPropertyName("autoTokenGeneration")]
    public bool AutoTokenGeneration { get; set; }

    [JsonPropertyName("azureKeyVault")]
    public bool AzureKeyVault { get; set; }
}
```

**Nowa zawartość klasy `LicenseFeatures`:**
```csharp
/// <summary>
/// Funkcje włączone w licencji
/// ✅ v2.0: Dodano wsparcie dla wielu produktów (KSeFConnector, KSeFPrinter)
/// ✅ Kompatybilność wsteczna: pola produktów są nullable
/// </summary>
public class LicenseFeatures
{
    // === PRODUKTY ===

    /// <summary>
    /// Licencja na KSeF Connector
    /// null = BRAK POLA (stara licencja) → interpretowane jako TRUE (kompatybilność wsteczna)
    /// </summary>
    [JsonPropertyName("ksefConnector")]
    public bool? KSeFConnector { get; set; }

    /// <summary>
    /// Licencja na KSeF Printer
    /// null = BRAK POLA (stara licencja) → interpretowane jako FALSE (nowy produkt)
    /// </summary>
    [JsonPropertyName("ksefPrinter")]
    public bool? KSeFPrinter { get; set; }

    // === FUNKCJE KSEF CONNECTOR ===

    [JsonPropertyName("batchMode")]
    public bool BatchMode { get; set; }

    [JsonPropertyName("autoTokenGeneration")]
    public bool AutoTokenGeneration { get; set; }

    [JsonPropertyName("azureKeyVault")]
    public bool AzureKeyVault { get; set; }
}
```

**Zmiana w klasie `LicenseInfo`:**
```csharp
/// <summary>
/// Informacje o licencji (wspólna dla KSeFConnector i KSeFPrinter)
/// ✅ v1.4.1: Dodano UnixTimestampDateTimeConverter dla spójnego formatowania DateTime
/// ✅ v2.0: Dodano wsparcie dla wielu produktów z kompatybilnością wsteczną
/// ✅ Kompatybilność wsteczna:
///    - Stare licencje: brak pól ksefConnector/ksefPrinter (null)
///    - KSeFConnector: null = true (domyślnie włączone dla starych licencji)
///    - KSeFPrinter: null = false (domyślnie wyłączone dla starych licencji)
/// </summary>
public class LicenseInfo
{
    // ... (reszta bez zmian)
}
```

---

## 3. ZMIANY W SERWISIE GENERATORA

### 3.1. Plik: `KSeFLicenseGeneratorINFO/Services/LicenseGeneratorService.cs`

**Aktualna sygnatura metody `CreateLicense`:**
```csharp
public LicenseInfo CreateLicense(
    string issuedTo,
    List<string> allowedNips,
    int maxNips,
    DateTime expiryDate,
    bool batchMode = true,
    bool autoTokenGeneration = true,
    bool azureKeyVault = true)
```

**Nowa sygnatura metody `CreateLicense`:**
```csharp
public LicenseInfo CreateLicense(
    string issuedTo,
    List<string> allowedNips,
    int maxNips,
    DateTime expiryDate,
    bool ksefConnector = true,        // ✅ NOWY PARAMETR
    bool ksefPrinter = false,          // ✅ NOWY PARAMETR
    bool batchMode = true,
    bool autoTokenGeneration = true,
    bool azureKeyVault = true)
```

**Aktualna inicjalizacja `Features`:**
```csharp
Features = new LicenseFeatures
{
    BatchMode = batchMode,
    AutoTokenGeneration = autoTokenGeneration,
    AzureKeyVault = azureKeyVault
}
```

**Nowa inicjalizacja `Features`:**
```csharp
Features = new LicenseFeatures
{
    // Produkty
    KSeFConnector = ksefConnector,        // ✅ NOWE POLE
    KSeFPrinter = ksefPrinter,            // ✅ NOWE POLE

    // Funkcje KSeFConnector
    BatchMode = batchMode,
    AutoTokenGeneration = autoTokenGeneration,
    AzureKeyVault = azureKeyVault
}
```

**Aktualna generacja klucza licencji:**
```csharp
return $"KSEF-CONN-{timestamp}-{randomPart}";
```

**Nowa generacja klucza licencji:**
```csharp
// ✅ ZMIANA: Usunięto "-CONN" dla uniwersalności
return $"KSEF-{timestamp}-{randomPart}";
```

---

## 4. ZMIANY W CLI (Program.cs)

### 4.1. Plik: `KSeFLicenseGeneratorINFO/Program.cs`

**Dodać nowe opcje do komendy `generate`:**

```csharp
var ksefConnectorOption = new Option<bool>(
    "--ksef-connector",
    description: "Enable KSeF Connector license",
    getDefaultValue: () => true
);

var ksefPrinterOption = new Option<bool>(
    "--ksef-printer",
    description: "Enable KSeF Printer license",
    getDefaultValue: () => false
);

generateCommand.AddOption(ksefConnectorOption);
generateCommand.AddOption(ksefPrinterOption);
```

**Zaktualizować handler komendy `generate`:**

```csharp
generateCommand.SetHandler(async (
    string customer,
    string nipsStr,
    int maxNips,
    DateTime expiryDate,
    string output,
    bool ksefConnector,      // ✅ NOWY PARAMETR
    bool ksefPrinter,        // ✅ NOWY PARAMETR
    bool batchMode,
    bool autoToken,
    bool azureKeyVault) =>
{
    // ...

    var license = generator.CreateLicense(
        issuedTo: customer,
        allowedNips: nips,
        maxNips: maxNips,
        expiryDate: expiryDate,
        ksefConnector: ksefConnector,      // ✅ NOWY PARAMETR
        ksefPrinter: ksefPrinter,          // ✅ NOWY PARAMETR
        batchMode: batchMode,
        autoTokenGeneration: autoToken,
        azureKeyVault: azureKeyVault
    );

    // ...
},
customerOption, nipsOption, maxNipsOption, expiryOption, outputOption,
ksefConnectorOption, ksefPrinterOption,  // ✅ NOWE OPCJE
batchModeOption, autoTokenOption, azureKeyVaultOption);
```

**Zaktualizować wyświetlanie szczegółów licencji:**

```csharp
Console.WriteLine($"\nFeatures:");
Console.WriteLine($"  • KSeF Connector:         {(license.Features.KSeFConnector == true ? "✅" : "❌")}");  // ✅ NOWA LINIA
Console.WriteLine($"  • KSeF Printer:           {(license.Features.KSeFPrinter == true ? "✅" : "❌")}");    // ✅ NOWA LINIA
Console.WriteLine($"  • Batch Mode:             {(license.Features.BatchMode ? "✅" : "❌")}");
Console.WriteLine($"  • Auto Token Generation:  {(license.Features.AutoTokenGeneration ? "✅" : "❌")}");
Console.WriteLine($"  • Azure Key Vault:        {(license.Features.AzureKeyVault ? "✅" : "❌")}");
```

**Zaktualizować komendę `verify` - wyświetlanie szczegółów:**

```csharp
Console.WriteLine($"\nFeatures:");
Console.WriteLine($"  • KSeF Connector:         {(license.Features.KSeFConnector == true ? "✅" : "❌")}");  // ✅ NOWA LINIA
Console.WriteLine($"  • KSeF Printer:           {(license.Features.KSeFPrinter == true ? "✅" : "❌")}");    // ✅ NOWA LINIA
Console.WriteLine($"  • Batch Mode:             {(license.Features.BatchMode ? "✅" : "❌")}");
Console.WriteLine($"  • Auto Token Generation:  {(license.Features.AutoTokenGeneration ? "✅" : "❌")}");
Console.WriteLine($"  • Azure Key Vault:        {(license.Features.AzureKeyVault ? "✅" : "❌")}");
```

---

## 5. PRZYKŁADY UŻYCIA ZAKTUALIZOWANEGO GENERATORA

### 5.1. Generowanie licencji tylko dla KSeFConnector (kompatybilne ze starymi)

```bash
dotnet run -- generate \
  --customer "Firma ABC Sp. z o.o." \
  --nips "1234567890" \
  --max-nips 5 \
  --expiry-date "2026-12-31" \
  --output "abc-connector.lic" \
  --ksef-connector true \
  --ksef-printer false
```

**Wygenerowana licencja:**
```json
{
  "licenseKey": "KSEF-20250102-A1B2C3D4",
  "issuedTo": "Firma ABC Sp. z o.o.",
  "expiryDate": "2026-12-31T00:00:00Z",
  "allowedNips": ["1234567890"],
  "maxNips": 5,
  "features": {
    "ksefConnector": true,    // ✅ JAWNIE USTAWIONE
    "ksefPrinter": false,     // ✅ JAWNIE USTAWIONE
    "batchMode": true,
    "autoTokenGeneration": true,
    "azureKeyVault": true
  },
  "signature": "..."
}
```

**Weryfikacja kompatybilności:** Ta licencja będzie działać z KSeFConnector (Features.KSeFConnector = true).

---

### 5.2. Generowanie licencji tylko dla KSeFPrinter

```bash
dotnet run -- generate \
  --customer "Firma XYZ Sp. z o.o." \
  --nips "0987654321" \
  --max-nips 3 \
  --expiry-date "2026-12-31" \
  --output "xyz-printer.lic" \
  --ksef-connector false \
  --ksef-printer true
```

**Wygenerowana licencja:**
```json
{
  "licenseKey": "KSEF-20250102-E5F6G7H8",
  "issuedTo": "Firma XYZ Sp. z o.o.",
  "expiryDate": "2026-12-31T00:00:00Z",
  "allowedNips": ["0987654321"],
  "maxNips": 3,
  "features": {
    "ksefConnector": false,   // ✅ WYŁĄCZONE
    "ksefPrinter": true,      // ✅ WŁĄCZONE
    "batchMode": true,
    "autoTokenGeneration": true,
    "azureKeyVault": true
  },
  "signature": "..."
}
```

**Weryfikacja:** Ta licencja będzie działać z KSeFPrinter, ale NIE z KSeFConnector.

---

### 5.3. Generowanie licencji dla obu produktów (pakiet)

```bash
dotnet run -- generate \
  --customer "Firma PREMIUM Sp. z o.o." \
  --nips "1111111111,2222222222" \
  --max-nips 10 \
  --expiry-date "2027-12-31" \
  --output "premium-full.lic" \
  --ksef-connector true \
  --ksef-printer true
```

**Wygenerowana licencja:**
```json
{
  "licenseKey": "KSEF-20250102-I9J0K1L2",
  "issuedTo": "Firma PREMIUM Sp. z o.o.",
  "expiryDate": "2027-12-31T00:00:00Z",
  "allowedNips": ["1111111111", "2222222222"],
  "maxNips": 10,
  "features": {
    "ksefConnector": true,    // ✅ OBA WŁĄCZONE
    "ksefPrinter": true,      // ✅ OBA WŁĄCZONE
    "batchMode": true,
    "autoTokenGeneration": true,
    "azureKeyVault": true
  },
  "signature": "..."
}
```

**Weryfikacja:** Ta licencja będzie działać z OBYDWOMA produktami.

---

## 6. KOMPATYBILNOŚĆ WSTECZNA - WERYFIKACJA

### 6.1. Stara licencja (przed zmianami)

**Przykład starej licencji:**
```json
{
  "licenseKey": "KSEF-CONN-20250101-OLDLIC01",
  "issuedTo": "Stary Klient Sp. z o.o.",
  "expiryDate": "2026-06-30T00:00:00Z",
  "allowedNips": ["9999999999"],
  "maxNips": 5,
  "features": {
    "batchMode": true,
    "autoTokenGeneration": true,
    "azureKeyVault": false
  },
  "signature": "..."
}
```

**Brak pól:** `ksefConnector`, `ksefPrinter`

**Zachowanie:**
- **KSeFConnector:** Pole `Features.KSeFConnector` = `null` → interpretowane jako `true` ✅ **DZIAŁA**
- **KSeFPrinter:** Pole `Features.KSeFPrinter` = `null` → interpretowane jako `false` ❌ **NIE DZIAŁA** (to poprawne - stara licencja nie obejmuje KSeFPrinter)

**WNIOSEK:** Stare licencje działają bez zmian w KSeFConnector! ✅

---

### 6.2. Nowa licencja - pełna jawność

**Nowe licencje muszą ZAWSZE mieć pola `ksefConnector` i `ksefPrinter` jawnie ustawione.**

**Przykład:**
```json
{
  "features": {
    "ksefConnector": true,   // ✅ JAWNIE
    "ksefPrinter": false,    // ✅ JAWNIE
    "batchMode": true,
    "autoTokenGeneration": true,
    "azureKeyVault": true
  }
}
```

---

## 7. TESTY - WYMAGANE SCENARIUSZE

### 7.1. Test 1: Generowanie licencji tylko dla KSeFConnector

```bash
dotnet run -- generate \
  --customer "Test KSeFConnector Only" \
  --nips "1111111111" \
  --max-nips 1 \
  --expiry-date "2026-12-31" \
  --output "test-connector-only.lic" \
  --ksef-connector true \
  --ksef-printer false
```

**Oczekiwany wynik:**
- Plik `test-connector-only.lic` zawiera `"ksefConnector": true` i `"ksefPrinter": false`
- Weryfikacja podpisu: ✅ OK
- KSeFConnector może użyć tej licencji
- KSeFPrinter odrzuci tę licencję

---

### 7.2. Test 2: Generowanie licencji tylko dla KSeFPrinter

```bash
dotnet run -- generate \
  --customer "Test KSeFPrinter Only" \
  --nips "2222222222" \
  --max-nips 1 \
  --expiry-date "2026-12-31" \
  --output "test-printer-only.lic" \
  --ksef-connector false \
  --ksef-printer true
```

**Oczekiwany wynik:**
- Plik `test-printer-only.lic` zawiera `"ksefConnector": false` i `"ksefPrinter": true`
- Weryfikacja podpisu: ✅ OK
- KSeFConnector odrzuci tę licencję
- KSeFPrinter może użyć tej licencji

---

### 7.3. Test 3: Generowanie licencji dla obu produktów

```bash
dotnet run -- generate \
  --customer "Test Both Products" \
  --nips "3333333333" \
  --max-nips 1 \
  --expiry-date "2026-12-31" \
  --output "test-both.lic" \
  --ksef-connector true \
  --ksef-printer true
```

**Oczekiwany wynik:**
- Plik `test-both.lic` zawiera `"ksefConnector": true` i `"ksefPrinter": true`
- Weryfikacja podpisu: ✅ OK
- KSeFConnector może użyć tej licencji
- KSeFPrinter może użyć tej licencji

---

### 7.4. Test 4: Weryfikacja starej licencji (bez nowych pól)

**Użyj istniejącej starej licencji KSeFConnector:**

```bash
dotnet run -- verify --file "../path/to/old-license.lic"
```

**Oczekiwany wynik:**
- Weryfikacja podpisu: ✅ OK
- Wyświetlone informacje pokazują:
  - `KSeF Connector: ❌` (pole nie istnieje, pokazujemy jako ❌ ale w rzeczywistości to `null`)
  - `KSeF Printer: ❌`
- **KRYTYCZNE:** Stara licencja musi dalej działać w KSeFConnector (bo tam `null` = `true`)

---

## 8. NAJWAŻNIEJSZE ZASADY

### 8.1. Podpis RSA - BEZ ZMIAN

**KRYTYCZNE:** Algorytm podpisywania i weryfikacji NIE MOŻE się zmienić!

**Obecna logika:**
```csharp
var dataToSign = JsonSerializer.Serialize(new
{
    license.LicenseKey,
    license.IssuedTo,
    license.ExpiryDate,
    license.AllowedNips,
    license.MaxNips,
    license.Features  // ✅ Cały obiekt Features (włącznie z nowymi polami)
}, new JsonSerializerOptions
{
    WriteIndented = false
});
```

**UWAGA:** Nowe pola `ksefConnector` i `ksefPrinter` będą AUTOMATYCZNIE uwzględnione w podpisie, ponieważ są częścią `Features`.

**Weryfikacja starych licencji:**
- Stare licencje nie mają pól `ksefConnector`/`ksefPrinter`
- Przy deserializacji JSON: te pola będą `null`
- Przy serializacji do podpisu: pola z wartością `null` są POMIJANE w JSON (to standardowe zachowanie JSON)
- Podpis będzie identyczny jak przy generowaniu starej licencji ✅

---

### 8.2. Format klucza licencji

**Stary format:** `KSEF-CONN-20250102-A1B2C3D4`
**Nowy format:** `KSEF-20250102-A1B2C3D4`

**UWAGA:** To kosmetyczna zmiana, nie wpływa na podpis ani weryfikację.

---

### 8.3. Domyślne wartości

**Przy wywołaniu bez opcji `--ksef-connector` i `--ksef-printer`:**

```bash
dotnet run -- generate \
  --customer "Test" \
  --nips "1111111111" \
  --max-nips 1 \
  --expiry-date "2026-12-31" \
  --output "test.lic"
```

**Domyślne wartości:**
- `--ksef-connector`: `true` (domyślnie włączone)
- `--ksef-printer`: `false` (domyślnie wyłączone)

**UZASADNIENIE:** Zachowanie zgodne ze starymi licencjami (były tylko dla KSeFConnector).

---

## 9. CHECKLIST IMPLEMENTACJI

### Pliki do zmiany:

- [ ] `KSeFLicenseGeneratorINFO/Models/LicenseModels.cs`
  - [ ] Dodać pola `KSeFConnector` i `KSeFPrinter` do klasy `LicenseFeatures`
  - [ ] Zaktualizować komentarze klasy `LicenseInfo`

- [ ] `KSeFLicenseGeneratorINFO/Services/LicenseGeneratorService.cs`
  - [ ] Dodać parametry `ksefConnector` i `ksefPrinter` do metody `CreateLicense`
  - [ ] Zaktualizować inicjalizację obiektu `Features`
  - [ ] Zmienić format klucza licencji z `KSEF-CONN-...` na `KSEF-...`

- [ ] `KSeFLicenseGeneratorINFO/Program.cs`
  - [ ] Dodać opcje `--ksef-connector` i `--ksef-printer` do komendy `generate`
  - [ ] Zaktualizować handler komendy `generate`
  - [ ] Zaktualizować wyświetlanie szczegółów licencji w komendzie `generate`
  - [ ] Zaktualizować wyświetlanie szczegółów licencji w komendzie `verify`
  - [ ] Zaktualizować wyświetlanie szczegółów licencji w komendzie `list`

### Testy:

- [ ] Test 1: Generowanie licencji tylko dla KSeFConnector
- [ ] Test 2: Generowanie licencji tylko dla KSeFPrinter
- [ ] Test 3: Generowanie licencji dla obu produktów
- [ ] Test 4: Weryfikacja starej licencji (kompatybilność wsteczna)
- [ ] Test 5: Weryfikacja nowej licencji w KSeFConnector
- [ ] Test 6: Weryfikacja nowej licencji w KSeFPrinter

---

## 10. UWAGI KOŃCOWE

1. **NIE ZMIENIAJ** algorytmu podpisywania/weryfikacji RSA
2. **NIE ZMIENIAJ** formatu serializacji JSON dla podpisu
3. **NIE ZMIENIAJ** klucza publicznego RSA
4. **ZAWSZE** ustawiaj pola `ksefConnector` i `ksefPrinter` jawnie w nowych licencjach
5. **PRZETESTUJ** kompatybilność wsteczną ze starą licencją przed wdrożeniem

---

## 11. KONTAKT

W razie pytań lub problemów, skonsultuj się z osobą odpowiedzialną za implementację w KSeFPrinter i KSeFConnector.

**Data stworzenia specyfikacji:** 2025-01-02
**Wersja:** 2.0
**Autor:** Claude Code (AI Assistant)

---

## KONIEC SPECYFIKACJI
