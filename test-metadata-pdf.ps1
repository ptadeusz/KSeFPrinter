# Test generowania PDF z metadatą pliku źródłowego

$apiUrl = "http://localhost:5000/api/Invoice/generate-pdf"

# Wczytaj przykładowy XML
$xmlPath = "C:\aplikacje\ksefprinter\examples\xml\FakturaTEST017.xml"
$xmlContent = Get-Content $xmlPath -Raw

# Przygotuj request z metadatą źródła
$requestBody = @{
    xmlContent = $xmlContent
    isBase64 = $false
    ksefNumber = ""
    useProduction = $false
    validateInvoice = $true
    returnFormat = "base64"
    sourceFile = @{
        fileName = "invoice_test_001.csv"
        fileHash = "abc123def456789abcdef0123456789abcdef0123456789abcdef0123456789ab"
        format = "csv"
        convertedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        conversionId = 12345
        additionalInfo = "ERP Test System v1.0"
    }
} | ConvertTo-Json -Depth 10

Write-Host "Wysyłanie requestu z metadatą źródła..." -ForegroundColor Cyan
Write-Host ""
Write-Host "Metadata:" -ForegroundColor Yellow
Write-Host "  FileName: invoice_test_001.csv"
Write-Host "  FileHash: abc123..."
Write-Host "  Format: csv"
Write-Host "  ConvertedAt: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $requestBody -ContentType "application/json"

    if ($response.pdfBase64) {
        $outputPath = "C:\aplikacje\ksefprinter\examples\output\TEST_With_Metadata.pdf"
        $pdfBytes = [Convert]::FromBase64String($response.pdfBase64)
        [System.IO.File]::WriteAllBytes($outputPath, $pdfBytes)

        Write-Host "✅ PDF wygenerowany pomyślnie!" -ForegroundColor Green
        Write-Host "   Rozmiar: $($pdfBytes.Length) bajtów"
        Write-Host "   Zapisano: $outputPath"
        Write-Host ""
        Write-Host "Sprawdź metadata PDF (Properties -> Details w Adobe Reader)" -ForegroundColor Cyan

        # Otwórz PDF
        Start-Process $outputPath
    }
}
catch {
    Write-Host "❌ Błąd: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.Exception.Response
}
