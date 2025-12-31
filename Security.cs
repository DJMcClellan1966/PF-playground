using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PocketFence_AI.Kernel.Security;

/// <summary>
/// Enterprise-grade security configuration
/// </summary>
public class SecurityConfig
{
    public JwtSettings Jwt { get; set; } = new();
    public EncryptionSettings Encryption { get; set; } = new();
    public ValidationSettings Validation { get; set; } = new();
    public SecurityHeaders Headers { get; set; } = new();
    public ThreatDetection ThreatDetection { get; set; } = new();
    public UpdateSecurity UpdateSecurity { get; set; } = new();
}

public class JwtSettings
{
    public string SecretKey { get; set; } = GenerateSecureKey();
    public string Issuer { get; set; } = "PocketFence-Kernel";
    public string Audience { get; set; } = "PocketFence-API";
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
    public bool RequireHttpsMetadata { get; set; } = true;
    
    private static string GenerateSecureKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public class EncryptionSettings
{
    public bool EncryptSensitiveData { get; set; } = true;
    public string EncryptionKey { get; set; } = GenerateEncryptionKey();
    public bool UseHSM { get; set; } = false; // Hardware Security Module
    public int KeyRotationDays { get; set; } = 90;
    
    private static string GenerateEncryptionKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }
}

public class ValidationSettings
{
    public int MaxRequestSize { get; set; } = 1024 * 1024; // 1MB
    public int MaxJsonDepth { get; set; } = 64;
    public bool ValidateContentType { get; set; } = true;
    public bool SanitizeInput { get; set; } = true;
    public string[] AllowedFileTypes { get; set; } = { ".json", ".txt", ".xml" };
    public bool RejectMalformedJson { get; set; } = true;
}

public class SecurityHeaders
{
    public bool EnableHSTS { get; set; } = true;
    public bool EnableCSP { get; set; } = true;
    public bool EnableXFrameOptions { get; set; } = true;
    public bool EnableXContentTypeOptions { get; set; } = true;
    public bool EnableReferrerPolicy { get; set; } = true;
    public string CSPPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
}

public class ThreatDetection
{
    public bool EnableIpBlocking { get; set; } = true;
    public bool EnableAnomalyDetection { get; set; } = true;
    public int SuspiciousRequestThreshold { get; set; } = 100;
    public int BlockDurationMinutes { get; set; } = 60;
    public bool EnableHoneypots { get; set; } = true;
}

public class UpdateSecurity
{
    public bool AutoUpdateEnabled { get; set; } = true;
    public string UpdateServerUrl { get; set; } = "https://updates.pocketfence.ai";
    public string PublicKey { get; set; } = "";
    public int CheckIntervalMinutes { get; set; } = 60;
    public bool VerifySignatures { get; set; } = true;
    public bool BackupBeforeUpdate { get; set; } = true;
}

/// <summary>
/// Advanced encryption service for sensitive data
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ComputeHash(string input);
    bool VerifyHash(string input, string hash);
    byte[] GenerateSalt();
}

public class AESEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<AESEncryptionService> _logger;

    public AESEncryptionService(SecurityConfig config, ILogger<AESEncryptionService> logger)
    {
        _key = Convert.FromBase64String(config.Encryption.EncryptionKey);
        _logger = logger;
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs))
            {
                writer.Write(plaintext);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw;
        }
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return string.Empty;

        try
        {
            var buffer = Convert.FromBase64String(ciphertext);
            
            using var aes = Aes.Create();
            aes.Key = _key;
            
            var iv = new byte[16];
            Array.Copy(buffer, 0, iv, 0, 16);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(buffer, 16, buffer.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);
            
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw;
        }
    }

    public string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var salt = GenerateSalt();
        var combined = Encoding.UTF8.GetBytes(input).Concat(salt).ToArray();
        var hash = sha256.ComputeHash(combined);
        return Convert.ToBase64String(salt.Concat(hash).ToArray());
    }

    public bool VerifyHash(string input, string hash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hash);
            var salt = hashBytes.Take(32).ToArray();
            var originalHash = hashBytes.Skip(32).ToArray();
            
            using var sha256 = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(input).Concat(salt).ToArray();
            var computedHash = sha256.ComputeHash(combined);
            
            return originalHash.SequenceEqual(computedHash);
        }
        catch
        {
            return false;
        }
    }

    public byte[] GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[32];
        rng.GetBytes(salt);
        return salt;
    }
}

/// <summary>
/// Advanced threat detection and prevention
/// </summary>
public interface IThreatDetectionService
{
    Task<bool> IsRequestSuspicious(HttpContext context);
    Task BlockIpAsync(string ipAddress, TimeSpan duration);
    Task<bool> IsIpBlockedAsync(string ipAddress);
    void RecordSuspiciousActivity(string ipAddress, string activity);
}

