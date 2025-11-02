# Instalator KSeF Printer - Brakujące elementy

**Data:** 2025-01-02
**Wersja instalatora:** 1.0.0 (WiX Toolset 5)

---

## Spis treści

1. [Krytyczne braki](#krytyczne-braki)
2. [Ważne braki](#ważne-braki)
3. [Opcjonalne usprawnienia](#opcjonalne-usprawnienia)
4. [Szczegółowa specyfikacja zmian](#szczegółowa-specyfikacja-zmian)
5. [Instrukcja implementacji](#instrukcja-implementacji)

---

## Krytyczne braki

### 1. ❌ System licencjonowania

**Problem:** Aplikacja wymaga licencji (`license.lic`) w folderze instalacji, ale instalator nie tworzy tej struktury ani nie informuje użytkownika.

**Co brakuje:**
- ✅ Folder na licencję nie jest tworzony
- ✅ Brak pliku `license.lic.template` jako przykładu
- ✅ Brak okna dialogowego informującego o konieczności licencji
- ✅ Brak instrukcji licencjonowania w dokumentacji instalatora

**Konsekwencje:**
- API nie uruchomi się (Exit code: 1)
- CLI nie uruchomi się
- Użytkownik nie wie, dlaczego aplikacja nie działa

**Priorytet:** 🔴 KRYTYCZNY

---

### 2. ❌ Brak serwisu Windows dla API

**Problem:** API działa jako konsola, nie ma serwisu Windows.

**Co brakuje:**
- ✅ Instalacja serwisu Windows dla `KSeFPrinter.API`
- ✅ Konfiguracja serwisu (ustawienie na "Manual" start, NIE automatic)
- ✅ Skrypty start/stop serwisu w Start Menu
- ✅ Logowanie serwisu do Event Log

**Konsekwencje:**
- API musi być uruchamiane ręcznie przez użytkownika
- Brak automatycznego uruchamiania przy starcie systemu (opcja)
- Utrudniona konfiguracja dla środowisk serwerowych

**Priorytet:** 🔴 KRYTYCZNY

---

### 3. ❌ Brak instrukcji post-instalacyjnej

**Problem:** Użytkownik nie wie, co zrobić po instalacji.

**Co brakuje:**
- ✅ Dialog "Finish" z instrukcjami następnych kroków
- ✅ Automatyczne otwarcie folderu instalacji
- ✅ Wyświetlenie ścieżki do pliku licencji
- ✅ Link do dokumentacji

**Priorytet:** 🔴 KRYTYCZNY

---

## Ważne braki

### 4. ⚠️ Dokumentacja w instalatorze

**Co brakuje:**
- ✅ `API_SPECIFICATION.md` nie jest dołączana
- ✅ `INSTALLATION_GUIDE.md` - kompletna instrukcja instalacji i konfiguracji
- ✅ `LICENSE_GUIDE.md` - instrukcja licencjonowania
- ✅ `CERTIFICATE_GUIDE.md` - instrukcja konfiguracji certyfikatów

**Priorytet:** 🟡 WAŻNY

---

### 5. ⚠️ Konfiguracja appsettings.json

**Problem:** `appsettings.json` dla API nie jest konfigurowany podczas instalacji.

**Co brakuje:**
- ✅ Możliwość wyboru portu API podczas instalacji (domyślnie: 5000/5001)
- ✅ Automatyczne ustawienie ścieżki licencji w `appsettings.json`
- ✅ Konfiguracja logowania (poziom, ścieżka)
- ✅ Możliwość konfiguracji Azure Key Vault (opcjonalnie)

**Priorytet:** 🟡 WAŻNY

---

### 6. ⚠️ Sprawdzenie .NET Runtime

**Problem:** Instalator nie sprawdza, czy .NET 9.0 Runtime jest zainstalowany.

**Co brakuje:**
- ✅ Custom Action sprawdzająca obecność .NET 9.0 Runtime
- ✅ Dialog z linkiem do pobrania .NET 9.0, jeśli nie jest zainstalowany
- ✅ Możliwość instalacji runtime razem z aplikacją (bundle)

**Priorytet:** 🟡 WAŻNY

---

### 7. ⚠️ Skróty w Start Menu

**Co brakuje:**
- ✅ Skrót do dokumentacji (otwiera folder z README.md i API_SPECIFICATION.md)
- ✅ Skrót do folderu instalacji (otwiera `C:\Program Files\KSeF Printer`)
- ✅ Skrót do Swagger UI (`http://localhost:5000/`)
- ✅ Skrót do "Zatrzymaj serwis API" (jeśli serwis Windows zostanie dodany)
- ✅ Skrót do "Uruchom serwis API" (jeśli serwis Windows zostanie dodany)

**Priorytet:** 🟡 WAŻNY

---

## Opcjonalne usprawnienia

### 8. 💡 Ikona aplikacji

**Stan:** Plik `icon.ico.README` istnieje, ale brak pliku `icon.ico`.

**Co zrobić:**
- Dodać plik `icon.ico` (ikona dla Add/Remove Programs)
- Odkomentować w `Package.wxs`:
  ```xml
  <Icon Id="ProductIcon" SourceFile="icon.ico" />
  <Property Id="ARPPRODUCTICON" Value="ProductIcon" />
  ```

**Priorytet:** 🟢 OPCJONALNY

---

### 9. 💡 Więcej przykładów faktur

**Stan:** Tylko 5 przykładowych faktur XML.

**Co dodać:**
- Wszystkie warianty faktur z `examples/xml/` (obecnie ~15 plików)
- Faktury korygujące
- Faktury z wieloma podmiotami
- Faktury z różnymi walutami (EUR, USD)

**Priorytet:** 🟢 OPCJONALNY

---

### 10. 💡 Firewall rules dla API

**Co dodać:**
- Automatyczne utworzenie reguły Windows Firewall dla portu 5000/5001
- Możliwość wyłączenia podczas instalacji

**Priorytet:** 🟢 OPCJONALNY

---

### 11. 💡 Automatyczne aktualizacje

**Co dodać:**
- Sprawdzanie dostępności nowszej wersji przy uruchomieniu
- Link do strony z aktualizacjami

**Priorytet:** 🟢 OPCJONALNY

---

### 12. 💡 Zachowanie licencji przy odinstalowaniu

**Problem:** Przy odinstalowaniu licencja jest usuwana.

**Co zrobić:**
- Dialog pytający, czy zachować plik `license.lic`
- Opcjonalnie: przeniesienie licencji do `%APPDATA%` zamiast `Program Files`

**Priorytet:** 🟢 OPCJONALNY

---

## Szczegółowa specyfikacja zmian

### 1. Dodanie systemu licencjonowania

#### 1.1. Struktura folderów

**Obecny stan:**
```
C:\Program Files\KSeF Printer\
├── CLI\
├── API\
└── Examples\
```

**Docelowy stan:**
```
C:\Program Files\KSeF Printer\
├── CLI\
│   └── license.lic (MUSI być dodany RĘCZNIE po instalacji)
├── API\
│   └── license.lic (MUSI być dodany RĘCZNIE po instalacji)
├── Examples\
└── Documentation\
    ├── README.md
    ├── API_SPECIFICATION.md
    ├── INSTALLATION_GUIDE.md
    ├── LICENSE_GUIDE.md
    └── CERTIFICATE_GUIDE.md
```

**UWAGA:** Plik `license.lic` NIE jest instalowany automatycznie - użytkownik musi go dodać ręcznie.

#### 1.2. Dodanie pliku `license.lic.template`

**Lokalizacja:** `C:\Program Files\KSeF Printer\CLI\license.lic.template`

**Zawartość:**
```json
{
  "licenseKey": "KSEF-YYYYMMDD-XXXXXXXX",
  "issuedTo": "Nazwa firmy",
  "expiryDate": "2026-12-31T00:00:00Z",
  "allowedNips": ["1234567890"],
  "maxNips": 5,
  "features": {
    "ksefConnector": false,
    "ksefPrinter": true,
    "batchMode": false,
    "autoTokenGeneration": false,
    "azureKeyVault": true
  },
  "signature": "..."
}
```

**Plik ten służy jako szablon - użytkownik musi go zastąpić prawdziwą licencją.**

#### 1.3. Dodanie okna dialogowego informującego o licencji

**Typ:** Custom dialog po ekranie "VerifyReadyDlg" (przed instalacją).

**Treść:**
```
╔═══════════════════════════════════════════════════════════╗
║           WAŻNE: Wymagana licencja KSeF Printer           ║
╚═══════════════════════════════════════════════════════════╝

KSeF Printer wymaga ważnej licencji do działania.

PO ZAKOŃCZENIU INSTALACJI:

1. Skopiuj plik licencji (license.lic) do folderu:
   C:\Program Files\KSeF Printer\CLI\
   C:\Program Files\KSeF Printer\API\

2. Uruchom aplikację CLI lub API.

Bez ważnej licencji aplikacja NIE URUCHOMI SIĘ.

Skontaktuj się z dostawcą w celu uzyskania licencji.

[✓] Rozumiem i posiadam licencję
```

**Implementacja WiX:**
```xml
<UI>
  <Dialog Id="LicenseRequiredDlg" Width="370" Height="270" Title="[ProductName] - Licencja wymagana">
    <Control Id="Title" Type="Text" X="15" Y="6" Width="200" Height="15" Transparent="yes" NoPrefix="yes" Text="{\WixUI_Font_Title}Licencja wymagana" />
    <Control Id="Description" Type="Text" X="25" Y="23" Width="320" Height="90" NoPrefix="yes">
      <Text>KSeF Printer wymaga ważnej licencji do działania.

PO ZAKOŃCZENIU INSTALACJI:

1. Skopiuj plik licencji (license.lic) do:
   [CLIINSTALLFOLDER]
   [APIINSTALLFOLDER]

2. Uruchom aplikację.

Bez ważnej licencji aplikacja NIE URUCHOMI SIĘ.</Text>
    </Control>
    <Control Id="UnderstandCheckbox" Type="CheckBox" X="20" Y="120" Width="290" Height="17" Property="LICENSE_UNDERSTOOD" CheckBoxValue="1">
      <Text>Rozumiem i posiadam licencję</Text>
    </Control>
    <Control Id="Back" Type="PushButton" X="180" Y="243" Width="56" Height="17" Text="&amp;Wstecz">
      <Publish Event="NewDialog" Value="VerifyReadyDlg">1</Publish>
    </Control>
    <Control Id="Next" Type="PushButton" X="236" Y="243" Width="56" Height="17" Default="yes" Text="&amp;Dalej">
      <Publish Event="NewDialog" Value="ProgressDlg">LICENSE_UNDERSTOOD = "1"</Publish>
      <Condition Action="disable">LICENSE_UNDERSTOOD &lt;&gt; "1"</Condition>
      <Condition Action="enable">LICENSE_UNDERSTOOD = "1"</Condition>
    </Control>
    <Control Id="Cancel" Type="PushButton" X="304" Y="243" Width="56" Height="17" Cancel="yes" Text="Anuluj">
      <Publish Event="SpawnDialog" Value="CancelDlg">1</Publish>
    </Control>
  </Dialog>
</UI>

<InstallUISequence>
  <Show Dialog="LicenseRequiredDlg" After="VerifyReadyDlg">NOT Installed</Show>
</InstallUISequence>
```

#### 1.4. Dodanie dialogu "Finish" z instrukcjami

**Typ:** Custom dialog zastępujący standardowy "ExitDialog".

**Treść:**
```
╔═══════════════════════════════════════════════════════════╗
║      Instalacja KSeF Printer zakończona pomyślnie!       ║
╚═══════════════════════════════════════════════════════════╝

NASTĘPNE KROKI:

1. ✅ Dodaj plik licencji (license.lic):
   • CLI: C:\Program Files\KSeF Printer\CLI\license.lic
   • API: C:\Program Files\KSeF Printer\API\license.lic

2. ✅ (Opcjonalnie) Skonfiguruj certyfikat:
   • Windows Certificate Store (thumbprint/subject)
   • Azure Key Vault (URL + nazwa certyfikatu)

3. ✅ Uruchom aplikację:
   • CLI: ksef-pdf.exe [plik.xml] [wyjscie.pdf]
   • API: Uruchom z Start Menu lub serwis Windows

📚 Dokumentacja: C:\Program Files\KSeF Printer\Documentation\

[✓] Otwórz folder instalacji
[✓] Otwórz dokumentację
[   Zakończ   ]
```

#### 1.5. Dodanie `LICENSE_GUIDE.md`

**Lokalizacja:** `Documentation\LICENSE_GUIDE.md`

**Zawartość:** Szczegółowa instrukcja:
- Jak uzyskać licencję
- Gdzie umieścić plik `license.lic`
- Format pliku licencji
- Rozwiązywanie problemów (licencja wygasła, nieprawidłowy podpis, itp.)
- Kontakt do dostawcy

---

### 2. Dodanie serwisu Windows dla API

#### 2.1. Konfiguracja serwisu

**Nazwa serwisu:** `KSeFPrinterAPI`

**Opis:** `KSeF Printer Web API - RESTful service for invoice PDF generation`

**Ustawienia:**
- **Start type:** Manual (NIE Automatic) - użytkownik musi uruchomić po dodaniu licencji
- **Account:** LocalSystem (lub opcjonalnie: NetworkService)
- **Dependencies:** Brak (opcjonalnie: HTTP.sys)

**Implementacja WiX:**
```xml
<Component Id="APIServiceComponent" Directory="APIINSTALLFOLDER" Guid="...">
  <File Id="APIExe" Source="..." KeyPath="yes" />

  <!-- Instalacja serwisu Windows -->
  <ServiceInstall
    Id="KSeFPrinterAPIService"
    Name="KSeFPrinterAPI"
    DisplayName="KSeF Printer API"
    Description="RESTful Web API for generating PDF from KSeF invoices (FA3 format)"
    Type="ownProcess"
    Start="demand"
    Account="LocalSystem"
    ErrorControl="normal"
    Arguments="--urls http://localhost:5000;https://localhost:5001">

    <!-- Opóźnione uruchomienie (tylko jeśli start type = auto) -->
    <util:ServiceConfig
      xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util"
      FirstFailureActionType="restart"
      SecondFailureActionType="restart"
      ThirdFailureActionType="restart"
      RestartServiceDelayInSeconds="60" />
  </ServiceInstall>

  <ServiceControl
    Id="StartKSeFPrinterAPIService"
    Name="KSeFPrinterAPI"
    Start="install"
    Stop="both"
    Remove="uninstall" />
</Component>
```

**UWAGA:** `Start="demand"` oznacza "Manual" - serwis NIE uruchomi się automatycznie przy starcie systemu.

#### 2.2. Skróty do zarządzania serwisem

**Start Menu → KSeF Printer:**
- `Uruchom API (serwis Windows).lnk`
- `Zatrzymaj API (serwis Windows).lnk`
- `Restart API (serwis Windows).lnk`
- `Status serwisu API.lnk`

**Implementacja:**
```xml
<!-- Skrót: Uruchom serwis -->
<Component Id="StartServiceShortcut" Directory="ApplicationProgramsFolder" Guid="...">
  <Shortcut
    Id="StartServiceShortcut"
    Name="Uruchom API (serwis Windows)"
    Description="Uruchamia serwis KSeF Printer API"
    Target="[System64Folder]net.exe"
    Arguments="start KSeFPrinterAPI"
    WorkingDirectory="APIINSTALLFOLDER" />
  <RemoveFolder Id="RemoveStartServiceShortcut" Directory="ApplicationProgramsFolder" On="uninstall" />
  <RegistryValue Root="HKCU" Key="Software\KSeF\KSeFPrinter" Name="ServiceShortcuts" Type="integer" Value="1" KeyPath="yes" />
</Component>

<!-- Skrót: Zatrzymaj serwis -->
<Component Id="StopServiceShortcut" Directory="ApplicationProgramsFolder" Guid="...">
  <Shortcut
    Id="StopServiceShortcut"
    Name="Zatrzymaj API (serwis Windows)"
    Description="Zatrzymuje serwis KSeF Printer API"
    Target="[System64Folder]net.exe"
    Arguments="stop KSeFPrinterAPI"
    WorkingDirectory="APIINSTALLFOLDER" />
  <RemoveFolder Id="RemoveStopServiceShortcut" Directory="ApplicationProgramsFolder" On="uninstall" />
  <RegistryValue Root="HKCU" Key="Software\KSeF\KSeFPrinter" Name="ServiceShortcuts2" Type="integer" Value="1" KeyPath="yes" />
</Component>

<!-- Skrót: Swagger UI -->
<Component Id="SwaggerShortcut" Directory="ApplicationProgramsFolder" Guid="...">
  <Shortcut
    Id="SwaggerShortcut"
    Name="Otwórz Swagger UI"
    Description="Otwiera interfejs Swagger w przeglądarce (wymaga uruchomionego serwisu)"
    Target="http://localhost:5000/" />
  <RemoveFolder Id="RemoveSwaggerShortcut" Directory="ApplicationProgramsFolder" On="uninstall" />
  <RegistryValue Root="HKCU" Key="Software\KSeF\KSeFPrinter" Name="SwaggerShortcut" Type="integer" Value="1" KeyPath="yes" />
</Component>
```

#### 2.3. Logowanie serwisu

**Event Log:**
- Source: `KSeFPrinterAPI`
- Log: `Application`

**Implementacja w `Program.cs`:**
```csharp
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "KSeFPrinterAPI";
});

builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "KSeFPrinterAPI";
    settings.LogName = "Application";
});
```

---

### 3. Sprawdzenie .NET Runtime

#### 3.1. Custom Action sprawdzająca .NET 9.0

**Implementacja:**

Plik: `CustomActions.cs`
```csharp
using WixToolset.Dtf.WindowsInstaller;
using System.Diagnostics;

public class CustomActions
{
    [CustomAction]
    public static ActionResult CheckDotNetRuntime(Session session)
    {
        session.Log("Sprawdzanie .NET 9.0 Runtime...");

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Szukaj "Microsoft.NETCore.App 9.0"
            if (output.Contains("Microsoft.NETCore.App 9.0") ||
                output.Contains("Microsoft.AspNetCore.App 9.0"))
            {
                session.Log("✓ .NET 9.0 Runtime znaleziony");
                return ActionResult.Success;
            }

            session.Log("✗ .NET 9.0 Runtime NIE znaleziony");
            session["DOTNET_MISSING"] = "1";
            return ActionResult.Success; // Nie blokuj instalacji, tylko pokaż ostrzeżenie
        }
        catch
        {
            session.Log("✗ Nie można sprawdzić .NET Runtime");
            session["DOTNET_MISSING"] = "1";
            return ActionResult.Success;
        }
    }
}
```

**Dialog ostrzeżenia:**
```xml
<Dialog Id="DotNetMissingDlg" Width="370" Height="270" Title="[ProductName] - Brak .NET Runtime">
  <Control Id="Title" Type="Text" X="15" Y="6" Width="300" Height="15" Transparent="yes" NoPrefix="yes" Text="{\WixUI_Font_Title}.NET 9.0 Runtime nie znaleziony" />
  <Control Id="Description" Type="Text" X="25" Y="23" Width="320" Height="120" NoPrefix="yes">
    <Text>KSeF Printer wymaga .NET 9.0 Runtime do działania.

Instalacja będzie kontynuowana, ale aplikacja NIE URUCHOMI SIĘ bez .NET 9.0.

Pobierz .NET 9.0 Runtime:
https://dotnet.microsoft.com/download/dotnet/9.0

Wybierz: ".NET Runtime 9.0.x" lub "ASP.NET Core Runtime 9.0.x"</Text>
  </Control>
  <Control Id="Next" Type="PushButton" X="236" Y="243" Width="56" Height="17" Default="yes" Text="&amp;Kontynuuj">
    <Publish Event="NewDialog" Value="VerifyReadyDlg">1</Publish>
  </Control>
  <Control Id="Cancel" Type="PushButton" X="304" Y="243" Width="56" Height="17" Cancel="yes" Text="Anuluj">
    <Publish Event="SpawnDialog" Value="CancelDlg">1</Publish>
  </Control>
</Dialog>

<InstallUISequence>
  <Custom Action="CheckDotNetRuntime" After="CostFinalize">NOT Installed</Custom>
  <Show Dialog="DotNetMissingDlg" After="LicenseAgreementDlg">DOTNET_MISSING = "1" AND NOT Installed</Show>
</InstallUISequence>
```

---

### 4. Konfiguracja appsettings.json podczas instalacji

#### 4.1. Dodanie właściwości instalatora

**Package.wxs:**
```xml
<!-- Właściwości konfiguracyjne -->
<Property Id="API_PORT_HTTP" Value="5000" />
<Property Id="API_PORT_HTTPS" Value="5001" />
<Property Id="LICENSE_PATH" Value="[APIINSTALLFOLDER]license.lic" />
```

#### 4.2. Dialog konfiguracji API

**Typ:** Custom dialog pozwalający na zmianę portów.

**Treść:**
```
╔═══════════════════════════════════════════════════════════╗
║             Konfiguracja KSeF Printer API                 ║
╚═══════════════════════════════════════════════════════════╝

Port HTTP:  [5000    ]
Port HTTPS: [5001    ]

Ścieżka licencji (automatyczna):
C:\Program Files\KSeF Printer\API\license.lic

[   Wstecz   ]  [   Dalej   ]
```

#### 4.3. XmlFile transformation

**Instalator WiX 5 obsługuje XML transformacje:**
```xml
<Component Id="APIConfigComponent" Directory="APIINSTALLFOLDER" Guid="...">
  <File Id="AppSettingsJson" Source="appsettings.json" KeyPath="yes" />

  <!-- Edycja appsettings.json -->
  <util:XmlFile
    xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util"
    Id="SetLicensePath"
    File="[APIINSTALLFOLDER]appsettings.json"
    Action="setValue"
    ElementPath="/License/FilePath"
    Value="[LICENSE_PATH]"
    SelectionLanguage="XPath" />
</Component>
```

---

### 5. Dodanie dokumentacji

#### 5.1. Struktura folderu Documentation

```
C:\Program Files\KSeF Printer\Documentation\
├── README.md                       # Główny README projektu
├── API_SPECIFICATION.md            # Specyfikacja API
├── INSTALLATION_GUIDE.md           # Instrukcja instalacji
├── LICENSE_GUIDE.md                # Instrukcja licencjonowania
└── CERTIFICATE_GUIDE.md            # Instrukcja konfiguracji certyfikatów
```

#### 5.2. Skrót w Start Menu do dokumentacji

```xml
<Component Id="DocumentationShortcut" Directory="ApplicationProgramsFolder" Guid="...">
  <Shortcut
    Id="DocumentationShortcut"
    Name="Dokumentacja"
    Description="Otwiera folder z dokumentacją KSeF Printer"
    Target="[INSTALLFOLDER]Documentation" />
  <RemoveFolder Id="RemoveDocShortcut" Directory="ApplicationProgramsFolder" On="uninstall" />
  <RegistryValue Root="HKCU" Key="Software\KSeF\KSeFPrinter" Name="DocShortcut" Type="integer" Value="1" KeyPath="yes" />
</Component>
```

---

### 6. Dodanie przykładów wszystkich wariantów faktur

**Obecny stan:** 5 przykładów XML, 2 przykłady PDF

**Docelowy stan:** Wszystkie pliki z `examples/xml/` i `examples/output/`

**Zmiana w `Components.wxs`:**
```xml
<ComponentGroup Id="ExamplesComponents">
  <!-- XML Examples - WSZYSTKIE warianty -->
  <Component Id="ExamplesXMLComponent" Directory="EXAMPLESXMLINSTALLFOLDER" Guid="...">
    <File Source="..\examples\xml\*.xml" />
  </Component>

  <!-- Output Examples - WSZYSTKIE PDF -->
  <Component Id="ExamplesOutputComponent" Directory="EXAMPLESOUTPUTINSTALLFOLDER" Guid="...">
    <File Source="..\examples\output\*.pdf" />
  </Component>
</ComponentGroup>
```

**UWAGA:** WiX 5 obsługuje wildcards (`*.xml`), ale lepiej wymienić pliki jawnie dla kontroli.

---

### 7. Firewall rules (opcjonalne)

```xml
<Component Id="FirewallRulesComponent" Directory="APIINSTALLFOLDER" Guid="...">
  <util:FirewallException
    xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util"
    Id="API_HTTP_Firewall"
    Name="KSeF Printer API (HTTP)"
    Port="[API_PORT_HTTP]"
    Protocol="tcp"
    Scope="localSubnet"
    IgnoreFailure="yes" />

  <util:FirewallException
    Id="API_HTTPS_Firewall"
    Name="KSeF Printer API (HTTPS)"
    Port="[API_PORT_HTTPS]"
    Protocol="tcp"
    Scope="localSubnet"
    IgnoreFailure="yes" />

  <RegistryValue Root="HKCU" Key="Software\KSeF\KSeFPrinter" Name="FirewallRules" Type="integer" Value="1" KeyPath="yes" />
</Component>
```

---

## Instrukcja implementacji

### Priorytet 1: Krytyczne (MUSI być zrobione)

1. ✅ **Dodać system licencjonowania**
   - Plik: `Package.wxs` - dodać folder `Documentation`
   - Plik: `Components.wxs` - dodać komponent z `license.lic.template`
   - Plik: `LicenseDialog.wxs` (nowy) - dialog informacyjny
   - Plik: `ExitDialog_Custom.wxs` (nowy) - custom finish dialog
   - Plik: `LICENSE_GUIDE.md` (nowy) - instrukcja licencjonowania

2. ✅ **Dodać serwis Windows dla API**
   - Plik: `Components.wxs` - dodać `ServiceInstall` i `ServiceControl`
   - Plik: `Package.wxs` - dodać skróty do zarządzania serwisem
   - Plik: `KSeFPrinter.API/Program.cs` - dodać `AddWindowsService()`

3. ✅ **Dodać sprawdzenie .NET Runtime**
   - Plik: `CustomActions.cs` (nowy) - custom action
   - Plik: `DotNetMissingDialog.wxs` (nowy) - dialog ostrzeżenia
   - Plik: `Package.wxs` - dodać Custom Action do InstallUISequence

### Priorytet 2: Ważne (Powinno być zrobione)

4. ✅ **Dodać dokumentację**
   - Plik: `Components.wxs` - dodać komponent z dokumentacją
   - Pliki: `INSTALLATION_GUIDE.md`, `CERTIFICATE_GUIDE.md` (nowe)
   - Plik: `Package.wxs` - skrót do folderu Documentation

5. ✅ **Konfiguracja appsettings.json**
   - Plik: `ConfigurationDialog.wxs` (nowy) - dialog konfiguracji
   - Plik: `Components.wxs` - dodać XmlFile transformation
   - Plik: `Package.wxs` - dodać właściwości API_PORT_HTTP, API_PORT_HTTPS

### Priorytet 3: Opcjonalne (Nice to have)

6. ✅ **Dodać ikonę aplikacji**
7. ✅ **Dodać wszystkie przykłady faktur**
8. ✅ **Dodać firewall rules**

---

## Checklist implementacji

### Krytyczne

- [ ] `Package.wxs` - dodać folder `Documentation`
- [ ] `Components.wxs` - dodać komponent `license.lic.template`
- [ ] `LicenseDialog.wxs` (nowy) - dialog informacyjny o licencji
- [ ] `ExitDialog_Custom.wxs` (nowy) - custom finish dialog z instrukcjami
- [ ] `LICENSE_GUIDE.md` (nowy) - szczegółowa instrukcja licencjonowania
- [ ] `Components.wxs` - dodać `ServiceInstall` dla API
- [ ] `Package.wxs` - dodać skróty do zarządzania serwisem Windows
- [ ] `KSeFPrinter.API/Program.cs` - dodać `AddWindowsService()`
- [ ] `CustomActions.cs` (nowy) - sprawdzanie .NET 9.0 Runtime
- [ ] `DotNetMissingDialog.wxs` (nowy) - dialog ostrzeżenia o braku .NET

### Ważne

- [ ] `INSTALLATION_GUIDE.md` (nowy) - kompletna instrukcja instalacji
- [ ] `CERTIFICATE_GUIDE.md` (nowy) - instrukcja konfiguracji certyfikatów
- [ ] `Components.wxs` - dodać wszystkie pliki dokumentacji
- [ ] `Package.wxs` - skrót do folderu Documentation w Start Menu
- [ ] `ConfigurationDialog.wxs` (nowy) - dialog konfiguracji portów API
- [ ] `Components.wxs` - XmlFile transformation dla appsettings.json

### Opcjonalne

- [ ] `icon.ico` - dodać ikonę aplikacji
- [ ] `Package.wxs` - odkomentować konfigurację ikony
- [ ] `Components.wxs` - dodać wszystkie przykłady faktur (wildcards)
- [ ] `Components.wxs` - dodać firewall rules dla API

---

**Szacowany czas implementacji:**
- Priorytet 1 (Krytyczne): 8-12 godzin
- Priorytet 2 (Ważne): 4-6 godzin
- Priorytet 3 (Opcjonalne): 2-4 godziny

**SUMA:** 14-22 godziny pracy

---

**Koniec specyfikacji**

Wygenerowano: 2025-01-02
