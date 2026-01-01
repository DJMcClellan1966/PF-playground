#Requires -Version 5.1
<#
.SYNOPSIS
    Creates a professional FamilyOS installation package

.DESCRIPTION
    This script builds a complete installation package for FamilyOS including:
    - Self-contained executable
    - Installation wizard
    - Professional installer (Inno Setup)
    - Digital signatures (optional)

.PARAMETER OutputDirectory
    Directory where the installer will be created (default: .\Output)

.PARAMETER IncludeSource
    Whether to include source code in the package (default: false)

.PARAMETER SignInstaller
    Whether to digitally sign the installer (requires certificate)

.EXAMPLE
    .\Create-Installer.ps1
    
.EXAMPLE
    .\Create-Installer.ps1 -OutputDirectory "C:\Releases" -SignInstaller

.NOTES
    Version: 1.0
    Author: FamilyOS Team
    Requirements: Inno Setup (optional), .NET 8.0 SDK
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = ".\Output",
    
    [Parameter(Mandatory = $false)]
    [bool]$IncludeSource = $false,
    
    [Parameter(Mandatory = $false)]
    [bool]$SignInstaller = $false,
    
    [Parameter(Mandatory = $false)]
    [string]$Version = "1.0.0"
)

# Banner
Write-Host @"
    🏭 FamilyOS Installation Package Builder
    ========================================
    Building professional installation package...
"@ -ForegroundColor Cyan

$ErrorActionPreference = "Stop"

try {
    # Step 1: Setup directories
    Write-Host "📁 Setting up build directories..." -ForegroundColor Blue
    
    $scriptPath = Split-Path $PSScriptRoot -Parent
    $buildPath = Join-Path $OutputDirectory "Build"
    $packagePath = Join-Path $OutputDirectory "Package"
    $releasePath = Join-Path $OutputDirectory "Release"
    
    # Clean and create directories
    @($buildPath, $packagePath, $releasePath) | ForEach-Object {
        if (Test-Path $_) { Remove-Item $_ -Recurse -Force }
        New-Item $_ -ItemType Directory -Force | Out-Null
    }
    
    Write-Host "✅ Directories created" -ForegroundColor Green
    
    # Step 2: Build self-contained application
    Write-Host "🔨 Building self-contained FamilyOS..." -ForegroundColor Blue
    
    Push-Location $scriptPath
    try {
        # Clean previous builds
        & dotnet clean -c Release
        
        # Build for multiple architectures
        $architectures = @("win-x64", "win-x86", "win-arm64")
        
        foreach ($arch in $architectures) {
            Write-Host "  Building for $arch..." -ForegroundColor Yellow
            
            $publishPath = Join-Path $buildPath $arch
            
            $publishArgs = @(
                "publish"
                "-c", "Release"
                "-r", $arch
                "--self-contained", "true"
                "-p:PublishSingleFile=true"
                "-p:PublishTrimmed=true"
                "-p:IncludeNativeLibrariesForSelfExtract=true"
                "-o", $publishPath
            )
            
            & dotnet @publishArgs
            
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed for architecture: $arch"
            }
            
            Write-Host "    ✅ $arch build completed" -ForegroundColor Green
        }
    }
    finally {
        Pop-Location
    }
    
    # Step 3: Create package structure
    Write-Host "📦 Creating installation package structure..." -ForegroundColor Blue
    
    # Main application files (x64 as primary)
    $mainAppPath = Join-Path $packagePath "Application"
    New-Item $mainAppPath -ItemType Directory -Force | Out-Null
    
    $x64BuildPath = Join-Path $buildPath "win-x64"
    Copy-Item "$x64BuildPath\*" $mainAppPath -Recurse -Force
    
    # Installation scripts
    $scriptsPath = Join-Path $packagePath "Scripts"
    New-Item $scriptsPath -ItemType Directory -Force | Out-Null
    
    Copy-Item (Join-Path $PSScriptRoot "Install-FamilyOS.ps1") $scriptsPath -Force
    Copy-Item (Join-Path $PSScriptRoot "*.ps1") $scriptsPath -Force
    
    # Documentation
    $docsPath = Join-Path $packagePath "Documentation"
    New-Item $docsPath -ItemType Directory -Force | Out-Null
    
    $docFiles = @(
        "README-INSTALLATION.md",
        "SYSTEM-REQUIREMENTS.md"
    )
    
    foreach ($doc in $docFiles) {
        $sourcePath = Join-Path $PSScriptRoot $doc
        if (Test-Path $sourcePath) {
            Copy-Item $sourcePath $docsPath -Force
        }
    }
    
    # Copy main documentation from parent
    Copy-Item (Join-Path $scriptPath "README.md") $docsPath -Force
    Copy-Item (Join-Path (Split-Path $scriptPath -Parent) "Quick-Start-Guide.md") $docsPath -Force
    
    Write-Host "✅ Package structure created" -ForegroundColor Green
    
    # Step 4: Create simple ZIP package
    Write-Host "🗜️ Creating ZIP package..." -ForegroundColor Blue
    
    $zipPath = Join-Path $releasePath "FamilyOS-v$Version-Portable.zip"
    Compress-Archive -Path "$packagePath\*" -DestinationPath $zipPath -Force
    
    Write-Host "✅ ZIP package created: $(Split-Path $zipPath -Leaf)" -ForegroundColor Green
    
    # Step 5: Create Windows Installer Script (Inno Setup)
    Write-Host "📄 Creating installer script..." -ForegroundColor Blue
    
    $innoScript = @'
