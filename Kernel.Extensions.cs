using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;

namespace PocketFence_AI.Kernel.Extensions;

/// <summary>
/// Thread-safe HashSet implementation for O(1) operations
/// </summary>
public class ConcurrentHashSet<T> : IDisposable where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public bool Add(T item) => _dictionary.TryAdd(item, 0);
    public bool Remove(T item) => _dictionary.TryRemove(item, out _);
    public bool Contains(T item) => _dictionary.ContainsKey(item);
    public int Count => _dictionary.Count;
    public void Clear() => _dictionary.Clear();
    
    public IEnumerable<T> ToEnumerable()
    {
        _lock.EnterReadLock();
        try
        {
            return _dictionary.Keys.ToArray(); // Snapshot to avoid enumeration issues
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public void Dispose()
    {
        _lock?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Enhanced configuration management for the kernel
/// </summary>
public class KernelConfig
{
    public CacheSettings Cache { get; set; } = new();
    public RateLimitSettings RateLimit { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public MonitoringSettings Monitoring { get; set; } = new();
    public PluginSettings Plugins { get; set; } = new();
}

public class CacheSettings
{
    public bool Enabled { get; set; } = true;
    public int DefaultTtlMinutes { get; set; } = 30;
    public int MaxCacheSize { get; set; } = 1000;
    public bool CompactOnMemoryPressure { get; set; } = true;
}

public class RateLimitSettings
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 100;
    public int BurstLimit { get; set; } = 20;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

public class SecuritySettings
{
    public bool RequireApiKey { get; set; } = false;
    public string[] ValidApiKeys { get; set; } = Array.Empty<string>();
    public bool EnableCors { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = { "*" };
}

public class MonitoringSettings
{
    public bool DetailedMetrics { get; set; } = true;
    public bool LogRequests { get; set; } = true;
    public int MetricsRetentionDays { get; set; } = 7;
    public bool HealthChecks { get; set; } = true;
}

public class PluginSettings
{
    public bool HotReload { get; set; } = false;
    public string PluginDirectory { get; set; } = "plugins";
    public int ReloadIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Simple rate limiter for the kernel
/// </summary>
public class SimpleRateLimiter
{
    private readonly ConcurrentDictionary<string, ClientRateInfo> _clients = new();
    private readonly RateLimitSettings _settings;

    public SimpleRateLimiter(RateLimitSettings settings)
    {
        _settings = settings;
    }

    public bool IsAllowed(string clientId)
    {
        var now = DateTime.UtcNow;
        var client = _clients.GetOrAdd(clientId, _ => new ClientRateInfo());

        lock (client)
        {
            // Reset if window has passed
            if (now - client.WindowStart > _settings.Window)
            {
                client.RequestCount = 0;
                client.WindowStart = now;
            }

            if (client.RequestCount >= _settings.RequestsPerMinute)
            {
                return false;
            }

            client.RequestCount++;
            client.LastRequest = now;
            return true;
        }
    }
}

public class ClientRateInfo
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; } = DateTime.UtcNow;
    public DateTime LastRequest { get; set; }
}

/// <summary>
/// Rate limiting middleware
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SimpleRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, SimpleRateLimiter rateLimiter, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        if (!_rateLimiter.IsAllowed(clientId))
        {
            _logger.LogWarning("Rate limit exceeded for {ClientId}", clientId);
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }
}
public interface IKernelCache
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task ClearAsync();
    CacheStatistics GetStatistics();
}

public class KernelMemoryCache : IKernelCache
{
    private readonly IMemoryCache _cache;
    private readonly CacheSettings _settings;
    private readonly CacheStatistics _stats = new();
    private readonly ConcurrentHashSet<string> _keyTracker = new(); // O(1) key tracking

    public KernelMemoryCache(IMemoryCache cache, KernelConfig config)
    {
        _cache = cache;
        _settings = config.Cache;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            Interlocked.Increment(ref _stats.Hits);
            return Task.FromResult(value);
        }
        
        Interlocked.Increment(ref _stats.Misses);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(_settings.DefaultTtlMinutes),
            Priority = CacheItemPriority.Normal,
            Size = 1 // Enable size-based eviction
        };

        if (_settings.CompactOnMemoryPressure)
        {
            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _keyTracker.Remove((string)k!);
                if (reason == EvictionReason.Capacity || reason == EvictionReason.TokenExpired)
                {
                    Interlocked.Increment(ref _stats.Evictions);
                }
            });
        }

        _cache.Set(key, value, options);
        _keyTracker.Add(key);
        Interlocked.Increment(ref _stats.Sets);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _keyTracker.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        // Efficient bulk removal using key tracking
        var keys = _keyTracker.ToEnumerable().ToArray();
        
        Parallel.ForEach(keys, key =>
        {
            _cache.Remove(key);
            _keyTracker.Remove(key);
        });
        
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0); // Final cleanup
        }
        
        return Task.CompletedTask;
    }

    public CacheStatistics GetStatistics() => _stats;
}

public class CacheStatistics
{
    public long Hits;
    public long Misses;
    public long Sets;
    public long Evictions;
    
    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
}

