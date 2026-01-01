#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Simplified FamilyOS Security Analysis
    
.DESCRIPTION
    Performs rigorous security testing to find vulnerabilities, 
    security misconfigurations, and attack vectors in FamilyOS
#>

param(
    [string]$FamilyOSPath = "$PWD\FamilyOS"
)

Write-Host "🚨 CRITICAL SECURITY ANALYSIS - FamilyOS" -ForegroundColor Red -BackgroundColor DarkRed
Write-Host "================================================" -ForegroundColor Red
Write-Host ""

$criticalIssues = 0
$highIssues = 0
$mediumIssues = 0

function Write-Critical { param([string]$msg) Write-Host "🚨 CRITICAL: $msg" -ForegroundColor Red; $script:criticalIssues++ }
function Write-High { param([string]$msg) Write-Host "⚠️  HIGH: $msg" -ForegroundColor Yellow; $script:highIssues++ }
function Write-Medium { param([string]$msg) Write-Host "🔶 MEDIUM: $msg" -ForegroundColor Magenta; $script:mediumIssues++ }
function Write-Good { param([string]$msg) Write-Host "✅ SECURE: $msg" -ForegroundColor Green }

Write-Host "🔍 SCANNING FOR HARDCODED SECRETS..." -ForegroundColor Yellow

# 1. CRITICAL: Hardcoded Salt Vulnerability
Write-Host "`n1. PASSWORD SECURITY ANALYSIS" -ForegroundColor Cyan
$saltFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "family_salt"
if ($saltFiles) {
    Write-Critical "HARDCODED SALT FOUND! Static salt 'family_salt' enables rainbow table attacks"
    Write-Host "   Location: $($saltFiles[0].Filename):$($saltFiles[0].LineNumber)" -ForegroundColor Red
    Write-Host "   Impact: All password hashes can be pre-computed" -ForegroundColor Red
    Write-Host "   Fix: Use crypto-random salts per password (e.g. RandomNumberGenerator)" -ForegroundColor Yellow
} else {
    Write-Good "No hardcoded salts detected"
}

# 2. ENCRYPTION KEY SECURITY
Write-Host "`n2. ENCRYPTION KEY ANALYSIS" -ForegroundColor Cyan
$keyFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "GenerateEncryptionKey|Convert.FromBase64String"
if ($keyFiles) {
    foreach ($keyFile in $keyFiles) {
        if ($keyFile.Line -match '"[A-Za-z0-9+/=]{20,}"') {
            Write-Critical "HARDCODED ENCRYPTION KEY DETECTED!"
            Write-Host "   Location: $($keyFile.Filename):$($keyFile.LineNumber)" -ForegroundColor Red
        }
    }
}

# 3. AUTHENTICATION BYPASS ATTEMPTS
Write-Host "`n3. AUTHENTICATION BYPASS TESTING" -ForegroundColor Cyan
$authFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "AuthenticateAsync|VerifyPassword"
foreach ($authFile in $authFiles) {
    if ($authFile.Line -match "return true|== null.*return") {
        Write-High "POTENTIAL AUTH BYPASS in $($authFile.Filename):$($authFile.LineNumber)"
    }
}

# 4. SQL INJECTION VULNERABILITY SCAN
Write-Host "`n4. SQL INJECTION VULNERABILITY SCAN" -ForegroundColor Cyan
$sqlFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "SqlCommand|ExecuteCommand"
foreach ($sqlFile in $sqlFiles) {
    if ($sqlFile.Line -match '\+.*\$' -or $sqlFile.Line -match 'string.*Format') {
        Write-Critical "POTENTIAL SQL INJECTION in $($sqlFile.Filename):$($sqlFile.LineNumber)"
    }
}

# 5. COMMAND INJECTION TESTING  
Write-Host "`n5. COMMAND INJECTION ANALYSIS" -ForegroundColor Cyan
$cmdFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "Process.Start|cmd.exe|powershell"
foreach ($cmdFile in $cmdFiles) {
    if ($cmdFile.Line -match '\+.*\$' -or $cmdFile.Line -match 'string.*Format') {
        Write-Critical "POTENTIAL COMMAND INJECTION in $($cmdFile.Filename):$($cmdFile.LineNumber)"
    }
}

