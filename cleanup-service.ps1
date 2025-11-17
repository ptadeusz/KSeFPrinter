# Skrypt do czyszczenia usługi KSeFPrinterAPI
# Uruchom jako Administrator

Write-Host "=== Czyszczenie usługi KSeF Printer API ===" -ForegroundColor Cyan
Write-Host ""

# Sprawdź czy usługa istnieje
$service = Get-Service -Name "KSeFPrinterAPI" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "Znaleziono usługę KSeFPrinterAPI:" -ForegroundColor Yellow
    Write-Host "  Status: $($service.Status)" -ForegroundColor White
    Write-Host "  Start Type: $($service.StartType)" -ForegroundColor White
    Write-Host ""

    # Zatrzymaj usługę jeśli działa
    if ($service.Status -eq 'Running') {
        Write-Host "Zatrzymywanie usługi..." -ForegroundColor Yellow
        Stop-Service -Name "KSeFPrinterAPI" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "✓ Usługa zatrzymana" -ForegroundColor Green
    }

    # Usuń usługę
    Write-Host "Usuwanie usługi..." -ForegroundColor Yellow
    sc.exe delete KSeFPrinterAPI
    Start-Sleep -Seconds 1

    # Sprawdź czy usunięto
    $serviceCheck = Get-Service -Name "KSeFPrinterAPI" -ErrorAction SilentlyContinue
    if ($serviceCheck) {
        Write-Host "✗ Nie udało się usunąć usługi" -ForegroundColor Red
        Write-Host "  Może być używana przez inny proces. Spróbuj zrestartować komputer." -ForegroundColor Yellow
    } else {
        Write-Host "✓ Usługa usunięta pomyślnie" -ForegroundColor Green
    }
} else {
    Write-Host "Usługa KSeFPrinterAPI nie istnieje" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Zakończono ===" -ForegroundColor Cyan
Write-Host "Możesz teraz uruchomić instalator ponownie." -ForegroundColor White
