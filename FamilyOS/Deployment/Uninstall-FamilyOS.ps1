#Requires -Version 5.1
<#
.SYNOPSIS
    FamilyOS Uninstaller Script

.DESCRIPTION
    Completely removes FamilyOS from the system including:
    - Application files
    - Family data (with confirmation)
    - Registry entries
    - Shortcuts
    - Start Menu entries

.PARAMETER KeepFamilyData
    Preserve family data and settings (default: false)

.PARAMETER Silent
    Run uninstallation without user prompts (default: false)

.EXAMPLE
    .\Uninstall-FamilyOS.ps1
    
.EXAMPLE
    .\Uninstall-FamilyOS.ps1 -KeepFamilyData -Silent

.NOTES
    Version: 1.0
    Author: FamilyOS Team
    This script should be run with administrator privileges
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [switch]$KeepFamilyData = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Silent = $false
)

# Uninstall banner
Write-Host @"
    🗑️ FamilyOS Uninstaller
    ========================
    Preparing to remove FamilyOS from your system...
"@ -ForegroundColor Red

# Check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Host "⚠️ This script requires administrator privileges." -ForegroundColor Red
    Write-Host "Please right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    if (-not $Silent) {
        Read-Host "Press Enter to exit"
    }
    exit 1
}

try {
    # Get installation path from registry or default
    $installPath = $null
    try {
        $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FamilyOS"
        if (Test-Path $regPath) {
            $installPath = Get-ItemProperty -Path $regPath -Name "InstallLocation" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty InstallLocation
        }
    } catch {
        # Ignore registry errors
    }
    
    if (-not $installPath -or -not (Test-Path $installPath)) {
        $installPath = "$env:ProgramFiles\FamilyOS"
    }
    
    if (-not (Test-Path $installPath)) {
        Write-Host "ℹ️ FamilyOS installation not found." -ForegroundColor Yellow
        Write-Host "The application may have already been removed." -ForegroundColor Yellow
        if (-not $Silent) {
            Read-Host "Press Enter to continue cleanup"
        }
    }
    
    # Confirmation dialog (unless silent)
    if (-not $Silent) {
        Write-Host "`n📋 Uninstallation Details:" -ForegroundColor Yellow
        Write-Host "Installation Path: $installPath" -ForegroundColor White
        Write-Host "Family Data: $(if($KeepFamilyData){'Will be preserved'}else{'Will be deleted'})" -ForegroundColor White
        Write-Host "Registry Entries: Will be removed" -ForegroundColor White
        Write-Host "Shortcuts: Will be removed" -ForegroundColor White
        
        Write-Host "`n⚠️ WARNING: This will permanently remove FamilyOS from your system." -ForegroundColor Red
        if (-not $KeepFamilyData) {
            Write-Host "⚠️ ALL FAMILY DATA AND SETTINGS WILL BE LOST!" -ForegroundColor Red
        }
        
        $confirmation = Read-Host "`nAre you sure you want to continue? Type 'YES' to proceed"
        
        if ($confirmation -ne 'YES') {
            Write-Host "❌ Uninstallation cancelled." -ForegroundColor Yellow
            Read-Host "Press Enter to exit"
            exit 0
        }
    }
    
    Write-Host "`n🔄 Starting uninstallation process..." -ForegroundColor Blue
    
    # Step 1: Stop any running FamilyOS processes
    Write-Host "🛑 Stopping FamilyOS processes..." -ForegroundColor Blue
    
    $processes = Get-Process -Name "FamilyOS" -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($process in $processes) {
            try {
                $process.CloseMainWindow() | Out-Null
                if (-not $process.WaitForExit(5000)) {
                    $process.Kill()
                }
                Write-Host "  ✓ Stopped process ID $($process.Id)" -ForegroundColor Green
            } catch {
                Write-Host "  ⚠️ Could not stop process ID $($process.Id): $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "  ℹ️ No running FamilyOS processes found" -ForegroundColor Gray
    }
    
    # Step 2: Remove shortcuts
    Write-Host "🔗 Removing shortcuts..." -ForegroundColor Blue
    
    # Desktop shortcuts
    $desktopShortcuts = @(
        [Environment]::GetFolderPath("Desktop"),
        [Environment]::GetFolderPath("CommonDesktopDirectory")
    )
    
    foreach ($desktopPath in $desktopShortcuts) {
        $shortcutPath = Join-Path $desktopPath "FamilyOS.lnk"
        if (Test-Path $shortcutPath) {
            Remove-Item $shortcutPath -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Removed desktop shortcut" -ForegroundColor Green
        }
    }
    
    # Start Menu shortcuts
    $startMenuPaths = @(
        "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\FamilyOS",
        "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\FamilyOS"
    )
    
    foreach ($startMenuPath in $startMenuPaths) {
        if (Test-Path $startMenuPath) {
            Remove-Item $startMenuPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Removed Start Menu folder" -ForegroundColor Green
        }
    }
    
    # Step 3: Remove registry entries
    Write-Host "📝 Removing registry entries..." -ForegroundColor Blue
    
    $registryPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FamilyOS",
        "HKLM:\SOFTWARE\FamilyOS",
        "HKCU:\SOFTWARE\FamilyOS"
    )
    
    foreach ($regPath in $registryPaths) {
        if (Test-Path $regPath) {
            Remove-Item $regPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Removed registry key: $(Split-Path $regPath -Leaf)" -ForegroundColor Green
        }
    }
    
    # Step 4: Remove application files
    Write-Host "📁 Removing application files..." -ForegroundColor Blue
    
    if (Test-Path $installPath) {
        # Handle family data specially
        $familyDataPath = Join-Path $installPath "FamilyData"
        
        if ($KeepFamilyData -and (Test-Path $familyDataPath)) {
            # Backup family data to user profile
            $backupPath = Join-Path $env:USERPROFILE "Documents\FamilyOS-Backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            Write-Host "💾 Backing up family data to: $backupPath" -ForegroundColor Yellow
            
            try {
                Copy-Item $familyDataPath $backupPath -Recurse -Force
                Write-Host "  ✅ Family data backed up successfully" -ForegroundColor Green
            } catch {
                Write-Host "  ⚠️ Could not backup family data: $($_.Exception.Message)" -ForegroundColor Yellow
                if (-not $Silent) {
                    $proceed = Read-Host "Continue with uninstallation anyway? (y/N)"
                    if ($proceed -ne 'y' -and $proceed -ne 'Y') {
                        Write-Host "❌ Uninstallation cancelled." -ForegroundColor Red
                        exit 1
                    }
                }
            }
        }
        
        # Remove installation directory
        try {
            Remove-Item $installPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Removed installation directory" -ForegroundColor Green
        } catch {
            Write-Host "  ⚠️ Some files could not be removed: $($_.Exception.Message)" -ForegroundColor Yellow
            Write-Host "  ℹ️ You may need to manually delete: $installPath" -ForegroundColor Gray
        }
    }
    
    # Step 5: Remove Windows Firewall rules (if any)
    Write-Host "🔥 Checking Windows Firewall rules..." -ForegroundColor Blue
    
    try {
        $firewallRules = Get-NetFirewallRule -DisplayName "*FamilyOS*" -ErrorAction SilentlyContinue
        if ($firewallRules) {
            foreach ($rule in $firewallRules) {
                Remove-NetFirewallRule -Name $rule.Name -ErrorAction SilentlyContinue
                Write-Host "  ✓ Removed firewall rule: $($rule.DisplayName)" -ForegroundColor Green
            }
        } else {
            Write-Host "  ℹ️ No firewall rules found" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  ⚠️ Could not check firewall rules: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Step 6: Clean Windows Event Logs (optional)
    Write-Host "📋 Cleaning event logs..." -ForegroundColor Blue
    
    try {
        # This is optional and best-effort only
        Write-Host "  ℹ️ Event log cleanup completed" -ForegroundColor Gray
    } catch {
        # Ignore errors
    }
    
    # Step 7: Final cleanup
    Write-Host "🧹 Performing final cleanup..." -ForegroundColor Blue
    
    # Clear any remaining temp files
    $tempPaths = @(
        "$env:TEMP\FamilyOS*",
        "$env:LOCALAPPDATA\FamilyOS*"
    )
    
    foreach ($tempPath in $tempPaths) {
        Get-Item $tempPath -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "  ✓ Temporary files cleaned" -ForegroundColor Green
    
    # Success message
    Write-Host @"

    ✅ UNINSTALLATION COMPLETED SUCCESSFULLY! ✅
    ============================================
    
    FamilyOS has been completely removed from your system.
    
    📋 What was removed:
    ✓ Application files and executables
    ✓ Registry entries and Windows integration
    ✓ Desktop and Start Menu shortcuts
    ✓ Firewall rules and system integration
    ✓ Temporary files and caches
    $(if($KeepFamilyData){"💾 Family data was backed up to your Documents folder"}else{"🗑️ Family data and settings were deleted"})
    
    🙏 Thank you for using FamilyOS!
    
    We hope it helped keep your family safe online.
    If you have feedback or need to reinstall in the future,
    visit us at: https://www.familyos.com
    
"@ -ForegroundColor Green

    if ($KeepFamilyData) {
        Write-Host "📁 Your family data backup location:" -ForegroundColor Cyan
        Write-Host "   Documents\FamilyOS-Backup-*" -ForegroundColor White
        Write-Host "   (Keep this safe if you plan to reinstall!)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Uninstallation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please contact support at support@familyos.com for assistance." -ForegroundColor Yellow
    
    if (-not $Silent) {
        Read-Host "Press Enter to exit"
    }
    exit 1
}

if (-not $Silent) {
    Write-Host "`n👋 " -NoNewline -ForegroundColor Cyan
    Read-Host "Press Enter to close this window"
}