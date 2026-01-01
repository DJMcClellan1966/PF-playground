using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PocketFence.FamilyOS.Core;
using PocketFence.FamilyOS.Platform.Windows;

namespace PocketFence.FamilyOS.Examples
{
    /// <summary>
    /// Demonstrates Windows-specific FamilyOS implementation
    /// Shows how to initialize and use Windows platform services
    /// </summary>
    public class WindowsPlatformExample
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🖥️ FamilyOS Windows Platform Example");
            Console.WriteLine("=====================================\n");

            // Set up dependency injection
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddSingleton<IPlatformService, WindowsPlatformService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<WindowsPlatformExample>>();
            
            try
            {
                // Initialize Windows platform service
                using var platformService = serviceProvider.GetRequiredService<IPlatformService>();
                
                logger.LogInformation("🏠 Initializing FamilyOS for {Platform}", platformService.PlatformName);
                
                // Initialize platform
                var initialized = await platformService.InitializePlatformAsync();
                if (!initialized)
                {
                    logger.LogError("❌ Failed to initialize Windows platform");
                    return;
                }
                
                // Get platform capabilities
                var capabilities = await platformService.GetPlatformCapabilitiesAsync();
                DisplayPlatformCapabilities(capabilities, logger);
                
                // Create a sample family member
                var childMember = new FamilyMember
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "child_user",
                    DisplayName = "Emma Thompson",
                    AgeGroup = AgeGroup.Elementary,
                    Role = FamilyRole.Child,
                    IsOnline = true
                };
                
                logger.LogInformation("👧 Created family member: {DisplayName} (Age: {AgeGroup})", 
                    childMember.DisplayName, childMember.AgeGroup);
                
                // Configure parental controls
                var parentalSettings = new ParentalControlSettings
                {
                    ContentFilterLevel = ContentFilterLevel.Moderate,
                    DailyTimeLimit = TimeSpan.FromHours(3),
                    BlockedApplications = new List<string> { "games.exe", "social_media.exe" },
                    AllowedApplications = new List<string> { "education.exe", "homework_helper.exe" },
                    BlockedWebsites = new List<string> { "inappropriate-site.com", "gaming-site.com" },
                    AllowedWebsites = new List<string> { "educational-site.com", "homework-help.com" },
                    RequireApprovalForDownloads = true,
                    EnableLocationTracking = false
                };
                
                // Apply Windows parental controls
                logger.LogInformation("🛡️ Applying parental controls for {DisplayName}...", childMember.DisplayName);
                var controlsApplied = await platformService.ApplyParentalControlsAsync(childMember, parentalSettings);
                
                if (controlsApplied)
                {
                    logger.LogInformation("✅ Successfully applied parental controls");
                    DisplayParentalControls(parentalSettings, logger);
                }
                else
                {
                    logger.LogWarning("⚠️ Some parental controls may not have been applied correctly");
                }
                
                // Start network monitoring
                logger.LogInformation("🌐 Starting network monitoring for {DisplayName}...", childMember.DisplayName);
                var monitoringStarted = await platformService.MonitorNetworkActivityAsync(childMember);
                
                if (monitoringStarted)
                {
                    logger.LogInformation("✅ Network monitoring started");
                }
                
                // Get running processes
                logger.LogInformation("🔍 Checking running processes for {DisplayName}...", childMember.DisplayName);
                var processes = await platformService.GetRunningProcessesAsync(childMember);
                
                logger.LogInformation("📊 Found {ProcessCount} running processes", processes.Count);
                foreach (var process in processes.Take(5)) // Show first 5
                {
                    logger.LogInformation("  📋 {ProcessName} (PID: {ProcessId}) - {WorkingSet:N0} bytes", 
                        process.ProcessName, process.ProcessId, process.WorkingSet);
                }
                
                // Get screen time data
                logger.LogInformation("⏱️ Retrieving screen time data for {DisplayName}...", childMember.DisplayName);
                var screenTimeData = await platformService.GetScreenTimeAsync(childMember, DateTime.Today);
                
                DisplayScreenTimeData(screenTimeData, logger);
                
