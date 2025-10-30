# Simple API test with base64
$xmlBytes = [System.IO.File]::ReadAllBytes("examples\xml\FakturaTEST017.xml")
$xmlBase64 = [Convert]::ToBase64String($xmlBytes)

$json = @{
    xmlContent = $xmlBase64
    isBase64 = $true
    validateInvoice = $false
    returnFormat = "base64"
} | ConvertTo-Json

try {
    Write-Host "Sending request to API..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/generate-pdf" `
        -Method Post -Body $json -ContentType "application/json"

    Write-Host "✓ SUCCESS! PDF generated" -ForegroundColor Green
    Write-Host "Invoice Number: $($response.invoiceNumber)"
    Write-Host "Mode: $($response.mode)"
    Write-Host "PDF Size: $($response.fileSizeBytes) bytes"

    # Save to file
    $pdfBytes = [Convert]::FromBase64String($response.pdfBase64)
    [System.IO.File]::WriteAllBytes("test-api-output.pdf", $pdfBytes)
    Write-Host "✓ Saved to: test-api-output.pdf" -ForegroundColor Green

} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
}
