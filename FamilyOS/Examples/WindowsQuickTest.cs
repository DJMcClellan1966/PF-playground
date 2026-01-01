using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PocketFence.FamilyOS.Core;
using PocketFence.FamilyOS.Platform.Windows;

namespace PocketFence.FamilyOS.Examples
{
    /// <summary>
    /// Simple test to demonstrate Windows platform functionality
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowsQuickTest
    {
        [SupportedOSPlatform("windows")]
        public static async Task RunWindowsDemo()
        {
            var ascii = PocketFence.FamilyOS.UI.FamilyOSUI.UseAscii;
            Console.WriteLine(ascii ? "FamilyOS Windows Platform Demo" : "🖥️ FamilyOS Windows Platform Demo");
            Console.WriteLine("=================================\n");

            // Set up dependency injection
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("WindowsQuickTest");
            
            try
            {
                // Create Windows platform service
                using var windowsPlatform = new WindowsPlatformService(
                    serviceProvider.GetRequiredService<ILogger<WindowsPlatformService>>()
                );
                
                logger.LogInformation(ascii ? "Testing FamilyOS on {Platform} v{Version}" : "🏠 Testing FamilyOS on {Platform} v{Version}", 
                    windowsPlatform.PlatformName, windowsPlatform.PlatformVersion);
                    
                logger.LogInformation(ascii ? "Administrator Privileges: {IsAdmin}" : "🔐 Administrator Privileges: {IsAdmin}", 
                    windowsPlatform.IsAdministrator);
                
                // Initialize platform
                var initialized = await windowsPlatform.InitializePlatformAsync();
                if (!initialized)
                {
                    logger.LogWarning(ascii ? "WARNING: Platform initialization failed - some features may be limited" : "⚠️ Platform initialization failed - some features may be limited");
                    return;
                }
                
                logger.LogInformation(ascii ? "OK: Windows platform initialized successfully!" : "✅ Windows platform initialized successfully!");
                
                // Get platform capabilities
                var capabilities = await windowsPlatform.GetPlatformCapabilitiesAsync();
                logger.LogInformation(ascii ? "\n[CONFIG] Windows Platform Capabilities:" : "\n🔧 Windows Platform Capabilities:");
                logger.LogInformation(ascii ? "  [OK] Parental Controls: {Supports}" : "  ✅ Parental Controls: {Supports}", capabilities.SupportsParentalControls);
                logger.LogInformation(ascii ? "  [OK] Content Filtering: {Supports}" : "  ✅ Content Filtering: {Supports}", capabilities.SupportsContentFiltering);
                logger.LogInformation(ascii ? "  [OK] Network Monitoring: {Supports}" : "  ✅ Network Monitoring: {Supports}", capabilities.SupportsNetworkMonitoring);
                logger.LogInformation(ascii ? "  [OK] Process Control: {Supports}" : "  ✅ Process Control: {Supports}", capabilities.SupportsProcessControl);
                logger.LogInformation(ascii ? "  [OK] Screen Time: {Supports}" : "  ✅ Screen Time: {Supports}", capabilities.SupportsScreenTimeTracking);
                logger.LogInformation(ascii ? "  [STEALTH] Stealth Mode: {Supports}" : "  🥷 Stealth Mode: {Supports}", capabilities.SupportsStealthMode);
                logger.LogInformation(ascii ? "  [AUTH] Security Level: {Level}" : "  🔒 Security Level: {Level}", capabilities.SecurityLevel);
                logger.LogInformation(ascii ? "  [FAMILY] Max Family Members: {Max}" : "  👨‍👩‍👧‍👦 Max Family Members: {Max}", capabilities.MaxFamilyMembers);
                
                // Create a test family member
                var testChild = new FamilyMember
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "test_child",
                    DisplayName = "Test Child",
                    AgeGroup = AgeGroup.Elementary,
                    Role = FamilyRole.Child,
                    IsOnline = true
                };
                
                logger.LogInformation(ascii ? "\n[CHILD] Created test family member: {Name} (Age: {Age})" : "\n👧 Created test family member: {Name} (Age: {Age})", 
                    testChild.DisplayName, testChild.AgeGroup);
                
                // Test getting running processes (basic functionality)
                logger.LogInformation(ascii ? "[SEARCH] Testing process enumeration..." : "🔍 Testing process enumeration...");
                var processes = await windowsPlatform.GetRunningProcessesAsync(testChild);
                logger.LogInformation(ascii ? "[STATS] Found {Count} running processes" : "📊 Found {Count} running processes", processes.Count);
                
                // Show a few sample processes
                logger.LogInformation(ascii ? "[LIST] Sample processes:" : "📋 Sample processes:");
                foreach (var process in processes.Take(3))
                {
                    logger.LogInformation("  • {Name} (PID: {Id}) - {Memory:N0} bytes", 
                        process.ProcessName, process.ProcessId, process.WorkingSet);
                }
                
                // Test screen time functionality
                logger.LogInformation(ascii ? "\n[TIME] Testing screen time tracking..." : "\n⏱️ Testing screen time tracking...");
                var screenTime = await windowsPlatform.GetScreenTimeAsync(testChild, DateTime.Today);
                logger.LogInformation(ascii ? "[APPS] Total screen time today: {Time}" : "📱 Total screen time today: {Time}", screenTime.TotalScreenTime);
                
                if (screenTime.ApplicationUsage.Any())
                {
                    logger.LogInformation(ascii ? "[STATS] Application usage:" : "📊 Application usage:");
                    foreach (var app in screenTime.ApplicationUsage.Take(3))
                    {
                        logger.LogInformation("  • {App}: {Time}", app.Key, app.Value);
                    }
                }
                
                logger.LogInformation(ascii ? "\nDONE: Windows platform demo completed successfully!" : "\n🎉 Windows platform demo completed successfully!");
                logger.LogInformation(ascii ? "TIP: FamilyOS is ready to protect families on Windows with enterprise-grade security!" : "💡 FamilyOS is ready to protect families on Windows with enterprise-grade security!");
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ascii ? "ERR: Error during Windows platform demo" : "❌ Error during Windows platform demo");
            }
        }
    }
}