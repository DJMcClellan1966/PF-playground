using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PocketFence_AI.Kernel;
using PocketFence_AI.Kernel.Extensions;
using PocketFence_AI.Kernel.Security;
using System.Diagnostics.CodeAnalysis;

namespace PocketFence_AI;

// DTO for kernel info endpoint
public class KernelInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Status { get; set; } = "";
    public string Mode { get; set; } = "";
    public string[] Endpoints { get; set; } = Array.Empty<string>();
}

/// <summary>
/// PocketFence AI - Kernel Mode with CLI and API support
/// Optimized for local inference and application ecosystem integration
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Check if running in kernel mode
        bool kernelMode = args.Contains("--kernel") || args.Contains("--api");
        bool serviceMode = args.Contains("--service");
        bool cliMode = !kernelMode;

        if (kernelMode)
        {
            await RunKernelMode(args, serviceMode);
        }
        else
        {
            await RunCliMode(args);
        }
    }

    private static async Task RunKernelMode(string[] args, bool serviceMode)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Add enhanced kernel features
        builder.Services.AddKernelEnhancements(builder.Configuration);
        
        // Add enterprise security features
        builder.Services.AddKernelSecurity(builder.Configuration);
        
        // Add file integrity monitoring and security audit
        builder.Services.AddSingleton<IFileIntegrityService, FileIntegrityService>();
        builder.Services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        
        // Configure kernel services
        builder.Services.AddSingleton<SimpleAI>();
        builder.Services.AddSingleton<ContentFilter>();
        builder.Services.AddSingleton<KernelConfiguration>();
        builder.Services.AddSingleton<KernelStatistics>();
        builder.Services.AddHostedService<PocketFenceKernel>();

        // Configure API services
        #pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
        builder.Services.AddControllers();
        #pragma warning restore IL2026
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "PocketFence Kernel API",
                Version = "v1.0",
                Description = "Content filtering kernel for application ecosystem"
            });
        });
        
        // Configure JSON options for proper serialization
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });
        
        // Also configure for MVC
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Configure for Windows/Linux service hosting
        if (serviceMode)
        {
            builder.Services.AddWindowsService();
            builder.Services.AddSystemd();
        }

        var app = builder.Build();
        // Get configuration for middleware setup
        var kernelConfig = app.Services.GetRequiredService<KernelConfig>();
        
        // Use enhanced kernel features
        app.UseKernelEnhancements(kernelConfig);
        
        // Enable enterprise security features
        app.UseKernelSecurity();
        
        // Initialize file integrity monitoring
        await SecurityInitializer.InitializeFileMonitoringAsync(app.Services);
        // Configure middleware pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PocketFence Kernel API v1.0");
                c.RoutePrefix = "";
            });
        }

        app.UseCors();
        app.UseRouting();
        app.MapControllers();

        // Add kernel info endpoint
        #pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
        app.MapGet("/", () => "PocketFence Kernel v1.0.0-enhanced - Running");
        #pragma warning restore IL2026

        Console.WriteLine("🚀 PocketFence Kernel - API Mode");
        Console.WriteLine("Enhanced with caching, rate limiting, and advanced metrics");
        Console.WriteLine();
        Console.WriteLine($"🌐 API Endpoint: http://localhost:{builder.Configuration["ASPNETCORE_URLS"]?.Split(';')[0]?.Split(':').LastOrDefault() ?? "5000"}");
        Console.WriteLine($"📚 API Documentation: http://localhost:{builder.Configuration["ASPNETCORE_URLS"]?.Split(';')[0]?.Split(':').LastOrDefault() ?? "5000"}/swagger");
        Console.WriteLine();
        Console.WriteLine("Available API Endpoints:");
        Console.WriteLine("  POST /api/filter/url      - Check URL safety");
        Console.WriteLine("  POST /api/filter/content  - Analyze text content");
        Console.WriteLine("  POST /api/filter/batch    - Batch analysis");
        Console.WriteLine("  GET  /api/filter/stats    - Get filtering statistics");
        Console.WriteLine("  GET  /api/kernel/health   - Kernel health check");
        Console.WriteLine("  GET  /api/kernel/metrics  - Detailed performance metrics");
        Console.WriteLine("  GET  /api/kernel/status   - System status");
        Console.WriteLine("  GET  /api/kernel/plugins  - List loaded plugins");
        Console.WriteLine();
        Console.WriteLine("Enhanced Features:");
        Console.WriteLine("  ✅ In-memory caching with TTL");
        Console.WriteLine("  ✅ Rate limiting by IP address");
        Console.WriteLine("  ✅ Detailed performance metrics");
        Console.WriteLine("  ✅ API key authentication (configurable)");
        Console.WriteLine("  ✅ Advanced health monitoring");
        Console.WriteLine();
        Console.WriteLine("💡 Example usage:");
        Console.WriteLine("  curl -X POST http://localhost:5000/api/filter/url \\");
        Console.WriteLine("    -H \"Content-Type: application/json\" \\");
        Console.WriteLine("    -d '{\"url\":\"https://example.com\"}'");
        Console.WriteLine();
        Console.WriteLine("Press Ctrl+C to stop the kernel");

        await app.RunAsync();
    }

    private static async Task RunCliMode(string[] args)
    {
        var _ai = new SimpleAI();
        var _filter = new ContentFilter();

        Console.WriteLine("🤖 PocketFence AI - CLI Mode");
        Console.WriteLine("Local content filter with kernel capabilities");
        Console.WriteLine();
        Console.WriteLine("💡 Pro Tip: Use '--kernel' for API mode or '--service' for service mode");
        Console.WriteLine();
        
        try
        {
            // Initialize AI model
            Console.WriteLine("Initializing AI model...");
            await _ai.InitializeAsync();
            
            // Load content filters
            Console.WriteLine("Loading content filters...");
            await _filter.LoadFiltersAsync();
            
            Console.WriteLine("✅ Ready! Type 'help' for commands or 'exit' to quit.");
            Console.WriteLine();
            
            // Start interactive CLI
            await RunInteractiveModeAsync(_ai, _filter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return;
        }
    }
    
    private static async Task RunInteractiveModeAsync(SimpleAI ai, ContentFilter filter)
    {
        while (true)
        {
            Console.Write("pocketfence> ");
            var input = Console.ReadLine()?.Trim() ?? "";
            
            switch (input.ToLower())
            {
                case "help":
                    ShowHelp();
                    break;
                case "kernel":
                    ShowKernelInfo();
                    break;
                case "api":
                    Console.WriteLine("💡 Restart with '--kernel' argument to enable API mode");
                    Console.WriteLine("   Example: dotnet run -- --kernel");
                    break;
                case "exit":
                case "quit":
                    Console.WriteLine("👋 Goodbye!");
                    return;
                case var cmd when cmd.StartsWith("check "):
                    await CheckContentAsync(input.Substring(6), ai, filter);
                    break;
                case var cmd when cmd.StartsWith("analyze "):
                    await AnalyzeContentAsync(input.Substring(8), ai);
                    break;
                case "stats":
                    ShowStats(filter, ai);
                    break;
                case "childmode on":
                    ai.EnableChildMode(true);
                    Console.WriteLine("🛡️ Child protection mode ENABLED");
                    break;
                case "childmode off":
                    ai.EnableChildMode(false);
                    Console.WriteLine("🔓 Child protection mode DISABLED");
                    break;
                case "childstats":
                    ShowChildStats(ai);
                    break;
                case "clear":
                    Console.Clear();
                    break;
                default:
                    Console.WriteLine("❓ Unknown command. Type 'help' for available commands.");
                    break;
            }
        }
    }
    
    private static void ShowHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  check <url>      - Check if URL should be blocked");
        Console.WriteLine("  analyze <text>   - Analyze text content for safety");
        Console.WriteLine("  stats            - Show filtering statistics");
        Console.WriteLine("  childmode on/off - Enable/disable child protection mode");
        Console.WriteLine("  childstats       - Show child protection statistics");
        Console.WriteLine("  kernel           - Show kernel mode information");
        Console.WriteLine("  api              - Show API mode information");
        Console.WriteLine("  clear            - Clear screen");
        Console.WriteLine("  help             - Show this help");
        Console.WriteLine("  exit             - Exit program");
        Console.WriteLine();
        Console.WriteLine("🚀 Kernel Mode Features:");
        Console.WriteLine("  • REST API for application integration");
        Console.WriteLine("  • Plugin system for extensibility");
        Console.WriteLine("  • Batch processing capabilities");
        Console.WriteLine("  • Service/daemon mode support");
        Console.WriteLine("  • Enhanced monitoring and statistics");
        Console.WriteLine();
        Console.WriteLine("🛡️ Child Protection Features:");
        Console.WriteLine("  • Blocks inappropriate content automatically");
        Console.WriteLine("  • Monitors for cyberbullying keywords");
        Console.WriteLine("  • Filters adult/violent content");
        Console.WriteLine("  • Detects stranger danger situations");
    }

    private static void ShowKernelInfo()
    {
        Console.WriteLine("🚀 PocketFence Kernel Information:");
        Console.WriteLine();
        Console.WriteLine("🔧 Kernel Features:");
        Console.WriteLine("  • REST API endpoints for app integration");
        Console.WriteLine("  • Plugin system for custom filtering logic");
        Console.WriteLine("  • Batch processing for high-volume requests");
        Console.WriteLine("  • Real-time statistics and health monitoring");
        Console.WriteLine("  • Cross-platform service deployment");
        Console.WriteLine();
        Console.WriteLine("🌐 API Endpoints (when running in kernel mode):");
        Console.WriteLine("  POST /api/filter/url      - URL safety checking");
        Console.WriteLine("  POST /api/filter/content  - Content analysis");
        Console.WriteLine("  POST /api/filter/batch    - Batch analysis");
        Console.WriteLine("  GET  /api/filter/stats    - Statistics");
        Console.WriteLine("  GET  /api/kernel/health   - Health check");
        Console.WriteLine("  GET  /api/kernel/plugins  - Plugin management");
        Console.WriteLine();
        Console.WriteLine("🚀 To start kernel mode:");
        Console.WriteLine("  dotnet run -- --kernel    (API mode)");
        Console.WriteLine("  dotnet run -- --service   (Service mode)");
        Console.WriteLine();
        Console.WriteLine("💡 Applications can connect to the kernel via HTTP API");
        Console.WriteLine("   for centralized content filtering across your ecosystem");
    }
    
    private static async Task CheckContentAsync(string url, SimpleAI ai, ContentFilter filter)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("❌ Please provide a URL to check");
            return;
        }
        
        var result = await filter.CheckUrlAsync(url);
        var aiScore = await ai.AnalyzeThreatLevelAsync(url);
        
        Console.WriteLine($"🔍 Analysis for: {url}");
        Console.WriteLine($"   Filter Result: {(result.IsBlocked ? "❌ BLOCKED" : "✅ ALLOWED")}");
        Console.WriteLine($"   AI Threat Score: {aiScore:F2}/1.0");
        Console.WriteLine($"   Reason: {result.Reason}");
        Console.WriteLine($"   Kernel Ready: ✅ Can be accessed via API");
        
        if (result.IsBlocked || aiScore > 0.7)
        {
            Console.WriteLine($"   ⚠️  Recommendation: {(result.IsBlocked ? "Block" : "Monitor")}");
        }
    }
    
    private static async Task AnalyzeContentAsync(string content, SimpleAI ai)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            Console.WriteLine("❌ Please provide content to analyze");
            return;
        }
        
        var analysis = await ai.AnalyzeContentAsync(content);
        
        Console.WriteLine("🧠 AI Content Analysis:");
        Console.WriteLine($"   Safety Score: {analysis.SafetyScore:F2}/1.0");
        Console.WriteLine($"   Category: {analysis.Category}");
        Console.WriteLine($"   Confidence: {analysis.Confidence:F2}");
        Console.WriteLine($"   Recommendation: {analysis.Recommendation}");
        Console.WriteLine($"   Kernel Ready: ✅ Can be accessed via API");
        
        if (analysis.Flags.Any())
        {
            Console.WriteLine($"   ⚠️  Flags: {string.Join(", ", analysis.Flags)}");
        }
    }
    
    private static void ShowStats(ContentFilter filter, SimpleAI ai)
    {
        var stats = filter.GetStatistics();
        
        Console.WriteLine("📊 Filtering Statistics:");
        Console.WriteLine($"   Total Requests: {stats.TotalRequests}");
        Console.WriteLine($"   Blocked: {stats.BlockedRequests} ({stats.BlockRate:P1})");
        Console.WriteLine($"   Allowed: {stats.AllowedRequests} ({stats.AllowRate:P1})");
        Console.WriteLine($"   AI Processed: {ai.GetProcessedCount()}");
        Console.WriteLine($"   Memory Usage: {GC.GetTotalMemory(false) / (1024 * 1024):F1} MB");
        Console.WriteLine($"   Kernel Mode: 🚀 Available via --kernel flag");
    }

    private static void ShowChildStats(SimpleAI ai)
    {
        var childStats = ai.GetChildProtectionStats();
        
        Console.WriteLine("🛡️ Child Protection Statistics:");
        Console.WriteLine($"   Mode: {(childStats.IsEnabled ? "✅ ENABLED" : "❌ DISABLED")}");
        Console.WriteLine($"   Content Blocked: {childStats.ContentBlocked}");
        Console.WriteLine($"   Violence Detected: {childStats.ViolenceDetected}");
        Console.WriteLine($"   Adult Content Blocked: {childStats.AdultContentBlocked}");
        Console.WriteLine($"   Cyberbullying Detected: {childStats.CyberBullyingDetected}");
        Console.WriteLine($"   Stranger Danger Alerts: {childStats.StrangerDangerAlerts}");
        Console.WriteLine($"   Safe Score Average: {childStats.SafeScoreAverage:F2}");
        Console.WriteLine($"   Kernel API: 🚀 Enhanced tracking available in kernel mode");
    }
}