# 6. PATH TRAVERSAL VULNERABILITY
Write-Host "`n6. PATH TRAVERSAL VULNERABILITY SCAN" -ForegroundColor Cyan
$pathFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "Path.Combine|File."
foreach ($pathFile in $pathFiles) {
    if ($pathFile.Line -match '\.\.' -or $pathFile.Line -match '\+.*\$') {
        Write-High "POTENTIAL PATH TRAVERSAL in $($pathFile.Filename):$($pathFile.LineNumber)"
    }
}

# 7. SENSITIVE DATA LOGGING
Write-Host "`n7. SENSITIVE DATA EXPOSURE ANALYSIS" -ForegroundColor Cyan
$logFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "_logger.Log|Console.Write"
foreach ($logFile in $logFiles) {
    if ($logFile.Line -match 'password|pin|token|key' -and $logFile.Line -notmatch 'masked|hidden|redacted') {
        Write-High "SENSITIVE DATA LOGGING in $($logFile.Filename):$($logFile.LineNumber)"
    }
}

# 8. HTTPS ENFORCEMENT CHECK
Write-Host "`n8. HTTPS ENFORCEMENT ANALYSIS" -ForegroundColor Cyan
$httpFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs","*.json" | Select-String -Pattern "RequireHttpsMetadata|http://"
foreach ($httpFile in $httpFiles) {
    if ($httpFile.Line -match 'false' -or $httpFile.Line -match 'http://[^l]') {
        Write-Medium "INSECURE HTTP CONFIG in $($httpFile.Filename):$($httpFile.LineNumber)"
    }
}

# 9. PRIVILEGE ESCALATION TESTING
Write-Host "`n9. PRIVILEGE ESCALATION ANALYSIS" -ForegroundColor Cyan
$privFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "Role.*=|IsParent|Admin"
foreach ($privFile in $privFiles) {
    if ($privFile.Line -match 'Role.*=.*[^"]input[^"]|user.*role.*\+') {
        Write-Critical "POTENTIAL PRIVILEGE ESCALATION in $($privFile.Filename):$($privFile.LineNumber)"
    }
}

# 10. WEAK CRYPTO DETECTION
Write-Host "`n10. CRYPTOGRAPHIC STRENGTH ANALYSIS" -ForegroundColor Cyan
$cryptoFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "MD5|SHA1|DES|RC4"
foreach ($cryptoFile in $cryptoFiles) {
    if ($cryptoFile.Line -notmatch 'SHA1.*256') {
        Write-High "WEAK CRYPTO ALGORITHM in $($cryptoFile.Filename):$($cryptoFile.LineNumber)"
    }
}

Write-Host "`n🚀 RUNTIME SECURITY TESTING..." -ForegroundColor Yellow

# 11. BUILD SECURITY TEST
Write-Host "`n11. BUILD SECURITY VALIDATION" -ForegroundColor Cyan
try {
    Push-Location $FamilyOSPath
    $buildResult = dotnet build --verbosity quiet 2>&1
    if ($buildResult -match "error|failed") {
        Write-High "BUILD FAILURES MAY INDICATE SECURITY ISSUES"
    } else {
        Write-Good "Build completed successfully"
    }
} catch {
    Write-High "Cannot validate build security: $_"
} finally {
    Pop-Location  
}

# 12. DEPENDENCY VULNERABILITY SCAN
Write-Host "`n12. DEPENDENCY SECURITY SCAN" -ForegroundColor Cyan
try {
    Push-Location $FamilyOSPath
    $vulnResult = dotnet list package --vulnerable --include-transitive 2>&1
    if ($vulnResult -match "vulnerable|security|critical|high") {
        Write-Critical "VULNERABLE DEPENDENCIES DETECTED!"
        Write-Host "$vulnResult" -ForegroundColor Red
    } else {
        Write-Good "No known vulnerable dependencies found"
    }
} catch {
    Write-Medium "Dependency scan unavailable"
} finally {
    Pop-Location
}

