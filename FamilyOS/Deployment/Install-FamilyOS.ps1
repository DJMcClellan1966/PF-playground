#Requires -Version 5.1
<#
.SYNOPSIS
    FamilyOS Professional Installation Script

.DESCRIPTION
    This script creates a complete self-contained installation of FamilyOS
    that families can easily install on Windows systems without technical knowledge.

.PARAMETER InstallPath
    Target installation directory (default: C:\Program Files\FamilyOS)

.PARAMETER CreateDesktopShortcut
    Whether to create a desktop shortcut (default: true)

.PARAMETER CreateStartMenuShortcut  
    Whether to create Start Menu shortcuts (default: true)

.EXAMPLE
    .\Install-FamilyOS.ps1
    
.EXAMPLE
    .\Install-FamilyOS.ps1 -InstallPath "C:\FamilyOS" -CreateDesktopShortcut $false

.NOTES
    Version: 1.0
    Author: FamilyOS Team
    Last Modified: December 31, 2025
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$InstallPath = "$env:ProgramFiles\FamilyOS",
    
    [Parameter(Mandatory = $false)]
    [bool]$CreateDesktopShortcut = $true,
    
    [Parameter(Mandatory = $false)]
    [bool]$CreateStartMenuShortcut = $true,
    
    [Parameter(Mandatory = $false)]
    [bool]$RegisterUninstaller = $true
)

# Installation banner
Write-Host @"
    ███████╗ █████╗ ███╗   ███╗██╗██╗  ██╗   ██╗ ██████╗ ███████╗
    ██╔════╝██╔══██╗████╗ ████║██║██║  ╚██╗ ██╔╝██╔═══██╗██╔════╝
    █████╗  ███████║██╔████╔██║██║██║   ╚████╔╝ ██║   ██║███████╗
    ██╔══╝  ██╔══██║██║╚██╔╝██║██║██║    ╚██╔╝  ██║   ██║╚════██║
    ██║     ██║  ██║██║ ╚═╝ ██║██║███████╗██║   ╚██████╔╝███████║
    ╚═╝     ╚═╝  ╚═╝╚═╝     ╚═╝╚═╝╚══════╝╚═╝    ╚═════╝ ╚══════╝
    
    🏠 Family Digital Safety Platform - Professional Installation
    ============================================================
"@ -ForegroundColor Cyan

Write-Host "🚀 Starting FamilyOS installation..." -ForegroundColor Green
Write-Host "📁 Target directory: $InstallPath" -ForegroundColor Yellow

# Check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-Host "⚠️ This script requires administrator privileges." -ForegroundColor Red
    Write-Host "Please right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

