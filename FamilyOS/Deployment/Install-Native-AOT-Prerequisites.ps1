# Install C++ Build Tools for Native AOT
param(
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================" -ForegroundColor Blue
Write-Host "  INSTALLING NATIVE AOT PREREQUISITES" -ForegroundColor Blue  
Write-Host "  C++ Build Tools for Ultra Performance" -ForegroundColor Blue
Write-Host "================================================" -ForegroundColor Blue
Write-Host ""

try {
    # Check if already installed
    Write-Host "[STEP 1] Checking existing C++ build tools..." -ForegroundColor Cyan
    
    $vcToolsPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\VC\Tools"
    $vcToolsInstalled = Test-Path $vcToolsPath
    
    if ($vcToolsInstalled -and !$Force) {
        Write-Host "    + C++ Build Tools already installed at: $vcToolsPath" -ForegroundColor Green
        Write-Host "    + Use -Force to reinstall" -ForegroundColor Gray
        return
    }

    # Download Visual Studio Build Tools
    Write-Host "[STEP 2] Downloading Visual Studio Build Tools..." -ForegroundColor Cyan
    
    $tempPath = $env:TEMP
    $installerPath = Join-Path $tempPath "vs_buildtools.exe"
    $downloadUrl = "https://aka.ms/vs/17/release/vs_buildtools.exe"
    
    Write-Host "    Downloading from: $downloadUrl" -ForegroundColor Gray
    Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -UseBasicParsing
    Write-Host "    + Downloaded to: $installerPath" -ForegroundColor Green

    # Install C++ build tools with required components
    Write-Host "[STEP 3] Installing C++ Build Tools..." -ForegroundColor Cyan
    Write-Host "    This may take several minutes..." -ForegroundColor Gray
    
    $installArgs = @(
        "--wait"
        "--quiet"
        "--norestart"
        "--add", "Microsoft.VisualStudio.Workload.VCTools"
        "--add", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64"
        "--add", "Microsoft.VisualStudio.Component.Windows11SDK.22000"
        "--add", "Microsoft.VisualStudio.Component.VC.CMake.Project"
    )
    
    $process = Start-Process -FilePath $installerPath -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -eq 0) {
        Write-Host "    + C++ Build Tools installed successfully!" -ForegroundColor Green
    } elseif ($process.ExitCode -eq 3010) {
        Write-Host "    + C++ Build Tools installed (reboot required)" -ForegroundColor Yellow
    } else {
        throw "Installation failed with exit code: $($process.ExitCode)"
    }

    # Verify installation
    Write-Host "[STEP 4] Verifying installation..." -ForegroundColor Cyan
    
    Start-Sleep -Seconds 5  # Allow time for registration
    
    $vcToolsPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\VC\Tools"
    if (Test-Path $vcToolsPath) {
        $vcVersions = Get-ChildItem $vcToolsPath -Directory | Sort-Object Name -Descending
        if ($vcVersions.Count -gt 0) {
            $latestVersion = $vcVersions[0].Name
            Write-Host "    + C++ Tools installed: Version $latestVersion" -ForegroundColor Green
        }
    }

    # Clean up
    Write-Host "[STEP 5] Cleaning up..." -ForegroundColor Cyan
    if (Test-Path $installerPath) {
        Remove-Item $installerPath -Force
        Write-Host "    + Installer cleaned up" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "[SUCCESS] Native AOT prerequisites installed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run: .\Create-Optimized-Package.ps1" -ForegroundColor Yellow
    Write-Host "For ultra-high performance Native AOT builds!" -ForegroundColor Yellow

} catch {
    Write-Host ""
    Write-Host "[ERROR] Installation failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative options:" -ForegroundColor Yellow
    Write-Host "1. Install Visual Studio Community with C++ workload" -ForegroundColor Gray
    Write-Host "2. Use regular optimized build (already available)" -ForegroundColor Gray
    Write-Host "3. Try manual installation from Visual Studio website" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "*** Native AOT prerequisites ready! ***" -ForegroundColor Blue