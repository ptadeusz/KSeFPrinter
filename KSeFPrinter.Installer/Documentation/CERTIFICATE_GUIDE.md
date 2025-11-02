# Przewodnik konfiguracji certyfikatów - KSeF Printer

**Wersja:** 1.0.0
**Data:** 2025-01-02

---

## Spis treści

1. [Informacje ogólne](#informacje-ogólne)
2. [Kiedy certyfikat jest wymagany](#kiedy-certyfikat-jest-wymagany)
3. [Typy kodów QR](#typy-kodów-qr)
4. [Opcje przechowywania certyfikatów](#opcje-przechowywania-certyfikatów)
5. [Konfiguracja: Windows Certificate Store](#konfiguracja-windows-certificate-store)
6. [Konfiguracja: Azure Key Vault](#konfiguracja-azure-key-vault)
7. [Testowanie konfiguracji](#testowanie-konfiguracji)
8. [Rozwiązywanie problemów](#rozwiązywanie-problemów)
9. [FAQ](#faq)

---

## Informacje ogólne

KSeF Printer umożliwia generowanie faktur PDF z **kodami QR** zgodnie ze specyfikacją Krajowego Systemu e-Faktur (KSeF).

Istnieją **dwa typy** kodów QR:
1. **KOD QR I** - prosty, bez podpisu (nie wymaga certyfikatu)
2. **KOD QR II** - z podpisem cyfrowym (wymaga certyfikatu kwalifikowanego)

**Certyfikat jest opcjonalny** - aplikacja działa bez niego, ale może generować tylko KOD QR I.

---

## Kiedy certyfikat jest wymagany

### ✅ Certyfikat NIE jest wymagany gdy:
- Generujesz faktury PDF z **KOD QR I** (podstawowy)
- Używasz aplikacji tylko do konwersji XML → PDF bez QR
- Pracujesz w środowisku testowym

### ⚠️ Certyfikat JEST wymagany gdy:
- Chcesz generować **KOD QR II** (z podpisem cyfrowym)
- Podpis cyfrowy jest wymagany przez przepisy/regulaminy
- Faktury muszą być zgodne z wymogami KSeF dla podpisanych QR

---

## Typy kodów QR

### KOD QR I (bez podpisu)

**Zawiera:**
- Numer KSeF
- URL weryfikacyjny

**Przykład:**
```
https://ksef.mf.gov.pl/web/check/1234567890-20250102-ABCD1234-12
```

**Zalety:**
- ✅ Nie wymaga certyfikatu
- ✅ Prosty w implementacji
- ✅ Szybkie generowanie

**Wady:**
- ❌ Brak podpisu cyfrowego
- ❌ Brak dodatkowej weryfikacji autentyczności

---

### KOD QR II (z podpisem)

**Zawiera:**
- Numer KSeF
- URL weryfikacyjny
- **Podpis cyfrowy** (RSA/ECDSA)

**Przykład:**
```json
{
  "ksefNumber": "1234567890-20250102-ABCD1234-12",
  "url": "https://ksef.mf.gov.pl/web/check/...",
  "signature": "BASE64_SIGNATURE_HERE"
}
```

**Zalety:**
- ✅ Podpis cyfrowy gwarantuje autentyczność
- ✅ Zgodność z wymogami KSeF
- ✅ Możliwość weryfikacji offline

**Wady:**
- ❌ Wymaga certyfikatu kwalifikowanego
- ❌ Koszt uzyskania certyfikatu
- ❌ Konieczność zarządzania certyfikatem

---

## Opcje przechowywania certyfikatów

KSeF Printer obsługuje **dwa sposoby** przechowywania certyfikatów:

| Opcja | Zalety | Wady | Zalecane dla |
|-------|--------|------|--------------|
| **Windows Certificate Store** | Lokalny dostęp<br/>Szybki<br/>Darmowy | Mniej bezpieczny<br/>Brak centralnego zarządzania | Małe firmy<br/>Środowiska testowe |
| **Azure Key Vault** | Wysoka bezpieczeństwo<br/>Centralne zarządzanie<br/>Audyt dostępu | Wymaga Azure<br/>Koszt (niewielki)<br/>Wymaga konfiguracji | Duże firmy<br/>Środowiska produkcyjne<br/>Wiele serwerów |

**Zalecenie:**
- **Produkcja:** Azure Key Vault
- **Test/Development:** Windows Certificate Store

---

## Konfiguracja: Windows Certificate Store

### Krok 1: Uzyskaj certyfikat kwalifikowany

Certyfikat musi być:
- **Kwalifikowany** (zgodny z eIDAS)
- Wydany przez **zaufany urząd certyfikacji** (np. Certum, KIR, Unizeto)
- Typ: **podpis elektroniczny** lub **uniwersalny**
- Format: `.pfx` (PKCS#12) z kluczem prywatnym

**Przykładowi dostawcy certyfikatów w Polsce:**
- **Certum** - https://www.certum.pl
- **KIR** - https://www.kir.pl
- **Unizeto** - https://www.unizeto.pl

**Koszt:** 100-400 PLN/rok (w zależności od dostawcy)

---

### Krok 2: Zainstaluj certyfikat w Windows

**Opcja A: Podwójne kliknięcie na plik `.pfx`**

1. Otwórz plik `.pfx`
2. Wybierz: **Current User** lub **Local Machine**
   - **Current User:** certyfikat dostępny tylko dla bieżącego użytkownika
   - **Local Machine:** certyfikat dostępny dla wszystkich użytkowników i serwisów (zalecane dla API)
3. Wprowadź hasło do `.pfx`
4. Wybierz: **Automatically select the certificate store**
5. Kliknij **Next** → **Finish**

**Opcja B: PowerShell (zalecane dla automatyzacji)**

```powershell
# Zainstaluj certyfikat w Local Machine (dla serwisu Windows)
$password = ConvertTo-SecureString -String "HASLO_DO_PFX" -Force -AsPlainText
Import-PfxCertificate `
    -FilePath "C:\certs\moj-certyfikat.pfx" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -Password $password

# Wyświetl zainstalowany certyfikat
Get-ChildItem -Path "Cert:\LocalMachine\My" | Where-Object { $_.Subject -like "*Twoja Firma*" }
```

**UWAGA:** Dla serwisu Windows API używaj **Local Machine**, nie **Current User**!

---

### Krok 3: Znajdź thumbprint certyfikatu

**PowerShell:**
```powershell
# Znajdź certyfikat po nazwie podmiotu
Get-ChildItem -Path "Cert:\LocalMachine\My" | Where-Object { $_.Subject -like "*Twoja Firma*" } | Select-Object Subject, Thumbprint, NotAfter

# Wynik:
# Subject                             Thumbprint                               NotAfter
# -------                             ----------                               --------
# CN=Twoja Firma Sp. z o.o., ...     A1B2C3D4E5F6789012345678901234567890ABCD  12/31/2026 11:59:59 PM
```

**Certmgr.msc (GUI):**
1. Uruchom: `certmgr.msc`
2. Przejdź do: **Personal** → **Certificates**
3. Znajdź swój certyfikat
4. Kliknij prawym → **Properties** → zakładka **Details**
5. Znajdź pole **Thumbprint**

**Skopiuj thumbprint** - będzie potrzebny w konfiguracji!

---

### Krok 4: Skonfiguruj aplikację

**Dla CLI:**

Edytuj: `C:\Program Files\KSeF Printer\CLI\appsettings.json`

```json
{
  "QRCode": {
    "CertificateSource": "WindowsStore",
    "WindowsCertificate": {
      "StoreLocation": "LocalMachine",
      "StoreName": "My",
      "Thumbprint": "A1B2C3D4E5F6789012345678901234567890ABCD"
    }
  }
}
```

**Dla API:**

Edytuj: `C:\Program Files\KSeF Printer\API\appsettings.json`

```json
{
  "QRCode": {
    "CertificateSource": "WindowsStore",
    "WindowsCertificate": {
      "StoreLocation": "LocalMachine",
      "StoreName": "My",
      "Thumbprint": "A1B2C3D4E5F6789012345678901234567890ABCD"
    }
  }
}
```

**WAŻNE:** Użyj **dokładnego thumbprint** skopiowanego z kroku 3!

---

### Krok 5: Nadaj uprawnienia (dla serwisu API)

Jeśli API działa jako **Windows Service**, serwis potrzebuje uprawnień do klucza prywatnego certyfikatu.

**PowerShell (jako Administrator):**
```powershell
# Znajdź certyfikat
$thumbprint = "A1B2C3D4E5F6789012345678901234567890ABCD"
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My\$thumbprint"

# Znajdź ścieżkę do klucza prywatnego
$rsaCert = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
$fileName = $rsaCert.Key.UniqueName

# Nadaj uprawnienia dla LocalSystem (serwis Windows)
$keyPath = "C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\$fileName"
icacls $keyPath /grant "NT AUTHORITY\SYSTEM:(R)"
```

**Weryfikacja:**
```powershell
icacls $keyPath
# Powinno zawierać: NT AUTHORITY\SYSTEM:(R)
```

---

## Konfiguracja: Azure Key Vault

### Krok 1: Utwórz Azure Key Vault

**Azure Portal:**
1. Zaloguj się do https://portal.azure.com
2. Przejdź do: **Key Vaults** → **Create**
3. Wypełnij:
   - **Resource group:** wybierz lub utwórz nową
   - **Key vault name:** np. `ksef-printer-vault`
   - **Region:** Poland Central (lub najbliższy)
   - **Pricing tier:** Standard
4. Zakładka **Access configuration:**
   - **Permission model:** Azure role-based access control (RBAC)
5. Kliknij: **Review + Create** → **Create**

**Azure CLI:**
```bash
# Zaloguj się
az login

# Utwórz Key Vault
az keyvault create \
  --name ksef-printer-vault \
  --resource-group MojaGrupa \
  --location polandcentral \
  --enable-rbac-authorization true
```

---

### Krok 2: Prześlij certyfikat do Key Vault

**Azure Portal:**
1. Otwórz swój Key Vault
2. Przejdź do: **Certificates** → **Generate/Import**
3. Wybierz: **Import**
4. Wypełnij:
   - **Certificate Name:** `ksef-signing-cert`
   - **Upload Certificate File:** wybierz plik `.pfx`
   - **Password:** hasło do `.pfx`
5. Kliknij: **Create**

**Azure CLI:**
```bash
az keyvault certificate import \
  --vault-name ksef-printer-vault \
  --name ksef-signing-cert \
  --file moj-certyfikat.pfx \
  --password HASLO_DO_PFX
```

**PowerShell:**
```powershell
# Zainstaluj moduł (jeśli nie masz)
Install-Module -Name Az.KeyVault -Force

# Prześlij certyfikat
Import-AzKeyVaultCertificate `
    -VaultName "ksef-printer-vault" `
    -Name "ksef-signing-cert" `
    -FilePath "C:\certs\moj-certyfikat.pfx" `
    -Password (ConvertTo-SecureString -String "HASLO_DO_PFX" -AsPlainText -Force)
```

---

### Krok 3: Nadaj uprawnienia aplikacji

Aplikacja potrzebuje uprawnień do **odczytu certyfikatów** z Key Vault.

**Opcja A: Managed Identity (zalecane dla Azure VM/App Service)**

1. Włącz **System-assigned Managed Identity** dla serwera/aplikacji
2. Nadaj uprawnienia:

```bash
# Pobierz ID Key Vault
VAULT_ID=$(az keyvault show --name ksef-printer-vault --query id -o tsv)

# Nadaj uprawnienia Managed Identity
az role assignment create \
  --role "Key Vault Certificates User" \
  --assignee PRINCIPAL_ID_MANAGED_IDENTITY \
  --scope $VAULT_ID
```

**Opcja B: Service Principal (dla on-premise)**

1. Utwórz Service Principal:
```bash
az ad sp create-for-rbac --name ksef-printer-app
# Zapisz: appId, password, tenant
```

2. Nadaj uprawnienia:
```bash
az role assignment create \
  --role "Key Vault Certificates User" \
  --assignee APP_ID \
  --scope $VAULT_ID
```

---

### Krok 4: Skonfiguruj aplikację

**Dla CLI:**

Edytuj: `C:\Program Files\KSeF Printer\CLI\appsettings.json`

```json
{
  "QRCode": {
    "CertificateSource": "AzureKeyVault",
    "AzureKeyVault": {
      "VaultUrl": "https://ksef-printer-vault.vault.azure.net/",
      "CertificateName": "ksef-signing-cert",
      "UseManagedIdentity": true,
      "TenantId": null,
      "ClientId": null,
      "ClientSecret": null
    }
  }
}
```

**Jeśli używasz Service Principal (nie Managed Identity):**
```json
{
  "QRCode": {
    "CertificateSource": "AzureKeyVault",
    "AzureKeyVault": {
      "VaultUrl": "https://ksef-printer-vault.vault.azure.net/",
      "CertificateName": "ksef-signing-cert",
      "UseManagedIdentity": false,
      "TenantId": "12345678-1234-1234-1234-123456789012",
      "ClientId": "87654321-4321-4321-4321-210987654321",
      "ClientSecret": "TAJNY_KLUCZ_TUTAJ"
    }
  }
}
```

**WAŻNE:** Nigdy nie commituj `ClientSecret` do repozytorium Git!

---

### Krok 5: Testowanie połączenia

**PowerShell:**
```powershell
# Test połączenia z Key Vault
az keyvault certificate show \
  --vault-name ksef-printer-vault \
  --name ksef-signing-cert

# Wynik powinien zawierać informacje o certyfikacie
```

**Aplikacja CLI:**
```bash
cd "C:\Program Files\KSeF Printer\CLI"
ksef-pdf.exe generate-qr-code --ksef-number "1234567890-20250102-ABCD1234-12" --output test-qr.png --type Signed

# Jeśli działa - QR zostanie wygenerowany z podpisem
```

---

## Testowanie konfiguracji

### Test 1: Weryfikacja dostępu do certyfikatu

**Windows Certificate Store:**
```powershell
$thumbprint = "A1B2C3D4E5F6789012345678901234567890ABCD"
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My\$thumbprint"

if ($cert) {
    Write-Host "✅ Certyfikat znaleziony" -ForegroundColor Green
    Write-Host "   Subject: $($cert.Subject)"
    Write-Host "   Valid until: $($cert.NotAfter)"

    # Sprawdź klucz prywatny
    if ($cert.HasPrivateKey) {
        Write-Host "✅ Klucz prywatny dostępny" -ForegroundColor Green
    } else {
        Write-Host "❌ Brak klucza prywatnego!" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Certyfikat nie znaleziony!" -ForegroundColor Red
}
```

**Azure Key Vault:**
```powershell
# Test dostępu
az keyvault certificate show \
  --vault-name ksef-printer-vault \
  --name ksef-signing-cert \
  --query "attributes.enabled" -o tsv

# Wynik: true (jeśli certyfikat jest aktywny)
```

---

### Test 2: Generowanie QR z podpisem (CLI)

```bash
cd "C:\Program Files\KSeF Printer\CLI"

# Test KOD QR II (podpisany)
ksef-pdf.exe generate-qr-code `
  --ksef-number "1234567890-20250102-ABCD1234-12" `
  --output "test-qr-signed.png" `
  --type Signed `
  --width 300 `
  --height 300

# Sprawdź, czy plik został utworzony
if (Test-Path "test-qr-signed.png") {
    Write-Host "✅ QR Code wygenerowany pomyślnie!"
} else {
    Write-Host "❌ Błąd generowania QR Code!"
}
```

---

### Test 3: API endpoint dla QR

**PowerShell:**
```powershell
# Uruchom API (jeśli nie działa)
net start KSeFPrinterAPI

# Test endpoint
$body = @{
    ksefNumber = "1234567890-20250102-ABCD1234-12"
    type = "Signed"
    width = 300
    height = 300
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://localhost:5000/api/invoice/generate-qr-code" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -OutFile "test-qr-api.png"

# Sprawdź wynik
if (Test-Path "test-qr-api.png") {
    Write-Host "✅ API działa poprawnie!"
} else {
    Write-Host "❌ Błąd API!"
}
```

---

## Rozwiązywanie problemów

### Problem 1: "Certyfikat nie znaleziony"

**Błąd:**
```
❌ Nie znaleziono certyfikatu o thumbprint: A1B2C3D4...
```

**Rozwiązanie:**
1. Sprawdź thumbprint w certmgr.msc
2. Upewnij się, że używasz **LocalMachine**, nie **CurrentUser**
3. Sprawdź, czy certyfikat jest zainstalowany:
   ```powershell
   Get-ChildItem -Path "Cert:\LocalMachine\My" | Format-Table Subject, Thumbprint
   ```
4. Zweryfikuj konfigurację w `appsettings.json`

---

### Problem 2: "Brak klucza prywatnego"

**Błąd:**
```
❌ Certyfikat nie zawiera klucza prywatnego
```

**Przyczyna:** Zaimportowano tylko certyfikat publiczny (`.cer`), nie pełny `.pfx`

**Rozwiązanie:**
1. Usuń certyfikat bez klucza prywatnego
2. Zaimportuj pełny plik `.pfx` (zawiera klucz prywatny)
3. Przy imporcie zaznacz: **Mark this key as exportable**

---

### Problem 3: "Access denied" do klucza prywatnego (Windows Service)

**Błąd (Event Log):**
```
Keyset does not exist
Access denied to private key
```

**Rozwiązanie:** Nadaj uprawnienia dla konta serwisu (LocalSystem)

```powershell
$thumbprint = "A1B2C3D4..."
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My\$thumbprint"
$rsaCert = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
$fileName = $rsaCert.Key.UniqueName
$keyPath = "C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\$fileName"

# Nadaj uprawnienia
icacls $keyPath /grant "NT AUTHORITY\SYSTEM:(R)"

# Restart serwisu
net stop KSeFPrinterAPI
net start KSeFPrinterAPI
```

---

### Problem 4: "Azure Key Vault: Forbidden (403)"

**Błąd:**
```
❌ HTTP 403: The user, group or application does not have certificates get permission
```

**Rozwiązanie:**

**Sprawdź uprawnienia:**
```bash
# Sprawdź RBAC
az role assignment list --scope /subscriptions/.../resourceGroups/.../providers/Microsoft.KeyVault/vaults/ksef-printer-vault

# Nadaj uprawnienia
az role assignment create \
  --role "Key Vault Certificates User" \
  --assignee YOUR_APP_ID \
  --scope VAULT_ID
```

**Managed Identity:**
- Upewnij się, że Managed Identity jest włączona
- Sprawdź, czy nadano uprawnienia dla właściwej tożsamości

---

### Problem 5: "Certificate expired"

**Błąd:**
```
❌ Certyfikat wygasł: 2024-12-31
```

**Rozwiązanie:**
1. Odnów certyfikat u dostawcy (Certum, KIR, etc.)
2. Zaimportuj nowy certyfikat:
   ```powershell
   Import-PfxCertificate `
       -FilePath "C:\certs\nowy-certyfikat.pfx" `
       -CertStoreLocation "Cert:\LocalMachine\My" `
       -Password (ConvertTo-SecureString -String "HASLO" -AsPlainText -Force)
   ```
3. Zaktualizuj thumbprint w `appsettings.json`
4. Restart aplikacji/serwisu

---

## FAQ

### Czy mogę używać certyfikatu testowego?

**TAK** - dla środowiska testowego możesz użyć self-signed certificate:

```powershell
# Wygeneruj self-signed cert (PowerShell jako Administrator)
$cert = New-SelfSignedCertificate `
    -Subject "CN=Test KSeF Printer" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(2)

Write-Host "Thumbprint: $($cert.Thumbprint)"
```

**UWAGA:** Self-signed certyfikaty NIE są akceptowane w produkcji!

---

### Ile kosztuje certyfikat kwalifikowany?

**Koszt roczny:**
- **Certum:** 100-200 PLN
- **KIR:** 150-250 PLN
- **Unizeto:** 200-400 PLN

**Azure Key Vault:**
- **Certificate operations:** ~0.03 USD za operację
- **Storage:** Darmowy (pierwsze 10,000 operacji/miesiąc)

---

### Czy certyfikat musi być na nazwę firmy?

**TAK** - dla produkcji certyfikat powinien być:
- Wydany na **osobę prawną** (NIP firmy)
- Zawierać dane zgodne z KRS
- Typ: podpis kwalifikowany lub uniwersalny

---

### Jak często certyfikat się automatycznie odnawia?

**Windows Certificate Store:** Nie odnawia się automatycznie - musisz ręcznie odnowić u dostawcy

**Azure Key Vault:** Można skonfigurować automatyczne odnawianie:
1. Azure Portal → Key Vault → Certificates
2. Wybierz certyfikat → **Lifetime Actions**
3. Ustaw: **Auto-renew before expiry**

**UWAGA:** Automatyczne odnawianie działa tylko dla certyfikatów zakupionych przez Azure!

---

### Czy mogę używać różnych certyfikatów dla CLI i API?

**TAK** - CLI i API mają osobne pliki `appsettings.json`. Możesz skonfigurować:
- CLI: Windows Certificate Store
- API: Azure Key Vault

Lub odwrotnie - zależy od potrzeb.

---

### Czy certyfikat musi być w formacie `.pfx`?

**TAK** - dla importu do Windows/Azure wymagany jest format `.pfx` (PKCS#12).

**Jeśli masz `.cer` + `.key`:**
```bash
# Konwersja do .pfx (OpenSSL)
openssl pkcs12 -export \
  -out certyfikat.pfx \
  -inkey klucz-prywatny.key \
  -in certyfikat.cer \
  -password pass:HASLO
```

---

## Kontakt

W razie problemów z konfiguracją certyfikatów:
- **Email:** [email wsparcia]
- **Telefon:** [telefon wsparcia]
- **Dokumentacja:** `C:\Program Files\KSeF Printer\Documentation\`

---

**Koniec przewodnika**

Wygenerowano: 2025-01-02
Wersja dokumentu: 1.0.0
