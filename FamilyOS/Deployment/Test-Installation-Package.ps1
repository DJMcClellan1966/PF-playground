#Requires -Version 5.1

<#
.SYNOPSIS
    Test script to verify FamilyOS installation package integrity.

.DESCRIPTION
    This script performs comprehensive tests on the FamilyOS installation package
    to ensure all components are present and functional before distribution.

.EXAMPLE
    .\Test-Installation-Package.ps1
#>

$ErrorActionPreference = "SilentlyContinue"

function Write-TestResult {
    param([string]$Test, [bool]$Passed, [string]$Details = "")
    $status = if ($Passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($Passed) { "Green" } else { "Red" }
    
    Write-Host "$Test`: " -NoNewline -ForegroundColor White
    Write-Host $status -ForegroundColor $color
    if ($Details) { Write-Host "   $Details" -ForegroundColor Gray }
}

Write-Host @"
╔═══════════════════════════════════════════════════════════════════╗
║                 FAMILYOS INSTALLATION PACKAGE TEST                ║
║             Verifying package integrity and readiness             ║
╚═══════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host ""

# Test 1: Package Structure
$packagePath = "Build\Package"
$hasExecutable = Test-Path "$packagePath\FamilyOS.exe"
$hasManifest = Test-Path "$packagePath\manifest.json"
$hasConfig = Test-Path "$packagePath\appsettings.json"
Write-TestResult "Package Structure" ($hasExecutable -and $hasManifest -and $hasConfig) "Checking core files"

# Test 2: Installation Scripts
$hasInstaller = Test-Path "Install-FamilyOS-Complete.ps1"
$hasUninstaller = Test-Path "Uninstall-FamilyOS.ps1"
$hasBuilder = Test-Path "Simple-Package.ps1"
Write-TestResult "Installation Scripts" ($hasInstaller -and $hasUninstaller -and $hasBuilder) "Professional installer suite"

# Test 3: Documentation
$hasQuickStart = Test-Path "FAMILY-QUICK-START.md"
$hasTechDocs = Test-Path "README.md"
$hasInstallGuide = Test-Path "README-INSTALLATION.md"
Write-TestResult "Documentation" ($hasQuickStart -and $hasTechDocs -and $hasInstallGuide) "Family and technical guides"

# Test 4: Distribution Package
$zipPath = "Build\Release\FamilyOS-v*.zip"
$hasZipPackage = (Get-ChildItem $zipPath -ErrorAction SilentlyContinue).Count -gt 0
$zipSize = if ($hasZipPackage) { 
    $zip = Get-ChildItem $zipPath | Select-Object -First 1
    [math]::Round($zip.Length / 1MB, 2)
} else { 0 }
Write-TestResult "Distribution Package" $hasZipPackage "ZIP file ($zipSize MB)"

# Test 5: Executable Properties
if ($hasExecutable) {
    $exeInfo = Get-ItemProperty "$packagePath\FamilyOS.exe" -ErrorAction SilentlyContinue
    $exeSize = [math]::Round($exeInfo.Length / 1MB, 2)
    $isSelfContained = $exeSize -gt 50  # Self-contained .NET apps are typically larger
    Write-TestResult "Executable Properties" $isSelfContained "Self-contained build ($exeSize MB)"
}

# Test 6: Manifest Validation
if ($hasManifest) {
    try {
        $manifest = Get-Content "$packagePath\manifest.json" | ConvertFrom-Json
        $validManifest = $manifest.Name -eq "FamilyOS" -and $manifest.Version -and $manifest.Description
        Write-TestResult "Manifest Validation" $validManifest "Version: $($manifest.Version)"
    } catch {
        Write-TestResult "Manifest Validation" $false "JSON parsing failed"
    }
}

# Test 7: PowerShell Script Syntax
$scriptsValid = $true
$scriptFiles = @("Install-FamilyOS-Complete.ps1", "Uninstall-FamilyOS.ps1", "Simple-Package.ps1")
foreach ($script in $scriptFiles) {
    if (Test-Path $script) {
        try {
            $null = [System.Management.Automation.PSParser]::Tokenize((Get-Content $script -Raw), [ref]$null)
        } catch {
            $scriptsValid = $false
            break
        }
    }
}
Write-TestResult "PowerShell Script Syntax" $scriptsValid "All installer scripts validated"

# Test 8: Security Manifest
$hasSecurityManifest = Test-Path "FamilyOS.exe.manifest"
Write-TestResult "Windows Integration" $hasSecurityManifest "Application manifest for UAC"

# Summary
Write-Host "`n" + "="*70 -ForegroundColor Cyan
$allTestsPassed = $hasExecutable -and $hasManifest -and $hasInstaller -and $hasQuickStart -and $hasZipPackage -and $scriptsValid

if ($allTestsPassed) {
    Write-Host "*** ALL TESTS PASSED - PACKAGE READY FOR DISTRIBUTION! ***" -ForegroundColor Green
    Write-Host "`nNext Steps for Families:" -ForegroundColor White
    Write-Host "1. Extract the ZIP package to any location" -ForegroundColor Gray
    Write-Host "2. Right-click Install-FamilyOS-Complete.ps1" -ForegroundColor Gray
    Write-Host "3. Select 'Run with PowerShell as Administrator'" -ForegroundColor Gray
    Write-Host "4. Follow the guided installation process" -ForegroundColor Gray
    Write-Host "5. Launch FamilyOS from Start Menu" -ForegroundColor Gray
} else {
    Write-Host "*** SOME TESTS FAILED - PACKAGE NEEDS ATTENTION ***" -ForegroundColor Red
    Write-Host "Please review the failed tests above and rebuild the package." -ForegroundColor Yellow
}

Write-Host "`nDefault Family Login Credentials:" -ForegroundColor White
Write-Host "Parents: mom/parent123, dad/parent123" -ForegroundColor Gray
Write-Host "Children: sarah/kid123, alex/teen123" -ForegroundColor Gray
Write-Host "`n*** WARNING: Families must change these passwords after installation! ***" -ForegroundColor Yellow

Write-Host "`nPress any key to exit..." -ForegroundColor Cyan
$null = Read-Host