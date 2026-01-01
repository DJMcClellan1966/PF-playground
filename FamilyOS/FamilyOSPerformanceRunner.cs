using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PocketFence.FamilyOS.Core;
using PocketFence.FamilyOS.Services;
using PocketFence.FamilyOS.Performance;
using System;
using System.Threading.Tasks;

namespace PocketFence.FamilyOS.TestRunner
{
    /// <summary>
    /// Performance test runner for FamilyOS
    /// Bootstraps the FamilyOS system and runs comprehensive performance tests
    /// </summary>
    public class FamilyOSPerformanceRunner
    {
        public static async Task RunPerformanceTest(string[] args)
        {
            Console.WriteLine("🏡 FamilyOS Performance Test Suite");
            Console.WriteLine("===================================");
            Console.WriteLine("Testing family authentication, content filtering, parental controls,");
            Console.WriteLine("encryption, concurrent users, and memory usage...");
            Console.WriteLine();

            try
            {
                // Build service container
                var services = new ServiceCollection();
                ConfigureServices(services);
                
                var serviceProvider = services.BuildServiceProvider();

                // Initialize FamilyOS components
                await InitializeFamilyOSAsync(serviceProvider);

                // Run performance tests
                var performanceTests = serviceProvider.GetRequiredService<FamilyOSPerformanceTests>();
                var results = await performanceTests.RunAllTestsAsync();

                Console.WriteLine();
                Console.WriteLine("🎉 Performance testing completed!");
                Console.WriteLine($"📊 {results.Count} performance metrics collected");
                
                // Check if we should save results
                Console.WriteLine();
                Console.WriteLine("💾 Save detailed results to file? (y/n)");
                var saveResponse = Console.ReadLine()?.ToLower();
                
                if (saveResponse == "y" || saveResponse == "yes")
                {
                    await SavePerformanceResultsAsync(results);
                }

                Console.WriteLine();
                Console.WriteLine("✨ FamilyOS performance analysis complete!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Performance test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add FamilyOS core services
            services.AddSingleton<ISystemSecurity>(provider =>
                new SystemSecurityService(
                    provider.GetRequiredService<ILogger<SystemSecurityService>>()));

            services.AddSingleton<IFamilyManager>(provider =>
                new FamilyManagerService(
                    provider.GetRequiredService<ILogger<FamilyManagerService>>(),
                    provider.GetRequiredService<ISystemSecurity>()));

            services.AddSingleton<IParentalControls>(provider =>
                new ParentalControlsService(
                    provider.GetRequiredService<ILogger<ParentalControlsService>>(),
                    provider.GetRequiredService<ISystemSecurity>()));

            services.AddSingleton<IContentFilter>(provider =>
                new ContentFilterService(
                    provider.GetRequiredService<ILogger<ContentFilterService>>(),
                    provider.GetRequiredService<ISystemSecurity>()));

            // Add performance test service
            services.AddSingleton<FamilyOSPerformanceTests>();
        }

        private static async Task InitializeFamilyOSAsync(IServiceProvider serviceProvider)
        {
            Console.WriteLine("🔧 Initializing FamilyOS components...");

            // Initialize system security
            var systemSecurity = serviceProvider.GetRequiredService<ISystemSecurity>();
            await systemSecurity.InitializeAsync();

            // Initialize family manager and load profiles
            var familyManager = serviceProvider.GetRequiredService<IFamilyManager>();
            await familyManager.LoadFamilyProfilesAsync();

            // Initialize parental controls
            var parentalControls = serviceProvider.GetRequiredService<IParentalControls>();
            await parentalControls.InitializeAsync();

            // Initialize content filter
            var contentFilter = serviceProvider.GetRequiredService<IContentFilter>();
            await contentFilter.StartAsync();

            Console.WriteLine("✅ FamilyOS initialization complete");
            Console.WriteLine();
        }

        private static async Task SavePerformanceResultsAsync(List<PerformanceResult> results)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var fileName = $"FamilyOS_Performance_Results_{timestamp}.json";
                
                var json = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await System.IO.File.WriteAllTextAsync(fileName, json);
                
                Console.WriteLine($"✅ Results saved to: {fileName}");
                
                // Also create a human-readable report
                var reportFileName = $"FamilyOS_Performance_Report_{timestamp}.txt";
                var report = GenerateHumanReadableReport(results);
                await System.IO.File.WriteAllTextAsync(reportFileName, report);
                
                Console.WriteLine($"📄 Human-readable report saved to: {reportFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to save results: {ex.Message}");
            }
        }

        private static string GenerateHumanReadableReport(List<PerformanceResult> results)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("FamilyOS Performance Test Report");
            report.AppendLine("================================");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            var excellentCount = 0;
            var goodCount = 0;
            var needsOptimizationCount = 0;

            foreach (var result in results)
            {
                report.AppendLine($"{result.Status} {result.TestName}:");
                report.AppendLine($"    {result.Metric}: {result.Value:F2}{result.Unit}");
                report.AppendLine($"    Details: {result.Details}");
                report.AppendLine();

                if (result.Status.StartsWith("✅")) excellentCount++;
                else if (result.Status.StartsWith("⚠️")) goodCount++;
                else if (result.Status.StartsWith("❌")) needsOptimizationCount++;
            }

            report.AppendLine("Performance Summary:");
            report.AppendLine("==================");
            report.AppendLine($"✅ Excellent: {excellentCount} tests");
            report.AppendLine($"⚠️ Good: {goodCount} tests");
            report.AppendLine($"❌ Needs Optimization: {needsOptimizationCount} tests");

            var overallScore = results.Count > 0 ? (excellentCount * 100 + goodCount * 75) / results.Count : 0;
            report.AppendLine();
            report.AppendLine($"Overall FamilyOS Performance Score: {overallScore}%");
            report.AppendLine();

            if (overallScore >= 90)
                report.AppendLine("🌟 Outstanding performance! FamilyOS is running excellently.");
            else if (overallScore >= 75)
                report.AppendLine("👍 Good performance! Minor optimizations recommended.");
            else
                report.AppendLine("⚠️ Performance improvements needed for optimal family experience.");

            report.AppendLine();
            report.AppendLine("Recommendations:");
            report.AppendLine("===============");

            if (needsOptimizationCount > 0)
            {
                report.AppendLine("• Focus on optimizing the components marked as 'Needs Optimization'");
                report.AppendLine("• Consider implementing caching for frequently accessed data");
                report.AppendLine("• Review database query efficiency for family operations");
            }
            
            if (goodCount > 0)
            {
                report.AppendLine("• Monitor components marked as 'Good' under heavy family usage");
                report.AppendLine("• Consider stress testing with more concurrent family members");
            }

            report.AppendLine("• Regular performance monitoring recommended for production deployment");
            report.AppendLine("• Test performance with larger family sizes (10+ members)");

            return report.ToString();
        }
    }
}