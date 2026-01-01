# Rebuild and publish FamilyOS in a GPT4All-like, simple flow
# ASCII-only output; uses win-x64 publish
# Usage: Run from project root (FamilyOS folder)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Info($msg) { Write-Host "[INFO] $msg" }
function Write-Ok($msg) { Write-Host "[OK]   $msg" }
function Write-Warn($msg) { Write-Host "[WARN] $msg" }
function Write-Err($msg) { Write-Host "[ERR]  $msg" }

try {
    $root = (Get-Location).Path
    Write-Host "========================================="
    Write-Host " FamilyOS Rebuild & Perf (Windows-only)  "
    Write-Host "========================================="
    Write-Info "Root: $root"

    # Clean output folders
    Write-Info "Cleaning bin/ and obj/"
    if (Test-Path "$root\bin") { Remove-Item "$root\bin" -Recurse -Force }
    if (Test-Path "$root\obj") { Remove-Item "$root\obj" -Recurse -Force }

    # Restore/build
    Write-Info "dotnet restore"
    dotnet restore | Out-Host

    Write-Info "dotnet build (Debug)"
    dotnet build | Out-Host

    # Publish win-x64
    Write-Info "dotnet publish -c Release -r win-x64 --self-contained false"
    dotnet publish -c Release -r win-x64 --self-contained false | Out-Host

    $publishPath = Resolve-Path "$root\bin\Release\net8.0-windows\win-x64\publish" | Select-Object -ExpandProperty Path
    Write-Ok "Publish output: $publishPath"

    # Zip artifacts
    $zipName = "FamilyOS-win-x64-publish.zip"
    $zipPath = Join-Path $root $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Write-Info "Creating zip: $zipPath"
    Compress-Archive -Path (Join-Path $publishPath '*') -DestinationPath $zipPath
    Write-Ok "Zip created: $zipPath"

    # Smoke test
    $exe = Join-Path $publishPath 'FamilyOS.exe'
    if (Test-Path $exe) {
        Write-Info "Smoke test: $exe --demo-windows --ascii"
        & $exe --demo-windows --ascii | Out-Host
        Write-Ok "Smoke test completed"

        Write-Info "Perf suite: $exe --perf-opt --ascii"
        & $exe --perf-opt --ascii | Out-Host
        Write-Ok "Perf suite completed"
    } else {
        Write-Warn "FamilyOS.exe not found at publish output"
    }

    Write-Ok "Done"
}
catch {
    Write-Err $_.Exception.Message
    exit 1
}