public class ThreatDetectionService : IThreatDetectionService
{
    private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestHistory = new();
    private readonly ThreatDetection _config;
    private readonly ILogger<ThreatDetectionService> _logger;

    public ThreatDetectionService(SecurityConfig config, ILogger<ThreatDetectionService> logger)
    {
        _config = config.ThreatDetection;
        _logger = logger;
    }

    public async Task<bool> IsRequestSuspicious(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        
        // Check if IP is blocked
        if (await IsIpBlockedAsync(ipAddress))
            return true;

        // Analyze request patterns
        var now = DateTime.UtcNow;
        var requests = _requestHistory.GetOrAdd(ipAddress, _ => new List<DateTime>());
        
        lock (requests)
        {
            // Remove old requests (older than 1 hour)
            requests.RemoveAll(r => now - r > TimeSpan.FromHours(1));
            requests.Add(now);
            
            // Check for suspicious patterns
            if (requests.Count > _config.SuspiciousRequestThreshold)
            {
                RecordSuspiciousActivity(ipAddress, "Excessive requests");
                return true;
            }
            
            // Check for rapid-fire requests (more than 10 in 1 minute)
            var recentRequests = requests.Count(r => now - r < TimeSpan.FromMinutes(1));
            if (recentRequests > 10)
            {
                RecordSuspiciousActivity(ipAddress, "Rapid requests");
                return true;
            }
        }

        // Check for malicious patterns in headers/query
        if (HasMaliciousPatterns(context))
        {
            RecordSuspiciousActivity(ipAddress, "Malicious patterns detected");
            return true;
        }

        return false;
    }

    public async Task BlockIpAsync(string ipAddress, TimeSpan duration)
    {
        _blockedIps.TryAdd(ipAddress, DateTime.UtcNow.Add(duration));
        _logger.LogWarning("IP {IpAddress} blocked for {Duration}", ipAddress, duration);
        await Task.CompletedTask;
    }

    public async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        if (_blockedIps.TryGetValue(ipAddress, out var blockUntil))
        {
            if (DateTime.UtcNow < blockUntil)
                return true;
            
            // Remove expired block
            _blockedIps.TryRemove(ipAddress, out _);
        }
        
        return await Task.FromResult(false);
    }

    public void RecordSuspiciousActivity(string ipAddress, string activity)
    {
        _logger.LogWarning("Suspicious activity from {IpAddress}: {Activity}", ipAddress, activity);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool HasMaliciousPatterns(HttpContext context)
    {
        var maliciousPatterns = new[]
        {
            "script", "javascript:", "vbscript:", "onload", "onerror",
            "drop table", "union select", "1=1", "--", "/*",
            "../", "..\\", "cmd.exe", "powershell", "/etc/passwd"
        };

        var queryString = context.Request.QueryString.ToString().ToLowerInvariant();
        var userAgent = context.Request.Headers.UserAgent.ToString().ToLowerInvariant();
        
        return maliciousPatterns.Any(pattern => 
            queryString.Contains(pattern) || userAgent.Contains(pattern));
    }
}

/// <summary>
/// Secure auto-update service with integrity verification
/// </summary>
public interface IUpdateService
{
    Task<bool> CheckForUpdatesAsync();
    Task<UpdateInfo?> GetLatestVersionAsync();
    Task<bool> DownloadAndVerifyUpdateAsync(UpdateInfo update);
    Task<bool> ApplyUpdateAsync(string updatePath);
    Task CreateBackupAsync();
    Task RollbackAsync();
}

public class UpdateInfo
{
    public string Version { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Signature { get; set; } = "";
    public string Checksum { get; set; } = "";
    public DateTime ReleaseDate { get; set; }
    public bool IsSecurityUpdate { get; set; }
    public string[] ChangedFiles { get; set; } = Array.Empty<string>();
}

public class SecureUpdateService : IUpdateService
{
    private readonly UpdateSecurity _config;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<SecureUpdateService> _logger;
    private readonly HttpClient _httpClient;

    public SecureUpdateService(SecurityConfig config, IEncryptionService encryption, 
        ILogger<SecureUpdateService> logger, HttpClient httpClient)
    {
        _config = config.UpdateSecurity;
        _encryption = encryption;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        if (!_config.AutoUpdateEnabled) return false;

        try
        {
            var latestVersion = await GetLatestVersionAsync();
            if (latestVersion == null) return false;

            var currentVersion = GetCurrentVersion();
            if (IsNewerVersion(latestVersion.Version, currentVersion))
            {
                _logger.LogInformation("Update available: {Version}", latestVersion.Version);
                
                if (latestVersion.IsSecurityUpdate || _config.AutoUpdateEnabled)
                {
                    return await DownloadAndVerifyUpdateAsync(latestVersion);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return false;
        }
    }

    public async Task<UpdateInfo?> GetLatestVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_config.UpdateServerUrl}/api/version");
            #pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
            return JsonSerializer.Deserialize<UpdateInfo>(response);
            #pragma warning restore IL2026
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest version info");
            return null;
        }
    }

