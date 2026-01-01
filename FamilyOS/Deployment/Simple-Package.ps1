param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

# Get paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
# Suppress false positive - this variable is used below for csproj path
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'projectDir')]
$projectDir = Split-Path -Parent $scriptDir  
$buildPath = Join-Path $scriptDir "Build"
$releasePath = Join-Path $buildPath "Release"
$packagePath = Join-Path $buildPath "Package"

Write-Host "=== FamilyOS Package Creator ===" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Green

try {
    # Clean directories
    if (Test-Path $buildPath) { Remove-Item $buildPath -Recurse -Force }
    New-Item -ItemType Directory -Path @($buildPath, $releasePath, $packagePath) -Force | Out-Null

    # Find project file
    $csprojPath = Join-Path $projectDir "FamilyOS.csproj"
    if (-not (Test-Path $csprojPath)) {
        throw "Project file not found: $csprojPath"
    }

    # Build application
    Write-Host "Building FamilyOS..." -ForegroundColor Yellow
    
    $outputPath = Join-Path $releasePath "win-x64"
    & dotnet publish $csprojPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $outputPath
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    Write-Host "Build completed successfully" -ForegroundColor Green

    # Copy files to package
    Copy-Item "$outputPath\*" $packagePath -Recurse -Force
    
    # Create manifest
    $manifest = @{
        Name = "FamilyOS"
        Version = $Version
        Description = "Family Digital Safety Platform"
    }
    
    $manifest | ConvertTo-Json | Out-File -FilePath (Join-Path $packagePath "manifest.json") -Encoding UTF8

    # Create ZIP
    $zipPath = Join-Path $releasePath "FamilyOS-v$Version.zip"
    Compress-Archive -Path "$packagePath\*" -DestinationPath $zipPath -Force
    
    Write-Host "Package created: $zipPath" -ForegroundColor Green

} catch {
    Write-Error "Failed: $($_.Exception.Message)"
    exit 1
}

Write-Host "Package creation completed!" -ForegroundColor Green