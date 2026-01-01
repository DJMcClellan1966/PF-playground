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
            Console.WriteLine("🖥️ FamilyOS Windows Platform Demo");
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
                
                logger.LogInformation("🏠 Testing FamilyOS on {Platform} v{Version}", 
                    windowsPlatform.PlatformName, windowsPlatform.PlatformVersion);
                    
                logger.LogInformation("🔐 Administrator Privileges: {IsAdmin}", 
                    windowsPlatform.IsAdministrator);
                
                // Initialize platform
                var initialized = await windowsPlatform.InitializePlatformAsync();
                if (!initialized)
                {
                    logger.LogWarning("⚠️ Platform initialization failed - some features may be limited");
                    return;
                }
                
                logger.LogInformation("✅ Windows platform initialized successfully!");
                
                // Get platform capabilities
                var capabilities = await windowsPlatform.GetPlatformCapabilitiesAsync();
                logger.LogInformation("\n🔧 Windows Platform Capabilities:");
                logger.LogInformation("  ✅ Parental Controls: {Supports}", capabilities.SupportsParentalControls);
                logger.LogInformation("  ✅ Content Filtering: {Supports}", capabilities.SupportsContentFiltering);
                logger.LogInformation("  ✅ Network Monitoring: {Supports}", capabilities.SupportsNetworkMonitoring);
                logger.LogInformation("  ✅ Process Control: {Supports}", capabilities.SupportsProcessControl);
                logger.LogInformation("  ✅ Screen Time: {Supports}", capabilities.SupportsScreenTimeTracking);
                logger.LogInformation("  🥷 Stealth Mode: {Supports}", capabilities.SupportsStealthMode);
                logger.LogInformation("  🔒 Security Level: {Level}", capabilities.SecurityLevel);
                logger.LogInformation("  👨‍👩‍👧‍👦 Max Family Members: {Max}", capabilities.MaxFamilyMembers);
                
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
                
                logger.LogInformation("\n👧 Created test family member: {Name} (Age: {Age})", 
                    testChild.DisplayName, testChild.AgeGroup);
                
                // Test getting running processes (basic functionality)
                logger.LogInformation("🔍 Testing process enumeration...");
                var processes = await windowsPlatform.GetRunningProcessesAsync(testChild);
                logger.LogInformation("📊 Found {Count} running processes", processes.Count);
                
                // Show a few sample processes
                logger.LogInformation("📋 Sample processes:");
                foreach (var process in processes.Take(3))
                {
                    logger.LogInformation("  • {Name} (PID: {Id}) - {Memory:N0} bytes", 
                        process.ProcessName, process.ProcessId, process.WorkingSet);
                }
                
                // Test screen time functionality
                logger.LogInformation("\n⏱️ Testing screen time tracking...");
                var screenTime = await windowsPlatform.GetScreenTimeAsync(testChild, DateTime.Today);
                logger.LogInformation("📱 Total screen time today: {Time}", screenTime.TotalScreenTime);
                
                if (screenTime.ApplicationUsage.Any())
                {
                    logger.LogInformation("📊 Application usage:");
                    foreach (var app in screenTime.ApplicationUsage.Take(3))
                    {
                        logger.LogInformation("  • {App}: {Time}", app.Key, app.Value);
                    }
                }
                
                logger.LogInformation("\n🎉 Windows platform demo completed successfully!");
                logger.LogInformation("💡 FamilyOS is ready to protect families on Windows with enterprise-grade security!");
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error during Windows platform demo");
            }
        }
    }
}