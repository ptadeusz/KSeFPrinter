# Generate WiX component files - FIXED VERSION
$ErrorActionPreference = "Stop"

function Get-SafeId {
    param([string]$name)
    $safe = $name -replace '[^a-zA-Z0-9_]', '_'
    if ($safe -match '^\d') { $safe = "F_$safe" }
    return $safe
}

function Generate-Components {
    param(
        [string]$PublishDir,
        [string]$GroupName,
        [string]$DirRef,
        [string[]]$Exclude = @()
    )

    # Only get files in root folder (no recursion) to avoid localization duplicates
    $files = Get-ChildItem -Path $PublishDir -File | Where-Object { $_.Name -notin $Exclude }

    Write-Host "  Found $($files.Count) files in $PublishDir"

    $xml = "<?xml version=`"1.0`" encoding=`"UTF-8`"?>`n"
    $xml += "<Wix xmlns=`"http://wixtoolset.org/schemas/v4/wxs`">`n"
    $xml += "  <Fragment>`n"
    $xml += "    <ComponentGroup Id=`"$($GroupName)Components`" Directory=`"$DirRef`">`n"

    $id = 1
    foreach ($file in $files) {
        # Get relative path from publish dir
        $relPath = $file.FullName.Substring($PublishDir.Length).TrimStart('\').Replace('\', '/')
        $safeName = Get-SafeId $file.Name
        $guid = [Guid]::NewGuid().ToString().ToUpper()

        $fileId = "$($GroupName)_File_$id"
        $xml += "      <Component Id=`"$($GroupName)_$safeName`_$id`" Guid=`"$guid`">`n"
        $xml += "        <File Id=`"$fileId`" Source=`"`$(var.$($GroupName)PublishDir)$relPath`" />`n"
        $xml += "      </Component>`n"
        $id++
    }

    $xml += "    </ComponentGroup>`n"
    $xml += "  </Fragment>`n"
    $xml += "</Wix>`n"

    return $xml
}

Write-Host "`n=== Generating WiX Component Files ===`n" -ForegroundColor Cyan

# CLI
Write-Host "1. CLI Components:" -ForegroundColor Yellow
$cliDir = Resolve-Path "..\KSeFPrinter.CLI\bin\Release\net9.0\win-x64\publish"
$cliXml = Generate-Components -PublishDir $cliDir -GroupName "CLI" -DirRef "CLIINSTALLFOLDER"
[System.IO.File]::WriteAllText("$PSScriptRoot\CLI_Generated.wxs", $cliXml, [System.Text.Encoding]::UTF8)
Write-Host "  Created: CLI_Generated.wxs`n" -ForegroundColor Green

# API
Write-Host "2. API Components:" -ForegroundColor Yellow
$apiDir = Resolve-Path "..\KSeFPrinter.API\bin\Release\net9.0\win-x64\publish"
$apiXml = Generate-Components -PublishDir $apiDir -GroupName "API" -DirRef "APIINSTALLFOLDER" -Exclude @("KSeFPrinter.API.exe")
[System.IO.File]::WriteAllText("$PSScriptRoot\API_Generated.wxs", $apiXml, [System.Text.Encoding]::UTF8)
Write-Host "  Created: API_Generated.wxs`n" -ForegroundColor Green

Write-Host "=== Done! ===`n" -ForegroundColor Green
