#Requires -RunAsAdministrator
#Requires -Version 5.1

<#
.SYNOPSIS
    Professional FamilyOS installer for Windows families.

.DESCRIPTION
    This script provides a complete one-click installation experience for FamilyOS.
    It handles downloads, extraction, Windows integration, and creates shortcuts.

.PARAMETER InstallPath
    Custom installation directory (default: C:\Program Files\FamilyOS)

.PARAMETER CreateDesktopShortcut
    Whether to create desktop shortcut (default: $true)

.PARAMETER Version
    Specific version to install (default: latest)

.EXAMPLE
    .\Install-FamilyOS-Complete.ps1
    
.EXAMPLE
    .\Install-FamilyOS-Complete.ps1 -InstallPath "C:\FamilyOS" -CreateDesktopShortcut $true
#>

param(
    [string]$InstallPath = "$env:ProgramFiles\FamilyOS",
    [bool]$CreateDesktopShortcut = $true,
    [string]$Version = "1.0.0"
)

# Enable strict error handling
$ErrorActionPreference = "Stop"

# Colors for output
$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Progress = "Magenta"
}

function Write-Step {
    param([string]$Message, [string]$Color = "Info")
    Write-Host "✓ $Message" -ForegroundColor $Colors[$Color]
}

function Write-Progress-Step {
    param([string]$Message)
    Write-Host "⏳ $Message" -ForegroundColor $Colors.Progress
}

function Write-Header {
    Clear-Host
    Write-Host @"
╔═══════════════════════════════════════════════════════════════════════╗
║                     🏠 FAMILYOS PROFESSIONAL INSTALLER                ║
║                                                                       ║
║  Family Digital Safety Platform - Enterprise Security for Families   ║
║                          Version $Version                                 ║
╚═══════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor $Colors.Info
    Write-Host ""
}

function Test-Prerequisites {
    Write-Progress-Step "Checking system prerequisites..."
    
    # Check Windows version
    $osVersion = [System.Environment]::OSVersion.Version
    if ($osVersion.Major -lt 10) {
        throw "FamilyOS requires Windows 10 or later. Current version: $($osVersion)"
    }
    
    # Check .NET (not required for self-contained but good to know)
    try {
        $dotnetVersion = & dotnet --version 2>$null
        Write-Step "✓ .NET detected: $dotnetVersion" "Success"
    } catch {
        Write-Step "ℹ .NET not installed (not required - FamilyOS is self-contained)" "Info"
    }
    
    # Check available disk space
    $drive = Split-Path $InstallPath -Qualifier
    $freeSpace = Get-WmiObject -Class Win32_LogicalDisk | Where-Object DeviceID -eq $drive | Select-Object -ExpandProperty FreeSpace
    $freeSpaceGB = [math]::Round($freeSpace / 1GB, 2)
    
    if ($freeSpaceGB -lt 1) {
        throw "Insufficient disk space. Available: ${freeSpaceGB}GB, Required: 1GB"
    }
    
    Write-Step "System requirements verified (${freeSpaceGB}GB free)" "Success"
}

function Install-FamilyOS {
    param([string]$PackagePath)
    
    Write-Progress-Step "Installing FamilyOS to $InstallPath..."
    
    # Create installation directory
    if (Test-Path $InstallPath) {
        Write-Host "⚠ Previous installation found. Backing up..." -ForegroundColor $Colors.Warning
        $backupPath = "$InstallPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Move-Item $InstallPath $backupPath
        Write-Step "Backup created: $backupPath" "Warning"
    }
    
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    
    # Extract/copy files
    if (Test-Path $PackagePath) {
        if ($PackagePath.EndsWith('.zip')) {
            Expand-Archive -Path $PackagePath -DestinationPath $InstallPath -Force
        } else {
            Copy-Item "$PackagePath\*" $InstallPath -Recurse -Force
        }
        Write-Step "FamilyOS files installed successfully" "Success"
    } else {
        throw "Package not found: $PackagePath"
    }
    
    # Set appropriate permissions
    $acl = Get-Acl $InstallPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl $InstallPath $acl
    
    Write-Step "Permissions configured for family access" "Success"
}

function Register-Windows-Integration {
    Write-Progress-Step "Configuring Windows integration..."
    
    # Register in Programs and Features
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FamilyOS"
    New-Item -Path $regPath -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name "DisplayName" -Value "FamilyOS - Family Digital Safety Platform"
    Set-ItemProperty -Path $regPath -Name "DisplayVersion" -Value $Version
    Set-ItemProperty -Path $regPath -Name "Publisher" -Value "FamilyOS Team"
    Set-ItemProperty -Path $regPath -Name "InstallLocation" -Value $InstallPath
    Set-ItemProperty -Path $regPath -Name "UninstallString" -Value "`"$InstallPath\Uninstall-FamilyOS.exe`""
    Set-ItemProperty -Path $regPath -Name "DisplayIcon" -Value "$InstallPath\FamilyOS.exe"
    Set-ItemProperty -Path $regPath -Name "NoModify" -Value 1 -Type DWord
    Set-ItemProperty -Path $regPath -Name "NoRepair" -Value 1 -Type DWord
    
    Write-Step "Registered in Windows Programs & Features" "Success"
}

function New-Shortcuts {
    Write-Progress-Step "Creating application shortcuts..."
    
    # Create Start Menu shortcut
    $startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\FamilyOS"
    New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null
    
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut("$startMenuPath\FamilyOS.lnk")
    $shortcut.TargetPath = "$InstallPath\FamilyOS.exe"
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Description = "FamilyOS - Family Digital Safety Platform"
    $shortcut.Save()
    
    Write-Step "Start Menu shortcut created" "Success"
    
    # Create Desktop shortcut if requested
    if ($CreateDesktopShortcut) {
        $desktopShortcut = $shell.CreateShortcut("$env:Public\Desktop\FamilyOS.lnk")
        $desktopShortcut.TargetPath = "$InstallPath\FamilyOS.exe"
        $desktopShortcut.WorkingDirectory = $InstallPath
        $desktopShortcut.Description = "FamilyOS - Family Digital Safety Platform"
        $desktopShortcut.Save()
        
        Write-Step "Desktop shortcut created" "Success"
    }
}

function New-Uninstaller {
    Write-Progress-Step "Creating uninstaller..."
    
    $uninstallScript = @"
# FamilyOS Uninstaller
param([switch]`$Silent)

