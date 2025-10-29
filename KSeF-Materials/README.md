# Paczka materiałów do projektu KSeF PDF Generator

## 📦 Zawartość paczki

Ta paczka zawiera kompletne materiały potrzebne do realizacji projektu **KSeF PDF Generator** - komponentu C# (.NET 9) do generowania wizualizacji faktur w formacie PDF na podstawie plików XML zgodnych ze schematem KSeF **FA(3) v1-0E**.

### Pliki w paczce:

1. **KSeF-PDF-Generator-Potrzebne-Elementy.md** ⭐
   - Szczegółowa analiza wszystkich potrzebnych elementów z repozytoriów
   - Lista priorytetów (krytyczne / ważne / przydatne)
   - Plan wykorzystania w kolejnych fazach projektu
   - Harmonogram studiowania materiałów

2. **INSTRUKCJA-POBIERANIA.md**
   - Kompletna lista plików do pobrania z GitHuba
   - Bezpośrednie linki do wszystkich plików
   - 4 różne metody pobierania (ręczne, wget, PowerShell, git clone)
   - Priorytetyzacja pobierania

3. **download-files.sh**
   - Gotowy skrypt bash do automatycznego pobrania wszystkich plików
   - Dla użytkowników Linux/Mac/WSL

4. **download-files.ps1**
   - Gotowy skrypt PowerShell do automatycznego pobrania wszystkich plików
   - Dla użytkowników Windows

### Struktura katalogów (po pobraniu plików):

```
ksef-materials/
├── README.md
├── INSTRUKCJA-POBIERANIA.md
├── KSeF-PDF-Generator-Potrzebne-Elementy.md
├── download-files.sh
├── download-files.ps1
├── ksef-client-csharp/
│   ├── Templates/
│   │   ├── invoice-template-fa-3.xml
│   │   ├── invoice-template-fa-3-with-custom-Subject2.xml
│   │   └── invoice-template-fa-2.xml
│   ├── E2E/
│   │   └── QrCode/
│   │       ├── QrCodeOfflineE2ETests.cs ⭐ NAJWAŻNIEJSZY!
│   │       └── QrCodeE2ETests.cs
│   ├── Api/
│   │   └── Services/
│   │       ├── CryptographyService.cs ⭐
│   │       ├── VerificationLinkService.cs
│   │       └── QrCodeService.cs
│   └── Core/
│       └── KsefNumberValidator.cs
└── ksef-docs/
    ├── faktury/
    │   ├── schemat-FA(3)-v1-0E.xsd ⭐
    │   └── numer-ksef.md
    ├── kody-qr.md
    ├── certyfikaty-KSeF.md
    └── offline/
        └── tryby-offline.md
```

## 🚀 Szybki start

### Opcja 1: Automatyczne pobieranie (Linux/Mac/WSL)

```bash
# 1. Rozpakuj paczkę
unzip ksef-materials.zip
cd ksef-materials

# 2. Uruchom skrypt
./download-files.sh

# Gotowe! Wszystkie pliki zostały pobrane
```

### Opcja 2: Automatyczne pobieranie (Windows PowerShell)

```powershell
# 1. Rozpakuj paczkę
# 2. Otwórz PowerShell w katalogu paczki
# 3. Uruchom skrypt

.\download-files.ps1

# Gotowe! Wszystkie pliki zostały pobrane
```

### Opcja 3: Ręczne pobieranie

Otwórz plik `INSTRUKCJA-POBIERANIA.md` i postępuj zgodnie z instrukcjami. Znajdziesz tam bezpośrednie linki do wszystkich potrzebnych plików.

## 📋 Priorytety studiowania

### 🔴 KRYTYCZNE (zacznij od tego):

1. **KSeF-PDF-Generator-Potrzebne-Elementy.md** (30 min)
   - Przeczytaj cały dokument, aby zrozumieć pełny zakres projektu

2. **schemat-FA(3)-v1-0E.xsd** (2-3 godz)
   - Przestudiuj strukturę XML faktury FA(3)
   - Lista wszystkich elementów i atrybutów