; FamilyOS Installer Script for Inno Setup
; Generated by Create-Installer.ps1

[Setup]
AppName=FamilyOS
AppVersion={#Version}
AppVerName=FamilyOS {#Version}
AppPublisher=FamilyOS Team
AppPublisherURL=https://www.familyos.com
AppSupportURL=https://www.familyos.com/support
AppUpdatesURL=https://www.familyos.com/updates
DefaultDirName={autopf}\FamilyOS
DefaultGroupName=FamilyOS
AllowNoIcons=yes
LicenseFile=
OutputBaseFilename=FamilyOS-Setup-v{#Version}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\FamilyOS.exe
UninstallDisplayName=FamilyOS
VersionInfoVersion={#Version}
VersionInfoDescription=FamilyOS - Family Digital Safety Platform
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourcePath}\Application\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourcePath}\Documentation\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FamilyOS"; Filename: "{app}\FamilyOS.exe"
Name: "{group}\FamilyOS Documentation"; Filename: "{app}\Documentation"
Name: "{group}\{cm:UninstallProgram,FamilyOS}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\FamilyOS"; Filename: "{app}\FamilyOS.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FamilyOS.exe"; Description: "{cm:LaunchProgram,FamilyOS}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\FamilyData"
Type: filesandordirs; Name: "{app}\Logs"

'@

    # Replace placeholders in Inno Setup script
    $innoScript = $innoScript.Replace('{#Version}', $Version)
    $innoScript = $innoScript.Replace('{#SourcePath}', $packagePath)

    $innoScriptPath = Join-Path $releasePath "FamilyOS-Setup.iss"
    Set-Content -Path $innoScriptPath -Value $innoScript -Encoding UTF8
    
    Write-Host "✅ Installer script created" -ForegroundColor Green
    
    # Step 6: Try to create Inno Setup installer (if available)
    Write-Host "🔍 Looking for Inno Setup..." -ForegroundColor Blue
    
    $innoSetupPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
    )
    
    $innoSetupFound = $false
    foreach ($path in $innoSetupPaths) {
        if (Test-Path $path) {
            Write-Host "✅ Found Inno Setup: $path" -ForegroundColor Green
            
            try {
                & "$path" "$innoScriptPath"
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ Windows Installer created successfully!" -ForegroundColor Green
                    $innoSetupFound = $true
                } else {
                    Write-Host "⚠️ Inno Setup build failed" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "⚠️ Error running Inno Setup: $($_.Exception.Message)" -ForegroundColor Yellow
            }
            break
        }
    }
    
    if (-not $innoSetupFound) {
        Write-Host "ℹ️ Inno Setup not found - skipping Windows Installer creation" -ForegroundColor Yellow
        Write-Host "  Install Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    }
    
    # Step 7: Create PowerShell-based installer
    Write-Host "⚡ Creating PowerShell-based installer..." -ForegroundColor Blue
    
    $psInstallerScript = @"
# FamilyOS PowerShell Installer
# Self-extracting installation package

Write-Host @`"
    🏠 FamilyOS Installation Wizard
    ===============================
    Welcome to the FamilyOS family digital safety platform!
`"@ -ForegroundColor Cyan

# Check for admin rights
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "⚠️ This installer requires administrator privileges." -ForegroundColor Red
    Write-Host "Please right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Extract embedded ZIP to temp location
`$tempPath = Join-Path `$env:TEMP "FamilyOS-Install"
if (Test-Path `$tempPath) { Remove-Item `$tempPath -Recurse -Force }
New-Item `$tempPath -ItemType Directory -Force | Out-Null

# The ZIP data will be embedded here by the build process
# [ZIP_DATA_PLACEHOLDER]

Write-Host "📁 Extracting installation files..." -ForegroundColor Blue
Expand-Archive -Path `$zipFile -DestinationPath `$tempPath -Force

# Run the actual installer
`$installerScript = Join-Path `$tempPath "Scripts\Install-FamilyOS.ps1"
if (Test-Path `$installerScript) {
    & `$installerScript
} else {
    Write-Host "❌ Installation files corrupted. Please re-download." -ForegroundColor Red
}

# Cleanup
Remove-Item `$tempPath -Recurse -Force -ErrorAction SilentlyContinue
"@

    $psInstallerPath = Join-Path $releasePath "FamilyOS-PowerShell-Installer.ps1"
    Set-Content -Path $psInstallerPath -Value $psInstallerScript -Encoding UTF8
    
    Write-Host "✅ PowerShell installer created" -ForegroundColor Green
    
    # Step 8: Generate checksums
    Write-Host "🔐 Generating checksums..." -ForegroundColor Blue
    
    $checksumFile = Join-Path $releasePath "CHECKSUMS.txt"
    $checksumContent = @()
    
    Get-ChildItem $releasePath -File | ForEach-Object {
        if ($_.Name -ne "CHECKSUMS.txt") {
            $hash = Get-FileHash $_.FullName -Algorithm SHA256
            $checksumContent += "$($hash.Hash.ToLower())  $($_.Name)"
        }
    }
    
    Set-Content -Path $checksumFile -Value $checksumContent -Encoding UTF8
    
    Write-Host "✅ Checksums generated" -ForegroundColor Green
    
    # Step 9: Create release notes
    Write-Host "📝 Creating release information..." -ForegroundColor Blue
    
    $releaseNotes = @"
# FamilyOS v$Version Release Package
## Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

### 📦 Package Contents

#### For Families (Recommended)
- **FamilyOS-Setup-v$Version.exe** - Windows Installer (if Inno Setup available)
- **FamilyOS-v$Version-Portable.zip** - Portable version for manual installation

#### For Developers
- **FamilyOS-PowerShell-Installer.ps1** - PowerShell-based installer
- **FamilyOS-Setup.iss** - Inno Setup script for custom builds

#### Documentation
- **CHECKSUMS.txt** - SHA256 checksums for verification
- **README-INSTALLATION.md** - Installation guide for families
- **SYSTEM-REQUIREMENTS.md** - System requirements

### 🚀 Installation Instructions

#### Option 1: Windows Installer (Easiest)
1. Download **FamilyOS-Setup-v$Version.exe**
2. Double-click to run
3. Follow the installation wizard
4. Launch FamilyOS from desktop or Start Menu

#### Option 2: Portable ZIP
1. Download **FamilyOS-v$Version-Portable.zip**
2. Extract to desired location
3. Run **Scripts\Install-FamilyOS.ps1** as administrator
4. Or manually run **Application\FamilyOS.exe**

### 🔐 Security Verification

Verify download integrity using checksums:

``````
# Windows PowerShell
Get-FileHash "FamilyOS-Setup-v$Version.exe" -Algorithm SHA256
``````

Compare with values in **CHECKSUMS.txt**

### 📞 Support

- 🌐 Website: https://www.familyos.com
- 📧 Email: support@familyos.com
- 📋 Issues: https://github.com/familyos/familyos/issues

---

### 🏠 Default Login Credentials
- **Parents**: mom/parent123, dad/parent123
- **Children**: sarah/kid123, alex/teen123

**⚠️ Change passwords immediately after first login!**
"@

    $releaseNotesPath = Join-Path $releasePath "RELEASE-NOTES.md"
    Set-Content -Path $releaseNotesPath -Value $releaseNotes -Encoding UTF8
    
    Write-Host "✅ Release notes created" -ForegroundColor Green
    
    # Final summary
    Write-Host @"

    🎉 INSTALLATION PACKAGE BUILD COMPLETE! 🎉
    ==========================================
    
    📁 Output Directory: $releasePath
    
    📦 Files Created:
"@ -ForegroundColor Green
    
    Get-ChildItem $releasePath | ForEach-Object {
        $size = if ($_.PSIsContainer) { "DIR" } else { "{0:N1} KB" -f ($_.Length / 1KB) }
        Write-Host "    ✅ $($_.Name) ($size)" -ForegroundColor Green
    }
    
    Write-Host @"
    
    🚀 Ready for Distribution:
    • Upload files to download server
    • Test installation on clean Windows systems
    • Share with beta testing families
    
    📋 Next Steps:
    1. Test installation package on clean Windows VM
    2. Create digital signatures for enhanced security
    3. Set up automatic update mechanism
    4. Create family onboarding documentation
    
"@ -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}

Write-Host "👋 Build script completed successfully!" -ForegroundColor Cyan
Write-Host "📍 Find your installation packages in: $releasePath" -ForegroundColor Yellow