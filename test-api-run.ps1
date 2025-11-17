# Stop any running dotnet processes
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Start API
cd test-output/API
$proc = Start-Process dotnet -ArgumentList "KSeFPrinter.API.dll --urls http://localhost:6000" `
    -RedirectStandardOutput "output-final.log" `
    -RedirectStandardError "error-final.log" `
    -NoNewWindow -PassThru

Write-Output "Started process ID: $($proc.Id)"
Start-Sleep -Seconds 7

# Show output
Write-Output "`n=== OUTPUT LOG ==="
if (Test-Path "output-final.log") {
    Get-Content "output-final.log"
} else {
    Write-Output "No output log found"
}

Write-Output "`n=== ERROR LOG ==="
if (Test-Path "error-final.log") {
    Get-Content "error-final.log"
} else {
    Write-Output "No error log found"
}

# Check if logs folder was created
Write-Output "`n=== LOGS FOLDER ==="
if (Test-Path "logs") {
    Write-Output "Logs folder exists!"
    Get-ChildItem "logs" -ErrorAction SilentlyContinue
} else {
    Write-Output "Logs folder NOT created"
}
