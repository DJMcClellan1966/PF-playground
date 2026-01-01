#Requires -Version 5.1

<#
.SYNOPSIS
    Creates FamilyOS installation packages for multiple architectures.

.DESCRIPTION
    This script builds self-contained FamilyOS applications for x64, x86, and ARM64 architectures,
    creates Windows shortcuts and registry entries, and optionally creates an Inno Setup installer.

.PARAMETER Version
    Version number for the build (default: 1.0.0)

.PARAMETER Architectures
    Target architectures to build for (default: x64, x86, ARM64)

.PARAMETER BuildPath
    Custom build output path (default: ..\Build)

.PARAMETER CreateInnoSetup
    Whether to create Inno Setup installer (default: $true)

.EXAMPLE
    .\Create-Installer-Fixed.ps1 -Version "1.2.0"
    
.EXAMPLE
    .\Create-Installer-Fixed.ps1 -Architectures @("win-x64", "win-x86") -Version "1.1.0"
#>

param(
    [string]$Version = "1.0.0",
    [string[]]$Architectures = @("win-x64", "win-x86", "win-arm64"),
    [string]$BuildPath = "..\Build",
    [bool]$CreateInnoSetup = $true
)

# Enable strict error handling
$ErrorActionPreference = "Stop"

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$sourceDir = Join-Path $projectDir "Source"

# Output directories
$buildPath = Join-Path $scriptDir $BuildPath
$releasePath = Join-Path $buildPath "Release"
$packagePath = Join-Path $buildPath "Package"
$docsPath = Join-Path $packagePath "Documentation"
$appPath = Join-Path $packagePath "Application"

Write-Host "=== FamilyOS Installation Package Creator ===" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Green
Write-Host "Target Architectures: $($Architectures -join ', ')" -ForegroundColor Green
Write-Host "Build Path: $buildPath" -ForegroundColor Green

try {
    # Clean and create directories
    Write-Host "`nPreparing build environment..." -ForegroundColor Yellow
    if (Test-Path $buildPath) {
        Remove-Item $buildPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path @($buildPath, $releasePath, $packagePath, $docsPath, $appPath) -Force | Out-Null

    # Verify source files exist
    $csprojPath = Join-Path $sourceDir "FamilyOS.csproj"
    if (-not (Test-Path $csprojPath)) {
        throw "Project file not found: $csprojPath"
    }

    # Build for each architecture
    Write-Host "`nBuilding FamilyOS for multiple architectures..." -ForegroundColor Yellow
    
    foreach ($arch in $Architectures) {
        Write-Host "  Building for $arch..." -ForegroundColor Cyan
        
        $outputPath = Join-Path $releasePath $arch
        $publishArgs = @(
            "publish",
            $csprojPath,
            "-c", "Release",
            "-r", $arch,
            "--self-contained", "true",
            "-p:PublishSingleFile=true",
            "-p:IncludeNativeLibrariesForSelfExtract=true",
            "-p:PublishTrimmed=true",
            "-p:TrimMode=partial",
            "-p:AssemblyVersion=$Version",
            "-p:FileVersion=$Version",
            "-p:Version=$Version",
            "-o", $outputPath,
            "--verbosity", "minimal"
        )
        
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $arch"
        }
        
        Write-Host "    ✓ Build completed for $arch" -ForegroundColor Green
        
        # Copy architecture-specific build to package
        $archPackagePath = Join-Path $appPath $arch
        New-Item -ItemType Directory -Path $archPackagePath -Force | Out-Null
        Copy-Item "$outputPath\*" $archPackagePath -Recurse -Force
    }

    # Copy documentation
    Write-Host "`nPackaging documentation..." -ForegroundColor Yellow
    
    $docsSourcePath = Join-Path $scriptDir "*.md"
    if (Test-Path $docsSourcePath) {
        Copy-Item $docsSourcePath $docsPath -Force
    }
    
    # Create main executable launcher
    Write-Host "`nCreating application launcher..." -ForegroundColor Yellow
    
    # Create batch file launcher content
    $batchLines = @(
        '@echo off',
        'setlocal enabledelayedexpansion',
        '',
        'REM FamilyOS Application Launcher',
        'REM Detects system architecture and launches appropriate version',
        '',
        'set "SCRIPT_DIR=%~dp0"',
        'set "ARCH_DIR="',
        '',
        'REM Detect architecture',
        'if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (',
        '    set "ARCH_DIR=win-x64"',
        ') else if "%PROCESSOR_ARCHITECTURE%"=="ARM64" (',
        '    set "ARCH_DIR=win-arm64"',
        ') else if "%PROCESSOR_ARCHITECTURE%"=="x86" (',
        '    set "ARCH_DIR=win-x86"',
        ') else (',
        '    set "ARCH_DIR=win-x64"',
        ')',
        '',
        'set "EXE_PATH=%SCRIPT_DIR%Application\%ARCH_DIR%\FamilyOS.exe"',
        '',
        'if exist "%EXE_PATH%" (',
        '    echo Starting FamilyOS...',
        '    start "" "%EXE_PATH%" %*',
        ') else (',
        '    echo Error: FamilyOS executable not found for your architecture.',
        '    echo Looking for: %EXE_PATH%',
        '    pause',
        ')'
    )

    $launcherPath = Join-Path $packagePath "FamilyOS.bat"
    $batchLines -join "`r`n" | Out-File -FilePath $launcherPath -Encoding ASCII

    # Create PowerShell launcher for better integration
    $psLauncherContent = @'
# FamilyOS PowerShell Launcher
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$arch = "win-x64"

# Detect architecture
switch ([System.Environment]::OSVersion.Platform) {
    "Win32NT" {
        switch ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture) {
            "X64" { $arch = "win-x64" }
            "X86" { $arch = "win-x86" }
            "Arm64" { $arch = "win-arm64" }
            default { $arch = "win-x64" }
        }
    }
}