// Lightweight AI engine optimized for local inference with child protection
public class SimpleAI
{
    private readonly Dictionary<string, double> _threatKeywords;
    private readonly Dictionary<string, double> _safePatterns;
    private readonly Dictionary<string, double> _childUnsafeKeywords;
    private int _processedCount = 0;
    private bool _childModeEnabled = true; // Default to child protection on
    private ChildProtectionStats _childStats = new ChildProtectionStats();
    
    public SimpleAI()
    {
        _threatKeywords = new Dictionary<string, double>
        {
            // High-risk keywords
            { "malware", 0.9 }, { "virus", 0.9 }, { "phishing", 0.95 },
            { "adult", 0.8 }, { "gambling", 0.7 }, { "violence", 0.85 },
            { "drugs", 0.8 }, { "weapons", 0.85 }, { "hate", 0.9 },
            
            // Medium-risk keywords
            { "download", 0.4 }, { "free", 0.3 }, { "click", 0.3 },
            { "urgent", 0.5 }, { "limited", 0.4 }, { "offer", 0.3 }
        };
        
        _childUnsafeKeywords = new Dictionary<string, double>
        {
            // Violence & Weapons
            { "violence", 0.95 }, { "fighting", 0.8 }, { "weapons", 0.9 },
            { "gun", 0.9 }, { "knife", 0.8 }, { "blood", 0.85 }, { "gore", 0.9 },
            { "killing", 0.95 }, { "murder", 0.95 }, { "death", 0.7 },
            
            // Adult Content
            { "adult", 0.9 }, { "mature", 0.8 }, { "explicit", 0.95 },
            { "sexual", 0.95 }, { "dating", 0.6 }, { "romance", 0.4 },
            { "intimate", 0.8 }, { "naked", 0.9 }, { "nude", 0.9 },
            
            // Stranger Danger
            { "meet me", 0.95 }, { "personal info", 0.8 }, { "address", 0.7 },
            { "phone number", 0.8 }, { "secret", 0.6 }, { "don't tell", 0.9 },
            { "parents", 0.5 }, { "alone", 0.6 }, { "private", 0.5 },
            
            // Cyberbullying
            { "stupid", 0.7 }, { "ugly", 0.7 }, { "hate you", 0.9 },
            { "kill yourself", 0.99 }, { "worthless", 0.8 }, { "loser", 0.7 },
            { "fat", 0.6 }, { "dumb", 0.6 }, { "nobody likes", 0.8 },
            
            // Drugs & Substances
            { "alcohol", 0.8 }, { "beer", 0.7 }, { "wine", 0.6 },
            { "smoking", 0.8 }, { "vaping", 0.8 }, { "cigarettes", 0.8 },
            { "marijuana", 0.9 }, { "weed", 0.8 }, { "drugs", 0.9 },
            { "pills", 0.7 }, { "medicine", 0.3 },
            
            // Gambling
            { "gambling", 0.9 }, { "casino", 0.8 }, { "betting", 0.8 },
            { "lottery", 0.6 }, { "poker", 0.7 }, { "slots", 0.8 }
        };
        
        _safePatterns = new Dictionary<string, double>
        {
            { "education", -0.3 }, { "learning", -0.3 }, { "school", -0.3 },
            { "tutorial", -0.2 }, { "help", -0.2 }, { "support", -0.2 },
            { "documentation", -0.3 }, { "official", -0.3 }
        };
    }
    
