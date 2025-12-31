using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PocketFence.FamilyOS.Core;
using PocketFence.FamilyOS.Services;
using PocketFence.FamilyOS.Apps;
using System;
using System.Threading.Tasks;

namespace PocketFence.FamilyOS
{
    /// <summary>
    /// FamilyOS Startup Program - Entry point for the family-oriented operating system
    /// Integrates with PocketFence AI Kernel for comprehensive family safety
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🏠 PocketFence FamilyOS - Starting...");
            Console.WriteLine("=====================================");

            try
            {
                var host = CreateHostBuilder(args).Build();
                
                var kernel = host.Services.GetRequiredService<FamilyOSKernel>();
                var familyManager = host.Services.GetRequiredService<IFamilyManager>();

                // Start the FamilyOS kernel
                var startSuccess = await kernel.StartAsync();
                
                if (!startSuccess)
                {
                    Console.WriteLine("❌ Failed to start FamilyOS Kernel");
                    return;
                }

                // Display welcome message
                await DisplayWelcomeAsync();

                // Main family interaction loop
                await RunFamilyInteractionLoopAsync(kernel, familyManager);

                // Graceful shutdown
                Console.WriteLine("\n🔄 FamilyOS shutting down...");
                await kernel.ShutdownAsync();
                
                Console.WriteLine("👋 Goodbye! Have a wonderful day!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical error: {ex.Message}");
                Console.WriteLine("Please contact your system administrator.");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Core FamilyOS configuration
                    var config = new FamilyOSConfig
                    {
                        FamilyName = "The Johnson Family", // Customize as needed
                        EnableContentFiltering = true,
                        EnableParentalControls = true,
                        EnableActivityLogging = true,
                        EnableScreenTimeManagement = true,
                        PocketFenceApiUrl = "http://localhost:5000",
                        DataDirectory = "./FamilyData"
                    };

                    services.AddSingleton(config);

                    // Register core services
                    services.AddSingleton<ISystemSecurity, SystemSecurityService>();
                    services.AddSingleton<IFamilyManager, FamilyManagerService>();
                    services.AddSingleton<IParentalControls, ParentalControlsService>();
                    services.AddSingleton<IContentFilter, ContentFilterService>();

                    // Register the main kernel
                    services.AddSingleton<FamilyOSKernel>();

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                });

        static async Task DisplayWelcomeAsync()
        {
            Console.WriteLine("\n🌟 Welcome to PocketFence FamilyOS! 🌟");
            Console.WriteLine("====================================");
            Console.WriteLine("🛡️  Safe computing environment for the whole family");
            Console.WriteLine("📚 Educational content prioritized");
            Console.WriteLine("⏰ Screen time management built-in");
            Console.WriteLine("🔒 Enterprise-grade security protection");
            Console.WriteLine();
            Console.WriteLine("👨‍👩‍👧‍👦 Available family members:");
            Console.WriteLine("  • Parents: mom/parent123, dad/parent123");
            Console.WriteLine("  • Children: sarah/kid123, alex/teen123");
            Console.WriteLine();
        }

        static async Task RunFamilyInteractionLoopAsync(FamilyOSKernel kernel, IFamilyManager familyManager)
        {
            FamilyMember? currentUser = null;

            while (true)
            {
                try
                {
                    // User authentication
                    if (currentUser == null)
                    {
                        currentUser = await AuthenticateUserAsync(kernel);
                        if (currentUser == null)
                        {
                            Console.WriteLine("❌ Authentication failed. Please try again.");
                            continue;
                        }
                    }

                    // Main menu
                    await DisplayMainMenuAsync(currentUser);
                    
                    Console.Write("Select an option: ");
                    var choice = Console.ReadLine()?.Trim();

                    switch (choice?.ToLowerInvariant())
                    {
                        case "1":
                            await LaunchApp("Safe Browser", kernel, currentUser);
                            break;
                        case "2":
                            await LaunchApp("Educational Hub", kernel, currentUser);
                            break;
                        case "3":
                            await LaunchApp("Family Game Center", kernel, currentUser);
                            break;
                        case "4":
                            await LaunchApp("Family Chat", kernel, currentUser);
                            break;
                        case "5":
                            await LaunchApp("Family File Manager", kernel, currentUser);
                            break;
                        case "6":
                            await LaunchApp("Screen Time Manager", kernel, currentUser);
                            break;
                        case "7":
                            await DisplaySystemStatusAsync(kernel);
                            break;
                        case "8":
                            if (currentUser.Role == FamilyRole.Parent)
                            {
                                await DisplayFamilyMembersAsync(familyManager);
                            }
                            else
                            {
                                Console.WriteLine("❌ Parent privileges required for family management.");
                            }
                            break;
                        case "9":
                            Console.WriteLine($"👋 Goodbye, {currentUser.DisplayName}!");
                            currentUser = null;
                            break;
                        case "exit":
                        case "quit":
                        case "0":
                            return;
                        default:
                            Console.WriteLine("❓ Invalid option. Please try again.");
                            break;
                    }

                    if (choice != "9" && choice != "exit" && choice != "quit" && choice != "0")
                    {
                        Console.WriteLine("\\nPress any key to continue...");
                        Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static async Task<FamilyMember?> AuthenticateUserAsync(FamilyOSKernel kernel)
        {
            Console.WriteLine("\\n🔐 Please log in to FamilyOS");
            Console.WriteLine("=============================");
            
            Console.Write("Username: ");
            var username = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrWhiteSpace(username))
                return null;

            Console.Write("Password: ");
            var password = ReadPassword();

            if (string.IsNullOrWhiteSpace(password))
                return null;

            return await kernel.AuthenticateFamilyMemberAsync(username, password);
        }

        static async Task DisplayMainMenuAsync(FamilyMember user)
        {
            Console.Clear();
            Console.WriteLine($"🏠 FamilyOS - Welcome, {user.DisplayName}!");
            Console.WriteLine($"👤 Age Group: {user.AgeGroup} | Role: {user.Role}");
            Console.WriteLine($"🕒 Last Login: {user.LastLoginTime:HH:mm:ss}");
            Console.WriteLine();
            Console.WriteLine("📱 Available Applications:");
            Console.WriteLine("  1. 🌐 Safe Browser");
            Console.WriteLine("  2. 📚 Educational Hub");
            Console.WriteLine("  3. 🎮 Family Game Center");
            Console.WriteLine("  4. 💬 Family Chat");
            Console.WriteLine("  5. 📁 Family File Manager");
            Console.WriteLine("  6. ⏰ Screen Time Manager");
            Console.WriteLine();
            Console.WriteLine("🛠️  System Options:");
            Console.WriteLine("  7. 📊 System Status");
            
            if (user.Role == FamilyRole.Parent)
            {
                Console.WriteLine("  8. 👨‍👩‍👧‍👦 Family Management (Parent Only)");
            }
            
            Console.WriteLine("  9. 🚪 Switch User");
            Console.WriteLine("  0. ❌ Exit FamilyOS");
            Console.WriteLine();
        }

        static async Task LaunchApp(string appName, FamilyOSKernel kernel, FamilyMember user)
        {
            Console.WriteLine($"\\n🚀 Launching {appName}...");
            
            var success = await kernel.LaunchAppAsync(appName, user);
            
            if (success)
            {
                Console.WriteLine($"✅ {appName} launched successfully!");
                
                // Simulate app usage for demo
                Console.WriteLine("📱 App is running... (Simulated)");
                await Task.Delay(2000); // Simulate app running time
                
                Console.WriteLine($"🔒 {appName} closed safely.");
            }
            else
            {
                Console.WriteLine($"❌ Could not launch {appName}");
                Console.WriteLine("💡 This might be due to:");
                Console.WriteLine("   • Age restrictions");
                Console.WriteLine("   • Screen time limits");
                Console.WriteLine("   • Parental controls");
            }
        }

        static async Task DisplaySystemStatusAsync(FamilyOSKernel kernel)
        {
            var status = kernel.GetSystemStatus();
            
            Console.WriteLine("\\n📊 FamilyOS System Status");
            Console.WriteLine("==========================");
            Console.WriteLine($"🟢 System Running: {status.IsRunning}");
            Console.WriteLine($"👨‍👩‍👧‍👦 Family Members: {status.FamilyMemberCount}");
            Console.WriteLine($"📱 Active Apps: {status.ActiveApps}");
            Console.WriteLine($"🔍 Content Filter: {(status.ContentFilterActive ? "Active" : "Inactive")}");
            Console.WriteLine($"🛡️ Parental Controls: {(status.ParentalControlsActive ? "Active" : "Inactive")}");
            Console.WriteLine($"⏱️ System Uptime: {status.SystemUptime.Hours}h {status.SystemUptime.Minutes}m");
            Console.WriteLine($"🕒 Last Updated: {status.LastUpdated:HH:mm:ss}");
            
            await Task.CompletedTask;
        }

        static async Task DisplayFamilyMembersAsync(IFamilyManager familyManager)
        {
            var members = await familyManager.GetFamilyMembersAsync();
            
            Console.WriteLine("\\n👨‍👩‍👧‍👦 Family Members");
            Console.WriteLine("==================");
            
            foreach (var member in members)
            {
                var statusIcon = member.IsOnline ? "🟢" : "⚫";
                Console.WriteLine($"{statusIcon} {member.DisplayName}");
                Console.WriteLine($"   👤 Username: {member.Username}");
                Console.WriteLine($"   🎂 Age Group: {member.AgeGroup}");
                Console.WriteLine($"   👮 Role: {member.Role}");
                Console.WriteLine($"   🛡️ Filter Level: {member.FilterLevel}");
                Console.WriteLine($"   ⏰ Daily Screen Time Limit: {member.ScreenTime.DailyLimit.TotalMinutes} min");
                Console.WriteLine($"   🕒 Last Login: {member.LastLoginTime:yyyy-MM-dd HH:mm}");
                Console.WriteLine();
            }
        }

        static string ReadPassword()
        {
            var password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                    Console.Write("\\b \\b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }
    }
}