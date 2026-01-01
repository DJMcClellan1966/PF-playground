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
                            Console.WriteLine("🏠 Returning to welcome screen...");
                            await Task.Delay(1000);
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
                                    Console.WriteLine($"🚫 Sorry {currentUser.DisplayName}, children cannot switch to other user accounts.");
                                    Console.WriteLine("📞 Please ask a parent or guardian to help you switch users.");
                                }
                                else
                                {
                                    Console.WriteLine($"👋 Goodbye, {currentUser.DisplayName}!");
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
                                Console.WriteLine("❌ Parent privileges required for password management.");
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
                            Console.WriteLine("❓ Invalid option. Please try again.");
                            break;
                    }

                    if (choice != "9" && choice != "11" && choice != "exit" && choice != "quit" && choice != "0")
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
            while (true)
            {
                Console.WriteLine("\\n🔐 Please log in to FamilyOS");
                Console.WriteLine("=============================");
                
                Console.Write("Username: ");
                var username = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrWhiteSpace(username))
                    continue;

                Console.Write("Password: ");
                var password = ReadPassword();

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
                
                // Authentication failed - provide options
                if (isLocked)
                {
                    Console.WriteLine("🔒 Account is temporarily locked due to multiple failed login attempts.");
                    Console.WriteLine("📞 Please ask a parent to unlock your account or wait 15 minutes.");
                }
                else
                {
                    Console.WriteLine("❌ Authentication failed. Invalid username or password.");
                }
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("  1. 🔄 Try again");
                Console.WriteLine("  2. 🏠 Return to main menu");
                Console.WriteLine("  3. ❌ Exit FamilyOS");
                Console.WriteLine();
                
                Console.Write("Select an option (1-3): ");
                var choice = Console.ReadLine()?.Trim();
                
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
                        Console.WriteLine("❓ Invalid choice. Let's try logging in again...");
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

        static async Task ChangePasswordAsync(IFamilyManager familyManager, FamilyMember currentUser)
        {
            Console.WriteLine("\\n🔐 Change Password");
            Console.WriteLine("===================");
            
            Console.Write("Current Password: ");
            var currentPassword = ReadPassword();
            
            Console.Write("New Password: ");
            var newPassword = ReadPassword();
            
            Console.Write("Confirm New Password: ");
            var confirmPassword = ReadPassword();
            
            if (newPassword != confirmPassword)
            {
                Console.WriteLine("❌ New passwords do not match. Please try again.");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                Console.WriteLine("❌ Password must be at least 4 characters long.");
                return;
            }
            
            var success = await familyManager.ChangePasswordAsync(currentUser.Username, currentPassword, newPassword, currentUser);
            
            if (success)
            {
                Console.WriteLine("✅ Password changed successfully!");
            }
            else
            {
                Console.WriteLine("❌ Failed to change password. Please check your current password.");
            }
        }

        static async Task PasswordManagementMenuAsync(IFamilyManager familyManager, FamilyMember parentUser)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("🔧 Password Management (Parent Only)");
                Console.WriteLine("====================================");
                Console.WriteLine("  1. 🔄 Reset Child's Password");
                Console.WriteLine("  2. 🔓 Unlock Account");
                Console.WriteLine("  3. 📋 View Password Change History");
                Console.WriteLine("  4. 🔍 Check Account Status");
                Console.WriteLine("  0. ⬅️ Back to Main Menu");
                Console.WriteLine();
                
                Console.Write("Select an option: ");
                var choice = Console.ReadLine()?.Trim();
                
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
                        Console.WriteLine("❓ Invalid option. Please try again.");
                        break;
                }
                
                if (choice != "0")
                {
                    Console.WriteLine("\\nPress any key to continue...");
                    Console.ReadKey();
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
    }
}