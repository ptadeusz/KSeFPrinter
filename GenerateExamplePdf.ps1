# Skrypt do generowania przykładowego PDF
Write-Host "Generowanie przykładowego PDF z faktury testowej..." -ForegroundColor Cyan

# Kompilacja projektu
dotnet build --configuration Release

# Uruchomienie testu który generuje PDF
dotnet test --filter "FullyQualifiedName~GeneratePdfToFileAsync_Should_Create_Pdf_File" --logger "console;verbosity=minimal"

Write-Host "`nPDF zostal wygenerowany przez test, ale zapisany w katalogu tymczasowym." -ForegroundColor Yellow
Write-Host "Tworze prosty program do generowania przykladu..." -ForegroundColor Cyan