    public Task InitializeAsync()
    {
        // Simulate model loading - in real implementation this would load ML models
        Thread.Sleep(100);
        return Task.CompletedTask;
    }
    
    public Task<double> AnalyzeThreatLevelAsync(string content)
    {
        _processedCount++;
        
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(0.0);
            
        var contentLower = content.ToLowerInvariant();
        double score = 0.0;
        int matches = 0;
        
        // Check threat keywords
        foreach (var keyword in _threatKeywords)
        {
            if (contentLower.Contains(keyword.Key))
            {
                score += keyword.Value;
                matches++;
            }
        }
        
        // Check safe patterns
        foreach (var pattern in _safePatterns)
        {
            if (contentLower.Contains(pattern.Key))
            {
                score += pattern.Value;
                matches++;
            }
        }
        
        // Normalize score
        if (matches > 0)
            score = Math.Max(0.0, Math.Min(1.0, score / matches));
            
        return Task.FromResult(score);
    }
    
    public async Task<ContentAnalysis> AnalyzeContentAsync(string content)
    {
        var threatLevel = await AnalyzeThreatLevelAsync(content);
        var childUnsafeScore = AnalyzeChildSafety(content);
        var flags = new List<string>();
        var category = "General";
        
        // Determine category and flags based on content
        var contentLower = content.ToLowerInvariant();
        
        // Child safety checks (higher priority when child mode is enabled)
        if (_childModeEnabled && childUnsafeScore > 0.6)
        {
            if (contentLower.Contains("violence") || contentLower.Contains("fighting") || contentLower.Contains("weapons"))
            {
                category = "Violence";
                flags.Add("Child Unsafe - Violence");
            }
            else if (contentLower.Contains("adult") || contentLower.Contains("explicit") || contentLower.Contains("sexual"))
            {
                category = "Adult Content";
                flags.Add("Child Unsafe - Adult Content");
            }
            else if (contentLower.Contains("meet") || contentLower.Contains("secret") || contentLower.Contains("address"))
            {
                category = "Stranger Danger";
                flags.Add("Child Unsafe - Stranger Danger");
            }
            else if (contentLower.Contains("stupid") || contentLower.Contains("hate") || contentLower.Contains("ugly"))
            {
                category = "Cyberbullying";
                flags.Add("Child Unsafe - Cyberbullying");
            }
        }
        else if (contentLower.Contains("adult") || contentLower.Contains("explicit"))
        {
            category = "Adult Content";
            flags.Add("NSFW");
        }
        else if (contentLower.Contains("violence") || contentLower.Contains("weapon"))
        {
            category = "Violence";
            flags.Add("Violent Content");
        }
        else if (contentLower.Contains("malware") || contentLower.Contains("virus"))
        {
            category = "Security Threat";
            flags.Add("Malicious");
        }
        
        // Combine threat and child safety scores
        var finalThreatLevel = _childModeEnabled ? Math.Max(threatLevel, childUnsafeScore) : threatLevel;
        
        var recommendation = finalThreatLevel > 0.7 ? "BLOCK" : 
                           finalThreatLevel > 0.4 ? "MONITOR" : "ALLOW";
        
        // Add child mode indicator
        if (_childModeEnabled && childUnsafeScore > 0.3)
        {
            flags.Add("🛡️ Child Protection Active");
        }
        
        return new ContentAnalysis
        {
            SafetyScore = 1.0 - finalThreatLevel,
            Category = category,
            Confidence = Math.Min(0.95, finalThreatLevel + 0.3),
            Recommendation = recommendation,
            Flags = flags
        };
    }