                // Enable stealth mode (if supported)
                if (capabilities.SupportsStealthMode)
                {
                    logger.LogInformation("🥷 Enabling stealth mode for {DisplayName}...", childMember.DisplayName);
                    var stealthEnabled = await platformService.EnableStealthModeAsync(childMember);
                    
                    if (stealthEnabled)
                    {
                        logger.LogInformation("✅ Stealth mode enabled - FamilyOS is now operating invisibly");
                    }
                    else
                    {
                        logger.LogWarning("⚠️ Could not enable stealth mode (may require administrator privileges)");
                    }
                }
                
                logger.LogInformation("\n🎉 Windows platform example completed successfully!");
                logger.LogInformation("💡 FamilyOS is now protecting {DisplayName} on Windows with comprehensive family safety features", 
                    childMember.DisplayName);
                    
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error in Windows platform example");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        private static void DisplayPlatformCapabilities(PlatformCapabilities capabilities, ILogger logger)
        {
            logger.LogInformation("\n🔧 Windows Platform Capabilities:");
            logger.LogInformation("  ✅ Parental Controls: {SupportsParental}", capabilities.SupportsParentalControls);
            logger.LogInformation("  ✅ Content Filtering: {SupportsContent}", capabilities.SupportsContentFiltering);
            logger.LogInformation("  ✅ Network Monitoring: {SupportsNetwork}", capabilities.SupportsNetworkMonitoring);
            logger.LogInformation("  ✅ Process Control: {SupportsProcess}", capabilities.SupportsProcessControl);
            logger.LogInformation("  ✅ Screen Time: {SupportsScreenTime}", capabilities.SupportsScreenTimeTracking);
            logger.LogInformation("  ✅ Stealth Mode: {SupportsStealth}", capabilities.SupportsStealthMode);
            logger.LogInformation("  ✅ Hardware Control: {SupportsHardware}", capabilities.SupportsHardwareControl);
            logger.LogInformation("  📊 Max Family Members: {MaxMembers}", capabilities.MaxFamilyMembers);
            logger.LogInformation("  🔒 Security Level: {SecurityLevel}", capabilities.SecurityLevel);
            logger.LogInformation("  🖼️ UI Framework: {UIFramework}", capabilities.NativeUIFramework);
        }
        
        private static void DisplayParentalControls(ParentalControlSettings settings, ILogger logger)
        {
            logger.LogInformation("\n🛡️ Applied Parental Control Settings:");
            logger.LogInformation("  🔒 Content Filter Level: {FilterLevel}", settings.ContentFilterLevel);
            logger.LogInformation("  ⏰ Daily Time Limit: {TimeLimit}", settings.DailyTimeLimit);
            logger.LogInformation("  🚫 Blocked Apps: {BlockedCount}", settings.BlockedApplications.Count);
            logger.LogInformation("  ✅ Allowed Apps: {AllowedCount}", settings.AllowedApplications.Count);
            logger.LogInformation("  🌐 Blocked Websites: {BlockedSites}", settings.BlockedWebsites.Count);
            logger.LogInformation("  📥 Download Approval Required: {RequireApproval}", settings.RequireApprovalForDownloads);
        }
        
        private static void DisplayScreenTimeData(ScreenTimeData screenTimeData, ILogger logger)
        {
            logger.LogInformation("\n⏱️ Screen Time Data for {Date:yyyy-MM-dd}:", screenTimeData.Date);
            logger.LogInformation("  ⏰ Total Screen Time: {TotalTime}", screenTimeData.TotalScreenTime);
            
            if (screenTimeData.ApplicationUsage.Any())
            {
                logger.LogInformation("  📱 Application Usage:");
                foreach (var app in screenTimeData.ApplicationUsage.Take(3))
                {
                    logger.LogInformation("    📋 {AppName}: {Usage}", app.Key, app.Value);
                }
            }
            
            if (screenTimeData.Violations.Any())
            {
                logger.LogInformation("  ⚠️ Time Violations: {ViolationCount}", screenTimeData.Violations.Count);
            }
        }
    }
}