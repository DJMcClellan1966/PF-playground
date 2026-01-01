#!/usr/bin/env pwsh
<#
.SYNOPSIS
    FamilyOS Security Testing and Vulnerability Assessment Tool
    
.DESCRIPTION
    Comprehensive security testing script that performs rigorous analysis of the FamilyOS system
    for vulnerabilities, security misconfigurations, and potential attack vectors.
    
.NOTES
    Author: Security Analysis Agent
    Version: 1.0
    Last Modified: 2024-12-15
    
    CRITICAL SECURITY FINDINGS WILL BE HIGHLIGHTED IN RED
    WARNINGS WILL BE HIGHLIGHTED IN YELLOW
    GENERAL INFO WILL BE IN GREEN
#>

[CmdletBinding()]
param(
    [string]$FamilyOSPath = "$PWD",
    [switch]$VerboseOutput,
    [switch]$PenetrationTest,
    [switch]$ExportReport
)

# Color coding for severity levels
function Write-Critical { param([string]$Message) Write-Host "🚨 CRITICAL: $Message" -ForegroundColor Red -BackgroundColor DarkRed }
function Write-High { param([string]$Message) Write-Host "⚠️  HIGH: $Message" -ForegroundColor Yellow }
function Write-Medium { param([string]$Message) Write-Host "🔶 MEDIUM: $Message" -ForegroundColor Magenta }
function Write-Low { param([string]$Message) Write-Host "ℹ️  LOW: $Message" -ForegroundColor Cyan }
function Write-Info { param([string]$Message) Write-Host "✅ INFO: $Message" -ForegroundColor Green }

$script:SecurityFindings = @()
$script:CriticalCount = 0
$script:HighCount = 0
$script:MediumCount = 0
$script:LowCount = 0

function Add-SecurityFinding {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Critical', 'High', 'Medium', 'Low')]
        [string]$Severity,
        
        [Parameter(Mandatory)]
        [string]$Finding,
        
        [Parameter(Mandatory)]
        [string]$Description,
        
        [string]$Location = "",
        [string]$Recommendation = "",
        [string]$CWE = "",
        [string]$CVSS = ""
    )
    
    $script:SecurityFindings += [PSCustomObject]@{
        Severity = $Severity
        Finding = $Finding
        Description = $Description
        Location = $Location
        Recommendation = $Recommendation
        CWE = $CWE
        CVSS = $CVSS
        Timestamp = Get-Date
    }
    
    switch ($Severity) {
        'Critical' { $script:CriticalCount++; Write-Critical "$Finding - $Description" }
        'High' { $script:HighCount++; Write-High "$Finding - $Description" }
        'Medium' { $script:MediumCount++; Write-Medium "$Finding - $Description" }
        'Low' { $script:LowCount++; Write-Low "$Finding - $Description" }
    }
}

function Test-PasswordSecurity {
    Write-Host "`n🔐 ANALYZING PASSWORD SECURITY..." -ForegroundColor Yellow
    
    # Check for hardcoded salts
    $saltPattern = 'family_salt|password.*salt|salt.*=.*\"[^\"]+\"'
    $saltFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $saltPattern -AllMatches
    
    if ($saltFiles) {
        Add-SecurityFinding -Severity "Critical" -Finding "Hardcoded Salt" `
            -Description "Static salt 'family_salt' found in password hashing. This makes rainbow table attacks feasible." `
            -Location "$($saltFiles[0].Filename):$($saltFiles[0].LineNumber)" `
            -Recommendation "Use cryptographically random salts per password" `
            -CWE "CWE-328" -CVSS "7.5"
    }
    
    # Check for weak hashing algorithms
    $weakHashPattern = 'MD5|SHA1(?!.*256)|GetHashCode\(\)'
    $weakHashFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $weakHashPattern -AllMatches
    
    if ($weakHashFiles) {
        Add-SecurityFinding -Severity "High" -Finding "Weak Hash Algorithm" `
            -Description "Potentially weak hashing algorithms detected" `
            -Location "$($weakHashFiles[0].Filename)" `
            -CWE "CWE-327"
    }
    
    # Check password complexity requirements
    $passwordPolicyFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern "Length.*[<>=].*[0-9]" -AllMatches
    
    foreach ($file in $passwordPolicyFiles) {
        if ($file.Line -match "Length.*[<>=].*([0-9]+)") {
            $minLength = [int]$matches[1]
            if ($minLength -lt 8) {
                Add-SecurityFinding -Severity "Medium" -Finding "Weak Password Policy" `
                    -Description "Password minimum length of $minLength is below security standards" `
                    -Location "$($file.Filename):$($file.LineNumber)" `
                    -Recommendation "Enforce minimum 12 characters with complexity requirements"
            }
        }
    }
}

