# Generate WiX component files for CLI and API
param(
    [string]$CLIPath = "..\KSeFPrinter.CLI\bin\Release\net9.0\win-x64\publish",
    [string]$APIPath = "..\KSeFPrinter.API\bin\Release\net9.0\win-x64\publish"
)

$ErrorActionPreference = "Stop"

function Get-SafeId {
    param([string]$name)
    $safe = $name -replace '[^a-zA-Z0-9_.]', '_'
    $safe = $safe -replace '\.', '_'
    if ($safe -match '^\d') { $safe = "F_$safe" }
    return $safe
}

function Generate-ComponentGroup {
    param(
        [string]$Path,
        [string]$GroupId,
        [string]$DirectoryRef,
        [string[]]$ExcludeFiles = @()
    )

    $fullPath = (Resolve-Path $Path).Path
    Write-Host "  Scanning: $fullPath"

    $files = Get-ChildItem -Path $fullPath -Recurse -File | Where-Object {
        $_.Name -notin $ExcludeFiles
    }

    Write-Host "  Found: $($files.Count) files"

    $xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="$($GroupId)Components" Directory="$DirectoryRef">
"@

    $componentId = 1
    foreach ($file in $files) {
        # Oblicz ścieżkę względną względem $fullPath
        $relativePath = $file.FullName.Substring($fullPath.TrimEnd('\').Length + 1)
        # Użyj pełnej ścieżki względnej zamiast tylko nazwy pliku, aby uniknąć duplikatów
        $safeId = Get-SafeId $relativePath
        $guid = [guid]::NewGuid().ToString().ToUpper()

        $xml += @"

      <Component Id="$($GroupId)_$safeId`_$componentId" Guid="$guid">
        <File Id="$($GroupId)_File_$componentId" Source="`$(var.$($GroupId)PublishDir)$relativePath" />
      </Component>
"@
        $componentId++
    }

    $xml += @"

    </ComponentGroup>
  </Fragment>
</Wix>
"@

    return $xml
}

Write-Host ""
Write-Host "=== Generating WiX Components ===" -ForegroundColor Cyan
Write-Host ""

# Generate CLI components
Write-Host "1. CLI Components:" -ForegroundColor Yellow
$cliXml = Generate-ComponentGroup -Path $CLIPath -GroupId "CLI" -DirectoryRef "CLIINSTALLFOLDER"
$cliXml | Out-File -FilePath "CLI_Generated.wxs" -Encoding UTF8
Write-Host "  ✓ CLI_Generated.wxs created" -ForegroundColor Green
Write-Host ""

# Generate API components (exclude KSeFPrinter.API.exe - added separately with service)
Write-Host "2. API Components:" -ForegroundColor Yellow
$apiXml = Generate-ComponentGroup -Path $APIPath -GroupId "API" -DirectoryRef "APIINSTALLFOLDER" -ExcludeFiles @("KSeFPrinter.API.exe")
$apiXml | Out-File -FilePath "API_Generated.wxs" -Encoding UTF8
Write-Host "  ✓ API_Generated.wxs created" -ForegroundColor Green
Write-Host ""

Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "Generated files:"
Write-Host "  - CLI_Generated.wxs"
Write-Host "  - API_Generated.wxs"
Write-Host ""