$exePath = Join-Path $scriptDir "Application\$arch\FamilyOS.exe"

if (Test-Path $exePath) {
    Write-Host "Starting FamilyOS..." -ForegroundColor Green
    Start-Process $exePath -ArgumentList $args
} else {
    Write-Error "FamilyOS executable not found: $exePath"
    Read-Host "Press Enter to exit"
}
'@

    $psLauncherPath = Join-Path $packagePath "FamilyOS.ps1"
    $psLauncherContent | Out-File -FilePath $psLauncherPath -Encoding UTF8

    # Create installer manifest
    Write-Host "`nCreating installation manifest..." -ForegroundColor Yellow
    
    $manifest = @{
        Name = "FamilyOS"
        Version = $Version
        Description = "Family Digital Safety Platform"
        Architectures = $Architectures
        InstallDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        RequiredFramework = ".NET 8.0 (included - self-contained)"
        MinimumOS = "Windows 10 Version 1809 or later"
    }
    
    $manifestPath = Join-Path $packagePath "install-manifest.json"
    $manifest | ConvertTo-Json -Depth 3 | Out-File -FilePath $manifestPath -Encoding UTF8

    # Create Inno Setup installer script if requested
    if ($CreateInnoSetup) {
        Write-Host "`nGenerating Inno Setup installer script..." -ForegroundColor Yellow
        
        # Create the Inno Setup script content
        $innoScriptLines = @(
            "; FamilyOS Installer Script for Inno Setup",
            "; Generated by Create-Installer.ps1",
            "",
            "[Setup]",
            "AppName=FamilyOS",
            "AppVersion=$Version",
            "AppVerName=FamilyOS $Version",
            "AppPublisher=FamilyOS Team",
            "AppPublisherURL=https://www.familyos.com",
            "AppSupportURL=https://www.familyos.com/support",
            "AppUpdatesURL=https://www.familyos.com/updates",
            "DefaultDirName={autopf}\FamilyOS",
            "DefaultGroupName=FamilyOS",
            "AllowNoIcons=yes",
            "LicenseFile=",
            "OutputBaseFilename=FamilyOS-Setup-v$Version",
            "Compression=lzma",
            "SolidCompression=yes",
            "WizardStyle=modern",
            "PrivilegesRequired=admin",
            "UninstallDisplayIcon={app}\FamilyOS.exe",
            "UninstallDisplayName=FamilyOS",
            "VersionInfoVersion=$Version",
            "VersionInfoDescription=FamilyOS - Family Digital Safety Platform",
            "ArchitecturesInstallIn64BitMode=x64",
            "ArchitecturesAllowed=x64",
            "",
            "[Languages]",
            "Name: `"english`"; MessagesFile: `"compiler:Default.isl`"",
            "",
            "[Tasks]", 
            "Name: `"desktopicon`"; Description: `"{cm:CreateDesktopIcon}`"; GroupDescription: `"{cm:AdditionalIcons}`"; Flags: unchecked",
            "",
            "[Files]",
            "Source: `"$packagePath\Application\*`"; DestDir: `"{app}`"; Flags: ignoreversion recursesubdirs createallsubdirs",
            "Source: `"$packagePath\Documentation\*`"; DestDir: `"{app}\Documentation`"; Flags: ignoreversion recursesubdirs createallsubdirs",
            "",
            "[Icons]",
            "Name: `"{group}\FamilyOS`"; Filename: `"{app}\FamilyOS.exe`"",
            "Name: `"{group}\FamilyOS Documentation`"; Filename: `"{app}\Documentation`"",
            "Name: `"{group}\{cm:UninstallProgram,FamilyOS}`"; Filename: `"{uninstallexe}`"",
            "Name: `"{autodesktop}\FamilyOS`"; Filename: `"{app}\FamilyOS.exe`"; Tasks: desktopicon",
            "",
            "[Run]",
            "Filename: `"{app}\FamilyOS.exe`"; Description: `"{cm:LaunchProgram,FamilyOS}`"; Flags: nowait postinstall skipifsilent",
            "",
            "[UninstallDelete]",
            "Type: filesandordirs; Name: `"{app}\FamilyData`"",
            "Type: filesandordirs; Name: `"{app}\Logs`""
        )
        
        $innoScriptPath = Join-Path $buildPath "FamilyOS-Setup.iss"
        $innoScriptLines -join "`r`n" | Out-File -FilePath $innoScriptPath -Encoding UTF8
        
        Write-Host "    ✓ Inno Setup script created: $innoScriptPath" -ForegroundColor Green
        Write-Host "    ⚠ Run Inno Setup Compiler on this .iss file to create Windows installer" -ForegroundColor Yellow
    }

    # Create ZIP package for manual installation
    Write-Host "`nCreating ZIP package..." -ForegroundColor Yellow
    
    $zipPath = Join-Path $releasePath "FamilyOS-v$Version-Portable.zip"
    
    # Use .NET compression if available, fallback to PowerShell 5.0+
    if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
        Compress-Archive -Path "$packagePath\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force
        Write-Host "    ✓ Portable ZIP package created: $zipPath" -ForegroundColor Green
    } else {
        Write-Warning "Compress-Archive not available. ZIP package creation skipped."
    }

    # Generate installation summary
    Write-Host "`n=== Installation Package Creation Complete ===" -ForegroundColor Cyan
    Write-Host "Package Details:" -ForegroundColor White
    Write-Host "  Version: $Version" -ForegroundColor Gray
    Write-Host "  Architectures: $($Architectures -join ', ')" -ForegroundColor Gray
    Write-Host "  Package Size: $([math]::Round((Get-ChildItem $packagePath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)) MB" -ForegroundColor Gray
    
    Write-Host "`nGenerated Files:" -ForegroundColor White
    Get-ChildItem $releasePath -Recurse -File | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 2)
        Write-Host "  $($_.Name) ($size MB)" -ForegroundColor Gray
    }
    
    Write-Host "`nInstallation Options:" -ForegroundColor White
    Write-Host "  1. Use Install-FamilyOS.ps1 for automated PowerShell installation" -ForegroundColor Gray
    Write-Host "  2. Extract ZIP package for manual installation" -ForegroundColor Gray
    if ($CreateInnoSetup) {
        Write-Host "  3. Compile .iss file with Inno Setup for Windows installer" -ForegroundColor Gray
    }
    
    Write-Host "`n✓ Find your installation packages in: $releasePath" -ForegroundColor Green

} catch {
    Write-Error "Package creation failed: $($_.Exception.Message)"
    Write-Host "Error Details:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}

Write-Host "`nPackage creation completed successfully!" -ForegroundColor Green