function Test-EncryptionSecurity {
    Write-Host "`n🔒 ANALYZING ENCRYPTION SECURITY..." -ForegroundColor Yellow
    
    # Check for hardcoded encryption keys
    $keyPatterns = @(
        'Key.*=.*\"[A-Za-z0-9+/=]{20,}\"',
        'SecretKey.*=.*\"[^\"]+\"',
        'EncryptionKey.*=.*\"[^\"]+\"',
        'Convert\.FromBase64String\(\"[A-Za-z0-9+/=]+\"\)'
    )
    
    foreach ($pattern in $keyPatterns) {
        $keyFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs","*.json","*.config" | 
            Select-String -Pattern $pattern -AllMatches
        
        if ($keyFiles) {
            Add-SecurityFinding -Severity "Critical" -Finding "Hardcoded Encryption Key" `
                -Description "Encryption keys found hardcoded in source code" `
                -Location "$($keyFiles[0].Filename):$($keyFiles[0].LineNumber)" `
                -Recommendation "Use secure key management (Azure Key Vault, HSM, etc.)" `
                -CWE "CWE-798" -CVSS "9.8"
        }
    }
    
    # Check for deprecated encryption methods
    $weakEncryptionPattern = 'DES|TripleDES|RC4|MD5|SHA1(?!.*256)'
    $weakEncFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $weakEncryptionPattern -AllMatches
    
    if ($weakEncFiles) {
        Add-SecurityFinding -Severity "High" -Finding "Weak Encryption Algorithm" `
            -Description "Deprecated or weak encryption algorithms detected" `
            -Location "$($weakEncFiles[0].Filename)" `
            -CWE "CWE-327"
    }
    
    # Check for proper IV handling
    $ivPattern = 'IV.*=.*new byte\[|GenerateIV\(\)'
    $ivFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $ivPattern -AllMatches
    
    if (-not $ivFiles) {
        Add-SecurityFinding -Severity "High" -Finding "Missing IV Generation" `
            -Description "No proper Initialization Vector (IV) generation found for AES encryption" `
            -Recommendation "Always generate random IVs for each encryption operation"
    }
}

function Test-AuthenticationSecurity {
    Write-Host "`n🛡️ ANALYZING AUTHENTICATION SECURITY..." -ForegroundColor Yellow
    
    # Check for JWT security issues
    $jwtFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern "ClockSkew.*TimeSpan\.Zero" -AllMatches
    
    if ($jwtFiles) {
        Add-SecurityFinding -Severity "Medium" -Finding "Zero Clock Skew" `
            -Description "JWT ClockSkew set to zero may cause legitimate tokens to be rejected" `
            -Location "$($jwtFiles[0].Filename)" `
            -Recommendation "Use reasonable clock skew (5-15 minutes)"
    }
    
    # Check for brute force protection
    $bruteForcePattern = 'FailedLoginAttempts|IsAccountLocked|lockout'
    $bruteForceFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $bruteForcePattern -AllMatches
    
    if ($bruteForceFiles) {
        Write-Info "Account lockout mechanism found"
    } else {
        Add-SecurityFinding -Severity "High" -Finding "No Brute Force Protection" `
            -Description "No account lockout or brute force protection mechanism detected" `
            -Recommendation "Implement account lockout after failed attempts"
    }
}

function Test-InputValidation {
    Write-Host "`n🔍 ANALYZING INPUT VALIDATION..." -ForegroundColor Yellow
    
    # Check for SQL injection vulnerabilities
    $sqlPattern = '(ExecuteCommand|SqlCommand|CommandText).*\+.*\$|\$"[^"]*\{[^}]*\}[^"]*".*Sql'
    $sqlFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $sqlPattern -AllMatches
    
    if ($sqlFiles) {
        Add-SecurityFinding -Severity "Critical" -Finding "Potential SQL Injection" `
            -Description "String concatenation in SQL commands detected" `
            -Location "$($sqlFiles[0].Filename)" `
            -CWE "CWE-89" -CVSS "9.8"
    }
    
    # Check for command injection vulnerabilities
    $commandPattern = 'Process\.Start.*\+|cmd\.exe.*\+|powershell.*\+'
    $commandFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $commandPattern -AllMatches
    
    if ($commandFiles) {
        Add-SecurityFinding -Severity "Critical" -Finding "Potential Command Injection" `
            -Description "Unsanitized input in system command execution" `
            -Location "$($commandFiles[0].Filename)" `
            -CWE "CWE-78" -CVSS "9.8"
    }
    
    # Check for path traversal vulnerabilities
    $pathPattern = 'Path\.Combine\([^)]*\+|File\..*\(.*\+.*\.|\.\.[\\/]'
    $pathFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $pathPattern -AllMatches
    
    if ($pathFiles) {
        Add-SecurityFinding -Severity "High" -Finding "Potential Path Traversal" `
            -Description "Unsanitized file path operations detected" `
            -Location "$($pathFiles[0].Filename)" `
            -CWE "CWE-22"
    }
}

function Test-DataExposure {
    Write-Host "`n📊 ANALYZING DATA EXPOSURE RISKS..." -ForegroundColor Yellow
    
    # Check for sensitive data in logs
    $logPattern = 'Log.*password|Log.*pin|Log.*token|Log.*key'
    $logFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $logPattern -AllMatches
    
    if ($logFiles) {
        Add-SecurityFinding -Severity "High" -Finding "Sensitive Data in Logs" `
            -Description "Potentially sensitive information being logged" `
            -Location "$($logFiles[0].Filename)" `
            -CWE "CWE-532"
    }
    
    # Check for debug information exposure
    $debugPattern = '#if DEBUG|Console\.WriteLine.*password|Console\.WriteLine.*pin'
    $debugFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $debugPattern -AllMatches
    
    if ($debugFiles) {
        Add-SecurityFinding -Severity "Medium" -Finding "Debug Information Exposure" `
            -Description "Debug code that may expose sensitive information" `
            -Location "$($debugFiles[0].Filename)"
    }
    
    # Check for exception details exposure
    $exceptionPattern = 'Exception.*Message.*Response|ex\.ToString\(\).*Response'
    $exceptionFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $exceptionPattern -AllMatches
    
    if ($exceptionFiles) {
        Add-SecurityFinding -Severity "Medium" -Finding "Exception Details Exposure" `
            -Description "Detailed exception information may be exposed to users" `
            -Location "$($exceptionFiles[0].Filename)" `
            -CWE "CWE-209"
    }
}

function Test-ConfigurationSecurity {
    Write-Host "`n⚙️ ANALYZING CONFIGURATION SECURITY..." -ForegroundColor Yellow
    
    # Check for insecure HTTP configurations
    $httpPattern = 'RequireHttpsMetadata.*false|UseHttps.*false|"http://[^"]+"'
    $httpFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs","*.json" | 
        Select-String -Pattern $httpPattern -AllMatches
    
    if ($httpFiles) {
        Add-SecurityFinding -Severity "High" -Finding "Insecure HTTP Configuration" `
            -Description "HTTPS not enforced in configuration" `
            -Location "$($httpFiles[0].Filename)" `
            -Recommendation "Enforce HTTPS in production environments"
    }
    
    # Check for default/weak CORS policies
    $corsPattern = 'AllowAnyOrigin|AllowAnyMethod|AllowAnyHeader|\*'
    $corsFiles = Get-ChildItem -Path $FamilyOSPath -Recurse -Include "*.cs" | 
        Select-String -Pattern $corsPattern -AllMatches
    
    if ($corsFiles) {
        Add-SecurityFinding -Severity "Medium" -Finding "Permissive CORS Policy" `
            -Description "Overly permissive CORS configuration detected" `
            -Location "$($corsFiles[0].Filename)" `
            -Recommendation "Restrict CORS to specific origins and methods"
    }
}

function Test-DependencyVulnerabilities {
    Write-Host "`n📦 ANALYZING DEPENDENCY VULNERABILITIES..." -ForegroundColor Yellow
    
    try {
        Push-Location $FamilyOSPath
        
        # Run dotnet list package --vulnerable if available
        $vulnerablePackages = dotnet list package --vulnerable 2>&1
        
        if ($vulnerablePackages -match "vulnerable|security") {
            Add-SecurityFinding -Severity "High" -Finding "Vulnerable Dependencies" `
                -Description "Vulnerable NuGet packages detected" `
                -Recommendation "Update to latest secure versions"
        }
        
        # Check for outdated packages
        $outdatedPackages = dotnet list package --outdated 2>&1
        if ($outdatedPackages -match "outdated") {
            Add-SecurityFinding -Severity "Medium" -Finding "Outdated Dependencies" `
                -Description "Outdated packages may contain known vulnerabilities"
        }
        
    } catch {
        Write-Info "Dependency vulnerability check skipped - dotnet CLI not available"
    } finally {
        Pop-Location
    }
}

function Test-RuntimeSecurity {
    Write-Host "`n🚀 TESTING RUNTIME SECURITY..." -ForegroundColor Yellow
    
    if ($PenetrationTest) {
        Write-Host "🔥 PENETRATION TESTING MODE ENABLED" -ForegroundColor Red
        
        # Test build with security flags
        try {
            Push-Location $FamilyOSPath
            
            # Test for buffer overflow protections
            $buildResult = dotnet build --verbosity minimal 2>&1
            if ($buildResult -match "error|fail") {
                Add-SecurityFinding -Severity "High" -Finding "Build Failures" `
                    -Description "Application fails to build, may indicate security issues"
            }
            
            # Test memory safety features
            if (Test-Path "*.csproj") {
                $csprojContent = Get-Content "*.csproj" -Raw
                if ($csprojContent -notmatch "Nullable.*enable|TreatWarningsAsErrors") {
                    Add-SecurityFinding -Severity "Medium" -Finding "Missing Security Hardening" `
                        -Description "Project lacks nullable reference types and warning enforcement"
                }
            }
            
        } catch {
            Add-SecurityFinding -Severity "High" -Finding "Runtime Test Failed" `
                -Description "Cannot perform runtime security tests: $($_.Exception.Message)"
        } finally {
            Pop-Location
        }
    }
}

function Start-SecurityAudit {
    Write-Host "🔐 FAMILYOS COMPREHENSIVE SECURITY AUDIT" -ForegroundColor Cyan
    Write-Host "=======================================" -ForegroundColor Cyan
    Write-Host "Target: $FamilyOSPath" -ForegroundColor White
    Write-Host "Time: $(Get-Date)" -ForegroundColor White
    Write-Host ""
    
    # Run all security tests
    Test-PasswordSecurity
    Test-EncryptionSecurity
    Test-AuthenticationSecurity
    Test-InputValidation
    Test-DataExposure
    Test-ConfigurationSecurity
    Test-DependencyVulnerabilities
    Test-RuntimeSecurity
    
    # Generate summary report
    Write-Host "`n📋 SECURITY AUDIT SUMMARY" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    
    if ($script:CriticalCount -gt 0) {
        Write-Critical "CRITICAL VULNERABILITIES: $script:CriticalCount"
    }
    if ($script:HighCount -gt 0) {
        Write-High "HIGH RISK ISSUES: $script:HighCount"
    }
    if ($script:MediumCount -gt 0) {
        Write-Medium "MEDIUM RISK ISSUES: $script:MediumCount"
    }
    if ($script:LowCount -gt 0) {
        Write-Low "LOW RISK ISSUES: $script:LowCount"
    }
    
    $totalIssues = $script:CriticalCount + $script:HighCount + $script:MediumCount + $script:LowCount
    
    if ($totalIssues -eq 0) {
        Write-Info "✅ NO MAJOR SECURITY ISSUES DETECTED"
    } else {
        Write-Host ""
        Write-Host "⚠️  TOTAL SECURITY ISSUES FOUND: $totalIssues" -ForegroundColor Yellow
        
        if ($script:CriticalCount -gt 0) {
            Write-Host "🚨 IMMEDIATE ACTION REQUIRED FOR CRITICAL ISSUES!" -ForegroundColor Red -BackgroundColor DarkRed
        }
    }
    
    # Export detailed report if requested
    if ($ExportReport) {
        $reportPath = "$FamilyOSPath\SecurityAuditReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
        $script:SecurityFindings | ConvertTo-Json -Depth 5 | Out-File $reportPath
        Write-Info "Detailed report exported to: $reportPath"
    }
}

# Execute the security audit
Start-SecurityAudit

# Return findings for programmatic access
return $script:SecurityFindings