/// <summary>
/// API Key authentication middleware
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecuritySettings _security;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, KernelConfig config, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _security = config.Security;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_security.RequireApiKey)
        {
            await _next(context);
            return;
        }

        // Skip authentication for health checks and documentation
        if (context.Request.Path.StartsWithSegments("/health") || 
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        const string apiKeyHeader = "X-API-Key";
        
        if (!context.Request.Headers.TryGetValue(apiKeyHeader, out var extractedApiKey))
        {
            _logger.LogWarning("API key missing for {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API key is required");
            return;
        }

        if (!_security.ValidApiKeys.Any(key => key == extractedApiKey))
        {
            _logger.LogWarning("Invalid API key for {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Enhanced metrics tracking
/// </summary>
public class DetailedMetrics
{
    public long TotalRequests;
    public long SuccessfulRequests;
    public long FailedRequests;
    public long AverageResponseTime; // milliseconds
    public long TotalResponseTime; // For accurate average calculation
    public DateTime StartTime;
    public ConcurrentDictionary<string, EndpointMetrics> EndpointMetrics = new();
    
    public DetailedMetrics()
    {
        StartTime = DateTime.UtcNow;
    }
}

public class EndpointMetrics
{
    public long RequestCount;
    public long TotalResponseTime;
    public long ErrorCount;
    public DateTime LastRequest;
}

/// <summary>
/// Request metrics middleware
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DetailedMetrics _metrics;
    private readonly MonitoringSettings _settings;
    private readonly ILogger<MetricsMiddleware> _logger;

    public MetricsMiddleware(RequestDelegate next, DetailedMetrics metrics, KernelConfig config, ILogger<MetricsMiddleware> logger)
    {
        _next = next;
        _metrics = metrics;
        _settings = config.Monitoring;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_settings.DetailedMetrics)
        {
            await _next(context);
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var endpoint = $"{context.Request.Method} {context.Request.Path}";

        try
        {
            await _next(context);
            
            stopwatch.Stop();
            RecordMetrics(endpoint, stopwatch.ElapsedMilliseconds, context.Response.StatusCode >= 200 && context.Response.StatusCode < 400);
            
            if (_settings.LogRequests)
            {
                _logger.LogInformation("Request {Endpoint} completed in {Duration}ms with status {StatusCode}", 
                    endpoint, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetrics(endpoint, stopwatch.ElapsedMilliseconds, false);
            _logger.LogError(ex, "Request {Endpoint} failed after {Duration}ms", endpoint, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void RecordMetrics(string endpoint, long duration, bool success)
    {
        var requestCount = Interlocked.Increment(ref _metrics.TotalRequests);
        
        if (success)
            Interlocked.Increment(ref _metrics.SuccessfulRequests);
        else
            Interlocked.Increment(ref _metrics.FailedRequests);

        // Update total response time and calculate average efficiently
        var totalTime = Interlocked.Add(ref _metrics.TotalResponseTime, duration);
        var newAverage = totalTime / requestCount;
        Interlocked.Exchange(ref _metrics.AverageResponseTime, newAverage);

        // O(1) endpoint metrics update using ConcurrentDictionary
        var endpointMetrics = _metrics.EndpointMetrics.GetOrAdd(endpoint, _ => new EndpointMetrics());
        
        Interlocked.Increment(ref endpointMetrics.RequestCount);
        Interlocked.Add(ref endpointMetrics.TotalResponseTime, duration);
        
        if (!success)
            Interlocked.Increment(ref endpointMetrics.ErrorCount);
            
        endpointMetrics.LastRequest = DateTime.UtcNow;
    }
}

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class KernelExtensions
{
    public static IServiceCollection AddKernelEnhancements(this IServiceCollection services, IConfiguration configuration)
    {
        // Register enhanced configuration
        var config = configuration.GetSection("Kernel").Get<KernelConfig>() ?? new KernelConfig();
        services.AddSingleton(config);
        
        // Register caching
        if (config.Cache.Enabled)
        {
            services.AddMemoryCache();
            services.AddSingleton<IKernelCache, KernelMemoryCache>();
        }
        
        // Register metrics
        if (config.Monitoring.DetailedMetrics)
        {
            services.AddSingleton<DetailedMetrics>();
        }
        
        // Register rate limiting
        if (config.RateLimit.Enabled)
        {
            services.AddSingleton(new SimpleRateLimiter(config.RateLimit));
        }

        return services;
    }

    public static IApplicationBuilder UseKernelEnhancements(this IApplicationBuilder app, KernelConfig config)
    {
        // Use rate limiting
        if (config.RateLimit.Enabled)
        {
            app.UseMiddleware<RateLimitMiddleware>();
        }
        
        // Use API key authentication
        if (config.Security.RequireApiKey)
        {
            app.UseMiddleware<ApiKeyMiddleware>();
        }
        
        // Use metrics tracking
        if (config.Monitoring.DetailedMetrics)
        {
            app.UseMiddleware<MetricsMiddleware>();
        }

        return app;
    }
}