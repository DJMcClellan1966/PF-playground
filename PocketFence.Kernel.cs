using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace PocketFence_AI.Kernel;

/// <summary>
/// PocketFence Kernel - Core content filtering service for application ecosystem
/// </summary>
public class PocketFenceKernel : BackgroundService
{
    private readonly ILogger<PocketFenceKernel> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly KernelConfiguration _config;
    private readonly ConcurrentDictionary<string, IKernelPlugin> _plugins;
    private readonly KernelStatistics _statistics;

    public PocketFenceKernel(
        ILogger<PocketFenceKernel> logger,
        IServiceProvider serviceProvider,
        KernelConfiguration config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config;
        _plugins = new ConcurrentDictionary<string, IKernelPlugin>();
        _statistics = new KernelStatistics();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 PocketFence Kernel starting...");
        
        // Initialize core services
        await InitializeCoreServices();
        
        // Load plugins
        await LoadPlugins();
        
        // Start monitoring
        await StartMonitoring(stoppingToken);
        
        _logger.LogInformation("✅ PocketFence Kernel is ready and running");
        
        // Keep running until cancellation
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task InitializeCoreServices()
    {
        _logger.LogInformation("Initializing core filtering services...");
        
        var ai = _serviceProvider.GetRequiredService<SimpleAI>();
        var filter = _serviceProvider.GetRequiredService<ContentFilter>();
        
        await ai.InitializeAsync();
        await filter.LoadFiltersAsync();
        
        _statistics.StartTime = DateTime.UtcNow;
        _logger.LogInformation("Core services initialized successfully");
    }

    private async Task LoadPlugins()
    {
        _logger.LogInformation("Loading kernel plugins...");
        
        var pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        
        if (Directory.Exists(pluginDirectory))
        {
            var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll");
            
            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    await LoadPlugin(pluginFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin: {PluginFile}", pluginFile);
                }
            }
        }
        
        _logger.LogInformation("Loaded {PluginCount} plugins", _plugins.Count);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access", Justification = "Plugin loading requires reflection by design")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'", Justification = "Plugin loading requires reflection by design")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'", Justification = "Plugin loading requires reflection by design")]
    private async Task LoadPlugin(string pluginFile)
    {
        try
        {
            var assembly = Assembly.LoadFrom(pluginFile);
            var allTypes = assembly.GetTypes();
            
            // O(n) single pass instead of O(n²) with Contains check
            var pluginInterface = typeof(IKernelPlugin);
            var pluginTypes = new List<Type>(capacity: 4); // Pre-allocate reasonable capacity
            
            foreach (var type in allTypes)
            {
                if (pluginInterface.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    pluginTypes.Add(type);
                }
            }

            // Parallel plugin initialization for better performance
            var initializationTasks = pluginTypes.Select(async ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type pluginType) =>
            {
                try
                {
                    var plugin = (IKernelPlugin)Activator.CreateInstance(pluginType)!;
                    await plugin.InitializeAsync();
                    return (plugin.Name, plugin);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize plugin type: {PluginType}", pluginType.Name);
                    return ((string?)null, (IKernelPlugin?)null);
                }
            });
            
            var results = await Task.WhenAll(initializationTasks);
            
            foreach (var (name, plugin) in results)
            {
                if (!string.IsNullOrEmpty(name) && plugin != null)
                {
                    _plugins.TryAdd(name, plugin);
                    _logger.LogInformation("Loaded plugin: {PluginName}", name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from: {PluginFile}", pluginFile);
        }
    }

    private async Task StartMonitoring(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await LogSystemHealth();
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task LogSystemHealth()
    {
        var memoryUsed = GC.GetTotalMemory(false) / (1024 * 1024); // MB
        var uptime = DateTime.UtcNow - _statistics.StartTime;
        
        _logger.LogInformation(
            "🛡️ Kernel Health: Memory: {MemoryMB}MB, Uptime: {Uptime}, " +
            "Requests: {Requests}, Plugins: {Plugins}",
            memoryUsed, uptime, _statistics.TotalRequests, _plugins.Count);
    }

    public IReadOnlyDictionary<string, IKernelPlugin> GetLoadedPlugins() => _plugins;
    public KernelStatistics GetStatistics() => _statistics;
}

/// <summary>
/// Plugin interface for extending kernel functionality
/// </summary>
public interface IKernelPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    Task InitializeAsync();
    Task<PluginResponse> ProcessAsync(PluginRequest request);
    Task ShutdownAsync();
}

/// <summary>
/// Plugin request structure with optimized metadata handling
/// </summary>
public class PluginRequest
{
    public string Content { get; set; } = "";
    public string ContentType { get; set; } = "text/plain";
    public string Source { get; set; } = "";
    
    private readonly Dictionary<string, object> _metadata = new(capacity: 8, StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();
    
    /// <summary>
    /// O(1) metadata operations with case-insensitive keys
    /// </summary>
    public void SetMetadata(string key, object value) => _metadata[key] = value;
    public T? GetMetadata<T>(string key) => _metadata.TryGetValue(key, out var value) && value is T result ? result : default;
    public bool HasMetadata(string key) => _metadata.ContainsKey(key);
}

/// <summary>
/// Plugin response structure with optimized collections
/// </summary>
public class PluginResponse
{
    public bool IsBlocked { get; set; }
    public double ThreatScore { get; set; }
    public string Reason { get; set; } = "";
    
    private readonly List<string> _tags = new(capacity: 4); // Pre-allocate common size
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    
    private readonly Dictionary<string, object> _data = new(capacity: 8); // Pre-allocate
    public IReadOnlyDictionary<string, object> Data => _data.AsReadOnly();
    
    /// <summary>
    /// O(1) amortized tag addition
    /// </summary>
    public void AddTag(string tag)
    {
        if (!_tags.Contains(tag)) // O(n) but typically small n
            _tags.Add(tag);
    }
    
    /// <summary>
    /// O(1) data setting
    /// </summary>
    public void SetData(string key, object value) => _data[key] = value;
    
    /// <summary>
    /// O(1) data retrieval
    /// </summary>
    public T? GetData<T>(string key) => _data.TryGetValue(key, out var value) && value is T result ? result : default;
}

/// <summary>
/// Kernel configuration with optimized collection management
/// </summary>
public class KernelConfiguration
{
    public bool EnableApi { get; set; } = true;
    public int ApiPort { get; set; } = 5000;
    public bool EnablePlugins { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public bool RunAsService { get; set; } = false;
    
    private readonly List<string> _allowedOrigins = new(capacity: 8) { "*" };
    public IReadOnlyList<string> AllowedOrigins => _allowedOrigins.AsReadOnly();
    
    private readonly ConcurrentDictionary<string, object> _pluginSettings = new();
    public ConcurrentDictionary<string, object> PluginSettings => _pluginSettings;
    
    /// <summary>
    /// O(1) thread-safe plugin setting operations
    /// </summary>
    public void SetPluginSetting(string key, object value) => _pluginSettings.TryAdd(key, value);
    public T? GetPluginSetting<T>(string key) => _pluginSettings.TryGetValue(key, out var value) && value is T result ? result : default;
}

/// <summary>
/// Kernel runtime statistics with optimized Big O performance
/// </summary>
public class KernelStatistics
{
    public DateTime StartTime { get; set; }
    public long TotalRequests;  // O(1) atomic operations
    public long BlockedRequests;  // O(1) atomic operations
    public long AllowedRequests;  // O(1) atomic operations
    public long TotalResponseTime; // For accurate average calculation
    public double AverageResponseTime { get; set; }
    public long MemoryUsage => GC.GetTotalMemory(false);
    public TimeSpan Uptime => DateTime.UtcNow - StartTime;
    public ConcurrentDictionary<string, long> PluginUsage { get; set; } = new(); // O(1) thread-safe operations
    
    /// <summary>
    /// O(1) plugin usage increment
    /// </summary>
    public void IncrementPluginUsage(string pluginName)
    {
        PluginUsage.AddOrUpdate(pluginName, 1, (key, value) => value + 1);
    }
    
    /// <summary>
    /// O(1) response time tracking with accurate averaging
    /// </summary>
    public void RecordResponseTime(long milliseconds)
    {
        var requestCount = Interlocked.Read(ref TotalRequests);
        var totalTime = Interlocked.Add(ref TotalResponseTime, milliseconds);
        AverageResponseTime = requestCount > 0 ? (double)totalTime / requestCount : 0;
    }
}

/// <summary>
/// API Controllers for kernel endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilterController : ControllerBase
{
    private readonly SimpleAI _ai;
    private readonly ContentFilter _filter;
    private readonly ILogger<FilterController> _logger;
    private readonly KernelStatistics _statistics;

    public FilterController(SimpleAI ai, ContentFilter filter, ILogger<FilterController> logger, KernelStatistics statistics)
    {
        _ai = ai;
        _filter = filter;
        _logger = logger;
        _statistics = statistics;
    }

    [HttpPost("url")]
    public async Task<ActionResult<UrlCheckResult>> CheckUrl([FromBody] UrlCheckRequest request)
    {
        try
        {
            var totalRequests = Interlocked.Increment(ref _statistics.TotalRequests);
            
            var filterResult = await _filter.CheckUrlAsync(request.Url);
            var threatScore = await _ai.AnalyzeThreatLevelAsync(request.Url);
            
            var result = new UrlCheckResult
            {
                Url = request.Url,
                IsBlocked = filterResult.IsBlocked,
                ThreatScore = threatScore,
                Reason = filterResult.Reason,
                Recommendation = threatScore > 0.7 ? "BLOCK" : 
                               threatScore > 0.4 ? "MONITOR" : "ALLOW",
                RequestId = request.RequestId,
                Timestamp = DateTime.UtcNow
            };

            if (result.IsBlocked)
                Interlocked.Increment(ref _statistics.BlockedRequests);
            else
                Interlocked.Increment(ref _statistics.AllowedRequests);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking URL: {Url}", request.Url);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("content")]
    public async Task<ActionResult<ContentAnalysisResult>> AnalyzeContent([FromBody] ContentAnalysisRequest request)
    {
        try
        {
            var totalRequests = Interlocked.Increment(ref _statistics.TotalRequests);
            
            var analysis = await _ai.AnalyzeContentAsync(request.Content);
            
            var result = new ContentAnalysisResult
            {
                Content = request.Content,
                SafetyScore = analysis.SafetyScore,
                Category = analysis.Category,
                Confidence = analysis.Confidence,
                Recommendation = analysis.Recommendation,
                Flags = analysis.Flags.ToList(),
                IsChildSafe = analysis.SafetyScore > 0.6,
                RequestId = request.RequestId,
                Timestamp = DateTime.UtcNow
            };

            if (!result.IsChildSafe)
                Interlocked.Increment(ref _statistics.BlockedRequests);
            else
                Interlocked.Increment(ref _statistics.AllowedRequests);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("stats")]
    public ActionResult<KernelStatistics> GetStatistics()
    {
        return Ok(_statistics);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchAnalysisResult>> BatchAnalysis([FromBody] BatchAnalysisRequest request)
    {
        try
        {
            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest(new { error = "No items provided for analysis" });
            }

            // Optimize: Parallel processing instead of sequential O(n) operations
            // Partition items by type for better parallelization
            var urlItems = new List<BatchAnalysisItem>();
            var contentItems = new List<BatchAnalysisItem>();
            
            foreach (var item in request.Items)
            {
                if (item.Type == "url")
                    urlItems.Add(item);
                else if (item.Type == "content")
                    contentItems.Add(item);
            }

            // Process URL and content items in parallel batches
            var urlTask = ProcessUrlBatchAsync(urlItems);
            var contentTask = ProcessContentBatchAsync(contentItems);
            
            await Task.WhenAll(urlTask, contentTask);
            
            var results = new List<AnalysisResultItem>(urlTask.Result.Count + contentTask.Result.Count);
            results.AddRange(urlTask.Result);
            results.AddRange(contentTask.Result);

            // O(1) atomic counter update
            Interlocked.Add(ref _statistics.TotalRequests, request.Items.Count);
            
            return Ok(new BatchAnalysisResult
            {
                RequestId = request.RequestId,
                Results = results,
                ProcessedCount = results.Count,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch analysis");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// O(n/p) parallel URL processing where p = parallelism degree
    /// </summary>
    private async Task<List<AnalysisResultItem>> ProcessUrlBatchAsync(IEnumerable<BatchAnalysisItem> urlItems)
    {
        if (!urlItems.Any()) return new List<AnalysisResultItem>();

        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2); // Limit concurrency
        var tasks = urlItems.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                var urlResult = await _filter.CheckUrlAsync(item.Content);
                var threatScore = await _ai.AnalyzeThreatLevelAsync(item.Content);
                
                return new AnalysisResultItem
                {
                    Id = item.Id,
                    Type = "url",
                    IsBlocked = urlResult.IsBlocked,
                    ThreatScore = threatScore,
                    Reason = urlResult.Reason
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        semaphore.Dispose();
        return results.ToList();
    }

    /// <summary>
    /// O(n/p) parallel content processing where p = parallelism degree
    /// </summary>
    private async Task<List<AnalysisResultItem>> ProcessContentBatchAsync(IEnumerable<BatchAnalysisItem> contentItems)
    {
        if (!contentItems.Any()) return new List<AnalysisResultItem>();

        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2); // Limit concurrency
        var tasks = contentItems.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                var analysis = await _ai.AnalyzeContentAsync(item.Content);
                
                return new AnalysisResultItem
                {
                    Id = item.Id,
                    Type = "content",
                    SafetyScore = analysis.SafetyScore,
                    Category = analysis.Category,
                    Recommendation = analysis.Recommendation
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        semaphore.Dispose();
        return results.ToList();
    }
}

[ApiController]
[Route("api/[controller]")]
public class KernelController : ControllerBase
{
    private readonly PocketFenceKernel _kernel;
    private readonly ILogger<KernelController> _logger;

    public KernelController(PocketFenceKernel kernel, ILogger<KernelController> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    [HttpGet("health")]
    public ActionResult<object> GetHealth()
    {
        var stats = _kernel.GetStatistics();
        return Ok(new
        {
            status = "healthy",
            uptime = stats.Uptime,
            memoryUsage = stats.MemoryUsage / (1024 * 1024), // MB
            totalRequests = stats.TotalRequests,
            version = "1.0.0-enhanced"
        });
    }

    [HttpGet("plugins")]
    public ActionResult<object> GetPlugins()
    {
        var plugins = _kernel.GetLoadedPlugins();
        return Ok(new
        {
            count = plugins.Count,
            plugins = plugins.Values.Select(p => new
            {
                name = p.Name,
                version = p.Version,
                description = p.Description
            })
        });
    }

    [HttpGet("metrics")]
    public ActionResult<object> GetMetrics()
    {
        var stats = _kernel.GetStatistics();
        var memoryUsage = GC.GetTotalMemory(false);
        
        return Ok(new
        {
            performance = new
            {
                totalRequests = stats.TotalRequests,
                blockedRequests = stats.BlockedRequests,
                allowedRequests = stats.AllowedRequests,
                uptime = stats.Uptime,
                requestsPerSecond = stats.TotalRequests / Math.Max(stats.Uptime.TotalSeconds, 1)
            },
            system = new
            {
                memoryUsage = memoryUsage / (1024 * 1024), // MB
                gcCollections = new
                {
                    gen0 = GC.CollectionCount(0),
                    gen1 = GC.CollectionCount(1),
                    gen2 = GC.CollectionCount(2)
                },
                processorCount = Environment.ProcessorCount,
                osVersion = Environment.OSVersion.ToString()
            },
            plugins = new
            {
                loaded = _kernel.GetLoadedPlugins().Count,
                available = _kernel.GetLoadedPlugins().Count
            }
        });
    }

    [HttpGet("status")]
    public ActionResult<object> GetDetailedStatus()
    {
        var stats = _kernel.GetStatistics();
        var plugins = _kernel.GetLoadedPlugins();
        
        return Ok(new
        {
            kernel = new
            {
                version = "1.0.0-enhanced",
                status = "running",
                startTime = stats.StartTime,
                uptime = stats.Uptime
            },
            performance = stats,
            plugins = plugins.Values.Select(p => new
            {
                name = p.Name,
                version = p.Version,
                description = p.Description,
                status = "loaded"
            }),
            features = new
            {
                caching = "available",
                rateLimiting = "available", 
                authentication = "available",
                metrics = "enhanced"
            }
        });
    }
}

// API Request/Response Models
public class UrlCheckRequest
{
    public string Url { get; set; } = "";
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Source { get; set; } = "";
}

public class ContentAnalysisRequest
{
    public string Content { get; set; } = "";
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Source { get; set; } = "";
}

public class BatchAnalysisRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public List<BatchAnalysisItem> Items { get; set; } = new();
}

public class BatchAnalysisItem
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = ""; // "url" or "content"
    public string Content { get; set; } = "";
}

public class BatchAnalysisResult
{
    public string RequestId { get; set; } = "";
    public List<AnalysisResultItem> Results { get; set; } = new();
    public int ProcessedCount { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AnalysisResultItem
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    
    // For URL analysis
    public bool? IsBlocked { get; set; }
    public double? ThreatScore { get; set; }
    public string? Reason { get; set; }
    
    // For content analysis
    public double? SafetyScore { get; set; }
    public string? Category { get; set; }
    public string? Recommendation { get; set; }
}

// Enhanced result models with kernel-specific fields
public class UrlCheckResult
{
    public string Url { get; set; } = "";
    public bool IsBlocked { get; set; }
    public double ThreatScore { get; set; }
    public string Reason { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string RequestId { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class ContentAnalysisResult
{
    public string Content { get; set; } = "";
    public double SafetyScore { get; set; }
    public string Category { get; set; } = "";
    public double Confidence { get; set; }
    public string Recommendation { get; set; } = "";
    public List<string> Flags { get; set; } = new();
    public bool IsChildSafe { get; set; }
    public string RequestId { get; set; } = "";
    public DateTime Timestamp { get; set; }
}