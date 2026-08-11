@echo off
REM Quick SSL certificate setup for a custom domain
REM Usage: add-ssl.bat link.client2.com
REM Run as Administrator!

if "%1"=="" (
    echo Usage: add-ssl.bat DOMAIN
    echo Example: add-ssl.bat link.client2.com
    exit /b 1
)

echo.
echo Adding SSL for: %1
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0add-domain-ssl.ps1" -Domain "%1"
pause
