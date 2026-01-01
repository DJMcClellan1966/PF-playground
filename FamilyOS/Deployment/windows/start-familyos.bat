@echo off
set ASPNETCORE_ENVIRONMENT=Production
set FAMILYOS_CONFIG_PATH=.\appsettings.production.json
echo Starting FamilyOS in Production mode...
FamilyOS.exe
pause