try {
    # Step 1: Create installation directory
    Write-Host "📁 Creating installation directory..." -ForegroundColor Blue
    if (-not (Test-Path $InstallPath)) {
        New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
        Write-Host "✅ Created: $InstallPath" -ForegroundColor Green
    }
    
    # Step 2: Build self-contained application
    Write-Host "🔨 Building self-contained FamilyOS application..." -ForegroundColor Blue
    $buildPath = Split-Path $PSScriptRoot -Parent
    $publishPath = Join-Path $buildPath "bin\Release\net8.0\win-x64\publish"
    
    # Clean previous builds
    if (Test-Path $publishPath) {
        Remove-Item $publishPath -Recurse -Force
    }
    
    Push-Location $buildPath
    try {
        # Publish as self-contained
        $publishResult = dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed: $publishResult"
        }
        Write-Host "✅ Build completed successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    
    # Step 3: Copy application files
    Write-Host "📋 Copying application files..." -ForegroundColor Blue
    $sourceFiles = @(
        Join-Path $publishPath "FamilyOS.exe"
        Join-Path $publishPath "FamilyOS.dll"
        Join-Path $publishPath "FamilyOS.pdb"
    )
    
    foreach ($file in $sourceFiles) {
        if (Test-Path $file) {
            $destination = Join-Path $InstallPath (Split-Path $file -Leaf)
            Copy-Item $file $destination -Force
            Write-Host "  ✓ $(Split-Path $file -Leaf)" -ForegroundColor Green
        }
    }
    
    # Copy all other necessary files
    Get-ChildItem $publishPath -File | Where-Object { 
        $_.Extension -in @('.dll', '.json', '.exe', '.config') -and 
        $_.Name -notlike "*.pdb" -and
        $_.Name -ne "FamilyOS.exe"
    } | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $InstallPath $_.Name) -Force
    }
    
    # Step 4: Create data directory
    Write-Host "📂 Setting up data directory..." -ForegroundColor Blue
    $dataPath = Join-Path $InstallPath "FamilyData"
    if (-not (Test-Path $dataPath)) {
        New-Item -Path $dataPath -ItemType Directory -Force | Out-Null
    }
    
    # Copy documentation
    $docsPath = Join-Path $InstallPath "Documentation"
    if (-not (Test-Path $docsPath)) {
        New-Item -Path $docsPath -ItemType Directory -Force | Out-Null
    }
    
    $docFiles = @(
        "README.md",
        "Quick-Start-Guide.md",
        "Deployment\README-INSTALLATION.md",
        "Deployment\SYSTEM-REQUIREMENTS.md"
    )
    
    foreach ($doc in $docFiles) {
        $sourcePath = Join-Path $buildPath $doc
        if (Test-Path $sourcePath) {
            $docName = Split-Path $doc -Leaf
            Copy-Item $sourcePath (Join-Path $docsPath $docName) -Force
        }
    }
    
    Write-Host "✅ Application files copied successfully" -ForegroundColor Green
    
    # Step 5: Create desktop shortcut
    if ($CreateDesktopShortcut) {
        Write-Host "🖥️ Creating desktop shortcut..." -ForegroundColor Blue
        $desktopPath = [Environment]::GetFolderPath("Desktop")
        $shortcutPath = Join-Path $desktopPath "FamilyOS.lnk"
        $targetPath = Join-Path $InstallPath "FamilyOS.exe"
        
        $WShell = New-Object -ComObject WScript.Shell
        $shortcut = $WShell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $targetPath
        $shortcut.WorkingDirectory = $InstallPath
        $shortcut.Description = "FamilyOS - Family Digital Safety Platform"
        $shortcut.Save()
        
        Write-Host "✅ Desktop shortcut created" -ForegroundColor Green
    }
    
    # Step 6: Create Start Menu shortcuts
    if ($CreateStartMenuShortcut) {
        Write-Host "📋 Creating Start Menu shortcuts..." -ForegroundColor Blue
        $startMenuPath = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\FamilyOS"
        
        if (-not (Test-Path $startMenuPath)) {
            New-Item -Path $startMenuPath -ItemType Directory -Force | Out-Null
        }
        
        # Main application shortcut
        $shortcutPath = Join-Path $startMenuPath "FamilyOS.lnk"
        $targetPath = Join-Path $InstallPath "FamilyOS.exe"
        
        $WShell = New-Object -ComObject WScript.Shell
        $shortcut = $WShell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $targetPath
        $shortcut.WorkingDirectory = $InstallPath
        $shortcut.Description = "FamilyOS - Family Digital Safety Platform"
        $shortcut.Save()
        
        # Documentation shortcut
        $docsShortcutPath = Join-Path $startMenuPath "FamilyOS Documentation.lnk"
        $docsTargetPath = Join-Path $InstallPath "Documentation"
        
        $docsShortcut = $WShell.CreateShortcut($docsShortcutPath)
        $docsShortcut.TargetPath = $docsTargetPath
        $docsShortcut.Description = "FamilyOS Documentation and Quick Start Guide"
        $docsShortcut.Save()
        
        # Uninstall shortcut
        $uninstallShortcutPath = Join-Path $startMenuPath "Uninstall FamilyOS.lnk"
        $uninstallTargetPath = "powershell.exe"
        $uninstallArguments = "-ExecutionPolicy Bypass -File `"$InstallPath\Uninstall-FamilyOS.ps1`""
        
        $uninstallShortcut = $WShell.CreateShortcut($uninstallShortcutPath)
        $uninstallShortcut.TargetPath = $uninstallTargetPath
        $uninstallShortcut.Arguments = $uninstallArguments
        $uninstallShortcut.Description = "Uninstall FamilyOS"
        $uninstallShortcut.Save()
        
        Write-Host "✅ Start Menu shortcuts created" -ForegroundColor Green
    }
    
    # Step 7: Register uninstaller in Windows
    if ($RegisterUninstaller) {
        Write-Host "📝 Registering uninstaller..." -ForegroundColor Blue
        
        $uninstallRegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FamilyOS"
        
        if (-not (Test-Path $uninstallRegPath)) {
            New-Item -Path $uninstallRegPath -Force | Out-Null
        }
        
        Set-ItemProperty -Path $uninstallRegPath -Name "DisplayName" -Value "FamilyOS"
        Set-ItemProperty -Path $uninstallRegPath -Name "DisplayVersion" -Value "1.0.0"
        Set-ItemProperty -Path $uninstallRegPath -Name "Publisher" -Value "FamilyOS Team"
        Set-ItemProperty -Path $uninstallRegPath -Name "InstallLocation" -Value $InstallPath
        Set-ItemProperty -Path $uninstallRegPath -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$InstallPath\Uninstall-FamilyOS.ps1`""
        Set-ItemProperty -Path $uninstallRegPath -Name "NoModify" -Value 1 -Type DWord
        Set-ItemProperty -Path $uninstallRegPath -Name "NoRepair" -Value 1 -Type DWord
        
        Write-Host "✅ Uninstaller registered" -ForegroundColor Green
    }
    
    # Step 8: Create uninstall script
    Write-Host "🗑️ Creating uninstall script..." -ForegroundColor Blue
    $uninstallScript = @"
