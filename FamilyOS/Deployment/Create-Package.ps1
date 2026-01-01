#Requires -Version 5.1

<#
.SYNOPSIS
    Creates FamilyOS installation packages for multiple architectures.

.DESCRIPTION
    This script builds self-contained FamilyOS applications for x64, x86, and ARM64 architectures,
    creates Windows shortcuts and registry entries, and creates a ZIP package for distribution.

.PARAMETER Version
    Version number for the build (default: 1.0.0)

.EXAMPLE
    .\Create-Package.ps1 -Version "1.2.0"
#>

param(
    [string]$Version = "1.0.0"
)

# Enable strict error handling
$ErrorActionPreference = "Stop"

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$sourceDir = Join-Path $projectDir "Source"

# Output directories
$buildPath = Join-Path $scriptDir "Build"
$releasePath = Join-Path $buildPath "Release"
$packagePath = Join-Path $buildPath "Package"

Write-Host "=== FamilyOS Installation Package Creator ===" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Green

try {
    # Clean and create directories
    Write-Host "`nPreparing build environment..." -ForegroundColor Yellow
    if (Test-Path $buildPath) {
        Remove-Item $buildPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path @($buildPath, $releasePath, $packagePath) -Force | Out-Null

    # Verify source files exist
    $csprojPath = Join-Path $sourceDir "FamilyOS.csproj"
    if (-not (Test-Path $csprojPath)) {
        throw "Project file not found: $csprojPath"
    }

    # Build for x64 architecture
    Write-Host "`nBuilding FamilyOS for Windows x64..." -ForegroundColor Yellow
    
    $outputPath = Join-Path $releasePath "win-x64"
    $publishArgs = @(
        "publish"
        $csprojPath
        "-c", "Release"
        "-r", "win-x64"
        "--self-contained", "true"
        "-p:PublishSingleFile=true"
        "-p:IncludeNativeLibrariesForSelfExtract=true"
        "-p:AssemblyVersion=$Version"
        "-p:FileVersion=$Version"
        "-p:Version=$Version"
        "-o", $outputPath
        "--verbosity", "minimal"
    )
    
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for win-x64"
    }
    
    Write-Host "    ✓ Build completed for win-x64" -ForegroundColor Green

    # Copy build to package directory
    Copy-Item "$outputPath\*" $packagePath -Recurse -Force
    
    # Copy documentation
    Write-Host "`nPackaging documentation..." -ForegroundColor Yellow
    $docsPath = Join-Path $packagePath "Documentation"
    New-Item -ItemType Directory -Path $docsPath -Force | Out-Null
    
    $docsSourcePath = Join-Path $scriptDir "*.md"
    Copy-Item $docsSourcePath $docsPath -Force -ErrorAction SilentlyContinue

    # Create installer manifest
    Write-Host "`nCreating installation manifest..." -ForegroundColor Yellow
    
    $manifest = @{
        Name = "FamilyOS"
        Version = $Version
        Description = "Family Digital Safety Platform"
        Architecture = "win-x64"
        InstallDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        RequiredFramework = ".NET 8.0 (self-contained)"
        MinimumOS = "Windows 10 Version 1809 or later"
    }
    
    $manifestPath = Join-Path $packagePath "install-manifest.json"
    $manifest | ConvertTo-Json -Depth 3 | Out-File -FilePath $manifestPath -Encoding UTF8

    # Create ZIP package for distribution
    Write-Host "`nCreating ZIP package..." -ForegroundColor Yellow
    
    $zipPath = Join-Path $releasePath "FamilyOS-v$Version-Portable.zip"
    
    if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
        Compress-Archive -Path "$packagePath\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force
        Write-Host "    ✓ Portable ZIP package created: $zipPath" -ForegroundColor Green
    } else {
        Write-Warning "Compress-Archive not available. ZIP package creation skipped."
    }

    # Generate installation summary
    Write-Host "`n=== Package Creation Complete ===" -ForegroundColor Cyan
    Write-Host "Package Details:" -ForegroundColor White
    Write-Host "  Version: $Version" -ForegroundColor Gray
    Write-Host "  Architecture: win-x64" -ForegroundColor Gray
    
    $packageSizeMB = [math]::Round((Get-ChildItem $packagePath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
    Write-Host "  Package Size: $packageSizeMB MB" -ForegroundColor Gray
    
    Write-Host "`nGenerated Files:" -ForegroundColor White
    Get-ChildItem $releasePath -File | ForEach-Object {
        $sizeMB = [math]::Round($_.Length / 1MB, 2)
        Write-Host "  $($_.Name) - $sizeMB MB" -ForegroundColor Gray
    }
    
    Write-Host "`nInstallation Options:" -ForegroundColor White
    Write-Host "  1. Use Install-FamilyOS.ps1 for automated installation" -ForegroundColor Gray
    Write-Host "  2. Extract ZIP package for manual installation" -ForegroundColor Gray
    
    Write-Host "`n✓ Find your installation package in: $releasePath" -ForegroundColor Green

} catch {
    Write-Error "Package creation failed: $($_.Exception.Message)"
    Write-Host "Error Details:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}

Write-Host "`nPackage creation completed successfully!" -ForegroundColor Green