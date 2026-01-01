# FamilyOS Production Deployment Script for Windows
# PowerShell version of the deployment script

Write-Host "FamilyOS Production Deployment" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

# Uses approved PowerShell verb 'Install' for PSScriptAnalyzer compliance
function Install-Platform {
    param(
        [string]$Platform,
        [string]$TargetDir
    )
    
    Write-Host "Installing FamilyOS for $Platform..." -ForegroundColor Yellow
    
    # Create target directory
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    
    # Copy deployment files
    Copy-Item -Path ".\publish\$Platform\*" -Destination $TargetDir -Recurse -Force
    
    # Copy production configuration
    Copy-Item -Path ".\publish\appsettings.production.json" -Destination $TargetDir -Force
    
    # Create startup script for Windows
    if ($Platform -eq "win-x64") {
        $startupScript = @'
@echo off
set ASPNETCORE_ENVIRONMENT=Production
set FAMILYOS_CONFIG_PATH=.\appsettings.production.json
echo Starting FamilyOS in Production mode...
FamilyOS.exe
pause
'@
        $startupScript | Out-File -FilePath "$TargetDir\start-familyos.bat" -Encoding ASCII
    }
    
    Write-Host "$Platform deployment complete: $TargetDir" -ForegroundColor Green
}

# Main deployment
Write-Host "Starting Windows deployment..." -ForegroundColor Cyan
Write-Host ""

# Deploy Windows
Install-Platform -Platform "win-x64" -TargetDir ".\deployment\windows"

# Deploy Windows ARM
Install-Platform -Platform "win-arm64" -TargetDir ".\deployment\windows-arm"

Write-Host ""
Write-Host "FamilyOS Production Deployment Complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Deployment locations:" -ForegroundColor Cyan
Write-Host "- Windows (x64): .\deployment\windows\" -ForegroundColor White
Write-Host "- Windows (ARM): .\deployment\windows-arm\" -ForegroundColor White
Write-Host ""
Write-Host "To start FamilyOS in production:" -ForegroundColor Cyan
Write-Host "- Windows: Run start-familyos.bat or FamilyOS.exe" -ForegroundColor White
Write-Host ""
Write-Host "Ready for production use!" -ForegroundColor Green