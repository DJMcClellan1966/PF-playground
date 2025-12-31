using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PocketFence_AI.Kernel.Security;
using System.Text;

namespace PocketFence_AI.Kernel;

/// <summary>
/// Security extensions for kernel configuration
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Add enterprise security services to the kernel
    /// </summary>
    public static IServiceCollection AddKernelSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Load security configuration
        var securityConfig = new SecurityConfig();
        configuration.GetSection("Security").Bind(securityConfig);
        services.AddSingleton(securityConfig);

        // Core security services
        services.AddSingleton<IEncryptionService, AESEncryptionService>();
        services.AddSingleton<IThreatDetectionService, ThreatDetectionService>();
        services.AddHttpClient<IUpdateService, SecureUpdateService>();

        // JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = securityConfig.Jwt.Issuer,
                    ValidAudience = securityConfig.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(securityConfig.Jwt.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
                
                options.RequireHttpsMetadata = securityConfig.Jwt.RequireHttpsMetadata;
            });

        // Authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("KernelAccess", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "kernel"));

            options.AddPolicy("AdminAccess", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "admin"));
        });

        // Security headers and CORS
        services.AddCors(options =>
        {
            options.AddPolicy("SecureCors", builder =>
                builder.WithOrigins("https://localhost", "https://pocketfence.ai")
                       .WithMethods("GET", "POST")
                       .WithHeaders("Authorization", "Content-Type")
                       .WithExposedHeaders("X-Request-ID")
                       .SetPreflightMaxAge(TimeSpan.FromMinutes(5))
                       .AllowCredentials());
        });

        // Auto-update background service
        services.AddHostedService<UpdateBackgroundService>();

        return services;
    }

    /// <summary>
    /// Configure security middleware pipeline
    /// </summary>
    public static IApplicationBuilder UseKernelSecurity(this IApplicationBuilder app)
    {
        // Security middleware (order matters!)
        app.UseHttpsRedirection();
        app.UseCors("SecureCors");
        app.UseAuthentication();
        app.UseMiddleware<SecurityMiddleware>();
        app.UseAuthorization();

        return app;
    }
}

/// <summary>
/// Background service for automatic security updates
/// </summary>
public class UpdateBackgroundService : BackgroundService
{
    private readonly IUpdateService _updateService;
    private readonly SecurityConfig _config;
    private readonly ILogger<UpdateBackgroundService> _logger;
    private readonly PeriodicTimer _timer;

    public UpdateBackgroundService(IUpdateService updateService, SecurityConfig config,
        ILogger<UpdateBackgroundService> logger)
    {
        _updateService = updateService;
        _config = config;
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(_config.UpdateSecurity.CheckIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.UpdateSecurity.AutoUpdateEnabled)
        {
            _logger.LogInformation("Auto-updates disabled");
            return;
        }

        _logger.LogInformation("Update service started, checking every {Interval} minutes", 
            _config.UpdateSecurity.CheckIntervalMinutes);

        try
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogDebug("Checking for updates...");
                    
                    var updateAvailable = await _updateService.CheckForUpdatesAsync();
                    if (updateAvailable)
                    {
                        _logger.LogInformation("Security update applied successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during update check");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update service stopping...");
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// File integrity monitoring service
/// </summary>
public interface IFileIntegrityService
{
    Task<string> ComputeFileHashAsync(string filePath);
    Task<bool> VerifyFileIntegrityAsync(string filePath, string expectedHash);
    Task StartMonitoringAsync(string[] filePaths);
    Task StopMonitoringAsync();
    event EventHandler<FileChangedEventArgs> FileChanged;
}

public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; }
    public string OldHash { get; }
    public string NewHash { get; }
    public DateTime Timestamp { get; }

    public FileChangedEventArgs(string filePath, string oldHash, string newHash)
    {
        FilePath = filePath;
        OldHash = oldHash;
        NewHash = newHash;
        Timestamp = DateTime.UtcNow;
    }
}

public class FileIntegrityService : IFileIntegrityService, IDisposable
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private readonly Dictionary<string, string> _fileHashes = new();
    private readonly ILogger<FileIntegrityService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public event EventHandler<FileChangedEventArgs>? FileChanged;

