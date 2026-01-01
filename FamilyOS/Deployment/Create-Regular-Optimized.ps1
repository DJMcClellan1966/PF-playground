# FamilyOS Regular Optimized Build (without Native AOT)
param(
    [string]$Version = "1.0.0",
    [string[]]$Architectures = @("win-x64", "win-arm64"),
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$script:startTime = Get-Date
$script:basePath = Split-Path -Parent $PSScriptRoot
$script:releasePath = Join-Path $basePath "Releases"
$script:packagePath = Join-Path $releasePath "FamilyOS-Optimized-v$Version"

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  FAMILYOS OPTIMIZED BUILD" -ForegroundColor Green  
Write-Host "  High Performance without Native AOT" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

try {
    # Clean and prepare
    Write-Host "[STEP 1] Preparing build environment..." -ForegroundColor Cyan
    if (Test-Path $releasePath) {
        Remove-Item $releasePath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $packagePath -Force | Out-Null

    # Verify .NET SDK
    Write-Host "[STEP 2] Verifying .NET SDK..." -ForegroundColor Cyan
    $dotnetVersion = & dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ".NET SDK required"
    }
    Write-Host "    + .NET SDK $dotnetVersion detected" -ForegroundColor Green

    # Build optimized versions for each architecture
    Write-Host "[STEP 3] Building optimized versions..." -ForegroundColor Cyan

    foreach ($arch in $Architectures) {
        Write-Host "  Building for $arch architecture..." -ForegroundColor Yellow
        
        $outputPath = Join-Path $packagePath "temp-$arch"
        New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
        
        $csprojPath = Join-Path $basePath "FamilyOS.csproj"
        
        # Regular optimized publish arguments (no AOT)
        $publishArgs = @(
            "publish"
            $csprojPath
            "-c", "Release"
            "-r", $arch
            "--self-contained", "true"
            "-p:PublishAot=false"
            "-p:PublishSingleFile=true"
            "-p:PublishTrimmed=true"
            "-p:TrimMode=partial"
            "-p:OptimizationPreference=Speed"
            "-p:AssemblyVersion=$Version"
            "-p:FileVersion=$Version"
            "-p:Version=$Version"
            "-o", $outputPath
            "--verbosity", "minimal"
        )
        
        Write-Host "    Compiling optimized build..." -ForegroundColor Gray
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $arch"
        }
        
        # Measure results
        $exePath = Join-Path $outputPath "FamilyOS.exe"
        if (Test-Path $exePath) {
            $fileInfo = Get-ItemProperty $exePath
            $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
            Write-Host "    + Build completed: $sizeMB MB" -ForegroundColor Green
        }
        
        # Copy build to package
        $archPackagePath = Join-Path $packagePath $arch
        New-Item -ItemType Directory -Path $archPackagePath -Force | Out-Null
        Copy-Item "$outputPath\*" $archPackagePath -Recurse -Force
    }

    # Create package
    Write-Host "[STEP 4] Creating distribution package..." -ForegroundColor Cyan
    
    $zipPath = Join-Path $releasePath "FamilyOS-v$Version-Optimized.zip"
    
    if (Get-Command "Compress-Archive" -ErrorAction SilentlyContinue) {
        Compress-Archive -Path "$packagePath\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force
        $zipInfo = Get-ItemProperty $zipPath
        $zipSizeMB = [math]::Round($zipInfo.Length / 1MB, 2)
        Write-Host "    + Package created: $zipSizeMB MB" -ForegroundColor Green
    }

    # Summary
    $totalTime = ((Get-Date) - $script:startTime).TotalMinutes
    Write-Host ""
    Write-Host "[SUCCESS] Optimized build complete!" -ForegroundColor Green
    Write-Host "Build completed in $([math]::Round($totalTime, 1)) minutes" -ForegroundColor Gray
    Write-Host "Package location: $packagePath" -ForegroundColor Gray
    if (Test-Path $zipPath) {
        Write-Host "ZIP package: $zipPath" -ForegroundColor Gray
    }

} catch {
    Write-Host ""
    Write-Host "[ERROR] Build failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "*** FamilyOS optimized build ready! ***" -ForegroundColor Green