# Build script for KSeF Printer Installer
# Automatycznie buduje aplikacje i tworzy MSI

param(
    [string]$Configuration = "Release",
    [switch]$SkipPublish,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "=== KSeF Printer Installer Build ===" -ForegroundColor Cyan
Write-Host ""

# Ścieżki
$rootDir = Split-Path $PSScriptRoot -Parent
$cliProject = Join-Path $rootDir "KSeFPrinter.CLI\KSeFPrinter.CLI.csproj"
$apiProject = Join-Path $rootDir "KSeFPrinter.API\KSeFPrinter.API.csproj"
$installerProject = Join-Path $PSScriptRoot "KSeFPrinter.Installer.wixproj"

# Clean
if ($Clean) {
    Write-Host "Czyszczenie poprzednich buildów..." -ForegroundColor Yellow
    dotnet clean $cliProject -c $Configuration
    dotnet clean $apiProject -c $Configuration
    dotnet clean $installerProject -c $Configuration
    Write-Host "✓ Wyczyszczono" -ForegroundColor Green
    Write-Host ""
}

# Publish CLI i API
if (-not $SkipPublish) {
    Write-Host "1. Publikowanie KSeFPrinter.CLI..." -ForegroundColor Yellow
    dotnet publish $cliProject -c $Configuration -r win-x64 --self-contained false
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Błąd podczas publikowania CLI"
        exit 1
    }
    Write-Host "✓ CLI opublikowane" -ForegroundColor Green
    Write-Host ""

    Write-Host "2. Publikowanie KSeFPrinter.API..." -ForegroundColor Yellow
    dotnet publish $apiProject -c $Configuration -r win-x64 --self-contained false
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Błąd podczas publikowania API"
        exit 1
    }
    Write-Host "✓ API opublikowane" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Pomijanie publikowania (używane istniejące pliki)" -ForegroundColor Gray
    Write-Host ""
}

# Build instalatora
Write-Host "3. Budowanie instalatora MSI..." -ForegroundColor Yellow
dotnet build $installerProject -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Błąd podczas budowania instalatora"
    exit 1
}
Write-Host "✓ Instalator zbudowany" -ForegroundColor Green
Write-Host ""

# Wyświetl informacje o wygenerowanym MSI
$msiPath = Join-Path $PSScriptRoot "bin\x64\$Configuration\KSeFPrinter.msi"
if (Test-Path $msiPath) {
    $msiInfo = Get-Item $msiPath
    Write-Host "=== Instalator gotowy ===" -ForegroundColor Green
    Write-Host "Lokalizacja: $msiPath"
    Write-Host "Rozmiar:     $([math]::Round($msiInfo.Length / 1KB, 2)) KB"
    Write-Host "Data:        $($msiInfo.LastWriteTime)"
    Write-Host ""
    Write-Host "Uruchom instalator poleceniem:" -ForegroundColor Cyan
    Write-Host "  msiexec /i `"$msiPath`"" -ForegroundColor White
    Write-Host ""
    Write-Host "Lub zainstaluj po cichu:" -ForegroundColor Cyan
    Write-Host "  msiexec /i `"$msiPath`" /qn" -ForegroundColor White
} else {
    Write-Error "Nie znaleziono pliku MSI w oczekiwanej lokalizacji: $msiPath"
    exit 1
}