    public void EnableChildMode(bool enabled)
    {
        _childModeEnabled = enabled;
        _childStats.IsEnabled = enabled;
    }

    public ChildProtectionStats GetChildProtectionStats()
    {
        return _childStats;
    }

    private double AnalyzeChildSafety(string content)
    {
        if (!_childModeEnabled) return 0.0;

        var contentLower = content.ToLowerInvariant();
        double unsafeScore = 0.0;
        int matches = 0;

        foreach (var keyword in _childUnsafeKeywords)
        {
            if (contentLower.Contains(keyword.Key))
            {
                unsafeScore += keyword.Value;
                matches++;

                // Update specific stats based on keyword type
                if (keyword.Key.Contains("violence") || keyword.Key.Contains("fighting") || keyword.Key.Contains("weapons"))
                    _childStats.ViolenceDetected++;
                else if (keyword.Key.Contains("adult") || keyword.Key.Contains("explicit") || keyword.Key.Contains("sexual"))
                    _childStats.AdultContentBlocked++;
                else if (keyword.Key.Contains("stupid") || keyword.Key.Contains("hate") || keyword.Key.Contains("kill yourself"))
                    _childStats.CyberBullyingDetected++;
                else if (keyword.Key.Contains("meet") || keyword.Key.Contains("secret") || keyword.Key.Contains("don't tell"))
                    _childStats.StrangerDangerAlerts++;
            }
        }

        if (matches > 0)
        {
            unsafeScore = Math.Max(0.0, Math.Min(1.0, unsafeScore / matches));
            if (unsafeScore > 0.6) _childStats.ContentBlocked++;
        }

        _childStats.SafeScoreAverage = (_childStats.SafeScoreAverage + (1.0 - unsafeScore)) / 2.0;
        return unsafeScore;
    }
    