#Requires -Version 5.1
# FamilyOS Uninstall Script

Write-Host "🗑️ FamilyOS Uninstaller" -ForegroundColor Red
Write-Host "=========================" -ForegroundColor Red

`$confirmation = Read-Host "Are you sure you want to uninstall FamilyOS? This will remove all family data. (y/N)"

if (`$confirmation -eq 'y' -or `$confirmation -eq 'Y') {
    Write-Host "🔄 Uninstalling FamilyOS..." -ForegroundColor Yellow
    
    # Stop any running processes
    Get-Process -Name "FamilyOS" -ErrorAction SilentlyContinue | Stop-Process -Force
    
    # Remove shortcuts
    Remove-Item "`$env:PUBLIC\Desktop\FamilyOS.lnk" -ErrorAction SilentlyContinue
    Remove-Item "`$env:ProgramData\Microsoft\Windows\Start Menu\Programs\FamilyOS" -Recurse -Force -ErrorAction SilentlyContinue
    
    # Remove registry entries
    Remove-Item "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FamilyOS" -Force -ErrorAction SilentlyContinue
    
    # Remove installation directory
    Remove-Item "$InstallPath" -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Host "✅ FamilyOS has been completely removed from your system." -ForegroundColor Green
    Write-Host "Thank you for using FamilyOS! 👋" -ForegroundColor Cyan
} else {
    Write-Host "❌ Uninstallation cancelled." -ForegroundColor Yellow
}

Read-Host "Press Enter to close"
"@
    
    $uninstallScriptPath = Join-Path $InstallPath "Uninstall-FamilyOS.ps1"
    Set-Content -Path $uninstallScriptPath -Value $uninstallScript -Encoding UTF8
    
    Write-Host "✅ Uninstall script created" -ForegroundColor Green
    
    # Step 9: Set permissions
    Write-Host "🔐 Setting appropriate permissions..." -ForegroundColor Blue
    
    # Give Users read and execute permissions
    $acl = Get-Acl $InstallPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl $InstallPath $acl
    
    Write-Host "✅ Permissions configured" -ForegroundColor Green
    
    # Installation complete
    Write-Host @"

    🎉 INSTALLATION COMPLETE! 🎉
    ===========================
    
    FamilyOS has been successfully installed on your system!
    
    📍 Installation Location: $InstallPath
    🖥️ Desktop Shortcut: $(if($CreateDesktopShortcut){'✅ Created'}else{'❌ Skipped'})
    📋 Start Menu: $(if($CreateStartMenuShortcut){'✅ Created'}else{'❌ Skipped'})
    
    🚀 Next Steps:
    1. Double-click the FamilyOS desktop icon to start
    2. Login with default credentials:
       • Parents: mom/parent123 or dad/parent123
       • Children: sarah/kid123 or alex/teen123
    3. Change your passwords immediately
    4. Add your family members
    
    📚 Need Help?
    • Check the Documentation folder in your Start Menu
    • Visit: www.familyos.com/support
    
    🏠 Welcome to safer family computing! 
"@ -ForegroundColor Green

    Write-Host "`n⚡ Would you like to launch FamilyOS now? (y/N): " -ForegroundColor Cyan -NoNewline
    $launchNow = Read-Host
    
    if ($launchNow -eq 'y' -or $launchNow -eq 'Y') {
        Write-Host "🚀 Starting FamilyOS..." -ForegroundColor Green
        Start-Process -FilePath (Join-Path $InstallPath "FamilyOS.exe") -WorkingDirectory $InstallPath
    }
    
} catch {
    Write-Host "❌ Installation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please contact support at support@familyos.com" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n👋 Installation script completed!" -ForegroundColor Cyan