@echo off
echo 🛡️ FamilyOS Enforcement Setup for Parents
echo =======================================
echo.

REM Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ This script requires administrator privileges
    echo    Right-click and "Run as administrator"
    pause
    exit /b 1
)

echo ✅ Administrator privileges confirmed
echo.

REM Get child username
set /p CHILD_USERNAME="Enter child's username (e.g., sarah_child): "
if "%CHILD_USERNAME%"=="" (
    echo ❌ Username cannot be empty
    pause
    exit /b 1
)

echo.
echo 📋 Enforcement Options:
echo 1. Basic - Startup application only
echo 2. Standard - Remove admin + startup + restrictions  
echo 3. Kiosk - Full lockdown mode
echo.
set /p ENFORCEMENT_LEVEL="Select enforcement level (1-3): "

if "%ENFORCEMENT_LEVEL%"=="1" goto basic
if "%ENFORCEMENT_LEVEL%"=="2" goto standard
if "%ENFORCEMENT_LEVEL%"=="3" goto kiosk
goto invalid

:basic
echo.
echo 🔧 Applying BASIC enforcement...
echo • Setting FamilyOS as startup application

REM Copy FamilyOS to startup folder
mkdir "C:\Users\%CHILD_USERNAME%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup" 2>nul
copy "FamilyOS.exe" "C:\Users\%CHILD_USERNAME%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\" >nul

echo ✅ Basic enforcement applied
goto end

:standard
echo.
echo 🔧 Applying STANDARD enforcement...
echo • Removing administrator privileges
echo • Setting startup application
echo • Applying user restrictions

REM Remove from Administrators group
net localgroup Administrators %CHILD_USERNAME% /delete 2>nul

REM Add to Users group (if not already)
net localgroup Users %CHILD_USERNAME% /add 2>nul

REM Set startup application
mkdir "C:\Users\%CHILD_USERNAME%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup" 2>nul
copy "FamilyOS.exe" "C:\Users\%CHILD_USERNAME%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\" >nul

REM Registry restrictions (disable Task Manager, etc.)
reg add "HKEY_USERS\%CHILD_USERNAME%\Software\Microsoft\Windows\CurrentVersion\Policies\System" /v DisableTaskMgr /t REG_DWORD /d 1 /f >nul 2>&1

echo ✅ Standard enforcement applied
goto end

:kiosk
echo.
echo 🔧 Applying KIOSK enforcement...
echo • Full kiosk mode configuration
echo • Single application access only
echo ⚠️  This requires Windows 10 Pro or Enterprise

REM Check Windows edition
for /f "tokens=3" %%i in ('systeminfo ^| findstr /C:"OS Name"') do set OS_EDITION=%%i

REM Set assigned access (requires PowerShell and proper Windows edition)
powershell -Command "& {Set-AssignedAccess -UserName '%CHILD_USERNAME%' -AppUserModelId 'FamilyOS_App' -ErrorAction SilentlyContinue}"

if %errorlevel% neq 0 (
    echo ⚠️  Kiosk mode setup may have failed
    echo    Ensure Windows 10/11 Pro/Enterprise and AppUserModelId is correct
) else (
    echo ✅ Kiosk mode applied
)

goto end

:invalid
echo ❌ Invalid option selected
pause
exit /b 1

:end
echo.
echo 🎯 ENFORCEMENT SUMMARY:
echo ==============================
echo Child Account: %CHILD_USERNAME%
echo Enforcement Level: %ENFORCEMENT_LEVEL%
echo.
echo 📱 NEXT STEPS FOR PARENTS:
echo • Test login with child account
echo • Verify FamilyOS auto-starts
echo • Configure family member profiles in FamilyOS
echo • Set up parental control rules
echo.
echo 🚨 EMERGENCY ACCESS:
echo • Use your administrator account to override
echo • Disable assigned access: Set-AssignedAccess -ClearConfig (PowerShell)
echo • Remove startup: Delete from Startup folder
echo.
echo ✅ Enforcement setup complete!
pause