@echo off
REM Build script for KSeF Printer Installer
REM Wrapper dla build.ps1

setlocal

set "PS_SCRIPT=%~dp0build.ps1"

if "%1"=="clean" (
    powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -Clean
) else (
    powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
)

endlocal