    public FileIntegrityService(ILogger<FileIntegrityService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ComputeFileHashAsync(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute hash for file {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> VerifyFileIntegrityAsync(string filePath, string expectedHash)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return false;
            }

            var currentHash = await ComputeFileHashAsync(filePath);
            var isValid = currentHash.Equals(expectedHash, StringComparison.Ordinal);
            
            if (!isValid)
            {
                _logger.LogWarning("File integrity check failed for {FilePath}. Expected: {Expected}, Actual: {Actual}",
                    filePath, expectedHash, currentHash);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying file integrity for {FilePath}", filePath);
            return false;
        }
    }

    public async Task StartMonitoringAsync(string[] filePaths)
    {
        await _semaphore.WaitAsync();
        try
        {
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Cannot monitor non-existent file: {FilePath}", filePath);
                    continue;
                }

                // Compute initial hash
                var initialHash = await ComputeFileHashAsync(filePath);
                _fileHashes[filePath] = initialHash;

                // Set up file watcher
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);
                
                if (directory != null && !_watchers.ContainsKey(filePath))
                {
                    var watcher = new FileSystemWatcher(directory, fileName)
                    {
                        EnableRaisingEvents = true,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                    };

                    watcher.Changed += async (sender, e) => await OnFileChangedAsync(e.FullPath);
                    _watchers[filePath] = watcher;
                    
                    _logger.LogInformation("Started monitoring file: {FilePath}", filePath);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopMonitoringAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            
            _watchers.Clear();
            _fileHashes.Clear();
            
            _logger.LogInformation("Stopped file monitoring");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task OnFileChangedAsync(string filePath)
    {
        try
        {
            // Small delay to ensure file write is complete
            await Task.Delay(500);

            if (_fileHashes.TryGetValue(filePath, out var oldHash))
            {
                var newHash = await ComputeFileHashAsync(filePath);
                if (!oldHash.Equals(newHash, StringComparison.Ordinal))
                {
                    _fileHashes[filePath] = newHash;
                    
                    var args = new FileChangedEventArgs(filePath, oldHash, newHash);
                    FileChanged?.Invoke(this, args);
                    
                    _logger.LogWarning("File integrity violation detected: {FilePath}", filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file change for {FilePath}", filePath);
        }
    }

    public void Dispose()
    {
        StopMonitoringAsync().GetAwaiter().GetResult();
        _semaphore?.Dispose();
    }
}

/// <summary>
/// Security audit logging service
/// </summary>
public interface ISecurityAuditService
{
    Task LogSecurityEventAsync(SecurityEvent securityEvent);
    Task<SecurityReport> GenerateSecurityReportAsync(DateTime from, DateTime to);
}

public class SecurityEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = "";
    public string Source { get; set; } = "";
    public string Description { get; set; } = "";
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public SecurityLevel Level { get; set; } = SecurityLevel.Info;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public enum SecurityLevel
{
    Info,
    Warning,
    Critical
}

public class SecurityReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int WarningEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public string[] TopThreats { get; set; } = Array.Empty<string>();
}

public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly List<SecurityEvent> _events = new();
    private readonly object _eventsLock = new();

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        lock (_eventsLock)
        {
            _events.Add(securityEvent);
            
            // Keep only last 10,000 events
            if (_events.Count > 10000)
            {
                _events.RemoveRange(0, _events.Count - 10000);
            }
        }

        var logLevel = securityEvent.Level switch
        {
            SecurityLevel.Critical => LogLevel.Critical,
            SecurityLevel.Warning => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, "Security Event: {EventType} - {Description} (IP: {IpAddress}, User: {UserId})",
            securityEvent.EventType, securityEvent.Description, securityEvent.IpAddress, securityEvent.UserId);

        await Task.CompletedTask;
    }

    public async Task<SecurityReport> GenerateSecurityReportAsync(DateTime from, DateTime to)
    {
        SecurityEvent[] events;
        lock (_eventsLock)
        {
            events = _events.Where(e => e.Timestamp >= from && e.Timestamp <= to).ToArray();
        }

        var report = new SecurityReport
        {
            TotalEvents = events.Length,
            CriticalEvents = events.Count(e => e.Level == SecurityLevel.Critical),
            WarningEvents = events.Count(e => e.Level == SecurityLevel.Warning),
            EventsByType = events.GroupBy(e => e.EventType)
                                 .ToDictionary(g => g.Key, g => g.Count()),
            TopThreats = events.Where(e => e.Level == SecurityLevel.Critical)
                              .GroupBy(e => e.Description)
                              .OrderByDescending(g => g.Count())
                              .Take(5)
                              .Select(g => g.Key)
                              .ToArray()
        };

        return await Task.FromResult(report);
    }
}