if (-not `$Silent) {
    `$result = [System.Windows.MessageBox]::Show("Are you sure you want to uninstall FamilyOS?", "Uninstall FamilyOS", "YesNo", "Question")
    if (`$result -eq "No") { exit 0 }
}

Write-Host "Uninstalling FamilyOS..." -ForegroundColor Yellow

# Remove installation directory
if (Test-Path "$InstallPath") {
    Remove-Item "$InstallPath" -Recurse -Force
    Write-Host "✓ Application files removed" -ForegroundColor Green
}

# Remove registry entries
Remove-Item "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FamilyOS" -Force -ErrorAction SilentlyContinue

# Remove shortcuts
Remove-Item "`$env:ProgramData\Microsoft\Windows\Start Menu\Programs\FamilyOS" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "`$env:Public\Desktop\FamilyOS.lnk" -Force -ErrorAction SilentlyContinue

Write-Host "✓ FamilyOS has been completely removed from your system" -ForegroundColor Green
Write-Host "Press any key to continue..." -ForegroundColor Cyan
Read-Host
"@

    $uninstallScript | Out-File -FilePath "$InstallPath\Uninstall-FamilyOS.ps1" -Encoding UTF8
    
    Write-Step "Uninstaller created" "Success"
}

function Show-Completion {
    Write-Host @"

╔═══════════════════════════════════════════════════════════════════════╗
║                    🎉 INSTALLATION COMPLETED SUCCESSFULLY! 🎉          ║
╚═══════════════════════════════════════════════════════════════════════╝

📍 Installation Location: $InstallPath
🚀 Version Installed: $Version
📋 Integration: Windows Start Menu, Programs & Features
🔗 Shortcuts: Created for easy access

🏠 DEFAULT FAMILY ACCOUNTS:
   👨‍👩‍👧‍👦 Parents: mom/parent123, dad/parent123
   👶 Children: sarah/kid123, alex/teen123

🔐 FIRST STEPS:
   1. Launch FamilyOS from Start Menu or Desktop
   2. Log in with parent credentials (mom/parent123)
   3. Customize family profiles and settings
   4. Set up parental controls for your children
   5. Review security and content filtering options

⚠️  IMPORTANT SECURITY NOTES:
   • Change default passwords immediately
   • Configure age-appropriate content filters
   • Set up screen time limits
   • Review all family member permissions

📚 SUPPORT & DOCUMENTATION:
   • User Guide: $InstallPath\Documentation\
   • Troubleshooting: Check Windows Event Viewer for logs
   • Uninstall: Use Programs & Features or run Uninstall-FamilyOS.ps1

Thank you for choosing FamilyOS for your family's digital safety! 🛡️

"@ -ForegroundColor $Colors.Success

    Write-Host "Press Enter to launch FamilyOS now, or Ctrl+C to exit..." -ForegroundColor $Colors.Info
    Read-Host
    
    # Launch FamilyOS
    try {
        Start-Process "$InstallPath\FamilyOS.exe" -WorkingDirectory $InstallPath
        Write-Step "FamilyOS launched successfully!" "Success"
    } catch {
        Write-Host "⚠ Could not auto-launch FamilyOS. Please start it manually from the Start Menu." -ForegroundColor $Colors.Warning
    }
}

# Main Installation Process
try {
    Write-Header
    
    # Check if already running as administrator
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "This installer must be run as Administrator. Please right-click and select 'Run as Administrator'."
    }
    
    # Pre-installation checks
    Test-Prerequisites
    
    # Determine package source
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $packagePath = Join-Path $scriptDir "Build\Package"
    
    if (-not (Test-Path $packagePath)) {
        $zipPath = Join-Path $scriptDir "Build\Release\FamilyOS-v$Version.zip"
        if (Test-Path $zipPath) {
            $packagePath = $zipPath
        } else {
            throw "FamilyOS package not found. Please run Simple-Package.ps1 first to build the installation package."
        }
    }
    
    # Install FamilyOS
    Install-FamilyOS -PackagePath $packagePath
    
    # Configure Windows integration
    Register-Windows-Integration
    
    # Create shortcuts
    New-Shortcuts
    
    # Create uninstaller
    New-Uninstaller
    
    # Show completion message
    Show-Completion
    
} catch {
    Write-Host "`n❌ INSTALLATION FAILED!" -ForegroundColor $Colors.Error
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor $Colors.Error
    Write-Host "`nTroubleshooting tips:" -ForegroundColor $Colors.Warning
    Write-Host "• Ensure you're running as Administrator" -ForegroundColor $Colors.Warning
    Write-Host "• Check available disk space" -ForegroundColor $Colors.Warning
    Write-Host "• Verify Windows 10+ version" -ForegroundColor $Colors.Warning
    Write-Host "• Run Simple-Package.ps1 first to build the package" -ForegroundColor $Colors.Warning
    Write-Host "`nPress any key to exit..." -ForegroundColor $Colors.Info
    Read-Host
    exit 1
}