3. **invoice-template-fa-3.xml** (30 min)
   - Zobacz przykład rzeczywistej faktury
   - Mapowanie XSD na konkretne wartości

4. **kody-qr.md** (1 godz)
   - Struktura KOD I i KOD II
   - Algorytmy podpisów (RSA-PSS, ECDSA)

### 🟡 WAŻNE (podczas implementacji):

5. **QrCodeOfflineE2ETests.cs** (2-3 godz) ⚡
   - **NAJWAŻNIEJSZY plik** dla podpisów kryptograficznych!
   - Dokładne przykłady RSA-PSS i ECDSA
   - Generowanie CSR i budowanie URL z podpisem

6. **CryptographyService.cs** (1 godz)
   - Implementacja kryptografii
   - SHA-256, Base64URL encoding

7. **certyfikaty-KSeF.md** (45 min)
   - Typy certyfikatów (Authentication vs Offline)
   - KRYTYCZNE ostrzeżenia o użyciu

### 🟢 PRZYDATNE (opcjonalnie):

8. **VerificationLinkService.cs**, **QrCodeService.cs** (1 godz łącznie)
9. **KsefNumberValidator.cs**, **numer-ksef.md** (30 min)
10. **tryby-offline.md** (20 min)

## ⚠️ Najważniejsze uwagi techniczne

### Krytyczne informacje:
1. **Tylko certyfikat typu `Offline`** może być używany do KODU II
2. **Namespace FA(3):** `http://crd.gov.pl/wzor/2025/06/25/13775/`
3. **Podpis KODU II** jest WYMAGANY - bez niego kod jest bezwartościowy
4. **Format podpisu:** ciąg bez `https://` i bez trailing `/`
5. **CRC-8 dla numeru KSeF:** polinom 0x07, init 0x00

### Algorytmy kryptograficzne:
- **RSA-PSS:** SHA-256, MGF1, salt 32 bajty, min. 2048 bitów
- **ECDSA P-256:** krzywa secp256r1, format IEEE P1363 (preferowany)
- **SHA-256:** dla skrótów faktur
- **Base64URL:** dla kodowania w URL

## 🔗 Linki do repozytoriów

- **ksef-client-csharp:** https://github.com/CIRFMF/ksef-client-csharp
- **ksef-docs:** https://github.com/CIRFMF/ksef-docs

## 📞 Wsparcie

W razie problemów:
1. Sprawdź plik `INSTRUKCJA-POBIERANIA.md` dla szczegółowych instrukcji
2. Odwiedź repozytoria na GitHubie
3. Sklonuj całe repozytoria: `git clone <url>`

## 📅 Harmonogram projektu

**Szacowany czas realizacji:** 8-11 dni roboczych

| Faza | Zadania | Czas |
|------|---------|------|
| 1-2 | Fundament, modele danych FA(3) | 1-2 dni |
| 3 | Parsowanie XML FA(3) | 1 dzień |
| 4 | Walidacja | 1 dzień |
| 5 | Kryptografia (RSA-PSS, ECDSA) | 1-2 dni |
| 6 | Kody QR (KOD I i KOD II) | 1 dzień |
| 7 | Generowanie PDF (QuestPDF) | 2-3 dni |
| 8 | Integracja i testy | 1-2 dni |

## ✅ Gotowość do implementacji

Po przestudiowaniu materiałów będziesz mieć:
- ✅ Pełne zrozumienie struktury FA(3)
- ✅ Przykłady rzeczywistych faktur do testów
- ✅ Gotowe implementacje kryptografii do adaptacji
- ✅ Wzorce generowania kodów QR
- ✅ Dokumentację wszystkich wymagań KSeF
- ✅ Testy E2E jako przykłady użycia

---

**Data przygotowania:** 2025-10-24  
**Projekt:** KSeF PDF Generator  
**Wersja:** 1.0  
**Schemat:** FA(3) v1-0E

**Powodzenia w realizacji projektu! 🚀**