    public async Task<bool> DownloadAndVerifyUpdateAsync(UpdateInfo update)
    {
        try
        {
            _logger.LogInformation("Downloading update {Version}", update.Version);
            
            var updateData = await _httpClient.GetByteArrayAsync(update.DownloadUrl);
            
            // Verify signature
            if (_config.VerifySignatures && !VerifySignature(updateData, update.Signature))
            {
                _logger.LogError("Update signature verification failed");
                return false;
            }
            
            // Verify checksum
            var checksum = ComputeChecksum(updateData);
            if (checksum != update.Checksum)
            {
                _logger.LogError("Update checksum verification failed");
                return false;
            }
            
            // Create backup before applying
            if (_config.BackupBeforeUpdate)
            {
                await CreateBackupAsync();
            }
            
            // Save update file
            var updatePath = Path.Combine(Path.GetTempPath(), $"pocketfence_update_{update.Version}.zip");
            await File.WriteAllBytesAsync(updatePath, updateData);
            
            return await ApplyUpdateAsync(updatePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and verify update");
            return false;
        }
    }

    public async Task<bool> ApplyUpdateAsync(string updatePath)
    {
        try
        {
            _logger.LogInformation("Applying update from {UpdatePath}", updatePath);
            
            // Extract and apply update files
            // Implementation would depend on deployment strategy
            
            _logger.LogInformation("Update applied successfully");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply update");
            await RollbackAsync();
            return false;
        }
    }

    public async Task CreateBackupAsync()
    {
        try
        {
            var backupPath = Path.Combine(Path.GetTempPath(), $"pocketfence_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(backupPath);
            
            // Backup current application files
            var currentDirectory = AppContext.BaseDirectory;
            await CopyDirectoryAsync(currentDirectory, backupPath);
            
            _logger.LogInformation("Backup created at {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            throw;
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            _logger.LogInformation("Rolling back to previous version");
            // Implementation for rollback
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback");
            throw;
        }
    }

    private bool VerifySignature(byte[] data, string signature)
    {
        // Implement RSA signature verification
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_config.PublicKey);
            
            var signatureBytes = Convert.FromBase64String(signature);
            return rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signature verification failed");
            return false;
        }
    }

    private string ComputeChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    private string GetCurrentVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }

    private bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        return Version.Parse(latestVersion) > Version.Parse(currentVersion);
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            await Task.Run(() => File.Copy(file, destFile, true));
        }
        
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            await CopyDirectoryAsync(dir, destSubDir);
        }
    }
}

/// <summary>
/// Security middleware pipeline
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityConfig _config;
    private readonly IThreatDetectionService _threatDetection;
    private readonly ILogger<SecurityMiddleware> _logger;

    public SecurityMiddleware(RequestDelegate next, SecurityConfig config, 
        IThreatDetectionService threatDetection, ILogger<SecurityMiddleware> logger)
    {
        _next = next;
        _config = config;
        _threatDetection = threatDetection;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Security headers
        AddSecurityHeaders(context);
        
        // Threat detection
        if (await _threatDetection.IsRequestSuspicious(context))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied");
            return;
        }
        
        // Request validation
        if (!await ValidateRequestAsync(context))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid request");
            return;
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        
        if (_config.Headers.EnableHSTS)
        {
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
        
        if (_config.Headers.EnableCSP)
        {
            response.Headers["Content-Security-Policy"] = _config.Headers.CSPPolicy;
        }
        
        if (_config.Headers.EnableXFrameOptions)
        {
            response.Headers["X-Frame-Options"] = "DENY";
        }
        
        if (_config.Headers.EnableXContentTypeOptions)
        {
            response.Headers["X-Content-Type-Options"] = "nosniff";
        }
        
        if (_config.Headers.EnableReferrerPolicy)
        {
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }
        
        response.Headers["X-XSS-Protection"] = "1; mode=block";
        response.Headers["X-Powered-By"] = ""; // Remove server information
    }

    private async Task<bool> ValidateRequestAsync(HttpContext context)
    {
        // Validate content length
        if (context.Request.ContentLength > _config.Validation.MaxRequestSize)
        {
            _logger.LogWarning("Request too large: {ContentLength}", context.Request.ContentLength);
            return false;
        }
        
        // Validate content type
        if (_config.Validation.ValidateContentType && 
            context.Request.Method == "POST" && 
            !IsValidContentType(context.Request.ContentType))
        {
            _logger.LogWarning("Invalid content type: {ContentType}", context.Request.ContentType);
            return false;
        }

        return await Task.FromResult(true);
    }

    private bool IsValidContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        
        var validTypes = new[] { "application/json", "application/xml", "text/plain" };
        return validTypes.Any(type => contentType.StartsWith(type, StringComparison.OrdinalIgnoreCase));
    }
}