using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PocketFence.FamilyOS.Core;
using PocketFence.FamilyOS.Services;
using PocketFence.FamilyOS.Apps;
using PocketFence.FamilyOS.UI;
using PocketFence.FamilyOS.Examples;
using System;
using System.Linq;
using System.Runtime.Versioning;
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
            // ASCII-only output flag
            if (args.Any(a => a.Equals("--ascii", StringComparison.OrdinalIgnoreCase)))
            {
                FamilyOSUI.EnableAscii();
            }
            // Performance optimization tests mode
            if (args.Length > 0 && args[0].Equals("--perf-opt", StringComparison.OrdinalIgnoreCase))
            {
                await global::FamilyOS.OptimizationTest.RunOptimizationTests();
                return;
            }

            // Check for demo mode
            if (args.Length > 0 && args[0].Equals("--demo-windows", StringComparison.OrdinalIgnoreCase))
            {
                if (OperatingSystem.IsWindows())
                {
                    await RunWindowsDemo();
                }
                else
                {
                    Console.WriteLine("❌ Windows demo mode is only available on Windows platform.");
                }
                return;
            }

            // Enhanced startup with modern UI
            await FamilyOSUI.ShowWelcomeHeaderAsync();

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

                // Enhanced welcome display already shown
                await FamilyOSUI.ShowLoadingAsync("🚀 Initializing family environment...", 1500);

                // Main family interaction loop
                await RunFamilyInteractionLoopAsync(kernel, familyManager);

                // Graceful shutdown with enhanced UI
                await FamilyOSUI.ShowLoadingAsync("🔄 FamilyOS shutting down...", 1000);
                await kernel.ShutdownAsync();
                
                await FamilyOSUI.ShowSuccessAsync("Goodbye! Have a wonderful day!");
            }
            catch (Exception ex)
            {
                FamilyOSUI.ShowError($"Critical error: {ex.Message}");
                FamilyOSUI.ShowInfo("Please contact your system administrator.");
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
                        PocketFenceApiUrl = "https://localhost:5001",
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
                            Console.WriteLine("🏠 Returning to welcome screen...");
                            await Task.Delay(1000);
                            continue;
                        }
                    }

                    // Enhanced main menu with modern styling
                    FamilyOSUI.ShowMainMenu(currentUser.DisplayName, currentUser.AgeGroup.ToString(), currentUser.Role.ToString(), currentUser.LastLoginTime);
                    
                    var choice = FamilyOSUI.GetInput("\n🎯 Select an option");

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
                            await ChangePasswordAsync(familyManager, currentUser);
                            break;
                        case "9":
                            if (currentUser.Role == FamilyRole.Parent)
                            {
                                await DisplayFamilyMembersAsync(familyManager);
                            }
                            else
                            {
                                // Check if non-adult user is trying to switch to potentially adult account
                                if (currentUser.AgeGroup != AgeGroup.Adult && currentUser.AgeGroup != AgeGroup.Parent)
                                {
                                    FamilyOSUI.ShowWarning($"Sorry {currentUser.DisplayName}, children cannot switch to other user accounts.");
                                    FamilyOSUI.ShowInfo("📞 Please ask a parent or guardian to help you switch users.");
                                }
                                else
                                {
                                    await FamilyOSUI.ShowSuccessAsync($"Goodbye, {currentUser.DisplayName}!");
                                    currentUser = null;
                                }
                            }
                            break;
                        case "10":
                            if (currentUser.Role == FamilyRole.Parent)
                            {
                                await PasswordManagementMenuAsync(familyManager, currentUser);
                            }
                            else
                            {
                                FamilyOSUI.ShowError("Parent privileges required for password management.");
                            }
                            break;
                        case "11":
                            if (currentUser.Role == FamilyRole.Parent)
                            {
                                // Check if non-adult user is trying to switch to potentially adult account
                                if (currentUser.AgeGroup != AgeGroup.Adult && currentUser.AgeGroup != AgeGroup.Parent)
                                {
                                    Console.WriteLine($"🚫 Sorry {currentUser.DisplayName}, children cannot switch to other user accounts.");
                                    Console.WriteLine("📞 Please ask a parent or guardian to help you switch users.");
                                }
                                else
                                {
                                    Console.WriteLine($"👋 Goodbye, {currentUser.DisplayName}!");
                                    currentUser = null;
                                }
                            }
                            else
                            {
                                // For non-parent users, case "9" handles user switching
                                goto case "9";
                            }
                            break;
                        case "exit":
                        case "quit":
                        case "0":
                            return;
                        default:
                            FamilyOSUI.ShowError("Invalid option. Please try again.");
                            break;
                    }

                    if (choice != "9" && choice != "11" && choice != "exit" && choice != "quit" && choice != "0")
                    {
                        FamilyOSUI.WaitForInput();
                    }
                }
                catch (Exception ex)
                {
                    FamilyOSUI.ShowError($"Error: {ex.Message}");
                    FamilyOSUI.WaitForInput();
                }
            }
        }

        static async Task<FamilyMember?> AuthenticateUserAsync(FamilyOSKernel kernel)
        {
            while (true)
            {
                FamilyOSUI.ShowLoginInterface();
                
                var username = FamilyOSUI.GetInput("Username");
                
                if (string.IsNullOrWhiteSpace(username))
                    continue;

                var password = FamilyOSUI.GetInput("Password", true);

                if (string.IsNullOrWhiteSpace(password))
                    continue;

                var authenticatedUser = await kernel.AuthenticateFamilyMemberAsync(username, password);
                
                if (authenticatedUser != null)
                {
                    return authenticatedUser;
                }
                
                // Check if account is locked to provide specific message
                var familyManager = kernel.GetService<IFamilyManager>();
                var isLocked = await familyManager.IsAccountLockedAsync(username);
                
                // Authentication failed - provide options with enhanced UI
                if (isLocked)
                {
                    FamilyOSUI.ShowWarning("🔒 Account is temporarily locked due to multiple failed login attempts.");
                    FamilyOSUI.ShowInfo("📞 Please ask a parent to unlock your account or wait 15 minutes.");
                }
                else
                {
                    FamilyOSUI.ShowError("Authentication failed. Invalid username or password.");
                }
                Console.WriteLine();
                FamilyOSUI.ShowInfo("Choose an option:");
                Console.WriteLine("  1. 🔄 Try again");
                Console.WriteLine("  2. 🏠 Return to main menu");
                Console.WriteLine("  3. ❌ Exit FamilyOS");
                Console.WriteLine();
                
                var choice = FamilyOSUI.GetInput("Select an option (1-3)");
                
                switch (choice)
                {
                    case "1":
                        // Continue the loop to try again
                        continue;
                    case "2":
                        // Return null to go back to main menu
                        return null;
                    case "3":
                        // Exit the application
                        Environment.Exit(0);
                        break;
                    default:
                        FamilyOSUI.ShowWarning("Invalid choice. Let's try logging in again...");
                        continue;
                }
            }
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
            Console.WriteLine("  8. 🔐 Change Password");
            
            if (user.Role == FamilyRole.Parent)
            {
                Console.WriteLine("  9. 👨‍👩‍👧‍👦 Family Management (Parent Only)");
                Console.WriteLine("  10. 🔧 Password Management (Parent Only)");
            }
            
            Console.WriteLine($"  {(user.Role == FamilyRole.Parent ? "11" : "9")}. 🚪 Switch User");
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
            
            Console.Clear();
            FamilyOSUI.ShowInfo("Loading system status...");
            await FamilyOSUI.ShowProgressAsync("🔍 Gathering system information", 8);
            
            Console.Clear();
            Console.WriteLine("📊 FamilyOS System Status");
            Console.WriteLine("═══════════════════════════════");
            Console.WriteLine();
            
            // Status with color coding
            Console.ForegroundColor = status.IsRunning ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"🟢 System Running: {status.IsRunning}");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"👨‍👩‍👧‍👦 Family Members: {status.FamilyMemberCount}");
            Console.WriteLine($"📱 Active Apps: {status.ActiveApps}");
            Console.ResetColor();
            
            Console.ForegroundColor = status.ContentFilterActive ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"🔍 Content Filter: {(status.ContentFilterActive ? "Active" : "Inactive")}");
            Console.ResetColor();
            
            Console.ForegroundColor = status.ParentalControlsActive ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"🛡️ Parental Controls: {(status.ParentalControlsActive ? "Active" : "Inactive")}");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"⏱️ System Uptime: {status.SystemUptime.Hours}h {status.SystemUptime.Minutes}m");
            Console.WriteLine($"🕒 Last Updated: {status.LastUpdated:HH:mm:ss}");
            Console.ResetColor();
            
            Console.WriteLine();
            FamilyOSUI.WaitForInput();
            
            await Task.CompletedTask;
        }

        static async Task DisplayFamilyMembersAsync(IFamilyManager familyManager)
        {
            await FamilyOSUI.ShowLoadingAsync("📊 Loading family information...", 1000);
            
            var members = await familyManager.GetFamilyMembersAsync();
            FamilyOSUI.ShowFamilyMembers(members);
            FamilyOSUI.WaitForInput();
        }

        static async Task ChangePasswordAsync(IFamilyManager familyManager, FamilyMember currentUser)
        {
            Console.Clear();
            Console.WriteLine("🔐 Change Password");
            Console.WriteLine("═══════════════════");
            Console.WriteLine();
            
            var currentPassword = FamilyOSUI.GetInput("Current Password", true);
            var newPassword = FamilyOSUI.GetInput("New Password", true);
            var confirmPassword = FamilyOSUI.GetInput("Confirm New Password", true);
            
            if (newPassword != confirmPassword)
            {
                FamilyOSUI.ShowError("New passwords do not match. Please try again.");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                FamilyOSUI.ShowError("Password must be at least 4 characters long.");
                return;
            }
            
            await FamilyOSUI.ShowLoadingAsync("🔄 Updating password...", 1500);
            
            var success = await familyManager.ChangePasswordAsync(currentUser.Username, currentPassword, newPassword, currentUser);
            
            if (success)
            {
                await FamilyOSUI.ShowSuccessAsync("Password changed successfully!");
            }
            else
            {
                FamilyOSUI.ShowError("Failed to change password. Please check your current password.");
            }
        }

        static async Task PasswordManagementMenuAsync(IFamilyManager familyManager, FamilyMember parentUser)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("🔧 Password Management (Parent Only)");
                Console.WriteLine("════════════════════════════════════");
                Console.WriteLine("  1. 🔄 Reset Child's Password");
                Console.WriteLine("  2. 🔓 Unlock Account");
                Console.WriteLine("  3. 📋 View Password Change History");
                Console.WriteLine("  4. 🔍 Check Account Status");
                Console.WriteLine("  0. ⬅️ Back to Main Menu");
                Console.WriteLine();
                
                var choice = FamilyOSUI.GetInput("Select an option");
                
                switch (choice)
                {
                    case "1":
                        await ResetChildPasswordAsync(familyManager, parentUser);
                        break;
                    case "2":
                        await UnlockAccountAsync(familyManager, parentUser);
                        break;
                    case "3":
                        await ViewPasswordHistoryAsync(familyManager, parentUser);
                        break;
                    case "4":
                        await CheckAccountStatusAsync(familyManager);
                        break;
                    case "0":
                        return;
                    default:
                        FamilyOSUI.ShowError("Invalid option. Please try again.");
                        break;
                }
                
                if (choice != "0")
                {
                    FamilyOSUI.WaitForInput();
                }
            }
        }

        static async Task ResetChildPasswordAsync(IFamilyManager familyManager, FamilyMember parentUser)
        {
            Console.WriteLine("\\n🔄 Reset Child's Password");
            Console.WriteLine("===========================");
            
            var members = await familyManager.GetFamilyMembersAsync();
            var children = members.Where(m => m.Role != FamilyRole.Parent).ToList();
            
            if (!children.Any())
            {
                Console.WriteLine("ℹ️ No child accounts found.");
                return;
            }
            
            Console.WriteLine("Select child account:");
            for (int i = 0; i < children.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {children[i].DisplayName} ({children[i].Username})");
            }
            
            Console.Write("Enter number: ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= children.Count)
            {
                var child = children[selection - 1];
                
                Console.Write($"New password for {child.DisplayName}: ");
                var newPassword = ReadPassword();
                
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                {
                    Console.WriteLine("❌ Password must be at least 4 characters long.");
                    return;
                }
                
                var success = await familyManager.ResetPasswordAsync(child.Username, newPassword, parentUser);
                
                if (success)
                {
                    Console.WriteLine($"✅ Password reset successfully for {child.DisplayName}!");
                    if (await familyManager.IsAccountLockedAsync(child.Username))
                    {
                        Console.WriteLine("🔓 Account has also been unlocked.");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Failed to reset password.");
                }
            }
            else
            {
                Console.WriteLine("❌ Invalid selection.");
            }
        }

        static async Task UnlockAccountAsync(IFamilyManager familyManager, FamilyMember parentUser)
        {
            Console.WriteLine("\\n🔓 Unlock Account");
            Console.WriteLine("==================");
            
            var members = await familyManager.GetFamilyMembersAsync();
            var lockedMembers = new List<FamilyMember>();
            
            foreach (var member in members)
            {
                if (await familyManager.IsAccountLockedAsync(member.Username))
                {
                    lockedMembers.Add(member);
                }
            }
            
            if (!lockedMembers.Any())
            {
                Console.WriteLine("ℹ️ No accounts are currently locked.");
                return;
            }
            
            Console.WriteLine("Locked accounts:");
            for (int i = 0; i < lockedMembers.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {lockedMembers[i].DisplayName} ({lockedMembers[i].Username})");
            }
            
            Console.Write("Select account to unlock: ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= lockedMembers.Count)
            {
                var member = lockedMembers[selection - 1];
                await familyManager.UnlockAccountAsync(member.Username, parentUser);
                Console.WriteLine($"✅ Account unlocked for {member.DisplayName}!");
            }
            else
            {
                Console.WriteLine("❌ Invalid selection.");
            }
        }

        static async Task ViewPasswordHistoryAsync(IFamilyManager familyManager, FamilyMember parentUser)
        {
            Console.WriteLine("\\n📋 Password Change History");
            Console.WriteLine("============================");
            
            var members = await familyManager.GetFamilyMembersAsync();
            
            Console.WriteLine("Select family member:");
            for (int i = 0; i < members.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {members[i].DisplayName} ({members[i].Username})");
            }
            
            Console.Write("Enter number: ");
            if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= members.Count)
            {
                var member = members[selection - 1];
                var history = await familyManager.GetPasswordChangeHistoryAsync(member.Username, parentUser);
                
                Console.WriteLine($"\\nPassword change history for {member.DisplayName}:");
                if (history.Any())
                {
                    foreach (var change in history)
                    {
                        Console.WriteLine($"  🕒 {change}");
                    }
                }
                else
                {
                    Console.WriteLine("  ℹ️ No password changes recorded.");
                }
            }
            else
            {
                Console.WriteLine("❌ Invalid selection.");
            }
        }

        static async Task CheckAccountStatusAsync(IFamilyManager familyManager)
        {
            Console.WriteLine("\\n🔍 Account Status Summary");
            Console.WriteLine("===========================");
            
            var members = await familyManager.GetFamilyMembersAsync();
            
            foreach (var member in members)
            {
                var isLocked = await familyManager.IsAccountLockedAsync(member.Username);
                var lockIcon = isLocked ? "🔒" : "🔓";
                var onlineIcon = member.IsOnline ? "🟢" : "⚫";
                
                Console.WriteLine($"{lockIcon} {onlineIcon} {member.DisplayName} ({member.Username})");
                Console.WriteLine($"    Role: {member.Role}");
                Console.WriteLine($"    Last Login: {member.LastLoginTime:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"    Failed Attempts: {member.FailedLoginAttempts}/3");
                
                if (isLocked && member.AccountLockedUntil.HasValue)
                {
                    var timeRemaining = member.AccountLockedUntil.Value.Subtract(DateTime.UtcNow);
                    Console.WriteLine($"    🕒 Locked for: {Math.Max(0, timeRemaining.Minutes)} more minutes");
                }
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

        [SupportedOSPlatform("windows")]
        static async Task RunWindowsDemo()
        {
            await WindowsQuickTest.RunWindowsDemo();
        }
    }
}