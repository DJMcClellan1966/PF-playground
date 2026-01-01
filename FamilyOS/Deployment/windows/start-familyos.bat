@echo off
echo 🏡 Starting FamilyOS in Production Mode
echo =====================================
echo.

set ASPNETCORE_ENVIRONMENT=Production
set FAMILYOS_CONFIG_PATH=.\appsettings.production.json

echo ✅ Production environment configured
echo 🚀 Launching FamilyOS...
echo.

FamilyOS.exe

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ FamilyOS encountered an error (Exit Code: %ERRORLEVEL%)
    pause
) else (
    echo.
    echo ✅ FamilyOS shut down successfully
)
pause