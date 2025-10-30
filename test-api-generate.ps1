# Test API endpoint for PDF generation
$xml = Get-Content "examples\xml\FakturaTEST017.xml" -Raw

$body = @{
    xmlContent = $xml
    isBase64 = $false
    validateInvoice = $false
    returnFormat = "base64"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
        -Method Post -Body $body -ContentType "application/json"

    Write-Host "✓ API Response received!" -ForegroundColor Green
    Write-Host "PDF Base64 length: $($response.pdfBase64.Length) chars"
    Write-Host "Invoice Number: $($response.invoiceNumber)"
    Write-Host "Mode: $($response.mode)"
    Write-Host "File Size: $($response.fileSizeBytes) bytes"

    # Save PDF to file for verification
    $pdfBytes = [Convert]::FromBase64String($response.pdfBase64)
    [System.IO.File]::WriteAllBytes("test-api-output.pdf", $pdfBytes)
    Write-Host "✓ PDF saved to test-api-output.pdf" -ForegroundColor Green

} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.Exception
}