    public int GetProcessedCount() => _processedCount;
}

// Child protection statistics
public class ChildProtectionStats
{
    public bool IsEnabled { get; set; } = true;
    public int ContentBlocked { get; set; } = 0;
    public int ViolenceDetected { get; set; } = 0;
    public int AdultContentBlocked { get; set; } = 0;
    public int CyberBullyingDetected { get; set; } = 0;
    public int StrangerDangerAlerts { get; set; } = 0;
    public double SafeScoreAverage { get; set; } = 1.0;
}

// Lightweight content filter
public class ContentFilter
{
    private readonly HashSet<string> _blockedDomains;
    private readonly List<string> _blockedKeywords;
    private int _totalRequests = 0;
    private int _blockedRequests = 0;
    
    public ContentFilter()
    {
        _blockedDomains = new HashSet<string>
        {
            // Security threats
            "malicious.com", "phishing.net", "illegal-downloads.net",
            
            // Adult content sites
            "pornhub.com", "xvideos.com", "xnxx.com", "redtube.com",
            "youporn.com", "tube8.com", "spankbang.com", "xhamster.com",
            "porn.com", "sex.com", "xxx.com", "adult.com",
            "chaturbate.com", "cam4.com", "myfreecams.com", "livejasmin.com",
            "onlyfans.com", "playboy.com", "penthouse.com",
            
            // Gambling sites
            "gambling.org", "casino.com", "bet365.com", "888casino.com",
            "pokerstars.com", "bwin.com", "betway.com",
            
            // Dating sites (child protection)
            "tinder.com", "bumble.com", "match.com", "okcupid.com",
            "pof.com", "adultfriendfinder.com", "ashley-madison.com"
        };
        
        _blockedKeywords = new List<string>
        {
            // Adult content keywords
            "porn", "xxx", "sex", "adult", "nude", "naked", "explicit",
            "erotic", "cam", "live", "chat", "dating", "hookup",
            
            // Gambling keywords  
            "gambling", "casino", "bet", "poker", "slots", "lottery",
            
            // Violence/weapons
            "weapons", "violence", "gore", "blood",
            
            // Substances
            "drugs", "weed", "marijuana", "cocaine",
            
            // Security threats
            "malware", "phishing", "virus", "hack",
            
            // Illegal content
            "illegal", "torrent", "pirate", "crack", "keygen"
        };
    }
    