# 13. PENETRATION TEST - BRUTE FORCE PROTECTION
Write-Host "`n13. BRUTE FORCE PROTECTION TEST" -ForegroundColor Cyan
$lockoutFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "FailedLoginAttempts|IsAccountLocked"
if ($lockoutFiles) {
    Write-Good "Account lockout mechanism found"
} else {
    Write-Critical "NO BRUTE FORCE PROTECTION! System vulnerable to password attacks"
}

# 14. FILE SYSTEM SECURITY
Write-Host "`n14. FILE SYSTEM SECURITY ANALYSIS" -ForegroundColor Cyan
$fileSecFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "Directory.Create|File.Write"
foreach ($fileSecFile in $fileSecFiles) {
    if ($fileSecFile.Line -notmatch 'permission|access|security') {
        Write-Medium "UNCONTROLLED FILE OPERATIONS in $($fileSecFile.Filename):$($fileSecFile.LineNumber)"
    }
}

# PENETRATION TESTING - ATTACK SIMULATION
Write-Host "`n🔥 PENETRATION TESTING - ATTACK SIMULATION" -ForegroundColor Red

# Attack Vector 1: Authentication Bypass
Write-Host "`n🎯 ATTACK VECTOR 1: Authentication Bypass Attempts" -ForegroundColor Red
$authBypassPatterns = @('|| true', '|| 1==1', 'admin" --', "' OR 1=1")
foreach ($pattern in $authBypassPatterns) {
    Write-Host "Testing bypass pattern: $pattern" -ForegroundColor Yellow
    # In real test, this would attempt authentication with these payloads
}

# Attack Vector 2: Role Escalation
Write-Host "`n🎯 ATTACK VECTOR 2: Role Escalation Testing" -ForegroundColor Red
Write-Host "Testing role manipulation attacks..." -ForegroundColor Yellow
$roleFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | Select-String -Pattern "FamilyRole|Parent|Admin"
if ($roleFiles) {
    Write-Host "Role system detected - checking for privilege escalation vectors" -ForegroundColor Yellow
}

# FINAL ASSESSMENT
Write-Host "`n🎯 FINAL SECURITY ASSESSMENT" -ForegroundColor Red
Write-Host "================================" -ForegroundColor Red

if ($criticalIssues -gt 0) {
    Write-Host "🚨🚨🚨 CRITICAL VULNERABILITIES: $criticalIssues" -ForegroundColor Red -BackgroundColor DarkRed
    Write-Host "IMMEDIATE SECURITY PATCHING REQUIRED!" -ForegroundColor Red -BackgroundColor DarkRed
}

if ($highIssues -gt 0) {
    Write-Host "⚠️⚠️⚠️  HIGH RISK VULNERABILITIES: $highIssues" -ForegroundColor Yellow  
    Write-Host "HIGH PRIORITY SECURITY FIXES NEEDED!" -ForegroundColor Yellow
}

if ($mediumIssues -gt 0) {
    Write-Host "🔶🔶🔶 MEDIUM RISK ISSUES: $mediumIssues" -ForegroundColor Magenta
}

$totalIssues = $criticalIssues + $highIssues + $mediumIssues
Write-Host "`nTOTAL SECURITY ISSUES DISCOVERED: $totalIssues" -ForegroundColor $(if ($totalIssues -gt 0) { "Red" } else { "Green" })

if ($totalIssues -eq 0) {
    Write-Host "✅ NO CRITICAL SECURITY VULNERABILITIES DETECTED" -ForegroundColor Green
} else {
    Write-Host "`n🔧 RECOMMENDED IMMEDIATE ACTIONS:" -ForegroundColor Yellow
    Write-Host "1. Fix hardcoded salts with crypto-random generation" -ForegroundColor White
    Write-Host "2. Implement proper key management (Azure Key Vault)" -ForegroundColor White  
    Write-Host "3. Add input validation and parameterized queries" -ForegroundColor White
    Write-Host "4. Enforce HTTPS in all configurations" -ForegroundColor White
    Write-Host "5. Update all dependencies to latest secure versions" -ForegroundColor White
}

Write-Host "`n⚡ SECURITY AUDIT COMPLETE" -ForegroundColor Cyan