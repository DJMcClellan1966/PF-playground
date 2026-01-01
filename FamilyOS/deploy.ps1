# FamilyOS Production Deployment Script for Windows
# PowerShell version of the deployment script

Write-Host "🚀 FamilyOS Production Deployment" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

function Install-Platform {
    param(
        [string]$Platform,
        [string]$TargetDir
    )
    
    Write-Host "📦 Deploying FamilyOS for $Platform..." -ForegroundColor Yellow
    
    # Create target directory
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    
    # Copy deployment files
    Copy-Item -Path ".\publish\$Platform\*" -Destination $TargetDir -Recurse -Force
    
    # Copy production configuration
    Copy-Item -Path ".\publish\appsettings.production.json" -Destination $TargetDir -Force
    
    # Create startup script for Windows
    if ($Platform -eq "win-x64") {
        $startupScript = @"
@echo off
set ASPNETCORE_ENVIRONMENT=Production
set FAMILYOS_CONFIG_PATH=.\appsettings.production.json
echo Starting FamilyOS in Production mode...
FamilyOS.exe
pause
"@
        $startupScript | Out-File -FilePath "$TargetDir\start-familyos.bat" -Encoding ASCII
    }
    
    Write-Host "✅ $Platform deployment complete: $TargetDir" -ForegroundColor Green
}

# Main deployment
Write-Host "🎯 Starting cross-platform deployment..." -ForegroundColor Cyan
Write-Host ""

# Deploy Windows
Install-Platform -Platform "win-x64" -TargetDir ".\deployment\windows"

# Deploy Linux  
Install-Platform -Platform "linux-x64" -TargetDir ".\deployment\linux"

# Deploy macOS
Install-Platform -Platform "osx-x64" -TargetDir ".\deployment\macos"

Write-Host ""
Write-Host "🏆 FamilyOS Production Deployment Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Deployment locations:" -ForegroundColor Cyan
Write-Host "• Windows: .\deployment\windows\" -ForegroundColor White
Write-Host "• Linux:   .\deployment\linux\" -ForegroundColor White  
Write-Host "• macOS:   .\deployment\macos\" -ForegroundColor White
Write-Host ""
Write-Host "To start FamilyOS in production:" -ForegroundColor Cyan
Write-Host "• Windows: Run start-familyos.bat or FamilyOS.exe" -ForegroundColor White
Write-Host "• Linux:   Run ./FamilyOS (requires .NET 8.0 runtime)" -ForegroundColor White
Write-Host "• macOS:   Run ./FamilyOS (requires .NET 8.0 runtime)" -ForegroundColor White
Write-Host ""
Write-Host "✅ Ready for production use!" -ForegroundColor Green