    public Task LoadFiltersAsync()
    {
        // Simulate loading filters - in real implementation load from file/database
        Thread.Sleep(50);
        return Task.CompletedTask;
    }
    
    public Task<FilterResult> CheckUrlAsync(string url)
    {
        _totalRequests++;
        
        var urlLower = url.ToLowerInvariant();
        var domain = ExtractDomain(url);
        
        // Check blocked domains
        if (_blockedDomains.Contains(domain))
        {
            _blockedRequests++;
            return Task.FromResult(new FilterResult
            {
                IsBlocked = true,
                Reason = $"Domain '{domain}' is in blocklist"
            });
        }
        
        // Check blocked keywords
        foreach (var keyword in _blockedKeywords)
        {
            if (urlLower.Contains(keyword))
            {
                _blockedRequests++;
                return Task.FromResult(new FilterResult
                {
                    IsBlocked = true,
                    Reason = $"Contains blocked keyword '{keyword}'"
                });
            }
        }
        
        return Task.FromResult(new FilterResult
        {
            IsBlocked = false,
            Reason = "No blocking rules matched"
        });
    }
    
    private string ExtractDomain(string url)
    {
        try
        {
            if (!url.StartsWith("http"))
                url = "http://" + url;
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();
            
            // Remove www. prefix for consistent matching
            if (host.StartsWith("www."))
                host = host.Substring(4);
                
            return host;
        }
        catch
        {
            var domain = url.Split('/')[0].ToLowerInvariant();
            if (domain.StartsWith("www."))
                domain = domain.Substring(4);
            return domain;
        }
    }
    
    public FilterStatistics GetStatistics()
    {
        return new FilterStatistics
        {
            TotalRequests = _totalRequests,
            BlockedRequests = _blockedRequests,
            AllowedRequests = _totalRequests - _blockedRequests,
            BlockRate = _totalRequests > 0 ? (double)_blockedRequests / _totalRequests : 0,
            AllowRate = _totalRequests > 0 ? (double)(_totalRequests - _blockedRequests) / _totalRequests : 0
        };
    }
}

// Simple data models
public class ContentAnalysis
{
    public double SafetyScore { get; set; }
    public string Category { get; set; } = "";
    public double Confidence { get; set; }
    public string Recommendation { get; set; } = "";
    public List<string> Flags { get; set; } = new();
}

public class FilterResult
{
    public bool IsBlocked { get; set; }
    public string Reason { get; set; } = "";
}

public class FilterStatistics
{
    public int TotalRequests { get; set; }
    public int BlockedRequests { get; set; }
    public int AllowedRequests { get; set; }
    public double BlockRate { get; set; }
    public double AllowRate { get; set; }
}

public static class SecurityInitializer
{
    /// <summary>
    /// Initialize file integrity monitoring for critical kernel files
    /// </summary>
    public static async Task InitializeFileMonitoringAsync(IServiceProvider services)
    {
        try
        {
            var fileIntegrityService = services.GetRequiredService<IFileIntegrityService>();
            var auditService = services.GetRequiredService<ISecurityAuditService>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            // Monitor critical kernel files
            var criticalFiles = new[]
            {
                "PocketFence.Kernel.cs",
                "Security.cs",
                "Security.Extensions.cs",
                "Kernel.Extensions.cs",
                "appsettings.json"
            }.Select(f => Path.Combine(AppContext.BaseDirectory, f))
             .Where(File.Exists)
             .ToArray();

            if (criticalFiles.Length > 0)
            {
                await fileIntegrityService.StartMonitoringAsync(criticalFiles);
                logger.LogInformation("File integrity monitoring started for {FileCount} files", criticalFiles.Length);

                // Subscribe to file change events
                fileIntegrityService.FileChanged += async (sender, e) =>
                {
                    var securityEvent = new SecurityEvent
                    {
                        EventType = "FileIntegrityViolation",
                        Source = "FileIntegrityService",
                        Description = $"File modified: {e.FilePath}",
                        Level = SecurityLevel.Critical,
                        Properties = new Dictionary<string, object>
                        {
                            ["FilePath"] = e.FilePath,
                            ["OldHash"] = e.OldHash,
                            ["NewHash"] = e.NewHash
                        }
                    };
                    
                    await auditService.LogSecurityEventAsync(securityEvent);
                    logger.LogCritical("SECURITY ALERT: Critical file modified - {FilePath}", e.FilePath);
                };
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Failed to initialize file monitoring");
        }
    }
}